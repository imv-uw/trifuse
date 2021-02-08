using Microsoft.Xna.Framework;
using NamespaceUtilities;
using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core.Interfaces;
using Core.Symmetry;

namespace Tools
{
    public static class Geometry
    {
        public static Vector3 GetCenterNCAC(IEnumerable<IAa> residues)
        {
            Vector3 sum = Vector3.Zero;
            int count = 0;
            foreach(IAa residue in residues)
            {
                Vector3 xyzN = residue[Aa.N_].Xyz;
                Vector3 xyzCA = residue[Aa.CA_].Xyz;
                Vector3 xyzC = residue[Aa.C_].Xyz;

                sum += xyzN;
                sum += xyzCA;
                sum += xyzC;
                count += 3;
            }

            Trace.Assert(count > 0, "One or more residues must be given.");
            Trace.Assert(!VectorMath.IsNaN(sum), "NaN entries were found at some N, CA, or C positions.");

            Vector3 center = sum / count;
            return center;
        }

        public static bool GetAxesAfterAlignmentToAngleDegrees(LineTrackingCoordinateSystem axis1, Vector3 rotationCenter1, LineTrackingCoordinateSystem axis2, Vector3 rotationCenter2, float angleDegrees, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float angleErrorDegrees)
        {
            float angleRadians = (float) (angleDegrees * Math.PI / 180);
            float angleErrorRadians = 0;
            bool result = GetAxesAfterAlignmentToAngleRadians(axis1, rotationCenter1, axis2, rotationCenter2, angleRadians, out realignedAxis1, out realignedAxis2, out angleErrorRadians);
            angleErrorDegrees = (float) (angleErrorRadians * 180 / Math.PI);
            return result;
        }

        public static bool GetAxesAfterAlignmentToAngleDegrees(LineTrackingCoordinateSystem axis1, Vector3 rotationCenter, LineTrackingCoordinateSystem axis2, float angleDegrees, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float angleErrorDegrees)
        {
            float angleRadians = (float)(angleDegrees * Math.PI / 180);
            float angleErrorRadians = 0;
            bool result = GetAxesAfterAlignmentToAngleRadians(axis1, rotationCenter, axis2, rotationCenter, angleRadians, out realignedAxis1, out realignedAxis2, out angleErrorRadians);
            angleErrorDegrees = (float)(angleErrorRadians * 180 / Math.PI);
            return result;
        }

        public static bool GetAxesAfterAlignmentToAngleRadians(LineTrackingCoordinateSystem axis1, Vector3 rotationCenter1, LineTrackingCoordinateSystem axis2, Vector3 rotationCenter2, float angleRadians, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float angleErrorRadians)
        {
            realignedAxis1 = null;
            realignedAxis2 = null;
            angleErrorRadians = float.NaN;

            // Find the midpoint of where the lines are nearest to one another
            // Rotate the lines such that the point at the same distance from the rotation center on each line is now coincident with the midpoint
            // Rotate the second line about the normal from the center to plane formed by both lines by the desired number of degrees (i think that works and is def. equivalent to rotating both separately)

            // midpoint
            Line line1 = Line.CreateFrom(axis1);
            Line line2 = Line.CreateFrom(axis2);
            Vector3 midpoint = Line.GetMidpoint(line1, line2);

            // Find points on each line at the same distance from the rotation center as the rotation center is from the midpoint
            float distance1 = Vector3.Distance(rotationCenter1, midpoint);
            float distance2 = Vector3.Distance(rotationCenter2, midpoint);
            Vector3 neighbor11;
            Vector3 neighbor12;
            Line.GetPointsOnLineAtDistance(line1, rotationCenter1, distance1, out neighbor11, out neighbor12);
            Vector3 point1 = VectorMath.GetNearestPoint(midpoint, neighbor11, neighbor12);

            Vector3 neighbor21;
            Vector3 neighbor22;
            Line.GetPointsOnLineAtDistance(line2, rotationCenter2, distance2, out neighbor21, out neighbor22);
            Vector3 point2 = VectorMath.GetNearestPoint(midpoint, neighbor21, neighbor22);

            if (VectorMath.IsNaN(point1) || VectorMath.IsNaN(point2))
            {
                return false;
            }

            // Rotate each line1 and line2 s.t. point1 and point2 are coincident with the midpoint
            realignedAxis1 = new LineTrackingCoordinateSystem(axis1);//.CreateFromPointDirection(line1.Point, line1.Direction);
            realignedAxis1.ApplyTranslation(-rotationCenter1);
            realignedAxis1.ApplyRotation(VectorMath.GetRotationQuaternion(Vector3.Normalize(point1 - rotationCenter1), Vector3.Normalize(midpoint - rotationCenter1)));
            realignedAxis1.ApplyTranslation(rotationCenter1);

            realignedAxis2 = new LineTrackingCoordinateSystem(axis2);//.CreateFromPointDirection(line2.Point, line2.Direction);
            realignedAxis2.ApplyTranslation(-rotationCenter2);
            realignedAxis2.ApplyRotation(VectorMath.GetRotationQuaternion(Vector3.Normalize(point2 - rotationCenter2), Vector3.Normalize(midpoint - rotationCenter2)));
            realignedAxis2.ApplyTranslation(rotationCenter2);

            if (Line.GetDistance(Line.CreateFrom(realignedAxis1), Line.CreateFrom(realignedAxis2)) > 0.5f)
            {
                // floating point error
                return false;
            }

            // Rotate lines about the plane normal to achieve the desired angle between
            float currentAngleRadians = (float)VectorMath.GetAngleRadians(realignedAxis1.Point, midpoint, realignedAxis2.Point);
            float requiredDelta = (angleRadians - currentAngleRadians) / 2;
            Vector3 rotationAxisDirection = Vector3.Cross(Vector3.Normalize(realignedAxis1.Point - midpoint), Vector3.Normalize(realignedAxis2.Point - midpoint));
            Line rotationAxis1 = Line.CreateFromPointAndDirection(rotationCenter1, -rotationAxisDirection);
            Line rotationAxis2 = Line.CreateFromPointAndDirection(rotationCenter2, -rotationAxisDirection);
            realignedAxis1.ApplyRotationRadians(rotationAxis1, requiredDelta);
            realignedAxis2.ApplyRotationRadians(rotationAxis2, -requiredDelta);
            float newAngleRadians = (float) VectorMath.GetAngleRadians(realignedAxis1.Direction, realignedAxis2.Direction);

            if(Line.GetDistance(Line.CreateFrom(realignedAxis1), Line.CreateFrom(realignedAxis2)) >= 1)
            {
                // TODO: This should never happen - debug why it is

                return false;
            }

            angleErrorRadians = Math.Abs(requiredDelta);
            return true;
        }

