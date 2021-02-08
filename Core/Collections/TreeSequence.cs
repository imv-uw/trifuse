using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structure.DataStructures
{
    public class TreeSequence<T> : IEnumerable<T[]>  where T : IEquatable<T>
    {
        TreeNode<T> _root = new TreeNode<T>();

        public void AddSequence(IEnumerable<T> sequence)
        {
            TreeNode<T> current = _root;
            foreach(T item in sequence)
            {
                TreeNode<T> next = current.GetChild(item);
                if (next == null)
                {
                    current = current.AddChildNode(item);
                }
                else
                {
                    current = next;
                }
            }
        }

        public void PrintLeaves()
        {
            _root.PrintLeaves("");
        }
        
        public class TreeNode<T>
        {
            T _item;
            List<TreeNode<T>> _children = null;

            public TreeNode() {}
            
            public TreeNode(T item)
            {
                _item = item;
            }

            public T GetItem()
            {
                return _item;
            }

            public int GetChildCount()
            {
                return (_children == null)? 0 : _children.Count;
            }

            public TreeNode<T> AddChildNode(T item)
            {
                TreeNode<T> child = new TreeNode<T>(item);
                if (_children == null)
                    _children = new List<TreeNode<T>>();
                _children.Add(child);
                return child;
            }

            public bool ContainsChild(T item)
            {
                foreach (TreeNode<T> child in _children)
                {
                    if (child._item.Equals(item))
                        return true;
                }
                return false;
            }

            public TreeNode<T> GetChild(T item)
            {
                if (_children == null)
                    return null;

                foreach (TreeNode<T> child in _children)
                {
                    if (child._item.Equals(item))
                        return child;
                }

                return null;
            }

            public TreeNode<T> GetChildAt(int index)
            {
                return _children[index];
            }

            public void PrintLeaves(string prefix)
            {
                string line = prefix + ", " + _item.ToString();
                if(_children == null || _children.Count == 0)
                {
                    Console.WriteLine(line);
                    return;
                }
                
                foreach(TreeNode<T> child in _children)
                {
                    child.PrintLeaves(line);
                }
            }
        }

        public class TreeSequenceEnumerator : IEnumerator<T[]>
        {
            TreeNode<T> _root = null;
            List<TreeNode<T>> _path = new List<TreeNode<T>>();
            List<int> _indexes = new List<int>();

            public TreeSequenceEnumerator(TreeNode<T> root)
            {
                _root = root;
            }

            public T[] Current
            {
                get
                {
                    // Retrieve the items from each link in the path, skipping
                    // the first node because that's the root and has an empty/default
                    // data tiem
                    T[] values = new T[_path.Count - 1];
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = _path[i + 1].GetItem();
                    }
                    return values;
                }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return Current;
                }
                
            }

            bool MoveRight()
            {
                // Get rightmost child
                TreeNode<T> last = _path.Last();
                int lastChildCount = last.GetChildCount();
                while (lastChildCount > 0)
                {
                    _path.Add(last.GetChildAt(lastChildCount - 1));
                    _indexes.Add(lastChildCount - 1);

                    if (_path.Last().GetChildCount() == 0)
                        return true;

                    last = _path.Last();
                    lastChildCount = last.GetChildCount();
                }
                return false;
            }

            public bool MoveNext()
            {
                // First time through, get the root's rightmost node
                if(_path.Count == 0)
                {
                    _path.Add(_root);
                    return MoveRight();
                }

                // Move up the tree until we reach a node with child
                // branches or nodes to the left of the current path
                while(_indexes.Count > 0 && _indexes.Last() == 0)
                {
                    _path.RemoveAt(_path.Count - 1);
                    _indexes.RemoveAt(_indexes.Count - 1);
                }
                _path.RemoveAt(_path.Count - 1);

                if(_indexes.Count == 0)
                    return false;

                // Decrement the index and find the rightmost
                int index = _indexes.Last() - 1;
                TreeNode<T> newBranch = _path.Last().GetChildAt(index);
                _path.Add(newBranch);
                _indexes[_indexes.Count - 1] = index;
                MoveRight();
                return true;
            }

            public void Reset()
            {
                _indexes.Clear();
                _path.Clear();
            }
        }

        public IEnumerator<T[]> GetEnumerator()
        {
            return new TreeSequenceEnumerator(_root);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
