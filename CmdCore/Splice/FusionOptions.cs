using CmdCore.OptionParsing;
using Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Symmetry;

namespace CmdCore.Splice
{
    public class FusionFlags : Flags
    {
        public uint TopX = 10;

        public override bool ParseArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-topx")
                {
                    if (!UInt32.TryParse(args[i + 1], out TopX))
                    {
                        Console.WriteLine("Flag '-topx' value could not be parsed as unsigned int: {0}", args[i + 1]);
                        return false;
                    }
                    i++;
                    continue;
                }
            }
            return true;
        }
    }

    public class AsymmetricFusionFlags : FusionFlags
    {
        public string PeptideRegex1 = "*";
        public string PeptideRegex2 = "*";
        public Range? Range1;
        public Range? Range2;
        public const string BaseFlag = "-asym_pair";

        public override bool ParseArgs(string[] args)
        {
            if (args.FirstOrDefault() != BaseFlag)
                return false;

            if (!base.ParseArgs(args))
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-range_n")
                {
                    try
                    { 
                        Range1 = new Range((int)UInt32.Parse(args[i + 1]), (int)UInt32.Parse(args[i + 2]));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Field '-range1' parsing failed: {0}", e.Message);
                        return false;
                    }
                }
               
                if (args[i] == "-range_c")
                {
                    try
                    {
                        Range2 = new Range((int)UInt32.Parse(args[i + 1]), (int)UInt32.Parse(args[i + 2]));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Field '-range1' parsing failed: {0}", e.Message);
                        return false;
                    }
                }

                if(args[i] == "-regex_n")
                {
                    try
                    {
                        PeptideRegex1 = args[i + 1];
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Console.WriteLine("Field '-peptide_n' omitted: {0}", e.Message);
                        return false;
                    }
                }

                if (args[i] == "-regex_c")
                {
                    try
                    {
                        PeptideRegex2 = args[i + 1];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Field '-peptide_c' omitted: {0}", e.Message);
                        return false;
                    }
                }
            }

            if (Range1 != null && PeptideRegex1.Contains("*"))
            {
                Console.WriteLine("Explicit ranges cannot be used with a set of peptides or search regular expression");
                return false;
            }

            if(Range2 != null && PeptideRegex2.Contains("*"))
            {
                Console.WriteLine("Explicit ranges cannot be used with a set of peptides or search regular expression");
                return false;
            }

            return true;
        }

        new public static IEnumerable<string> GetUsageOptions()
        {
            return FusionFlags.GetUsageOptions().Concat(new String[] 
            {
                GetFormattedOptionString("-regex_n <path>",         "Full path (or optionally regular expression) of one or more pdbs"),
                GetFormattedOptionString("-regex_c <path>",         "Full path (or optionally regular expression) of one or more pdbs"),
                GetFormattedOptionString("-range_n <int> <int>",    "Limit the allowed alignment ranges to the indicated inclusive range"),
                GetFormattedOptionString("",                        "Only for use with a single N-terminal peptide - not a set"),
                GetFormattedOptionString("-range_c <int> <int>",    "Limit the allowed alignment ranges to the indicated inclusive range"),
                GetFormattedOptionString("",                        "Only for use with a single C-terminal peptide - not a set"),
            });
        }
    }

    public class CyclizeAssemblyFlags : FusionFlags
    {
        public uint Multiplicity = 0;
        public string AssemblyRegex = "*";
        public string RepeatRegex = "*";
        public int ChainIndex1 = -1;
        public int ChainIndex2 = -1;

        public override bool ParseArgs(string[] args)
        {
            if (!base.ParseArgs(args))
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-multiplicity")
                {
                    if (!UInt32.TryParse(args[i + 1], out Multiplicity))
                    {
                        Console.WriteLine("Flag '-multiplicity' could not be parsed as unsigned int: {0}", args[i + 1]);
                        return false;
                    }

                    if (Multiplicity < 2)
                    {
                        Console.WriteLine("Flag '-multiplicity' must be 2 or greater");
                        return false;
                    }
                    i++;
                    continue;
                }

                if (args[i] == "-regex_repeat")
                {
                    RepeatRegex = args[i + 1];
                    i++;
                    continue;
                }

                if (args[i] == "-regex_assembly")
                {
                    AssemblyRegex = args[i + 1];
                    i++;
                    continue;
                }

                if (args[i] == "-chain_index1")
                {
                    if (!Int32.TryParse(args[i + 1], out ChainIndex1))
                    {
                        Console.WriteLine("Flag '-chain_index1' could not be parsed as unsigned int: {0}", args[i + 1]);
                        return false;
                    }

                    if (ChainIndex1 < 0)
                    {
                        Console.WriteLine("Flag '-chain_index1' must be 0 or greater, different from '-chain_id2', and less than the assembly chain count.");
                        return false;
                    }
                    i++;
                    continue;
                }

                if (args[i] == "-chain_index2")
                {
                    if (!Int32.TryParse(args[i + 1], out ChainIndex2))
                    {
                        Console.WriteLine("Flag '-chain_index2' could not be parsed as unsigned int: {0}", args[i + 1]);
                        return false;
                    }

                    if (ChainIndex2 < 0)
                    {
                        Console.WriteLine("Flag '-chain_index2' must be 0 or greater, different from '-chain_id1', and less than the assembly chain count.");
                        return false;
                    }
                    i++;
                    continue;
                }
            }

            if(Multiplicity < 2)
            {
                Console.WriteLine("Flag '-multiplicity' is required and must be 2 or greater");
                return false;
            }

            if (String.IsNullOrWhiteSpace(AssemblyRegex))
            {
                Console.WriteLine("Flag '-regex_assembly' is required and must match one or more multi-chain PDB structures");
                return false;
            }

            if(ChainIndex1 < 0)
            {
                Console.WriteLine("Flag '-chain_id1' must be specified");
                return false;
            }

            if (ChainIndex2 < 0)
            {
                Console.WriteLine("Flag '-chain_id2' must be specified");
                return false;
            }

            if (ChainIndex1 == ChainIndex2)
            {
                Console.WriteLine("Flags '-chain_id1' and '-chain_id2' must be different");
                return false;
            }

            return true;
        }

        new public static IEnumerable<string> GetUsageOptions()
        {
            return FusionFlags.GetUsageOptions().Concat(new String[]
            {
                GetFormattedOptionString("-multiplicity",               "An integer 2 or greater;  the number of times the assembly should be patterned."),
                GetFormattedOptionString("-regex_assembly <regex>",     "Regular expression naming at least one multi-chain assembly. All scaffold directories are searched by default."),
                GetFormattedOptionString("-regex_repeat <regex>",       "Regular expression naming repeat proteins. The DHR scaffold directory is searched by default."),
                GetFormattedOptionString("-chain_index1 <char>",        "Assembly chain index to use at the N-terminus of the final fusion."),
                GetFormattedOptionString("-chain_index2 <char>",        "Assembly chain index to use at the C-terminus of the final fusion."),
            });
        }
    }
    
    public class CxcxFusionFlags : FusionFlags
    {
        static Dictionary<Tuple<string, string>, int> _architectureUnitToMultiplicity = new Dictionary<Tuple<string, string>, int>();
        public const string BaseFlag = "-cx_cx";

        static CxcxFusionFlags()
        {
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("I", "C5"), 5);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("I", "C3"), 3);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("I", "C2"), 2);

            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("O", "C4"), 4);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("O", "C3"), 3);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("O", "C2"), 2);

            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("T", "C3"), 3);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("T", "C2"), 2);

            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D2", "C2"), 2);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D2", "C2X"), 2);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D2", "C2Y"), 2);

            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D3", "C3"), 3);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D3", "C2X"), 2);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D3", "C2Y"), 2);

            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D4", "C4"), 4);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D4", "C2X"), 2);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D4", "C2Y"), 2);

            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D5", "C5"), 5);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D5", "C2X"), 2);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("D5", "C2Y"), 2);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("P42", "C4"), 4);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("P42", "C2"), 2);

            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("P6", "C6"), 6);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("P6", "C3"), 3);
            _architectureUnitToMultiplicity.Add(new Tuple<string, string>("P6", "C2"), 2);

            /*  angle_source_info
                Proc Natl Acad Sci U S A. 2001 Feb 27; 98(5): 2217–2221.
                Published online 2001 Feb 20. doi:  10.1073/pnas.041614998
            */
        }

        protected string InstanceBaseFlag { get; set; } = "-cx_cx";
        public string Architecture;
        public string UnitId1 = null;
        public string UnitId2 = null;
        public string OligomerRegex1 = "*";
        public string OligomerRegex2 = "*";
        public int Oligomerization1 {  get { return _architectureUnitToMultiplicity[new Tuple<string, string>(Architecture, UnitId1)]; } }
        public int Oligomerization2 { get { return _architectureUnitToMultiplicity[new Tuple<string, string>(Architecture, UnitId2)]; } }

        public override bool ParseArgs(string[] args)
        {
            if (args.FirstOrDefault() != InstanceBaseFlag)
                return false;

            if (!base.ParseArgs(args))
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-arch")
                {
                    Architecture = args[i + 1];
                    if (!SymmetryBuilderFactory.GetKnownSymmetryNames().Contains(Architecture))
                    {
                        Console.WriteLine("Unknown '-arch' value - allowed values are:");
                        Console.Write(SymmetryBuilderFactory.GetKnownSymmetryNames().Aggregate("", (a, b) => a + "\n\t" + b));
                        Console.WriteLine();
                        return false;
                    }
                    i++;
                    continue;
                }

                if (args[i] == "-regex_oligomer1")
                {
                    OligomerRegex1 = args[i + 1];
                    i++;
                    continue;
                }

                if (args[i] == "-regex_oligomer2")
                {
                    OligomerRegex2 = args[i + 1];
                    i++;
                    continue;
                }
                if(args[i] == "-axis1")
                {
                    UnitId1 = args[i + 1];
                    i++;
                    continue;
                }
                if (args[i] == "-axis2")
                {
                    UnitId2 = args[i + 1];
                    i++;
                    continue;
                }
            }

            if(Architecture == null || !SymmetryBuilderFactory.GetKnownSymmetryNames().Contains(Architecture))
            {
                Console.WriteLine("Mandatory '-arch' flag missing or invalid - allowed values are:");
                Console.Write(SymmetryBuilderFactory.GetKnownSymmetryNames().Aggregate("", (a, b) => a + "\n\t" + b));
                Console.WriteLine();
                return false;
            }
            if(UnitId1 == null || !SymmetryBuilderFactory.GetSymmetryUnitIds(Architecture).Contains(UnitId1))
            {
                Console.WriteLine("Mandatory '-axis1' flag is missing or invalid - allowed values are:");
                Console.Write(SymmetryBuilderFactory.GetSymmetryUnitIds(Architecture).Aggregate("", (a, b) => a + "\n\t" + b));
                return false;
            }
            if (UnitId2 == null || !SymmetryBuilderFactory.GetSymmetryUnitIds(Architecture).Contains(UnitId2))
            {
                Console.WriteLine("Mandatory '-axis1' flag is missing or invalid - allowed values are:");
                Console.Write(SymmetryBuilderFactory.GetSymmetryUnitIds(Architecture).Aggregate("", (a, b) => a + "\n\t" + b));
                return false;
            }
            return true;
        }

        new public static IEnumerable<string> GetUsageOptions()
        {
            IEnumerable<string> options = FusionFlags.GetUsageOptions();

            List<string> additionalOptions = new List<string>();
            List<string> symmetries = SymmetryBuilderFactory.GetKnownSymmetryNames().Where(sym => !sym.StartsWith("C")).ToList();

            // architecture options
            additionalOptions.Add(GetFormattedOptionString("-arch <architecture>", "desired symmetry architecture, options are:"));
            additionalOptions.Add(GetFormattedOptionString("", symmetries.Aggregate((a, b) => a + ", " + b)));

            // axis1 options
            additionalOptions.Add(GetFormattedOptionString("-axis1 <axis>", "first axis name corresponding to selected architecture"));
            foreach (string symmetry in symmetries)
            {
                additionalOptions.Add(GetFormattedOptionString("", String.Format("\t{0} available in architecture {1}", GetAxesOptionsString(symmetry), symmetry)));
            }

            // axis2 options
            additionalOptions.Add(GetFormattedOptionString("-axis2 <axis>", "first axis name corresponding to selected architecture"));
            foreach (string symmetry in symmetries)
            {
                additionalOptions.Add(GetFormattedOptionString("", String.Format("\t{0} available in architecture {1}", GetAxesOptionsString(symmetry), symmetry)));
            }

            // oligomer selection
            additionalOptions.Add(GetFormattedOptionString("-regex_oligomer1 <regex>", "oligomer1 pdb search pattern"));
            additionalOptions.Add(GetFormattedOptionString("-regex_oligomer2 <regex>", "oligomer2 pdb search pattern"));

            return options.Concat(additionalOptions);
        }

        static string GetAxesOptionsString(string symmetry)
        {
            string[] units = SymmetryBuilderFactory.GetSymmetryUnitIds(symmetry);
            if (units.Length == 1)
                return units[0];
            return units.Aggregate((a,b) => a + ", " + b);
        }
    }

    public class CxrcxFusionFlags : CxcxFusionFlags
    {
        public string RepeatRegex = "*";
        new public const string BaseFlag = "-cx_r_cx";

        public CxrcxFusionFlags() { InstanceBaseFlag = "-cx_r_cx"; }

        public override bool ParseArgs(string[] args)
        {
            if (!base.ParseArgs(args))
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-regex_repeat")
                {
                    RepeatRegex = args[i + 1];
                    i++;
                    continue;
                }
            }

            return true;
        }

        new public static IEnumerable<string> GetUsageOptions()
        {
            return CxcxFusionFlags.GetUsageOptions().Concat(new String[]
            {
                GetFormattedOptionString("-regex_repeat <regex>", "repeat pdb search pattern"),
            });
        }
    }
}
