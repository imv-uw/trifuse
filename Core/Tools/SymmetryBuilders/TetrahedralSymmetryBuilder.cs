using Microsoft.Xna.Framework;
using System.Linq;
using System;
using Core.Interfaces;
using Newtonsoft.Json;

namespace Core.Symmetry
{
    [Serializable]
    public class TetrahedralSymmetryBuilder : SymmetryBuilder
    {
        public static string Architecture { get { return "T"; } }
        public static string[] Units { get { return new string[] { "C3X", "C3Y", "C2" }; } }
        public static int[] Multiplicities { get { return new int[] { 3, 3, 2 }; } }

        [JsonConstructor]
        public TetrahedralSymmetryBuilder()
        {
            Vector3[][] trimersX = new Vector3[][]
            {
                new Vector3[] { new Vector3( 1, 1, 1), new Vector3( 1, 1,-2), new Vector3() },
                new Vector3[] { new Vector3( 1, 1, 1), new Vector3( 1,-2, 1), new Vector3() },
                new Vector3[] { new Vector3( 1, 1, 1), new Vector3(-2, 1, 1), new Vector3() },

                new Vector3[] { new Vector3(-1,-1, 1), new Vector3(-1,-1,-2), new Vector3() },
                new Vector3[] { new Vector3(-1,-1, 1), new Vector3(-1, 2, 1), new Vector3() },
                new Vector3[] { new Vector3(-1,-1, 1), new Vector3( 2,-1, 1), new Vector3() },

                new Vector3[] { new Vector3(-1, 1,-1), new Vector3(-1,-2,-1), new Vector3() },
                new Vector3[] { new Vector3(-1, 1,-1), new Vector3(-1, 1, 2), new Vector3() },
                new Vector3[] { new Vector3(-1, 1,-1), new Vector3( 2, 1,-1), new Vector3() },

                new Vector3[] { new Vector3( 1,-1,-1), new Vector3(-2,-1,-1), new Vector3() },
                new Vector3[] { new Vector3( 1,-1,-1), new Vector3( 1,-1, 2), new Vector3() },
                new Vector3[] { new Vector3( 1,-1,-1), new Vector3( 1, 2,-1), new Vector3() }
            };

            Vector3[][] trimersY = new Vector3[][]
            {
                new Vector3[] { new Vector3( 1, 1,-1), new Vector3( 1, 1, 2), new Vector3() },
                new Vector3[] { new Vector3( 1, 1,-1), new Vector3( 1,-2,-1), new Vector3() },
                new Vector3[] { new Vector3( 1, 1,-1), new Vector3(-2, 1,-1), new Vector3() },

                new Vector3[] { new Vector3( 1,-1, 1), new Vector3( 1, 2, 1), new Vector3() },
                new Vector3[] { new Vector3( 1,-1, 1), new Vector3( 1,-1,-2), new Vector3() },
                new Vector3[] { new Vector3( 1,-1, 1), new Vector3(-2,-1, 1), new Vector3() },

                new Vector3[] { new Vector3(-1, 1, 1), new Vector3( 2, 1, 1), new Vector3() },
                new Vector3[] { new Vector3(-1, 1, 1), new Vector3(-1, 1,-2), new Vector3() },
                new Vector3[] { new Vector3(-1, 1, 1), new Vector3(-1,-2, 1), new Vector3() },

                new Vector3[] { new Vector3(-1,-1,-1), new Vector3(-1,-1, 2), new Vector3() },
                new Vector3[] { new Vector3(-1,-1,-1), new Vector3(-1, 2,-1), new Vector3() },
                new Vector3[] { new Vector3(-1,-1,-1), new Vector3( 2,-1,-1), new Vector3() }
            };  

            Vector3[][] dimers = new Vector3[][]
            {
                new Vector3[] { new Vector3(2.980232E-08f,2.086163E-07f,1f),   new Vector3(-0.7071071f,0.7071066f,-1.41561E-07f),  new Vector3() },
                new Vector3[] { new Vector3(2.980232E-08f,2.086163E-07f,1f),   new Vector3(0.7071071f,-0.7071066f,1.41561E-07f),   new Vector3() },
                new Vector3[] { new Vector3(1f,2.384186E-07f,-8.940697E-08f),  new Vector3(3.278255E-07f,-0.7071067f,0.707107f),   new Vector3() },
                new Vector3[] { new Vector3(1f,2.384186E-07f,-8.940697E-08f),  new Vector3(-3.278255E-07f,0.7071067f,-0.707107f),  new Vector3() },
                new Vector3[] { new Vector3(-4.172325E-07f,1f,-2.682209E-07f), new Vector3(0.7071068f,2.980232E-08f,-0.7071069f),  new Vector3() },
                new Vector3[] { new Vector3(-4.172325E-07f,1f,-2.682209E-07f), new Vector3(-0.7071068f,-2.980232E-08f,0.7071069f), new Vector3() },
                new Vector3[] { new Vector3(3.874302E-07f,-1f,2.980232E-07f),  new Vector3(-0.7071068f,-3.278255E-07f,-0.7071068f),new Vector3() },
                new Vector3[] { new Vector3(3.874302E-07f,-1f,2.980232E-07f),  new Vector3(0.7071068f,3.278255E-07f,0.7071068f),   new Vector3() },
                new Vector3[] { new Vector3(-1f,-2.384186E-07f,1.192093E-07f), new Vector3(-2.682209E-07f,0.7071069f,0.7071066f),  new Vector3() },
                new Vector3[] { new Vector3(-1f,-2.384186E-07f,1.192093E-07f), new Vector3(2.682209E-07f,-0.7071069f,-0.7071066f), new Vector3() },
                new Vector3[] { new Vector3(0f,-1.788139E-07f,-1f),            new Vector3(-0.7071065f,-0.707107f,2.682209E-07f),  new Vector3() },
                new Vector3[] { new Vector3(0f,-1.788139E-07f,-1f),            new Vector3(0.7071065f,0.707107f,-2.682209E-07f),   new Vector3() },
            };

            string[] trimerSubunitNamesX = new string[]
            {
                    "3X.1.1",  "3X.1.2",  "3X.1.3",
                    "3X.2.1",  "3X.2.2",  "3X.2.3",
                    "3X.3.1",  "3X.3.2",  "3X.3.3",
                    "3X.4.1",  "3X.4.2",  "3X.4.3",
            };

            string[] trimerSubunitNamesY = new string[]
            {
                    "3Y.1.1",  "3Y.1.2",  "3Y.1.3",
                    "3Y.2.1",  "3Y.2.2",  "3Y.2.3",
                    "3Y.3.1",  "3Y.3.2",  "3Y.3.3",
                    "3Y.4.1",  "3Y.4.2",  "3Y.4.3",
            };

            string[] dimerSubunitNames = new string[]
            {
                    "2.1.1",  "2.1.2",
                    "2.2.1",  "2.2.2",
                    "2.3.1",  "2.3.2",
                    "2.4.1",  "2.4.2",
                    "2.5.1",  "2.5.2",
                    "2.6.1",  "2.6.2",
            };
            
            base.Setup("C3X", 12);
            int subunitIndex = 0;
            trimersX.ToList().ForEach(xy => base.AddSymdefStyleCoordinateSystem("C3X", trimerSubunitNamesX[subunitIndex++], xy[0], xy[1], xy[2]));

            base.Setup("C3Y", 12);
            subunitIndex = 0;
            trimersY.ToList().ForEach(xy => base.AddSymdefStyleCoordinateSystem("C3Y", trimerSubunitNamesY[subunitIndex++], xy[0], xy[1], xy[2]));

            base.Setup("C2", 12);
            subunitIndex = 0;
            dimers.ToList().ForEach(xy => base.AddSymdefStyleCoordinateSystem("C2", dimerSubunitNames[subunitIndex++], xy[0], xy[1], xy[2]));
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            TetrahedralSymmetryBuilder builder = new TetrahedralSymmetryBuilder();
            graph.Add(this, builder);
            DeepCopyPopulateFields(graph, builder);
            return builder;
        }
    }
}
