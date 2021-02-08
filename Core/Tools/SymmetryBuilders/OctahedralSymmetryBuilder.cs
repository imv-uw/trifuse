using Core.Interfaces;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Core.Symmetry
{
    [Serializable]
    public class OctahedralSymmetryBuilder : SymmetryBuilder
    {
        public static string Architecture {  get { return "O"; } }
        public static string[] Units { get { return new string[] { "C4", "C3", "C2" }; } }

        public static int[] Multiplicities { get { return new int[] { 4, 3, 2 }; } }

        [JsonConstructor]
        public OctahedralSymmetryBuilder()
        {
            Vector3[][] tetramers = new Vector3[][]
            {
                new Vector3[] { new Vector3(+1,+0,+0), new Vector3(+0,+1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+0,+0), new Vector3(+0,+0,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+0,+0), new Vector3(+0,-1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+0,+0), new Vector3(+0,+0,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+0,+0), new Vector3(+0,+1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+0,+0), new Vector3(+0,+0,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+0,+0), new Vector3(+0,-1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+0,+0), new Vector3(+0,+0,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+1,+0), new Vector3(+1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+1,+0), new Vector3(+0,+0,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+1,+0), new Vector3(-1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+1,+0), new Vector3(+0,+0,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,-1,+0), new Vector3(+1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,-1,+0), new Vector3(+0,+0,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,-1,+0), new Vector3(-1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,-1,+0), new Vector3(+0,+0,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+0,+1), new Vector3(+1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+0,+1), new Vector3(+0,+1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+0,+1), new Vector3(-1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+0,+1), new Vector3(+0,-1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+0,-1), new Vector3(+1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+0,-1), new Vector3(+0,+1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+0,-1), new Vector3(-1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+0,-1), new Vector3(+0,-1,+0), new Vector3(0,0,0) },
            };

            Vector3[][] trimers = new Vector3[][]
            {
                new Vector3[] { new Vector3(+1,+1,+1), new Vector3(+1,+1,-2), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+1,+1), new Vector3(+1,-2,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+1,+1), new Vector3(-2,+1,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+1,-1), new Vector3(+1,+1,+2), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+1,-1), new Vector3(+1,-2,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+1,-1), new Vector3(-2,+1,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,-1,+1), new Vector3(+1,+2,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,-1,+1), new Vector3(+1,-1,-2), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,-1,+1), new Vector3(-2,-1,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+1,+1), new Vector3(+2,+1,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+1,+1), new Vector3(-1,+1,-2), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+1,+1), new Vector3(-1,-2,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,-1,+1), new Vector3(-1,-1,-2), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,-1,+1), new Vector3(-1,+2,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,-1,+1), new Vector3(+2,-1,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+1,-1), new Vector3(-1,-2,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+1,-1), new Vector3(-1,+1,+2), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+1,-1), new Vector3(+2,+1,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,-1,-1), new Vector3(-2,-1,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,-1,-1), new Vector3(+1,-1,+2), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,-1,-1), new Vector3(+1,+2,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,-1,-1), new Vector3(-1,-1,+2), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,-1,-1), new Vector3(-1,+2,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,-1,-1), new Vector3(+2,-1,-1), new Vector3(0,0,0) },
            };

            Vector3[][] dimers = new Vector3[][]
            {
                new Vector3[] { new Vector3(+1,+1,+0), new Vector3(+0,+0,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+1,+0), new Vector3(+0,-0,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,-1,+0), new Vector3(+0,+0,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,-1,+0), new Vector3(+0,-0,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,-1,+0), new Vector3(+0,+0,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,-1,+0), new Vector3(+0,-0,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+1,+0), new Vector3(+0,+0,+1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+1,+0), new Vector3(+0,-0,-1), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+0,+1), new Vector3(+0,+1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+0,+1), new Vector3(+0,-1,-0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+0,-1), new Vector3(+0,+1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+1,+0,-1), new Vector3(+0,-1,-0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+0,-1), new Vector3(+0,+1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+0,-1), new Vector3(+0,-1,-0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+0,+1), new Vector3(+0,+1,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(-1,+0,+1), new Vector3(+0,-1,-0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+1,+1), new Vector3(+1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+1,+1), new Vector3(-1,+0,-0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+1,-1), new Vector3(+1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,+1,-1), new Vector3(-1,+0,-0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,-1,-1), new Vector3(+1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,-1,-1), new Vector3(-1,+0,-0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,-1,+1), new Vector3(+1,+0,+0), new Vector3(0,0,0) },
                new Vector3[] { new Vector3(+0,-1,+1), new Vector3(-1,+0,-0), new Vector3(0,0,0) },
            };

            string[] tetramerSubunitNames = new string[]
            {
                    "3.1.1",  "3.1.2",  "3.1.3",  "3.1.4",
                    "3.2.1",  "3.2.2",  "3.2.3",  "3.2.4",
                    "3.3.1",  "3.3.2",  "3.3.3",  "3.3.4",
                    "3.4.1",  "3.4.2",  "3.4.3",  "3.4.4",
                    "3.5.1",  "3.5.2",  "3.5.3",  "3.5.4",
                    "3.6.1",  "3.6.2",  "3.6.3",  "3.6.4",
            };

            string[] trimerSubunitNames = new string[]
            {
                    "3.1.1",  "3.1.2",  "3.1.3",
                    "3.2.1",  "3.2.2",  "3.2.3",
                    "3.3.1",  "3.3.2",  "3.3.3",
                    "3.4.1",  "3.4.2",  "3.4.3",
                    "3.5.1",  "3.5.2",  "3.5.3",
                    "3.6.1",  "3.6.2",  "3.6.3",
                    "3.7.1",  "3.7.2",  "3.7.3",
                    "3.8.1",  "3.8.2",  "3.8.3",
            };

            string[] dimerSubunitNames = new string[]
            {
                    "2.1.1",  "2.1.2",
                    "2.2.1",  "2.2.2",
                    "2.3.1",  "2.3.2",
                    "2.4.1",  "2.4.2",
                    "2.5.1",  "2.5.2",
                    "2.6.1",  "2.6.2",
                    "2.7.1",  "2.7.2",
                    "2.8.1",  "2.8.2",
                    "2.9.1",  "2.9.2",
                    "2.10.1", "2.10.2",
                    "2.11.1", "2.11.2",
                    "2.12.1", "2.12.2",
            };
            
            base.Setup("C4", 24);
            int subunitIndex = 0;
            tetramers.ToList().ForEach(xy => base.AddSymdefStyleCoordinateSystem("C4", tetramerSubunitNames[subunitIndex++], xy[0], xy[1], xy[2]));

            base.Setup("C3", 24);
            subunitIndex = 0;
            trimers.ToList().ForEach(xy => base.AddSymdefStyleCoordinateSystem("C3", trimerSubunitNames[subunitIndex++], xy[0], xy[1], xy[2]));

            base.Setup("C2", 24);
            subunitIndex = 0;
            dimers.ToList().ForEach(xy => base.AddSymdefStyleCoordinateSystem("C2", dimerSubunitNames[subunitIndex++], xy[0], xy[1], xy[2]));
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            OctahedralSymmetryBuilder builder = new OctahedralSymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }

    }
}
