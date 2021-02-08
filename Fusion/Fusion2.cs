using Microsoft.Xna.Framework;
using Core;
using Core.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tools;
using Core.Interfaces;

namespace Fuse
{
    public class Fusion2
    {
        public const int DefaultAlignmentLength = 8;
        public const int DefaultTopX = 10;

        public static IEnumerable<FusionDesignInfo> GetSplices(IChain nPeptide, Range nAllowedSpliceRange, IChain cPeptide, Range cAllowedSpliceRange, int topX = DefaultTopX)
        {
            List<FusionDesignInfo> splices = new List<FusionDesignInfo>();
            int minAlignmentLength = 8;
            float maxRmsd = 0.25f;

            // Find all alignments and remove those that are not between the desired ranges
            // If this becomes a bottleneck, could specify allowed alignment ranges
            List<SequenceAlignment> alignments = Fusion.GetAlignmentsPreservingFullSsBlocks(nPeptide, cPeptide, SS.Helix, minAlignmentLength, maxRmsd);
            alignments.RemoveAll(a => !(nAllowedSpliceRange.Start <= a.Range1.Start && a.Range1.End <= nAllowedSpliceRange.End));
            alignments.RemoveAll(a => !(cAllowedSpliceRange.Start <= a.Range2.Start && a.Range2.End <= cAllowedSpliceRange.End));

            foreach(SequenceAlignment alignment in alignments)
            {
                Matrix alignmentTransform = Rmsd.GetRmsdTransformForResidues(nPeptide, alignment.Range1.Start, alignment.Range1.End, cPeptide, alignment.Range2.Start, alignment.Range2.End);
                IChain copy1 = new Chain(nPeptide);
                IChain copy2 = new Chain(cPeptide);
                copy1.Transform(alignmentTransform);
                IChain[] clashCheckPeptides = new IChain[] { copy1, copy2 };
                if (Clash.GetInterSetBackboneClashes(clashCheckPeptides, new int[] { 0, alignment.Range2.End + 1 }, new int[] { alignment.Range1.Start - 1, copy2.Count - 1 }))
                    continue;

                // Create the spliced peptide
                IChain splicedPeptide = Fusion.GetChain(
                    new IChain[] { copy1, copy2 },
                    new SequenceAlignment[] { alignment },
                    new IEnumerable<int>[] { new int[] { }, new int[] { } });

                // Save it
                FusionDesignInfo spliceInfo = new FusionDesignInfo();
                Range[] originalRanges = null;
                Range[] finalRanges = null;
                Fusion.GetIdentityRanges(clashCheckPeptides, new SequenceAlignment[] { alignment }, out originalRanges, out finalRanges);
                spliceInfo.Peptide = splicedPeptide;
                spliceInfo.OriginalChains = new IChain[] { nPeptide, cPeptide };
                spliceInfo.PdbOutputChains = new IChain[] { splicedPeptide, copy1, copy2 };
                spliceInfo.ImmutablePositions = new int[] { };
                spliceInfo.IdentityRanges = finalRanges;
                spliceInfo.OriginalRanges = originalRanges;

                splices.Add(spliceInfo);
                splices.Sort((a, b) => b.Score.CompareTo(a.Score));
                if (splices.Count > topX)
                {
                    splices.RemoveAt(topX);
                }
            }

            return splices;
        }
    }
}
