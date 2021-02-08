using Core.Quick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Tools;
using Core.Symmetry;
using Newtonsoft.Json;
using Core.Interfaces;
using System.Diagnostics;
using Core.Quick.Pattern;

namespace Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Model
    {
        string _name;
        SymmetryBuilder _symmetry;
        PatternStructure _structure = new PatternStructure();
        
        [JsonProperty]
        public string Name
        {
            get => _name;
            set 
            {
                if (value == null)
                    // Keep it non-null for callees
                    _name = String.Empty;
                else 
                    _name = value;
            }
        }

        [JsonIgnore]
        public bool IsSymmetric { get => _symmetry as IdentitySymmetryBuilder == null; }

        [JsonProperty] public IStructure Structure { get => _structure; }
       
        [JsonProperty] public SymmetryBuilder Symmetry
        {
            get => _symmetry;
            private set
            {
                _symmetry = value?? new IdentitySymmetryBuilder();
                _structure.SetSymmetry(_symmetry);
            }
        }


        public Dictionary<string, Selection> Selections { get; private set; } = new Dictionary<string, Selection>();
        public Dictionary<string, Selection[]> SelectionSets { get; private set; } = new Dictionary<string, Selection[]>();
        public IArraySource<IChain> AsymmetricUnit { get => _structure.AsymmetricUnit; set => _structure.AsymmetricUnit = value; }

        // Members to store selections between deserialization constructor and deserialization callback. The selections
        // cannot be regenerated until Asu and Structure have been set.
        //[IgnoreDataMember]
        //Dictionary<string, IndexSelection> _deserializeSelections;
        //[IgnoreDataMember]
        //Dictionary<string, IndexSelection[]> _deserializeSelectionSets;

        [JsonConstructor]
        protected Model()
        {
        }

        public Model(IStructure structure)
        {
            Symmetry = new IdentitySymmetryBuilder();
            _structure.AsymmetricUnit = structure;
        }

        public Model(string name, IStructure structure)
            : this(structure)
        {
            Name = name;
        }

        public Model(SymmetryBuilder symmetry, IStructure asymmetricUnit)
        {
            Symmetry = symmetry;
            _structure.AsymmetricUnit = asymmetricUnit;
        }

        public Model(string name, SymmetryBuilder symmetry, IStructure asymmetricUnit)
            : this(symmetry, asymmetricUnit)
        {
            Name = name;
        }
    }
}
