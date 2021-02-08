using Core.Interfaces;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Core.Collections
{
    public class NodeArraySource<T> : ArraySource<T>, INodeArraySource<T> where T : class, ITransformNode, ITransformable, IDeepCopy
    {
        public T this[int index, bool placed]
        {
            set
            {
                if(placed)
                {
                    Matrix nodeTransform = GetAddInPlaceNodeTransform(value);
                    this[index] = value;
                    value.NodeTransform = nodeTransform;
                }
                else
                {
                    this[index] = value;
                }
            }
        }

        public NodeArraySource()
        {
        }

        public NodeArraySource(IArraySource<T> facade) : base(facade)
        {
        }

        public NodeArraySource(IArraySource<T> facade, IArraySource<T> other) : base(facade, other)
        {
        }

        public NodeArraySource(IArraySource<T> facade, T item) : base(facade, item)
        {
        }

        public NodeArraySource(IArraySource<T> facade, IEnumerable<T> items) : base(facade, items)
        {
        }

        public void AddInPlace(T item)
        {
            Matrix nodeTransform = GetAddInPlaceNodeTransform(item);
            base.Add(item);
            item.NodeTransform = nodeTransform;
        }

        public void AddRangeInPlace(IEnumerable<T> items)
        {
            foreach(T item in items)
            {
                AddInPlace(item);
            }
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            NodeArraySource<T> array = new NodeArraySource<T>();
            graph.Add(this, array);
            DeepCopyPopulateFields(graph, array);
            return array;
        }
    }
}
