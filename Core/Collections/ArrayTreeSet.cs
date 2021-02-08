using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamespaceStructure
{
    public class ArrayTreeSet<T /*peptide */, X /*residue*/, Y/* Atom */> : ArrayTree<T> 
        where T : ArrayTree<X>
        where X : ArrayTree<Y>
    {
        ArrayTree<X> _xTree = new ArrayTree<X>();
        ArrayTree<Y> _yTree = new ArrayTree<Y>();

        public ArrayTree<U> GetTree<U>()
        {
            if (typeof(ArrayTree<X>) == typeof(U))
                return _xTree as ArrayTree<U>;
            if (typeof(ArrayTree<Y>) == typeof(U))
                return _yTree as ArrayTree<U>;
            return null;
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, item);
            
            ArrayTreeSet<X,Y> source = item as ArrayTreeSet<X,Y>;
            if(source != null)
            {
                _xTree.InsertSource(index, source.GetTree<X>());
                _yTree.InsertSource(index, source.GetTree<Y>());
            }
        }

        public new void Add(T item)
        {
            base.Add(item);
            
            ArrayTreeSet<X, Y> source = item as ArrayTreeSet<X, Y>;
            if (source != null)
            {
                _xTree.AddSource(source.GetTree<X>());
                _yTree.AddSource(source.GetTree<Y>());
            }
        }

        public new void Remove(T item)
        {
            base.Remove(item);
            ArrayTreeSet<X, Y> source = item as ArrayTreeSet<X, Y>;
            if (source != null)
            {
                _xTree.RemoveSource(source.GetTree<X>());
                _yTree.RemoveSource(source.GetTree<Y>());
            }
        }

        public new void RemoveRange(int index, int count)
        {
            for(int i = index; i < index + count; i++)
            {
                ArrayTreeSet<X, Y> source = this[i] as ArrayTreeSet<X, Y>;
                if (source != null)
                {
                    _xTree.RemoveSource(source.GetTree<X>());
                    _yTree.RemoveSource(source.GetTree<Y>());
                }
            }

            base.RemoveRange(index, count);
        }
    }

    public class ArrayTreeSet<X,Y> : ArrayTree<X>
    {
        ArrayTree<Y> _yTree = new ArrayTree<Y>();

        public ArrayTree<U> GetTree<U>()
        {
            return  _yTree as ArrayTree<U>;
        }

        public new void Insert(int index, X item)
        {
            base.Insert(index, item);
            InsertSecondarySources(index, item as ArrayTree<Y>);
        }

        public new void Add(X item)
        {
            base.Add(item);
            AddSecondarySources(item as ArrayTree<Y>);
        }

        void AddSecondarySources(ArrayTree<Y> source)
        {
            if (source == null)
                return;

            _yTree.AddSource(source);
        }

        void InsertSecondarySources(int index, ArrayTree<Y> source)
        {
            if (source == null)
                return;

            _yTree.InsertSource(index, source);
        }

        public new void Remove(X item)
        {
            base.Remove(item);
            RemoveSecondarySources(item as ArrayTree<Y>);
        }

        public new void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
        }

        void RemoveSecondarySources(ArrayTree<Y> source)
        {
            if (source == null)
                return;

            _yTree.RemoveSource(source);
        }
    }
}
