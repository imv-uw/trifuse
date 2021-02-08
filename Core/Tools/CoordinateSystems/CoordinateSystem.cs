using Core.Interfaces;
using Microsoft.Xna.Framework;
using NamespaceUtilities;
using System;
using System.Diagnostics;
using Tools;

namespace Core.Symmetry
{
    [Serializable]
    public class CoordinateSystem : ICoordinateFrame
    {
        public Vector3 Translation { get { return Transform.Translation; } set { Transform.Translation = value; } } // Origin of this coordinate system in World-space
        public Quaternion Rotation { get { return Transform.Rotation; } }    // Rotation of World-space before application of Translation
        public Matrix Transform = Matrix.Identity;                           // Cumulative application of Rotation followed by Translation

        public Vector3 UnitX { get { return Vector3.Transform(Vector3.UnitX, Rotation); } } // Unit vectors of this coordinate system in World-space
        public Vector3 UnitY { get { return Vector3.Transform(Vector3.UnitY, Rotation); } }

        public void Copy(CoordinateSystem other)
        {
            Transform = other.Transform;
        }

        public Vector3 UnitZ { get { return Vector3.Transform(Vector3.UnitZ, Rotation); } }

        public Vector3 Origin
        {
            get
            {
                return Translation;
            }
        }

        public CoordinateSystem() { }
        public CoordinateSystem(CoordinateSystem other)
        {
            Transform = other.Transform;
        }

        public static CoordinateSystem FromUnitXY(Vector3 unitX, Vector3 unitY)
        {
#if DEBUG            
            Vector3 unitZ = Vector3.Cross(unitX, unitY);

            float xLength = unitX.Length();
            float yLength = unitY.Length();
            float zLength = unitZ.Length();
            Debug.Assert(0.999 < xLength && xLength < 1.001);
            Debug.Assert(0.999 < yLength && yLength < 1.001);
            Debug.Assert(0.999 < zLength && zLength < 1.001);
#endif
            CoordinateSystem system = new CoordinateSystem();
            system.RotateGlobal(Vector3.Zero, VectorMath.GetRotationQuaternion(Vector3.UnitX, unitX));
            system.RotateGlobal(Vector3.Zero, VectorMath.GetRotationQuaternion(system.UnitY, unitY));
            return system;
        }

        public static float GetRotationAngleViaRmsdInDegrees(CoordinateSystem move, CoordinateSystem reference)
        {
            float angleRadians = GetRotationAngleViaRmsdInRadians(move, reference);
            float angleDegrees = (float)(angleRadians * 180 / Math.PI);
            return angleDegrees;
        }

        public static float GetRotationAngleViaQuaternionsInDegrees(CoordinateSystem move, CoordinateSystem reference)
        {
            float angleRadians = GetRotationAngleViaQuaternionsInRadians(move, reference);
            float angleDegrees = (float)(angleRadians * 180 / Math.PI);
            return angleDegrees;
        }

        public static float GetRotationAngleViaRmsdInRadians(CoordinateSystem move, CoordinateSystem reference)
        {
            Vector3[] moveVectors = new Vector3[] { move.UnitX, move.UnitY, move.UnitZ, Vector3.Zero };
            Vector3[] referenceVectors = new Vector3[] { reference.UnitX, reference.UnitY, reference.UnitZ, Vector3.Zero };
            Matrix rotationAlignment = VectorMath.GetRmsdAlignmentMatrix(referenceVectors, false, moveVectors, false); // Find the rotation to move from reference frame to the move-frame
            float angle = (float)Math.Acos(rotationAlignment.Rotation.W) * 2;
            if (angle > Math.PI)
                angle = (float)(angle - 2 * Math.PI);
            if (angle < -Math.PI)
                angle = (float)(angle + 2 * Math.PI);
            return angle;

        }

