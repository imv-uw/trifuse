using System.Collections.Generic;

namespace Core.Interfaces
{

    public interface INodeArraySource<T> : IArraySource<T>
    {
        T this[int index, bool placed] { set; }

        void AddInPlace(T item);
        void AddRangeInPlace(IEnumerable<T> items);
        
        
    }

    public interface IArraySource<T> : ITransformable, ITransformNode, IList<T>, IDeepCopy
    {
        void AddArraySourceInPlace(IArraySource<T> source);
        void InsertArraySourceInPlace(int index, IArraySource<T> source);

        void AddArraySource(IArraySource<T> source);
        void AddMonitor(IArraySourceMonitor<T> monitor);
        void AddRange(IEnumerable<T> items);
        void InsertArraySource(int index, IArraySource<T> source);
        void RemoveArraySource(IArraySource<T> source);
        void RemoveMonitor(IArraySourceMonitor<T> monitor);
        void RemoveRange(int start, int count);
        IList<T> this[int start, int end] { get; }
    }

    public interface IArraySourceMonitor<T> : IDeepCopy
    {
        void NotifyAdd(IArraySource<T> source, int position, T added);
        void NotifyRemove(IArraySource<T> source, int position, T removed);
        void NotifyReplace(IArraySource<T> source, int position, T removed, T added);
        void NotifyAddRange(IArraySource<T> source, int position, T[] added);
        void NotifyRemoveRange(IArraySource<T> source, int position, T[] removed);
    }
}
