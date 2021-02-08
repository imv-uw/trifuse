using Core.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace Core.Collections
{
    public class ArraySourceEnumerator<T> : IEnumerator<T>
    {
        int _position = -1;
        IArraySource<T> _source;

        public ArraySourceEnumerator(IArraySource<T> source)
        {
            _source = source;
        }

        public T Current
        {
            get
            {
                return _source[_position];
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _source = null;
            _position = -1;
        }

        public bool MoveNext()
        {
            return ++_position < _source.Count;
        }

        public void Reset()
        {
            _position = -1;
        }
    }
}