        public static bool GetCoordinateSystemsAfterAlignmentToAngleDegrees(CoordinateSystem unalignedSystem1, Vector3 rotationCenter1, CoordinateSystem unalignedSystem2, Vector3 rotationCenter2, float degrees, out CoordinateSystem alignedSystem1, out CoordinateSystem alignedSystem2, out float errorDegrees)
        {
#if DEBUG
            //ValidateGeometryUtilities();
#endif

            // Copy the original coordinate systems as a starting point for the aligned systems
            alignedSystem1 = new CoordinateSystem(unalignedSystem1);
            alignedSystem2 = new CoordinateSystem(unalignedSystem2);


            // Track the lever lengths throughout to ensure coordinate system origin w.r.t. rotation coordinates remains the same
            float lengthORC1 = (rotationCenter1 - alignedSystem1.Translation).Length();
            float lengthORC2 = (rotationCenter2 - alignedSystem2.Translation).Length();

            // Fix the rotation angle
            {
                Quaternion localRotation = VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation);
                float desiredRad = (float)VectorMath.DegreesToRad(degrees);
                float currentRad;
                Vector3 uaxis = VectorMath.GetRotationVector(localRotation); // axis of rotation unit vector
                Vector3 uAxis;
                VectorMath.GetRotationVectorAndAngleRadians(localRotation, out uAxis, out currentRad);
                uAxis = Vector3.Transform(uAxis, alignedSystem1.Rotation);

                if (!VectorMath.IsValid(uAxis) || uAxis == Vector3.Zero)
                {
                    errorDegrees = float.NaN;
                    return false;
                }
                    

                float delta = (desiredRad - currentRad) / 2; // divide by two because both systems will have the same rotation applied


                Quaternion fix1 = Quaternion.CreateFromAxisAngle(uAxis, -delta);
                Quaternion fix2 = Quaternion.CreateFromAxisAngle(uAxis, delta);

                alignedSystem1.RotateGlobal(rotationCenter1, fix1);
                alignedSystem2.RotateGlobal(rotationCenter2, fix2);

                Debug.Assert(Math.Abs(lengthORC1 - (rotationCenter1 - alignedSystem1.Translation).Length()) < 0.1);
                Debug.Assert(Math.Abs(lengthORC2 - (rotationCenter2 - alignedSystem2.Translation).Length()) < 0.1);

#if DEBUG
                float newDegrees = (float)VectorMath.GetRotationAngleDegrees(alignedSystem1.Rotation, alignedSystem2.Rotation);
                Debug.Assert(float.IsNaN(newDegrees) || Math.Abs(newDegrees - degrees) < 1 || Math.Abs(360 - newDegrees - degrees) < 1);
                float deltaDegrees = (float)VectorMath.RadToDegrees(delta);
#endif
            }

