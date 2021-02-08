using Core.Interfaces;
using Core.Symmetry;

namespace Core
{
    public class IdentitySymmetryBuilder : SymmetryBuilder
    {
        public static string Architecture { get { return "Identity"; } }
        public static string[] Units { get { return new string[] { "1" }; } }
        public static int[] Multiplicities { get { return new int[] { 1 }; } }

        public IdentitySymmetryBuilder()
        {
            base.Setup("1", 1);
            base.AddCoordinateSystem("1", "1", new CoordinateSystem());

            EnabledUnits = Units;
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            IdentitySymmetryBuilder builder = new IdentitySymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
}