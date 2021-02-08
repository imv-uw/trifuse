using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Core.Collections
{
    public static class BlockingCollectionQueueFactory<T>
    {
        public static void Create(out BlockingCollectionEnqueue<T> enqueue, out BlockingCollectionDequeue<T> dequeue, out CancellationTokenSource cancellation)
        {
            BlockingCollection<T> collection = new BlockingCollection<T>();

            cancellation = new CancellationTokenSource();
            enqueue = new BlockingCollectionEnqueue<T>(collection, cancellation.Token);
            dequeue = new BlockingCollectionDequeue<T>(collection, cancellation.Token);
        }

        public static void Create(out BlockingCollectionEnqueue<T> enqueue, out BlockingCollectionDequeue<T> dequeue, CancellationToken cancellation)
        {
            BlockingCollection<T> collection = new BlockingCollection<T>();
            enqueue = new BlockingCollectionEnqueue<T>(collection, cancellation);
            dequeue = new BlockingCollectionDequeue<T>(collection, cancellation);
        }

    }


    public class BlockingCollectionEnqueue<T>
    {
        BlockingCollection<T> _collection;
        CancellationToken _cancellationToken;

        public BlockingCollectionEnqueue(BlockingCollection<T> collection, CancellationToken cancellationToken)
        {
            Trace.Assert(collection != null);
            _collection = collection;
            _cancellationToken = cancellationToken;
            Token = cancellationToken;
        }

        public int Count { get { return _collection.Count; } }
        public int BoundedCapacity { get { return _collection.BoundedCapacity; } }
        public bool IsAddingCompleted { get { return _collection.IsAddingCompleted; } }
        public CancellationToken Token { get; private set; }


        public void Add(T item)
        {
            _collection.Add(item, _cancellationToken);
        }
        public void CompleteAdding()
        {
            _collection.CompleteAdding();
        }
        public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return _collection.TryAdd(item, millisecondsTimeout, cancellationToken);
        }
        public bool TryAdd(T item, int millisecondsTimeout)
        {
            return _collection.TryAdd(item, millisecondsTimeout);
        }
        public bool TryAdd(T item)
        {
            return _collection.TryAdd(item);
        }
    }

    public class BlockingCollectionDequeue<T>
    {
        BlockingCollection<T> _collection;
        CancellationToken _cancellationToken;

        public BlockingCollectionDequeue(BlockingCollection<T> collection, CancellationToken cancellationToken)
        {
            Trace.Assert(collection != null);
            _collection = collection;
            _cancellationToken = cancellationToken;
        }

        public int Count { get { return _collection.Count; } }
        public int BoundedCapacity { get { return _collection.BoundedCapacity; } }
        public bool IsAddingCompleted { get { return _collection.IsAddingCompleted; } }
        public bool TryTake(out T output)
        {
            try
            {
                bool taken = _collection.TryTake(out output, -1, _cancellationToken);
                return taken;
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("BlockingCollectionDequeue: OperationCanceledException");
            }
            catch (ObjectDisposedException)
            {
                Trace.WriteLine("BlockingCollectionDequeue: ObjectDisposedException");
            }
            catch (InvalidOperationException)
            {
                Trace.WriteLine("BlockingCollectionDequeue: InvalidOperationException");
                //Trace.Assert(outputs.Token.IsCancellationRequested || methodSource.IsAddingCompleted == true);
            }
            output = default(T);
            return false;
        }
        public void CompleteAdding()
        {
            _collection.CompleteAdding();
        }
    }
}
