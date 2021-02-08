using Microsoft.Xna.Framework;
using NamespaceUtilities;
using System;

namespace Tools
{
    public class Line
    {
        public Vector3 Point;
        public Vector3 Direction;

        public Line() { }

        public Line(Line other)
        {
            Point = other.Point;
            Direction = other.Direction;
        }

        public static Line CreateFromTwoPoints(Vector3 point1, Vector3 point2)
        {
            Line line = new Line();
            line.Point = point1;
            line.Direction = Vector3.Normalize(point2 - point1);
            return line;            
        }

        public static Line CreateFromPointAndDirection(Vector3 point, Vector3 direction)
        {
            Line line = new Line();
            line.Point = point;
            line.Direction = Vector3.Normalize(direction);
            return line;
        }

        public static Line CreateFrom(LineTrackingCoordinateSystem other)
        {
            return Line.CreateFromPointAndDirection(other.Point, other.Direction);
        }

        public static Vector3 GetNearestPointOnLine(Line line, Vector3 point)
        {
            Vector3 p1p2 = line.Point - point;
            Vector3 nearestPoint = line.Point - line.Direction * Vector3.Dot(p1p2, line.Direction);
            return nearestPoint;
        }

        public static void GetPointsOnLineAtDistance(Line line, Vector3 point, float distance, out Vector3 neighbor1, out Vector3 neighbor2)
        {
            Vector3 rightAngleCoordinate = Line.GetNearestPointOnLine(line, point);

            // An unusual case is when the point is on the line, so the result is a simple offset in both directions
            if (rightAngleCoordinate == point)
            {
                Vector3 offset = line.Direction * distance;
                neighbor1 = point + offset;
                neighbor2 = point - offset;
                return;
            }

            // If the point and line are too far apart, it is impossible to find a point on the line at the desired distance
            float pointLineDistance = Vector3.Distance(point, rightAngleCoordinate);
            if(pointLineDistance > distance)
            {
                neighbor1 = VectorMath.NaN;
                neighbor2 = VectorMath.NaN;
                return;
            }

            if(pointLineDistance == distance)
            {
                neighbor1 = rightAngleCoordinate;
                neighbor2 = rightAngleCoordinate;
                return;
            }

            {
                float scale = (float)Math.Sqrt(distance * distance - pointLineDistance * pointLineDistance);
                Vector3 offset = line.Direction * scale;
                neighbor1 = rightAngleCoordinate + offset;
                neighbor2 = rightAngleCoordinate - offset;
            }
        }

        /// <summary>
        /// http://morroworks.com/Content/Docs/Rays%20closest%20point.pdf
        /// It took me a little while to realize that in the diagram, a, b, and c are not unit vectors. However in my lines they are, so "a dot a" and 
        /// similar can be replaced by 1. I am using c as in their definition, i.e. not a unit vector.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Vector3 GetNearestPointOnLine(Line line, Line other)
        {
            Vector3 A = line.Point;
            Vector3 B = other.Point;
            Vector3 a = line.Direction;
            Vector3 b = other.Direction;
            //Vector3 c = Vector3.Normalize(B - A);
            Vector3 c = B - A;

            float ab = Vector3.Dot(a, b);
            float bc = Vector3.Dot(b, c);
            float ac = Vector3.Dot(a, c);
            Vector3 closest = A + a * (-ab * bc + ac) / (1 - ab * ab);
            return closest;
        }

        public static Vector3 GetMidpoint(Line one, Line two)
        {
            Vector3 a = GetNearestPointOnLine(one, two);
            Vector3 b = GetNearestPointOnLine(two, one);
            Vector3 midpoint = (a + b) / 2;
            return midpoint;
        }

        public static float GetDistance(Line line, Line other)
        {
            Vector3 p12 = other.Point - line.Point;
            if (p12.LengthSquared() == 0)
                return 0;

            Vector3 normal = Vector3.Normalize(Vector3.Cross(line.Direction, other.Direction));
            float distance = Vector3.Dot(normal, p12);
            return (float) Math.Abs(distance);
        }

        public static float GetDistance(Line line, Vector3 point)
        {
            double distance = Math.Sqrt(GetDistanceSquared(line, point));
            return (float) distance;
        }

        public static float GetDistanceSquared(Line line, Line other)
        {
            float distance = GetDistance(line, other);
            return distance * distance;
        }

        public static float GetDistanceSquared(Line line, Vector3 point)
        {
            Vector3 p12 = line.Point - point;
            Vector3 pointToLine = p12 - line.Direction * Vector3.Dot(p12, line.Direction);
            float distanceSquared = pointToLine.LengthSquared();
            return distanceSquared;
        }
    }
}
