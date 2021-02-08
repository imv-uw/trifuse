using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    [Serializable]
    public class IndexSelection : IDeserializationCallback
    {
        HashSet<int> StructureIndices = new HashSet<int>();
        HashSet<int> ChainIndices = new HashSet<int>();
        HashSet<int> AaIndices = new HashSet<int>();
        HashSet<int> AtomIndices = new HashSet<int>();

        /// <summary>
        /// Stores the indices of selected structure, chain, aa, and atom indices from within a set of one or more structures.
        /// This enables serialization and storage of a Selection without references to the associated objects. The associated
        /// associated objects must be stored or regenerated separately.
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="selectionStructures"></param>
        public IndexSelection(Selection selection, Structure selectionStructure)
            : this(selection, new Structure[] { selectionStructure })
        {
        }

        public IndexSelection(Selection selection, IEnumerable<Structure> selectionStructures)
        {
            Structure[] structures = selectionStructures.ToArray();
            IChain[] chains = structures.SelectMany(s => s).ToArray();
            IAa[] aas = chains.SelectMany(c => c).ToArray();
            IAtom[] atoms = aas.SelectMany(a => a).ToArray();

            StructureIndices.UnionWith(Enumerable.Range(0, structures.Length).Where(i => selection.Structures.Contains(structures[i])));
            ChainIndices.UnionWith(Enumerable.Range(0, chains.Length).Where(i => selection.Chains.Contains(chains[i])));
            AaIndices.UnionWith(Enumerable.Range(0, aas.Length).Where(i => selection.Aas.Contains(aas[i])));
            AtomIndices.UnionWith(Enumerable.Range(0, atoms.Length).Where(i => selection.Atoms.Contains(atoms[i])));
        }

        public Selection ToSelection(Structure selectionStructure)
        {
            Selection selection = ToSelection(new Structure[] { selectionStructure });
            return selection;
        }

        public Selection ToSelection(IEnumerable<Structure> selectionStructures)
        {
            Structure[] structures = selectionStructures.ToArray();
            IChain[] chains = structures.SelectMany(s => s).ToArray();
            IAa[] aas = chains.SelectMany(c => c).ToArray();
            IAtom[] atoms = aas.SelectMany(a => a).ToArray();

            Selection selection = new Selection();
            selection.Structures.UnionWith(StructureIndices.Select(i => structures[i]));
            selection.Chains.UnionWith(ChainIndices.Select(i => chains[i]));
            selection.Aas.UnionWith(AaIndices.Select(i => aas[i]));
            selection.Atoms.UnionWith(AtomIndices.Select(i => atoms[i]));
            return selection;
        }

        public void OnDeserialization(object sender)
        {
            StructureIndices.OnDeserialization(sender);
            ChainIndices.OnDeserialization(sender);
            AaIndices.OnDeserialization(sender);
            AtomIndices.OnDeserialization(sender);
        }
    }
}
