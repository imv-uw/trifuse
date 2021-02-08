using Core.Symmetry;
using Microsoft.Xna.Framework;
using NamespaceUtilities;
using System.Linq;
using Tools;

namespace Fuse
{
    public class AxisRealignmentFactory
    {
        public static TwoAxisRealignment Create(SymmetryBuilder builder, string axis1, string axis2)
        {
            CoordinateSystem coordinateSystem1 = builder.GetPrincipalCoordinateSystem(axis1);
            CoordinateSystem coordinateSystem2 = builder.GetPrincipalCoordinateSystem(axis2);
            Line principalAxis1 = Line.CreateFromPointAndDirection(coordinateSystem1.Translation, coordinateSystem1.UnitX); // The rosetta symdefs axes are along X of each transformed coordinate system, but something not requiring foreknowledge would be nice.
            Line principalAxis2 = Line.CreateFromPointAndDirection(coordinateSystem2.Translation, coordinateSystem2.UnitX); // The rosetta symdefs axes are along X of each transformed coordinate system, but something not requiring foreknowledge would be nice.
            Quaternion premultiply = Quaternion.Inverse(coordinateSystem1.Transform.Rotation);
            principalAxis1.Direction = Vector3.Transform(principalAxis1.Direction, premultiply);
            principalAxis2.Direction = Vector3.Transform(principalAxis2.Direction, premultiply);
            principalAxis1.Point = Vector3.Transform(principalAxis1.Point, premultiply);
            principalAxis2.Point = Vector3.Transform(principalAxis2.Point, premultiply);
            float angleDegrees = (float)VectorMath.GetAngleDegrees(principalAxis1.Direction, principalAxis2.Direction);
            TwoAxisRealignment result = TwoAxisRealignment.Create(angleDegrees, principalAxis1, principalAxis2);
            return result;
        }

        public static TwoAxisRealignment Create(string symmetry, int multiplicity1, int multiplicity2)
        {
            SymmetryBuilder builder = SymmetryBuilderFactory.CreateFromSymmetryName(symmetry);
            string axis1 = builder.GetUnits().First(axis => builder.GetMultiplicity(axis) == multiplicity1);
            string axis2 = builder.GetUnits().First(axis => builder.GetMultiplicity(axis) == multiplicity2 && axis != axis1);
            TwoAxisRealignment result = Create(symmetry, axis1, axis2);
            return result;
        }

        public static TwoAxisRealignment Create(SymmetryBuilder builder, int multiplicity1, int multiplicity2)
        { 
            string axis1 = builder.GetUnits().First(axis => builder.GetMultiplicity(axis) == multiplicity1);
            string axis2 = builder.GetUnits().First(axis => builder.GetMultiplicity(axis) == multiplicity2 && axis != axis1);
            TwoAxisRealignment result = Create(builder, axis1, axis2);
            return result;
        }

        public static TwoAxisRealignment Create(string symmetry, string axisId1, string axisId2)
        {
            SymmetryBuilder builder = SymmetryBuilderFactory.CreateFromSymmetryName(symmetry);
            TwoAxisRealignment result = Create(builder, axisId1, axisId2);
            return result;
        }

        public static bool TryCreate(string symmetry, int multiplicity1, int multiplicity2, out TwoAxisRealignment realignment)
        { 
            try
            {
                realignment = Create(symmetry, multiplicity1, multiplicity2);
                return true;
            }
            catch
            {
                realignment = null;
            }
            return false;
        }
    }
}
