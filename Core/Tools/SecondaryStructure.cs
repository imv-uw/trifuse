using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Utilities;
using System.Diagnostics;
using Core.Interfaces;

namespace Tools
{

    public struct SSBlock
    {
        public SS SS;
        public int Start;
        public int End;

        public bool Contains(int index) { return Start <= index && index <= End; }
        public SSBlock(SS ss, int start, int end) { SS = ss; Start = start; End = end; }
        public int Length {  get { return End - Start + 1; } }
    }
    
    public class TransformSequenceAlignment : SequenceAlignment
    {
        Matrix _align1 = Matrix.Identity;
        Matrix _align2 = Matrix.Identity;
        Vector3 _centroid1;
        Vector3 _centroid2;

        public Matrix Align2 { get { return _align1; } set { _align1 = value; } }
        public Matrix Align1 { get { return _align2; } set { _align2 = value; } }
        public Vector3 Centroid1 { get { return _centroid1; } set { _centroid1 = value; } }
        public Vector3 Centroid2 { get { return _centroid2; } set { _centroid2 = value; } }

        public TransformSequenceAlignment(TransformSequenceAlignment other)
            : this(other.ChainIndex1, other.Range1, other.ChainIndex2, other.Range2)
        {
            Align2 = other.Align2;
            Align1 = other.Align1;
            Centroid1 = other.Centroid1;
            Centroid2 = other.Centroid2;
        }

        public TransformSequenceAlignment(int start1, int end1, int start2, int end2)
            : this(0, start1, end1, 0, start2, end2) // assume chain1
        { }

        public TransformSequenceAlignment(int chainIndex1, Range range1, int chainIndex2, Range range2)
            : this(chainIndex1, range1.Start, range1.End, chainIndex2, range2.Start, range2.End)
        { }

        public TransformSequenceAlignment(int chainIndex1, int start1, int end1, int chainIndex2, int start2, int end2)
            : base(start1, end1, start2, end2, chainIndex1, chainIndex2)
        {
            Align2 = Matrix.Identity;
            Align1 = Matrix.Identity;
            Centroid1 = Vector3.Zero;
            Centroid2 = Vector3.Zero;
        }

        public override void Reverse()
        {
            Swap<Matrix>(ref _align1, ref _align2);
            Swap<Vector3>(ref _centroid1, ref _centroid2);
            base.Reverse();
        }

        public static TransformSequenceAlignment Reversed(TransformSequenceAlignment other)
        {
            TransformSequenceAlignment result = new TransformSequenceAlignment(other);
            result.Reverse();
            return result;
        }
    }

    public class SequenceAlignment
    {
        int _chainIndex1 = 0;
        int _chainIndex2 = 0;
        Range _range1;
        Range _range2;

        public int ChainIndex1 { get { return _chainIndex1; } set { _chainIndex1 = value; } }
        public int ChainIndex2 { get { return _chainIndex2; } set { _chainIndex2 = value; } }
        public Range Range1 { get { return _range1; } set { _range1 = value; } }
        public Range Range2 { get { return _range2; } set { _range2 = value; } }

        public SequenceAlignment(Range range1, Range range2, int chainIndex1 = 0, int chainIndex2 = 0)
        {
            Range1 = range1;
            Range2 = range2;
            ChainIndex1 = chainIndex1;
            ChainIndex2 = chainIndex2;
        }

        public SequenceAlignment(int start1, int end1, int start2, int end2, int chain1 = 0, int chain2 = 0)
        {
            Range1 = new Range(start1, end1);
            Range2 = new Range(start2, end2);
            ChainIndex1 = chain1;
            ChainIndex2 = chain2;
        }

        void Validate()
        {
            Trace.Assert(Range1.Length == Range2.Length);
        }

        public int Length { get { return Range1.Length; } }

        public static SequenceAlignment Reversed(SequenceAlignment other)
        {
            return new SequenceAlignment(other.Range2, other.Range1, other.ChainIndex2, other.ChainIndex1);
        }

        public virtual void Reverse()
        {
            Swap<int>(ref _chainIndex1, ref _chainIndex2);
            Swap<Range>(ref _range1, ref _range2);
        }

        protected static void Swap<T>(ref T left, ref T right)
        {
            T tmp = left;
            left = right;
            right = tmp;
        }
    }

