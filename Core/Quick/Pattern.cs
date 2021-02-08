using Core.Collections;
using Core.Interfaces;
using Core.Quick.Pattern;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;
using Core.Symmetry;
using Newtonsoft.Json;

namespace Core.Quick
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Pattern<T> : IArraySource<T> where T : class, IMirror<T>, ITransformable, ITransformNode, IDeepCopy
    {
        [JsonProperty] int _patternCount;
        
        [JsonProperty] ArraySource<T>[] _transformed;
        [JsonProperty] MirrorArraySource<T>[] _copies;
        [JsonProperty] IArraySource<T> _out;
        [JsonProperty] IArraySource<T> _in;

        public Pattern()
        {
            _in = GetArraySourceInput();
            _out = GetArraySourceExit();
        }

        public Pattern(SymmetryBuilder symmetry)
        {
            SetSymmetry(symmetry);
        }

        public virtual IArraySource<T> GetArraySourceInput()
        {
            return _in ?? new ArraySource<T>();
        }

        public virtual IArraySource<T> GetArraySourceExit()
        {
            return _out ?? new ArraySource<T>(this);
        }

        CoordinateSystem[] _coordinateSystems;
        public CoordinateSystem[] CoordinateSystems
        {
            get
            {
                return _coordinateSystems;
            }
            protected set
            {
                if(_coordinateSystems != null)
                {
                    // Disconnect existing pattern-flow
                    for (int i = 0; i < _patternCount; i++)
                    {
                        _transformed[i].RemoveArraySource(_copies[i]);
                        _out.RemoveArraySource(_transformed[i]);
                    }

                    // Remove any existing asymmetric unit
                    Clear();
                }
                

                _coordinateSystems = value;
                _patternCount = CoordinateSystems.Length;
                _transformed = new ArraySource<T>[_patternCount];
                _copies = new MirrorArraySource<T>[_patternCount];

                for (int i = 0; i < _patternCount; i++)
                {
                    //_inversions[i] = Matrix.Invert(_coordinateSystems[i].Transform);
                    _transformed[i] = new ArraySource<T>(null);
                    _copies[i] = new MirrorArraySource<T>(_in, true, true, null); // This thing 
                    _transformed[i].AddArraySource(_copies[i]);
                    _transformed[i].Transform(_coordinateSystems[i].Transform);
                    _out.AddArraySource(_transformed[i]);
                }
            }
        }

        public void SetSymmetry(SymmetryBuilder symmetry)
        {
            Trace.Assert(symmetry.EnabledUnits != null && symmetry.EnabledUnits.Length > 0);            

            // Replace intermediate pattern-flow stages, but neither _in or _out, so as to maintain
            // connectivity with other objects
            CoordinateSystems = symmetry.EnabledUnits.SelectMany(unitId => symmetry.GetCoordinateSystems(unitId)).ToArray();
        }

        public virtual CoordinateSystem[] GetCoordinateSystems()
        {
            return CoordinateSystems;
        }

        IArraySource<T> PatternArraySource { get { return _in; } }

        public T this[int index]
        {
            get
            {
                return _out[index];
            }
            set
            {
                int patternIndex = index / _in.Count;
                int sourceIndex = index % _in.Count;

                _in[sourceIndex] = value;
            }
        }

        public IList<T> this[int start, int end] => _out[start, end];

        public bool IsMirror => false;

        public Matrix NodeTransform { get => _out.NodeTransform; set => _out.NodeTransform = value; }

        public Matrix TotalParentTransform => _out.TotalParentTransform;

        public Matrix TotalTransform => _out.TotalTransform;

        public ITransformNode Parent { get => _out.Parent; set => _out.Parent = value; }

        public Vector3 UnitX => _out.UnitX;

        public Vector3 UnitY => _out.UnitY;

        public Vector3 UnitZ => _out.UnitZ;

        public Vector3 Origin => _out.Origin;

        public int Count => _out.Count;

        public bool IsReadOnly => _out.IsReadOnly;

        public IArraySource<T> AsymmetricUnit
        {
            get { return _in; }
            set
            {
                Trace.Assert(value != null);
                _in.Clear();
                _in.AddArraySourceInPlace(value);
            }
        }

        public IArraySource<T> PlacedAsymmetricUnit
        {
            get { return _transformed[0]; }
            set { new NotImplementedException(); }
        }

        public float Energy { get; set; }
        public Vector3 Force { get; set; }
        
        public void Add(T item)
        {
            _in.Add(item);
        }

        public void AddArraySource(IArraySource<T> source)
        {
            _in.AddArraySource(source);
        }

        public void AddMonitor(IArraySourceMonitor<T> monitor)
        {
            _out.AddMonitor(monitor);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach(T item in items)
            {
                Add(item);
            }
        }

        public void Clear()
        {
            _in.Clear();
        }

        public bool Contains(T item)
        {
            return _out.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for(int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = _out[i];
            }
        }

        public void GetCoordinateSystem(out Vector3 origin, out Vector3 unitX, out Vector3 unitY, out Vector3 unitZ)
        {
            _out.GetCoordinateSystem(out origin, out unitX, out unitY, out unitZ);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ArraySourceEnumerator<T>(_out);
        }

        public int IndexOf(T item)
        {
            return _out.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            int patternIndex = index / Count;
            int inIndex = index % Count;

            if (index < 0 || Count <= patternIndex)
                throw new IndexOutOfRangeException();

            _in.Insert(inIndex, item);
        }

        public bool Remove(T item)
        {
            int outIndex = _out.IndexOf(item);
            if (outIndex < 0)
                return false;

            int patternIndex = outIndex / _in.Count;
            if (patternIndex >= CoordinateSystems.Length)
                throw new Exception("bug - output item count is greater than expected");

            int inIndex = outIndex % _in.Count;
            _in.RemoveAt(inIndex);
            return true;
        }

        public void RemoveArraySource(IArraySource<T> source)
        {
            _in.Transform(CoordinateSystems[0].Transform);
            _in.RemoveArraySource(source);
        }

        public void RemoveAt(int index)
        {
            int inIndex = index % _in.Count;
            _in.RemoveAt(inIndex);
        }

        public void RemoveMonitor(IArraySourceMonitor<T> monitor)
        {
            _out.RemoveMonitor(monitor);
        }

        public void RemoveRange(int start, int count)
        {
            if (start < 0 || Count <= start + count - 1)
                throw new IndexOutOfRangeException();

            int inIndexStart = start % _in.Count;
            int inIndexEnd = (start + count - 1) % _in.Count;

            if(count >= Count)
            {
                Clear();
            }
            else if (inIndexStart <= inIndexEnd)
            {
                // The removal range is fully contained in one patterned element
                _in.RemoveRange(inIndexStart, inIndexEnd - inIndexStart + 1);
            }
            else
            {
                // The removal range wraps around between two patterned elements
                _in.RemoveRange(inIndexStart, _in.Count - inIndexStart + 1);
                _in.RemoveRange(0, inIndexEnd + 1);
            }
        }

        public void Rotate(Quaternion rotation, Vector3 origin)
        {
            _out.Rotate(rotation, origin);
        }

        public void RotateNode(Quaternion rotation, Vector3 origin)
        {
            _out.RotateNode(rotation, origin);
        }

        public void Transform(Matrix transform)
        {
            _out.Transform(transform);
        }

        public void TransformNode(Matrix transform)
        {
            _out.TransformNode(transform);
        }

        public void Translate(Vector3 translation)
        {
            _out.Translate(translation);
        }

        public void TranslateNode(Vector3 translation)
        {
            _out.TranslateNode(translation);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ArraySourceEnumerator<T>(_out);
        }

        public virtual object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(graph);
        }

        public virtual object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            Pattern<T> pattern = new Pattern<T>();
            graph.Add(this, pattern);
            DeepCopyPopulateFields(graph, pattern);
            return pattern;
        }

        public virtual void DeepCopyPopulateFields(IDeepCloneObjectGraph context, object clone)
        {
            Pattern<T> pattern = (Pattern<T>)clone;
            pattern.CoordinateSystems = (CoordinateSystem[])CoordinateSystems.Clone();
            pattern._patternCount = _patternCount;
            pattern._transformed = _transformed.Select(t => t.DeepCopyFindOrCreate(context)).Cast<ArraySource<T>>().ToArray();
            pattern._copies = _copies.Select(c => c.DeepCopyFindOrCreate(context)).Cast<MirrorArraySource<T>>().ToArray();
            pattern._out = (IArraySource<T>) _out.DeepCopyFindOrCreate(context);
            pattern._in = (IArraySource<T>) _in.DeepCopyFindOrCreate(context);
        }

        public void InsertArraySource(int index, IArraySource<T> source)
        {
            _in.InsertArraySource(index, source);
        }

        public void AddArraySourceInPlace(IArraySource<T> source)
        {
            _in.AddArraySourceInPlace(source);
            throw new NotImplementedException("AddArraySource must account for the 0th element transform, not just _out.TotalTransform");
        }

        public void InsertArraySourceInPlace(int index, IArraySource<T> source)
        {
            _in.InsertArraySourceInPlace(index, source);
            throw new NotImplementedException("AddArraySource must account for the 0th element transform, not just _out.TotalTransform");
        }

        public void DisconnectDependent(object dependent)
        {
            throw new InvalidOperationException("How is this object the parent of anything? - the I/O objects should be.");
        }
    }
}
