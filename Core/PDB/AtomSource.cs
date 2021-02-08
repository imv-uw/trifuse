using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Core
{
    // An atom as taken from a source PDB, Dynameomics, or Hatchet.
    public class AtomSource
    {
        // Physical properties
        public Vector3 XYZ;
        public float X { get { return XYZ.X; } set { XYZ.X = value; } }
        public float Y { get { return XYZ.Y; } set { XYZ.Y = value; } }
        public float Z { get { return XYZ.Z; } set { XYZ.Z = value; } }
        public float Charge { get; set; }
        public float Epsilon { get; set; }
        public float Mass { get; set; }
        public float Radius { get; set; }
        public int Index { get; set; }  // Zero-based index, which should match its list position

        public Element Element { get; set; }

        // Bonds
        public List<AtomSource> BondedAtoms { get; private set; }       // All bonded atoms

        public string Name { get; set; }
        public string ElementName { get; set; }
        public string ResidueName { get; set; }
        public char ChainIndex { get; set; }
        public int ResidueIndex { get; set; } // Zero-based residue index
        public int PdbModelIndex { get; set; }
        public int PdbResidueIndex { get; set; }
        public Char PdbAltLoc { get; set; }
        //public Residue Residue { get; private set; }
        
        public AtomSource()
        {
            XYZ = new Vector3(float.NaN);
            BondedAtoms = new List<AtomSource>();
        }

        //public static void ResetSerialNumbers(List<AtomSource> atoms)
        //{
        //    atoms.Sort((x, y) => x.Index.CompareTo(y.Index));
        //    for (int i = 0; i < atoms.Count; i++)
        //    {
        //        if(atoms[i].Index != i)
        //            atoms[i].Index = i;
        //    }
        //}

        ////// Keep a directed-graph record of bonding, i.e. each atom only has a single parent and one or more children.
        ////// Connect the two iff they aren't already bonded. This maintains a parent/child relationship among atoms,
        ////// which may not be necessary at all depending on the route this project goes, but it was done to maintain a directed-graph.
        ////// When rotating atoms around a bond, one side remains stationary, while those on the other move. However, if the bond is part of
        ////// a cycle, rotation cannot be performed about this bond.
        ////public static void AddBond(SourceAtom atom1, SourceAtom atom2, double length, int energy)
        ////{           
        ////    // Set the bonded atoms
        ////    if (!atom1.BondedAtoms.Contains(atom2))
        ////        atom1.BondedAtoms.Add(atom2);

        ////    if (!atom2.BondedAtoms.Contains(atom1))
        ////        atom2.BondedAtoms.Add(atom1);

        ////    Bond bond = new Bond(atom1, atom2, length, energy);
        ////    atom1.Bonds.Add(bond);
        ////    atom2.Bonds.Add(bond);

        ////}
        
        //public static void SetRadiusAndEpsilon(AtomSource atom)
        //{
        //    // MMPL only defines a set number of atom types and doesn't include a programatic
        //    // way to figure out which one an atom in a molecule would correspond to. The comments
        //    // which describe the correspondence (atom in molecule -> MMPL atom type) are being codified here.
        //    List<AtomSource> bondedAtoms = atom.BondedAtoms;
        //    switch (atom.Element)
        //    {
        //        case Element.H:
        //            AtomSource polarPartner = bondedAtoms.SingleOrDefault(partner => partner.Element != Element.C);
        //            if (polarPartner != null)
        //            {
        //                atom.Radius = 0.91f;
        //                atom.Epsilon = 0.01001f;
        //            }
        //            else
        //            {
        //                atom.Radius = 2.8525f;
        //                atom.Epsilon = 0.038f;
        //            }
        //            break;
        //        case Element.C:
        //            if (bondedAtoms.Count == 3)
        //            {
        //                atom.Radius = 4.2202f;
        //                atom.Epsilon = 0.03763f;
        //            }
        //            else if (bondedAtoms.Count == 4)
        //            {
        //                atom.Radius = 4.315f;
        //                atom.Epsilon = 0.07382f;
        //            }
        //            else
        //                throw new Exception(String.Format("MMPL only defines tri-valent and tetra-valent Carbon's radius and epsilon values, but this atom bonds {0} other atoms. Is the PDB missing CONECT records?", bondedAtoms.Count));
        //            break;
        //        case Element.N:
        //            if (atom.Charge < -0.5)
        //            {
        //                // Charged Nitrogen
        //                atom.Radius = 3.92965f;
        //                atom.Epsilon = 0.34705f;
        //            }
        //            else
        //            {
        //                // Uncharged Nitrogen
        //                atom.Radius = 3.8171f;
        //                atom.Epsilon = 0.41315f;
        //            }
        //            //else
        //            //    throw new Exception(String.Format("MMPL only defines tri-valent and tetra-valent Nitrogen's radius and epsilon values, but this atom bonds {0} other atoms. Is the PDB missing CONECT records?", bondedAtoms.Count));
        //            break;
        //        case Element.O:
        //            atom.Mass = 15.999f;
        //            if (bondedAtoms.Count == 1)
        //            {
        //                // Note: The charge threshold for differentiating is 
        //                // arbitrary. I'm not sure 
        //                if (atom.Charge < -0.5)
        //                {   // MONO_VALENT_OXYGEN_C (C means charged)
        //                    atom.Radius = 3.19192f;
        //                    atom.Epsilon = 0.15522f;
        //                }
        //                else
        //                {   // MONO_VALENT_OXYGEN_U (U means uncharged)
        //                    atom.Radius = 3.1005f;
        //                    atom.Epsilon = 0.18479f;
        //                }
        //            }
        //            else if (bondedAtoms.Count == 2)
        //            {
        //                atom.Radius = 3.55322f;
        //                atom.Epsilon = 0.18479f;
        //            }
        //            else
        //                throw new Exception(String.Format("MMPL only defines tri-valent and tetra-valent Nitrogen's radius and epsilon values, but this atom bonds {0} other atoms. Is the PDB missing CONECT records?", bondedAtoms.Count));
        //            break;
        //        case Element.P:
        //            // Note: Suspicious that MMPL defines S and P identically. Mistake?
        //            atom.Radius = 4.315f;
        //            atom.Epsilon = 0.07382f;
        //            break;
        //        case Element.S:
        //            atom.Radius = 4.315f;
        //            atom.Epsilon = 0.07382f;
        //            break;
        //        case Element.Cl: // Chloride 1-Ion
        //            atom.Radius = 4.83858f;
        //            atom.Epsilon = 0.084f;
        //            break;
        //        default:
        //            throw new Exception(String.Format("MMPL does not define the atom type {0}", atom.Element.ToString()));
        //    }
        //}

        //public static void SetAtomicMass(AtomSource atom)
        //{
        //    switch (atom.Element)
        //    {
        //        case Element.H:
        //            atom.Mass = 1.008f;
        //            break;
        //        case Element.C:
        //            atom.Mass = 12.011f;
        //            break;
        //        case Element.N:
        //            atom.Mass = 14.007f;
        //            break;
        //        case Element.O:
        //            atom.Mass = 15.999f;
        //            break;
        //        case Element.P:
        //            atom.Mass = 30.974f;
        //            break;
        //        case Element.S:
        //            atom.Mass = 32.064f;
        //            break;
        //        case Element.Cl:
        //            atom.Mass = 35.450f;
        //            break;
        //        default:
        //            throw new Exception(String.Format(@"Element ""{0}"" has not been assigned a mass in Atom.cs", atom.Element.ToString()));
        //    }
        //}

        //// Create a shallow copy (don't allow references to template atom's backing)
        //public AtomSource Copy()
        //{
        //    AtomSource copy = MemberwiseClone() as AtomSource;
        //    Debug.Assert(copy != null);
        //    copy.BondedAtoms = new List<AtomSource>();    
        //    return copy;
        //}
    }
}
