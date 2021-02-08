using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamespaceStructure
{
    public class TreeSet3<T,X,Y,Z> : TreeSet<T>
        where T : ITreeSet
    {
        public TreeSet3()
        {
            base.AddType<X>();
            base.AddType<Y>();
            base.AddType<Z>();
        }

        public new void Add(T item)
        {
            base.Add(item);

            ITreeSet set = item as ITreeSet;
            ArrayTree<X> setX = set.GetTree<X>();
            ArrayTree<Y> setY = set.GetTree<Y>();
            ArrayTree<Z> setZ = set.GetTree<Z>();
            base.AddSource(setX);
            base.AddSource(setY);
            base.AddSource(setZ);
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, item);

            ITreeSet set = item as ITreeSet;
            ArrayTree<X> setX = set.GetTree<X>();
            ArrayTree<Y> setY = set.GetTree<Y>();
            ArrayTree<Z> setZ = set.GetTree<Z>();
            base.InsertSource(index, setX);
            base.InsertSource(index, setY);
            base.InsertSource(index, setZ);
        }

        public new void Remove(T item)
        {
            base.Remove(item);

            ITreeSet set = item as ITreeSet;
            ArrayTree<X> setX = set.GetTree<X>();
            ArrayTree<Y> setY = set.GetTree<Y>();
            ArrayTree<Z> setZ = set.GetTree<Z>();
            base.RemoveSource(setX);
            base.RemoveSource(setY);
            base.RemoveSource(setZ);
        }
    }

    public class TreeSet2<T, X, Y> : TreeSet<T>
        where T : ITreeSet
    {
        public TreeSet2()
        {
            base.AddType<X>();
            base.AddType<Y>();
        }

        public new void Add(T item)
        {
            base.Add(item);

            ITreeSet set = item as ITreeSet;
            ArrayTree<X> setX = set.GetTree<X>();
            ArrayTree<Y> setY = set.GetTree<Y>();
            base.AddSource(setX);
            base.AddSource(setY);
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, item);

            ITreeSet set = item as ITreeSet;
            ArrayTree<X> setX = set.GetTree<X>();
            ArrayTree<Y> setY = set.GetTree<Y>();
            base.InsertSource(index, setX);
            base.InsertSource(index, setY);
        }

        public new void Remove(T item)
        {
            base.Remove(item);

            ITreeSet set = item as ITreeSet;
            ArrayTree<X> setX = set.GetTree<X>();
            ArrayTree<Y> setY = set.GetTree<Y>();
            base.RemoveSource(setX);
            base.RemoveSource(setY);
        }

        public new void RemoveRange(int index, int count)
        {
            while(count-- > 0)
            {
                Remove(this[index]);
            }
        }
    }

    public class TreeSet1<T, X> : TreeSet<T>
        where T : ArrayTree<X>
    {
        protected TreeSet1()
        {
            base.AddType<X>();
        }

        protected new void Add(T item)
        {
            base.Add(item);
            base.AddSource(item as ArrayTree<X>);
        }

        protected new void Insert(int index, T item)
        {
            base.Insert(index, item);
            base.InsertSource(index, item as ArrayTree<X>);
        }

        protected new void Remove(T item)
        {
            base.Remove(item);
            base.RemoveSource(item as ArrayTree<X>);
        }

        public new void RemoveRange(int index, int count)
        {
            while (count-- > 0)
            {
                Remove(this[index]);
            }
        }
    }

    public abstract class TreeSet<T> : ArrayTree<T>, ITreeSet
    {
        Type _tType = typeof(T);
        Dictionary<Type, object> _trees = new Dictionary<Type, object>();
        
        public ArrayTree<U> GetTree<U>()
        {
            Type uType = (typeof(U));

            if (uType == _tType)
                return this as ArrayTree<U>;

            if (_trees.ContainsKey(uType))
                return _trees[uType] as ArrayTree<U>;

            return null;
        }

        protected void AddType<U>()
        {
            _trees.Add(typeof(U), new ArrayTree<U>());
        }

        protected void AddSource<U>(ArrayTree<U> set)
        {
            ArrayTree<U> tree = GetTree<U>();
            tree.AddSource(set);
        }

        protected void RemoveSource<U>(ArrayTree<U> set)
        {
            ArrayTree<U> tree = GetTree<U>();
            tree.RemoveSource(set);
        }

        protected void InsertSource<U>(int index, ArrayTree<U> set)
        {
            ArrayTree<U> tree = GetTree<U>();
            tree.InsertSource(index, set);
        }
    }

    public interface ITreeSet
    {
        ArrayTree<U> GetTree<U>();
    }
}
