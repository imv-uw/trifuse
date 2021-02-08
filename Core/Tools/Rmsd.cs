using Microsoft.Xna.Framework;
using NamespaceUtilities;
using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Interfaces;

namespace Tools
{
    public class Rmsd
    {
        /// <summary>
        /// Computes the transform necessary to place the 'move' rigid body onto the location defined by the 'final' rigid body. 
        /// </summary>
        /// <param name="move">The rigid body being moved by the transform</param>
        /// <param name="final">The rigid body already at the desired location for the moved (post-transform) rigid body</param>
        /// <returns></returns>
        public static Matrix GetTransform(ICoordinateFrame move, ICoordinateFrame final)
        {
            Vector3 mOrigin;
            Vector3 mUnitX;
            Vector3 mUnitY;
            Vector3 mUnitZ;
            move.GetCoordinateSystem(out mOrigin, out mUnitX, out mUnitY, out mUnitZ);
            Vector3 mX = mOrigin + mUnitX;
            Vector3 mY = mOrigin + mUnitY;
            Vector3 mZ = mOrigin + mUnitZ;


            Vector3 fOrigin;
            Vector3 fUnitX;
            Vector3 fUnitY;
            Vector3 fUnitZ;
            final.GetCoordinateSystem(out fOrigin, out fUnitX, out fUnitY, out fUnitZ);
            Vector3 fX = fOrigin + fUnitX;
            Vector3 fY = fOrigin + fUnitY;
            Vector3 fZ = fOrigin + fUnitZ;

            Vector3[] mV  = new Vector3[] { mOrigin, mX, mY, mZ };
            Vector3[] fV = new Vector3[] { fOrigin, fX, fY, fZ };

            Matrix transform = VectorMath.GetRmsdAlignmentMatrix(mV, false, fV, false);
            return transform;
        }

        public static Matrix GetRmsdTransformForResidues(IChain move, int firstMoveResidue, int lastMoveResidue, IChain stationary, int firstStationaryResidue, int lastStationaryResidue)
        {
            Vector3[] stationaryCoordinates = new Vector3[(lastStationaryResidue - firstStationaryResidue + 1) * 3];
            Vector3[] moveCoordinates = new Vector3[(lastMoveResidue - firstMoveResidue + 1) * 3];

            int arrayIndex = 0;
            for (int residueIndex = firstStationaryResidue; residueIndex <= lastStationaryResidue; residueIndex++)
            {
                stationaryCoordinates[arrayIndex++] = stationary[residueIndex][Aa.N_].Xyz;
                stationaryCoordinates[arrayIndex++] = stationary[residueIndex][Aa.CA_].Xyz;
                stationaryCoordinates[arrayIndex++] = stationary[residueIndex][Aa.C_].Xyz;
            }

            arrayIndex = 0;
            for (int residueIndex = firstMoveResidue; residueIndex <= lastMoveResidue; residueIndex++)
            {
                moveCoordinates[arrayIndex++] = move[residueIndex][Aa.N_].Xyz;
                moveCoordinates[arrayIndex++] = move[residueIndex][Aa.CA_].Xyz;
                moveCoordinates[arrayIndex++] = move[residueIndex][Aa.C_].Xyz;
            }

            Matrix matrix = VectorMath.GetRmsdAlignmentMatrix(moveCoordinates, false, stationaryCoordinates, false);
            return matrix;
        }

        public static Matrix GetRmsdTransformForAtoms(IChain move, IChain stationary)
        {
            IReadOnlyList<IAtom> moveAtoms = move.Atoms;
            IReadOnlyList<IAtom> stationaryAtoms = stationary.Atoms;

            Debug.Assert(moveAtoms.Count == stationaryAtoms.Count);
            Vector3[] stationaryCoordinates = new Vector3[stationaryAtoms.Count];
            Vector3[] moveCoordinates = new Vector3[moveAtoms.Count];

            for (int i = 0; i < moveAtoms.Count; i++)
            {
                stationaryCoordinates[i] = stationaryAtoms[i].Xyz;
                moveCoordinates[i] = moveAtoms[i].Xyz;
            }

            Matrix matrix = VectorMath.GetRmsdAlignmentMatrix(moveCoordinates, false, stationaryCoordinates, false);
            return matrix;
        }