            // Apply a second rotation - **the same rotation** - to both coordinate systems s.t. their origins project onto the same
            // point on the new axis. This eliminates translation and allows a straight up measurement of rotation angle delta and also eliminates
            // the need for moving the spacer via RMSD-minimization alignment with the moved other two systems.
            {
                Quaternion rotation = VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation);
                Vector3 uAxis;
                float rad;
                VectorMath.GetRotationVectorAndAngleRadians(rotation, out uAxis, out rad);
                uAxis = Vector3.Transform(uAxis, alignedSystem1.Rotation);
                Vector3 vX12 = alignedSystem2.Translation - alignedSystem1.Translation;
                Vector3 vRC12 = rotationCenter2 - rotationCenter1;
                Vector3 uRC12 = Vector3.Normalize(vRC12);
                float lRC12 = vRC12.Length();

                float xHeightAlongAxis = Vector3.Dot(vX12, uAxis);
                float rcHeightAlongAxis = Vector3.Dot(vRC12, uAxis);
                float leverHeightAlongAxis = xHeightAlongAxis - rcHeightAlongAxis;

                Debug.Assert(Math.Abs(Vector3.Dot((alignedSystem2.Translation - rotationCenter2) - (alignedSystem1.Translation - rotationCenter1), uAxis) - leverHeightAlongAxis) < 0.1);
                //Debug.Assert(VectorMath.GetRotationVector(VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation)) == uAxis);

                if (lRC12 < Math.Abs(leverHeightAlongAxis))
                {
                    // No change in direction of the rotation axis can place the two aligned systems at the same height, because one lever arm
                    // is so much longer than the other
                    errorDegrees = float.NaN;
                    return false;
                }

                // Figure out the angle between axis and rc1->rc2 that will make the rc1->rc2 projection cancel out the lever arm projection
                float desiredRcHeightAlongAxis = -leverHeightAlongAxis;
                float radDesired = (float)Math.Acos(desiredRcHeightAlongAxis / lRC12);
                Vector3 rotationAxis = Vector3.Normalize(Vector3.Cross(uRC12, uAxis));

                if(!VectorMath.IsValid(rotationAxis))
                {
                    errorDegrees = float.NaN;
                    return false;
                }

                Quaternion rotationOption1 = Quaternion.CreateFromAxisAngle(rotationAxis, radDesired);
                Quaternion rotationOption2 = Quaternion.CreateFromAxisAngle(rotationAxis, -radDesired);
                Vector3 axisOption1 = Vector3.Transform(uRC12, rotationOption1);
                Vector3 axisOption2 = Vector3.Transform(uRC12, rotationOption2);
                Vector3 uAxisFinal = Vector3.Distance(uAxis, axisOption1) < Vector3.Distance(uAxis, axisOption2) ? axisOption1 : axisOption2;

                float angle = (float)VectorMath.GetAngleRadians(uAxis, uAxisFinal);
                if (angle != 0)
                {
                    Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(uAxis, uAxisFinal)), angle);
                    if (Vector3.Dot(Vector3.Transform(uAxis, quat), uAxisFinal) < 0.99)
                    {
                        throw new Exception();
                    }

                    Quaternion rotationToMakePlanar = quat;
                    if (Vector3.Dot(Vector3.Transform(uAxis, rotationToMakePlanar), uAxisFinal) < 0.99)
                    {
                        throw new Exception();
                    }

                    Vector3 rotationVector = VectorMath.GetRotationVector(rotationToMakePlanar);
                    if (Math.Abs(VectorMath.GetRotationAngleDegrees(rotationToMakePlanar)) < 0.1)
                    {
                        errorDegrees = (float)Math.Max(VectorMath.GetRotationAngleDegrees(unalignedSystem1.Rotation, alignedSystem1.Rotation), VectorMath.GetRotationAngleDegrees(unalignedSystem2.Rotation, alignedSystem2.Rotation));
                        Debug.Assert(Math.Abs(Vector3.Dot((alignedSystem2.Translation - rotationCenter2) - (alignedSystem1.Translation - rotationCenter1), uAxis) - leverHeightAlongAxis) < 0.5); // any rotation should preserve lever height along the axis
                        return true;
                    }

