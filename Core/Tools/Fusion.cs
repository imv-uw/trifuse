#define DEBUG_MIRRORS
#define DEBUG_TRANSFORMS

using Microsoft.Xna.Framework;
using Core;
using Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Interfaces;
using Core.Quick.Pattern;

namespace Tools
{
    public class Fusion
    {
        public const int DefaultMinAlignmentLength = 8;
        public const int DefaultMaxThirdHelixTrimming = 8;
        public const float DefaultRmsdThreshold = 0.5f;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nChain">The peptide that will be constitute the N-terminal region of the resultant spliced peptide</param>
        /// <param name="cChain">The peptide that will be constitute the C-terminal region of the resultant spliced peptide</param>
        /// <param name="nInclude">The included portion of the N-terminal peptide</param>
        /// <param name="cInclude">The included portion of hte C-terminal peptide</param>
        /// <param name="nAlignNullable">The region to align on prior to splicing</param>
        /// <param name="cAlignNullable">The region to align on prior to splicing</param>
        /// <returns></returns>
        public static IChain GetPeptide(IChain nChain, IChain cChain, Range nInclude, Range cInclude, Range? nAlignNullable = null, Range? cAlignNullable = null)
        {
            bool performAlignment = nAlignNullable != null && cAlignNullable != null;
            Range nAlign = performAlignment ? new Range((Range)nAlignNullable) : new Range();
            Range cAlign = performAlignment ? new Range((Range)cAlignNullable) : new Range();

            if (performAlignment && ((Range)nAlignNullable).Length != ((Range)cAlignNullable).Length)
                throw new ArgumentException("Splice ranges must be of equal length");
            if (nInclude.Start < 0 || nChain.Count <= nInclude.End || cInclude.Start < 0 || cChain.Count <= cInclude.End)
                throw new IndexOutOfRangeException("Splice ranges exceed the peptide ranges");
            
            IChain chain = new Chain();
            //float rmsd = Rmsd.GetRmsd(cTerminus[cAlign.Start, cAlign.End], nTerminus[nAlign.Start, nAlign.End]);
            Matrix cTerminusTransform = performAlignment ? Rmsd.GetRmsdTransform(cChain[cAlign.Start, cAlign.End], nChain[nAlign.Start, nAlign.End]) : Matrix.Identity;
            cTerminusTransform.Rotation.Normalize();
            //Quaternion rotation = cTerminusTransform.Rotation;
            //rotation.Normalize();
            //cTerminusTransform.Rotation = rotation;
            for (int i = nInclude.Start; i <= nInclude.End; i++)
            {
                chain.Add(new Aa(nChain[i]));
            }
            for(int i = cInclude.Start; i <= cInclude.End; i++)
            {
                IAa residue = new Aa(cChain[i]);
                residue.Transform(cTerminusTransform);
                chain.Add(residue);
            }
            return chain;
        }

