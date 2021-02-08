#define DEBUG_MIRROR_TRANSFORM


using Core.Collections;
using Core.Interfaces;
using Core.Tools;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Core.Quick.Pattern
{
    /// <summary>
    /// MirrorArraySource<T> mirrors another array source which serves as a template. The mirror can (and usually does) apply a final transform 
    /// after any other transforms from parents of the template. The template is unaware of the mirroring and has its own hierarchy. The template
    /// might be the top-level element in its own hierarchy, or not. The mirror lazily creates wrapping elements for each T item that is being
    /// mirrored.
    /// 
    /// How should transform operations be handled? Going with option 2.
    ///   Problem:
    ///      -If they apply only to the mirror element, they never get pushed to the template and reproduced by other mirror/pattern elements.
    ///      -If they always get pushed to the template, how is the template transform separately applied?
    ///   Option 1: Add separate methods to initialize or modify the mirror matrix and then by default push operations to the template. This will
    ///             create inconsistent ways of DOF access.
    ///   Option 2: Have mirror *just mirror* and have no independent transforms on its own. All operations are pushed to the template. Pattern<T>
    ///             will have to manage an additional parent layer to the mirror, which can apply the transform.
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [JsonObject(MemberSerialization.OptIn)]
    public class MirrorArraySource<T> : IArraySource<T>, IArraySourceMonitor<T>, IDeepCopy, ITransformNode where T : class, IMirror<T>, ITransformable, IDeepCopy
    {
        [JsonProperty] List<IArraySourceMonitor<T>> _monitors = new List<IArraySourceMonitor<T>>();
        [JsonProperty] Dictionary<T, T> _lookupTemplateToMirror = new Dictionary<T, T>(); // template to mirror element lookup
        [JsonProperty] Dictionary<T, T> _lookupMirrorToTemplate = new Dictionary<T, T>(); // template to mirror element lookup
        [JsonProperty] IArraySource<T> _template;

        ITransformNode _parent;
        [JsonProperty] bool _root;
        bool _childIsRoot;
        //protected Matrix MirrorInverseTransform { get => Matrix.Invert(Root.TotalTransform); }

        [JsonConstructor]
        protected MirrorArraySource() { }

        public MirrorArraySource(IArraySource<T> template)
        {
            _template = template;
            _template.AddMonitor(this);
            _root = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <param name="root"></param>
        /// <param name="childIsRoot">
        /// Whether child elements should be constructed as the root mirror element, so that they
        /// incorporate the template's parent transform into their own transform
        /// </param>
        /// <param name="parent"></param>
        public MirrorArraySource(IArraySource<T> template, bool root, bool childIsRoot, ITransformNode parent)
        {
            _template = template;
            _template.AddMonitor(this);
            _parent = parent;
            _root = root;
            _childIsRoot = childIsRoot;
        }

        //~MirrorArraySource()
        //{
        //    _template.RemoveMonitor(this);
        //}

        public T this[int index]
        {
            get
            {
                T templateItem = _template[index];
                if (!_lookupTemplateToMirror.TryGetValue(templateItem, out T mirrorItem))
                {
                    mirrorItem = templateItem.GetMirroredElement(_childIsRoot, this);
                    _lookupTemplateToMirror.Add(templateItem, mirrorItem);
                    _lookupMirrorToTemplate.Add(mirrorItem, templateItem);
                }
                Debug.Assert(mirrorItem != null && templateItem != null);
                return mirrorItem;
            }
            set
            {
                Debug.Assert(value != null);
                //Matrix inversion = Matrix.Invert(NodeTransform);
                //value.Transform(inversion);
                _template[index] = value;
            }
        }

        public IList<T> this[int start, int end]
        {
            get
            {
                int count = end - start + 1;
                T[] result = new T[count];
                for(int i = 0; i < count; i++)
                {
                    result[i] = this[start + i];
                }
                return result;
            }
        }

        public bool IsMirror => true;

        public virtual Matrix NodeTransform
        {
            get
            {
                return _root ? _template.TotalTransform : TotalTransform * Matrix.Invert(TotalParentTransform);

                //return _root ? _template.TotalTransform : 
                //    // This is wrong - should really be TotalTransform * Matrix.Inverse(TotalParentTransform)
                //    // or along the lines of: 
                //    // do { nodetransform *= _template.NodeTransform; _template = _template.Parnet } while(parent._template above _template.Parent)
                //    _template.TotalTransform * Matrix.Invert(TotalParentTransform);
            }
            set
            {
                if(_root)
                {
                    _template.NodeTransform = value * Matrix.Invert(_template.TotalParentTransform);
                }
                else
                {
                    _template.NodeTransform = value;
                }
            }
        }

        public virtual Matrix TotalParentTransform
        {
            get
            {
                return Parent == null ? Matrix.Identity : Parent.TotalTransform;
            }
        }

        public virtual Matrix TotalTransform
        {
            get
            {
                if (_root)
                    return _template.TotalTransform * TotalParentTransform;

                ITransformNode mirrorRoot = this;
                while (mirrorRoot.Parent != null && mirrorRoot.Parent.IsMirror)
                    mirrorRoot = mirrorRoot.Parent;

                return _template.TotalTransform * mirrorRoot.TotalParentTransform;

                //return _root ?
                //    // as is, _template.TotalTransform * TotalParentTransform
                //    _template.TotalTransform * TotalParentTransform : 
                //    // _template.TotalTransform * mirror_root.TotalParentTransform
                //    _template.NodeTransform * TotalParentTransform;
            }
        }

        public virtual ITransformNode Parent
        {
            get => _parent;
            set
            {
                if (_parent != null && _parent != value)
                    _parent.DisconnectDependent(this);

                _parent = value;
                _root = true;
            }
        }

        public virtual Vector3 UnitX => Vector3.Transform(_template.UnitX, (Matrix.Invert(_template.TotalTransform) * TotalTransform).Rotation);

        public virtual Vector3 UnitY => Vector3.Transform(_template.UnitY, (Matrix.Invert(_template.TotalTransform) * TotalTransform).Rotation);

        public virtual Vector3 UnitZ => Vector3.Transform(_template.UnitZ, (Matrix.Invert(_template.TotalTransform) * TotalTransform).Rotation);

        public virtual Vector3 Origin => Vector3.Transform(_template.Origin, (Matrix.Invert(_template.TotalTransform) * TotalTransform));

        public virtual int Count => _template.Count;

        public virtual bool IsReadOnly => _template.IsReadOnly;

        //public T this[int index, bool placed] { set => _template[index, placed] = value; }

        public void Add(T item)
        {
            //item.Transform(MirrorInverseTransform);
            _template.Add(item);
        }

        public void AddArraySource(IArraySource<T> source)
        {
            //source.Transform(MirrorInverseTransform);
            _template.AddArraySource(source);
        }

        public void AddMonitor(IArraySourceMonitor<T> monitor)
        {
            _monitors.Add(monitor);
        }

        public void AddRange(IEnumerable<T> items)
        {
            //foreach(T item in items)
            //{
            //    item.Transform(MirrorInverseTransform);
            //}
            _template.AddRange(items);
        }

        public void Clear()
        {
            _template.Clear();
        }

        public bool Contains(T item)
        {
            return _lookupMirrorToTemplate.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for(int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public void DisconnectDependent(object dependent)
        {
            _template.DisconnectDependent(_lookupMirrorToTemplate[(T)dependent]);
        }

        public void GetCoordinateSystem(out Vector3 origin, out Vector3 unitX, out Vector3 unitY, out Vector3 unitZ)
        {
            _template.GetCoordinateSystem(out Vector3 templateOrigin, out Vector3 templateX, out Vector3 templateY, out Vector3 templateZ);
            Matrix mirrorTransform = (Matrix.Invert(_template.TotalTransform) * TotalTransform);
            origin = Vector3.Transform(templateOrigin, mirrorTransform);
            unitX = Vector3.Transform(templateX, mirrorTransform);
            unitY = Vector3.Transform(templateY, mirrorTransform);
            unitZ = Vector3.Transform(templateZ, mirrorTransform);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ArraySourceEnumerator<T>(this);
        }

        public int IndexOf(T item)
        {
            if (!_lookupMirrorToTemplate.ContainsKey(item))
                return -1;
            return _template.IndexOf(_lookupMirrorToTemplate[item]);
        }

        public void Insert(int index, T item)
        {
            //item.Transform(MirrorInverseTransform);
            _template.Insert(index, item);
        }

        public void NotifyAdd(IArraySource<T> source, int position, T added)
        {
            Trace.Assert(source == _template);
            if (_monitors.Count > 0)
            {
                T mirror = added.GetMirroredElement(_childIsRoot, this);
                _lookupTemplateToMirror.Add(added, mirror);
                _lookupMirrorToTemplate.Add(mirror, added);
                Debug.Assert(mirror != null && added != null);
                foreach (IArraySourceMonitor<T> monitor in _monitors)
                {
                    monitor.NotifyAdd(this, position, mirror);
                }
            }
        }

        public void NotifyAddRange(IArraySource<T> source, int position, T[] added)
        {
            Trace.Assert(source == _template);
            if (_monitors.Count > 0)
            {
                T[] mirrors = new T[added.Length];
                for (int i = 0; i < added.Length; i++)
                {
                    mirrors[i] = added[i].GetMirroredElement(_childIsRoot, this);
                    _lookupTemplateToMirror.Add(added[i], mirrors[i]);
                    _lookupMirrorToTemplate.Add(mirrors[i], added[i]);
                    Debug.Assert(mirrors[i] != null && added[i] != null);
                }
                foreach (IArraySourceMonitor<T> monitor in _monitors)
                {
                    monitor.NotifyAddRange(this, position, mirrors);
                }
            }
        }

        public void NotifyRemove(IArraySource<T> source, int position, T removed)
        {
            // eventually want to recycle into mirror pool?
            if(_monitors.Count > 0)
            {
                if (_lookupTemplateToMirror.TryGetValue(removed, out T mirror))
                {
                    _lookupTemplateToMirror.Remove(removed);
                    _lookupMirrorToTemplate.Remove(mirror);
                }
                foreach(IArraySourceMonitor<T> monitor in _monitors)
                {
                    // mirro might be null, but presumably if this is the case, the monitor
                    // could not have a reference to it, so it's ok to pass up null?
                    monitor.NotifyRemove(this, position, mirror);
                }
            }
        }

        public void NotifyRemoveRange(IArraySource<T> source, int position, T[] removed)
        {
            // eventually want to recycle into mirror pool?
            if (_monitors.Count > 0)
            {
                T[] mirrors = new T[removed.Length];
                for(int i = 0; i < removed.Length; i++)
                {
                    if (_lookupTemplateToMirror.TryGetValue(removed[i], out T mirror))
                    {
                        _lookupTemplateToMirror.Remove(removed[i]);
                        _lookupMirrorToTemplate.Remove(mirror);
                        mirrors[i] = mirror;
                        Debug.Assert(mirror != null);
                    }
                }
                
                foreach (IArraySourceMonitor<T> monitor in _monitors)
                {
                    // Mirror elements might be null, but presumably if this is the case, the monitor
                    // could not have a reference to it, so it's ok to pass up null?
                    monitor.NotifyRemoveRange(this, position, mirrors);
                }
            }
        }

        public void NotifyReplace(IArraySource<T> source, int position, T removed, T added)
        {
            if (_monitors.Count > 0)
            {
                if (_lookupTemplateToMirror.TryGetValue(removed, out T mirrorRemoved))
                {
                    _lookupTemplateToMirror.Remove(removed);
                    _lookupMirrorToTemplate.Remove(mirrorRemoved);

                }
                if (!_lookupTemplateToMirror.TryGetValue(added, out T mirrorAdded))
                {
                    mirrorAdded = added.GetMirroredElement(_childIsRoot, this);
                    _lookupTemplateToMirror.Add(added, mirrorAdded);
                    _lookupMirrorToTemplate.Add(mirrorAdded, added);
                    Debug.Assert(mirrorAdded != null && added != null);
                }
            }
        }

        public bool Remove(T item)
        {
            if (!_lookupMirrorToTemplate.TryGetValue(item, out T templateItem))
                return false;
            return _template.Remove(templateItem);
        }

        public void RemoveArraySource(IArraySource<T> source)
        {
            _template.RemoveArraySource(source);
        }

        public void RemoveAt(int index)
        {
            _template.RemoveAt(index);
        }

        public void RemoveMonitor(IArraySourceMonitor<T> monitor)
        {
            _monitors.Remove(monitor);
        }

        public void RemoveRange(int start, int count)
        {
            _template.RemoveRange(start, count);
        }

        public void Rotate(Quaternion rotation, Vector3 origin)
        {
            Matrix transform = MatrixUtil.GetRotation(ref rotation, ref origin);
            Transform(transform);
        }

        public void RotateNode(Quaternion rotation, Vector3 origin)
        {
            Matrix transform = MatrixUtil.GetRotation(ref rotation, ref origin);
            TransformNode(transform);
        }

        public void Transform(Matrix transform)
        {
            // The transform hierarchy for node is 
            // 1) node transform
            // 2) parent total transform
            // 3) root total transform
            // And a modified transform 1' must be found that will be placed immediately following
            // the node transform, such that 1*1'*2*3 == 1*2*3*transform, or equivalently 1'==2*3*transform*INV(2*3)
            // A more intuitive rationalization is that the total transform must be what already exists, plus the desired
            // transform, after adding back the parent/root transforms, which accounts for the inverse.

#if DEBUG_MIRROR_TRANSFORM
            Matrix expectedFinalTransform = TotalTransform * transform;
#endif

            Matrix mirrorTransform = Matrix.Invert(_template.TotalTransform) * TotalTransform;
            Matrix templateTransform = mirrorTransform * transform * Matrix.Invert(mirrorTransform);
            _template.Transform(templateTransform);

#if DEBUG_MIRROR_TRANSFORM
            Debug.Assert((TotalTransform - expectedFinalTransform).Translation.Length() < 0.1);
#endif
        }

        public void TransformNode(Matrix transform)
        {
            _template.TransformNode(transform);
        }

        public void Translate(Vector3 translation)
        {
            Matrix transform = Matrix.Identity;
            transform.Translation += translation;
            Transform(transform);
        }

        public void TranslateNode(Vector3 translation)
        {
            Matrix transform = Matrix.Identity;
            transform.Translation += translation;
            _template.TransformNode(transform);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ArraySourceEnumerator<T>(this);
        }

        public virtual object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(graph);
        }

        public virtual object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return (MirrorArraySource<T>)clone;

            MirrorArraySource<T> mirrorArraySource = new MirrorArraySource<T>();
            graph.Add(this, mirrorArraySource);
            DeepCopyPopulateFields(graph, mirrorArraySource);
            return mirrorArraySource;
        }

        public virtual void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            MirrorArraySource<T> mirrorArraySource = (MirrorArraySource<T>)clone;

            foreach(IArraySourceMonitor<T> monitor in _monitors)
            {
                mirrorArraySource._monitors.Add((IArraySourceMonitor<T>)monitor.DeepCopyFindOrCreate(graph));
            }

            foreach(KeyValuePair<T,T> kvp in _lookupTemplateToMirror)
            {
                T key = (T)kvp.Key.DeepCopyFindOrCreate(graph);
                T value = (T)kvp.Value.DeepCopyFindOrCreate(graph);
                mirrorArraySource._lookupTemplateToMirror.Add(key, value);
                mirrorArraySource._lookupMirrorToTemplate.Add(value, key);
                Debug.Assert(key != null && value != null);
            }

            mirrorArraySource._parent = _parent == null ? null : (ITransformNode)_parent.DeepCopyFindOrCreate(graph);
            mirrorArraySource._template = (IArraySource<T>) _template.DeepCopyFindOrCreate(graph);
            mirrorArraySource._root = _root;
            mirrorArraySource._childIsRoot = _childIsRoot;
        }

        public void InsertArraySource(int index, IArraySource<T> source)
        {
            _template.InsertArraySource(index, source);
        }

        public void AddArraySourceInPlace(IArraySource<T> source)
        {
#if DEBUG
            Matrix sourceTransform = source.TotalTransform;
#endif
            _template.AddArraySourceInPlace(source);
#if DEBUG
            Debug.Assert((source.TotalTransform - sourceTransform).Translation.Length() < 0.1);
#endif
        }

        public void InsertArraySourceInPlace(int index, IArraySource<T> source)
        {
            source.Transform(Matrix.Invert(TotalTransform));
            _template.InsertArraySourceInPlace(index, source);
        }
    }
}