    public class SecondaryStructure
    {
        // αR: −100° ≤ φ ≤ −30°, −80° ≤ ψ ≤ −5°; 
        // near-αR: −175° ≤ φ ≤ −100°, −55° ≤ ψ ≤ −5°; 
        // αL: 5° ≤ φ ≤ 75°, 25° ≤ ψ ≤ 120°; 
        // β: −180° ≤ φ ≤ −50°, 80° ≤ ψ ≤ −170°; 
        // PIR: −180° ≤ φ ≤ −115°, 50° ≤ ψ ≤ 100°; 
        // PIIL: −110° ≤ φ ≤ −50°, 120° ≤ ψ ≤ 180°
        public static SS[] GetPhiPsiSS(IChain peptide, int minStructureLength = 1)
        {
            int start = 1;
            SS[] bins = new SS[peptide.Count];
            for(int i = 1; i < peptide.Count - 1; i++)
            {
                // Make the bin assignment
                if(i != peptide.Count - 1)
                {
                    double phi = peptide.GetPhiDegrees(i);
                    double psi = peptide.GetPsiDegrees(i);
                    if (-100 <= phi && phi <= -30 && -80 <= psi && psi <= -5)
                        bins[i] = SS.Helix;

                    if (-180 <= phi && phi <= -50 && (psi <= -170 || 80 <= psi))
                        bins[i] = SS.Extended;
                }
                
                // Clear preceding assignments in range [start, i-1] if there
                // is a transition before the minimum structure length
                if(bins[i] != bins[start])
                {
                    if (i - start < minStructureLength)
                    {
                        for (int j = start; j < i; j++)
                            bins[j] = SS.Loop;
                    }
                    start = i;
                }
                
            }
            for (int i = 0; i < peptide.Count; i++)
            {
                if (bins[i] == SS.Undefined)
                    bins[i] = SS.Loop;
            }
            return bins;
        }

        public static char GetDsspChar(SS ss)
        {
            switch(ss)
            {
                case SS.Undefined: return 'C'; // not initialized -> Coil
                case SS.Helix: return 'H';
                case SS.Extended: return 'E';
                case SS.Loop: return 'C';
            }
            throw new ArgumentException();
        }

        public static List<SSBlock> GetPhiPsiSSBlocks(IChain peptide, int minStructureLength = 1)
        {
            List<SSBlock> blocks = new List<SSBlock>();
            SS[] bins = GetPhiPsiSS(peptide, minStructureLength);
            int start = 1;
            for (int i = 0; i <= peptide.Count; i++)
            {
                if (i == peptide.Count || bins[i] != bins[start])
                {
                    if (minStructureLength <= i - start)
                        blocks.Add(new SSBlock(bins[start], start, i - 1));
                    start = i;
                }
            }
            return blocks;
        }

        public static List<SSBlock> GetPhiPsiSSBlocksOfType(IChain peptide, SS allowedTypes, int minStructureLength = 1)
        {
            List<SSBlock> list = GetPhiPsiSSBlocks(peptide, minStructureLength);
            List<SSBlock> matchingTypeList = list.Where(block => (block.SS & allowedTypes) != 0).ToList();
            return matchingTypeList;
        }

        public static List<Vector3> GetHelixVectors(IChain peptide, IEnumerable<SSBlock> blocks)
        {
            List<Vector3> vectors = new List<Vector3>();

            foreach(SSBlock block in blocks.Where(b => b.SS == SS.Helix))
            {
                Vector3 start = peptide[block.Start][Aa.CA_].Xyz;
                Vector3 end = peptide[block.End][Aa.CA_].Xyz;
                if (18 < block.Length)
                    end = peptide[block.Start + 18][Aa.CA_].Xyz;
                else if(11 < block.Length)
                    end = peptide[block.Start + 11][Aa.CA_].Xyz;
                else if (7 < block.Length)
                    end = peptide[block.Start + 7][Aa.CA_].Xyz;
                Vector3 vec = end - start;
                vectors.Add(vec);
            }
            return vectors;
        }

        public static List<Vector3> GetHelixMidpoints(IChain peptide, IEnumerable<SSBlock> blocks)
        {
            List<Vector3> midpoints = new List<Vector3>();
            foreach (SSBlock block in blocks.Where(b => b.SS == SS.Helix))
            {
                Vector3 start = peptide[block.Start][Aa.CA_].Xyz;
                Vector3 end = peptide[block.End][Aa.CA_].Xyz;
                Vector3 vec = (start + end)/2;
                midpoints.Add(vec);
            }
            return midpoints;
        }

        public static SSQuadrant[] GetQuadrants(IChain peptide)
        {
            SSQuadrant[] quadrants = new SSQuadrant[peptide.Count];
            for(int i = 1; i < quadrants.Length - 1; i++)
            {
                float phi = (float) peptide.GetPhiRadians(i);
                float psi = (float) peptide.GetPsiRadians(i);
                quadrants[i] = phi < 0 ? (psi < 0 ? SSQuadrant.Alpha : SSQuadrant.Beta) : (psi < 0 ? SSQuadrant.Other : SSQuadrant.AlphaLeft);
            }
            quadrants[0] = SSQuadrant.Undefined;
            quadrants[quadrants.Length - 1] = SSQuadrant.Undefined;
            return quadrants;
        }
    }
}
