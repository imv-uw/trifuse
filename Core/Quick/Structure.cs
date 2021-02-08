using Core.Collections;
using Core.Interfaces;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Core.Quick.Pattern;
using Core.Quick;
using Newtonsoft.Json;

namespace Core
{
    /*
    
    Thoughts on cloning new structures from existing ones
    
    new is Global and other is either Global or Transform
	    copy coordinates
	    Structure/Chain/Aa matrix = identity
	    Atom.xyz = other.xyz

    new is Transform and other is Global
	    Structure/Chain: Identity
	    AA: align NodeTransform by NCAC matrix and then set XYZ to match
	    Atom: Set by AA
	
    new is Transform and other is Transform
	    Structure/Chain: other.Transform
	    Aa: other.Transform
	    Atom: _xyz cloned directly / no setting of 
    */

    [JsonObject(MemberSerialization.OptIn)]
    public class Structure : INodeArraySource<IChain>, IStructure, IDeepCopy
    {
        [JsonProperty] NodeArraySource<IChain> _chains;

        [JsonConstructor]
        public Structure()
        {
            _chains = new NodeArraySource<IChain>(this);
        }

        public Structure(IArraySource<IChain> other)
        {
            _chains = new NodeArraySource<IChain>(this);
            _chains.AddArraySource(other);
        }

        public Structure(IChain chain)
        {
            _chains = new NodeArraySource<IChain>(this, chain);
        }

        public Structure(IEnumerable<IChain> chains)
        {
            _chains = new NodeArraySource<IChain>(this, chains);
        }

        public IChain this[int index] { get => _chains[index]; set => _chains[index] = value; }

        public IList<IChain> this[int start, int end] => _chains[start, end];

        public IChain this[int index, bool placed] { set => _chains[index, placed] = value; }

        public ITransformNode Parent { get => _chains.Parent; set => _chains.Parent = value; }

        public int Count => _chains.Count;

        public float Energy { get; set; }

        public Vector3 Force { get; set; }

        public bool IsMirror => false;

        public bool IsReadOnly => _chains.IsReadOnly;

        public Matrix NodeTransform { get => _chains.NodeTransform; set => _chains.NodeTransform = value; }

        public Matrix TotalParentTransform => _chains.TotalParentTransform;

        public Matrix TotalTransform => _chains.TotalTransform;

        public Vector3 UnitX => _chains.UnitX;

        public Vector3 UnitY => _chains.UnitY;

        public Vector3 UnitZ => _chains.UnitZ;

        public Vector3 Origin
        {
            get
            {
                Vector3[] aaOrigins /* CA atom */ = _chains.SelectMany(chain => chain.Select(aa => aa.Origin)).ToArray();
                if (aaOrigins.Length == 0)
                    return Vector3.Zero;

                Vector3 com = aaOrigins.Aggregate(Vector3.Zero, (a, b) => a + b);
                com /= aaOrigins.Length;
                return com;
            }
        }

        public void Add(IChain item)
        {
            _chains.Add(item);
        }

        public void AddArraySource(IArraySource<IChain> source)
        {
            _chains.AddArraySource(source);
        }

        public void AddArraySourceInPlace(IArraySource<IChain> source)
        {
            _chains.AddArraySourceInPlace(source);
        }

        public void AddInPlace(IChain item)
        {
            _chains.AddInPlace(item);
        }

        public void AddMonitor(IArraySourceMonitor<IChain> monitor)
        {
            _chains.AddMonitor(monitor);
        }

        public void AddRange(IEnumerable<IChain> items)
        {
            _chains.AddRange(items);
        }

        public void AddRangeInPlace(IEnumerable<IChain> items)
        {
            _chains.AddRangeInPlace(items);
        }

        public void Clear()
        {
            _chains.Clear();
        }

        public bool Contains(IChain item)
        {
            return _chains.Contains(item);
        }

        public void CopyTo(IChain[] array, int arrayIndex)
        {
            _chains.CopyTo(array, arrayIndex);
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

            Structure structure = new Structure();
            graph.Add(this, structure);
            DeepCopyPopulateFields(graph, structure);
            return structure;
        }

        public void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            Structure structure = (Structure)clone;
            structure._chains = (NodeArraySource<IChain>) _chains.DeepCopyFindOrCreate(graph);
        }

        public void DisconnectDependent(object dependent)
        {
            _chains.DisconnectDependent(dependent);
        }

        public void GetCoordinateSystem(out Vector3 origin, out Vector3 unitX, out Vector3 unitY, out Vector3 unitZ)
        {
            _chains.GetCoordinateSystem(out origin, out unitX, out unitY, out unitZ);
        }

        public IEnumerator<IChain> GetEnumerator()
        {
            return _chains.GetEnumerator();
        }

        public IStructure GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorStructure(this, root, parent);
        }

        public IStructure GetMirrorTemplate()
        {
            return this;
        }

        public int IndexOf(IChain item)
        {
            return _chains.IndexOf(item);
        }

        public void Insert(int index, IChain item)
        {
            _chains.Insert(index, item);
        }

        public void InsertArraySource(int index, IArraySource<IChain> source)
        {
            _chains.InsertArraySource(index, source);
        }

        public void InsertArraySourceInPlace(int index, IArraySource<IChain> source)
        {
            _chains.InsertArraySourceInPlace(index, source);
        }

        public bool Remove(IChain item)
        {
            return _chains.Remove(item);
        }

        public void RemoveArraySource(IArraySource<IChain> source)
        {
            _chains.RemoveArraySource(source);
        }

        public void RemoveAt(int index)
        {
            _chains.RemoveAt(index);
        }

        public void RemoveMonitor(IArraySourceMonitor<IChain> monitor)
        {
            _chains.RemoveMonitor(monitor);
        }

        public void RemoveRange(int start, int count)
        {
            _chains.RemoveRange(start, count);
        }

        public void Rotate(Quaternion rotation, Vector3 origin)
        {
            _chains.Rotate(rotation, origin);
        }

        public void RotateNode(Quaternion rotation, Vector3 origin)
        {
            _chains.RotateNode(rotation, origin);
        }

        public void Transform(Matrix transform)
        {
            _chains.Transform(transform);
        }

        public void TransformNode(Matrix transform)
        {
            _chains.TransformNode(transform);
        }

        public void Translate(Vector3 translation)
        {
            _chains.Translate(translation);
        }

        public void TranslateNode(Vector3 translation)
        {
            _chains.TranslateNode(translation);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
