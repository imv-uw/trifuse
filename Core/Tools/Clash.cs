using Microsoft.Xna.Framework;
using Core;
using System;
using System.Collections.Generic;
using Core.Utilities;
using NamespaceUtilities;
using Core.Interfaces;
using System.Linq;
using System.Diagnostics;
using Core.Quick.Tuples;

namespace Tools
{
    public class Clash
    {
        public enum ContactType
        {
            Clash = 1,                  // Search is for contact closer than the clash threshold - doesn't really makes sense with vector, but does for atomic contact/clash
            SidechainSidechain = 2,
            SidechainMainchain = 4,
            MainchainSidechain = 8,
            MainchainMainchain = 16,
            VectorCACB = 32,
            IgnoreInvalidCoordinates = 64,
            IgnoreHydrogen = 128,
            SidechainSidechainClash = SidechainSidechain | Clash | IgnoreInvalidCoordinates,
            SidechainMainchainClash = SidechainMainchain | Clash | IgnoreInvalidCoordinates,
            MainchainSidechainClash = MainchainSidechain | Clash | IgnoreInvalidCoordinates,
            MainchainMainchainClash = MainchainMainchain | Clash | IgnoreInvalidCoordinates,
            Atomic = MainchainMainchain | SidechainMainchain | MainchainSidechain| SidechainSidechain | IgnoreInvalidCoordinates,
            AtomicClash = MainchainMainchain | SidechainMainchain | MainchainSidechain | SidechainSidechain | IgnoreInvalidCoordinates | Clash | IgnoreHydrogen,
            Any = Atomic | VectorCACB
        }

        /*  van der Waals radii (Wikipedia)
            Element	radius (Å)
            Hydrogen	1.2 (1.09)[1]
            Carbon      1.7
            Nitrogen	1.55
            Oxygen	    1.52
            Fluorine	1.47
            Phosphorus	1.8
            Sulfur	    1.8
            Chlorine	1.75
            Copper      1.4
        
         */
        public const float VdwHydrogenNeutral = 1.2f;
        public const float VdwHydrogenPositive = 1.09f;
        public const float VdwCarbon = 1.7f;
        public const float VdwNitrogen = 1.55f; // Hbond --> 1.35
        public const float VdwOxygen = 1.52f;   // Hbond --> 1.35
        public const float VdwFluorine = 1.47f;
        public const float VdwPhosphorus = 1.8f;
        public const float VdwSulfur = 1.8f;
        public const float VdwChlorine = 1.75f;
        public const float VdwCopper = 1.4f;

        public const float ClashCC2 = 0.9f * (VdwCarbon   + VdwCarbon)   * (VdwCarbon   + VdwCarbon);
        public const float ClashCN2 = 0.9f * (VdwCarbon   + VdwNitrogen) * (VdwCarbon   + VdwNitrogen);
        public const float ClashCO2 = 0.9f * (VdwCarbon   + VdwOxygen)   * (VdwCarbon   + VdwOxygen);
        public const float ClashNN2 = 0.9f * (VdwNitrogen + VdwNitrogen) * (VdwNitrogen + VdwNitrogen);
        public const float ClashNO2 = 2.5f * 2.5f; //0.9f * (VdwNitrogen + VdwOxygen)   * (VdwNitrogen + VdwOxygen);    // replaced because h-bonds can be ~2.6
        public const float ClashOO2 = 2.5f * 2.5f; //0.9f * (VdwOxygen   + VdwOxygen)   * (VdwOxygen   + VdwOxygen);    // replaced because h-bonds can be ~2.6
        public const float ClashSO2 = 0.9f * (VdwSulfur   + VdwOxygen)   * (VdwSulfur   + VdwOxygen);
        public const float ClashSC2 = 0.9f * (VdwSulfur   + VdwCarbon)   * (VdwSulfur   + VdwCarbon);
        public const float ClashSN2 = 0.9f * (VdwSulfur   + VdwNitrogen) * (VdwSulfur   + VdwNitrogen);
        public const float ClashSS2 = 1.75f * 1.75f; // Disulfide bond distance is ~2.05A, so consider it a clash if they're even closer than that

