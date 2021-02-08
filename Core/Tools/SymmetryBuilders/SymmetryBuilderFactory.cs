using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Core.Symmetry
{
    public class SymmetryDescriptor
    {
        string[] _units;
        int[] _multiplicities;

        public SymmetryDescriptor(Type type, string architecture, string[] units, int[] multiplicities)
        {
            Type = type;
            Architecture = architecture;
            _units = (string[]) units.Clone();
            _multiplicities = (int[]) multiplicities.Clone();

            Trace.Assert(units != null && multiplicities != null && units.Length == multiplicities.Length);
        }

        public Type Type { get; private set; }
        public string Architecture { get; private set; }
        public string[] Units { get { return (string[])_units.Clone(); } }
        public int[] Multiplicities { get { return (int[])_multiplicities.Clone(); } }
    }

    public static class SymmetryBuilderFactory
    {
        static Dictionary<string, SymmetryDescriptor> _knownSymmetriesByName = new Dictionary<string, SymmetryDescriptor>();
        static Dictionary<Type, SymmetryDescriptor> _knownSymmetriesByType = new Dictionary<Type, SymmetryDescriptor>();

        static SymmetryBuilderFactory()
        {
            SymmetryDescriptor[] descriptors = new SymmetryDescriptor[]
            {
                // Cyclic
                new SymmetryDescriptor(typeof(C2SymmetryBuilder), C2SymmetryBuilder.Architecture, C2SymmetryBuilder.Units, C2SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C3SymmetryBuilder), C3SymmetryBuilder.Architecture, C3SymmetryBuilder.Units, C3SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C4SymmetryBuilder), C4SymmetryBuilder.Architecture, C4SymmetryBuilder.Units, C4SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C5SymmetryBuilder), C5SymmetryBuilder.Architecture, C5SymmetryBuilder.Units, C5SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C6SymmetryBuilder), C6SymmetryBuilder.Architecture, C6SymmetryBuilder.Units, C6SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C7SymmetryBuilder), C7SymmetryBuilder.Architecture, C7SymmetryBuilder.Units, C7SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C8SymmetryBuilder), C8SymmetryBuilder.Architecture, C8SymmetryBuilder.Units, C8SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C9SymmetryBuilder), C9SymmetryBuilder.Architecture, C9SymmetryBuilder.Units, C9SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C10SymmetryBuilder), C10SymmetryBuilder.Architecture, C10SymmetryBuilder.Units, C10SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C11SymmetryBuilder), C11SymmetryBuilder.Architecture, C11SymmetryBuilder.Units, C11SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(C12SymmetryBuilder), C12SymmetryBuilder.Architecture, C12SymmetryBuilder.Units, C12SymmetryBuilder.Multiplicities),

                // Dihedral
                new SymmetryDescriptor(typeof(D2SymmetryBuilder), D2SymmetryBuilder.Architecture, D2SymmetryBuilder.Units, D2SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D3SymmetryBuilder), D3SymmetryBuilder.Architecture, D3SymmetryBuilder.Units, D3SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D4SymmetryBuilder), D4SymmetryBuilder.Architecture, D4SymmetryBuilder.Units, D4SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D5SymmetryBuilder), D5SymmetryBuilder.Architecture, D5SymmetryBuilder.Units, D5SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D6SymmetryBuilder), D6SymmetryBuilder.Architecture, D6SymmetryBuilder.Units, D6SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D7SymmetryBuilder), D7SymmetryBuilder.Architecture, D7SymmetryBuilder.Units, D7SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D8SymmetryBuilder), D8SymmetryBuilder.Architecture, D8SymmetryBuilder.Units, D8SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D9SymmetryBuilder), D9SymmetryBuilder.Architecture, D9SymmetryBuilder.Units, D9SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D10SymmetryBuilder), D10SymmetryBuilder.Architecture, D10SymmetryBuilder.Units, D10SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D11SymmetryBuilder), D11SymmetryBuilder.Architecture, D11SymmetryBuilder.Units, D11SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(D12SymmetryBuilder), D12SymmetryBuilder.Architecture, D12SymmetryBuilder.Units, D12SymmetryBuilder.Multiplicities),

                // Cage         
                new SymmetryDescriptor(typeof(TetrahedralSymmetryBuilder), TetrahedralSymmetryBuilder.Architecture, TetrahedralSymmetryBuilder.Units,  TetrahedralSymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(OctahedralSymmetryBuilder), OctahedralSymmetryBuilder.Architecture, OctahedralSymmetryBuilder.Units, OctahedralSymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(IcosahedralSymmetryBuilder), IcosahedralSymmetryBuilder.Architecture, IcosahedralSymmetryBuilder.Units, IcosahedralSymmetryBuilder.Multiplicities),

                // Plane
                new SymmetryDescriptor(typeof(P4SymmetryBuilder), P4SymmetryBuilder.Architecture, P4SymmetryBuilder.Units, P4SymmetryBuilder.Multiplicities),
                new SymmetryDescriptor(typeof(P6SymmetryBuilder), P6SymmetryBuilder.Architecture, P6SymmetryBuilder.Units, P6SymmetryBuilder.Multiplicities)
            };

            foreach(SymmetryDescriptor descriptor in descriptors)
            {
                _knownSymmetriesByName.Add(descriptor.Architecture, descriptor);
                _knownSymmetriesByType.Add(descriptor.Type, descriptor);
            }
        }

        public static SymmetryBuilder Clone(SymmetryBuilder template)
        {
            Type symmetryType = template.GetType();
            SymmetryDescriptor descriptor = _knownSymmetriesByType[symmetryType];
            SymmetryBuilder builder = CreateFromSymmetryName(descriptor.Architecture);
            builder.EnabledUnits = template.EnabledUnits == null? null : (string[])template.EnabledUnits.Clone();
            return builder;
        }

        public static SymmetryDescriptor GetDescriptor(Type type)
        {
            return _knownSymmetriesByType[type];
        }

        public static SymmetryDescriptor GetDescriptor(string name)
        {
            return _knownSymmetriesByName[name];
        }



        public static string[] GetKnownSymmetryNames()
        {
            return _knownSymmetriesByName.Keys.ToArray();
        }

        public static string[] GetSymmetryUnitIds(string symmetryName)
        {
            if (!_knownSymmetriesByName.ContainsKey(symmetryName))
            {
                throw new ArgumentException(String.Format("Unknown symmetry requested: {0}", symmetryName));
            }

            return _knownSymmetriesByName[symmetryName].Units;
        }

        

        public static SymmetryBuilder CreateFromSymmetryName(string symmetryName)
        {
            if(!_knownSymmetriesByName.ContainsKey(symmetryName))
            {
                throw new ArgumentException(String.Format("Unknown symmetry requested: {0}", symmetryName));
            }

            SymmetryBuilder instance = (SymmetryBuilder)Activator.CreateInstance(_knownSymmetriesByName[symmetryName].Type);

            return instance;
        }

        public static SymmetryBuilder CreateFromSymmetryName(string symmetryName, string unitId)
        {
            SymmetryBuilder instance = CreateFromSymmetryName(symmetryName, new string[] { unitId });
            return instance;
        }

        public static SymmetryBuilder CreateFromSymmetryName(string symmetryName, IEnumerable<string> unitIds)
        {
            SymmetryBuilder instance = CreateFromSymmetryName(symmetryName);
            instance.EnabledUnits = unitIds.ToArray();
            return instance;
        }
    }
}