        /// <summary>
        /// Find splicing alignment ranges for two peptides that would retain a full secondary structure block for at least one of the two peptides. This prevents
        /// short splice overlaps that remove some of the secondary structure on both sides of the alignment. Basically, the alignment must result in either:
        ///   1) one secondary structure block being fully included in the other
        ///   2) the N-terminal peptide being spliced has the end of its block fully in the resultant alignment, thus preserving the entire N-side block
        ///   3) the C-terminal peptide being spliced has the start of its block fully in the resultant alignment, thus preserving the entire C-side block
        /// </summary>
        /// <param name="chain1"></param>
        /// <param name="chain2"></param>
        /// <param name="allowedTypes"></param>
        /// <param name="minAlignmentLength"></param>
        /// <returns></returns>
        public static List<TransformSequenceAlignment> GetTransformAlignments(IChain chain1, IChain chain2, SS allowedTypes, int minAlignmentLength = DefaultMinAlignmentLength, float maxRmsd = DefaultRmsdThreshold)
        {
            List<TransformSequenceAlignment> alignments = new List<TransformSequenceAlignment>();
            List<SSBlock> nBlocks = SecondaryStructure.GetPhiPsiSSBlocksOfType(chain1, allowedTypes, minAlignmentLength).Where(block => block.Length >= minAlignmentLength).ToList();
            List<SSBlock> cBlocks = SecondaryStructure.GetPhiPsiSSBlocksOfType(chain2, allowedTypes, minAlignmentLength).Where(block => block.Length >= minAlignmentLength).ToList();

            // Previously, calling this function with peptides A,B vs B,A returns different numbers of alignments. The original intent was for the first chain passed in to be at the 
            // N-terminus and the second at the C-terminus (post-fusion), so these post-fusion lengths were being calculated here and culled differently based on order.

            foreach (SSBlock nBlock in nBlocks)
            {
                foreach (SSBlock cBlock in cBlocks)
                {

                    for (int nAlignmentOfBlockC = nBlock.End - minAlignmentLength + 1; nAlignmentOfBlockC + cBlock.Length >= nBlock.Start + minAlignmentLength; nAlignmentOfBlockC--)
                    {
                        // Figure out the start and ends of the overlap region
                        int nStart = Math.Max(nBlock.Start, nAlignmentOfBlockC);
                        int nEnd = Math.Min(nBlock.End, nAlignmentOfBlockC + cBlock.Length - 1);
                        int alignmentLength = nEnd - nStart + 1;

                        int cStart = Math.Max(cBlock.Start, cBlock.Start + (nStart - nAlignmentOfBlockC));
                        int cEnd = cStart + alignmentLength - 1;

                        //Debug.WriteLine("Comparing {0}-{1} vs {2}-{3}", nStart, nEnd, cStart, cEnd);

                        Debug.Assert(nBlock.Start <= nStart && nEnd <= nBlock.End);
                        Debug.Assert(cBlock.Start <= cStart && cEnd <= cBlock.End);
                        Debug.Assert(nEnd - nStart == cEnd - cStart);

                        //bool cFullyIncluded = (cBlock.Start == cStart && cBlock.End == cEnd);
                        //bool nFullyIncluded = (nBlock.Start == nStart && nBlock.End == nEnd);

                        //if (!nFullyIncluded && !cFullyIncluded && cStart != cBlock.Start && nEnd != nBlock.End)
                        //    continue;

                        Trace.Assert(nEnd - nStart + 1 >= minAlignmentLength);

                        float rmsd = float.NaN;
                        Matrix matrix = Rmsd.GetRmsdAndTransform(chain1, nStart, nEnd, chain2, cStart, cEnd, out rmsd);
                        bool fail = rmsd > maxRmsd;

#if DEBUG && false
                        float rmsd2 = float.NaN;
                        Matrix matrix2 = Rmsd.GetRmsdAndTransform(chain2, cStart, cEnd, chain1, nStart, nEnd, out rmsd2);
                        bool fail2 = rmsd2 > maxRmsd;
                        Trace.Assert(fail == fail2);
#endif

                        if (rmsd > maxRmsd)
                            continue;

                        TransformSequenceAlignment alignment = new TransformSequenceAlignment(nStart, nEnd, cStart, cEnd);
                        alignment.Centroid1 = Geometry.GetCenterNCAC(chain1[nStart, nEnd]);
                        alignment.Centroid2 = Geometry.GetCenterNCAC(chain2[cStart, cEnd]);
                        alignment.Align1 = matrix;
                        alignment.Align2 = Matrix.Invert(matrix);
                        alignments.Add(alignment);

                    }
                }
            }

            return alignments;
        }

        /// <summary>
        /// Find splicing alignment ranges for two peptides that would retain a full secondary structure block for at least one of the two peptides. This prevents
        /// short splice overlaps that remove some of the secondary structure on both sides of the alignment. Basically, the alignment must result in either:
        ///   1) one secondary structure block being fully included in the other
        ///   2) the N-terminal peptide being spliced has the end of its block fully in the resultant alignment, thus preserving the entire N-side block
        ///   3) the C-terminal peptide being spliced has the start of its block fully in the resultant alignment, thus preserving the entire C-side block
        /// </summary>
        /// <param name="n"></param>
        /// <param name="c"></param>
        /// <param name="allowedTypes"></param>
        /// <param name="minAlignmentLength"></param>
        /// <returns></returns>
        public static List<SequenceAlignment> GetAlignmentsPreservingFullSsBlocks(IChain n, IChain c, SS allowedTypes, int minAlignmentLength = DefaultMinAlignmentLength, float maxRmsd = DefaultRmsdThreshold)
        {
            // TODO!!!: Debug why calling this function with peptides A,B vs B,A returns different numbers of alignments.
            List<SequenceAlignment> alignments = new List<SequenceAlignment>();
            List<SSBlock> nBlocks = SecondaryStructure.GetPhiPsiSSBlocksOfType(n, allowedTypes, minAlignmentLength).Where(block => block.Length >= minAlignmentLength).ToList();
            List<SSBlock> cBlocks = SecondaryStructure.GetPhiPsiSSBlocksOfType(c, allowedTypes, minAlignmentLength).Where(block => block.Length >= minAlignmentLength).ToList();
            foreach (SSBlock nBlock in nBlocks)
            {
                foreach (SSBlock cBlock in cBlocks)
                {
                    for(int nAlignmentOfBlockC = nBlock.End - minAlignmentLength + 1; nAlignmentOfBlockC + cBlock.Length > nBlock.Start + minAlignmentLength; nAlignmentOfBlockC--)
                    {
                        // Figure out the start and ends of the overlap region
                        int nStart = Math.Max(nBlock.Start, nAlignmentOfBlockC);
                        int nEnd = Math.Min(nBlock.End, nAlignmentOfBlockC + cBlock.Length - 1);
                        int alignmentLength = nEnd - nStart + 1;

                        int cStart = Math.Max(cBlock.Start, cBlock.Start + (nStart - nAlignmentOfBlockC));
                        int cEnd = cStart + alignmentLength - 1;

                        Debug.Assert(nBlock.Start <= nStart && nEnd <= nBlock.End);
                        Debug.Assert(cBlock.Start <= cStart && cEnd <= cBlock.End);
                        Debug.Assert(nEnd - nStart == cEnd - cStart);

                        bool cFullyIncluded = (cBlock.Start == cStart && cBlock.End == cEnd);
                        bool nFullyIncluded = (nBlock.Start == nStart && nBlock.End == nEnd);

                        if (!nFullyIncluded && !cFullyIncluded && cStart != cBlock.Start && nEnd != nBlock.End)
                            continue;

                        if (Rmsd.GetRmsdNCAC(n, nStart, nEnd, c, cStart, cEnd) > maxRmsd)
                            continue;

                        alignments.Add(new SequenceAlignment(nStart, nEnd, cStart, cEnd));
                    }
                }
            }

            return alignments;
        }

