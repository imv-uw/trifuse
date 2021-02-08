#define DEBUG_TRANSFORMS

using Core.Interfaces;
using Core.Quick;
using Core.Tools;
using Core.Utilities;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Core.Collections
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ArraySource<T> : IArraySource<T>, IArraySourceMonitor<T>, ITransformNode, IList<T>, IDeepCopy where T : class, ITransformable, IDeepCopy
    {
        // Transforms
        [JsonProperty] Matrix _matrix = Matrix.Identity;
        [JsonProperty] Matrix _cachedNodeTransform;        // If input transforms are cached (node and parent), then the 
        [JsonProperty] Matrix _cachedParentTransform;      // cached output is valid (node total)
        [JsonProperty] Matrix _cachedTotalTransform;

        // Wrapper/facade for this object. When children are added, their parent property will be set to the facade, allowing 
        // the child to step through the hierarchy of the container types.
        [JsonProperty] IArraySource<T> _facade;             
                                                            
        // Contents
        [JsonProperty] ITransformNode _parent;
        [JsonProperty] int _count = 0;
        [JsonProperty] List<T> _items = new List<T>();
        [JsonProperty] List<int> _sourceOffsets = new List<int>(); // Offsets into the _items list at which this source is inserted
        [JsonProperty] List<IArraySourceMonitor<T>> _monitors = new List<IArraySourceMonitor<T>>();
        [JsonProperty] List<IArraySource<T>> _sources = new List<IArraySource<T>>();

        [JsonConstructor]
        public ArraySource() { }
        public ArraySource(IArraySource<T> facade)
        {
            _facade = facade;
        }
        public ArraySource(IArraySource<T> facade, IArraySource<T> other)
        {
            _facade = facade;
            _matrix = other.NodeTransform;
            //Parent = other.Parent;
        }
        public ArraySource(IArraySource<T> facade, T item)
        {
            _facade = facade;
            Add(item);
        }
        public ArraySource(IArraySource<T> facade, IEnumerable<T> items)
        {
            _facade = facade;
            AddRange(items);
        }

        #region Public Properties
        public T this[int index]
        {
            get
            {
                int sourceIndex;
                int subIndex;
                GetSubIndex(index, out sourceIndex, out subIndex);

                if (sourceIndex < 0)
                {
                    T item = _items[subIndex];
                    return item;
                }
                else
                {
                    T item = _sources[sourceIndex][subIndex];
                    return item;
                }
            }
            set
            {
                //int position = _count;
                //item.Parent = _facade ?? this;
                //item.TransformSetting = TransformSetting;
                //_items.Add(item);
                //_count++;
                //NotifyMonitorsItemAdded(position, item);

                int sourceIndex;
                int subIndex;
                GetSubIndex(index, out sourceIndex, out subIndex);

                
                if (sourceIndex < 0)
                {
                    T removed = _items[subIndex];
                    _items[subIndex] = value;
                    value.Parent = _facade ?? this;
                    NotifyReplace(_facade?? this, index, removed, value);
                }
                else
                {
                    _sources[sourceIndex][subIndex] = value;
                }
            }
        }
        public IList<T> this[int start, int end]
        {
            get
            {
                List<T> list = new List<T>(end - start + 1);
                for (int i = start; i <= end; i++)
                {
                    list.Add(this[i]);
                }
                return list;
            }
        }
        public int Count => _count;
        public bool IsMirror => false;
        public bool IsReadOnly => false;
        public ITransformNode Parent
        {
            get => _parent;

            set
            {
                // This is an ugly hack, but set _parent to null before calling parent.DisconnectDependent
                // because otherwise recursion may occur. The parent will likely set child.Parent to null
                // in its DisconnectDependent call.
                if(_parent != value && _parent != null)
                {
                    ITransformNode parent = _parent;
                    _parent = null;
                    parent.DisconnectDependent(_facade ?? this);
                }
                    
                _parent = value;
            }
        }
        public virtual Matrix NodeTransform
        {
            get
            {
                return _matrix;
                //Matrix myTransform = _transforms.Aggregate(Matrix.Identity, (a, b) => Matrix.Multiply(a, b.Transform));
                //return myTransform;
            }
            set
            {
                _matrix = value;
            }
        }
        public Matrix TotalParentTransform
        {
            get
            {
                Matrix parentTransform = Parent == null ? Matrix.Identity : Parent.TotalTransform;
                return parentTransform;
            }
        }
        public Matrix TotalTransform
        {
            get
            {
                if (Parent == null)
                    return _matrix;

                Matrix totalParentTransform = TotalParentTransform;

                if (_matrix != _cachedNodeTransform || totalParentTransform != _cachedParentTransform)
                {
                    _cachedParentTransform = totalParentTransform;
                    _cachedNodeTransform = _matrix;
                    _cachedTotalTransform = _matrix * totalParentTransform;
                }

                return _cachedTotalTransform;
            }
        }
        public Vector3 Origin => Vector3.Transform(Vector3.Zero, TotalTransform);
        public Vector3 UnitX => Vector3.Transform(Vector3.UnitX, TotalTransform.Rotation);
        public Vector3 UnitY => Vector3.Transform(Vector3.UnitY, TotalTransform.Rotation);
        public Vector3 UnitZ => Vector3.Transform(Vector3.UnitZ, TotalTransform.Rotation);
        #endregion

        #region Public Methods
        public void Add(T item)
        {
            int position = _count;
            item.Parent = _facade ?? this;
            _items.Add(item);
            _count++;
            NotifyMonitorsItemAdded(position, item);
        }
        public void AddArraySourceInPlace(IArraySource<T> source)
        {
#if DEBUG_TRANSFORMS
            Matrix current = source.TotalTransform;
#endif
            Matrix nodeTransform = GetAddInPlaceNodeTransform(source);
            AddArraySource(source);
            source.NodeTransform = nodeTransform;

#if DEBUG_TRANSFORMS
            Matrix final = source.TotalTransform;
            Debug.Assert((final - current).Translation.Length() < 0.1);
#endif
        }
        public void AddMonitor(IArraySourceMonitor<T> monitor)
        {
            Trace.Assert(monitor != null);
            _monitors.Add(monitor);
        }
        public void AddRange(IEnumerable<T> items)
        {
            int position = _count;
            T[] array = items.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Parent = _facade ?? this;
            }

            _items.AddRange(array);
            _count += array.Length;
            NotifyMonitorsRangeAdded(position, array);
        }
        public void AddArraySource(IArraySource<T> source)
        {
            _sources.Add(source);
            _sourceOffsets.Add(_items.Count);
            _count += source.Count;
            source.Parent = _facade?? this;
            //source.TransformSetting = TransformSetting;
            source.AddMonitor(this);
            NotifyMonitorsRangeAdded(_count - source.Count, source.ToArray());
        }
        public void Clear()
        {
            T[] removed = _monitors.Count == 0 ? null : this.ToArray();

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Parent = null;
            }
            _items.Clear();

            for (int i = 0; i < _sources.Count; i++)
            {
                IArraySource<T> source = _sources[i];
                source.Parent = null;
                source.RemoveMonitor(this);
            }
            _sources.Clear();
            _sourceOffsets.Clear();

            for (int i = 0; i < _monitors.Count; i++)
            {
                _monitors[i].NotifyRemoveRange(this, 0, removed);
            }
            _count = 0;
        }
        public bool Contains(T item)
        {
            return _items.Contains(item) || _sources.Any(source => source.Contains(item));
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _count; i++)
            {
                array[i + arrayIndex] = this[i];
            }
        }
        public void DisconnectDependent(object dependent)
        {
            if (dependent is T)
            {
                Remove((T)dependent);
                return;
            }

            if (dependent is IArraySource<T> && _sources.Contains(dependent))
            {
                RemoveArraySource((IArraySource<T>)dependent);
                return;
            }
        }
        public void GetCoordinateSystem(out Vector3 origin, out Vector3 unitX, out Vector3 unitY, out Vector3 unitZ)
        {
            Matrix totalTransform = TotalTransform;
            origin = Vector3.Transform(Vector3.Zero, totalTransform);
            unitX = Vector3.Transform(Vector3.UnitX, totalTransform);
            unitY = Vector3.Transform(Vector3.UnitY, totalTransform);
            unitZ = Vector3.Transform(Vector3.UnitZ, totalTransform);
        }
        public IEnumerator<T> GetEnumerator()
        {
            return new ArraySourceEnumerator<T>(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public int IndexOf(T item)
        {
            int index = _items.IndexOf(item);
            if (0 <= index)
            {
                for (int i = 0; i < _sources.Count; i++)
                {
                    if (_sourceOffsets[i] <= index)
                        index += _sources[i].Count;
                }
                return index;
            }
            else
            {
                for (int i = 0; i < _sources.Count; i++)
                {
                    index = _sources[i].IndexOf(item);
                    if (0 <= index)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            index += _sources[j].Count;
                        }
                        return index;
                    }
                }
                return -1;
            }
        }
        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }
        public void InsertArraySource(int index, IArraySource<T> source)
        {
            GetSubIndex(index, out int sourceIndex, out int subIndex);

            if (sourceIndex < 0)
            {
                // It can be inserted at the top level at in index corresponding to _items
                for (sourceIndex = 0; sourceIndex <= _sources.Count; sourceIndex++)
                {
                    if (sourceIndex == _sources.Count)
                    {
                        _sources.Add(source);
                        _sourceOffsets.Add(subIndex);
                        break;
                    }
                    else if (subIndex <= _sourceOffsets[sourceIndex])
                    {
                        _sources.Insert(sourceIndex, source);
                        _sourceOffsets.Insert(subIndex, subIndex);
                        break;
                    }
                }
                _count += source.Count;
                source.Parent = _facade ?? this;
                source.AddMonitor(this);
                NotifyMonitorsRangeAdded(index, source.ToArray());
            }
            else
            {
                // It falls within _sources
                if (subIndex == 0)
                {
                    // It's the first element - we can insert beforehand
                    _sources.Insert(sourceIndex, source);
                    _sourceOffsets.Insert(sourceIndex, _sourceOffsets[sourceIndex]);
                    _count += source.Count;
                    source.Parent = _facade ?? this;
                    source.AddMonitor(this);
                    NotifyMonitorsRangeAdded(index, source.ToArray());
                }
                else
                {
                    // Insert the new array source into an existing array source
                    _sources[sourceIndex].InsertArraySource(subIndex, source);
                }
            }
        }
        public void InsertArraySourceInPlace(int index, IArraySource<T> source)
        {
            // The matrix cannot be precalculated for insertion, because it might end up in 
            // a sub-source, in which case the total upstream transform is not defined yet
            Matrix desired = source.TotalTransform;
            InsertArraySource(index, source);
            source.NodeTransform = desired * Matrix.Invert(source.TotalParentTransform);
        }
        public void NotifyAdd(IArraySource<T> source, int position, T added)
        {
            NotifyAddRange(source, position, new T[] { added });
        }
        public void NotifyAddRange(IArraySource<T> source, int position, T[] added)
        {
            _count += added.Length;

            if (_monitors.Count == 0)
                return;

            int sourceIndex = _sources.IndexOf(source);
            if (0 <= sourceIndex)
            {
                int sourceGlobalOffset = GetArraySourceItemIndex(sourceIndex);
                NotifyMonitorsRangeAdded(sourceGlobalOffset + position, added);
            }
            else
            {
                throw new ArgumentException("source has not been added");
            }
        }
        public void NotifyRemove(IArraySource<T> source, int position, T removed)
        {
            NotifyRemoveRange(source, position, new T[] { removed });
        }
        public void NotifyReplace(IArraySource<T> arraySource, int index, T removed, T added)
        {
            if (_monitors.Count == 0)
                return;

            for (int i = 0; i < _sources.Count; i++)
            {
                if (arraySource != _sources[i])
                    continue;

                for (int j = 0; j < _monitors.Count; j++)
                {
                    _monitors[j].NotifyReplace(_facade ?? this, index + _sourceOffsets[i], removed, added);
                }
            }
        }
        public void NotifyRemoveRange(IArraySource<T> source, int position, T[] removed)
        {
            _count -= removed.Length;

            if (_monitors.Count == 0)
                return;

            int sourceIndex = _sources.IndexOf(source);
            if (0 <= sourceIndex)
            {
                int sourceGlobalOffset = GetArraySourceItemIndex(sourceIndex);
                NotifyMonitorsRangeRemoved(sourceGlobalOffset + position, removed);
                return;
            }
            else
            {
                throw new ArgumentException("source has not been added");
            }
        }
        public bool Remove(T item)
        {
            int index = _items.IndexOf(item);
            if (index < 0)
            {
                for(int i = 0; i < _sources.Count; i++)
                {
                    if (_sources[i].Remove(item))
                        return true;
                }
                return false;
                //throw new ArgumentException("the item is not directly contained, but might be part of a sub-source. re-examine usage needs?");
                //return false;
            }
                
            this.RemoveAt(index);
            return true;
        }
        public void RemoveArraySource(IArraySource<T> source)
        {
            int sourceIndex = _sources.IndexOf(source);
            int sourceGlobalOffset = GetArraySourceItemIndex(source);
            _sources.RemoveAt(sourceIndex);
            _sourceOffsets.RemoveAt(sourceIndex);
            _count -= source.Count;
            source.Parent = null;
            source.RemoveMonitor(this);

            NotifyMonitorsRangeRemoved(sourceGlobalOffset, source.ToArray());
        }
        public void RemoveAt(int index)
        {
            T removed = RemoveWithoutNotificationAt(index);
            _count--;
            NotifyMonitorsItemRemoved(index, removed);
            //NotifyRemove(this, index, removed);
        }
        public void RemoveMonitor(IArraySourceMonitor<T> monitor)
        {
            _monitors.Remove(monitor);
        }
        public void RemoveRange(int start, int count)
        {
            T[] removed = new T[count];

            for (int i = start + count - 1; start <= i; i--)
            {
                removed[i - start] = RemoveWithoutNotificationAt(i);
                _count--;
            }
            NotifyMonitorsRangeRemoved(start, removed);
            //NotifyRemoveRange(this, start, removed);
        }
        public virtual void Rotate(Quaternion rotation, Vector3 origin)
        {
            Matrix transform = MatrixUtil.GetRotation(ref rotation, ref origin);
            Transform(transform);
        }
        public virtual void RotateNode(Quaternion rotation, Vector3 origin)
        {
            Matrix transform = MatrixUtil.GetRotation(ref rotation, ref origin);
            _matrix *= transform;
        }
        public virtual void Transform(Matrix transform)
        {
            //switch (TransformSetting)
            //{
            //    case TransformSettings.Global:
            //        for (int i = 0; i < _items.Count; i++)
            //        {
            //            _items[i].Transform(transform);
            //        }
            //        for (int i = 0; i < _sources.Count; i++)
            //        {
            //            _sources[i].Transform(transform);
            //        }
            //        break;
                //case TransformSettings.Transform:
                    // Final matrix = this * parent
                    // Transformed world = this * parent * transform
                    // Solve for this' s.t. this' * parent = this * parent * transform
                    // this' = this * parent * transform * parent^-1
                    _matrix = _matrix * TotalParentTransform * transform * Matrix.Invert(TotalParentTransform);
                    //break;
            //}
        }
        public virtual void TransformNode(Matrix transform)
        {
            _matrix *= transform;
        }
        public virtual void TranslateNode(Vector3 translation)
        {
            _matrix.Translation += translation;
        }
        public virtual void Translate(Vector3 translation)
        {
            //switch (TransformSetting)
            //{
            //    case TransformSettings.Global:
            //        for (int i = 0; i < _items.Count; i++)
            //        {
            //            _items[i].Translate(translation);
            //        }
            //        for (int i = 0; i < _sources.Count; i++)
            //        {
            //            _sources[i].Translate(translation);
            //        }
            //        break;
            //    case TransformSettings.Transform:
                    Matrix transform = Matrix.Identity;
                    transform.Translation = translation;
                    Transform(transform);
            //        break;
            //}
        }
        #endregion

        #region Protected Methods
        protected Matrix GetAddInPlaceNodeTransform(ITransformNode node)
        {
            Matrix current = node.TotalTransform;
            Matrix desired = current * Matrix.Invert(TotalTransform);
            return desired;
        }
        #endregion

        #region Private Methods
        int GetArraySourceItemIndex(IArraySource<T> source)
        {
            int sourceIndex = _sources.IndexOf(source);

            if (sourceIndex < 0)
                throw new ArgumentException("source has not been added");

            int itemOffset = GetArraySourceItemIndex(sourceIndex);

            return itemOffset;
        }
        int GetArraySourceItemIndex(int arraySourceIndex)
        {
            int itemOffset = _sourceOffsets[arraySourceIndex];
            for (int i = 0; i < arraySourceIndex; i++)
            {
                itemOffset += _sources[i].Count;
            }

            return itemOffset;
        }
        Range GetArraySourceItemRange(int arraySourceIndex)
        {
            int itemOffset = _sourceOffsets[arraySourceIndex];
            for (int i = 0; i < arraySourceIndex; i++)
            {
                itemOffset += _sources[i].Count;
            }

            Range range = new Range(itemOffset, itemOffset + _sources[arraySourceIndex].Count - 1);
            return range;
        }
        Range GetArraySourceItemRange(IArraySource<T> source)
        {
            int sourceIndex = _sources.IndexOf(source);

            if (sourceIndex < 0)
                throw new ArgumentException("source has not been added");

            Range range = GetArraySourceItemRange(sourceIndex);
            return range;
        }
        /// <summary>
        /// Determines the indices of the item as either:
        /// 1) _items[subIndex] or
        /// 2) _sources[sourceIndex][subIndex]
        /// If sourceIndex is -1, this indicates the index corresponds to _items.
        /// Item in an array source take precedence / are indexed first, i.e.
        /// --array sources at item index
        /// --item at that index
        /// Array source offsets are offsets into the _items list - not global offsets.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="subIndex"></param>
        void GetSubIndex(int index, out int sourceIndex, out int subIndex)
        {
            int externalIndex = index; // As visible externally

            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sourceOffsets[i] + _sources[i].Count <= index)
                {
                    // Index above source range
                    index -= _sources[i].Count;
                    continue;
                }
                else if (_sourceOffsets[i] <= index && index < _sourceOffsets[i] + _sources[i].Count)
                {
                    // Index within source range
                    sourceIndex = i;
                    subIndex = index - _sourceOffsets[i];
                    return;
                }
                else
                {
                    // Index below next source range
                    Trace.Assert(index < _sourceOffsets[i]);
                    break;
                }
            }

            sourceIndex = -1;
            subIndex = index;
        }
        void NotifyMonitorsItemAdded(int position, T item)
        {
            foreach (IArraySourceMonitor<T> monitor in _monitors)
            {
                monitor.NotifyAdd(_facade ?? this, position, item);
            }
        }
        void NotifyMonitorsItemRemoved(int position, T item)
        {
            foreach (IArraySourceMonitor<T> monitor in _monitors)
            {
                monitor.NotifyRemove(_facade ?? this, position, item);
            }
        }
        void NotifyMonitorsRangeAdded(int position, T[] items)
        {
            foreach (IArraySourceMonitor<T> monitor in _monitors)
            {
                monitor.NotifyAddRange(_facade ?? this, position, items);
            }
        }
        void NotifyMonitorsRangeRemoved(int position, T[] items)
        {
            foreach (IArraySourceMonitor<T> monitor in _monitors)
            {
                monitor.NotifyRemoveRange(_facade ?? this, position, items);
            }
        }
        T RemoveWithoutNotificationAt(int index)
        {
            T removed = null;
            int externalIndex = index; // As visible externally

            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sourceOffsets[i] + _sources[i].Count <= index)
                {
                    index -= _sources[i].Count;
                    continue;
                }
                else if (_sourceOffsets[i] <= index && index < _sourceOffsets[i] + _sources[i].Count)
                {
                    removed = _sources[i][index - _sourceOffsets[i]];
                    break;
                }
                else
                {
                    Trace.Assert(index < _sourceOffsets[i]);
                    break;
                }
            }

            if (removed == null && index < _items.Count)
            {
                removed = _items[index];
                _items.RemoveAt(index);
            }

            if (removed != null && removed.Parent == this)
                removed.Parent = null;

            return removed;
        }
        #endregion

        #region IDeepCopy
        public virtual object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(graph);
        }
        public virtual object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            ArraySource<T> array = new ArraySource<T>();
            graph.Add(this, array);
            DeepCopyPopulateFields(graph, array);
            return array;
        }
        public virtual void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            ArraySource<T> array = (ArraySource<T>)clone;

            array._count = _count;
            array._matrix = _matrix;
            array._cachedNodeTransform = _cachedNodeTransform;
            array._cachedParentTransform = _cachedParentTransform;
            array._cachedTotalTransform = _cachedTotalTransform; ;
            //array._transformSetting = _transformSetting;
            array._facade = _facade == null? null : (IArraySource<T>) _facade.DeepCopyFindOrCreate(graph);
            array._sources.AddRange(_sources.Select(s => s.DeepCopyFindOrCreate(graph)).Cast<IArraySource<T>>());
            array._sourceOffsets.AddRange(_sourceOffsets);
            array._items.AddRange(_items.Select(i => i.DeepCopyFindOrCreate(graph)).Cast<T>());
            array._monitors.AddRange(_monitors.Select(m => m.DeepCopyFindOrCreate(graph)).Cast<IArraySourceMonitor<T>>());
            array.Parent = Parent == null ? null : (ITransformNode) Parent.DeepCopyFindOrCreate(graph);

        }
        #endregion
    }
}