        public const float ContactThresholdCA2 = 144f;
        public const float AtomClashDistance2 = 0.9f * (VdwCarbon + VdwCarbon) * (VdwCarbon + VdwCarbon);
        //public const float AtomClashDistance2 = 9f; // REVISIT!!! Just here for fusion protocol because the value was giving false positives
        public const float AtomClashDistanceH2 = 0.9f * (VdwHydrogenPositive + VdwHydrogenPositive) * (VdwHydrogenPositive + VdwHydrogenPositive);
        public const float AtomContactDistanceCC2 = 5.5f * 5.5f;
        public const float AtomContactDistance2 = 16.8f; // Lowered it to ~4.2^2 based on looking at PDBs and it's still on the high side.

        public const float ContactDistanceCA1CA2 = 8f;

        public static bool AnyContact(IEnumerable<Selection> selections, ContactType type)
        {
            IAa[][] aas = selections.Select((Func<Selection, IAa[]>)(s => Enumerable.ToArray<IAa>(s.Aas))).ToArray();
            bool clash = AnyContact(aas, type);
            return clash;
        }

        public static bool AnyContact(IStructure structure1, IStructure structure2, ContactType type)
        {
            bool clashes = AnyContact(new IStructure[] { structure1 }, new IStructure[] { structure2 }, type);
            return clashes;
        }

        public static bool AnyContact(IStructure structure1, IStructure structure2, ISet<IAa> ignoreClash, ContactType type)
        {
            bool clashes = AnyContact(new IStructure[] { structure1 }, new IStructure[] { structure2 }, ignoreClash, type);
            return clashes;
        }

        public static bool AnyContact(IEnumerable<IStructure> set1, IEnumerable<IStructure> set2, ContactType type)
        {
            IAa[] aas1 = set1.SelectMany(structure => structure).SelectMany(chain => chain).ToArray();
            IAa[] aas2 = set2.SelectMany(structure => structure).SelectMany(chain => chain).ToArray();
            bool clash = AnyContact(aas1, aas2, type);
            return clash;
        }

        public static bool AnyContact(IEnumerable<IStructure> set1, IEnumerable<IStructure> set2, ISet<IAa> ignoreClash, ContactType type)
        {
            IAa[] aas1 = set1.SelectMany(structure => structure).SelectMany(chain => chain).Where(aa => !ignoreClash.Contains(aa)).ToArray();
            IAa[] aas2 = set2.SelectMany(structure => structure).SelectMany(chain => chain).Where(aa => !ignoreClash.Contains(aa)).ToArray();
            bool clash = AnyContact(aas1, aas2, type);
            return clash;
        }

        public static bool AnyContact(IEnumerable<IAa> set1, IEnumerable<IAa> set2, ContactType type)
        {
            IAa[] aas1 = set1.ToArray();
            IAa[] aas2 = set2.ToArray();
            bool clash = AnyContact(aas1, aas2, type);
            return clash;
        }

        public static bool AnyContact(IEnumerable<IChain> set1, IEnumerable<IChain> set2, ContactType type)
        {
            IAa[] aas1 = set1.SelectMany(s => s).ToArray();
            IAa[] aas2 = set2.SelectMany(s => s).ToArray();
            bool clash = AnyContact(aas1, aas2, type);
            return clash;
        }

        public static bool AnyContact(IEnumerable<IAa[]> sets, ContactType type)
        {
            bool clash = AnyContact(sets.ToArray(), type);
            return clash;
        }

        public static bool AnyContact(IAa[][] sets, ContactType type)
        {
            for (int i = 0; i < sets.Length - 1; i++)
            {
                for(int j = i + 1; j < sets.Length; j++)
                {
                    if (AnyContact(sets[i], sets[j], type))
                        return true;
                }
            }
            return false;
        }

        public static bool AnyContact(IAa[] set1, IAa[] set2, ContactType type)
        {
            for (int i = 0; i < set1.Length; i++)
            {
                IAa aa1 = set1[i];
                for (int j = 0; j < set2.Length; j++)
                {
                    IAa aa2 = set2[j];
                    if (AnyContact(aa1, aa2, type))
                        return true;
                }
            }
            return false;
        }