        public static List<TransformSequenceAlignment> GetRepeatFilteredTransformAlignments(
            IStructure structure1, int chainIndex1, bool fuseEndsOnly1, 
            IStructure structure2, int chainIndex2, bool fuseEndsOnly2,
            SS allowedTypes, int minAlignmentLength = DefaultMinAlignmentLength, float maxRmsd = DefaultRmsdThreshold)
        {
            IChain chain1 = structure1[chainIndex1];
            IChain chain2 = structure2[chainIndex2];
            List<TransformSequenceAlignment> alignments = GetRepeatFilteredTransformAlignments(chain1, fuseEndsOnly1, chain2, fuseEndsOnly2, allowedTypes, minAlignmentLength, maxRmsd);
            alignments.ForEach(a => { a.ChainIndex1 = chainIndex1; a.ChainIndex2 = chainIndex2; });
            return alignments;

        }

        public static List<TransformSequenceAlignment> GetRepeatFilteredTransformAlignments(
            IChain chain1, bool fuseEndsOnly1, 
            IChain chain2, bool fuseEndsOnly2, 
            SS allowedTypes, int minAlignmentLength = DefaultMinAlignmentLength, float maxRmsd = DefaultRmsdThreshold)
        {
            List<TransformSequenceAlignment> alignments = GetTransformAlignments(chain1, chain2, allowedTypes, minAlignmentLength, maxRmsd);

            if (fuseEndsOnly1)
            {
                int repeatLength1;
                if (Sequence.TryGetInternalRepeatLength(chain1, out repeatLength1) && repeatLength1 > 15)
                {
                    alignments.RemoveAll(a => (a.Range1.Start > 1.15 * repeatLength1) && (a.Range1.End < chain1.Count - 1.15 * repeatLength1));
                }
            }

            if (fuseEndsOnly2)
            {
                int repeatLength2;
                if (Sequence.TryGetInternalRepeatLength(chain2, out repeatLength2) && repeatLength2 > 15)
                {
                    alignments.RemoveAll(a => (a.Range2.Start > 1.15 * repeatLength2) && (a.Range2.End < chain1.Count - 1.15 * repeatLength2));
                }
            }

            return alignments;
        }

        public static List<SequenceAlignment> GetFilteredSequenceAlignments(IChain n, bool nKeepRepeats, IChain c, bool cKeepRepeats, SS allowedTypes, int minAlignmentLength = DefaultMinAlignmentLength, float maxRmsd = DefaultRmsdThreshold)
        {
            List<SequenceAlignment> alignments = GetAlignmentsPreservingFullSsBlocks(n, c, allowedTypes, minAlignmentLength, maxRmsd);
            int maxThirdHelixShortening = DefaultMaxThirdHelixTrimming;
            //int maxOvershoot = 8;

            // Find all alignments and remove those that would:
            // -- remove more than one repeat of a repeat protein
            if(nKeepRepeats)
            {
                int nRepeatLength;
                if (Sequence.TryGetInternalRepeatLength(n, out nRepeatLength) && nRepeatLength > 15)
                {
                    alignments.RemoveAll(a => a.Range1.End < n.Count - 1.25 * nRepeatLength);
                }
            }
            if (cKeepRepeats)
            {
                int cRepeatLength;
                if (Sequence.TryGetInternalRepeatLength(c, out cRepeatLength) && cRepeatLength > 15)
                {
                    alignments.RemoveAll(a => a.Range1.Start < 1.25 * cRepeatLength);
                }
            }

            // -- leave two or fewer secondary structure elements in either chain. An exception is made if there are only two secondary structure 
            // elements total to begin with, which is common for helical bundles
            List<SSBlock> nBlocks = SecondaryStructure.GetPhiPsiSSBlocksOfType(n, SS.Helix | SS.Extended, minAlignmentLength);
            List<SSBlock> cBlocks = SecondaryStructure.GetPhiPsiSSBlocksOfType(c, SS.Helix | SS.Extended, minAlignmentLength);

            if (nBlocks.Count > 2)
            {
                alignments.RemoveAll(a => a.Range1.Start < nBlocks[1].End);
            }
            if (cBlocks.Count > 2)
            {
                alignments.RemoveAll(a => a.Range2.End > cBlocks[cBlocks.Count - 2].Start);
            }

            // -- shorten the third helix of a 3-helix bundle too much, because that would also probably destabilize the oligomer
            if (nBlocks.Count > 2)
            {
                int removeCount = alignments.RemoveAll(a => a.Range1.End < nBlocks[2].End - maxThirdHelixShortening);
            }
            if (cBlocks.Count > 2)
            {
                int removeCount = alignments.RemoveAll(a => a.Range2.Start > cBlocks[cBlocks.Count - 3].Start + maxThirdHelixShortening);
            }

            return alignments;
        }

