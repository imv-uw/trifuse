using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Core.Quick;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Core
{
    /// <summary>
    /// This class contains a table of all residue types, defined as a tuple of [canonical residue type, centroid, n-terminus, c-terminus].
    /// For each one, a list of atoms and their initial coordinates are defined. The N, CA, and C atoms have their centroid at the origin, to speed up
    /// RMSD-minimizing alignment calculations.
    /// 
    /// In addition, each atom 
    ///
    /// </summary>
    public class AaTable
    {
        // Multidimensional array indexes are residue number, nterminus (0 or 1), cterminus (0 or 1)
        static AaDefinition[,,] aaDefinitions_ = null;
        static AtomDefinition[,,][] atomDefinitions_ = null;
        static Dictionary<string, int> residueNameToIndex_ = new Dictionary<string, int>();
        static Dictionary<char, int> residueCharToIndex_ = new Dictionary<char, int>();
        static char[] residueClassNumberToLetter_ = null;

        public static int AaTypeUndefined
        {
            get
            {
                return AaCount;
            }
        }

#if DEBUG
        static int _aaCount = 0;
        public static int AaCount {
            get
            {
                Initialize();
                return _aaCount;
            }
            set
            {
                _aaCount = value;
            }
        }
#else
        public static int AaCount { get; private set; }
#endif
        public static IEnumerable<int> GetAaTypes() { return Enumerable.Range(0, AaCount); }

        static AaTable()
        {
#if !DEBUG
            Initialize();
#endif
        }


        //static bool initialized = false;
#if DEBUG
        public static bool IsInitialized { private set; get; }

        public
#endif
        static void Initialize()
        {
#if DEBUG
            if (IsInitialized)
                return;

            IsInitialized = true;
#endif

            string json = File.ReadAllText(Database.Paths.Json.ResiduesPath);
            JArray residues = JArray.Parse(json);
            aaDefinitions_ = new AaDefinition[residues.Count, 2, 2];
            atomDefinitions_ = new AtomDefinition[residues.Count, 2, 2][];
            residueClassNumberToLetter_ = new char[residues.Count];
            AaCount = residues.Count;

            // index 1
            for (int residueIndex = 0; residueIndex < residues.Count; residueIndex++)
            {   // keep array calling convention reference: AtomQuickDefinition[] allPossibleResidueAtoms = JsonConvert.DeserializeObject<AtomQuickDefinition[]>((residues.ElementAt(i)["atoms"].ToString())); 
                AaDefinition residue = JsonConvert.DeserializeObject<AaDefinition>((residues.ElementAt(residueIndex).ToString()));

                if (!residueCharToIndex_.ContainsKey(residue.Letter))
                    residueCharToIndex_[residue.Letter] = residueIndex;

                residueClassNumberToLetter_[residueIndex] = residue.Letter;

                // index 2
                for (int nTerminus = 0; nTerminus <= 1; nTerminus++)
                {
                    //index 3
                    Terminus nTerminusCase = nTerminus == 0 ? Terminus.NotNTerminus : Terminus.NTerminus;
                    for (int cTerminus = 0; cTerminus <= 1; cTerminus++)
                    {
                        // TODO: create json files for each residue class (centroid, heavy-atom, etc). will require minor changes like jagged arrays.
                        Terminus cTerminusCase = cTerminus == 0 ? Terminus.NotCTerminus : Terminus.CTerminus;
                        AtomDefinition[] subsetAtoms = residue.Atoms.Where(definition => definition.TerminusCase == Terminus.Always || definition.TerminusCase == nTerminusCase || definition.TerminusCase == cTerminusCase).ToArray();

                        atomDefinitions_[residueIndex, nTerminus, cTerminus] = subsetAtoms;
                        aaDefinitions_[residueIndex, nTerminus, cTerminus] = new AaDefinition(residue.Name, residue.Letter, subsetAtoms);

                        for (int i = 0; subsetAtoms != null && i < subsetAtoms.Length; i++)
                        {
                            AtomDefinition atom = subsetAtoms[i];
                            switch(atom.Name)
                            {
                                case "N": Debug.Assert(i == AaDefinition.IndexN); break;
                                case "CA": Debug.Assert(i == AaDefinition.IndexCA); break;
                                case "C": Debug.Assert(i == AaDefinition.IndexC); break;
                                case "O": Debug.Assert(i == AaDefinition.IndexO); break;
                                //case "H": Debug.Assert(i == ResidueQuickDefinition.IndexH); break;
                            }
                        }
                    }
                }
                residueNameToIndex_[residue.Name] = residueIndex;
                    
            }
        }

        public static AtomDefinition[] GetAtomDefinitions(int residueNumber, bool nTerminus, bool cTerminus)
        {
#if DEBUG
            Initialize();
#endif
            AtomDefinition[] atomDefinitions = atomDefinitions_[residueNumber, nTerminus ? 1 : 0, cTerminus ? 1 : 0];
            return (AtomDefinition[]) atomDefinitions.Clone();
        }

//        public static Atom[] GetAtoms(int residueClass, string name, bool nTerminus, bool cTerminus)
//        {
//#if DEBUG
//            Initialize();
//#endif

//            int residueIndex = residueNameToIndex_[name];
//            return GetAtomDefinitions(residueIndex, nTerminus, cTerminus);
//        }

        public static int GetResidueTypeIndex(string name)
        {
#if DEBUG
            Initialize();
#endif

            return residueNameToIndex_[name];
        }

        public static int GetResidueTypeIndex(char letter)
        {
#if DEBUG
            Initialize();
#endif

            return residueCharToIndex_[letter];
        }

        // Certain letters, like 'H' and 'C' can map to residues that are considered distinct
        // by the implementation here. For example, HIE/HID and CYH/CYS respectively,
        public static List<int> GetResidueTypeIndices(char letter)
        {
#if DEBUG
            Initialize();
#endif

            List<int> matches = new List<int>();
            for(int i = 0; i < residueClassNumberToLetter_.Length; i++)
            {
                if(residueClassNumberToLetter_[i] == letter)
                {
                    matches.Add(i);
                }
            }
            return matches;
        }

        public static char GetResidueLetter(int residueIndex)
        {
#if DEBUG
            Initialize();
#endif

            if(0 <= residueIndex && residueIndex < residueClassNumberToLetter_.Length)
            {
                char letter = residueClassNumberToLetter_[residueIndex];
                return letter;
            }

            return 'X';
        }

        public static string GetResidueName(int residueIndex)
        {
#if DEBUG
            Initialize();
#endif
            return aaDefinitions_[residueIndex, 0, 0].Name;
        }

        public static bool IsResidueNameKnown(string residue)
        {
#if DEBUG
            Initialize();
#endif
            return residueNameToIndex_.ContainsKey(residue);
        }

        public static bool IsResidueLetterKnown(char residue)
        {
#if DEBUG
            Initialize();
#endif
            return residueCharToIndex_.ContainsKey(residue);
        }

    }
}
