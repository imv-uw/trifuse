using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Structure
{
    public class AtomHash
    {
        List<AtomSource>[, ,] _lists = null;
        int _binCount = 0;
        double _cutoff = 0;
        double _boxSize = 0;

        public AtomHash(double cutoff, int binCount)
        {
            Debug.Assert(binCount > 0);

            _cutoff = cutoff;
            _binCount = binCount;
            _boxSize = _binCount * _cutoff;
            _lists = new List<AtomSource>[_binCount, _binCount, _binCount];

            for (int i = 0; i < binCount; i++)
            {
                for (int j = 0; j < binCount; j++)
                {
                    for (int k = 0; k < binCount; k++)
                    {
                        _lists[i, j, k] = new List<AtomSource>();
                    }
                }
            }
        }

        private int GetBin(float position)
        {
            // Add the entire box size as many times as necessary to ensure a positive coordinate
            if(position < 0)
            {
                int boxOffsets = (int) (-position / _boxSize);
                if (position % _boxSize != 0)
                    boxOffsets++;
                position += (float) (boxOffsets * _boxSize);
            }

            // Assign the position to a particular bin
            Debug.Assert(position >= 0);
            double placeInBox = position % _boxSize;    // Offset into the box
            int bin = (int) (placeInBox / _cutoff);     // Offset into the bins
            return bin;
        }

        void Hash(List<AtomSource> atoms)
        {
            // Clear existing bin contents
            for (int i = 0; i < _binCount; i++)
            {
                for (int j = 0; j < _binCount; j++)
                {
                    for (int k = 0; k < _binCount; k++)
                    {
                        _lists[i, j, k].Clear();
                    }
                }
            }

            // Rehash atoms into bins
            foreach(AtomSource atom in atoms)
            {
                Vector3 coord = atom.XYZ;
                int xBox = GetBin(coord.X);
                int yBox = GetBin(coord.Y);
                int zBox = GetBin(coord.Z);
                _lists[xBox, yBox, zBox].Add(atom);
            }
        }

        // Retreives a list of collision pairs. The returned list contains repeats, because it is faster this way.
        public bool[,] GetCollisionsBool(List<AtomSource> atoms, bool ignoreHydrogen)
        {
            bool[,] collisions = new bool[atoms.Count, atoms.Count];
            Dictionary<AtomSource, int> indexLookup = new Dictionary<AtomSource, int>();
            for (int i = 0; i < atoms.Count; i++)
            {
                indexLookup[atoms[i]] = i;
            }

            Hash(atoms);

            for (int i = 0; i < _binCount; i++)
            {
                for (int j = 0; j < _binCount; j++)
                {
                    for (int k = 0; k < _binCount; k++)
                    {
                        List<AtomSource> set1 = _lists[i, j, k];
                        for (int iNeighbor = i - 1; iNeighbor <= i + 1; iNeighbor++)
                        {
                            for (int jNeighbor = j - 1; jNeighbor <= j + 1; jNeighbor++)
                            {
                                for (int kNeighbor = k - 1; kNeighbor <= k + 1; kNeighbor++)
                                {
                                    // The neighbor indices can go off the end of the array, in which case they wrap
                                    int i2 = ReboxBin(iNeighbor);
                                    int j2 = ReboxBin(jNeighbor);
                                    int k2 = ReboxBin(kNeighbor);

                                    List<AtomSource> set2 = _lists[i2, j2, k2];
                                    foreach(AtomSource atom1 in set1)
                                    {
                                        if (ignoreHydrogen && atom1.Name != null && atom1.Name.StartsWith("H"))
                                            continue;

                                        foreach(AtomSource atom2 in set2)
                                        {
                                            if (ignoreHydrogen && atom2.Name != null && atom2.Name.StartsWith("H"))
                                                continue;

                                            if ((atom1.XYZ-atom2.XYZ).Length() <= _cutoff)
                                            {
                                                int index1 = indexLookup[atom1];
                                                int index2 = indexLookup[atom2];
                                                collisions[index1, index2] = true;
                                                collisions[index2, index1] = true;
                                            }
                                        }
                                    }
                                }
                            }   
                        }
                    }
                }
            }
            return collisions;
        }

        public int[,] GetCollisionCount(List<AtomSource> atoms, bool standardCutoffs)
        {
            int[,] counts = new int[atoms.Count, atoms.Count];
            Dictionary<AtomSource, int> indexLookup = new Dictionary<AtomSource, int>();
            for (int i = 0; i < atoms.Count; i++)
            {
                indexLookup[atoms[i]] = i;
            }

            // Standard cutoff is 5.4 for C-C and 4.4 for C-X or X-X. If using standard cutoffs,
            // collisions will be missed if the bin size is less than 5.4.
            Debug.Assert(!standardCutoffs || _cutoff <= 5.4);

            Hash(atoms);

            for (int i = 0; i < _binCount; i++)
            {
                for (int j = 0; j < _binCount; j++)
                {
                    for (int k = 0; k < _binCount; k++)
                    {
                        List<AtomSource> set1 = _lists[i, j, k];
                        for (int iNeighbor = i - 1; iNeighbor <= i + 1; iNeighbor++)
                        {
                            for (int jNeighbor = j - 1; jNeighbor <= j + 1; jNeighbor++)
                            {
                                for (int kNeighbor = k - 1; kNeighbor <= k + 1; kNeighbor++)
                                {
                                    // The neighbor indices can go off the end of the array, in which case they wrap
                                    int i2 = ReboxBin(iNeighbor);
                                    int j2 = ReboxBin(jNeighbor);
                                    int k2 = ReboxBin(kNeighbor);

                                    List<AtomSource> set2 = _lists[i2, j2, k2];
                                    foreach (AtomSource atom1 in set1)
                                    {
                                        double collisionDistance = _cutoff;

                                        foreach (AtomSource atom2 in set2)
                                        {
                                            // Only compare each pair once.
                                            if (atom1 == atom2)
                                                continue;
                                            if (atom1.Index > atom2.Index)
                                                continue;
                                            if (atom1.Index == atom2.Index)
                                                throw new Exception("atom1.index == atom2.index");


                                            // When using the standard cutoff distances, set the collision distance to 4.4 or 5.4,
                                            // depending on the atom types
                                            if (standardCutoffs)
                                            {   
                                                collisionDistance = 4.6; // Default for anything except Carbon-Carbon

                                                if (atom1.Name != null && atom2.Name != null && atom1.Name.StartsWith("C") && atom2.Name.StartsWith("C"))
                                                {
                                                    collisionDistance = 5.4;    // Cutoff for Carbon-Carbon
                                                }
                                            }

                                            if ((atom1.XYZ - atom2.XYZ).Length() <= collisionDistance)
                                            {
                                                int index1 = indexLookup[atom1];
                                                int index2 = indexLookup[atom2];
                                                counts[index1, index2]++;
                                                counts[index2, index1]++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return counts;
        }

        int ReboxBin(int bin)
        {
            if (bin < 0)
                bin += _binCount;
            else if (bin >= _binCount)
                bin -= _binCount;
            Debug.Assert(0 <= bin && bin <= _binCount);
            return bin;
        }
    }
}
