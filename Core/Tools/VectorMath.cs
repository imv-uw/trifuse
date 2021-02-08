using System;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNetMatrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using DenseMatrix = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix;


namespace NamespaceUtilities
{
    public class VectorMath
    {
        public static readonly Vector3 NaN = new Vector3(float.NaN, float.NaN, float.NaN);

        public static bool IsValid(Vector3 vector)
        {
            return !IsNaN(vector) && !IsInfinity(vector);
        }

        public static bool IsNaN(Vector3 vector)
        {
            return float.IsNaN(vector.X + vector.Y + vector.Z);
        }

        public static float Distance2(Vector3[] one, Vector3[] two)
        {
            Trace.Assert(one.Length == two.Length);

            int count = 0;
            float distance2 = 0;
            for (int i = 0; i < one.Length; i++)
            {
                if (VectorMath.IsNaN(one[i]) || VectorMath.IsNaN(two[i]))
                    continue;
                count++;
                distance2 += Vector3.DistanceSquared(one[i], two[i]);
            }
            if (count == 0)
                return 0;

            distance2 /= count;
            return distance2;
        }

        public static float Distance(Vector3[] one, Vector3[] two)
        { 
            return (float) Math.Sqrt(Distance2(one, two));
        }

        static public double RadToDegrees(double radians)
        {
            double degrees = radians / Math.PI * 180;
            return degrees;
        }

        static public double DegreesToRad(double degrees)
        {
            double rad = degrees / 180 * Math.PI;
            return rad;
        }

        public static float DotBounded(Vector3 one, Vector3 two)
        {
            return Math.Max(-1f, Math.Min(1, Vector3.Dot(one, two)));
        }

        public static float DotBounded(Quaternion one, Quaternion two)
        {
            return Math.Max(-1f, Math.Min(1, Quaternion.Dot(one, two)));
        }

        /// <summary>
        /// Find the quaternion Q s.t. "from * Q == to". Q is being applied in the local coordinate system of 'from'.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Quaternion GetLocalRotation(Quaternion from, Quaternion to)
        {
            to.Normalize();
            from.Normalize();

            Quaternion fromInverse = Quaternion.Inverse(from);
            //Quaternion fromInverse = from;
            //fromInverse.Conjugate();

            Quaternion rotation = fromInverse * to; //Quaternion.Normalize(fromInverse * to);
            
            return rotation;
        }

        /// <summary>
        /// Find the quaternion 'Q' s.t. "Q * from == to", where Q is 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Quaternion GetGlobalRotation(Quaternion from, Quaternion to)
        {

            //throw new NotImplementedException("I don't understand this");

            to.Normalize();
            from.Normalize();

            Quaternion fromInverse = Quaternion.Inverse(from);
            //Quaternion fromInverse = from;
            //fromInverse.Conjugate();

            Quaternion rotation = to * fromInverse; //Quaternion.Normalize(fromInverse * to);

            return rotation;
        }

        public static void GetRotationVectorAndAngleRadians(Quaternion rotation, out Vector3 vector, out float radians)
        {
            // Either normalize the rotation (primarily to deal with W being ever so slightly past the bounds [-1, 1]
            // rotation.Normalize()
            float x = rotation.X;// / sq1mW2;
            float y = rotation.Y;// / sq1mW2;
            float z = rotation.Z;// / sq1mW2;

            if (x == 0 && y == 0 && z == 0)
            {
                vector = Vector3.Zero;
                radians = 0;
                return;
            }

            // This was selected because it's slightly faster and results in roughly the same answer - rotation by 0 or 180 degrees
            if (rotation.W < -1)
                rotation.W = -1;

            if (1 < rotation.W)
                rotation.W = 1;

            vector = Vector3.Normalize(new Vector3(x, y, z));
            radians = (float)(2 * Math.Acos(rotation.W));

            if(rotation.W < 0)
            {
                vector = -vector;
                radians = (float) (2 * Math.PI - radians);
            }

            Debug.Assert(!VectorMath.IsNaN(vector));
            Debug.Assert(Math.Abs(vector.Length() - 1) < 0.1 || radians == 0);
        }

        public static void GetRotationVectorAndAngleDegrees(Quaternion rotation, out Vector3 vector, out float degrees)
        {
            float radians = 0;
            GetRotationVectorAndAngleRadians(rotation, out vector, out radians);
            degrees = (float) RadToDegrees(radians);

        }

