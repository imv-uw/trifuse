using Core.Symmetry;
using Microsoft.Xna.Framework;
using NamespaceUtilities;
using System;
using System.Diagnostics;
using Tools;

namespace Fuse
{
    public abstract class TwoAxisRealignment
    {
        public abstract bool GetLocalRealignmentWithTwoRotationCenters(LineTrackingCoordinateSystem axis1, Vector3 center1, LineTrackingCoordinateSystem axis2, Vector3 center2, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float errorDegrees);
        public abstract bool GetLocalRealignmentWithOneRotationCenter(LineTrackingCoordinateSystem unalignedAxis1, Vector3 rotationCenter1, LineTrackingCoordinateSystem unalignedAxis2, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float errorDegrees);
        public abstract bool GetGlobalRealignment(Line axis1, Line axis2, out Matrix matrix);

        public Line Axis1 { get; protected set; }
        public Line Axis2 { get; protected set; }
        public float AngleDegrees { get; protected set; }

        public static TwoAxisRealignment Create(float angle, Line axis1, Line axis2)
        {
            Debug.Assert(0 <= angle && angle <= 180, "Angle must be greater than zero and less than 180");

            float distanceBetweenAxes = Line.GetDistance(axis1, axis2);

            if (distanceBetweenAxes < 0.001 /* arbitrary threshold, oh well */)
            {
                if (angle == 0 || angle == 180)
                {
                    return new SharedAxisRealignment(axis1);
                }

                return new AngleAxisRealignment(angle, axis1, axis2);
            }

            if (angle == 0 || angle == 180)
            {
                return new ParallelAxisRealignment(axis1, axis2);
            }

            throw new NotImplementedException("Nonintersecting, non-parallel axes are not a handled case at the moment");
        }
    }

    public class AngleAxisRealignment : TwoAxisRealignment
    {        
        public AngleAxisRealignment(float angle, Line axis1, Line axis2)
        {
            AngleDegrees = angle;
            Axis1 = axis1;
            Axis2 = axis2;
        }

        // Finds the transform that will map an input set of axes to the globaly defined axes
        public override bool GetGlobalRealignment(Line localAxis1, Line localAxis2, out Matrix matrix)
        {
            bool success = Geometry.TryGetTwoAxisAlignmentMatrix(localAxis1, localAxis2, Axis1, Axis2, out matrix);
            return success;
        }

        // Returns the two coordinate system transformations necessary to place two C-symmetry axes at the desired angle from one another.
        public override bool GetLocalRealignmentWithTwoRotationCenters(LineTrackingCoordinateSystem unalignedAxis1, Vector3 rotationCenter1, LineTrackingCoordinateSystem unalignedAxis2, Vector3 rotationCenter2, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float errorDegrees)
        {
            bool success = Geometry.GetAxesAfterAlignmentToAngleDegrees(unalignedAxis1, rotationCenter1, unalignedAxis2, rotationCenter2, AngleDegrees, out realignedAxis1, out realignedAxis2, out errorDegrees);
            return success;
        }

        // Returns the two coordinate system transformations necessary to place two C-symmetry axes at the desired angle from one another.
        public override bool GetLocalRealignmentWithOneRotationCenter(LineTrackingCoordinateSystem unalignedAxis1, Vector3 rotationCenter, LineTrackingCoordinateSystem unalignedAxis2, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float errorDegrees)
        {
            bool success = Geometry.GetAxesAfterAlignmentToAngleDegrees(unalignedAxis1, rotationCenter, unalignedAxis2, AngleDegrees, out realignedAxis1, out realignedAxis2, out errorDegrees);
            return success;
        }
    }

    public class CyclizeAngleRealignment
    {
        public readonly float Angle = 0;

        public CyclizeAngleRealignment(float angle)
        {
            Angle = angle;
        }

        // Returns the two coordinate system transformations necessary to place two C-symmetry axes at the desired angle from one another.
        public bool GetLocalRealignmentWithTwoRotations(CoordinateSystem unalignedSystem1, Vector3 rotationCenter1, CoordinateSystem unalignedSystem2, Vector3 rotationCenter2, out CoordinateSystem alignedSystem1, out CoordinateSystem alignedSystem2, out float errorDegrees)
        {
            bool success = Geometry.GetCoordinateSystemsAfterAlignmentToAngleDegrees(unalignedSystem1, rotationCenter1, unalignedSystem2, rotationCenter2, Angle, out alignedSystem1, out alignedSystem2, out errorDegrees);
            return success;
        }

