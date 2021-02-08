using Core.Interfaces;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace Core.Symmetry
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class DxSymmetryBuilder : SymmetryBuilder
    {
        [JsonProperty] int _multiplicity = -1;
        [JsonProperty] string[] _unitNames = new string[3];

        public DxSymmetryBuilder(int multiplicity)
        {
            if (multiplicity <= 1)
                throw new ArgumentException("D symmetry requires a multiplicity must be greater than 1");

            string axisC = _unitNames[0] = "C" + multiplicity.ToString();
            string axisC2X = _unitNames[1] = "C2X";
            string axisC2Y = _unitNames[2] = "C2Y";

            Setup(axisC, multiplicity * 2);
            Setup(axisC2X, multiplicity * 2);
            Setup(axisC2Y, multiplicity * 2);

            foreach (bool top in new bool[] { true, false })
            {
                for (int i = 0; i < multiplicity; i++)
                {
                    // The X-axis of each coordinate system is considered the rotation-symmetry axis, so it must be alligned to the Z/C, X/C2X, or Y/C2Y

                    // C axis - Cn symmetry about the Z axis. X (the principal axis direction) now points towards Z.
                    CoordinateSystem coordinateSystemC = new CoordinateSystem();
                    //coordinateSystemC.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll((top? -1 : 1) * (float) Math.PI / 2, 0, 0));        // Move X to Z (top) or -Z (bottom) in a way that keeps it C2 about global X
                    
                    coordinateSystemC.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll((float) -Math.PI / 2, 0, 0));            // Move coordX to globalZ (top or bottom)
                    coordinateSystemC.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(0, top ? 0 : (float) Math.PI, 0));        // Move coordX to -globalZ (bottom)
                    coordinateSystemC.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(0, 0, (top? 1 : -1) * (float) (i * 2 * Math.PI / multiplicity)));   // Rotate about global (not this coordinate system) Z
                    base.AddCoordinateSystem(axisC, "C." + (top ? "UP." : "DOWN.") + multiplicity.ToString(), coordinateSystemC);

                    // C2X - C2 symmetry about the X axis and every 2PI/n of X rotated about the Z. X (the principal axis direction) now lies in the XY plane.
                    CoordinateSystem coordinateSystemC2X = new CoordinateSystem();
                    coordinateSystemC2X.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(0, (top? 0 : 1) * (float) Math.PI, 0));          // Rotate about X, not at all (top), or 180 (bottom)
                    coordinateSystemC2X.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(0, 0, (float)(i * 2 * Math.PI / multiplicity)));  // Rotate about global (not this coordinate system) Z;
                    base.AddCoordinateSystem(axisC2X, "C2X." + (top ? "UP" : "DOWN"), coordinateSystemC2X);

                    // C2Y - C2 symmetry about the Y axis and every 2PI/n of Y rotated about the Z
                    CoordinateSystem coordinateSystemC2Y = new CoordinateSystem();
                    coordinateSystemC2Y.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(0, (top ? 0 : 1) * (float) Math.PI, 0));          // Rotate about X, not at all (top), or 180 (bottom)
                    coordinateSystemC2Y.Transform *= Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(0, 0, (float)((i + 0.5f) * 2 * Math.PI / multiplicity)));   // Rotate about global (not this coordinate system) Z;
                    base.AddCoordinateSystem(axisC2Y, "C2Y." + (top ? "UP" : "DOWN"), coordinateSystemC2Y);

                }
            }
        }

        public override void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            base.DeepCopyPopulateFields(graph, clone);

            DxSymmetryBuilder builder = (DxSymmetryBuilder)clone;
            builder._multiplicity = _multiplicity;
            builder._unitNames = _unitNames;
        }
    }

    [Serializable]
    public class D2SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D2SymmetryBuilder() : base(2) { }
        public static string Architecture { get { return "D2"; } }
        public static string[] Units { get { return new string[] { "C2", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 2, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D2SymmetryBuilder builder = new D2SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D3SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D3SymmetryBuilder() : base(3) { }
        public static string Architecture { get { return "D3"; } }
        public static string[] Units { get { return new string[] { "C3", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 3, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D3SymmetryBuilder builder = new D3SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D4SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D4SymmetryBuilder() : base(4) { }
        public static string Architecture { get { return "D4"; } }
        public static string[] Units { get { return new string[] { "C4", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 4, 2, 2 }; } }


        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D4SymmetryBuilder builder = new D4SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D5SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D5SymmetryBuilder() : base(5) { }
        public static string Architecture { get { return "D5"; } }
        public static string[] Units { get { return new string[] { "C5", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 5, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D5SymmetryBuilder builder = new D5SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D6SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D6SymmetryBuilder() : base(6) { }
        public static string Architecture { get { return "D6"; } }
        public static string[] Units { get { return new string[] { "C6", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 6, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D6SymmetryBuilder builder = new D6SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D7SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D7SymmetryBuilder() : base(7) { }
        public static string Architecture { get { return "D7"; } }
        public static string[] Units { get { return new string[] { "C7", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 7, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D7SymmetryBuilder builder = new D7SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D8SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D8SymmetryBuilder() : base(8) { }
        public static string Architecture { get { return "D8"; } }
        public static string[] Units { get { return new string[] { "C8", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 8, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D8SymmetryBuilder builder = new D8SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D9SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D9SymmetryBuilder() : base(9) { }
        public static string Architecture { get { return "D9"; } }
        public static string[] Units { get { return new string[] { "C9", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 9, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D9SymmetryBuilder builder = new D9SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D10SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D10SymmetryBuilder() : base(10) { }
        public static string Architecture { get { return "D10"; } }
        public static string[] Units { get { return new string[] { "C10", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 10, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D10SymmetryBuilder builder = new D10SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D11SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D11SymmetryBuilder() : base(11) { }
        public static string Architecture { get { return "D11"; } }
        public static string[] Units { get { return new string[] { "C11", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 11, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D11SymmetryBuilder builder = new D11SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    [Serializable]
    public class D12SymmetryBuilder : DxSymmetryBuilder
    {
        [JsonConstructor]
        public D12SymmetryBuilder() : base(12) { }
        public static string Architecture { get { return "D12"; } }
        public static string[] Units { get { return new string[] { "C12", "C2X", "C2Y" }; } }
        public static int[] Multiplicities { get { return new int[] { 12, 2, 2 }; } }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            D12SymmetryBuilder builder = new D12SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
}
