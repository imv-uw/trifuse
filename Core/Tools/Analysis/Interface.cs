using Core.Interfaces;
using Core.Quick.Pattern;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tools;

namespace Core.Tools.Analysis
{
    public class Interface
    {
        public static Selection GetCnInterAsuContacts(IStructure cnAsymmetricUnit, int multiplicity, Vector3 axis)
        {
            AxisPattern<IStructure> pattern = new AxisPattern<IStructure>(axis, multiplicity, cnAsymmetricUnit);
            IStructure[] others = pattern[1, multiplicity - 1].ToArray(); // CxUtilities.Pattern(cnAsymmetricUnit, axis, multiplicity, new int[] { 0 }, true);
            Selection selection = Clash.GetContactSelectionInFocusSet(new IStructure[] { cnAsymmetricUnit }, others);
            return selection;
        }

        public static Selection GetCnInterAsuContacts(IStructure cnAsymmetricUnit, int multiplicity, Line axis, Clash.ContactType contactType = Clash.ContactType.Atomic)
        {
            IStructure mirror = cnAsymmetricUnit.GetMirroredElement(true, null);
            AxisPattern<IStructure> pattern = new AxisPattern<IStructure>(axis.Point, axis.Direction, multiplicity, mirror, new int[] { 0 } );
            IStructure[] others = pattern.ToArray();//[1, multiplicity - 1].ToArray(); // CxUtilities.Pattern(cnAsymmetricUnit, axis, multiplicity, new int[] { 0 }, true);
            Selection selection = Clash.GetContactSelectionInFocusSet(new IStructure[] { cnAsymmetricUnit }, others, contactType);
            return selection;
        }

        public static Selection GetContacts(IStructure structure)
        {
            Selection selection = Clash.GetContactSelection(structure.ToArray());
            return selection;
        }

        public static List<int> GetCnInterAsuContactIndices(IStructure cnAsymmetricUnit, int chainIndex, int multiplicity, Vector3 axis)
        {
            Trace.Assert(0 <= chainIndex && chainIndex < cnAsymmetricUnit.Count);

            AxisPattern<IStructure> pattern = new AxisPattern<IStructure>(axis, multiplicity, cnAsymmetricUnit);
            IStructure[] others = pattern[1, multiplicity - 1].ToArray();// CxUtilities.Pattern(cnAsymmetricUnit, axis, multiplicity, new int[] { 0 }, true);

            List<int> interAsuContacts = Clash.GetContactIndices(cnAsymmetricUnit[chainIndex], others.SelectMany(str => str).ToArray());
            return interAsuContacts;
        }

        public static List<int> GetCnInterAsuContactIndices(IChain monomer, int multiplicity, Vector3 axis)
        {
            IChain[] oligomer = new IChain[multiplicity - 1];
            for (int i = 0; i < multiplicity - 1; i++)
            {
                oligomer[i] = new Chain(monomer);
                oligomer[i].RotateRadians(Vector3.Zero, Vector3.UnitZ, (float)((i + 1) * 2 * Math.PI / multiplicity));
            }

            List<int> contacts = Clash.GetContactIndices(monomer, oligomer);
            return contacts;
        }

        public static List<int> GetInterChainContactIndices(IStructure assembly, int chainIndex)
        {
            IChain a = assembly[chainIndex];
            IChain[] others = assembly.Where(chain => chain != a).ToArray();
            List<int> contacts = Clash.GetContactIndices(a, others);
            return contacts;
        }

        public static bool[][] GetCnInterAsuContactBools(IStructure asu, int multiplicity)
        {
            bool[][] contacts = GetCnInterAsuContactBools(asu, multiplicity, Vector3.UnitZ);
            return contacts;
        }

        public static bool[][] GetCnInterAsuContactBools(IStructure asu, int multiplicity, Vector3 axis)
        {
            asu = (IStructure)asu.DeepCopy();
            IStructure mirror = asu.GetMirroredElement(true, null);
            AxisPattern<IStructure> pattern = new AxisPattern<IStructure>(axis, multiplicity, mirror, new int[] { 0 });
            IStructure[] neighbors = pattern.ToArray();

            //IStructure[] neighbors = CxUtilities.Pattern(asu, axis, multiplicity, new int[] { 0 }, true);

            Selection contactAas = Clash.GetContactSelectionInFocusSet(new IStructure[] { asu }, neighbors, Clash.ContactType.Atomic);
            bool[][] contacts = asu.Select(chain => chain.Select(aa => contactAas.Aas.Contains(aa)).ToArray()).ToArray();
            return contacts;
        }

        public static bool[][] GetInterChainContactBools(IStructure structure)
        {
            // Identify inter-chain contacts and then map all aas in the structure to true/false
            Selection contactAas = Clash.GetContactSelection(structure.ToArray());
            bool[][] contacts = structure.Select(chain => chain.Select(aa => contactAas.Aas.Contains(aa)).ToArray()).ToArray();
            return contacts;
        }

        public static bool [][] GetExpendableCnAsuTowardsN(IStructure asu, int multiplicity, int maxInterfaceTruncation = 0)
        {
            bool[][] results = GetCnInterAsuContactBools(asu, multiplicity);
            TransformContactsToExpendableTowardsN(results, maxInterfaceTruncation);
            return results;
        }

        public static bool[][] GetExpendableCnAsuTowardsC(IStructure asu, int multiplicity, int maxInterfaceTruncation = 0)
        {
            bool[][] results = GetCnInterAsuContactBools(asu, multiplicity);
            TransformContactsToExpendableTowardsC(results, maxInterfaceTruncation);
            return results;
        }

        public static bool [][] GetExpendableTowardsN(IStructure structure, int maxInterfaceTruncation = 0)
        {
            // Get a map of contacts and overwrite it to indicate which chain positions
            // have only expendable (non-contact) residues towards their N-terminus
            bool[][] results = GetInterChainContactBools(structure);
            TransformContactsToExpendableTowardsN(results, maxInterfaceTruncation);
            return results;
        }

        public static bool[][] GetExpendableTowardsC(IStructure structure, int maxInterfaceTruncation = 0)
        {
            // Get a map of contacts and overwrite it to indicate which chain positions
            // have only expendable (non-contact) residues towards their N-terminus
            bool[][] results = GetInterChainContactBools(structure);
            TransformContactsToExpendableTowardsC(results, maxInterfaceTruncation);
            return results;
        }

        static void TransformContactsToExpendableTowardsN(bool[][] contacts, int maxInterfaceTruncation = 0)
        {
            foreach (bool[] contact in contacts)
            {
                bool[] expendable = contact;

                // Iterate from N to C
                int? firstContactIndex = null;
                for (int i = 0; i < contact.Length; i++)
                {
                    if (contact[i] && firstContactIndex == null)
                        firstContactIndex = i;

                    expendable[i] = firstContactIndex == null || i < firstContactIndex + maxInterfaceTruncation;
                }
            }
        }

        static void TransformContactsToExpendableTowardsC(bool[][] contacts, int maxInterfaceTruncation = 0)
        {
            foreach (bool[] contact in contacts)
            {
                bool[] expendable = contact;

                // Iterate from C to N
                int? firstContactIndex = null;
                for (int i = contact.Length - 1; i >= 0; i--)
                {
                    if (contact[i] && firstContactIndex == null)
                        firstContactIndex = i;

                    expendable[i] = firstContactIndex == null || firstContactIndex - maxInterfaceTruncation < i;
                }
            }
        }
    }
}