        public bool GetGlobalRealignmentToZ(CoordinateSystem cs1, CoordinateSystem cs2, out Matrix matrix)
        {
            Vector3 axis;
            float angleDegrees;
            //Quaternion rotation = VectorMath.GetLocalRotation(cs1.Rotation, cs2.Rotation);
            Quaternion rotation = VectorMath.GetGlobalRotation(cs1.Rotation, cs2.Rotation);
            VectorMath.GetRotationVectorAndAngleDegrees(rotation, out axis, out angleDegrees);

            Vector3 v12 = cs2.Translation - cs1.Translation;
            Vector3 u12 = Vector3.Normalize(v12);
            Vector3 xMidpoint = (cs1.Translation + cs2.Translation) / 2;
            float midpointOffset = (float) (v12.Length() / 2 / Math.Tan(VectorMath.DegreesToRad(Angle) / 2));
            Vector3 center = Vector3.Normalize(Vector3.Cross(axis, u12)) * midpointOffset + xMidpoint;

            LineTrackingCoordinateSystem line = LineTrackingCoordinateSystem.CreateFromPointDirection(center, axis);
            line.ApplyTranslation(-center);

            Debug.Assert(Math.Abs(Vector3.Dot(cs2.Translation, axis) - Vector3.Dot(cs1.Translation, axis)) < 0.1);

            //float rad = (float) VectorMath.GetAngleRadians(line.Direction, Vector3.UnitZ);
            //Vector3 rotationAxis = Vector3.Cross(line.Direction, Vector3.UnitZ);
            //line.Rotate(Vector3.Zero, Quaternion.CreateFromAxisAngle(rotationAxis, -rad));


            line.ApplyRotation(VectorMath.GetRotationQuaternion(axis, Vector3.UnitZ));

            matrix = line.Transform;

            Debug.Assert(Math.Abs(angleDegrees - Angle) < 0.1);
            Debug.Assert(Math.Abs(Vector3.Transform(cs1.Translation, matrix).Length() - Vector3.Transform(cs2.Translation, matrix).Length()) < 0.1);
            //Debug.Assert((Vector3.Transform(cs2.Translation, matrix) - Vector3.Transform(cs1.Translation, matrix)).Length() < 0.1);


            return true;
        }
    }

    public class ParallelAxisRealignment : TwoAxisRealignment
    {
        public ParallelAxisRealignment(Line axis1, Line axis2)
        {
            Axis1 = axis1;
            Axis2 = axis2;

            Trace.Assert((axis1.Direction - axis2.Direction).Length() < 0.00001);
        }

        public override bool GetGlobalRealignment(Line local1, Line local2, out Matrix matrix)
        {
            LineTrackingCoordinateSystem cs1 = new LineTrackingCoordinateSystem(local1);
            LineTrackingCoordinateSystem cs2 = new LineTrackingCoordinateSystem(local2);

            if (VectorMath.GetAngleRadians(local1.Direction, local2.Direction) > 0.001)
            {
                matrix = Matrix.Identity;
                return false;
            }

            // Position the assembly so that
            // 1) the axes point in the same direction as Axis1
            Quaternion rotation1 = VectorMath.GetRotationQuaternion(local1.Direction, Axis1.Direction);
            cs1.ApplyRotation(rotation1);
            cs2.ApplyRotation(rotation1);

            // 2) axis1 local and desired overlap
            Vector3 translateToAxis1 = -cs1.Point;
            cs1.Translation += translateToAxis1;
            cs2.Translation += translateToAxis1;

            // 3) axis2 local is offset from Axis1 in the same direction as Axis2. The scaling is taken care of later.
            float angleDegrees = (float) VectorMath.GetDihedralAngleDegrees(cs2.Point, Axis1.Point, Axis1.Point + 50 * Axis1.Direction, Axis2.Point);
            cs2.ApplyRotationDegrees(Line.CreateFrom(cs1), angleDegrees);
            
            matrix = cs2.Transform;
            
            return true;
        }

        public override bool GetLocalRealignmentWithTwoRotationCenters(LineTrackingCoordinateSystem axis1, Vector3 center1, LineTrackingCoordinateSystem axis2, Vector3 center2, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float errorDegrees)
        {
            Vector3 dir1 = axis1.Direction;
            Vector3 dir2 = axis2.Direction;
            
            if (VectorMath.GetAngleRadians(dir1, dir2) > Math.PI)
                dir1 = -dir1;

            Vector3 average = Vector3.Normalize(dir1 + dir2);

            Matrix mat1 = VectorMath.GetRotationMatrix(dir1, average);
            mat1.Translation += center1 - Vector3.Transform(center1, mat1);

            Matrix mat2 = VectorMath.GetRotationMatrix(dir2, average);
            mat2.Translation += center2 - Vector3.Transform(center2, mat2);

            realignedAxis1 = new LineTrackingCoordinateSystem(axis1);
            realignedAxis2 = new LineTrackingCoordinateSystem(axis2);
            realignedAxis1.ApplyTransform(mat1);
            realignedAxis2.ApplyTransform(mat2);

            errorDegrees = (float) VectorMath.GetAngleDegrees(dir1, average);

            return true;
        }

