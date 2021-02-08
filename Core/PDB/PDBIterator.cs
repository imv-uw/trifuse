using Core.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Core.PDB
{
    public class PdbEnumerable : IEnumerable<IChain>
    {
        string[] _codes = null;
        bool _statusMessages = false;

        public PdbEnumerable(string[] codes, bool statusMessages = false)
        {
            _codes = codes;
            _statusMessages = statusMessages;
        }

        public IEnumerator<IChain> GetEnumerator()
        {
            return new ChainEnumerator(_codes, _statusMessages);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class ChainEnumerator : IEnumerator<IChain>
        {
            int _loadBufferCount = 5;
            Queue<Tuple<string, IChain>> _loaded = new Queue<Tuple<string, IChain>>();
            string[] _codes = null;
            int _index = 0;
            bool _statusMessages = false;
            int _tryCount = 0;
            int _successCount = 0;
            

            public ChainEnumerator(string[] codes, bool statusMessages)
            {
                _codes = codes;
                _statusMessages = statusMessages;
                _loaded.Enqueue(new Tuple<string, IChain>(string.Empty, null));

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += LoadStructuresCallback;
                worker.RunWorkerAsync(this);
            }

            private void LoadStructuresCallback(object sender, DoWorkEventArgs e)
            {
                ChainEnumerator enumerator = (ChainEnumerator) e.Argument;

                while (enumerator._index < enumerator._codes.Length)
                {
                    if (enumerator._loaded.Count < enumerator._loadBufferCount)
                    {
                        string name = enumerator._codes[enumerator._index];
                        IChain chain = PdbQuick.ChainFromFileOrCode(name);

                        lock (enumerator)
                        {
                            enumerator._loaded.Enqueue(new Tuple<string, IChain>(name, chain));
                            enumerator._index++;

                            Console.WriteLine("Enqueue");

                            //Console.WriteLine("Structure loaded, queue count is {0}", enumerator._loaded.Count);
                            Monitor.PulseAll(enumerator);
                        }
                    }
                    else
                    {
                        lock (enumerator)
                        {
                            Monitor.Wait(enumerator);
                        }
                    }
                }
            }

            public IChain Current
            {
                get
                {
                    Console.WriteLine("Current");
                    while (true)
                    {

                        lock(this)
                        {
                            if (_index >= _codes.Length && _loaded.Count == 0)
                                return null;

                            if (_loaded.Count > 0)
                            {
                                Tuple<string, IChain> current = _loaded.Peek();

                                PrintStatus(current.Item1, current.Item2 != null);

                                return current.Item2;

                            }

                            Monitor.Wait(this);
                        }
                    }

                    
                }
            }

            private void PrintStatus(string name, bool success)
            {
                if (success)
                {
                    _successCount++;
                }
                _tryCount++;

                if (_statusMessages)
                {
                    Console.WriteLine("Success={0}/{1}={2:F1}%, Complete={3}/{4}={5:F1}%, Code={6}",
                        _successCount, _tryCount, (float)_successCount / _tryCount * 100,
                        _successCount, _codes.Length, (float)_successCount / _codes.Length * 100, name);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    Console.WriteLine("Current");
                    return Current;
                }
            }

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                Console.WriteLine("Move Next");
                lock(this)
                {
                    while (true)
                    {
                        if (_index >= _codes.Length && _loaded.Count == 0)
                            return false;

                        if (_loaded.Count > 0)
                        {
                            _loaded.Dequeue();
                            //Console.WriteLine("Structure unloaded, queue count is {0}", _loaded.Count);
                            Monitor.PulseAll(this);
                            return true;
                        }
                            
                        Monitor.Wait(this);
                    }
                }
            }

            public void Reset()
            {
                _loaded = new Queue<Tuple<string, IChain>>();
                _loaded.Enqueue(new Tuple<string, IChain>(String.Empty, null));
                _index = 0;
                _successCount = 0;
                _tryCount = 0;
            }
        }
    }
}