        public static float GetRmsdForAtoms(IChain move, IChain stationary)
        {
            IReadOnlyList<IAtom> moveAtoms = move.Atoms;
            IReadOnlyList<IAtom> stationaryAtoms = stationary.Atoms;

            Debug.Assert(moveAtoms.Count == stationaryAtoms.Count);

            Matrix matrix = GetRmsdTransformForAtoms(move, stationary);

            float deviation2 = 0;
            for (int i = 0; i < moveAtoms.Count; i++)
            {
                Vector3 aligned = Vector3.Transform(moveAtoms[i].Xyz, matrix);
                float distance2 = Vector3.DistanceSquared(aligned, stationaryAtoms[i].Xyz);
                deviation2 += distance2;
            }

            double rmsd = Math.Sqrt(deviation2 / moveAtoms.Count);
            return (float)rmsd;
        }

        public static float GetRmsdNCAC(IChain peptide1, int rangeStart1, int rangeEnd1, IChain peptide2, int rangeStart2, int rangeEnd2)
        {
            Matrix transform = Rmsd.GetRmsdTransformForResidues(peptide1, rangeStart1, rangeEnd1, peptide2, rangeStart2, rangeEnd2);
            float rmsd = GetRmsdNCAC(transform, peptide1, rangeStart1, rangeEnd1, peptide2, rangeStart2, rangeEnd2);
            return rmsd;
        }

        public static float GetRmsdNCAC(Matrix transform, IChain move, int firstMoveResidue, int lastMoveResidue, IChain stationary, int firstStationaryResidue, int lastStationaryResidue)
        {
            float rmsd2 = 0;
            for(int i = firstMoveResidue, j = firstStationaryResidue; i <= lastMoveResidue; i++, j++)
            {
                Vector3 N = Vector3.Transform(move[i][Aa.N_].Xyz, transform);
                Vector3 CA = Vector3.Transform(move[i][Aa.CA_].Xyz, transform);
                Vector3 C = Vector3.Transform(move[i][Aa.C_].Xyz, transform);
                rmsd2 += Vector3.DistanceSquared(N, stationary[j][Aa.N_].Xyz);
                rmsd2 += Vector3.DistanceSquared(CA, stationary[j][Aa.CA_].Xyz);
                rmsd2 += Vector3.DistanceSquared(C, stationary[j][Aa.C_].Xyz);
            }

            float rmsd = (float) Math.Sqrt(rmsd2 / (lastMoveResidue - firstMoveResidue + 1));
            return rmsd;
        }

        public static Matrix GetRmsdTransform(IAa move, IAa stay)
        {
            IAa[] arrayMove = new IAa[] { move };
            IAa[] arrayStay = new IAa[] { stay };
            Matrix matrix = GetRmsdTransform(arrayMove, arrayStay);
            return matrix;
        }

        public static Matrix GetRmsdTransform(IList<IAa> move, IList<IAa> stay)
        {
            Debug.Assert(move.Count == stay.Count);

            Vector3[] stayCoordinates = new Vector3[stay.Count * 3];
            Vector3[] moveCoordinates = new Vector3[move.Count * 3];

            for (int index = 0; index < stay.Count; index++)
            {
                stayCoordinates[index * 3] = stay[index][Aa.N_].Xyz;
                stayCoordinates[index * 3 + 1] = stay[index][Aa.CA_].Xyz;
                stayCoordinates[index * 3 + 2] = stay[index][Aa.C_].Xyz;
            }

            for (int index = 0; index < stay.Count; index++)
            {
                moveCoordinates[index * 3] = move[index][Aa.N_].Xyz;
                moveCoordinates[index * 3 + 1] = move[index][Aa.CA_].Xyz;
                moveCoordinates[index * 3 + 2] = move[index][Aa.C_].Xyz;
            }

            Matrix matrix = VectorMath.GetRmsdAlignmentMatrix(moveCoordinates, false, stayCoordinates, false);
            return matrix;
        }

