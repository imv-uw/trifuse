using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    [Serializable]
    public class Selection : IDeserializationCallback
    {
        HashSet<IStructure> _structures = new HashSet<IStructure>();
        HashSet<IChain> _chains = new HashSet<IChain>();
        HashSet<IAa> _aas = new HashSet<IAa>();
        HashSet<IAtom> _atoms = new HashSet<IAtom>();

        public Selection() { }

        public Selection(Selection other)
        {
            _structures = new HashSet<IStructure>(other._structures);
            _chains = new HashSet<IChain>(other._chains);
            _aas = new HashSet<IAa>(other._aas);
            _atoms = new HashSet<IAtom>(other._atoms);
        }

        public Selection(IStructure structure)
        {
            this.UnionWith(structure);
        }

        public Selection(IEnumerable<IStructure> structures)
        {
            foreach (IStructure structure in structures)
            {
                this.UnionWith(structure);
            }
        }

        public Selection(IChain chain)
        {
            this.UnionWith(chain);
        }

        public Selection(IEnumerable<IChain> chains)
        {
            foreach (IChain chain in chains)
            {
                this.UnionWith(chain);
            }
        }

        public Selection(IAa aa)
        {
            this.UnionWith(aa);
        }

        public Selection(IEnumerable<IAa> aas)
        {
            foreach (IAa aa in aas)
            {
                this.UnionWith(aa);
            }
        }

        public Selection(Atom atom)
        {
            this.UnionWith(atom);
        }

        public Selection(IEnumerable<Atom> atoms)
        {
            foreach (Atom atom in atoms)
            {
                this.UnionWith(atom);
            }
        }

        public static Selection Union(Selection selection1, Selection selection2)
        {
            Selection result = new Selection(selection1);
            result.UnionWith(selection2);
            return result;
        }

        public static Selection Intersect(Selection selection1, Selection selection2)
        {
            Selection result = new Selection(selection1);
            result.IntersectWith(selection2);
            return result;
        }

        public static Selection Xor(Selection selection1, Selection selection2)
        {
            Selection result = new Selection(selection1);
            result.XorWith(selection2);
            return result;
        }

        public static Selection Except(Selection selection1, Selection selection2)
        {
            Selection result = new Selection(selection1);
            result.ExceptWith(selection2);
            return result;
        }

        /// <summary>
        /// Given a selection and two structures - an original and a cloned structure, returns a selection
        /// matching the original selection, but for the cloned structure
        /// </summary>
        /// <param name="template"></param>
        /// <param name="templateStructure"></param>
        /// <param name="cloneStructure"></param>
        /// <returns></returns>
        public static Selection Clone(Selection template, IStructure templateStructure, IStructure cloneStructure)
        {
            Selection result = new Selection();


            if (template.Structures.Contains(templateStructure))
                result.Structures.Add(cloneStructure);

            if (template.Atoms.Count == 0 && template.Aas.Count == 0 && template.Chains.Count == 0)
                return result;

            IChain[] tChains = templateStructure.ToArray();
            IChain[] cChains = cloneStructure.ToArray();
            Trace.Assert(tChains.Length == cChains.Length);

            if (template.Chains.Count > 0)
            {
                for (int i = 0; i < tChains.Length; i++)
                {
                    if (template.Chains.Contains(tChains[i]))
                        result.Chains.Add(cChains[i]);
                }
            }

            if (template.Aas.Count == 0 && template.Atoms.Count == 0)
                return result;

            IAa[] tAas = tChains.SelectMany(chain => chain).ToArray();
            IAa[] cAas = cChains.SelectMany(chain => chain).ToArray();
            Trace.Assert(tAas.Length == cAas.Length);

            if (template.Aas.Count > 0)
            {
                for(int i = 0; i < tAas.Length; i++)
                {
                    if (template.Aas.Contains(tAas[i]))
                        result.Aas.Add(cAas[i]);
                }
            }

            if (template.Atoms.Count == 0)
                return result;

            IAtom[] tAtoms = tAas.SelectMany(aa => aa).ToArray();
            IAtom[] cAtoms = cAas.SelectMany(aa => aa).ToArray();
            Trace.Assert(tAtoms.Length == cAtoms.Length);

            for (int i = 0; i < tAtoms.Length; i++)
            {
                if (template.Atoms.Contains(tAtoms[i]))
                    result.Atoms.Add(cAtoms[i]);
            }

            return result;
        }

        public void Clear()
        {
            _atoms.Clear();
            _aas.Clear();
            _chains.Clear();
            _structures.Clear();
        }

        public void UnionWith(IStructure structure)
        {
            _structures.Add(structure);
            foreach (IChain chain in structure)
            {
                this.UnionWith(chain);
            }
        }

        public void UnionWith(IChain chain)
        {
            _chains.Add(chain);
            foreach (IAa aa in chain)
            {
                this.UnionWith(aa);
            }
        }

        public void UnionWith(IAa aa)
        {
            _aas.Add(aa);
            foreach (IAtom atom in aa)
            {
                this.UnionWith(atom);
            }
        }

        public void UnionWith(IAtom atom)
        {
            _atoms.Add(atom);
        }

        public ISet<IStructure> Structures
        {
            get
            {
                return _structures;
            }
        }

        public ISet<IChain> Chains
        {
            get
            {
                return _chains;
            }
        }

        public ISet<IAa> Aas
        {
            get
            {
                return _aas;
            }
        }

        public ISet<IAtom> Atoms
        {
            get
            {
                return _atoms;
            }
        }

        public void IntersectWith(Selection other)
        {
            _structures.IntersectWith(other._structures);
            _chains.IntersectWith(other._chains);
            _aas.IntersectWith(other._aas);
            _atoms.IntersectWith(other._atoms);
        }

        public void UnionWith(Selection other)
        {
            _structures.UnionWith(other._structures);
            _chains.UnionWith(other._chains);
            _aas.UnionWith(other._aas);
            _atoms.UnionWith(other._atoms);
        }

        // Modify the current set to include objects that reside in only one set, but not both
        public void XorWith(Selection other)
        {
            _structures.SymmetricExceptWith(other._structures);
            _chains.SymmetricExceptWith(other._chains);
            _aas.SymmetricExceptWith(other._aas);
            _atoms.SymmetricExceptWith(other._atoms);
        }

        public void ExceptWith(Selection other)
        {
            _structures.ExceptWith(other._structures);
            _chains.ExceptWith(other._chains);
            _aas.ExceptWith(other._aas);
            _atoms.ExceptWith(other._atoms);
        }

        public void OnDeserialization(object sender)
        {
            _structures.OnDeserialization(sender);
            _chains.OnDeserialization(sender);
            _aas.OnDeserialization(sender);
            _atoms.OnDeserialization(sender);
        }

        public static Selection operator +(Selection set1, Selection set2)
        {
            Selection result = new Selection();
            result._structures.UnionWith(set1._structures);
            result._structures.UnionWith(set2._structures);
            result._chains.Union(set1._chains);
            result._chains.Union(set2._chains);
            result._aas.Union(set1._aas);
            result._aas.Union(set2._aas);
            result._atoms.Union(set1._atoms);
            result._atoms.Union(set2._atoms);
            return result;
        }

        public static Selection operator -(Selection set1, Selection set2)
        {
            Selection result = new Selection();
            result._structures.UnionWith(set1._structures);
            result._structures.ExceptWith(set2._structures);
            result._chains.Union(set1._chains);
            result._chains.ExceptWith(set2._chains);
            result._aas.Union(set1._aas);
            result._aas.ExceptWith(set2._aas);
            result._atoms.Union(set1._atoms);
            result._atoms.ExceptWith(set2._atoms);
            return result;
        }

        public static Selection operator &(Selection set1, Selection set2)
        {
            Selection result = new Selection();
            result._structures.UnionWith(set1._structures);
            result._structures.Intersect(set2._structures);
            result._chains.Union(set1._chains);
            result._chains.Intersect(set2._chains);
            result._aas.Union(set1._aas);
            result._aas.Intersect(set2._aas);
            result._atoms.Union(set1._atoms);
            result._atoms.Intersect(set2._atoms);
            return result;
        }
    }
}
