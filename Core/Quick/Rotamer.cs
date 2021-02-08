using Core.Interfaces;
using Microsoft.Xna.Framework;
using NamespaceUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Core
{
    public class RotamerDefinition
    {
        [JsonProperty("residue")]
        public readonly string ResidueName;
        [JsonProperty("letter")]
        public readonly char ResidueLetter;
        [JsonProperty("backbone")]
        public readonly string[] BackboneAtomNames;
        [JsonProperty("dependents")]
        public readonly string[][] DependentAtomNames;

        public List<String> GetBackboneAtomNames(int chiIndex)
        {
            return BackboneAtomNames.ToList().GetRange(chiIndex - 1, 4);
        }

        public List<String> GetDependentAtomNames(int chiIndex)
        {
            return DependentAtomNames[chiIndex - 1].ToList();
        }

        public int GetChiCount()
        {
            if (BackboneAtomNames.Length == 0)
                return 0;
            Debug.Assert(BackboneAtomNames.Length >= 4);
            return BackboneAtomNames.Length - 3;
        }
    }

    public class RotamerInstanceSet
    {
        [JsonProperty("residue")]
        public readonly string[] ResidueNames;
        [JsonProperty("torsions")]
        public float[][] Torsions;

        public int Count { get { return Torsions.Length; } }
        public int ChiCount 
        { 
            get 
            {
                if (Count == 0) 
                    return 0;
                return Torsions[0].Length - 1;
            }
        }
        public float GetPercent(int index) { return Torsions[index][0]; }
        public float GetTorsionDegrees(int index, int chiIndex) { return Torsions[index][chiIndex]; }

        public void OrderByPercent()
        {
            throw new NotImplementedException();
        }

        public void DropZeroPercentRotamers()
        {
            throw new NotImplementedException();
        }
    }

    public class Rotamer
    {
        static Dictionary<string, RotamerInstanceSet[]> _instances = new Dictionary<string, RotamerInstanceSet[]>();

        static Vector3[/*residue type*/][/*rotamer index*/][/*atom index*/] coordinates_ = null;
        static RotamerDefinition[] definitions_ = null;
        static RotamerInstanceSet[] sets_ = null;

        public static IReadOnlyList<Vector3> GetAtomCoordinates(int residueNumber, int rotamerIndex)
        {
            return coordinates_[residueNumber][rotamerIndex];
        }

        public static int GetRotamerCount(int residueNumber)
        {
            return coordinates_[residueNumber].Length;
        }

        public static int GetRotamerCount(string name)
        {
            return _instances[name].Length;
        }

        public static int GetChiCount(int residueNumber)
        {
            return definitions_[residueNumber].GetChiCount();
        }

        public static RotamerDefinition GetDefinition(int residue)
        {
            return definitions_[residue];
        }

        public static void ParseJsonDefinitions()
        {
            string json = File.ReadAllText(Database.Paths.Rotamer.JsonRotamerDefinitions);
            JArray jArray = JArray.Parse(json);
            RotamerDefinition[] definitions = new RotamerDefinition[jArray.Count];
            
            for (int definitionIndex = 0; definitionIndex < jArray.Count; definitionIndex++)
            {
                RotamerDefinition definition = JsonConvert.DeserializeObject<RotamerDefinition>((jArray.ElementAt(definitionIndex).ToString()));
                definitions[definitionIndex] = definition;
            }
            definitions_ = definitions;
        }

        public static void ParseJsonInstanceSets()
        {
            string json = File.ReadAllText(Database.Paths.Rotamer.JsonRotamerInstances);
            JArray jArray = JArray.Parse(json);
            RotamerInstanceSet[] sets = new RotamerInstanceSet[jArray.Count];

            for (int definitionIndex = 0; definitionIndex < jArray.Count; definitionIndex++)
            {
                RotamerInstanceSet set = JsonConvert.DeserializeObject<RotamerInstanceSet>((jArray.ElementAt(definitionIndex).ToString()));
                sets[definitionIndex] = set;
            }
            sets_ = sets;
        }

        public static void CacheInstanceCoordinates()
        {
            coordinates_ = new Vector3[AaTable.AaCount][][];
            for (int residueTypeIndex = 0; residueTypeIndex < AaTable.AaCount; residueTypeIndex++)
            {
                IAa residue = new Aa(residueTypeIndex, true, true);
                RotamerInstanceSet rotamerSet = sets_.First(match => match.ResidueNames.Contains(residue.Name)); Debug.Assert(rotamerSet != null, "Residue type " + residue.Name + " was not found in rotamer_dynameomics.json");
                int rotamerCount = rotamerSet.Torsions.GetLength(0);
                coordinates_[residueTypeIndex] = new Vector3[rotamerCount][];
                if (rotamerCount == 0)
                    continue;
                    
                // Record the OXT index. This is shitty [TODO - rethink], but all required atoms (N,CA,C and sidechain) come first in the ResidueQuick atom list, 
                // and atoms that may not exist (OXT, H, H1,H2,H3) are last in the list. This allows for caching of coordinates for just the sidechain (as necessary when
                // dealing with rotamers) using a continuous list of indices in the range [0, index(OXT) - 1].
                int oxtIndex = 0;
                while (residue[oxtIndex].Name != "OXT")
                    oxtIndex++;

                // Iterate rotamers belonging to this residue type
                for(int rotamerIndex = 0; rotamerIndex < rotamerSet.Torsions.GetLength(0); rotamerIndex++)
                {
                    // Create a scratch residue to work with, to prevent floating point error buildup
                    IAa tmp = new Aa(residue);

                    // Apply torsions at each chi index
                    for (int chiIndex = 1; chiIndex <= rotamerSet.ChiCount; chiIndex++)
                    {
                        RotamerDefinition rotamerDefinition = definitions_[residueTypeIndex]; Debug.Assert(rotamerDefinition.ResidueName == residue.Name, "JSON for rotamer definitions and residue definitions is out of sync, rotamer definition=" + rotamerDefinition.ResidueName + ", residue definition=" + residue.Name);
                        float torsion = rotamerSet.GetTorsionDegrees(rotamerIndex, chiIndex);
                        SetChiAngleDegrees(tmp, chiIndex, torsion);
                        //SetChiAngleDegrees(tmp, torsion, rotamerDefinition.GetBackboneAtomNames(chiIndex), rotamerDefinition.GetDependentAtomNames(chiIndex));
                    }

                    // Cache the final coordinates 
                    coordinates_[residueTypeIndex][rotamerIndex] = new Vector3[oxtIndex];
                    for (int atomIndex = 0; atomIndex < oxtIndex; atomIndex++)
                    {
                        coordinates_[residueTypeIndex][rotamerIndex][atomIndex] = tmp[atomIndex].Xyz;
                    }
                }
            }
        }

        public static void SetChiAngleDegrees(IAa residue, int chiIndex, float degrees)
        {
            SetChiAngleRadians(residue, chiIndex, (float) (degrees * Math.PI / 180));
        }

        public static void SetChiAngleRadians(IAa residue, int chiIndex, float radians)
        {
            RotamerDefinition rotamerDefinition = definitions_[residue.ResidueNumber]; Debug.Assert(rotamerDefinition.ResidueName == residue.Name, "JSON for rotamer definitions and residue definitions is out of sync, rotamer definition=" + rotamerDefinition.ResidueName + ", residue definition=" + residue.Name);

            List<string> torsionBackboneNames = rotamerDefinition.GetBackboneAtomNames(chiIndex);
            List<string> dependentNames = rotamerDefinition.GetDependentAtomNames(chiIndex);

            Debug.Assert(dependentNames.Contains(torsionBackboneNames[3]), "The last atom that defines a torsion should move when that torsion is changed. Fix JSON 'dependents' for residue '" + residue.Name + "' atom '" + torsionBackboneNames[3] + "'");

            string name1 = torsionBackboneNames[0];
            string name2 = torsionBackboneNames[1];
            string name3 = torsionBackboneNames[2];
            string name4 = torsionBackboneNames[3];

            double currentTorsionRadians = VectorMath.GetDihedralAngleRadians(residue[name1].Xyz, residue[name2].Xyz, residue[name3].Xyz, residue[name4].Xyz);
            double deltaDegrees = radians - currentTorsionRadians;

            Vector3 coord2 = residue[name2].Xyz;
            Vector3 coord3 = residue[name3].Xyz;
            Vector3 axis = Vector3.Normalize(coord3 - coord2);
            Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, radians);

            foreach (string dependentName in dependentNames)
            {
                IAtom atom = residue[dependentName];
                atom.Xyz -= coord3;
                atom.Xyz = Vector3.Transform(atom.Xyz, rotation);
                atom.Xyz += coord3;
            }
        }
    }
}