        public static IChain GetPeptideWithMinimizedClashes(IChain nChain, IChain cChain, SequenceAlignment alignment, IEnumerable<int>[] immutableResidues = null)
        {
            Selection selection = new Selection();

            if (immutableResidues != null)
            {
                Trace.Assert(immutableResidues.Length == 2);
                selection.Aas.UnionWith(immutableResidues[0].Select(index => nChain[index]));
                selection.Aas.UnionWith(immutableResidues[1].Select(index => cChain[index]));
            }

            return GetChain(new IChain[] { nChain, cChain }, new SequenceAlignment[] { alignment }, selection);
        }

        public static IChain GetChain(IChain[] chains, SequenceAlignment[] alignments, IEnumerable<int>[] immutableResidues)
        {
            Trace.Assert(chains != null && chains.Length > 0);
            Trace.Assert(alignments != null && alignments.Length == chains.Length - 1);

            Selection selection = new Selection();

            if (immutableResidues != null)
            {
                Trace.Assert(immutableResidues.Length == chains.Length);
                for(int i = 0; i < chains.Length; i++)
                {
                    selection.Aas.UnionWith(immutableResidues[i].Select(index => chains[i][index]));
                }
            }

            return GetChain(chains, alignments, selection);
        }

        public static IChain GetChain(IChain[] peptides, SequenceAlignment[] alignments, Selection immutableAas)
        {
            IChain fusion = new Chain();

            // Do all pairwise analysis
            for(int i = 0; i < peptides.Length - 1; i++)
            {
                // Determine the ranges outside of the splice
                int start1 = i == 0 ? 0 : alignments[i - 1].Range2.End + 1;
                int end1 = alignments[i].Range1.Start - 1;
                int start2 = alignments[i].Range2.End + 1;
                int end2 = i < alignments.Length - 1 ? alignments[i + 1].Range1.Start - 1 : peptides[i + 1].Count - 1;

                // Add the non-overlapping region of the first peptide
                if (start1 <= end1)
                {
                    foreach(Aa aa in peptides[i][start1, end1])
                    {
                        Aa copy = new Aa(aa, i == 0 && start1 == 0, false);
                        copy.NodeTransform = aa.TotalTransform;
                        fusion.Add(copy);
                    }
                }
                
                // Add the alignment region, selecting either from the first or second peptide so as to minimize clashes with the sidechains that
                // are for sure being kept on either side
                SequenceAlignment alignment = alignments[i];
                Debug.Assert(alignment.Range1.Length == alignment.Range2.Length);
                for (int alignmentOffset = 0; alignmentOffset < alignment.Range1.Length; alignmentOffset++)
                {
                    int index1 = alignment.Range1.Start + alignmentOffset;
                    int index2 = alignment.Range2.Start + alignmentOffset;
                    IAa option1 = peptides[i][index1];
                    IAa option2 = peptides[i + 1][index2];
                    bool nTerminus = i == 0 && index1 == 0;
                    bool cTerminus = (i == peptides.Length - 2) && (index2 == peptides[i + 1].Count - 1);

                    if(immutableAas.Aas.Contains(option1))
                    {
                        Aa copy = new Aa(option1, nTerminus, cTerminus);
                        copy.NodeTransform = option1.TotalTransform;
                        fusion.Add(copy);
                    }
                    else if (immutableAas.Aas.Contains(option2))
                    {
                        Aa copy = new Aa(option2, nTerminus, cTerminus);
                        copy.NodeTransform = option2.TotalTransform;
                        fusion.Add(copy);
                    }
                    else
                    {
                        if(option2.Letter == 'P' && index1 >= 4)
                        { 
                            SS[] ss1 = SecondaryStructure.GetPhiPsiSS(peptides[i], 5);

                            bool allHelical = (ss1[index1] | ss1[index1 - 1] | ss1[index1 - 2] | ss1[index1 - 3] | ss1[index1 - 4]) == SS.Helix;
                            if (allHelical)
                            {
                                Aa copy = new Aa(option1, nTerminus, cTerminus);
                                copy.NodeTransform = option1.TotalTransform;
                                fusion.Add(copy);
                                continue;
                            }
                        }

                        // Otherwise, select the residue with fewer clashes
                        int clashCount1 = end2 >= start2? peptides[i + 1][start2, end2].Select(other => Clash.AnyContact(other, option1, Clash.ContactType.SidechainSidechainClash) ? 1 : 0).Aggregate(0, (a, b) => a + b) : 0;
                        int clashCount2 = end1 >= start1? peptides[i][start1, end1].Select(other => Clash.AnyContact(other, option2, Clash.ContactType.SidechainSidechainClash) ? 1 : 0).Aggregate(0, (a, b) => a + b) : 0;

                        if (clashCount1 <= clashCount2)
                        {
                            Aa copy = new Aa(option1, nTerminus, cTerminus);
                            copy.NodeTransform = option1.TotalTransform;
                            fusion.Add(copy);
                            continue;
                        }

                        if (clashCount2 < clashCount1)
                        {
                            Aa copy = new Aa(option2, nTerminus, cTerminus);
                            copy.NodeTransform = option2.TotalTransform;
                            fusion.Add(copy);
                            continue;
                        }
                    }
                }

                // Add the non-overlapping region of the last peptide
                if(i == peptides.Length - 2 && start2 <= end2)
                {
                    foreach (Aa aa in peptides[i + 1][start2, end2])
                    {
                        Aa copy = new Aa(aa, false, aa.IsCTerminus);
                        copy.NodeTransform = aa.TotalTransform;
                        fusion.Add(copy);
                    }
                }
            }
            return fusion;
        }

