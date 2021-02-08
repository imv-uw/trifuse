using Core;
using Core.Interfaces;
using Core.PDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tools
{
    public enum ResfileDesignOperation
    {
        CLEAR = 0,
        NATRO = 1,
        NATAA = 2,
        PIKAA = 4,
        NOTAA = 8,
        ANYAA = 16
    }

    public class Resfile
    {
        class ResfileAaIdentifier
        {
            public ResfileAaIdentifier(int chainIndex, int aaIndex)
            {
                ChainIndex = chainIndex;
                AaIndex = aaIndex;
            }

            public int ChainIndex { get; set; }
            public int AaIndex { get; set; }
        }

        class ResfileAaOperation
        {
            public ResfileAaOperation(ResfileDesignOperation operation, IEnumerable<char> letters = null)
            {
                Operation = operation;
                if(letters != null)
                {
                    Letters.Clear();
                    Letters.AddRange(letters);
                }
            }

            public ResfileDesignOperation Operation { get; set; }
            public List<char> Letters { get; set; } = new List<char>();
        }

        Dictionary<ResfileAaIdentifier, ResfileAaOperation> _operations = new Dictionary<ResfileAaIdentifier, ResfileAaOperation>();

        public Resfile()
        {
            DefaultDesignOperation = ResfileDesignOperation.NATRO;
            DefaultChain = 'A';
            ZeroIndexedInput = false;
        }

        public ResfileDesignOperation DefaultDesignOperation { get; set; }
        public char DefaultChain { get; set; }
        public int DesignOperationsCount { get { return _operations.Count; } }
        public bool ZeroIndexedInput { get; set; }

        public void SetNativeRotamer(int chainIndex, int aaIndex)
        {
            SetDesignOperation(ResfileDesignOperation.NATRO, chainIndex, aaIndex, null);
        }

        public void SetNativeAminoAcid(int chainIndex, int aaIndex)
        {
            SetDesignOperation(ResfileDesignOperation.NATAA, chainIndex, aaIndex, null);
        }

        public void SetAnyAminoAcid(int chainIndex, int aaIndex)
        {
            SetDesignOperation(ResfileDesignOperation.ANYAA, chainIndex, aaIndex, null);
        }

        public void SetPickFromAminoAcids(int chainIndex, int aaIndex, IEnumerable<int> residueTypes)
        {
            SetDesignOperation(ResfileDesignOperation.PIKAA, chainIndex, aaIndex, residueTypes.Select(num => AaTable.GetResidueLetter(num)));
        }

        public void SetPickFromAminoAcids(int chainIndex, int aaIndex, IEnumerable<char> residueLetters)
        {
            SetDesignOperation(ResfileDesignOperation.PIKAA, chainIndex, aaIndex, residueLetters);
        }

        public void SetNotAminoAcids(int chainIndex, int aaIndex, IEnumerable<int> residueTypes)
        {
            SetDesignOperation(ResfileDesignOperation.NOTAA, chainIndex, aaIndex, residueTypes.Select(num => AaTable.GetResidueLetter(num)));
        }

        public void SetNotAminoAcids(int chainIndex, int aaIndex, IEnumerable<char> residueLetters)
        {
            SetDesignOperation(ResfileDesignOperation.NOTAA, chainIndex, aaIndex, residueLetters);
        }

        public void Write(TextWriter stream)
        {
            foreach(string line in GetFileText())
            {
                stream.WriteLine(line);
            }
            stream.Flush();
        }

        public void SetDesignOperation(ResfileDesignOperation operation, int chainIndex, int aaIndex, IEnumerable<char> letters = null)
        {
            ResfileAaIdentifier aaIdentifier = new ResfileAaIdentifier(chainIndex, aaIndex);
            ResfileAaOperation aaOperation = new ResfileAaOperation(operation, letters);
            _operations[aaIdentifier] = aaOperation;
        }

        public void SetDesignOperation(IEnumerable<IChain> chains, Selection selection, ResfileDesignOperation operation, IEnumerable<char> aas = null)
        {
            int chainIndex = 0;
            foreach(IChain chain in chains)
            { 
                for (int aaIndex = 0; aaIndex < chain.Count; aaIndex++)
                {
                    if (!selection.Aas.Contains(chain[aaIndex]))
                        continue;

                    SetDesignOperation(operation, chainIndex, aaIndex, aas);
                }
                chainIndex++;
            }
        }
           
        public string[] GetFileText()
        {
            List<string> text = new List<string>();
            text.Add(DefaultDesignOperation.ToString());
            text.Add("START");

            List<ResfileAaIdentifier> positions = _operations.Keys.ToList();
            positions.Sort((a, b) => a.ChainIndex != b.ChainIndex ? a.ChainIndex.CompareTo(b.ChainIndex) : a.AaIndex.CompareTo(b.AaIndex));

            int chainIndex = 0;
            char chain = DefaultChain;
            foreach (ResfileAaIdentifier position in positions)
            {
                ResfileAaOperation operation = _operations[position];
                while(chainIndex < position.ChainIndex)
                {
                    chainIndex++;
                    chain = PdbQuick.GetNextChainId(chain);
                }

                string letters = operation.Letters.Aggregate("", (a, b) => a + b);
                string line = String.Format("{0} {1} {2} {3}", position.AaIndex + 1, chain, operation.Operation, letters);
                text.Add(line);
            }
            return text.ToArray();
        }
    }
}