                    // Do it
                    Debug.Assert(Vector3.Cross(rotationVector, uAxis).Length() > 0.99);
                    Debug.Assert(Math.Abs(Vector3.Dot(uAxis, Vector3.Transform(VectorMath.GetRotationVector(Quaternion.Normalize(VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation))), alignedSystem1.Rotation))) > 0.99);
                    //rotationToMakePlanar.W = -rotationToMakePlanar.W;
                    alignedSystem1.RotateGlobal(rotationCenter1, rotationToMakePlanar);
                    alignedSystem2.RotateGlobal(rotationCenter2, rotationToMakePlanar);

                    //Vector3 uAxisQuat = Vector3.Transform(uAxis, rotationToMakePlanar);

                    Quaternion newLocalRotation = VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation);
                    uAxis = Vector3.Transform(VectorMath.GetRotationVector(newLocalRotation), alignedSystem1.Rotation);
                }


                float newRadAxisRC12 = (float)VectorMath.GetAngleRadians(uRC12, uAxis);
           

#if DEBUG && BROKEN
                uAxis = new Vector3(1, 2, 3);
                uAxis.Normalize();
                Quaternion testRotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(uAxis), (float)Math.PI / 4);
                Console.WriteLine("PRE  rc1->x1 dot uaxis = " + Vector3.Dot((alignedSystem1.Translation - rotationCenter1), uAxis));
                Console.WriteLine("PRE  rc2->x2 dot uaxis = " + Vector3.Dot((alignedSystem2.Translation - rotationCenter2), uAxis));
                Console.WriteLine("PRE  rc1->x1 distance = " + (alignedSystem1.Translation - rotationCenter1).Length());
                Console.WriteLine("PRE  rc2->x2 distance = " + (alignedSystem2.Translation - rotationCenter2).Length());

                alignedSystem1.RotateRadians(rotationCenter1, uAxis, (float)Math.PI / 4);
                alignedSystem2.RotateRadians(rotationCenter2, uAxis, (float)Math.PI / 4);
                //alignedSystem1.Rotate(rotationCenter1, testRotation);
                //alignedSystem2.Rotate(rotationCenter2, testRotation);
                //uAxis = Vector3.Transform(uAxis, testRotation);

                Console.WriteLine("POST rc1->x1 dot uaxis = " + Vector3.Dot((alignedSystem1.Translation - rotationCenter1), uAxis));
                Console.WriteLine("POST rc2->x2 dot uaxis = " + Vector3.Dot((alignedSystem2.Translation - rotationCenter2), uAxis));
                Console.WriteLine("POST rc1->x1 distance = " + (alignedSystem1.Translation - rotationCenter1).Length());
                Console.WriteLine("POST rc2->x2 distance = " + (alignedSystem2.Translation - rotationCenter2).Length());
                
#endif

#if DEBUG
                float newDegrees = (float)VectorMath.GetRotationAngleDegrees(alignedSystem1.Rotation, alignedSystem2.Rotation);
                Debug.Assert(float.IsNaN(newDegrees) || Math.Abs(newDegrees - degrees) < 1 || Math.Abs(360 - newDegrees - degrees) < 1);
                //float deltaDegrees = (float)VectorMath.RadToDegrees(delta);