        public static bool AnyContact(IAa aa1, IAa aa2, ContactType type)
        {
            bool ignoreInvalid = (type & ContactType.IgnoreInvalidCoordinates) != 0;
            bool clash = (type & ContactType.Clash) != 0;
            bool MCMC = (type & ContactType.MainchainMainchain) != 0;
            bool SCSC = (type & ContactType.SidechainSidechain) != 0;
            bool SCMC = (type & ContactType.SidechainMainchain) != 0;
            bool MCSC = (type & ContactType.MainchainSidechain) != 0;
            bool CACB = (type & ContactType.VectorCACB) != 0;
            
            Vector3 iCA = aa1[Aa.CA_].Xyz;
            Vector3 jCA = aa2[Aa.CA_].Xyz;

            int count1 = aa1.Count;
            int count2 = aa2.Count;
            float contactDistance2 = clash ? AtomClashDistance2 : AtomContactDistance2;

            // If they're pretty far apart, sidechains can't reach
            if (ContactThresholdCA2 < Vector3.DistanceSquared(iCA, jCA))
                return false;

            if (MCMC || SCMC || MCSC || CACB)
            {
                Vector3[] MC1 = new Vector3[4] { iCA, aa1[Aa.C_].Xyz, aa1[Aa.N_].Xyz, aa1[Aa.O_].Xyz };
                Vector3[] MC2 = new Vector3[4] { jCA, aa2[Aa.C_].Xyz, aa2[Aa.N_].Xyz, aa2[Aa.O_].Xyz };

                if (MCMC)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        CheckInvalidCoordinates(ref MC1[i], ignoreInvalid);

                        for (int j = 0; j < 4; j++)
                        {
                            CheckInvalidCoordinates(ref MC2[j], ignoreInvalid);

                            if (Vector3.DistanceSquared(MC1[i], MC2[j]) < contactDistance2)
                                return true;
                        }
                    }
                }

                if (MCSC)
                {
                    // Invert the loop order for MC1:SC2 to reduce the number of residue lookups and element checks, which are slower than simple  array lookups
                    for (int j = Aa.SidechainStart_; j < count2; j++)
                    {
                        IAtom atom2 = aa2[j];
                        if (atom2.Element == Element.H)
                            continue;

                        CheckInvalidCoordinates(atom2.Xyz, ignoreInvalid);
                        for (int i = 0; i < 4; i++)
                        {
                            CheckInvalidCoordinates(MC1[i], ignoreInvalid);
                            if (Vector3.DistanceSquared(MC1[i], atom2.Xyz) < contactDistance2)
                                return true;
                        }
                    }
                }

                if (SCMC)
                {
                    for (int i = Aa.SidechainStart_; i < count1; i++)
                    {
                        IAtom atom1 = aa1[i];
                        if (atom1.Element == Element.H)
                            continue;

                        CheckInvalidCoordinates(atom1.Xyz, ignoreInvalid);
                        for (int j = 0; j < 4; j++)
                        {
                            CheckInvalidCoordinates(MC2[j], ignoreInvalid);
                            if (Vector3.DistanceSquared(atom1.Xyz, MC2[j]) < contactDistance2)
                                return true;
                        }
                    }
                }

                if (CACB)
                {
                    // Check that each sidechain points in the direction of the other, with a distance-based scaling factor s.t. a distance of 10 pointing exactly in the same
                    // direction would count as "in contact"
                    // Note: CA-CB isn't really used (due to GLY), instead CA->CB is approximated with ( k1 * unit(N->CA + C->CA) + k2 * (unit(N->CA cross C->CA) )
                    // U = unit(N->CA + C->CA)
                    // V = unit(N->CA cross C->CA)
                    Vector3 NCA1 = MC1[0] - MC1[2]; // MC1/2 order: CA, C, N, O
                    Vector3 NCA2 = MC2[0] - MC2[2];
                    Vector3 CCA1 = MC1[0] - MC1[1];
                    Vector3 CCA2 = MC2[0] - MC2[1];
                    Vector3 U1 = Vector3.Normalize(NCA1 + CCA1);
                    Vector3 U2 = Vector3.Normalize(NCA2 + CCA2);
                    Vector3 V1 = Vector3.Normalize(Vector3.Cross(NCA1, CCA1));
                    Vector3 V2 = Vector3.Normalize(Vector3.Cross(NCA2, CCA2));
                    Vector3 CACB1 = 0.577f * U1 + 0.817f * V1; // Constants are cosine and sine of 109.5/2 degrees
                    Vector3 CACB2 = 0.577f * U2 + 0.817f * V2; // Constants are cosine and sine of 109.5/2 degrees
                    Vector3 CA1CA2 = jCA - iCA;
                    Vector3 unitCA1CA2 = Vector3.Normalize(CA1CA2);

                    float dot1 = Vector3.Dot(CACB1, unitCA1CA2);
                    float dot2 = Vector3.Dot(CACB2, -unitCA1CA2);
                    float scaled = (dot1 + dot2 + 1 /* 90 degree angle can definitely clash/interact, but dot1/2 == 0 */ ) / 2 * ContactDistanceCA1CA2 / CA1CA2.Length();
                    if (scaled > 1)
                        return true;
                }
            }