        // Returns the two coordinate system transformations necessary to place two C-symmetry axes at the desired angle from one another.
        public override bool GetLocalRealignmentWithOneRotationCenter(LineTrackingCoordinateSystem unalignedAxis1, Vector3 rotationCenter, LineTrackingCoordinateSystem unalignedAxis2, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float errorDegrees)
        {

            throw new NotImplementedException();
            //bool success = Geometry.GetAxesAfterAlignmentToAngleDegrees(unalignedAxis1, rotationCenter, unalignedAxis2, Angle, out realignedAxis1, out realignedAxis2, out errorDegrees);
            //return success;
        }
    }

    public class SharedAxisRealignment : TwoAxisRealignment
    {
        public readonly Line Axis1 = null;

        public SharedAxisRealignment(Line axis1)
        {
            Axis1 = axis1;
        }

        public override bool GetGlobalRealignment(Line local1, Line local2, out Matrix matrix)
        {
            // Verify that local1 and local2 overlap
            if (VectorMath.GetAngleRadians(local1.Direction, local2.Direction) > 0.001)
            {
                matrix = Matrix.Identity;
                return false;
            }

            // Move the line so that it intersects the origin and then rotate it such that it points in the Z direction
            LineTrackingCoordinateSystem cs1 = new LineTrackingCoordinateSystem(local1);
            cs1.Translation -= cs1.Origin;
            cs1.ApplyRotation(VectorMath.GetRotationQuaternion(cs1.Direction, Vector3.UnitZ));
            matrix = cs1.Transform;
            return true;
        }

