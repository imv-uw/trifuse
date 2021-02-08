using Core.Symmetry;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;

namespace Tools
{
    public class LineTrackingCoordinateSystem : CoordinateSystem
    {
        public Vector3 OriginalPoint;
        public Vector3 OriginalDirection;

        public LineTrackingCoordinateSystem() { }

        public LineTrackingCoordinateSystem(Vector3 position, Vector3 direction)
        {
            OriginalPoint = position;
            OriginalDirection = Vector3.Normalize(direction);
        }

        public LineTrackingCoordinateSystem(Line line)
        {
            OriginalPoint = line.Point;
            OriginalDirection = line.Direction;
        }

        public LineTrackingCoordinateSystem(LineTrackingCoordinateSystem other)
            : base(other)
        {
            OriginalDirection = other.OriginalDirection;
            OriginalPoint = other.OriginalPoint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <param name="identity">Whether the transform matrix should be identity, vs copied from the other</param>
        public LineTrackingCoordinateSystem(LineTrackingCoordinateSystem other, bool identity)
        {
            if(identity)
            {
                OriginalDirection = other.Direction;
                OriginalPoint = other.Point;
            }
            else
            {
                OriginalDirection = other.OriginalDirection;
                OriginalPoint = other.OriginalPoint;
                base.Copy((CoordinateSystem)other);
            }
        }

        public Vector3 Point { get { return Vector3.Transform(OriginalPoint, Transform); } }
        public Vector3 Direction { get { return Vector3.Transform(OriginalDirection, Transform.Rotation); } }

        public static LineTrackingCoordinateSystem CreateFromPointDirection(Vector3 point, Vector3 direction)
        {
            LineTrackingCoordinateSystem line = new LineTrackingCoordinateSystem();
            line.OriginalPoint = point;
            line.OriginalDirection = direction;
            line.OriginalDirection.Normalize();
            return line;
        }


        public static bool Equals(LineTrackingCoordinateSystem one, LineTrackingCoordinateSystem two, float rotationDegreesMax, float translationMax)
        {
            float degrees = CoordinateSystem.GetRotationAngleViaQuaternionsInDegrees(one, two);
            if (degrees < rotationDegreesMax && Vector3.DistanceSquared(one.Point, two.Point) < translationMax * translationMax)
                return true;
            return false;
        }

        public bool Equals(LineTrackingCoordinateSystem other, float rotationDegreesMax, float translationMax)
        {
            return LineTrackingCoordinateSystem.Equals(this, other, rotationDegreesMax, translationMax);
        }
    }
}