            if (SCSC)
            {
                for (int i = Aa.SidechainStart_; i < count1; i++)
                {
                    IAtom atom1 = aa1[i];
                    if (atom1.Element == Element.H)
                        continue;
                    Vector3 xyz1 = atom1.Xyz;

                    for (int j = Aa.SidechainStart_; j < count2; j++)
                    {
                        IAtom atom2 = aa2[j];
                        if (atom2.Element == Element.H)
                            continue;

                        Vector3 xyz2 = atom2.Xyz;
                        CheckInvalidCoordinates(xyz1, ignoreInvalid);
                        CheckInvalidCoordinates(xyz2, ignoreInvalid);
                        if (Vector3.DistanceSquared(xyz1, xyz2) < contactDistance2)
                            return true;
                    }
                }
            }

            return false;
        }

        static List<int> ConvertArrayToList(bool[] array)
        {
            List<int> list = new List<int>();
            for(int i = 0; i < array.Length; i++)
            {
                if (array[i])
                {
                    list.Add(i);
                }
            }
            return list;
        }

        public static List<int> GetContactIndices(IChain peptide, Range[] ranges, ContactType type)
        {
            // Since the 
            if ((type & ContactType.SidechainMainchain | type & ContactType.MainchainSidechain) != 0)
                type |= ContactType.SidechainMainchain | ContactType.MainchainSidechain;

            bool[] contacts = new bool[peptide.Count];
            for(int rangeIndex1 = 0; rangeIndex1 < ranges.Length - 1; rangeIndex1++)
            {
                Range range1 = ranges[rangeIndex1];
                for(int i = range1.Start; i <= range1.End; i++)
                {
                    IAa residue1 = peptide[i];
                    for (int rangeIndex2 = rangeIndex1 + 1; rangeIndex2 < ranges.Length; rangeIndex2++)
                    {
                        Range range2 = ranges[rangeIndex2];
                        for (int j = range2.Start; j <= range2.End; j++)
                        {
                            IAa residue2 = peptide[j];
                            if (contacts[i] && contacts[j])
                                continue;

                            bool contact = false;

                            // Check mainchain-mainchain only. This only applies to residues separated by 1 or more 
                            // residues. Otherwise, covalently bound atoms would be considered clashing.
                            if ((type & ContactType.MainchainMainchain) != 0 && Math.Abs(i - j) > 1)
                            {
                                contact |= AnyContact(residue1, residue2, type & ContactType.MainchainMainchainClash);
                            }

                            // Check everything except for mainchain-mainchain
                            if(contact == false)
                            {
                                contact |= AnyContact(residue1, residue2, type & ~ContactType.MainchainMainchain);
                                contact |= AnyContact(residue2, residue1, type & ~ContactType.MainchainMainchain);
                            }

                            contacts[i] |= contact;
                            contacts[j] |= contact;
                        }
                    }
                }              
            }

            List<int> list = ConvertArrayToList(contacts);
            return list;
        }

        public static bool GetBackboneClashesInResidueRange(IChain peptide, int start, int end)
        {
            for(int i = start; i <= end; i++)
            {
                Vector3 iCA = peptide[i][Aa.CA_].Xyz;
                Vector3 iC = peptide[i][Aa.C_].Xyz;
                Vector3 iN = peptide[i][Aa.N_].Xyz;
                Vector3 iO = peptide[i][Aa.O_].Xyz;

                // TODO: detect immediate neighbor clashes via reduced bond lengths, perhaps as in
                // http://www.open.edu/openlearn/science-maths-technology/science/biology/proteins/content-section-1.2
                for (int j = i + 2; j <= end; j++)
                {
                    Vector3 jCA = peptide[j][Aa.CA_].Xyz;

                    // If they're pretty far apart, sidechains can't reach
                    if (ContactThresholdCA2 < Vector3.DistanceSquared(iCA, jCA))
                        continue;

                    Vector3 jC = peptide[j][Aa.C_].Xyz;
                    Vector3 jN = peptide[j][Aa.N_].Xyz;
                    Vector3 jO = peptide[j][Aa.O_].Xyz;

                    // Bah, let's burn some cycles                                                          
                    if (Vector3.DistanceSquared(iN, jCA)  < ClashCN2) 
                        return true;
                    if (Vector3.DistanceSquared(iN, jC)   < ClashCN2) 
                        return true;
                    if (Vector3.DistanceSquared(iN, jN)   < ClashNN2) 
                        return true;
                    if (Vector3.DistanceSquared(iN, jO)   < ClashNO2) 
                        return true;

                    if (Vector3.DistanceSquared(iCA, jCA) < ClashCC2) 
                        return true;
                    if (Vector3.DistanceSquared(iCA, jC)  < ClashCC2) 
                        return true;
                    if (Vector3.DistanceSquared(iCA, jN)  < ClashCN2) 
                        return true;
                    if (Vector3.DistanceSquared(iCA, jO)  < ClashCO2) 
                        return true;

                    if (Vector3.DistanceSquared(iO, jCA)  < ClashCO2) 
                        return true;
                    if (Vector3.DistanceSquared(iO, jC)   < ClashCO2) 
                        return true;
                    if (Vector3.DistanceSquared(iO, jN)   < ClashNO2) 
                        return true;
                    if (Vector3.DistanceSquared(iO, jO)   < ClashOO2) 
                        return true;

                    if (Vector3.DistanceSquared(iC, jCA)  < ClashCC2) 
                        return true;
                    if (Vector3.DistanceSquared(iC, jC)   < ClashCC2) 
                        return true;
                    if (Vector3.DistanceSquared(iC, jO)   < ClashCO2) 
                        return true;
                    if (Vector3.DistanceSquared(iC, jN)    < ClashCN2) 
                        return true;
                                     

                }    
            }

            return false;
        }

        public static bool GetInterSetBackboneClashes(IChain[] peptides, int[] starts, int[] ends)
        {
            // CA-CA distance (squared) at which clashes become impossible for two residues
            const float nonClashingDistance2 = 4 * VdwPhosphorus * 2 * VdwPhosphorus; // square of largest clash distsance

            for (int peptideIndex1 = 0; peptideIndex1 < peptides.Length; peptideIndex1++)
            {
                IChain peptide1 = peptides[peptideIndex1];
                for (int peptideIndex2 = peptideIndex1 + 1; peptideIndex2 < peptides.Length; peptideIndex2++)
                {
                    IChain peptide2 = peptides[peptideIndex2];
                    for (int i = starts[peptideIndex1]; i <= ends[peptideIndex1]; i++)
                    {
                        Vector3 iCA = peptide1[i][Aa.CA_].Xyz;
                        Vector3 iC = peptide1[i][Aa.C_].Xyz;
                        Vector3 iN = peptide1[i][Aa.N_].Xyz;
                        Vector3 iO = peptide1[i][Aa.O_].Xyz;

                        // TODO: detect immediate neighbor clashes via reduced bond lengths, perhaps as in
                        // http://www.open.edu/openlearn/science-maths-technology/science/biology/proteins/content-section-1.2
                        for (int j = starts[peptideIndex2]; j <= ends[peptideIndex2]; j++)
                        {
                            Vector3 jCA = peptide2[j][Aa.CA_].Xyz;

                            // If they're pretty far apart, sidechains can't reach
                            if (nonClashingDistance2 < Vector3.DistanceSquared(iCA, jCA))
                                continue;

                            Vector3 jC = peptide2[j][Aa.C_].Xyz;
                            Vector3 jN = peptide2[j][Aa.N_].Xyz;
                            Vector3 jO = peptide2[j][Aa.O_].Xyz;

                            // Bah, let's burn some cycles                                                          
                            if (Vector3.DistanceSquared(iN, jCA) < ClashCN2)
                                return true;
                            if (Vector3.DistanceSquared(iN, jC) < ClashCN2)
                                return true;
                            if (Vector3.DistanceSquared(iN, jN) < ClashNN2)
                                return true;
                            if (Vector3.DistanceSquared(iN, jO) < ClashNO2)
                                return true;

                            if (Vector3.DistanceSquared(iCA, jCA) < ClashCC2)
                                return true;
                            if (Vector3.DistanceSquared(iCA, jC) < ClashCC2)
                                return true;
                            if (Vector3.DistanceSquared(iCA, jN) < ClashCN2)
                                return true;
                            if (Vector3.DistanceSquared(iCA, jO) < ClashCO2)
                                return true;

                            if (Vector3.DistanceSquared(iO, jCA) < ClashCO2)
                                return true;
                            if (Vector3.DistanceSquared(iO, jC) < ClashCO2)
                                return true;
                            if (Vector3.DistanceSquared(iO, jN) < ClashNO2)
                                return true;
                            if (Vector3.DistanceSquared(iO, jO) < ClashOO2)
                                return true;

                            if (Vector3.DistanceSquared(iC, jCA) < ClashCC2)
                                return true;
                            if (Vector3.DistanceSquared(iC, jC) < ClashCC2)
                                return true;
                            if (Vector3.DistanceSquared(iC, jO) < ClashCO2)
                                return true;
                            if (Vector3.DistanceSquared(iC, jN) < ClashCN2)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsInContact(IAa aa1, int index1, IAa aa2, int index2, ContactType contactType = ContactType.Atomic)
        {
            bool MCMC = (contactType & ContactType.MainchainMainchain) != 0;
            bool SCSC = (contactType & ContactType.SidechainSidechain) != 0;
            bool SCMC = (contactType & ContactType.SidechainMainchain) != 0;
            bool MCSC = (contactType & ContactType.MainchainSidechain) != 0;
            bool ignoreInvalid = (contactType & ContactType.IgnoreInvalidCoordinates) != 0;
            bool ignoreHydrogen = (contactType & ContactType.IgnoreHydrogen) != 0;
            bool clash = (contactType & ContactType.Clash) != 0;
            bool vectorCACB = (contactType & ContactType.VectorCACB) != 0;

            Trace.Assert(!vectorCACB, "Vector CACB contact is an IAa test, not IAtom test");

            IAtom atom1 = aa1[index1];
            IAtom atom2 = aa2[index2];

            if (ignoreHydrogen && (atom1.Element == Element.H || atom2.Element == Element.H))
                return false;

            bool applies =
                (MCMC && atom1.IsMainchain && atom2.IsMainchain) ||
                (SCMC && atom1.IsSidechain && atom2.IsMainchain) ||
                (MCSC && atom1.IsMainchain && atom2.IsSidechain) ||
                (SCSC && atom1.IsSidechain && atom2.IsSidechain);

            if (!applies)
                return false;

            Vector3 v1 = atom1.Xyz;
            Vector3 v2 = atom2.Xyz;

            CheckInvalidCoordinates(v1, ignoreInvalid);
            CheckInvalidCoordinates(v2, ignoreInvalid);

            float d2 = Vector3.DistanceSquared(v1, v2);
            float cutoff2 = clash ? ElementProperties.VdwClashSum2[(int)atom1.Element, (int)atom2.Element]
                : ElementProperties.VdwRadiusSum2[(int)atom1.Element, (int)atom2.Element];

            bool contact = d2 < cutoff2;
            return contact;
        }

        public static float GetVdwContactCutoff(IAtom atom1, IAtom atom2, ContactType contactType = ContactType.Atomic)
        {
            bool clash = (contactType & ContactType.Clash) != 0;

            return clash? 
                ElementProperties.VdwClashSum[(int)atom1.Element, (int)atom2.Element] : 
                ElementProperties.VdwRadiusSum[(int) atom1.Element, (int) atom2.Element];
        }

        public static bool[] GetContactVectorDim1(IChain chain, IChain other, ContactType contactType = ContactType.Atomic)
        {
            bool[] contacts = new bool[chain.Count];
            for(int i = 0; i < chain.Count; i++)
            {
                IAa residue1 = chain[i];
                for(int j = 0; j < other.Count; j++)
                {
                    if (contacts[i])
                        break;

                    IAa residue2 = other[j];
                    contacts[i] = AnyContact(residue1, residue2, contactType);
                }
            }

            return contacts;
        }

        public static bool[,] GetContactVectorDim2(IChain chain, IChain other, ContactType contactType = ContactType.Atomic)
        {
            bool[,] contacts = new bool[chain.Count, other.Count];
            for (int i = 0; i < chain.Count; i++)
            {
                IAa residue1 = chain[i];
                for (int j = 0; j < other.Count; j++)
                {
                    IAa residue2 = other[j];
                    contacts[i,j] = AnyContact(residue1, residue2, contactType);
                }
            }

            return contacts;
        }

        public static List<int> GetContactIndices(IChain chain, IChain other, ContactType contactType = ContactType.Atomic)
        {
            bool[] contacts = GetContactVectorDim1(chain, other, contactType);
            List<int> list = new List<int>();
            for(int i = 0; i < contacts.Length; i++)
            {
                if (contacts[i])
                    list.Add(i);
            }
            return list;
        }

        /// <summary>
        /// Returns a list of residue indices for a given peptide that are in contact with 
        /// any residues in other peptides. Useful for determining which residues form an interface.
        /// </summary>
        /// <param name="peptide"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static List<int> GetContactIndices(IChain peptide, IChain[] others, ContactType contactType = ContactType.Atomic)
        {
            List<int> contacts = new List<int>();
            bool[] contactsVector = new bool[peptide.Count];
            foreach (IChain other in others)
            {
                foreach(int position in GetContactIndices(peptide, other, contactType))
                {
                    contactsVector[position] = true;
                }
            }

            for (int i = 0; i < contactsVector.Length; i++)
            {
                if(contactsVector[i])
                    contacts.Add(i);    
            }
                
            return contacts;
        }

        public static Selection GetContactSelection(IEnumerable<Selection> regions, ContactType contactType = ContactType.Atomic)
        {
            IAa[][] arrays = regions.Select((Func<Selection, IAa[]>)(region => Enumerable.ToArray<IAa>(region.Aas))).ToArray();
            Selection selection = new Selection();

            for (int arrayIndex1 = 0; arrayIndex1 < arrays.Length - 1; arrayIndex1++)
            {
                for (int arrayIndex2 = arrayIndex1 + 1; arrayIndex2 < arrays.Length; arrayIndex2++)
                {
                    IAa[] set1 = arrays[arrayIndex1];
                    IAa[] set2 = arrays[arrayIndex2];

                    for (int index1 = 0; index1 < set1.Length; index1++)
                    {
                        for (int index2 = 0; index2 < set2.Length; index2++)
                        {
                            IAa aa1 = set1[index1];
                            IAa aa2 = set2[index2];
                            Trace.Assert(aa1 != aa2);

                            if (AnyContact(aa1, aa2, contactType))
                            {
                                selection.Aas.Add(aa1);
                                selection.Aas.Add(aa2);
                            }
                        }
                    }
                }
            }

            return selection;
        }

        public static Selection GetContactSelection(IEnumerable<IChain> set1, IEnumerable<IChain> set2, ContactType contactType = ContactType.Atomic)
        {
            IAa[] aas1 = set1.SelectMany(set => set).ToArray();
            IAa[] aas2 = set2.SelectMany(set => set).ToArray();
            Selection aas = GetContactSelection(aas1, aas2, contactType);
            return aas;
        }

        // This function requires set1 and set2 to be disjoint - otherwise desired behavior for self-contact
        // would be unknown
        public static Selection GetContactSelection(IAa[] set1, IAa[] set2, ContactType contactType = ContactType.Atomic)
        {
            Selection selection = new Selection();
            for (int index1 = 0; index1 < set1.Length; index1++)
            {
                for (int index2 = 0; index2 < set2.Length; index2++)
                {
                    IAa aa1 = set1[index1];
                    IAa aa2 = set2[index2];
                    Trace.Assert(aa1 != aa2);

                    if (AnyContact(aa1, aa2, contactType))
                    {
                        selection.Aas.Add(aa1);
                        selection.Aas.Add(aa2);
                    }
                }
            }
            return selection;
        }

        public static Selection GetContactSelection(IChain[] chains, ContactType contactType = ContactType.Atomic)
        {
            Selection selection = new Selection();
            for (int chainIndex1 = 0; chainIndex1 < chains.Length - 1; chainIndex1++)
            {
                for (int chainIndex2 = chainIndex1 + 1; chainIndex2 < chains.Length; chainIndex2++)
                {
                    IChain chain1 = chains[chainIndex1];
                    IChain chain2 = chains[chainIndex2];
                    Trace.Assert(chain1 != chain2);

                    for (int aaIndex1 = 0; aaIndex1 < chain1.Count; aaIndex1++)
                    {
                        IAa aa1 = chain1[aaIndex1];
                        for (int aaIndex2 = 0; aaIndex2 < chain2.Count; aaIndex2++)
                        {
                            IAa aa2 = chain2[aaIndex2];
                            Trace.Assert(aa1 != aa2);

                            if (AnyContact(aa1, aa2, contactType))
                            {
                                selection.Aas.Add(aa1);
                                selection.Aas.Add(aa2);
                            }
                        }
                    }
                }
            }
            return selection;
        }

        public static Selection GetContactSelectionInFocusSet(IStructure focus, IStructure other, ContactType contactType = ContactType.Atomic)
        {
            Selection selection = GetContactSelectionInFocusSet(new IStructure[] { focus }, new IStructure[] { other }, contactType);
            return selection;
        }

        public static Selection GetContactSelectionInFocusSet(IList<IStructure> focus, IList<IStructure> others, ContactType contactType = ContactType.Atomic)
        {
            Selection selection = GetContactSelectionInFocusSet(focus.SelectMany(s => s).ToArray(), others.SelectMany(s => s).ToArray(), contactType );
            return selection;
        }

        public static Selection GetContactSelectionInFocusSet(IList<IChain> focus, IList<IChain> other, ContactType contactType = ContactType.Atomic)
        {
            Selection selection = new Selection();
            for(int focusIndex = 0; focusIndex < focus.Count; focusIndex++)
            {
                for(int otherIndex = 0; otherIndex < other.Count; otherIndex++)
                {
                    IChain focusChain = focus[focusIndex];
                    IChain otherChain = other[otherIndex];

                    for (int focusAaIndex = 0; focusAaIndex < focusChain.Count; focusAaIndex++)
                    {
                        IAa aaFocus = focusChain[focusAaIndex];
                        for (int otherAaIndex = 0; otherAaIndex < otherChain.Count; otherAaIndex++)
                        {
                            IAa aaOther = otherChain[otherAaIndex];
                            Trace.Assert(aaFocus != aaOther);
                            if (AnyContact(aaFocus, aaOther, contactType))
                            {
                                selection.Aas.Add(aaFocus);
                                break;
                            }
                        }
                    }
                }
            }
            return selection;
        }

        public static Selection GetContactSelectionInFocusSet(IList<IAa> focus, IList<IAa> other, ContactType contactType = ContactType.Atomic)
        {
            Selection selection = new Selection();
            
            for (int focusAaIndex = 0; focusAaIndex < focus.Count; focusAaIndex++)
            {
                IAa aaFocus = focus[focusAaIndex];
                for (int otherAaIndex = 0; otherAaIndex < other.Count; otherAaIndex++)
                {
                    IAa aaOther = other[otherAaIndex];
                    Trace.Assert(aaFocus != aaOther);
                    if (AnyContact(aaFocus, aaOther, contactType))
                    {
                        selection.Aas.Add(aaFocus);
                        break;
                    }
                }
            }
            return selection;
        }

        public static Aa2[] GetContactsAa2(Selection set1, Selection set2, ContactType contactType = ContactType.Atomic)
        {
            List<Aa2> results = new List<Aa2>();
            IAa[] s1 = set1.Aas.ToArray();
            IAa[] s2 = set2.Aas.ToArray();

            for (int index1 = 0; index1 < s1.Length; index1++)
            {
                for (int index2 = 0; index2 < s2.Length; index2++)
                {
                    IAa aa1 = s1[index1];
                    IAa aa2 = s2[index2];
                    Trace.Assert(aa1 != aa2);

                    if (AnyContact(aa1, aa2, contactType))
                    {
                        results.Add(new Aa2(aa1, aa2));
                    }
                }
            }

            return results.ToArray();
        }

        static void CheckInvalidCoordinates(ref Vector3 vec, bool allowInvalidCoordinates)
        {
            if(!allowInvalidCoordinates && !VectorMath.IsValid(vec))
                throw new Exception("Invalid coordinates detected");
        }

        static void CheckInvalidCoordinates(Vector3 vec, bool allowInvalidCoordinates)
        {
            if (!allowInvalidCoordinates && !VectorMath.IsValid(vec))
                throw new Exception("Invalid coordinates detected");
        }

        public static int[] GetSidechainContactCounts(IChain peptide1, IChain peptide2, Range range1, Range range2, ContactType contactType)
        {
            int[] counts = new int[peptide1.Count];
            for(int i = range1.Start; i <= range1.End; i++)
            {
                IAa residue1 = peptide1[i];
                for (int j = range2.Start; j <= range2.End; j++)
                {
                    IAa residue2 = peptide2[j];
                    if(AnyContact(residue1, residue2, contactType))
                    {
                        counts[i]++;
                    }
                }
            }
            return counts;
        }
    }
}