        public override bool GetLocalRealignmentWithTwoRotationCenters(LineTrackingCoordinateSystem axis1, Vector3 center1, LineTrackingCoordinateSystem axis2, Vector3 center2, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float errorDegrees)
        {
            // Set out parameter default values
            realignedAxis1 = new LineTrackingCoordinateSystem(axis1);
            realignedAxis2 = new LineTrackingCoordinateSystem(axis2);
            errorDegrees = float.NaN;

            // Find the points (p1 and p2) where axes (l1 and l2) are nearest and perpendicular to the rotation center
            // Find the plane normals (r1 and r2) defined by each center the new axis (p1->p2) that isn't yet tangent both spheres
            Vector3 p1 = Line.GetNearestPointOnLine(Line.CreateFrom(axis1), center1); 
            Vector3 p2 = Line.GetNearestPointOnLine(Line.CreateFrom(axis2), center2);
            Line p12 = Line.CreateFromTwoPoints(p1, p2);
            Vector3 r1 = Vector3.Normalize(Vector3.Cross(p1 - center1, p12.Direction)); // Plane normals
            Vector3 r2 = Vector3.Normalize(Vector3.Cross(p2 - center2, p12.Direction));
            Vector3 vC12 = center2 - center1;
            Vector3 uC12 = Vector3.Normalize(vC12);

            // Get the "average" plane that is created by rotating each plane about (p1->p2) until they are coincident
            Vector3 r = Vector3.Dot(r1, r2) > 0? Vector3.Normalize(r1 + r2) : Vector3.Normalize(r1 - r2); // This gives the normal for the nearest of two possible mid-planes
            Debug.Assert(Math.Abs(Vector3.Dot(r, p12.Direction)) < 0.001);

            // Project center1 and center2 onto the plane to determine circle centers and their radii
            Vector3 c1projection = center1 + Vector3.Dot(p1 - center1, r) * r;
            Vector3 c2projection = center2 + Vector3.Dot(p2 - center2, r) * r;
            float c1radius = Vector3.Distance(p1, c1projection);
            float c2radius = Vector3.Distance(p2, c2projection);
            Vector3 vProjectionC1C2 = c2projection - c1projection;
            Vector3 uProjectionC1C2 = Vector3.Normalize(vProjectionC1C2);
            float distanceProjectionC12 = vProjectionC1C2.Length();
            Debug.Assert(Math.Abs(Vector3.Dot(p1 - c1projection, r)) < 0.001);
            Debug.Assert(Math.Abs(Vector3.Dot(p2 - c2projection, r)) < 0.001);

            // If one spherical surface fully contains the other, there are no solutions
            if (c1radius + distanceProjectionC12 < c2radius || c2radius + distanceProjectionC12 < c1radius)
                return false;

            // Compute theta (angle between c1->c2 and the p1 on c1 or p2 on c2) 
            //float originalTheta1 = (float) Math.Acos(p2 - c1proj)
            float solutionTheta1 = (float) Math.Acos((c1radius - c2radius) / distanceProjectionC12);
            float solutionTheta2 = c1radius + c2radius < distanceProjectionC12 ? (float)Math.Acos((c2radius + c2radius) / distanceProjectionC12) : float.NaN; // theta2 is only defined for spheres that don't overlap

            // Solutions exist at +/- theta1
            Vector3 positiveTheta1P1 = c1projection + Vector3.Transform(uProjectionC1C2, Quaternion.CreateFromAxisAngle(r, solutionTheta1));
            Vector3 positiveTheta1P2 = c2projection + Vector3.Transform(uProjectionC1C2, Quaternion.CreateFromAxisAngle(r, solutionTheta1));
            Vector3 negativeTheta1P1 = c1projection + Vector3.Transform(uProjectionC1C2, Quaternion.CreateFromAxisAngle(r, -solutionTheta1));
            Vector3 negativeTheta1P2 = c2projection + Vector3.Transform(uProjectionC1C2, Quaternion.CreateFromAxisAngle(r, -solutionTheta1));
            Vector3 uPositiveTheta1 = positiveTheta1P2 - positiveTheta1P1;
            Vector3 uNegativeTheta1 = negativeTheta1P1 - negativeTheta1P2;


            float radErrorPositiveTheta1 = (float)(VectorMath.GetAngleDegrees(positiveTheta1P1 - c1projection, p1 - c1projection) + VectorMath.GetAngleDegrees(positiveTheta1P2 - c2projection, p2 - c2projection));
            float radErrorNegativeTheta1 = (float)(VectorMath.GetAngleDegrees(negativeTheta1P1 - c1projection, p1 - c1projection) + VectorMath.GetAngleDegrees(negativeTheta1P2 - c2projection, p2 - c2projection));

            if(radErrorPositiveTheta1 < radErrorNegativeTheta1)
            {
                errorDegrees = radErrorPositiveTheta1;

                // Move the realigned axes to reflect rotation onto the p1->p2 line segment
                Quaternion rotation1 = VectorMath.GetRotationQuaternion(p1 - center1, positiveTheta1P1 - center1);
                Quaternion rotation2 = VectorMath.GetRotationQuaternion(p2 - center2, positiveTheta1P2 - center2);
                realignedAxis1.Translation -= center1;
                realignedAxis1.ApplyRotation(rotation1);
                realignedAxis1.Translation += center1;
                realignedAxis2.Translation -= center2;
                realignedAxis2.ApplyRotation(rotation2);
                realignedAxis2.Translation += center2;

                rotation1 = Vector3.Dot(realignedAxis1.Direction, uPositiveTheta1) > 0 ? VectorMath.GetRotationQuaternion(realignedAxis1.Direction, uPositiveTheta1) : VectorMath.GetRotationQuaternion(realignedAxis1.Direction, -uPositiveTheta1);
                rotation2 = Vector3.Dot(realignedAxis2.Direction, uPositiveTheta1) > 0 ? VectorMath.GetRotationQuaternion(realignedAxis2.Direction, uPositiveTheta1) : VectorMath.GetRotationQuaternion(realignedAxis2.Direction, -uPositiveTheta1);
                realignedAxis1.Translation -= center1;
                realignedAxis1.ApplyRotation(rotation1);
                realignedAxis1.Translation += center1;
                realignedAxis2.Translation -= center2;
                realignedAxis2.ApplyRotation(rotation2);
                realignedAxis2.Translation += center2;
            }
            else
            {
                errorDegrees = radErrorNegativeTheta1;
                return false;
            }
            // TODO: negative theta1, theta2 positive and negative

            return true;
        }

        // Returns the two coordinate system transformations necessary to place two C-symmetry axes at the desired angle from one another.
        public override bool GetLocalRealignmentWithOneRotationCenter(LineTrackingCoordinateSystem unalignedAxis1, Vector3 rotationCenter, LineTrackingCoordinateSystem unalignedAxis2, out LineTrackingCoordinateSystem realignedAxis1, out LineTrackingCoordinateSystem realignedAxis2, out float errorDegrees)
        {
            throw new InvalidOperationException("One joint is insufficient to align two lines");
        }
    }
}
