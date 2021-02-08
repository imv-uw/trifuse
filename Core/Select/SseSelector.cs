using Core;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;

namespace WebService.Pipeline
{
    public class SseSelector : Selector
    {
        public override IEnumerable<Selection> Select(IStructure structure)
        {
            List<Selection> results = new List<Selection>();

            for(int chainIndex = 0; chainIndex < structure.Count; chainIndex++)
            {
                if (chainIndex == ChainIndex)
                    continue;

                IChain chain = structure[chainIndex];
                List<SSBlock> chainSelections = SecondaryStructure.GetPhiPsiSSBlocks(chain, MinLength);
                
                if (IncludeAdjacentLoops)
                {
                    // Extend the SSBlock ranges to include everything up to the neighboring blocks
                    List<SSBlock> extendedChainSelections = new List<SSBlock>();
                    for (int selectionIndex = 0; selectionIndex < chainSelections.Count; selectionIndex++)
                    {
                        int start = selectionIndex == 0 ? 0 : chainSelections[selectionIndex - 1].End + 1;
                        int end = selectionIndex == chainSelections.Count - 1 ? chain.Count - 1 : chainSelections[selectionIndex + 1].Start - 1;
                        extendedChainSelections.Add(new SSBlock(chainSelections[selectionIndex].SS, start, end));
                    }
                    chainSelections = extendedChainSelections;
                }

                if (SkipCountC != null && SkipCountC > 0)
                {
                    int firstSkipIndex = Math.Max(0, chainSelections.Count - (int)SkipCountC);
                    chainSelections.RemoveRange(firstSkipIndex, chainSelections.Count - firstSkipIndex);
                }

                if (SkipCountN != null && SkipCountN > 0)
                {
                    int skipCount = Math.Min((int)SkipCountN, chainSelections.Count);
                    chainSelections.RemoveRange(0, skipCount);
                }

                // Convert the computed SS block ranges to an AA selection and add that to list
                results.AddRange(chainSelections.Select(block => new Selection(chain[block.Start, block.End])));
            }

            return results;
        }

        public int MinLength { get; set; } = 10;

        public int? ChainIndex { get; set; } = null;

        public int? SkipCountN { get; set; } = null;

        public int? SkipCountC { get; set; } = null;

        public bool IncludeAdjacentLoops { get; set; }

        public Selection Set
        {
            get;
            private set;
        } = new Selection();

        public override bool Valid { get; protected set; } = true;

        public override string[] UsageText { get; protected set; } = new string[] { "Manually select residues." };
    }
}