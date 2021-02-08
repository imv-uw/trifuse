using Core;
using Core.PDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Tools;
using Fuse;
using Core.Symmetry;
using Core.Interfaces;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] skip = new string[]
            {
                // questionable C2 scaffolds, IMO - don't use them
                "ZC",
                "4pwwfrxC2",
                "bex2C2G2",
                "Di13",

                // questionable C3 scaffolds IMO or lacking free termini - don't use them
                "2L6HC3",
                "C3-1BH",
                "EXTN8",
                "1na0C3int2-xtal",
                "C3V"
            };

            foreach (string symmetry in new string[] { "T" })
            {
                string basedir = Directory.GetCurrentDirectory();
                foreach (string fileNameOligomer1 in Directory.EnumerateFiles(Path.Combine(basedir, @"Database\Scaffolds\Denovo\C3")))
                {
                    if (skip.Any(str => fileNameOligomer1.Contains(str)))
                        continue;

                    IStructure oligomer1 = PdbQuick.AssemblyFromFileOrCode(fileNameOligomer1);
                    foreach (string fileNameOligomer2 in Directory.EnumerateFiles(Path.Combine(basedir, @"Database\Scaffolds\Denovo\C2")))
                    {
                        if (skip.Any(str => fileNameOligomer2.Contains(str)))
                            continue;

                        IStructure oligomer2 = PdbQuick.AssemblyFromFileOrCode(fileNameOligomer2);
                        foreach (string fileNameStrut in Directory.EnumerateFiles(Path.Combine(basedir, @"Database\Scaffolds\Denovo\repeats\saxs_and_crystal")))
                        {
                            IStructure strut = PdbQuick.AssemblyFromFileOrCode(fileNameStrut);

                            string basenameC3 = (new FileInfo(fileNameOligomer1)).Name;
                            string basenameC2 = (new FileInfo(fileNameOligomer2)).Name;
                            string basenameStrut = (new FileInfo(fileNameStrut)).Name;
                            string outputBase = String.Format("{0}32_{1}_{2}_{3}", symmetry, basenameC3, basenameStrut, basenameC2).Replace(".pdb", "");

                            Console.WriteLine("Trying {0} + {1} + {2}", basenameC3, basenameC2, basenameStrut);

                            SymmetryBuilder builder = SymmetryBuilderFactory.CreateFromSymmetryName(symmetry);
                            List<Model> models = SymmetricFusionGenerator.CnSnCn(oligomer1, oligomer2, new IStructure[] { strut },
                                new int[] { 0 }, new int[] { 0 }, new int[][] { new int[] { 0 } }, builder, "C3X", "C2", 5, 5, 5, 20);

                            for (int i = 0; i < models.Count; i++)
                            {
                                Model model = models[i];
                                string outputName = outputBase + "_" + i;
                                PdbQuick.Save(outputName + ".pdb", model.Structure);
                                foreach (KeyValuePair<string, Selection> kvp in model.Selections)
                                {
                                    string selectionName = kvp.Key;
                                    Selection selection = kvp.Value;

                                    if (model.AsymmetricUnit != null)
                                    {
                                        Resfile resfile = new Resfile();
                                        resfile.SetDesignOperation(model.AsymmetricUnit, selection, ResfileDesignOperation.NOTAA, new char[] { 'W', 'M', 'P', 'C' });

                                        if (resfile.DesignOperationsCount > 0)
                                        {
                                            string[] lines = resfile.GetFileText();
                                            string resfileName = String.Format("{0}_{1}_asu.resfile", outputName, selectionName);
                                            File.WriteAllLines(resfileName, lines);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