        public static void GetIdentityRanges(IChain[] peptides, SequenceAlignment[] sequenceAlignment, out Range[] originalRanges, out Range[] finalRanges)
        {
            Debug.Assert(peptides.Length == sequenceAlignment.Length + 1);

            originalRanges = new Range[sequenceAlignment.Length + 1];
            finalRanges = new Range[sequenceAlignment.Length + 1];

            int length = 0; // length of peptide as it has additional peptides are spliced in
            for(int i = 0; i < peptides.Length; i++)
            {
                originalRanges[i] = new Range(i == 0 ? 0 : sequenceAlignment[i - 1].Range2.End + 1, i == peptides.Length - 1? peptides[i].Count - 1 : sequenceAlignment[i].Range1.Start - 1);
                finalRanges[i] = new Range(length, length + originalRanges[i].Length -1);

                // Add the identity region and subsequence splice region, if there is one
                length += originalRanges[i].Length;

                if(i < peptides.Length - 1)
                {
                    length += sequenceAlignment[i].Length;
                }
            }
        }

        /// <summary>
        /// Returns sequence alignments that could join all of the input structures
        /// </summary>
        /// <param name="structures"></param>
        /// <param name="chains">IChain indices for each structure that should be </param>
        /// <returns></returns>
        public static TransformSequenceAlignment[][] GetRepeatFilteredTransformAlignments(IStructure[] structures, int[][] chains)
        {
            TransformSequenceAlignment[][] result = new TransformSequenceAlignment[structures.Length - 1][];
            for (int i = 0; i < structures.Length - 1; i++)
            {
                List<TransformSequenceAlignment> alignments = new List<TransformSequenceAlignment>();
                foreach(int chainIndex1 in chains[i])
                {
                    foreach (int chainIndex2 in chains[i + 1])
                    {
                        IChain chain1 = structures[i][chainIndex1];
                        IChain chain2 = structures[i + 1][chainIndex2];

                        // Shorten the 
                        List<TransformSequenceAlignment> chainAlignments = GetRepeatFilteredTransformAlignments(structures[i], chainIndex1, false, structures[i + 1], chainIndex2, i != structures.Length - 2 && structures[i + 1].Count == 1, SS.Helix | SS.Extended);
                        foreach (TransformSequenceAlignment alignment in chainAlignments)
                        {
                            alignment.ChainIndex1 = chainIndex1;
                            alignment.ChainIndex2 = chainIndex2;
                        }
                        alignments.AddRange(chainAlignments);
                    }
                }
                result[i] = alignments.ToArray();
            }
            return result;
        }