#endif


                Debug.Assert(Math.Abs(uAxis.Length() - 1) < 0.001);
                Debug.Assert(Math.Abs(lengthORC1 - (rotationCenter1 - alignedSystem1.Translation).Length()) < 0.1);
                Debug.Assert(Math.Abs(lengthORC2 - (rotationCenter2 - alignedSystem2.Translation).Length()) < 0.1);

                if (Vector3.Dot(uAxis, uAxisFinal) < 0.99)
                {
                    errorDegrees = -1;
                    return false;
                }

                if (Math.Abs(Vector3.Dot(uAxis, alignedSystem2.Translation - alignedSystem1.Translation)) > 0.1)
                {
                    errorDegrees = -1;
                    return false;
                }
                Debug.Assert(Math.Abs(Vector3.Dot(uAxis, alignedSystem2.Translation) - Vector3.Dot(uAxis, alignedSystem1.Translation)) < 0.1);

                errorDegrees = (float)Math.Max(VectorMath.GetRotationAngleDegrees(unalignedSystem1.Rotation, alignedSystem1.Rotation), VectorMath.GetRotationAngleDegrees(unalignedSystem2.Rotation, alignedSystem2.Rotation));
                if (errorDegrees < 5)
                    return true;
            }

            errorDegrees = (float)Math.Max(VectorMath.GetRotationAngleDegrees(unalignedSystem1.Rotation, alignedSystem1.Rotation), VectorMath.GetRotationAngleDegrees(unalignedSystem2.Rotation, alignedSystem2.Rotation));
            return true;
        }

        public static bool GetCoordinateSystemsAfterAlignmentToAngleDegrees2(CoordinateSystem unalignedSystem1, Vector3 rotationCenter1, CoordinateSystem unalignedSystem2, Vector3 rotationCenter2, float degrees, out CoordinateSystem alignedSystem1, out CoordinateSystem alignedSystem2, out float errorDegrees)
        {
#if DEBUG
            //ValidateGeometryUtilities();
#endif
            
            // Copy the original coordinate systems as a starting point for the aligned systems
            alignedSystem1 = new CoordinateSystem(unalignedSystem1);
            alignedSystem2 = new CoordinateSystem(unalignedSystem2);


            // Track the lever lengths throughout to ensure coordinate system origin w.r.t. rotation coordinates remains the same
            float lengthORC1 = (rotationCenter1 - alignedSystem1.Translation).Length();
            float lengthORC2 = (rotationCenter2 - alignedSystem2.Translation).Length();

            // Fix the rotation angle
            {
                Quaternion localRotation = VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation);
                float desiredRad = (float)VectorMath.DegreesToRad(degrees);
                float currentRad;
                Vector3 uaxis = VectorMath.GetRotationVector(localRotation); // axis of rotation unit vector
                Vector3 uAxis;
                VectorMath.GetRotationVectorAndAngleRadians(localRotation, out uAxis, out currentRad);
                //uAxis = Vector3.Transform(localRotation, alignedSystem1.Rotation);
                float delta = (desiredRad - currentRad) / 2; // divide by two because both systems will have the same rotation applied


                Quaternion fix1 = Quaternion.CreateFromAxisAngle(uAxis, -delta);
                Quaternion fix2 = Quaternion.CreateFromAxisAngle(uAxis, delta);

                alignedSystem1.RotateLocal(rotationCenter1, fix1);
                alignedSystem2.RotateLocal(rotationCenter2, fix2);

                //Debug.Assert(Vector3.Dot(uAxis, VectorMath.GetRotationVector(Quaternion.Normalize(VectorMath.GetQuaternion(alignedSystem1.Rotation, alignedSystem2.Rotation)))) > 0.99);
                Debug.Assert(Math.Abs(Vector3.Dot(uAxis, VectorMath.GetRotationVector(Quaternion.Normalize(VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation))))) > 0.99);


                Debug.Assert(Math.Abs(lengthORC1 - (rotationCenter1 - alignedSystem1.Translation).Length()) < 0.1);
                Debug.Assert(Math.Abs(lengthORC2 - (rotationCenter2 - alignedSystem2.Translation).Length()) < 0.1);

#if DEBUG
                float newDegrees = (float)VectorMath.GetRotationAngleDegrees(alignedSystem1.Rotation, alignedSystem2.Rotation);
                Debug.Assert(float.IsNaN(newDegrees) || Math.Abs(newDegrees - degrees) < 1 || Math.Abs(360 - newDegrees - degrees) < 1);
                float deltaDegrees = (float)VectorMath.RadToDegrees(delta);
