using Core.Interfaces;
using Microsoft.Xna.Framework;
using NamespacePdb;
using NamespaceUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace Core.PDB
{
    public enum PdbFormat
    {
        DefaultPdbV3,
        BMRB,
        RosettaPdbV1,
        Count
        
    }

    public enum PdbLoadOptions
    {
        WellFormatted = 0,
        RequireNCAC = 1,
        RequireNonNCAC = 2,
        MissingAtomsNaN = 4,
        Default = RequireNCAC | MissingAtomsNaN
    }  

    public class PdbQuick
    {
        static Dictionary<string, List<string>> alternateAtomNames_ = new Dictionary<string, List<string>>();
        static Dictionary<PdbFormat, Dictionary<string, string>> alternateAtomNamesByFormat_ = new Dictionary<PdbFormat, Dictionary<string, string>>();

        static PdbFormat OutputPdbFormat = PdbFormat.DefaultPdbV3;

        const string _rcsbRootUrl = "http://www.rcsb.org/pdb/files";

        static PdbQuick()
        {
            alternateAtomNames_["H1"] = new List<string>(new string[] { "1H" });
            alternateAtomNames_["H2"] = new List<string>(new string[] { "2H" });
            alternateAtomNames_["H3"] = new List<string>(new string[] { "3H" });

            alternateAtomNames_["HA1"] = new List<string>(new string[] { "1HA", "HA3", "3HA" });
            alternateAtomNames_["HA2"] = new List<string>(new string[] { "2HA" });

            alternateAtomNames_["HB1"] = new List<string>(new string[] { "1HB", "HB3", "3HB" });
            alternateAtomNames_["HB2"] = new List<string>(new string[] { "2HB" });
            alternateAtomNames_["HB3"] = new List<string>(new string[] { "3HB", "HB1", "1HB" });

            alternateAtomNames_["HD1"] = new List<string>(new string[] { "1HD", "HD3", "3HD" });
            alternateAtomNames_["HD2"] = new List<string>(new string[] { "2HD" });

            alternateAtomNames_["HE1"] = new List<string>(new string[] { "1HE", "3HE", "HE3" });
            alternateAtomNames_["HE2"] = new List<string>(new string[] { "2HE" });
            alternateAtomNames_["HE3"] = new List<string>(new string[] { "3HE", "HE1", "1HE" });

            alternateAtomNames_["HG1"] = new List<string>(new string[] { "1HG", "HG3", "3HG" });
            alternateAtomNames_["HG2"] = new List<string>(new string[] { "2HG" });

            alternateAtomNames_["HZ1"] = new List<string>(new string[] { "1HZ", "HZ3", "3HZ" });
            alternateAtomNames_["HZ2"] = new List<string>(new string[] { "2HZ" });
            alternateAtomNames_["HZ3"] = new List<string>(new string[] { "3HZ", "HZ1", "1HZ" });

            alternateAtomNames_["HD11"] = new List<string>(new string[] { "1HD1" });
            alternateAtomNames_["HD12"] = new List<string>(new string[] { "2HD1" });
            alternateAtomNames_["HD13"] = new List<string>(new string[] { "3HD1" });
            alternateAtomNames_["HD21"] = new List<string>(new string[] { "1HD2" });
            alternateAtomNames_["HD22"] = new List<string>(new string[] { "2HD2" });
            alternateAtomNames_["HD23"] = new List<string>(new string[] { "3HD2" });

            alternateAtomNames_["HE21"] = new List<string>(new string[] { "1HE2" });
            alternateAtomNames_["HE22"] = new List<string>(new string[] { "2HE2" });

            alternateAtomNames_["HG11"] = new List<string>(new string[] { "1HG1", "HG13", "3HG1" });
            alternateAtomNames_["HG12"] = new List<string>(new string[] { "2HG1" });
            alternateAtomNames_["HG13"] = new List<string>(new string[] { "3HG1" });
            alternateAtomNames_["HG21"] = new List<string>(new string[] { "1HG2", "HG23", "3HG2" });
            alternateAtomNames_["HG22"] = new List<string>(new string[] { "2HG2" });
            alternateAtomNames_["HG23"] = new List<string>(new string[] { "3HG2" });

            alternateAtomNames_["HH11"] = new List<string>(new string[] { "1HH1", "HH13", "3HH1" });
            alternateAtomNames_["HH12"] = new List<string>(new string[] { "2HH1" });
            alternateAtomNames_["HH21"] = new List<string>(new string[] { "1HH2", "HH23", "3HH2" });
            alternateAtomNames_["HH22"] = new List<string>(new string[] { "2HH2" });
        }

        //public void LoadAtomNamingTable()
        //{
        //    foreach(PdbFormat format in Enum.GetValues(typeof(PdbFormat)))
        //    {
        //        alternateAtomNamesByFormat_[format] = new Dictionary<string, string>();
        //    }

        //    foreach (string line in File.ReadAllLines(Path.Combine("Database", "PdbFormatAtomNames.csv")))
        //    {
        //        if (line.StartsWith("#") || String.IsNullOrWhiteSpace(line))
        //            continue;
        //        string[] words = line.Split('\t');
        //        Debug.Assert(words.Length == 3);

        //        string residue = words[0];
        //        string atomNamePdbV3=words[1];
        //        string atomNameBmrb=words[2];
        //        string atomnamePdbV1=words[3];
        //        string key = residue + "_" + atomNamePdbV3;
        //        alternateAtomNamesByFormat_[PdbFormat.DefaultPdbV3][key] = atomNamePdbV3;
        //        alternateAtomNamesByFormat_[PdbFormat.BMRB][key] = atomNameBmrb;
        //        alternateAtomNamesByFormat_[PdbFormat.DefaultPdbV3][key] = atomNamePdbV3;
        //    }
        //}

        public static void Save(string file, IAa residue, char chainId = 'A')
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                int atomNumber = 1;
                int residueNumber = 1;
                Save(writer, chainId, residue, ref atomNumber, ref residueNumber);
            }
        }

        public static Model ModelFromFileOrCode(string code)
        {
            Structure structure = (Structure) AssemblyFromFileOrCode(code);
            Model model = new Model(code, structure);
            return model;
        }

        public static void Save(TextWriter writer, IEnumerable<IChain> structure, ref int residueNumber, ref int atomNumber, ref char chainId)
        {
            foreach(IChain chain in structure)
            {
                Save(writer, chain, ref residueNumber, ref atomNumber, chainId);
                chainId = GetNextChainId(chainId);
                residueNumber = 1;
            }
            writer.Flush();
        }

        public static void Save(string file, IEnumerable<IChain> structure, ref int residueNumber, ref int atomNumber, ref char chainId)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                Save(writer, structure, ref residueNumber, ref atomNumber, ref chainId);
            }
        }

        public static void Save(string file, IEnumerable<IStructure> structures)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                int residueNumber = 1;
                int atomNumber = 1;
                char chain = 'A';
                foreach (IStructure structure in structures)
                {
                    Save(writer, structure, ref residueNumber, ref atomNumber, ref chain);
                }
            }
        }

        public static Model ModelFromText(string fileContentsUnicode, string name = null)
        {
            Structure structure = AssemblyFromText(fileContentsUnicode);
            Model model = new Model(name, structure);
            return model;
        }

        public static void Save(TextWriter stream, IChain peptide, ref int residueNumber, ref int atomNumber, char chainId = 'A')
        {
            foreach (IAa residue in peptide)
            {
                Save(stream, chainId, residue, ref atomNumber, ref residueNumber);
            }
            Save(stream, new TerRecord());
            stream.Flush();
        }

        private static void Save(TextWriter stream, Record record)
        {
            stream.WriteLine(record.Text);
        }

        public static void Save(string file, IChain peptide, char chainId = 'A')
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                int atomNumber = 1;
                int residueNumber = 1;
                foreach (IAa residue in peptide)
                {
                    Save(writer, chainId, residue, ref atomNumber, ref residueNumber);
                }
            }
        }

        public static void Save(string file, IEnumerable<IAa> residues, char chainId = 'A')
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                int atomNumber = 1;
                int residueNumber = 1;
                foreach (IAa residue in residues)
                {
                    Save(writer, chainId, residue, ref atomNumber, ref residueNumber);
                }
            }
        }

        public static void Save(string file, IStructure assembly)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                int atomNumber = 1;
                int residueNumber = 1;
                char chainId = 'A';
                foreach (IChain chain in assembly)
                {
                    Save(writer, chain, ref residueNumber, ref atomNumber, (char) chainId);

                    chainId = GetNextChainId(chainId);
                    residueNumber = 1;
                }
            }
        }

        public static void Save(string file, IEnumerable<IChain> chains)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                int atomNumber = 1;
                int residueNumber = 1;
                char chain = 'A';
                foreach (IChain peptide in chains)
                {
                    Save(writer, peptide, ref residueNumber, ref atomNumber, chain++);
                    residueNumber = 1;
                }

            }
        }

        public static void Save(string directory, IEnumerable<Model> models)
        {

        }

        public static void SaveXyzCoordinates(string file, IEnumerable<Vector3> coordinates)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                int residueNumber = 1;
                int atomNumber = 1;
                SaveXyzCoordinates(writer, coordinates.ToArray(), ref residueNumber, ref atomNumber);
            }
        }

        static void SaveXyzCoordinates(TextWriter stream, Vector3[] coordinates, ref int residueNumber, ref int atomNumber)
        {
            foreach (Vector3 coordinate in coordinates)
            {
                AtomRecord record = new AtomRecord();
                record.XYZ = coordinate;
                record.Name = "XYZ";
                record.ResidueName = "XYZ";
                record.Element = Element.H;
                record.ResidueSequenceNumber = residueNumber++;
                record.Serial = atomNumber++;
                stream.WriteLine(record.Text);
            }
        }

        public static char GetNextChainId(char chain)
        {
            byte chainId = (byte)chain;
            // Use ascii characters 32-127 which pymol seems okay with
            chainId++;
            chainId %= 127;

            if (chainId == 0)
                chainId = 32;

            // pymol barfs on these characters when running simple commands - only tried 'util.cbc'
            // to identify these
            while (chainId == 34 || chainId == 40 || chainId == 41)
                chainId++;
            return (char)chainId;
        }

        public static SS[] GetPeptideSeqresSS(string file)
        {
            IEnumerable<Record> records = GetPdbRecords(file);
            IEnumerable<SeqresRecord> seqresRecords = records.Select(record => record as SeqresRecord).Where(record => record != null);
            IEnumerable<HelixRecord> helixRecords = records.Select(record => record as HelixRecord).Where(record => record != null);

            if (seqresRecords.Count() == 0)
                return null;
            if (helixRecords.Count() == 0)
                return null;


            SeqresRecord firstChainSeqres = seqresRecords.First();
            SS[] ss = new SS[firstChainSeqres.NumRes];
            foreach (HelixRecord helixRecord in helixRecords)
            {
                if (helixRecord.InitChainId != firstChainSeqres.ChainId)
                    continue;

                for (int i = helixRecord.InitSeqNum - 1 /* 1-indexed -> 0-indexed */; i < helixRecord.EndSeqNum; i++)
                {
                    // Return null if records are unreliable
                    if (i < 0 || ss.Length <= i)
                        return null;
                    ss[i] = SS.Helix;
                }
            }
            return ss;
        }

        public static Structure AssemblyFromFileOrCode(string file, PdbLoadOptions loadOptions = PdbLoadOptions.Default)
        {
            Structure protein = new Structure();
            IEnumerable<AtomRecord> records = GetPdbRecords(file, false).Select(r => r as AtomRecord).Where(r => r != null).Where(r => r as HetatmRecord == null);

            List<char> letters = records.Select(record => record.ChainId).Distinct().ToList();

            foreach (char chainId in records.Select(record => record.ChainId).Distinct())
            {
                IEnumerable<AtomRecord> chainRecords = records.Where(record => record.ChainId == chainId);
                IChain chain = ChainFromRecords(chainRecords);
                protein.Add(chain);
            }
            return protein;
        }

        public static Structure AssemblyFromText(string text, PdbLoadOptions loadOptions = PdbLoadOptions.Default)
        {
            using (TextReader stream = new StringReader(text))
            {

                IEnumerable<AtomRecord> records = LoadRecords(stream).Where(record => record is AtomRecord).Cast<AtomRecord>();
                List<char> letters = records.Select(record => record.ChainId).Distinct().ToList();

                Structure protein = new Structure();
                foreach (char chainId in letters)
                {
                    IEnumerable<AtomRecord> chainRecords = records.Where(record => record.ChainId == chainId);
                    IChain chain = ChainFromRecords(chainRecords);
                    protein.Add(chain);
                }

                if (protein.Count == 0)
                    return null;

                return protein;
            }
        }

        public static IChain ChainFromFileOrCode(string file, PdbLoadOptions loadOptions = PdbLoadOptions.Default, char? chainId = null)
        {
            IEnumerable<AtomRecord> records = GetPdbRecords(file).Select(record => record as AtomRecord).Where(record => record as HetatmRecord == null);
            IChain chain = ChainFromRecords(records, loadOptions, chainId);
            return chain;
        }

        public static IChain ChainFromText(string text, PdbLoadOptions loadOptions = PdbLoadOptions.Default, char? chainId = null)
        {
            using (TextReader stream = new StringReader(text))
            {
                IEnumerable<AtomRecord> records = LoadRecords(stream).Where(record => record is AtomRecord).Cast<AtomRecord>();
                IChain chain = ChainFromRecords(records, loadOptions, chainId);
                return chain;
            }
        }

        public static IChain ChainFromRecords(IEnumerable<AtomRecord> records, PdbLoadOptions loadOptions = PdbLoadOptions.Default, char? chainId = null)
        { 
            bool wellFormatted = (loadOptions) == PdbLoadOptions.WellFormatted;
            bool requireNCAC = (loadOptions & PdbLoadOptions.RequireNCAC) != 0;
            bool requireNonNCAC = (loadOptions & PdbLoadOptions.RequireNonNCAC) != 0;
            bool missingAtomsNaN = (loadOptions & PdbLoadOptions.MissingAtomsNaN) != 0;

            IEnumerable<AtomSource> allAtoms = records.Where(record => record != null).Select(record => record.ToAtom());
            IEnumerable<IGrouping<char, AtomSource>> chains = allAtoms.GroupBy(atom => atom.ChainIndex);
            foreach (IGrouping<char, AtomSource> chain in chains)
            {
                bool chainFailed = false;
                char actualChainId = chain.Key;
                if (chainId != null && chainId != actualChainId)
                    continue;

                Chain peptideQuick = new Chain();
                List<IGrouping<int, AtomSource>> pdbResidues = chain.GroupBy(atom => atom.ResidueIndex).OrderBy(group => group.Key).ToList();
                foreach(IGrouping<int, AtomSource> pdbResidue in pdbResidues)
                {
                    bool nTerminus = pdbResidue == pdbResidues.First();
                    bool cTerminus = pdbResidue == pdbResidues.Last();
                    AtomSource N = pdbResidue.FirstOrDefault(atom => atom.Name == "N");
                    AtomSource CA = pdbResidue.FirstOrDefault(atom => atom.Name == "CA");
                    AtomSource C = pdbResidue.FirstOrDefault(atom => atom.Name == "C");

                    // Get the name and create the residue
                    string name3 = pdbResidue.First().ResidueName;
                    if (name3 == "HIS") // TODO: Is this the best way of doing this?
                    {
                        if (pdbResidue.FirstOrDefault(atom => atom.Name == "HD1" || atom.Name == "1HD") != null)
                            name3 = "HID";
                        else
                            name3 = "HIE";
                    }

                    // Default to non-disulfide and then replace with disulfides when CYH are nearby with nearby SG atoms
                    if (name3 == "CYS")
                        name3 = "CYH";

                    if (!AaTable.IsResidueNameKnown(name3))
                    {
                        // TODO: Handle ions in some way
                        if ((new string[] { "ZN", "HOH", "H20", "AU", "CA", "MG", "SO4" }.Contains(name3)))
                            continue;


                        Console.WriteLine("Failing on unknown residue " + name3);
                        chainFailed = true;
                        break;
                    }
                    IAa residueQuick = new Aa(name3, nTerminus, cTerminus);

                    // Check for existence of atoms
                    if (N == null || CA == null || C == null)
                    {
                        if(requireNCAC)
                        {
                            // TODO: Tracer output
                            chainFailed = true;
                            break;
                        }
                    }
                    else if (float.IsNaN(N.XYZ.Length()) || float.IsNaN(CA.XYZ.Length()) || float.IsNaN(C.XYZ.Length()))
                    {
                        if (requireNCAC)
                        {
                            // TODO: Tracer output
                            chainFailed = true;
                            break;
                        }
                    }
                    else
                    {
                        residueQuick.AlignToNCAC(N.XYZ, CA.XYZ, C.XYZ);
                    }

                    if (missingAtomsNaN)
                    {
                        // Only explicitly PDB-defined atoms have real coordinates
                        for (int i = 0; i < residueQuick.Count; i++)
                        {
                            residueQuick[i].Xyz = new Vector3(float.NaN, float.NaN, float.NaN);
                        }
                    }

                    // Method 1
                    //foreach(AtomQuick atomQuick in residueQuick)
                    //{
                    //    AtomSource atomSource = pdbResidue.FirstOrDefault(atomPdb => atomPdb.Name == atomQuick.Name);

                    //    // Some PDBs are formatted with hydrogens like 2HD1 instead of HD12
                    //    AtomSource alternateAtomSource = null;
                    //    if(atomQuick.Name.StartsWith("H") && Char.IsNumber(atomQuick.Name.Last())) {
                    //       string alternateName = atomQuick.Name.Last() + atomQuick.Name.Substring(0, atomQuick.Name.Length - 1);
                    //       alternateAtomSource = pdbResidue.FirstOrDefault(atomPdb => atomPdb.Name == alternateName);
                    //    }

                    //    if(atomSource != null)
                    //    {
                    //        atomQuick.XYZ = atomSource.XYZ;
                    //    }
                    //    else if (alternateAtomSource != null)
                    //    {
                    //        atomQuick.XYZ = alternateAtomSource.XYZ;
                    //    }
                    //    else if (!allowMissingAtoms)
                    //    {
                    //        // TODO ... return null?
                    //        continue;
                    //    }
                    //}

                    // Method 2 - explicit atom name alternatives
                    // First pass: initialize atom locations based on exact name-matched PDB records
                    List<IAtom> initializedAtoms = new List<IAtom>();
                    List<AtomSource> usedSourceAtoms = new List<AtomSource>();
                    List<IAtom> uninitializedAtoms = new List<IAtom>(residueQuick);
                    foreach (IAtom atomQuick in residueQuick)
                    {
                        AtomSource atomSource = pdbResidue.FirstOrDefault(atomPdb => atomPdb.Name == atomQuick.Name);

                        if (atomSource != null)
                        {
                            atomQuick.Xyz = atomSource.XYZ;
                            initializedAtoms.Add(atomQuick);
                            uninitializedAtoms.Remove(atomQuick);
                            usedSourceAtoms.Add(atomSource);
                        }
                    }

                    // Second pass: try to initialize any remaining atoms from PDB records corresponding to known alternative atom names. Multiple
                    // passes are made to try to resolve the names, moving from more to less preferred. 
                    int alternateAtomNamesMaxCount = alternateAtomNames_.Values.Select(value => value.Count).Max();
                    for (int nameIndex = 0; nameIndex < alternateAtomNamesMaxCount; nameIndex++)
                    {
                        foreach (Atom atomQuick in residueQuick)
                        {
                            if (initializedAtoms.Contains(atomQuick))
                                continue;

                            List<string> alternativeAtomNames = null;
                            if (!alternateAtomNames_.TryGetValue(atomQuick.Name, out alternativeAtomNames))
                                continue;
                            if (alternativeAtomNames.Count <= nameIndex)
                                continue;

                            IEnumerable<AtomSource> atomSources = pdbResidue.Where(atomPdb => alternativeAtomNames[nameIndex] == atomPdb.Name);
                            foreach (AtomSource atomSource in atomSources)
                            {
                                if (usedSourceAtoms.Contains(atomSource))
                                    continue;
                                atomQuick.Xyz = atomSource.XYZ;
                                initializedAtoms.Add(atomQuick);
                                uninitializedAtoms.Remove(atomQuick);
                                usedSourceAtoms.Add(atomSource);
                            }
                        }
                    }

                    if (initializedAtoms.Count != residueQuick.Count)
                    {
                        if (!requireNCAC && !requireNonNCAC)
                            continue;

                        IEnumerable<IAtom> uninitializedNCAC = uninitializedAtoms.Where(atom => atom.Name == "N" || atom.Name == "CA" || atom.Name == "C").Where(atom => float.IsNaN(atom.Xyz.Length()));
                        IEnumerable<IAtom> uninitializedNonNCAC = uninitializedAtoms.Where(atom => atom.Name != "N" && atom.Name != "CA" && atom.Name != "C").Where(atom => float.IsNaN(atom.Xyz.Length()));
                        if (requireNCAC && uninitializedNCAC.Count() > 0  )
                        {
                            chainFailed = true;
                            break;
                        }

                        if (requireNonNCAC && uninitializedNonNCAC.Count() > 0)
                        {
                            chainFailed = true;
                            break;
                        }

                        //// TODO: Create trace output and move this there
                        //Console.WriteLine("Failed to initialize residue " + residueQuick.Name);
                        //foreach (AtomQuick atom in uninitializedAtoms)
                        //{
                        //    Console.WriteLine(atom.Name);
                        //}

                        //chainFailed = true;
                        //break;
                    }


                    peptideQuick.Add(residueQuick);
                }

                if (!chainFailed && peptideQuick.Count > 0)
                {
                    AddDisulfides(peptideQuick);
                    return peptideQuick;
                }
            }
            return null;
        }

        public static string CodeFromFilePath(string file)
        {
            string pdbCode = null;
            char pathDelimiter = Path.DirectorySeparatorChar;
            int lastDeliminterIndex = file.LastIndexOf(pathDelimiter);
            if (lastDeliminterIndex >= 0)
                pdbCode = file.Substring(lastDeliminterIndex + 1, file.Length - lastDeliminterIndex - 1);

            pdbCode = pdbCode.Replace(".pdb.gz", "").Replace(".pdb", "");
            return pdbCode;
        }

        static List<Record> GetPdbRecords(string pdbOrFile, bool singleModel = true)
        {
            string file = pdbOrFile;
            string pdbCode = file.Replace(".pdb", "").Replace(".ent", "").Replace(".gz", "");
            //List<string> lines = new List<string>();

            List<Record> records = new List<Record>();

            if (!File.Exists(file))
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        Uri uri = new Uri(String.Format("{0}/{1}.pdb.gz", _rcsbRootUrl, pdbCode));
                        byte[] data = client.DownloadData(uri);
                        using (Stream zipStream = new MemoryStream(data))
                        {
                            using (GZipStream dataStream = new GZipStream(zipStream, CompressionMode.Decompress))
                            {
                                using (TextReader textReader = new StreamReader(dataStream))
                                {
                                    records = LoadRecords(textReader);
                                }
                            }
                        }
                    }
                }
                catch (WebException) { }
                catch (InvalidDataException) { }
            }
            else if (file.EndsWith(".gz"))
            {
                // The first stream reads the file, the second one decompresses it, and the third reads 
                // formats it as text/string data.
                using (FileStream zipStream = File.OpenRead(file))
                {
                    using (GZipStream dataStream = new GZipStream(zipStream, CompressionMode.Decompress))
                    {
                        using (TextReader textReader = new StreamReader(dataStream))
                        {
                            records = LoadRecords(textReader);
                        }
                    }
                }
            }
            else if (file.EndsWith(".pdb") || file.EndsWith(".ent"))
            {
                using (TextReader textReader = new StreamReader(file))
                {
                    records = LoadRecords(textReader);
                }
            }

            if (singleModel)
            {
                Record firstEndmdlRecord = records.FirstOrDefault(record => (record as EndmdlRecord) != null);
                if (firstEndmdlRecord != null)
                {
                    int desiredCount = records.IndexOf(firstEndmdlRecord);
                    records = records.GetRange(0, desiredCount);
                }
            }

            return records;
        }

        static List<Record> LoadRecords(TextReader reader)
        {
            List<Record> records = new List<Record>();
            String line = null;
            while ((line = reader.ReadLine()) != null)
            {
                Record record = Record.GetRecord(line);
                if (record != null)
                    records.Add(record);
            }
            return records;
        }

        static void Save(TextWriter stream, char chainId, IAa residue, ref int atomNumber, ref int residueSequenceNumber)
        {
            foreach (IAtom atom in residue)
            {
                if (float.IsNaN(atom.Xyz.Length()) || atom.Element == Element.H && atom.Xyz == Vector3.Zero)
                    continue;

                AtomRecord record = new AtomRecord(atom, chainId, residue.Name.ToString(), residueSequenceNumber, atomNumber++);
                atomNumber %= 100000;
                switch (OutputPdbFormat)
                {
                    case PdbFormat.DefaultPdbV3: break;
                    case PdbFormat.BMRB: break;
                    case PdbFormat.RosettaPdbV1:
                        string name = record.Name;
                        if (alternateAtomNamesByFormat_[OutputPdbFormat].TryGetValue(residue.Letter + "_" + name, out name) ||
                            alternateAtomNamesByFormat_[OutputPdbFormat].TryGetValue("X_" + name, out name))
                            record.Name = name;
                        break;
                    default: throw new InvalidDataException();
                }
                stream.WriteLine(record.Text);
            }
            residueSequenceNumber++;
            residueSequenceNumber %= 10000;
            if (residueSequenceNumber == 0)
                residueSequenceNumber++;
        }

        static void AddDisulfides(Chain peptide)
        {
            for(int i = 0; i < peptide.Count - 1; i++)
            {
                IAa residue1 = peptide[i];
                if (residue1.Name != "CYH")
                    continue;
                for (int j = i + 1; j < peptide.Count; j++)
                {
                    IAa residue2 = peptide[j];
                    if (residue2.Name != "CYH")
                        continue;

                    Vector3 SG1 = residue1["SG"].Xyz;
                    Vector3 SG2 = residue2["SG"].Xyz;
                    if (!VectorMath.IsValid(SG1) || !VectorMath.IsValid(SG2))
                        continue;

                    if(Vector3.Distance(SG1, SG2) < 2.5)
                    {
                        IAa replace1 = new Aa("CYS", residue1.IsNTerminus, residue1.IsCTerminus);
                        replace1.AlignToNCAC(residue1);
                        foreach (IAtom atom in residue1)
                        {
                            IAtom match = replace1[atom.Name];
                            if(match != null)
                            {
                                match.Xyz = atom.Xyz;
                            }
                        }

                        IAa replace2 = new Aa("CYS", residue2.IsNTerminus, residue2.IsCTerminus);
                        replace2.AlignToNCAC(residue2);
                        foreach (IAtom atom in residue2)
                        {
                            IAtom match = replace2[atom.Name];
                            if (match != null)
                            {
                                match.Xyz = atom.Xyz;
                            }
                        }

                        peptide[i, true] = replace1;
                        peptide[j, true] = replace2;
                        break;
                    }
                }
            }
        }
    }
}
