using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Tools;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Core.Quick;

namespace Core.Symmetry
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class CxSymmetryBuilder : SymmetryBuilder
    {
        public CxSymmetryBuilder() { }

        [JsonProperty] int _multiplicity = -1;
        [JsonProperty] string[] _unitNames = new string[1];

        public CxSymmetryBuilder(int multiplicity)
        {
            if (multiplicity <= 1)
                throw new ArgumentException("D symmetry requires a multiplicity must be greater than 1");

            _multiplicity = multiplicity;
            string axis = _unitNames[0] = "C" + multiplicity.ToString();

            Setup(axis, multiplicity);

            for(int i = 0; i < multiplicity; i++)
            {
                CoordinateSystem coordinateSystem = new CoordinateSystem();
                coordinateSystem.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(0, 0, (float)(i * 2 * Math.PI / multiplicity)));   // Rotate about global (not this coordinate system) Z
                base.AddCoordinateSystem(axis, "C" + multiplicity, coordinateSystem);
            }
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            IDeepCloneObjectGraph context = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(context);
        }

        public override void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            base.DeepCopyPopulateFields(graph, clone);

            CxSymmetryBuilder builder = (CxSymmetryBuilder)clone;
            builder._multiplicity = _multiplicity;
            builder._unitNames = _unitNames;
        }
    }

    public class C2SymmetryBuilder : CxSymmetryBuilder
    {
        public C2SymmetryBuilder() : base(2) { }
        public static string Architecture { get { return "C2"; } }
        public static string[] Units { get { return new string[] { "C2" }; } }
        public static int[] Multiplicities { get { return new int[] { 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            CxSymmetryBuilder builder = new CxSymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    public class C3SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C3SymmetryBuilder() : base(3) { }
        public static string Architecture { get { return "C3"; } }
        public static string[] Units { get { return new string[] { "C3" }; } }
        public static int[] Multiplicities { get { return new int[] { 3 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            CxSymmetryBuilder builder = new CxSymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C4SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C4SymmetryBuilder() : base(4) { }
        public static string Architecture { get { return "C4"; } }
        public static string[] Units { get { return new string[] { "C4" }; } }
        public static int[] Multiplicities { get { return new int[] { 4 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            CxSymmetryBuilder builder = new CxSymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C5SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C5SymmetryBuilder() : base(5) { }
        public static string Architecture { get { return "C5"; } }
        public static string[] Units { get { return new string[] { "C5" }; } }
        public static int[] Multiplicities { get { return new int[] { 5 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            CxSymmetryBuilder builder = new CxSymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C6SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C6SymmetryBuilder() : base(6) { }
        public static string Architecture { get { return "C6"; } }
        public static string[] Units { get { return new string[] { "C6" }; } }
        public static int[] Multiplicities { get { return new int[] { 6 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            C6SymmetryBuilder builder = new C6SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C7SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C7SymmetryBuilder() : base(7) { }
        public static string Architecture { get { return "C7"; } }
        public static string[] Units { get { return new string[] { "C7" }; } }
        public static int[] Multiplicities { get { return new int[] { 7 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            C7SymmetryBuilder builder = new C7SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C8SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C8SymmetryBuilder() : base(8) { }
        public static string Architecture { get { return "C8"; } }
        public static string[] Units { get { return new string[] { "C8" }; } }
        public static int[] Multiplicities { get { return new int[] { 8 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            C8SymmetryBuilder builder = new C8SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C9SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C9SymmetryBuilder() : base(9) { }
        public static string Architecture { get { return "C9"; } }
        public static string[] Units { get { return new string[] { "C9" }; } }
        public static int[] Multiplicities { get { return new int[] { 9 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            C9SymmetryBuilder builder = new C9SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C10SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C10SymmetryBuilder() : base(10) { }
        public static string Architecture { get { return "C10"; } }
        public static string[] Units { get { return new string[] { "C10" }; } }
        public static int[] Multiplicities { get { return new int[] { 10 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            C10SymmetryBuilder builder = new C10SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C11SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C11SymmetryBuilder() : base(11) { }
        public static string Architecture { get { return "C11"; } }
        public static string[] Units { get { return new string[] { "C11" }; } }
        public static int[] Multiplicities { get { return new int[] { 11 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            C11SymmetryBuilder builder = new C11SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
    public class C12SymmetryBuilder : CxSymmetryBuilder
    {
        [JsonConstructor]
        public C12SymmetryBuilder() : base(12) { }
        public static string Architecture { get { return "C12"; } }
        public static string[] Units { get { return new string[] { "C12" }; } }
        public static int[] Multiplicities { get { return new int[] { 12 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            C12SymmetryBuilder builder = new C12SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

}