        public static Vector3 GetRotationVector(Quaternion rotation)
        {
            Vector3 vector;
            float radians;
            GetRotationVectorAndAngleRadians(rotation, out vector, out radians);
            return vector;
        }

        public static float GetRotationAngleDegrees(Quaternion rotation)
        {
            float degrees = (float) RadToDegrees(GetRotationAngleRadians(rotation));
            return degrees;
        }

        public static float GetRotationAngleRadians(Quaternion rotation)
        {
            Vector3 vector;
            float radians;
            GetRotationVectorAndAngleRadians(rotation, out vector, out radians);
            return radians;
        }

        public static double GetRotationAngleRadians(Quaternion from, Quaternion to)
        {
            Quaternion rotation = GetLocalRotation(from, to);
            Vector3 vector;
            float radians;
            GetRotationVectorAndAngleRadians(rotation, out vector, out radians);
            return radians;
        }

        public static double GetRotationAngleDegrees(Quaternion from, Quaternion to)
        {
            float degrees = (float) RadToDegrees(GetRotationAngleRadians(from, to));
            return degrees;
        }

        public static Vector3 Rotate(Vector3 coordinate, Vector3 origin, Quaternion rotation)
        {
            coordinate -= origin;
            Vector3.Transform(coordinate, rotation);
            coordinate += origin;
            return coordinate;
        }

