using Microsoft.Xna.Framework;
using NamespaceUtilities;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
//using Test.DataStructures;
using Core.Interfaces;
using Core.Quick;
using System;
using Core.Collections;
using System.Collections;
using Core.Quick.Pattern;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Aa : IAa
    {
        /// <summary>
        /// A parameter-less constructor is required by the deep-copy implementation. 
        /// The constructos ir marked as protected so that derived classes may be deep-copied.
        /// </summary>

        [JsonConstructor]
        protected Aa() { }

        [JsonProperty] ArraySource<IAtom> _atoms;
        [JsonProperty] int _residueNumber = -1;


        // ... bleh.
        public const int N_ = AaDefinition.IndexN;
        public const int CA_ = AaDefinition.IndexCA;
        public const int C_ = AaDefinition.IndexC;
        public const int O_ = AaDefinition.IndexO;
        public const int SidechainStart_ = O_ + 1;
        //public const int H_ = Structure.Quick.ResidueQuickDefinition.IndexH;


        public char Letter { get { return AaTable.GetResidueLetter(_residueNumber); } set { ResidueNumber = AaTable.GetResidueTypeIndex(value); } }
        public string Name { get { return AaTable.GetResidueName(_residueNumber); } set { ResidueNumber = AaTable.GetResidueTypeIndex(value); } }
        public bool IsNTerminus { get; private set; }
        public bool IsCTerminus { get; private set; }
        public int IsNTerminusAsIndex { get { return IsNTerminus ? 1 : 0; } }
        public int IsCTerminusAsIndex { get { return IsCTerminus ? 1 : 0; } }
        public bool IsMirror => false;
        public int ResidueNumber
        {
            get
            {
                return _residueNumber;
            }
            set
            {
                if (_residueNumber == value)
                    return;

                AtomDefinition[] atomDefinitions = AaTable.GetAtomDefinitions(value, IsNTerminus, IsCTerminus);
                IAtom[] atoms = new Atom[atomDefinitions.Length];

                for (int i = 0; i < atomDefinitions.Length; i++)
                {
                    if (_residueNumber == -1)
                    {
                        Vector3 position = new Vector3(atomDefinitions[i].X, atomDefinitions[i].Y, atomDefinitions[i].Z);
                        atoms[i] = new Atom(atomDefinitions[i], position);
                    }
                    else if (i <= O_)
                    {
                        atoms[i] = _atoms[i];
                    }
                    else if (atomDefinitions[i].Name == "H" && _atoms.Any(a => a.Name == "H"))
                    {
                        IAtom existingH = _atoms.Single(a => a.Name == "H");
                        atoms[i] = existingH;
                    }
                    else
                    {
                        Vector3 position = new Vector3(atomDefinitions[i].X, atomDefinitions[i].Y, atomDefinitions[i].Z);
                        atoms[i] = new Atom(atomDefinitions[i], position);
                    }
                }

                _residueNumber = value;

                Clear();
                AddRange(atoms);
            }
        }

        public Aa(int residueNumber, bool nTerminus, bool cTerminus)
        {
            _atoms = new ArraySource<IAtom>(this);
            //AtomDefinition[] atomDefinitions = AaTable.GetAtomDefinitions(residueNumber, nTerminus, cTerminus);
            //Atom[] atoms = new Atom[atomDefinitions.Length];

            //for(int i = 0; i < atomDefinitions.Length; i++)
            //{
            //    Vector3 position = new Vector3(atomDefinitions[i].X, atomDefinitions[i].Y, atomDefinitions[i].Z);
            //    atoms[i] = new Atom(atomDefinitions[i], position);
            //}
            //AddRange(atoms);
            IsNTerminus = nTerminus;
            IsCTerminus = cTerminus;
            ResidueNumber = residueNumber;
        }

        public Aa(string residueName, bool nTerminus, bool cTerminus)
            : this(AaTable.GetResidueTypeIndex(residueName), nTerminus, cTerminus)
        {
        }

        public Aa(char letter, bool nTerminus, bool cTerminus)
            : this(AaTable.GetResidueTypeIndex(letter), nTerminus, cTerminus)
        {

        }

        public Aa(IAa other)
            : this(other, other.IsNTerminus, other.IsCTerminus)
        {
        }

        public Aa(IAa other, bool nTerminus, bool cTerminus)
        {
            _atoms = new ArraySource<IAtom>(this, other);
            
            ResidueNumber = other.ResidueNumber;
            IsNTerminus = nTerminus;
            IsCTerminus = cTerminus;

            AtomDefinition[] definitions = AaTable.GetAtomDefinitions(ResidueNumber, IsNTerminus, IsCTerminus);

            Matrix unidentifiedAtomTransform = Matrix.Identity;
            //if (TransformSetting == TransformSettings.Transform)
            {
                Vector3[] vLocal = new Vector3[] { definitions[N_].Xyz, definitions[CA_].Xyz, definitions[C_].Xyz };
                Vector3[] vOther = new Vector3[] { other[N_].Xyz, other[CA_].Xyz, other[C_].Xyz };
                unidentifiedAtomTransform = VectorMath.GetRmsdAlignmentMatrix(vLocal, false, vOther, false);
            }

            // Create atoms at final positions
            for (int i = 0; i < definitions.Length; i++)
            {
                Vector3 position = Vector3.Transform(definitions[i].Xyz, unidentifiedAtomTransform);
                Atom atom = new Atom(definitions[i], position);
                Add(atom);
            }

            this[N_].Xyz = other[N_].Xyz;
            this[CA_].Xyz = other[CA_].Xyz;
            this[C_].Xyz = other[C_].Xyz;
            this[O_].Xyz = other[O_].Xyz;

            for (int i = Aa.SidechainStart_; i < other.Count; i++)
            {
                IAtom otherAtom = other[i];
                IAtom thisAtom = this[otherAtom.Name];
                if(thisAtom != null)
                {
                    thisAtom.Xyz = otherAtom.Xyz;
                }
            }

            // Remove parent context - this will result in atoms NOT BEING IN THE ORIGINAL LOCATION
            this.Parent = null;
        }

        public void AlignToNCAC(IAa other)
        {
            AlignToNCAC(other[N_].Xyz, other[CA_].Xyz, other[C_].Xyz);            
        }

        public void AlignToNCAC(Vector3 N, Vector3 CA, Vector3 C)
        {
            Vector3[] vOther = new Vector3[] { N, CA, C };
            Vector3[] vLocal = new Vector3[] { this[N_].Xyz, this[CA_].Xyz, this[C_].Xyz };
            Matrix matrix = VectorMath.GetRmsdAlignmentMatrix(vLocal, false, vOther, false);
            this.Transform(matrix);
        }  

        public Vector3 HeavyXYZ
        {
            get
            {
                IEnumerable<IAtom> heavyAtoms = this.Where(atom => atom.Element != Element.H);
                Vector3 position = heavyAtoms.Select(atom => atom.Xyz).Aggregate((a, b) => a + b) / heavyAtoms.Count();
                return position;
            }
        }

        public Vector3 UnitX
        {
            get
            {
                Vector3 N = this[Aa.N_].Xyz;
                Vector3 CA = this[Aa.CA_].Xyz;
                Vector3 C = this[Aa.C_].Xyz;
                Vector3 unitX = Vector3.Normalize(N + C - 2 * CA);
                return unitX;
            }
        }

        public Vector3 UnitY
        {
            get
            {
                Vector3 N = this[Aa.N_].Xyz;
                Vector3 CA = this[Aa.CA_].Xyz;
                Vector3 C = this[Aa.C_].Xyz;
                Vector3 x = N + C - 2 * CA;
                Vector3 unitY = Vector3.Normalize(Vector3.Cross(x, C - CA));
                return unitY;
            }
        }

        public Vector3 UnitZ
        {
            get
            {
                Vector3 N = this[Aa.N_].Xyz;
                Vector3 CA = this[Aa.CA_].Xyz;
                Vector3 C = this[Aa.C_].Xyz;
                Vector3 x = N + C - 2 * CA;
                Vector3 y = Vector3.Cross(x, C - CA);
                Vector3 unitZ = Vector3.Cross(x, y);
                return unitZ;
            }
        }

        public Vector3 Origin
        {
            get
            {
                Vector3 CA = this[Aa.CA_].Xyz;
                return CA;
            }
        }

        public virtual ITransformNode Parent { get => _atoms.Parent; set => _atoms.Parent = value; }

        public int Count => _atoms.Count;

        public bool IsReadOnly => _atoms.IsReadOnly;

        public virtual Matrix NodeTransform { get => _atoms.NodeTransform; set => _atoms.NodeTransform = value; }

        public virtual Matrix TotalParentTransform => _atoms.TotalParentTransform;

        public virtual Matrix TotalTransform => _atoms.TotalTransform;

        public virtual Vector3 Force { get; set; }

        public virtual float Energy { get; set; }

        public IList<IAtom> this[int start, int end] => _atoms[start, end];

        public IAtom this[int index] { get => _atoms[index]; set => _atoms[index] = value; }

        public void GetCoordinateSystem(out Vector3 origin, out Vector3 unitX, out Vector3 unitY, out Vector3 unitZ)
        {
            Vector3 N = this[Aa.N_].Xyz;
            Vector3 CA = this[Aa.CA_].Xyz;
            Vector3 C = this[Aa.C_].Xyz;
            unitX = Vector3.Normalize(N + C - 2 * CA);
            unitY = Vector3.Normalize(Vector3.Cross(unitX, C - CA));
            unitZ = Vector3.Cross(unitX, unitY);
            origin = CA;
        }

        public IAtom this[string name]
        {
            get
            {
                foreach (Atom atom in this)
                {
                    if (atom.Name == name)
                        return atom;
                }
                return null;
            }
        }

        public void AddMonitor(IArraySourceMonitor<IAtom> monitor)
        {
            _atoms.AddMonitor(monitor);
        }

        public void RemoveMonitor(IArraySourceMonitor<IAtom> monitor)
        {
            _atoms.RemoveMonitor(monitor);
        }

        public void RotateNode(Quaternion rotation, Vector3 origin)
        {
            _atoms.RotateNode(rotation, origin);
        }

        public void TransformNode(Matrix transform)
        {
            _atoms.TransformNode(transform);
        }

        public void TranslateNode(Vector3 translation)
        {
            _atoms.TranslateNode(translation);
        }

        public void Rotate(Quaternion rotation, Vector3 origin)
        {
            _atoms.Rotate(rotation, origin);
        }

        public void Transform(Matrix transform)
        {
            _atoms.Transform(transform);
        }

        public void Translate(Vector3 translation)
        {
            _atoms.Translate(translation);
        }

        public int IndexOf(IAtom item)
        {
            return _atoms.IndexOf(item);
        }

        public void Insert(int index, IAtom item)
        {
            _atoms.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _atoms.RemoveAt(index);
        }

        public void Add(IAtom item)
        {
            _atoms.Add(item);
        }

        public void Clear()
        {
            _atoms.Clear();
        }

        public bool Contains(IAtom item)
        {
            return _atoms.Contains(item);
        }

        public void CopyTo(IAtom[] array, int arrayIndex)
        {
            _atoms.CopyTo(array, arrayIndex);
        }

        public bool Remove(IAtom item)
        {
            return _atoms.Remove(item);
        }

        public IEnumerator<IAtom> GetEnumerator()
        {
            return _atoms.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddRange(IEnumerable<IAtom> items)
        {
            _atoms.AddRange(items);
        }

        public void RemoveRange(int start, int count)
        {
            _atoms.RemoveRange(start, count);
        }

        public void AddArraySource(IArraySource<IAtom> source)
        {
            _atoms.AddArraySource(source);
        }

        public void RemoveArraySource(IArraySource<IAtom> source)
        {
            _atoms.RemoveArraySource(source);
        }

        public virtual IAa GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorAa(this, root, parent);
        }

        public virtual IAa GetMirrorTemplate()
        {
            return this;
        }

        //public IArraySource<IAtom> Clone()
        //{
        //    throw new NotImplementedException();
        //}

        public object DeepCopy()
        {
            IDeepCloneObjectGraph context = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(context);
        }

        public object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return (IAa)clone;

            Aa aa = new Aa();
            graph.Add(this, aa);
            DeepCopyPopulateFields(graph, aa);
            return aa;
        }

        public void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            Aa aa = (Aa) clone;
            aa._atoms = (ArraySource<IAtom>) _atoms.DeepCopyFindOrCreate(graph);

            aa._residueNumber = _residueNumber;
            aa.IsNTerminus = IsNTerminus;
            aa.IsCTerminus = IsCTerminus;
        }

        public void InsertArraySource(int index, IArraySource<IAtom> source)
        {
            _atoms.InsertArraySource(index, source);
        }

        public void AddArraySourceInPlace(IArraySource<IAtom> source)
        {
            _atoms.AddArraySourceInPlace(source);
        }

        public void InsertArraySourceInPlace(int index, IArraySource<IAtom> source)
        {
            _atoms.InsertArraySourceInPlace(index, source);
        }

        public void DisconnectDependent(object dependent)
        {
            _atoms.DisconnectDependent(dependent);
        }
    }
}