        public static Matrix GetRmsdAndTransform(IChain move, int moveStart, int moveEnd, IChain stationary, int stationaryStart, int stationaryEnd, out float rmsd)
        {
            Matrix transform = Rmsd.GetRmsdTransformForResidues(move, moveStart, moveEnd, stationary, stationaryStart, stationaryEnd);
            rmsd = GetRmsdNCAC(transform, move, moveStart, moveEnd, stationary, stationaryStart, stationaryEnd);
            return transform;
        }

        public static Vector3 GetBackboneCentroid(Structure protein)
        {
            Vector3 sum = Vector3.Zero;
            int count = 0;
            foreach(IChain chain in protein)
            {
                foreach (IAa residue in chain)
                {
                    sum += residue[Aa.N_].Xyz;
                    sum += residue[Aa.CA_].Xyz;
                    sum += residue[Aa.C_].Xyz;
                    count += 3;
                }
            }
            
            Vector3 average = sum / count;
            return average;
        }

        public static Vector3 GetBackboneCentroid(IChain chain)
        {
            Vector3 sum = Vector3.Zero;
            int count = 0;
            foreach (IAa residue in chain)
            {
                sum += residue[Aa.N_].Xyz;
                sum += residue[Aa.CA_].Xyz;
                sum += residue[Aa.C_].Xyz;
                count += 3;
            }
            Vector3 average = sum / count;
            return average;
        }

        public static Vector3 GetBackboneCentroid(IEnumerable<IAa> residues)
        {
            Vector3 vector = Vector3.Zero;
            int count = residues.Count() * 3;

            foreach(IAa residue in residues)
            {
                vector += residue[Aa.N_].Xyz;
                vector += residue[Aa.CA_].Xyz;
                vector += residue[Aa.C_].Xyz;
                count += 3;
            }

            Vector3 average = vector / count;
            return average;
        }

        public static float GetRmsd(IEnumerable<IAtom> atoms1, IEnumerable<IAtom> atoms2)
        {
            Debug.Assert(atoms1.Count() == atoms2.Count());

            int atomCount = atoms1.Count();
            float rmsd = 0;
            for(int i = 0; i < atomCount; i++)
            {
                Debug.Assert(atoms1.ElementAt(i).Name == atoms2.ElementAt(i).Name);
                rmsd += Vector3.DistanceSquared(atoms1.ElementAt(i).Xyz, atoms2.ElementAt(i).Xyz);
            }
            rmsd /= atomCount;
            rmsd = (float) Math.Sqrt(rmsd);
            return rmsd;
        }

        public static float GetRmsd(IEnumerable<Vector3> coordinates1, IEnumerable<Vector3> coordinates2)
        {
            Debug.Assert(coordinates1.Count() == coordinates2.Count());

            int count = coordinates1.Count();
            float rmsd = 0;
            for (int i = 0; i < count; i++)
            {
                rmsd += Vector3.DistanceSquared(coordinates1.ElementAt(i), coordinates2.ElementAt(i));
            }
            rmsd /= count;
            rmsd = (float)Math.Sqrt(rmsd);
            return rmsd;
        }

        public static float GetRmsd(IEnumerable<Core.Interfaces.IAa> residues1, IEnumerable<Core.Interfaces.IAa> residues2)
        {
            Trace.Assert(residues1.Count() == residues2.Count());

            int atomCount = 0;
            int residueCount = residues1.Count();
            float rmsd = 0;
            for(int residueIndex = 0; residueIndex < residues1.Count(); residueIndex++)
            {
                Core.Interfaces.IAa residue1 = residues1.ElementAt(residueIndex);
                Core.Interfaces.IAa residue2 = residues2.ElementAt(residueIndex);

                Trace.Assert(residue1.Count == residue2.Count);
                for (int i = 0; i < residue1.Count; i++)
                {
                    Trace.Assert(!VectorMath.IsNaN(residue1[i].Xyz));
                    Trace.Assert(!VectorMath.IsNaN(residue2[i].Xyz));
                    rmsd += Vector3.DistanceSquared(residue1[i].Xyz, residue2[i].Xyz);
                    atomCount++;
                }
            }

            rmsd /= atomCount;
            rmsd = (float)Math.Sqrt(rmsd);
            return rmsd;
        }
    }
}