#endif
            }

            // Apply a second rotation - **the same rotation** - to both coordinate systems s.t. their origins project onto the same
            // point on the new axis. This eliminates translation and allows a straight up measurement of rotation angle delta and also eliminates
            // the need for moving the spacer via RMSD-minimization alignment with the moved other two systems.
            {
                Quaternion rotation = VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation);
                Vector3 uAxis;
                float rad;
                VectorMath.GetRotationVectorAndAngleRadians(rotation, out uAxis, out rad);
                Vector3 vX12 = alignedSystem2.Translation - alignedSystem1.Translation;
                Vector3 vRC12 = rotationCenter2 - rotationCenter1;
                Vector3 uRC12 = Vector3.Normalize(vRC12);
                float lRC12 = vRC12.Length();
                
                float xHeightAlongAxis = Vector3.Dot(vX12, uAxis);
                float rcHeightAlongAxis = Vector3.Dot(vRC12, uAxis);
                float leverHeightAlongAxis = xHeightAlongAxis - rcHeightAlongAxis;

                Debug.Assert(Math.Abs(Vector3.Dot((alignedSystem2.Translation - rotationCenter2) - (alignedSystem1.Translation - rotationCenter1), uAxis) - leverHeightAlongAxis) < 0.1);
                Debug.Assert(VectorMath.GetRotationVector(VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation)) == uAxis);

                if (lRC12 < Math.Abs(leverHeightAlongAxis))
                {
                    // No change in direction of the rotation axis can place the two aligned systems at the same height, because one lever arm
                    // is so much longer than the other
                    errorDegrees = -1;
                    return false;
                }

                // Figure out the angle between axis and rc1->rc2 that will make the rc1->rc2 projection cancel out the lever arm projection
                float desiredRcHeightAlongAxis = -leverHeightAlongAxis;
                float radDesired = (float)Math.Acos(desiredRcHeightAlongAxis / lRC12);
                Quaternion rotationOption1 = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(uRC12, uAxis)), radDesired);
                Quaternion rotationOption2 = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(uRC12, uAxis)), -radDesired);
                Vector3 axisOption1 = Vector3.Transform(uRC12, rotationOption1);
                Vector3 axisOption2 = Vector3.Transform(uRC12, rotationOption2);
                Vector3 uAxisFinal = Vector3.Distance(uAxis, axisOption1) < Vector3.Distance(uAxis, axisOption2)? axisOption1 : axisOption2;

                float angle = (float) VectorMath.GetAngleRadians(uAxis, uAxisFinal);
                Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(uAxis, uAxisFinal)), angle);
                if (Vector3.Dot(Vector3.Transform(uAxis, quat), uAxisFinal) < 0.99)
                {
                    throw new Exception();
                }

                //Quaternion alternateRotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(uAxis, uRC12)), angle);
                Quaternion rotationToMakePlanar = quat; // VectorMath.GetRotationQuaternion(uAxis, uAxisFinal);
                if (Vector3.Dot(Vector3.Transform(uAxis, rotationToMakePlanar), uAxisFinal) < 0.99)
                {
                    throw new Exception();
                }



                
                Vector3 rotationVector = VectorMath.GetRotationVector(rotationToMakePlanar);
                if (Math.Abs(VectorMath.GetRotationAngleDegrees(rotationToMakePlanar)) < 0.1)
                {
                    errorDegrees = (float)Math.Max(VectorMath.GetRotationAngleDegrees(unalignedSystem1.Rotation, alignedSystem1.Rotation), VectorMath.GetRotationAngleDegrees(unalignedSystem2.Rotation, alignedSystem2.Rotation));
                    Debug.Assert(Math.Abs(Vector3.Dot((alignedSystem2.Translation - rotationCenter2) - (alignedSystem1.Translation - rotationCenter1), uAxis) - leverHeightAlongAxis) < 0.5); // any rotation should preserve lever height along the axis
                    return true;
                }

                // Do it
                Debug.Assert(Vector3.Cross(rotationVector, uAxis).Length() > 0.99);
                Debug.Assert(Math.Abs(Vector3.Dot(uAxis, VectorMath.GetRotationVector(Quaternion.Normalize(VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation))))) > 0.99);
                rotationToMakePlanar.W = -rotationToMakePlanar.W;
                alignedSystem1.RotateLocal(rotationCenter1, rotationToMakePlanar);
                alignedSystem2.RotateLocal(rotationCenter2, rotationToMakePlanar);

                Vector3 uAxisQuat = Vector3.Transform(uAxis, rotationToMakePlanar);
                uAxis = VectorMath.GetRotationVector(VectorMath.GetLocalRotation(alignedSystem1.Rotation, alignedSystem2.Rotation));

                float newRadAxisRC12 = (float)VectorMath.GetAngleRadians(uRC12, uAxis);
                //Debug.Assert(Math.Abs(newRadAxisRC12 - radDesired2) < 0.001);
                //Debug.Assert(Math.Abs(Vector3.Dot((alignedSystem2.Translation - rotationCenter2) - (alignedSystem1.Translation - rotationCenter1), uAxis) - leverHeightAlongAxis) < 0.1); // any rotation should preserve lever height along the axis


#if DEBUG && BROKEN
                uAxis = new Vector3(1, 2, 3);
                uAxis.Normalize();
                Quaternion testRotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(uAxis), (float)Math.PI / 4);
                Console.WriteLine("PRE  rc1->x1 dot uaxis = " + Vector3.Dot((alignedSystem1.Translation - rotationCenter1), uAxis));
                Console.WriteLine("PRE  rc2->x2 dot uaxis = " + Vector3.Dot((alignedSystem2.Translation - rotationCenter2), uAxis));
                Console.WriteLine("PRE  rc1->x1 distance = " + (alignedSystem1.Translation - rotationCenter1).Length());
                Console.WriteLine("PRE  rc2->x2 distance = " + (alignedSystem2.Translation - rotationCenter2).Length());

                alignedSystem1.RotateRadians(rotationCenter1, uAxis, (float)Math.PI / 4);
                alignedSystem2.RotateRadians(rotationCenter2, uAxis, (float)Math.PI / 4);
                //alignedSystem1.Rotate(rotationCenter1, testRotation);
                //alignedSystem2.Rotate(rotationCenter2, testRotation);
                //uAxis = Vector3.Transform(uAxis, testRotation);

                Console.WriteLine("POST rc1->x1 dot uaxis = " + Vector3.Dot((alignedSystem1.Translation - rotationCenter1), uAxis));
                Console.WriteLine("POST rc2->x2 dot uaxis = " + Vector3.Dot((alignedSystem2.Translation - rotationCenter2), uAxis));
                Console.WriteLine("POST rc1->x1 distance = " + (alignedSystem1.Translation - rotationCenter1).Length());
                Console.WriteLine("POST rc2->x2 distance = " + (alignedSystem2.Translation - rotationCenter2).Length());
                
