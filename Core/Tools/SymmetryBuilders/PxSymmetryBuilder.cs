using Core.Interfaces;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Core.Symmetry
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class PxSymmetryBuilder : SymmetryBuilder
    {
        [JsonProperty] string[] _units = new string[0];

        [JsonProperty] float _scale = 1f;
        public float Scale
        {
            get
            {
                return _scale;
            }

            set
            {
                Trace.Assert(0 < _scale && !float.IsInfinity(_scale), "Scale cannot be negative or infinite");
                float rescaleFactor = value / _scale;
                foreach(string unitId in GetUnits())
                {
                    foreach (CoordinateSystem system in GetTemplateCoordinateSystems(unitId))
                    {
                        system.Translation *= rescaleFactor;
                    }
                }
                _scale = value;
            }
        }
        
        protected void InitializeCyclicAxis(string unitId, string copyId, int multiplicity, float cellSpacingX, float cellSpacingY, float rotationOffset = 0)
        {
            for(int i = 0; i < multiplicity; i++)
            {
                CoordinateSystem coordinateSystem = new CoordinateSystem();
                coordinateSystem.Transform *= Matrix.CreateFromYawPitchRoll((float) -Math.PI / 2, 0, 0);
                coordinateSystem.Transform *= Matrix.CreateFromYawPitchRoll(0, 0, (float)(2 * Math.PI / multiplicity * i + rotationOffset)); // y, x, z = yaw pitch roll
                coordinateSystem.Translation += new Vector3(cellSpacingX, cellSpacingY, 0);
                base.AddCoordinateSystem(unitId, copyId + "." + i.ToString(), coordinateSystem);
            }
        }

        public override CoordinateSystem[] GetCoordinateSystems(string unitId)
        {
            return base.GetCoordinateSystems(unitId);
        }

        public override CoordinateSystem GetPrincipalCoordinateSystem(string unitId)
        {
            return base.GetPrincipalCoordinateSystem(unitId);
        }
    }

    public class P4SymmetryBuilder : PxSymmetryBuilder
    {
        public static string Architecture { get { return "P4"; } }
        public static string[] Units = new string[] { "C4", "C2" };
        public static int[] Multiplicities { get { return new int[] { 4, 2 }; } }

        [JsonConstructor]
        public P4SymmetryBuilder()
        {
            Setup("C4", 16);
            Setup("C2", 16);

            InitializeCyclicAxis("C4", "C4-X0-Y0", 4, 0.0f, 0.0f);
            InitializeCyclicAxis("C4", "C4-X1-Y0", 4, 1.0f, 0.0f);
            InitializeCyclicAxis("C4", "C4-X0-Y1", 4, 0.0f, 1.0f);
            InitializeCyclicAxis("C4", "C4-X1-Y1", 4, 1.0f, 1.0f);

            InitializeCyclicAxis("C2", "C2-X0-Y0.5", 2, 0.0f, 0.5f);
            InitializeCyclicAxis("C2", "C2-X0.5-Y0", 2, 0.5f, 0.0f, (float) (Math.PI / 2));
            InitializeCyclicAxis("C2", "C2-X1-Y0.5", 2, 1.0f, 0.5f);
            InitializeCyclicAxis("C2", "C2-X0.5-Y1", 2, 0.5f, 1.0f, (float)(Math.PI / 2));

            InitializeCyclicAxis("C2", "C2-X0-Y1.5", 2, 0f, 1.5f);
            InitializeCyclicAxis("C2", "C2-X1-Y1.5", 2, 1f, 1.5f);
            InitializeCyclicAxis("C2", "C2-X1.5-Y0", 2, 1.5f, 0f, (float) (Math.PI / 2));
            InitializeCyclicAxis("C2", "C2-X1.5-Y1", 2, 1.5f, 1f, (float)(Math.PI / 2));
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            P4SymmetryBuilder builder = new P4SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }

    public class P6SymmetryBuilder : PxSymmetryBuilder
    {
        public static string Architecture { get { return "P6"; } }
        public static string[] Units = new string[] { "C6", "C3", "C2" };
        public static int[] Multiplicities { get { return new int[] { 6, 3, 2 }; } }

        [JsonConstructor]
        public P6SymmetryBuilder()
        {
            Setup("C6", 6);
            Setup("C3", 18);
            Setup("C2", 24);

            // Centered on the C6 axis
            InitializeCyclicAxis("C6", "C6", 6, 0.0f, 0.0f);

            // C3 (hexagon vertices)
            for(int i = 0; i < 6; i++)
            {
                double rotation = i * 2 * Math.PI / 6;
                double len = 1;
                float positionX = (float) (len * Math.Cos(rotation));
                float positionY = (float) (len * Math.Sin(rotation));
                
                InitializeCyclicAxis("C3", "C3" + i, 3, positionX, positionY, (float) rotation);
            }

            // C2 (hexagon side midpoints)
            for (int i = 0; i < 6; i++)
            {
                double rotation = i * 2 * Math.PI / 6 + Math.PI / 6;
                double len = Math.Sqrt(3) / 2;
                float positionX = (float) (len * Math.Cos(rotation));
                float positionY = (float) (len * Math.Sin(rotation));

                InitializeCyclicAxis("C2", "C2" + i, 2, positionX, positionY, (float)rotation);
            }

            // C2 (extension past vertices)
            for (int i = 0; i < 6; i++)
            {
                double rotation = i * 2 * Math.PI / 6;
                double len = 1.5;
                float positionX = (float)(len * Math.Cos(rotation));
                float positionY = (float)(len * Math.Sin(rotation));

                InitializeCyclicAxis("C2", "C2" + i, 2, positionX, positionY, (float) (rotation + Math.PI / 2));
            }
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            P6SymmetryBuilder builder = new P6SymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }


}
