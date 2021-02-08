using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamespaceStructure
{
    // This class holds generic objects T. It provides indexed access to leaves (items of type T
    // stored in this object) or nested lists (items of type T in lower level lists).
    //
    // Leaves are stored in the current object, which inherits from List<T>.
    // As far as indexing goes - local object leaves come first, followed by all objects in the nested
    // lists below.

    // Peptide.Atoms[index] -> an atom
    // Atom.IndexInProtein -> index
    // Atom.IndexInPeptide -> index
    // Atom.IndexInResidue -> index
    // 
    // Residue.Add(atom)
    // --> Residue.Atoms[atomCount++] = atom
    // --> Residue.
    
    // The only purpose for the hash-set inheritance is to maintain count and provide a check
    // that prevents underlying accidental duplication of data
    public class ArrayTree<T> : IEnumerable<T>
    {
        int _sourceItemCount = 0;
        List<T> _leaves = new List<T>();
        //HashSet<T> _leavesSet = new HashSet<T>();

        List<ArrayTree<T>> _sources = new List<ArrayTree<T>>();
        //Dictionary<ArrayTree<T>, HashSet<T>> _sourceSets = new Dictionary<ArrayTree<T>, HashSet<T>>();

        public delegate void CountChangedHandler(int change);
        public event CountChangedHandler CountChanged;

        //public delegate void ItemRemovedEventHandler(T item);
        //public event ItemRemovedEventHandler ItemRemovedEvent;

        //public delegate void ItemAddedEventHandler(T item);
        //public event ItemAddedEventHandler ItemAddedEvent;

        private void CountChangedHandlerFunction(int count)
        {
            _sourceItemCount += count;
            if (CountChanged != null)
                CountChanged(count);

            if (_sourceItemCount < 0) { throw new Exception("Bug"); }
        }

        protected void Add(T item)
        {
            _leaves.Add(item);
            //_leavesSet.Add(item);
            if (CountChanged != null)
                CountChanged(1);
        }


        protected void Insert(int index, T item)
        {
            _leaves.Insert(index, item);
            if (CountChanged != null)
                CountChanged(1);
            //_leavesSet.Add(item);
        }

        protected void Remove(T item)
        {
            if(!_leaves.Remove(item)) { throw new ArgumentException("Item must be removed at the proper level"); }
            //if(!_leavesSet.Remove(item)) { throw new ArgumentException("Item must be removed at the proper level"); }

            if (CountChanged != null)
                CountChanged(-1);
        }

        protected void RemoveRange(int index, int count)
        {
            _leaves.RemoveRange(index, count);
            if(CountChanged != null)
                CountChanged(-count);
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (item.Equals(this[i]))
                    return i;
            }
            //    if (_leavesSet.Contains(item))
            //    {
            //        return _leaves.IndexOf(item);
            //    }

            //int index = _leaves.Count;
            //foreach (ArrayTree<T> source in _sources)
            //{
            //    if (_sourceSets[source].Contains(item))
            //        return index + source.IndexOf(item);
            //    index += source.Count;
            //}

            throw new Exception("Item not found");
        }

        public void AddSource(ArrayTree<T> source)
        {
            _sources.Add(source);
            _sourceItemCount += source.Count;
            
            source.CountChanged += CountChangedHandlerFunction;

            if (CountChanged != null)
                CountChanged(source.Count);
        }

        public void InsertSource(int index, ArrayTree<T> source)
        {
            _sources.Insert(index, source);
            _sourceItemCount += source.Count;

            source.CountChanged += CountChangedHandlerFunction;

            if (CountChanged != null)
                CountChanged(source.Count);
        }

        public void RemoveSource(ArrayTree<T> source)
        {   
            _sources.Remove(source);
            _sourceItemCount -= source.Count;

            source.CountChanged += CountChangedHandlerFunction;

            if (CountChanged != null)
                CountChanged(-source.Count);
        }

        public T this[int index]
        {
            get
            {
                if(index < 0 || this.Count <= index) { throw new IndexOutOfRangeException(); }

                if(index < _leaves.Count)
                    return _leaves[index];

                index -= _leaves.Count;

                for(int sourceIndex = 0; sourceIndex < _sources.Count; sourceIndex++)
                {
                    if(index < _sources[sourceIndex].Count)
                    {
                        return _sources[sourceIndex][index];
                    }
                    index -= _sources[sourceIndex].Count;
                }
                throw new Exception("Bug.");
            }
        }

        public int Count
        {
            get
            {
                return _leaves.Count + _sourceItemCount;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new NestedListEnumerator(this);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct NestedListEnumerator : IEnumerator<T>, IEnumerator
        {
            ArrayTree<T> _set;
            int _currentIndex;

            public NestedListEnumerator(ArrayTree<T> set)
            {
                _set = set;
                _currentIndex = -1;
            }

            public object Current
            {
                get { return _set[_currentIndex]; }
            }

            public bool MoveNext()
            {
                if (++_currentIndex < _set.Count)
                    return true;
                return false;
            }

            public void Reset()
            {
                _currentIndex = -1;
            }

            T IEnumerator<T>.Current
            {
                get { return _set[_currentIndex]; }
            }

            public void Dispose()
            {
                _set = null;
            }
        }
    }
}
