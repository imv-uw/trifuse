using Core;
using Core.Interfaces;
using Core.Quick;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Core.Symmetry
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SymmetryBuilder : IDeserializationCallback, IDeepCopy
    {
        [JsonProperty] Dictionary<string, string[]> _unitIdToSubunitNames = new Dictionary<string, string[]>();
        [JsonProperty] Dictionary<string, CoordinateSystem[]> _unitIdToSubunitCoordinateSystems = new Dictionary<string, CoordinateSystem[]>();

        [JsonProperty]
        public string[] EnabledUnits { get; set; }

        [JsonConstructor]
        protected SymmetryBuilder() { }

        public virtual void OnDeserialization(object sender)
        {
            _unitIdToSubunitNames.OnDeserialization(sender);
            _unitIdToSubunitCoordinateSystems.OnDeserialization(sender);
        }

        public int GetMultiplicity(string axis)
        {
            SymmetryDescriptor descriptor = SymmetryBuilderFactory.GetDescriptor(this.GetType());
            for(int i = 0; i < descriptor.Units.Length; i++)
            {
                if (descriptor.Units[i].Equals(axis, StringComparison.InvariantCultureIgnoreCase))
                    return descriptor.Multiplicities[i];
            }
            throw new ArgumentException(String.Format("{0} has no unit {1}", descriptor.Architecture, axis));
        }

        public IList<string> GetAxesWithMultiplicity(int multiplicity)
        {
            List<string> axes = new List<string>();
            SymmetryDescriptor descriptor = SymmetryBuilderFactory.GetDescriptor(this.GetType());
            for (int i = 0; i < descriptor.Units.Length; i++)
            {
                if (descriptor.Multiplicities[i] == multiplicity)
                    axes.Add(descriptor.Units[i]);
            }
            return axes;
        }

        // unused
        public string[] GetUnits() { return SymmetryBuilderFactory.GetDescriptor(this.GetType()).Units; }
        public string GetArchitecture() { return SymmetryBuilderFactory.GetDescriptor(this.GetType()).Architecture; }

        public virtual CoordinateSystem GetPrincipalCoordinateSystem(string unitId)
        {
            return GetCoordinateSystems(unitId).First();
        }

        public virtual CoordinateSystem GetCoordinateSystem(string unitId, int index)
        {
            return new CoordinateSystem(_unitIdToSubunitCoordinateSystems[unitId][index]);
        }

        public virtual CoordinateSystem[] GetCoordinateSystems(string unitId)
        {
            //TODO: Make a real copy not a shallow copy
            return (CoordinateSystem[])_unitIdToSubunitCoordinateSystems[unitId].Clone();
        }

        public int GetCoordinateSystemsCount(string unitId)
        {
            return _unitIdToSubunitCoordinateSystems[unitId].Length;
        }

        protected CoordinateSystem GetTemplatePrincipalCoordinateSystem(string unitId)
        {
            return GetCoordinateSystems(unitId).First();
        }

        protected CoordinateSystem[] GetTemplateCoordinateSystems(string unitId)
        {
            return (CoordinateSystem[])_unitIdToSubunitCoordinateSystems[unitId].Clone();
        }

        // Subclass coordinate system access methods
        protected void Setup(string unitId, int subunitCount)
        {
            _unitIdToSubunitNames[unitId] = new string[subunitCount];
            _unitIdToSubunitCoordinateSystems[unitId] = new CoordinateSystem[subunitCount];
        }

        protected void AddSymdefStyleCoordinateSystem(string unitId, string subunitId, Vector3 x, Vector3 y, Vector3 translation)
        {
            if (!_unitIdToSubunitNames.ContainsKey(unitId))
                throw new ArgumentException("Invalid attempt to add coordinate systems without first calling Setup()");

            // Create a the given coordinate system
            CoordinateSystem system = new CoordinateSystem();
            system.Transform = Matrix.CreateWorld(translation, -Vector3.Normalize(Vector3.Cross(x, y)), Vector3.Normalize(y));
            
            AddCoordinateSystem(unitId, subunitId, system);

            Trace.Assert(Math.Abs(Vector3.Cross(Vector3.Normalize(x), Vector3.Normalize(y)).Length() - 1) < 0.001, String.Format("Symmetry definition X and Y axes are not orthogonal:\n X {0}, Y {1}", x.ToString(), y.ToString()));
        }

        protected void AddCoordinateSystem(string unitId, string subunitId, CoordinateSystem system)
        {

            // Fill in the first blank entry in the table
            for (int i = 0; i <= _unitIdToSubunitNames[unitId].Length; i++)
            {
                if (i == _unitIdToSubunitNames[unitId].Length)
                    throw new ArgumentException("Invalid attempt to add more coordinate systems than were initialized by Setup()");

                if (_unitIdToSubunitNames[unitId][i] != null)
                    continue;

                _unitIdToSubunitNames[unitId][i] = subunitId;
                _unitIdToSubunitCoordinateSystems[unitId][i] = system;
                break;
            }
        }

        public IStructure Pattern(string unitId, IStructure structure)
        {
            Structure pattern = new Structure();

            foreach(CoordinateSystem coordinateSystem in GetTemplateCoordinateSystems(unitId))
            {
                Structure copy = new Structure(structure);
                copy.Transform(coordinateSystem.Transform);
                pattern.AddRange(copy);
            }
            return pattern;
        }

        public IStructure Pattern(string unitId, int index, IStructure structure)
        {
            CoordinateSystem coordinateSystem = GetTemplateCoordinateSystems(unitId)[index];
            Structure copy = (Structure)structure.DeepCopy();
            copy.Transform(coordinateSystem.Transform);
            return copy;
        }

        public object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(graph);
        }

        public abstract object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph);

        public virtual void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            SymmetryBuilder builder = (SymmetryBuilder)clone;
            builder._unitIdToSubunitNames = _unitIdToSubunitNames;
            builder._unitIdToSubunitCoordinateSystems = _unitIdToSubunitCoordinateSystems;
            builder.EnabledUnits = EnabledUnits == null? null : (string[]) EnabledUnits.Clone();
        }
    }
}
