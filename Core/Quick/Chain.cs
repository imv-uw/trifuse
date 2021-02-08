using Microsoft.Xna.Framework;
using NamespaceUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Quick;
using Core.Collections;
using Core.Interfaces;
using System.Collections;
using Tools;
using Core.Quick.Pattern;
using Newtonsoft.Json;

namespace Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Chain : IChain
    {
        [JsonProperty]
        NodeArraySource<IAa> _aas;

        // ... gross:
        public const int N_ = AaDefinition.IndexN;
        public const int CA_ = AaDefinition.IndexCA;
        public const int C_ = AaDefinition.IndexC;
        public const int O_ = AaDefinition.IndexO;
        
        public IReadOnlyList<IAtom> Atoms { get { return this.SelectMany(aa => aa).ToList().AsReadOnly(); } }

        [JsonConstructor]
        public Chain()
        {
            _aas = new NodeArraySource<IAa>(this);
        }

        public string GetSequence1()
        {
            if (_aas.Count == 0)
                return "";
            string sequence = this.Select(residue => residue.Letter.ToString()).Aggregate((i, j) => i + j);
            return sequence;
        }

        public Chain(IEnumerable<IAa> aas, bool clone = true)
        {
            _aas = new NodeArraySource<IAa>(this);
            if (clone)
            {
                _aas.AddRange(aas.Select(aa => new Aa(aa)));
            }
            else
            {
                _aas.AddRange(aas);
            }
        }

        public Chain(string sequence)
        {
            _aas = new NodeArraySource<IAa>(this);
            foreach (char residue in sequence)
            {
                AddAtTerminus(residue);
            }
        }

        public Chain(IEnumerable<char> sequence)
        {
            _aas = new NodeArraySource<IAa>(this);
            foreach (char residue in sequence)
            {
                AddAtTerminus(residue);
            }
        }

        [JsonProperty]
        public float Energy { get; set; }

        [JsonProperty]
        public Vector3 Force { get; set; }

        public IAa this[int index, bool reposition]
        {
            set
            {
                if(reposition)
                {
                    IAa current = this[index];
                    Vector3 N = current[Aa.N_].Xyz;
                    Vector3 CA = current[Aa.CA_].Xyz;
                    Vector3 C = current[Aa.C_].Xyz;
                    this[index] = value;
                    this[index].AlignToNCAC(N, CA, C);
                }
                else
                {
                    this[index] = value;
                }
            }
        }
        public IAa this[int index] { get => _aas[index]; set => _aas[index] = value; }
        public IList<IAa> this[int start, int end] => _aas[start, end];

        public void AddAtTerminus(char letter)
        {
            bool nTerminus = this.Count == 0;
            bool cTerminus = true;
            IAa residue = new Aa(letter, nTerminus, cTerminus);
            AddAtTerminus(residue);
        }

        public void AddAtTerminus(string name)
        {
            bool nTerminus = this.Count == 0;
            bool cTerminus = true;
            IAa residue = new Aa(name, nTerminus, cTerminus);
            AddAtTerminus(residue);
        }

        public void AddAtTerminus(IAa residue)
        {
            if (Count > 0)
            {
                IAa previous = this[this.Count - 1];
                IAa replacement = new Aa(previous, previous.IsNTerminus, false);
                this[this.Count - 1] = replacement;
                OrientPlanar(replacement, residue);
                _aas.AddInPlace(residue);
            }   
            else
            {
                _aas.Add(residue);
            }
        }

        public void Add(IAa residue)
        {
            _aas.Add(residue);
        }

        public void AddArraySourceInPlace(IArraySource<IAa> source)
        {
            _aas.AddArraySourceInPlace(source);
        }

        public void AddInPlace(IAa item)
        {
            _aas.AddInPlace(item);
        }

        public void AddRangeInPlace(IEnumerable<IAa> items)
        {
            _aas.AddRangeInPlace(items);
        }

        public void Mutate(int residueIndex, char letter)
        {
            int residueType = AaTable.GetResidueTypeIndex(letter);
            Replace(residueIndex, residueType);
        }

        public void Mutate(int residueIndex, string name)
        {
            int residueType = AaTable.GetResidueTypeIndex(name);
            Replace(residueIndex, residueType);
        }

        public void Mutate(int index, int aa)
        {
            Replace(index, aa);
        }

        void Replace(int residueIndex, int residueType)
        {
            IAa current = this[residueIndex];
            IAa residue = new Aa(residueType, current.IsNTerminus, current.IsCTerminus);
            residue.AlignToNCAC(current);
            residue[N_].Xyz = current[N_].Xyz;
            residue[CA_].Xyz = current[CA_].Xyz;
            residue[C_].Xyz = current[C_].Xyz;
            residue[O_].Xyz = current[O_].Xyz;

            IAtom currentH = current["H"];
            IAtom newH = residue["H"];
            if (currentH != null && newH != null)
                newH.Xyz = currentH.Xyz;

            this[residueIndex] = residue;
        }

        void Replace(int residueIndex, IAa residue, bool reposition = true)
        {
            IAa current = this[residueIndex];
            if(reposition)
            {
                residue.AlignToNCAC(current);
                residue[N_].Xyz = current[N_].Xyz;
                residue[CA_].Xyz = current[CA_].Xyz;
                residue[C_].Xyz = current[C_].Xyz;
                residue[O_].Xyz = current[O_].Xyz;

                IAtom currentH = current["H"];
                IAtom newH = residue["H"];
                if (currentH != null && newH != null)
                    newH.Xyz = currentH.Xyz;
            }
            this[residueIndex] = residue;
        }


        /// <summary>
        /// Append a residue to the peptide. Fix up the C-terminus residue and orient the new
        /// residue properly if the 'reposition' flag is given.
        /// </summary>
        /// <param name="residue"></param>
        /// <param name="reposition"></param>

        public void Orient(IAa r1, IAa r2)
        {
            r2.Translate(-r2[N_].Xyz);
            Matrix mat = GetOrientationTransformAboutR2N(r1, r2);
            foreach(Atom atom in r2)
            {
                atom.Xyz = Vector3.Transform(atom.Xyz, mat);
            }
        }

        // TODO: Get rid of this in favor of RMSD minimization (using a fragment based approach
        // with 2 residues and trailing residue prepositioned?)
        // Orient the new residue s.t. the torsion C->N->CA->C == 180 degrees, the angle
        // N->CA->C == 116 degrees, and the C->N distance is 1.322.
        // This is done as follows:
        // 1) Transform r2 s.t. r2:N->CA points in the same direction as r1:CA->C
        // 2) Offset r2 so r2:N is in the right spot. Determine this spot as a rotation of the
        //    r1:C->O unit vector about the R1:CA->C->O normal vector s.t. the angle formed
        //    between CA->C->N will be 116 degrees. Scale this to the peptide bond length.
        // 3) Rotate the 
        public void OrientPlanar(IAa r1, IAa move)
        {
            Vector3 v1C = r1[C_].Xyz;
            Vector3 v1CA = r1[CA_].Xyz;
            Vector3 v1O = r1[O_].Xyz;
            Vector3 v2N = move[N_].Xyz;
            Vector3 v2CA = move[CA_].Xyz;

            // 1) Orient r2:N->CA to the same direction as r1:CA->C
            Matrix matrix = VectorMath.GetRotationMatrix((v2CA - v2N), (v1C - v1CA));
            foreach (IAtom atom in move)
            {
                atom.Xyz = Vector3.Transform(atom.Xyz, matrix);
            }

            // 2) Position r2:N s.t. the angle CA->C->N == 116 and it lies in the same plane as r1:CA,C,O
            // and the C->N distance is 1.322 Angstroms.
            v2N = move[N_].Xyz;
            v2CA = move[CA_].Xyz;
            Vector3 normal = Vector3.Cross(v1O - v1C, v1CA - v1C); normal.Normalize();
            Vector3 uvCCA = v1CA - v1C; uvCCA.Normalize();
            Quaternion rotation = Quaternion.CreateFromAxisAngle(normal /* this expects a unit vector */, (float)(116.0 / 180 * Math.PI));
            Vector3 vCN = Vector3.Transform(uvCCA, rotation) * 1.322f;
            Vector3 offset = v1C - v2N + vCN;
            foreach (Atom atom in move)
            {
                atom.Xyz += offset;
            }

            // Set C1-N2-CA2-C2 torsion angle to 180 degrees, or 0 for proline
            double rotateRadians = 0;
            if (move.Letter == 'P')
            {
                rotateRadians = Math.PI - VectorMath.GetDihedralAngleRadians(r1[C_].Xyz, move[N_].Xyz, move[CA_].Xyz, move["CD"].Xyz);
            }
            else
            {
                rotateRadians = Math.PI - VectorMath.GetDihedralAngleRadians(r1[C_].Xyz, move[N_].Xyz, move[CA_].Xyz, move[C_].Xyz);
            }

            matrix = Geometry.GetAxisRotationRadians(move[N_].Xyz, move[CA_].Xyz, rotateRadians);
            this.Transform(matrix);
        }

        /// <summary>
        /// Finds the transform that will rotate residue2 (whose N must be centered at zero)
        /// such that it is properly oriented with respect to residue 1
        /// </summary>
        /// <param name="residue1"></param>
        /// <param name="residue2"></param>
        public Matrix GetOrientationTransformAboutR2N(IAa r1, IAa r2)
        {
            Vector3 v2N = r2[N_].Xyz;
            Vector3 v1C = r1[C_].Xyz;
            Vector3 v1CA = r1[CA_].Xyz;
            Vector3 v1O = r1[O_].Xyz;
            Vector3 v2CA = r2[CA_].Xyz;
            Vector3 additionalOffset = v1C;

            Debug.Assert(v2N == Vector3.Zero);
            //if (v2N != Vector3.Zero)
            //{
            //    v1C -= v2N;
            //    v1CA -= v2N;
            //    v1O -= v2N;
            //    v2CA -= v2N;
            //    v2N = Vector3.Zero;
            //}

            // 1) Orient r2:N->CA to the same direction as r1:CA->C
            // TODO: I think this is a bug. Presumably N should be placed at 116 degrees for CA->C->N
            // and the following CA should be placed at some angle for C->N->CA, within the plane of r1's
            // CA,C,N (thus 180 degree trans)
            Matrix transform = VectorMath.GetRotationMatrix(v2CA - v2N, v1C - v1CA);

            // 2) Position r2:N s.t. the angle CA->C->N == 116 and it lies in the same plane as r1:CA,C,O
            // and the C->N distance is 1.322 Angstroms.
            //v1C = Vector3.Transform(v1C, transform);
            //v1CA = Vector3.Transform(v1CA, transform);
            //v1O = Vector3.Transform(v1O, transform);
            v2N = Vector3.Transform(v2N, transform);
            v2CA = Vector3.Transform(v2CA, transform);

            Vector3 normal = Vector3.Cross(v1O - v1C, v1CA - v1C); normal.Normalize();
            Vector3 uvCCA = v1CA - v1C; uvCCA.Normalize();
            Matrix rotation = Matrix.CreateFromAxisAngle(normal /* unit vector required */, (float)(116.0 / 180 * Math.PI));
            Vector3 vCN = Vector3.Transform(uvCCA, rotation) * 1.322f;
            transform.Translation = vCN + additionalOffset;

            return transform;
        }

        public void RemoveAt(int index, bool reposition)
        {
            if (index < 0 || Count <= index)
                throw new IndexOutOfRangeException();

            if(!reposition)
            {
                _aas.RemoveAt(index);
                return;
            }

            IAa residue = this[index];
            bool nTerminus = index == 0;
            bool cTerminus = Count == 0 || index == Count - 1;    //              Debug.Assert(residue.IsNTerminus == nTerminus && residue.IsCTerminus == cTerminus);

            double phi = 0;
            double psi = 0;
            if (!nTerminus)
                psi = GetPsiRadians(index - 1);
            if (!cTerminus)
                phi = GetPhiRadians(index + 1);

            // Position residues after 
            if (!nTerminus && !cTerminus)
            {
                IAa previous = this[index - 1];
                IAa next = this[index + 1];
                Vector3 N = next[N_].Xyz;

                for (int otherIndex = index + 1; otherIndex < Count; otherIndex++)
                {
                    IAa other = this[otherIndex];
                    other.Translate(-N);
                }

                Matrix transform = GetOrientationTransformAboutR2N(previous, next);

                for (int otherIndex = index + 1; otherIndex < Count; otherIndex++)
                {
                    IAa other = this[otherIndex];
                    //other.Translate(-N);
                    other.Transform(transform);
                }
            }

            _aas.RemoveAt(index);

            // Maintain the phi and psi angles of adjacent residues
            if (1 < Count && !nTerminus)
            {
                SetPsiRadians(index - 1, psi);
                SetOmegaDegrees(index - 1, 180);
            }
            if (1 < Count && !cTerminus)
            {
                SetPhiRadians(index, phi);
            }            
        }

        public bool SetPhiDegrees(int index, double degrees)
        {
            return SetPhiRadians(index, degrees * Math.PI / 180);
        }

        public bool SetPhiRadians(int index, double phi)
        {
            if (index < 0)
                throw new ArgumentException();

            if (index == 0)
                return false;

            IAa current = this[index];
            IAa previous = this[index - 1];
            if (previous == null)
                return false;

            Vector3 N = current[N_].Xyz;
            Vector3 CA = current[CA_].Xyz;
            Vector3 axis = CA - N; axis.Normalize();

            double radians = phi - GetPhiRadians(index);// VectorMath.GetDihedralAngleRadians(previous["C"].XYZ, current["N"].XYZ, current["CA"].XYZ, current["C"].XYZ);
            Matrix matrix = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(axis /* this expects a unit vector */, (float)radians));
            foreach (Atom atom in this[index])
            {
                if (atom.Name.Equals("N") || atom.Name.Equals("H") || atom.Name.Equals("H1") || atom.Name.Equals("H2") || atom.Name.Equals("H3"))
                    continue;
                atom.Xyz -= N;
                atom.Xyz = Vector3.Transform(atom.Xyz, matrix);
                atom.Xyz += N;
            }

            for (int i = index + 1; i < this.Count; i++)
            {
                foreach (Atom atom in this[i])
                {
                    atom.Xyz -= N;
                    atom.Xyz = Vector3.Transform(atom.Xyz, matrix);
                    atom.Xyz += N;
                }
            }
            return true;
        }

        public bool SetPsiDegrees(int index, double degrees)
        {
            return SetPsiRadians(index, degrees * Math.PI / 180);
        }

        public bool SetPsiRadians(int index, double psi)
        {
            //throw new NotImplementedException();
            if (index < 0 || _aas.Count <= index)
                throw new ArgumentException();

            if (_aas.Count - 1 == index)
                return false;

            IAa current = this[index];
            IAa next = this[index + 1];
            if (next == null)
                return false;

            Vector3 CA = current[CA_].Xyz;
            Vector3 C = current[C_].Xyz;
            Vector3 axis = C - CA; axis.Normalize();

            double radians = psi - GetPsiRadians(index);// VectorMath.GetDihedralAngleRadians(current[N_].XYZ, current[CA_].XYZ, current[C_].XYZ, next[N_].XYZ);
            Matrix matrix = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(axis /* this expects a unit vector */, (float)radians));

            IAtom O = current[O_];
            O.Xyz -= CA;
            O.Xyz = Vector3.Transform(O.Xyz, matrix);
            O.Xyz += CA;

            for (int i = index + 1; i < this.Count; i++)
            {
                foreach (Atom atom in this[i])
                {
                    atom.Xyz -= CA;
                    atom.Xyz = Vector3.Transform(atom.Xyz, matrix);
                    atom.Xyz += CA;
                }
            }
            return true;
        }

        public bool SetOmegaDegrees(int index, double degrees)
        {
            return SetOmegaRadians(index, degrees * Math.PI / 180);
        }

        public bool SetOmegaRadians(int index, double omega)
        {
            if (index < 0 || this.Count <= index)
                throw new ArgumentException();

            if (this.Count - 1 == index)
                return false;

            double radians = omega - GetOmegaRadians(index);// VectorMath.GetDihedralAngleRadians(current[N_].XYZ, current[CA_].XYZ, current[C_].XYZ, next[N_].XYZ);
            IAa current = this[index];
            IAa next = this[index + 1];
            Vector3 C = current[C_].Xyz;
            Vector3 N = next[N_].Xyz;
            Vector3 axis = N - C; axis.Normalize();
            Matrix matrix = Matrix.CreateFromAxisAngle(axis /* this expects a unit vector */, (float)radians);

            for (int i = index + 1; i < this.Count; i++)
            {
                foreach (IAtom atom in this[i])
                {
                    atom.Xyz -= C;
                    atom.Xyz = Vector3.Transform(atom.Xyz, matrix);
                    atom.Xyz += C;
                }
            }
            return true;
        }

        public double GetPhiDegrees(int index)
        {
            double degrees = GetPhiRadians(index) * 180 / Math.PI;
            return degrees;
        }

        public double GetPhiRadians(int index)
        {
            if (index < 1 || index >= this.Count)
                throw new Exception("Invalid index.");

            IAa current = this[index];
            IAa previous = this[index - 1];
            double radians = VectorMath.GetDihedralAngleRadians(previous[C_].Xyz, current[N_].Xyz, current[CA_].Xyz, current[C_].Xyz);
            return radians;
        }

        public double GetPsiDegrees(int index)
        {
            double degrees = GetPsiRadians(index) * 180 / Math.PI;
            return degrees;
        }

        public double GetPsiRadians(int index)
        {
            IAa current = this[index];
            IAa next = this[index + 1];
            double radians = VectorMath.GetDihedralAngleRadians(current[N_].Xyz, current[CA_].Xyz, current[C_].Xyz, next[N_].Xyz);
            return radians;
        }

        public double GetOmegaDegrees(int index)
        {
            double degrees = GetOmegaRadians(index) * 180 / Math.PI;
            return degrees;
        }

        public double GetOmegaRadians(int index)
        {
            IAa current = this[index];
            IAa next = this[index + 1];           
            double radians = VectorMath.GetDihedralAngleRadians(current[CA_].Xyz, current[C_].Xyz, next[N_].Xyz, next[CA_].Xyz);
            return radians;
        }

        public Vector3 HeavyXYZ
        {
            get
            {
                int count = 0;
                Vector3 sum = Vector3.Zero;
                foreach(Vector3 xyz in this.SelectMany(aa => aa.Where(atom => atom.IsHeavy).Select(atom => atom.Xyz)))
                {
                    sum += xyz;
                    count++;
                }

                if(count > 0)
                    sum /= count;

                return sum;
            }
        }

        public ITransformNode Parent { get => _aas.Parent; set => _aas.Parent = value; }

        public int Count => _aas.Count;
        public bool IsMirror => false;
        public bool IsReadOnly => _aas.IsReadOnly;

        public Matrix NodeTransform { get => _aas.NodeTransform; set => _aas.NodeTransform = value; }

        public Matrix TotalParentTransform => _aas.TotalParentTransform;

        public Matrix TotalTransform => _aas.TotalTransform;

        public Vector3 UnitX => _aas.UnitX;

        public Vector3 UnitY => _aas.UnitY;

        public Vector3 UnitZ => _aas.UnitZ;

        public Vector3 Origin => _aas.Origin;

        public void RotateRadians(Vector3 origin, Vector3 axis, double radians)
        {

            Matrix transform = Matrix.CreateFromAxisAngle(axis, (float)radians);
            Transform(transform);
        }

        public void AddMonitor(IArraySourceMonitor<IAa> monitor)
        {
            _aas.AddMonitor(monitor);
        }

        public void RemoveMonitor(IArraySourceMonitor<IAa> monitor)
        {
            _aas.RemoveMonitor(monitor);
        }

        public void AddRange(IEnumerable<IAa> items)
        {
            _aas.AddRange(items);
        }

        public void RotateNode(Quaternion rotation, Vector3 origin)
        {
            _aas.RotateNode(rotation, origin);
        }

        public void TransformNode(Matrix transform)
        {
            _aas.TransformNode(transform);
        }

        public void TranslateNode(Vector3 translation)
        {
            _aas.TranslateNode(translation);
        }

        public void Rotate(Quaternion rotation, Vector3 origin)
        {
            _aas.Rotate(rotation, origin);
        }

        public int IndexOf(IAa item)
        {
            return _aas.IndexOf(item);
        }

        public void Insert(int index, IAa item)
        {
            _aas.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _aas.RemoveAt(index);
        }

        public void Clear()
        {
            _aas.Clear();
        }

        public bool Contains(IAa item)
        {
            return _aas.Contains(item);
        }

        public void CopyTo(IAa[] array, int arrayIndex)
        {
            _aas.CopyTo(array, arrayIndex);
        }

        public bool Remove(IAa item)
        {
            return _aas.Remove(item);
        }

        public IEnumerator<IAa> GetEnumerator()
        {
            return _aas.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Transform(Matrix transform)
        {
            _aas.Transform(transform);
        }

        public void Translate(Vector3 translation)
        {
            _aas.Translate(translation);
        }

        public void RemoveRange(int start, int count)
        {
            _aas.RemoveRange(start, count);
        }

        public void GetCoordinateSystem(out Vector3 origin, out Vector3 unitX, out Vector3 unitY, out Vector3 unitZ)
        {
            _aas.GetCoordinateSystem(out origin, out unitX, out unitY, out unitZ);
        }

        public void AddArraySource(IArraySource<IAa> source)
        {
            _aas.AddArraySource(source);
        }

        public void RemoveArraySource(IArraySource<IAa> source)
        {
            _aas.RemoveArraySource(source);
        }

        public IChain GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorChain(this, root, parent);
        }

        public IChain GetMirrorTemplate()
        {
            return this;
        }

        public object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(graph);
        }

        public object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            Chain chain = new Chain();
            graph.Add(this, chain);
            DeepCopyPopulateFields(graph, chain);
            return chain;
        }

        public void DeepCopyPopulateFields(IDeepCloneObjectGraph context, object clone)
        {
            Chain chain = (Chain)clone;
            chain._aas = (NodeArraySource<IAa>)_aas.DeepCopyFindOrCreate(context);
        }

        public void InsertArraySourceInPlace(int index, IArraySource<IAa> source)
        {
            _aas.InsertArraySourceInPlace(index, source);
        }

        public void InsertArraySource(int index, IArraySource<IAa> source)
        {
            _aas.InsertArraySource(index, source);
        }

        public void DisconnectDependent(object dependent)
        {
            _aas.DisconnectDependent(dependent);
        }
    }
}