        /// <summary>
        /// Outputs a structure that is a fusion of the chains indicated by the sequence alignments. In the case of a cycle (wherein the last structure
        /// is a copy of the first at a different position), only one set of chains for the two endpoint structures is included.
        /// </summary>
        /// <param name="structures">structures to fuse</param>
        /// <param name="alignments">the sequence positions at which to fuse</param>
        /// <param name="ncDirections">the directionality of the alignment: true -> the first structure is N-terminal, false -> C-terminal</param>
        /// <param name="cycle">whether the structure forms a cycle, in which case the first and last fusions affect both ends</param>
        /// <param name="partialChain">a partial chain has been created due to cyclization whose entirety will be restored only through asu pattering</param>
        /// <returns></returns>
        public static IStructure GetStructure(IStructure[] structures, SequenceAlignment[] alignments, bool[] ncDirections, bool cycle, out IChain partialChain)
        {
            partialChain = null;

            // Note: The fusion product's middle residue from the alignment range comes from the N-terminal chain, i.e the N-term range is [0, Middle] and C-term is [Middle + 1, Len-1]
            Trace.Assert(structures != null && alignments != null && ncDirections != null);
            Trace.Assert(structures.Length > 0);
            Trace.Assert(structures.Length == alignments.Length + 1);
            Trace.Assert(alignments.Length == ncDirections.Length);

            // Data for the cyclization case - the chain that the final fusion should be joined with fused back onto fusion products of the first chain should be tracked so that the last chain can be fused back onto that chain
            IChain cycleDoubleFusedChain = null; // Track what chain the first fusion chain ends up in
            bool isCycleDoubleFused = cycle && alignments.First().ChainIndex1 == alignments.Last().ChainIndex2; // Whether the first chain is fused both to the next chain and to the last/prior (wrap-around) chain
            Matrix cycleAlignment = isCycleDoubleFused? Rmsd.GetRmsdTransform(structures.First()[0][0], structures.Last()[0][0]) : Matrix.Identity;


            // Mark which aas to remove without actually removing them, so that their indices remain unchanged while 
            // those to be removed are being computed
            Selection remove = new Selection();
            for(int alignmentIndex = 0; alignmentIndex < alignments.Length; alignmentIndex++)
            {
                bool ncDirection = ncDirections[alignmentIndex];
                SequenceAlignment alignment = alignments[alignmentIndex];
                SequenceAlignment ncAlignment = ncDirection? alignment : SequenceAlignment.Reversed(alignment); // N as chain1
                IStructure structureN = ncDirection ? structures[alignmentIndex] : structures[alignmentIndex + 1];
                IStructure structureC = ncDirection ? structures[alignmentIndex + 1] : structures[alignmentIndex];
                IChain chainN = structureN[ncAlignment.ChainIndex1];
                IChain chainC = structureC[ncAlignment.ChainIndex2];
                remove.Aas.UnionWith(chainN[ncAlignment.Range1.Middle + 1, chainN.Count - 1]);
                remove.Aas.UnionWith(chainC[0, ncAlignment.Range2.Middle]);

                // If a cycle exists, then for each chain in the first and last structure (which are identical, modulo a transform), only one copy should be
                // preserved. If that copy is fused on both sides, then it must be trimmed according to both the first and final sequence alignments. In such a
                // case, remove the entire last structure.
                // Cycle and fusion is to the same chain on both sides:
                // -> remove all of the second copy
                // -> transform the second copy neighbor chain onto the first
                // Cycle and fusion is to separate chains on either side:
                // -> remove all of the second copy except for the fused chain
                // -> remove from the first copy the chain that was fused on the second copy
                if(cycle && alignmentIndex == alignments.Length - 1)
                {
                    if (isCycleDoubleFused)
                    {
                        // Mark all chains from second copy for deletion
                        foreach(IChain chain in structures[structures.Length - 1])
                        {
                            remove.Aas.UnionWith(chain);
                        }

                        // Mark the first structure residues for deletion on one side of the fusion point
                        IChain chain0 = structures[0][alignment.ChainIndex2];
                        if (ncDirection)
                            remove.Aas.UnionWith(chain0[0, alignment.Range2.Middle]);
                        else
                            remove.Aas.UnionWith(chain0[alignment.Range2.Middle + 1, chain0.Count - 1]);
                    }
                    else
                    {
                        // Mark all chains from the second copy for deletion, except the one being fused
                        IChain keep = structures[alignmentIndex + 1][alignment.ChainIndex2];
                        foreach (IChain chain in structures[alignmentIndex + 1])
                        {
                            if (chain != keep)
                                remove.Aas.UnionWith(chain);
                        }

                        // For the one being fused, remove that index from the first structure
                        remove.Aas.UnionWith(structures[0][alignment.ChainIndex2]);
                    }
                }
            }

            // Remove them
            for (int alignmentIndex = 0; alignmentIndex < alignments.Length; alignmentIndex++)
            {
                SequenceAlignment alignment = alignments[alignmentIndex];
                IStructure structure1 = structures[alignmentIndex];
                IStructure structure2 = structures[alignmentIndex + 1];
                IChain chain1 = structure1[alignment.ChainIndex1];
                IChain chain2 = structure2[alignment.ChainIndex2];

                for(int i = chain1.Count - 1; i >= 0; i--)
                {
                    IAa aa = chain1[i];
                    if(remove.Aas.Contains(aa))
                    {
                        Matrix desired = aa.TotalTransform;
                        chain1.RemoveAt(i);
                        aa.Transform(desired * Matrix.Invert(aa.TotalTransform));
#if DEBUG_TRANSFORMS
                        Debug.Assert((desired - aa.TotalTransform).Translation.Length() < 0.1);
#endif
                    }
                }

                for (int i = chain2.Count - 1; i >= 0; i--)
                {
                    IAa aa = chain2[i];
                    if (remove.Aas.Contains(aa))
                    {
                        Matrix desired = aa.TotalTransform;
                        chain2.RemoveAt(i);
                        aa.Transform(desired * Matrix.Invert(aa.TotalTransform));
#if DEBUG_TRANSFORMS
                        Debug.Assert((desired - aa.TotalTransform).Translation.Length() < 0.1);
#endif
                    }
                }

                if (cycle && alignmentIndex == 0)
                {
                    foreach(IChain chain in structure1)
                    {
                        for (int i = chain.Count - 1; i >= 0; i--)
                        {
                            IAa aa = chain[i];
                            if (remove.Aas.Contains(aa))
                            {
                                Matrix desired = aa.TotalTransform;
                                chain.RemoveAt(i);
                                aa.Transform(desired * Matrix.Invert(aa.TotalTransform));
#if DEBUG_TRANSFORMS
                                Debug.Assert((desired - aa.TotalTransform).Translation.Length() < 0.1);
#endif
                            }
                        }
                    }
                }

                if (cycle && alignmentIndex == alignments.Length - 1)
                {
                    foreach (IChain chain in structure2)
                    {
                        for (int i = chain.Count - 1; i >= 0; i--)
                        {
                            IAa aa = chain[i];
                            if (remove.Aas.Contains(aa))
                            {
                                Matrix desired = aa.TotalTransform;
                                chain.RemoveAt(i);
                                aa.Transform(desired * Matrix.Invert(aa.TotalTransform));
#if DEBUG_TRANSFORMS
                                Debug.Assert((desired - aa.TotalTransform).Translation.Length() < 0.1);
#endif
                            }
                        }
                    }
                }
            }

            // Join the chains and allocate the result to the second structure so as to preserve the indexes for the next fusion step
            for (int alignmentIndex = 0; alignmentIndex < alignments.Length; alignmentIndex++)
            {
                bool ncDirection = ncDirections[alignmentIndex];
                SequenceAlignment alignment = alignments[alignmentIndex];
                IStructure structure1 = structures[alignmentIndex];
                IStructure structure2 = structures[alignmentIndex + 1];
                IChain chain1 = structure1[alignment.ChainIndex1];
                IChain chain2 = structure2[alignment.ChainIndex2];

                if (ncDirection)
                {
#if DEBUG_MIRRORS
                    foreach(IAa aa2 in chain2.ToArray())
                    {
                        chain1.AddInPlace(aa2);
                    }
#else
                    chain1.AddArraySourceInPlace(chain2);
#endif
                    structure2[alignment.ChainIndex2, true] = chain1;
                }
                else
                {
#if DEBUG_MIRRORS
                    foreach (IAa aa1 in chain1.ToArray())
                    {
                        chain2.AddInPlace(aa1);
                    }
#else
                    chain2.AddArraySourceInPlace(chain1);
#endif
                }

                // Track which chain contains the first chain in the cycle so that the final chain in the cycle can fuse to it
                if (isCycleDoubleFused && (alignmentIndex == 0 || chain1 == cycleDoubleFusedChain || chain2 == cycleDoubleFusedChain))
                {
                    cycleDoubleFusedChain = ncDirection? chain1 : chain2;
                }

                if (isCycleDoubleFused && alignmentIndex == alignments.Length - 1)
                {
                    // If it's a cycle on the same chain, move the chain back to where it fuses to the first structure
                    IChain combined = ncDirection ? chain1 : chain2;
                    combined.Transform(cycleAlignment);

                    if (combined == cycleDoubleFusedChain)
                    {
                        // If the structure[0] fusion chain has been fused all the way through, there is no need to move the
                        // current chain to meet the first, since they're already joined
                        partialChain = cycleDoubleFusedChain;
                    }
                    else
                    {
                        if(ncDirection)
                        {
                            combined.AddArraySourceInPlace(cycleDoubleFusedChain);
                            IStructure cycleDoubleFusedParent = (IStructure) cycleDoubleFusedChain.Parent;
                        }
                        else
                        {
                            cycleDoubleFusedChain.AddArraySourceInPlace(combined);
                        }
                    }
                }
            }

            // Add all unique chains to a new structure
            Structure total = new Structure();
            structures.SelectMany(s => s).Distinct().Where(c => c.Count > 0).ToList().ForEach(c => total.AddInPlace(c));
            return total;
        }