        /// <summary>
        /// Computes the minimal rotation angle required to align the reference coordinate system to other, ignoring the final translation
        /// that would be required to merge them.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static float GetRotationAngleViaQuaternionsInRadians(CoordinateSystem move, CoordinateSystem reference)
        {
            Quaternion referenceInverse = Quaternion.Inverse(reference.Rotation);
            Quaternion moveRelativeToReference = move.Rotation * referenceInverse;
            moveRelativeToReference.Normalize();
            float angle = (float)Math.Acos(moveRelativeToReference.W) * 2;
            if (angle > Math.PI)
                angle = (float) (angle - 2 * Math.PI);
            if (angle < -Math.PI)
                angle = (float)(angle + 2 * Math.PI);
            Debug.Assert(-Math.PI <= angle && angle <= Math.PI);
            return angle;
        }

        public void RotateLocal(Vector3 origin, Quaternion quaternion)
        {

#if METHOD1
            
            quaternion.Normalize();

            
            Translation -= origin;
            Transform = Matrix.CreateFromQuaternion(quaternion) * Transform; // Note: order of application for quaternions to go from->to is from * quat -> to, but opposite for matrices, as in quat operating on matrix
            Translation += origin;
#else

            //throw new Exception("Either this method doesn't work or the caller's quaternion generation doesn't work");
            Quaternion rotation = Rotation;
            Vector3 translation = Translation;

            //rotation.Normalize();
            //quaternion.Normalize();

            Quaternion finalRotation = rotation * quaternion;// Quaternion.Concatenate(rotation, quaternion);
            Vector3 finalOffset = Vector3.Transform((translation - origin), quaternion) + origin;

            Transform = Matrix.CreateFromQuaternion(finalRotation);
            Translation = finalOffset;
#endif
        }

        public void RotateGlobal(Vector3 origin, Quaternion quaternion)
        {
            //throw new Exception("Either this method doesn't work or the caller's quaternion generation doesn't work");
            Quaternion rotation = Rotation;
            Vector3 translation = Translation;

            //rotation.Normalize();
            //quaternion.Normalize();

            Quaternion finalRotation = quaternion * rotation;// Quaternion.Concatenate(rotation, quaternion);
            Vector3 finalOffset = Vector3.Transform((translation - origin), quaternion) + origin;

            Transform = Matrix.CreateFromQuaternion(finalRotation);
            Translation = finalOffset;
        }

        public void RotateDegrees(Vector3 origin, Vector3 axis, float radians)
        {
            RotateRadians(origin, axis, (float) VectorMath.DegreesToRad(radians));
        }

        public void RotateRadians(Vector3 origin, Vector3 axis, float radians)
        {
            Matrix transform = Matrix.CreateFromAxisAngle(axis, (float)radians);
            Translation -= origin;
            Transform *= transform;
            Translation += origin;
        }

        public void GetCoordinateSystem(out Vector3 origin, out Vector3 unitX, out Vector3 unitY, out Vector3 unitZ)
        {
            origin = Origin;
            unitX = UnitX;
            unitY = UnitY;
            unitZ = UnitZ;
        }



        public void ApplyRotation(Quaternion quaternion)
        {
            Transform = Matrix.Multiply(Transform, Matrix.CreateFromQuaternion(quaternion));
            Transform.Rotation.Normalize();
        }

        public void ApplyTransform(Matrix matrix)
        {
            Transform = Matrix.Multiply(Transform, matrix);
            Transform.Rotation.Normalize();
        }

        public void ApplyTranslation(Vector3 translation)
        {
            Transform.Translation += translation;
        }

        public void ApplyRotationDegrees(Line line, float degrees)
        {
            float radians = (float)(degrees * Math.PI / 180);
            ApplyRotationRadians(line, radians);
        }

        public void ApplyRotationRadians(Line line, float radians)
        {
            Debug.Assert(Math.Abs(line.Direction.Length() - 1) < 0.001);

            Matrix rotation = Matrix.CreateFromAxisAngle(line.Direction, radians);
            Vector3 additionalOffset = line.Point - Vector3.Transform(line.Point, rotation);
            rotation.Translation = additionalOffset;

            Transform = Transform * rotation;
        }
    }
}