#endif

#if DEBUG
                float newDegrees = (float)VectorMath.GetRotationAngleDegrees(alignedSystem1.Rotation, alignedSystem2.Rotation);
                Debug.Assert(float.IsNaN(newDegrees) || Math.Abs(newDegrees - degrees) < 1 || Math.Abs(360 - newDegrees - degrees) < 1);
                //float deltaDegrees = (float)VectorMath.RadToDegrees(delta);
#endif


                Debug.Assert(Math.Abs(uAxis.Length() - 1) < 0.001);
                Debug.Assert(Math.Abs(lengthORC1 - (rotationCenter1 - alignedSystem1.Translation).Length()) < 0.1);
                Debug.Assert(Math.Abs(lengthORC2 - (rotationCenter2 - alignedSystem2.Translation).Length()) < 0.1);
                //Debug.Assert(Math.Abs(Vector3.Dot((alignedSystem2.Translation - rotationCenter2) - (alignedSystem1.Translation - rotationCenter1), uAxis) - leverHeightAlongAxis) < 0.1); // any rotation should preserve lever height along the axis

                if(Vector3.Dot(uAxis, uAxisFinal) < 0.99)
                {
                    errorDegrees = -1;
                    return false;
                }

                if(Math.Abs(Vector3.Dot(uAxis, alignedSystem2.Translation - alignedSystem1.Translation)) > 0.1)
                {
                    errorDegrees = -1;
                    return false;
                }
                Debug.Assert(Math.Abs(Vector3.Dot(uAxis, alignedSystem2.Translation) - Vector3.Dot(uAxis, alignedSystem1.Translation)) < 0.1);

                errorDegrees = (float)Math.Max(VectorMath.GetRotationAngleDegrees(unalignedSystem1.Rotation, alignedSystem1.Rotation), VectorMath.GetRotationAngleDegrees(unalignedSystem2.Rotation, alignedSystem2.Rotation));
                if(errorDegrees < 5)
                    return true;
            }

            errorDegrees = (float) Math.Max(VectorMath.GetRotationAngleDegrees(unalignedSystem1.Rotation, alignedSystem1.Rotation), VectorMath.GetRotationAngleDegrees(unalignedSystem2.Rotation, alignedSystem2.Rotation));
            return true;
        }

        private static void ValidateGeometryUtilities()
        {
            for (int i = 0; i < 10; i++)
            {
                // Check the proper quaternion is generated to go 'from'->'to'
                Random r = new Random();
                Vector3 v1 = Vector3.Normalize(new Vector3(r.Next(), r.Next(), r.Next()));
                Vector3 v2 = Vector3.Normalize(new Vector3(r.Next(), r.Next(), r.Next()));
                float angle1 = r.Next() % 360;
                float angle2 = r.Next() % 360;

                if (angle1 == 0 || angle2 == 0)
                    continue;

                Quaternion from = Quaternion.CreateFromAxisAngle(v1, (float)VectorMath.DegreesToRad(angle1));
                Quaternion to = Quaternion.CreateFromAxisAngle(v2, (float)VectorMath.DegreesToRad(angle2));
                Quaternion transformQuat = VectorMath.GetLocalRotation(from, to);

                Quaternion fromTo = from * transformQuat;
                //Quaternion fromTo = from;
                float newAngleBetween = VectorMath.GetAngleDegrees(to, fromTo);
                Debug.Assert(Math.Abs(newAngleBetween) < 5);

                float verifyAngle1;
                Vector3 verifyAxis1;
                VectorMath.GetRotationVectorAndAngleDegrees(from, out verifyAxis1, out verifyAngle1);
                Debug.Assert(Vector3.Dot(verifyAxis1, v1) > 0.99 && Math.Abs(verifyAngle1 - angle1) < 1 || Vector3.Dot(verifyAxis1, v1) < -0.99 && Math.Abs(360 - verifyAngle1 - angle1) < 1);
                //Debug.Assert( <= 2);

                Quaternion verifyFrom = VectorMath.GetLocalRotation(Quaternion.Identity, from);
                VectorMath.GetRotationVectorAndAngleDegrees(verifyFrom, out verifyAxis1, out verifyAngle1);
                Debug.Assert(Vector3.Dot(verifyAxis1, v1) > 0.99 && Math.Abs(verifyAngle1 - angle1) < 1 || Vector3.Dot(verifyAxis1, v1) < -0.99 && Math.Abs(360 - verifyAngle1 - angle1) < 1);
            }

            {
                Quaternion rotation1 = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.PI / 2);
                CoordinateSystem cs = new CoordinateSystem();
                cs.Translation = new Vector3(1, 2, 3);
                Console.WriteLine("PRE  = " + cs.Translation.ToString());
                cs.RotateLocal(new Vector3(-1, -2, -3), rotation1);
                Console.WriteLine("POST = " + cs.Translation.ToString());

            }

            {
                for(int i = 0; i < 10; i++)
                {
                    Random r = new Random();
                    Vector3 v1 = Vector3.Normalize(new Vector3(r.Next(), r.Next(), r.Next()));
                    Vector3 v2 = Vector3.Normalize(new Vector3(r.Next(), r.Next(), r.Next()));
                    Quaternion quat = VectorMath.GetRotationQuaternion(v1, v2);
                    Debug.Assert(Vector3.Dot(Vector3.Transform(v1, quat), v2) > 0.99);
                }

            }

            
        }

        public static Matrix GetAxisRotationRadians(Vector3 axisXyz1, Vector3 axisXyz2, double radians)
        {
            // Rotate the the residue about the center1->center2 axis by the specified angle
            Vector3 axis = axisXyz2 - axisXyz1; axis.Normalize();
            Debug.Assert(axis.Length() - 1 <= 0.00001);

            Matrix matrix = Matrix.Identity;
            matrix.Translation -= axisXyz1;
            matrix *= Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(axis /* this expects a unit vector */, (float)radians));
            matrix.Translation += axisXyz1;
            return matrix;
        }

        /// <summary>
        /// Return a premultiplication matrix required to place the input system of axes in the same coordinate
        /// system as the base coordinate system from which all subunit coordinate systems derive. 
        /// </summary>
        /// <param name="move1"></param>
        /// <param name="move2"></param>
        /// <param name="stay1"></param>
        /// <param name="stay2"></param>
        /// <returns></returns>
        public static bool TryGetTwoAxisAlignmentMatrix(Line move1, Line move2, Line stay1, Line stay2, out Matrix matrix, float maxError = 0.2f)
        {
            if (Line.GetDistance(move1, move2) > maxError)
            {
                Console.WriteLine("Failed to align axes within {0:F4} angstroms, distance was {1:F4}).", maxError, Line.GetDistance(move1, move2));
                matrix = Matrix.Identity;
                return false;
            }
            
            // Create working copies of the lines to be moved and make sure they have the same relative directionality as stationary axes
            // by switching directions if necessary
            LineTrackingCoordinateSystem axisCopy1 = LineTrackingCoordinateSystem.CreateFromPointDirection(move1.Point, move1.Direction);
            LineTrackingCoordinateSystem axisCopy2 = LineTrackingCoordinateSystem.CreateFromPointDirection(move2.Point, move2.Direction);

            if (Vector3.Dot(stay1.Direction, stay2.Direction) * Vector3.Dot(axisCopy1.Direction, axisCopy2.Direction) < 0)
                axisCopy2.OriginalDirection *= -1;

            // Track rotations and translations to align axes to principal axes
            Quaternion alignAxis1 = VectorMath.GetRotationQuaternion(axisCopy1.Direction, stay1.Direction);
            axisCopy1.ApplyRotation(alignAxis1);
            axisCopy2.ApplyRotation(alignAxis1);

            float angleError2 = VectorMath.GetDihedralAngleRadians(axisCopy2.Direction, axisCopy1.Direction, stay2.Direction);
            Quaternion alignAxis2 = Quaternion.CreateFromAxisAngle(axisCopy1.Direction, -angleError2);
            axisCopy1.ApplyRotation(alignAxis2);
            axisCopy2.ApplyRotation(alignAxis2);

            Vector3 intersect = Line.GetNearestPointOnLine(Line.CreateFrom(axisCopy1), Line.CreateFrom(axisCopy2));
            axisCopy1.ApplyTranslation(-intersect);
            axisCopy2.ApplyTranslation(-intersect);

            matrix = axisCopy1.Transform;

            //Debug.Assert((stay1.Direction - Vector3.Transform(move1.Direction, matrix.Rotation)).Length() < 0.1f);
            //Debug.Assert((stay2.Direction - Vector3.Transform(move2.Direction, matrix.Rotation)).Length() < 0.1f);
            return true;
        }

    }
}