        /// <summary>
        /// Tests whether either N fused to C or C fused to N has a valid combination. If both are being tested, the function
        /// returns whether either has a valid combination
        /// </summary>
        /// <param name="multiplicity"></param>
        /// <param name="cn"></param>
        /// <param name="other"></param>
        /// <param name="alignment"></param>
        /// <param name="ntermCnTest"></param>
        /// <param name="ctermCnTest"></param>
        /// <param name="minSubstructureLength"></param>
        /// <returns></returns>
        public static bool TestClashCn(int multiplicity, IStructure cn, IStructure other, TransformSequenceAlignment alignment, bool ntermCnTest = true, bool ctermCnTest = true, int minSubstructureLength = 0)
        {
            Trace.Assert(minSubstructureLength >= 0);

            IChain cnChain = cn[alignment.ChainIndex1];
            IChain otherChain = other[alignment.ChainIndex2];

            if (ntermCnTest)
            {
                int finalOtherIndex = Math.Min(otherChain.Count - 1, alignment.Range2.End + minSubstructureLength - 1);
                Trace.Assert(alignment.Range1.Start < cnChain.Count);
                Trace.Assert(0 <= alignment.Range2.End && alignment.Range2.End < otherChain.Count && finalOtherIndex < otherChain.Count);
                IChain copyCn = new Chain(cnChain[0, alignment.Range1.Start]);
                IChain copyOther = new Chain(otherChain[alignment.Range2.End, finalOtherIndex], true);

                copyOther.Transform(alignment.Align2);

                if (!Clash.AnyContact(copyCn, copyOther, Clash.ContactType.MainchainMainchainClash))
                {
                    copyCn.AddRange(copyOther);


                    AxisPattern<IChain> pattern = new AxisPattern<IChain>(Vector3.UnitZ, multiplicity, copyCn);
                    IChain[] neighbors = pattern[1, multiplicity - 1].ToArray();

                    //IChain[] pattern = CxUtilities.Pattern(copyCn, Vector3.UnitZ, multiplicity, new int[] { 0 }, true);

                    if (!Clash.AnyContact(new IChain[] { copyCn }, neighbors, Clash.ContactType.MainchainMainchainClash))
                        return false;
                }
            }

            if (ctermCnTest)
            {
                int firstOtherIndex = Math.Max(0, alignment.Range2.Start - minSubstructureLength + 1);
                Trace.Assert(0 <= alignment.Range1.End && alignment.Range1.End < cnChain.Count);
                Trace.Assert(0 <= firstOtherIndex && firstOtherIndex < otherChain.Count && alignment.Range2.Start < otherChain.Count);
                IChain copyCn = new Chain(cnChain[alignment.Range1.End, cnChain.Count - 1]);
                IChain copyOther = new Chain(otherChain[firstOtherIndex, alignment.Range2.Start], true);
                copyOther.Transform(alignment.Align2);

                if (!Clash.AnyContact(copyCn, copyOther, Clash.ContactType.MainchainMainchainClash))
                {
                    copyCn.AddRange(copyOther);

                    AxisPattern<IChain> pattern = new AxisPattern<IChain>(Vector3.UnitZ, multiplicity, copyCn);
                    IChain[] neighbors = pattern[1, multiplicity - 1].ToArray();

                    //IChain[] pattern = CxUtilities.Pattern(copyCn, Vector3.UnitZ, multiplicity, new int[] { 0 }, true);

                    if (!Clash.AnyContact(new IChain[] { copyCn }, neighbors, Clash.ContactType.MainchainMainchainClash))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Given a set of structure and alignments between chains, and directionality to identify which structure contributes the N and C-terminus, 
        /// return selections corresponding to each block of structure that is left remaining between fusion regions, and selections constituting the fusion
        /// regions themselves.
        /// </summary>
        /// <param name="structures"></param>
        /// <param name="alignments"></param>
        /// <param name="ncDirections"></param>
        /// <param name="blocks"></param>
        /// <param name="junctions"></param>
        public static void GetSelections(IStructure[] structures, SequenceAlignment[] alignments, bool[] ncDirections, out Selection[] blocks, out Selection[] junctions, out Selection[] removed)
        {
            junctions = new Selection[alignments.Length];
            blocks = new Selection[structures.Length];
            removed = new Selection[structures.Length];

            // Mark which aas to remove without actually removing them at first, so that their indices remain unchanged while 
            // those to be removed are being computed
            Selection remove = new Selection();

            // Determine junction residues and residues that are removed due to splicing
            for (int alignmentIndex = 0; alignmentIndex < alignments.Length; alignmentIndex++)
            {
                Selection junction = new Selection();
                bool ncDirection = ncDirections[alignmentIndex];
                SequenceAlignment alignment = ncDirection ? alignments[alignmentIndex] : SequenceAlignment.Reversed(alignments[alignmentIndex]); // N as chain1
                IStructure structureN = ncDirection ? structures[alignmentIndex] : structures[alignmentIndex + 1];
                IStructure structureC = ncDirection ? structures[alignmentIndex + 1] : structures[alignmentIndex];
                IChain chainN = structureN[alignment.ChainIndex1];
                IChain chainC = structureC[alignment.ChainIndex2];
                remove.Aas.UnionWith(chainN[alignment.Range1.Middle + 1, chainN.Count - 1]);
                remove.Aas.UnionWith(chainC[0, alignment.Range2.Middle]);
                junction.Aas.UnionWith(chainN[alignment.Range1.Start, alignment.Range1.Middle]);
                junction.Aas.UnionWith(chainC[alignment.Range2.Middle + 1, alignment.Range2.End]);
                junctions[alignmentIndex] = junction;
            }

            // Determine blocks as the set of aas in the original structure, minus junction residues and residues that are lost from fusion
            for(int structureIndex = 0; structureIndex < structures.Length; structureIndex++)
            {
                IStructure structure = structures[structureIndex];
                Selection block = new Selection(structure.SelectMany(chain => chain));
                removed[structureIndex] = Selection.Intersect(block, remove);
                block.ExceptWith(remove);
                

                if (structureIndex > 0)
                    block.ExceptWith(junctions[structureIndex - 1]);

                if(structureIndex < junctions.Length)
                    block.ExceptWith(junctions[structureIndex]);

                blocks[structureIndex] = block;
            }
        }
    }
}
