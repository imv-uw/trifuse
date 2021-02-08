using Core;
using Core.Interfaces;
using Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tools
{
    public class AlignmentInfo
    {
        public AlignmentInfo(IEnumerable<IChain> peptides, IEnumerable<Range> sequenceRanges)
        {
            Peptides = peptides.ToArray();
            SequenceRanges = sequenceRanges.ToArray();
        }

        public AlignmentInfo(IChain peptide1, IChain peptide2, Range range1, Range range2)
        {
            Peptides = new IChain[] { peptide1, peptide2 };
            SequenceRanges = new Range[] { range1, range2 };
        }

        public IChain[] Peptides;
        public Range[] SequenceRanges;
    }

    public class Sequence
    {
        public static List<Range> GetIdenticalRepeatBlocks(IChain peptide)
        {
            string sequence = peptide.GetSequence1();
            return GetIdenticalRepeatBlocks(sequence);
        }

        public static bool TryGetInternalRepeatLength(IChain peptide, out int length)
        {
            List<Range> repeats = GetIdenticalRepeatBlocks(peptide);
            if (repeats.Count >= 4)
            {
                length = repeats[2].Start - repeats[1].Start;
                return true;
            }
                
            if (repeats.Count >= 2)
            {
                length = repeats[1].Start - repeats[1].Start;
                return true;
            }


            // Nothing found
            length = -1;
            return false;
        }

        /// <summary>
        /// This function is sort of like BLAST in that it finds matches/repeats of a minimum length and then extends
        /// them as much as possible. Unlike BLAST, an exact match is required for lengthening a hit.
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="minimumRepeatLength"></param>
        /// <returns></returns>
        public static List<Range> GetIdenticalRepeatBlocks(string sequence, int minimumRepeatLength = 10)
        {
            List<Range> blocks = new List<Range>();

            for(int i = 0; i < sequence.Length - minimumRepeatLength; i++)
            {
                // Identify repeats of minimum length
                string template = sequence.Substring(i, minimumRepeatLength);
                List<int> repeatLocations = new List<int>();
                for(int j = i + minimumRepeatLength; j < sequence.Length - minimumRepeatLength;)
                {
                    string repeat = sequence.Substring(j, minimumRepeatLength);
                    if (template != repeat)
                    {
                        j++;
                        continue;
                    }
                    
                    repeatLocations.Add(j);
                    j += minimumRepeatLength;
                }

                if (repeatLocations.Count == 0)
                    continue;

                // Expand the repeats towards the C-terminus. There's no point in extending them the other way, since no repeats
                // were detected earlier
                bool extensionFailed = false;
                int actualRepeatLength = minimumRepeatLength;
                while(!extensionFailed)
                {
                    int tryRepeatLength = actualRepeatLength + 1;

                    // Check that extension doesn't pass the C terminus
                    if (sequence.Length <= repeatLocations.Last() + tryRepeatLength) 
                        break;

                    // Check that extension doesn't span multiple repeats
                    if (repeatLocations.First() <= i + tryRepeatLength - 1)
                        break;

                    // Check that each repeat can be extended while matching the original sequence
                    foreach(int copyLocation in repeatLocations)
                    {
                        int endLocationOriginal = i + tryRepeatLength - 1;
                        int endLocationCopy = copyLocation + tryRepeatLength - 1;
                        if(sequence[endLocationOriginal] == sequence[endLocationCopy])
                            continue;

                        extensionFailed = true;
                        break;
                    }

                    if(!extensionFailed)
                        actualRepeatLength = tryRepeatLength;
                }

                blocks.Add(new Range(i, i + actualRepeatLength - 1));
                foreach(int copyLocation in repeatLocations)
                {
                    blocks.Add(new Range(copyLocation, copyLocation + actualRepeatLength - 1));
                }
                return blocks;
            }
            return blocks;
        }

        public static List<AlignmentInfo> GetSsAlignmentsByRmsd(Chain peptide1, Chain peptide2, SS secondaryStructure, int minimumAlignmentLength, float maxRmsd)
        {
            List<AlignmentInfo> alignments = new List<AlignmentInfo>();
            List<SS> ss1 = Tools.SecondaryStructure.GetPhiPsiSS(peptide1, minimumAlignmentLength).ToList();
            List<SS> ss2 = Tools.SecondaryStructure.GetPhiPsiSS(peptide2, minimumAlignmentLength).ToList();

            // Slide the repeat sequence against the oligomer1 sequence and find helix-helix segments
            for (int alignmentOffset2 = minimumAlignmentLength - ss2.Count; alignmentOffset2 < ss1.Count - minimumAlignmentLength; alignmentOffset2++)
            {
                int maxRangeEnd1Added = -1;
                for (int rangeStart1 = Math.Max(0, -alignmentOffset2); rangeStart1 < Math.Min(ss1.Count - minimumAlignmentLength, ss2.Count - minimumAlignmentLength - alignmentOffset2); rangeStart1++)
                {
                    // Grow the alignment length until SS no longer matches for the entire alignment, RMSD gets too big, or the sequence terminates
                    int alignmentLength = minimumAlignmentLength;
                    while (true)
                    {
                        int rangeStart2 = rangeStart1 + alignmentOffset2;
                        int rangeEnd2 = rangeStart2 + alignmentLength - 1;
                        int rangeEnd1 = rangeStart1 + alignmentLength - 1;
                        bool allowableRangeExceeded = ss1.Count <= rangeEnd1 || ss2.Count <= rangeEnd2;
                        bool onlyContainsDesiredSs = allowableRangeExceeded ? false :
                            ss1.GetRange(rangeStart1, alignmentLength).Select(ss => ss == secondaryStructure).Aggregate((a, b) => a && b) &&
                            ss2.GetRange(rangeStart2, alignmentLength).Select(ss => ss == secondaryStructure).Aggregate((a, b) => a && b);
                        bool rmsdExceeded = allowableRangeExceeded ? false :
                            Rmsd.GetRmsdNCAC(peptide1, rangeStart1, rangeEnd1, peptide2, rangeStart2, rangeEnd2) > maxRmsd;

                        if (allowableRangeExceeded || rmsdExceeded || !onlyContainsDesiredSs)
                        {
                            if(alignmentLength > minimumAlignmentLength && rangeEnd1 - 1 > maxRangeEnd1Added)
                            {
                                // This alignment failed but the previous one did not. Add the previous one - **iff it is not a subset of a previous alignment
                                AlignmentInfo alignment = new AlignmentInfo(peptide1, peptide2, new Range(rangeStart1, rangeEnd1 - 1), new Range(rangeStart2, rangeEnd2 - 1));
                                alignments.Add(alignment);
                                maxRangeEnd1Added = rangeEnd1 - 1;
                            }

                            // If failure occurs due to rmsd being exceeded, start again with (rangeStart + 1) because it will result in a slightly different alignment orientation
                            // If failure occurs due to mismatching SS, start again with (rangeStart  +1) because all alignments including (rangeStart) will fail
                            // Otherwise, start at (rangeEnd + 1) because all other intermediate alignments will be identical (within the maxRmsd tolerance)
                            if (alignmentLength > minimumAlignmentLength && !rmsdExceeded)
                            {
                                rangeStart1 = rangeEnd1;
                            }
                            
                            break;
                        }

                        alignmentLength++;
                    }
                }
            }
            return alignments;
        }

        public static double[] GetIdentityAtOffsets(IChain chain)
        {
            double[] result = new double[chain.Count];
            for(int offset = 0; offset < chain.Count; offset++)
            {
                result[offset] = Enumerable.Range(offset, chain.Count - offset).Select(i => (chain[i].Letter == chain[i - offset].Letter? 1.0 : 0.0)).Average();
            }
            return result;
        }

        public static void GetOptimalAlignmentShift(string stay, string move, out double identityFraction, out int moveShift)
        {
            int[,] matchMatrix = new int[move.Length, stay.Length];
            Dictionary<int, int> shiftToScore = new Dictionary<int, int>();

            for (int i = 0; i < move.Length; i++)
            {
                for(int j = 0; j < stay.Length; j++)
                {
                    matchMatrix[i, j] = move[i] == stay[j] ? 1 : 0;
                }
            }

            // Shift the move peptide 0 or more positions to the left against the stationary peptide and count the identical positions
            for (int shift = 0; shift < move.Length; shift++)
            {
                int sum = 0;
                for(int i = shift, j = 0; i < move.Length && j < stay.Length; i++, j++)
                {
                    sum += matchMatrix[i, j];
                }
                shiftToScore[-shift] = sum;
            }

            // Shift the move peptide 1 or more positions to the right against the stationary peptide and count the identical positions
            for(int shift = 1; shift < stay.Length; shift++)
            {
                int sum = 0;
                for (int j = shift, i = 0; i < move.Length && j < stay.Length; i++, j++)
                {
                    sum += matchMatrix[i, j];
                }
                shiftToScore[shift] = sum;
            }

            KeyValuePair<int, int> maxIdentity = shiftToScore.OrderByDescending(a => a.Value).First();
            Debug.Assert(shiftToScore.Values.Max() == maxIdentity.Value, "Previous line not working as expected");

            moveShift = maxIdentity.Key;
            identityFraction = (double) maxIdentity.Value / Math.Max(move.Length, stay.Length);
        }
    }
}