        public static Vector3 RotateDegrees(Vector3 coordinate, Vector3 origin, Vector3 axis, float degrees)
        {
            Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), (float) DegreesToRad(degrees));
            Vector3 result = Rotate(coordinate, origin, rotation);
            return result;
        }

        // Returns the angle between planes a-c1-c2 and c1-c2-b
        public static double GetDihedralAngleRadians(Vector3 a, Vector3 center1, Vector3 center2, Vector3 b)
        {
            double ret;

            // These vectors are: a -> center1, center1 -> center2, and center2 -> b
            Vector3 vector1 = center1 - a; vector1.Normalize();
            Vector3 vector2 = center2 - center1; vector2.Normalize();
            Vector3 vector3 = b - center2; vector3.Normalize();

            Vector3 vector1Scaled = Vector3.Multiply(vector1, vector2.Length());
            Vector3 vector2Cross3 = Vector3.Cross(vector2, vector3);
            Vector3 vector1Cross2 = Vector3.Cross(vector1, vector2);
            ret = Math.Atan2(Vector3.Dot(vector1Scaled, vector2Cross3), Vector3.Dot(vector1Cross2, vector2Cross3));
            return ret;
        }

        // Find the angle in [-Pi, Pi]
        public static float GetDihedralAngleRadians(Vector3 vector1, Vector3 vector2, Vector3 vector3)
        {
            double result;

            // These vectors are: a -> center1, center1 -> center2, and center2 -> b
            Vector3 v3x2 = Vector3.Normalize(Vector3.Cross(vector3, vector2));
            Vector3 v1x2 = Vector3.Normalize(Vector3.Cross(vector1, vector2));
            Vector3 v3norm2 = Vector3.Normalize(Vector3.Cross(v3x2, vector2)); // v1 portion that's normal to v2
            result = Math.Atan2(-Vector3.Dot(v1x2, v3norm2), Vector3.Dot(v1x2, v3x2));
            Debug.Assert(-Math.PI <= result && result <= Math.PI);
            return (float) result;
        }

        // Find the dihedral angle in [0, 2*Pi]
        public static float GetDihedralAngleRadians2Pi(Vector3 vector1, Vector3 vector2, Vector3 vector3)
        {
            double result = GetDihedralAngleRadians(vector1, vector2, vector3);
            if (result < 0)
                result += 2 * Math.PI;

            Debug.Assert(0 <= result && result <= 2 * Math.PI);
            return (float)result;
        }

        // Find the cartesian system after 
        public static void GetRemappedVectorBasis(ref Vector3 basisOrigin, ref Vector3 basisUnitX, ref Vector3 basisUnitY, ref Vector3 origin, ref Vector3 unitX, ref Vector3 unitY, out Vector3 remappedOrigin, out Vector3 remappedUnitX, out Vector3 remappedUnitY)
        {
            Vector3 basisUnitZ = Vector3.Cross(basisUnitX, basisUnitY);
            Vector3 o12 = origin - basisOrigin;
            remappedOrigin.X = Vector3.Dot(basisUnitX, o12);
            remappedOrigin.Y = Vector3.Dot(basisUnitY, o12);
            remappedOrigin.Z = Vector3.Dot(basisUnitZ, o12);

            remappedUnitX.X = Vector3.Dot(basisUnitX, unitX);
            remappedUnitX.Y = Vector3.Dot(basisUnitY, unitX);
            remappedUnitX.Z = Vector3.Dot(basisUnitZ, unitX);

            remappedUnitY.X = Vector3.Dot(basisUnitX, unitY);
            remappedUnitY.Y = Vector3.Dot(basisUnitY, unitY);
            remappedUnitY.Z = Vector3.Dot(basisUnitZ, unitY);
        }

        //public static Vector3 GetRemappedVectorBasis(Vector3 basisUnitX, Vector3 basisUnitY, Vector3 vector)
        public static Vector3 GetRemappedVectorBasis(ref Vector3 basisUnitX, ref Vector3 basisUnitY, Vector3 vector)
        {
            Vector3 basisUnitZ = Vector3.Cross(basisUnitX, basisUnitY);
            Vector3 result = new Vector3(Vector3.Dot(basisUnitX, vector), Vector3.Dot(basisUnitY, vector), Vector3.Dot(basisUnitZ, vector));
            return result;
        }

        // Returns the angle between planes a-c1-c2 and c1-c2-b
        public static double GetDihedralAngleDegrees(Vector3 a, Vector3 center1, Vector3 center2, Vector3 b)
        {
            double ret = GetDihedralAngleRadians(a, center1, center2, b);
            ret = ret * 180 / Math.PI;
            return ret;
        }

        public static double GetAngleDegrees(Vector3 a, Vector3 b)
        {
            double angle = 180 / Math.PI * GetAngleRadians(a, b);
            return angle;
        }

        // Find the angle between vectors b->a and b->c
        public static double GetAngleRadians(Vector3 a, Vector3 b)
        {
            // Calculate angle
            Vector3 dir1 = Vector3.Normalize(a);
            Vector3 dir2 = Vector3.Normalize(b);
            float dotProduct = Vector3.Dot(dir1, dir2);
            if (dotProduct > 1)
                dotProduct = 1; // This can actually happen due to floating point rounding error
            double radians = Math.Acos((double)dotProduct);
            return radians;
        }

        // Find the angle between vectors b->a and b->c
        public static double GetAngleRadians(Vector3 a, Vector3 b, Vector3 c)
        {
            // Calculate angle
            Vector3 dir1 = a - b; dir1.Normalize();
            Vector3 dir2 = c - b;
            return GetAngleRadians(dir1, dir2);
        }

        // Find the angle between vectors b->a and b->c
        public static double GetAngleDegrees(Vector3 a, Vector3 b, Vector3 c)
        {
            // Calculate angle
            double radians = GetAngleRadians(a, b, c);
            double degrees = radians * 180 / Math.PI;
            return degrees;
        }

        public static float GetAngleDegrees(Quaternion one, Quaternion two)
        {
            float dot = Math.Abs(DotBounded(one, two));
            float angleDegrees = (float)(Math.Acos(dot) * 180 / Math.PI);
            Debug.Assert(0 <= angleDegrees);
            return angleDegrees;
        }

        // Return a matrix that will transform a unit vector in the direction of fromVector to a unit vector in the direction of toVector
        public static Matrix GetRotationMatrix(Vector3 fromVector, Vector3 toVector)
        {
            //Debug.Assert(fromVector.LengthSquared() > 0);
            //Debug.Assert(toVector.LengthSquared() > 0);
            Vector3 normal = Vector3.Cross(fromVector, toVector); normal.Normalize();
            double angle = GetAngleRadians(fromVector, Vector3.Zero, toVector);
            return Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(normal /* this expects a unit vector */, (float) angle));
        }

        // Multiples a matrix N times
        public static Matrix Pow(Matrix matrix, int power)
        {
            Matrix result = Matrix.Identity;
            for(int i = 0; i < power; i++)
            {
                result = result * matrix;
            }
            return result;
        }

        public static Vector3 GetNearestPoint(Vector3 origin, Vector3 one, Vector3 two)
        {
            if (Vector3.DistanceSquared(origin, one) < Vector3.DistanceSquared(origin, two))
            {
                return one;
            }
            return two;
        }

        // Return a matrix that will transform a unit vector in the direction of fromVector to a unit vector in the direction of toVector
        public static Quaternion GetRotationQuaternion(Vector3 fromVector, Vector3 toVector)
        {
            /*
            //Debug.Assert(fromVector.LengthSquared() > 0);
            //Debug.Assert(toVector.LengthSquared() > 0);
            Vector3 normal = Vector3.Normalize(Vector3.Cross(fromVector, toVector));
            double angle = GetAngleRadians(fromVector, Vector3.Zero, toVector);
            //Quaternion rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(normal /* this expects a unit vector /, (float)angle));
            Quaternion rotation = Quaternion.CreateFromAxisAngle(normal / this expects a unit vector /, (float)angle);

            Debug.Assert(Vector3.Dot(Vector3.Normalize(Vector3.Transform(fromVector, rotation)), Vector3.Normalize(toVector)) > 0.99);
            return rotation;
            */

            Vector3 normal = Vector3.Normalize(Vector3.Cross(fromVector, toVector));

            // Handle special case where start/end vectors are identical or opposite / 180 degrees rotation
            if (VectorMath.IsNaN(normal))
            {
                if (Vector3.Dot(fromVector, toVector) > 0)
                    return Quaternion.Identity;
                else
                    return new Quaternion(1, 0, 0, 0);
            }

            float angle = (float) -GetDihedralAngleRadians(fromVector, normal, toVector);
            //Quaternion.CreateFromAxisAngle()
            //double angle = GetAngleRadians(fromVector, Vector3.Zero, toVector);
            //Quaternion rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(normal /* this expects a unit vector */, (float)angle));
            Quaternion rotation = Quaternion.CreateFromAxisAngle(normal /* this expects a unit vector */, (float)angle);
            rotation.Normalize();

            Debug.Assert(Vector3.Dot(Vector3.Normalize(Vector3.Transform(fromVector, rotation)), Vector3.Normalize(toVector)) > 0.99);
            return rotation;
        }

        /// <summary>
        /// Computes the transform of the first set of coordinates that minimizes the RMSD
        /// between the two.
        /// 
        /// The math was taken from OpenStax CNX RMSD Molecular Distance Measures
        /// http://cnx.org/contents/1d5f91b1-dc0b-44ff-8b4d-8809313588f2@23/Molecular-Distance-Measures
        /// ***NOTE***: The above document calls for using a SVD W matrix while the MathNet library
        /// calls this U. So the code references Svd.U instead of Svd.W. This is either a bug or just
        /// differs from standard SVD naming conventions.
        /// </summary>
        /// <param name="coordinates1">First set of coordinates</param>
        /// <param name="coordinates2">Second set of coordinates</param>
        /// <param name="isOrigin1">Whether the first set of coordinates is centroid-centered at the origin</param>
        /// <param name="isOrigin2">Whether the second set of coordinates is centroid-centered at the origin</param>
        /// <returns></returns>
        public static Matrix GetRmsdAlignmentMatrix(Vector3[] move, bool isOrigin1, Vector3[] stay, bool isOrigin2)
        {
            if (move.Length != stay.Length)
                throw new ArgumentException("Array lengths must be equal.");

            if (move.Length < 3)
                throw new ArgumentException("At least 3 points are required for RMSD minimization");

            Vector3[] coordinates1 = null; //Vector3[])moveCoord1.Clone();
            Vector3[] coordinates2 = null; //(Vector3[])fixedCoord2.Clone();
            int length = move.Length;

            if(length == 3)
            {
                coordinates1 = new Vector3[4] { move[0], move[1], move[2], Vector3.Zero };
                coordinates2 = new Vector3[4] { stay[0], stay[1], stay[2], Vector3.Zero };
                coordinates1[3] = Vector3.Normalize(Vector3.Cross(move[2] - move[1], move[0] - move[1])) + move[1];
                coordinates2[3] = Vector3.Normalize(Vector3.Cross(stay[2] - stay[1], stay[0] - stay[1])) + stay[1];
                length = 4;
            }
            else
            {
                coordinates1 = (Vector3[]) move.Clone();
                coordinates2 = (Vector3[]) stay.Clone();
            }

            Vector3 centroid1 = Vector3.Zero;
            Vector3 centroid2 = Vector3.Zero;
            if(!isOrigin1)
            {
                // Via indexers because it's faster than aggregation
                for (int i = 0; i < length; i++)
                {
                    centroid1 += coordinates1[i];
                }
                centroid1 /= length;
                
                for(int i = 0; i < length; i++)
                {
                    coordinates1[i] = coordinates1[i] - centroid1;
                }
            }

            if (!isOrigin2)
            {
                // Via indexers because it's faster than aggregation
                for (int i = 0; i < length; i++)
                {
                    centroid2 += coordinates2[i];
                }
                centroid2 /= length;

                for (int i = 0; i < length; i++)
                {
                    coordinates2[i] = coordinates2[i] - centroid2;
                }
            }

            double[] storage1 = new double[length * 3];
            double[] storage2 = new double[length * 3];
            for(int i = 0; i < coordinates1.Length; i++)
            {
                storage1[i * 3 + 0] = coordinates1[i].X; // -centroid1.X;
                storage1[i * 3 + 1] = coordinates1[i].Y; //- centroid1.Y;
                storage1[i * 3 + 2] = coordinates1[i].Z; //- centroid1.Z;
                storage2[i * 3 + 0] = coordinates2[i].X; // -centroid1.X;
                storage2[i * 3 + 1] = coordinates2[i].Y; //- centroid1.Y;
                storage2[i * 3 + 2] = coordinates2[i].Z; //- centroid1.Z;
            }

            MathNetMatrix matrix1 = new DenseMatrix(3, coordinates1.Length, storage1);
            MathNetMatrix matrix2 = new DenseMatrix(3, coordinates1.Length, storage2);
            MathNetMatrix covariance = matrix1.TransposeAndMultiply(matrix2);

            Svd<double> svd = covariance.Svd(true);
            double d = svd.Determinant >= 0? 1 : -1;
            MathNetMatrix W = svd.U; /* U and W seem to be switched in comparison with the docs/rmsd.pdf which was the math reference */
            MathNetMatrix VT = svd.VT;
            MathNetMatrix trace = new DenseMatrix(3, 3, new double[] {/*col1:*/1,0,0, /*col2:*/0,1,0, /*col3:*/0,0,d});

            MathNetMatrix transform = W.Multiply(trace).Multiply(VT);
            Matrix conversion = new Matrix(
                (float)transform[0, 0], (float)transform[0, 1], (float)transform[0, 2], 0,
                (float)transform[1, 0], (float)transform[1, 1], (float)transform[1, 2], 0,
                (float)transform[2, 0], (float)transform[2, 1], (float)transform[2, 2], 0,
                0, 0, 0, 1);

            // Figure out the final translation component if the sets aren't centered
            if(!isOrigin1 || !isOrigin2)
            {
                centroid1 = Vector3.Transform(centroid1, conversion);
                conversion.Translation = (centroid2 - centroid1);
            }

            //Console.WriteLine("X\n" + matrix1.ToString() + "\n");
            //Console.WriteLine("Y\n" + matrix2.ToString() + "\n");
            //Console.WriteLine("covariance\n" + covariance.ToString() + "\n");
            //Console.WriteLine("W\n" + W.ToString() + "\n");
            //Console.WriteLine("VT\n" + VT.ToString() + "\n");
            //Console.WriteLine("trace\n" + trace.ToString() + "\n");
            //Console.WriteLine("transform\n" + transform.ToString() + "\n");

            return conversion;
        }

        public static bool IsInfinity(Vector3 xyz)
        {
            return (float.IsInfinity(xyz.X) || float.IsInfinity(xyz.Y) || float.IsInfinity(xyz.Z));
        }

        public static Matrix GetRmsdAlignmentMatrix(Vector3[] move, Vector3 origin1, Vector3[] stay, Vector3 origin2)
        {
            Vector3[] coordinates1 = (Vector3[])move.Clone();
            Vector3[] coordinates2 = (Vector3[])stay.Clone();            

            for(int i = 0; i < coordinates1.Length; i++)
            {
                coordinates1[i] -= origin1;
            }

            for(int j = 0; j < coordinates2.Length; j++)
            {
                coordinates2[j] -= origin2;
            }

            Matrix matrix = GetRmsdAlignmentMatrix(coordinates1, true, coordinates2, true);
            Vector3 transformedOrigin = Vector3.Transform(origin1, matrix);
            Vector3 requiredTranslation = origin2 - transformedOrigin;
            matrix.Translation = requiredTranslation;
            return matrix;
        }

        // http://pages.pacificcoast.net/~cazelais/251/distance.pdf
        public static float GetDistanceBetweenLines(Vector3 coord1, Vector3 direction1, Vector3 coord2, Vector3 direction2)
        {
            Vector3 P1P2 = coord2 - coord1;
            Vector3 n = Vector3.Cross(direction1, direction2);
            float numerator = Math.Abs(Vector3.Dot(P1P2, n));
            float denominator = n.Length();
            float result = numerator / denominator;
            return result;
        }
    }
}
