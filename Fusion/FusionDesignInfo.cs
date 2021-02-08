using Core.Interfaces;
using Core.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tools;

namespace Fuse
{
    public class FusionDesignInfo
    {
        public IChain Peptide;
        public IChain[] OriginalChains;   // Chains from which the fusion peptide was created
        public IChain[] PdbOutputChains;  // All chains that should be output to a PDB - might include context like a bound antigen or symmetric copies
        public Range[] IdentityRanges;   // Ranges with sequence identity to OriginalChains
        public Range[] OriginalRanges;          // Ranges in the original
        public int[] ImmutablePositions;

        int[] designablePositions_ = null;
        public int[] DesignablePositions
        {
            get
            {
                if (designablePositions_ == null)
                {
                    InitializeDesignablePositions();
                    Debug.Assert(designablePositions_ != null);
                }

                return designablePositions_;
            }
        }

        float? score_ = null;
        public float Score
        {
            get
            {
                if (score_ == null)
                {
                    return 0;
                    //ResidueTransformGridMap<float> map = ResidueTransformGridMapLoader.LoadDefault<float>("residue_contact", false, true);
                    //ResidueTransformGridMap<float> alaMap = ResidueTransformGridMapLoader.LoadDefault<float>("ala_contact", false, true);
                    //InitializeAlaPenaltyAlternativeScore(map, alaMap);
                    //Debug.Assert(score_ != null);
                    //Console.WriteLine("score=" + score_);
                }
                return (float)score_;
            }
        }

        void InitializeDesignablePositions()
        {
            List<int> designablePositions = new List<int>();

            // Identify new contacts created between ranges (atomic contact or vector contact, i.e. potentially contacting), but not including the fusion region 
            // Identify any clashes involving the fusion region
            {
                for (int i = 0; i < IdentityRanges.Length - 1; i++)
                {
                    List<int> newContactPositions = Clash.GetContactIndices(Peptide, new Range[] { IdentityRanges[i], IdentityRanges[i + 1] }, Clash.ContactType.VectorCACB | Clash.ContactType.Atomic);
                    Range spliceRange = new Range(IdentityRanges[i].End + 1, IdentityRanges[i + 1].Start - 1);
                    List<int> clashes = Clash.GetContactIndices(Peptide, new Range[] { IdentityRanges[i], IdentityRanges[i + 1], spliceRange }, Clash.ContactType.AtomicClash);
                    designablePositions.AddRange(newContactPositions);
                    designablePositions.AddRange(clashes);
                    designablePositions.AddRange(Enumerable.Range(spliceRange.Start, spliceRange.Length + 1));
                }
            }

            // Identify broken contacts, i.e. those from within the identity region that interact with something
            // past the splice region. 
            {
                for (int i = 0; i < OriginalRanges.Length; i++)
                {
                    List<Range> rangesForChain = new List<Range>();
                    Range rangeIncludingSplice = OriginalRanges[i];
                    if(i > 0)
                    {
                        rangeIncludingSplice.Start -= (IdentityRanges[i].Start - IdentityRanges[i - 1].End - 1);
                    }
                    if(i < OriginalRanges.Length - 1)
                    {
                        rangeIncludingSplice.End += (IdentityRanges[i + 1].Start - IdentityRanges[i].End - 1);
                    }

                    rangesForChain.Add(rangeIncludingSplice);

                    if(rangeIncludingSplice.Start > 0)
                    {
                        rangesForChain.Add(new Range(0, rangeIncludingSplice.Start - 1));
                    }
                    if(rangeIncludingSplice.End < OriginalChains[i].Count - 1)
                    {
                        rangesForChain.Add(new Range(rangeIncludingSplice.End + 1, OriginalChains[i].Count - 1));
                    }

                    List<int> brokenContacts = Clash.GetContactIndices(OriginalChains[i], rangesForChain.ToArray(), Clash.ContactType.SidechainSidechain | Clash.ContactType.IgnoreInvalidCoordinates);
                    foreach(int contactIndex in brokenContacts)
                    {
                        if(OriginalRanges[i].Contains(contactIndex))
                        {
                            int remappedIndex = contactIndex + IdentityRanges[i].Start - OriginalRanges[i].Start;
                            designablePositions.Add(remappedIndex);
                            Debug.Assert(0 <= remappedIndex && remappedIndex < Peptide.Count);
                        }
                    }
                }
            }   
            designablePositions.Sort();
            designablePositions_ = designablePositions.Distinct().ToArray();
        }
    }

    public class TwoAxisFusionDesignInfo : FusionDesignInfo
    {
        public Line Axis1;
        public Line Axis2;
    }



}
