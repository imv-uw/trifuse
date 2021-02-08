//using NamespaceDynameomics;
using Structure;
using NamespaceUtilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Misc
{
    public struct RotamerRow
    {
        public int PhiMin;
        public int PsiMin;
        public int PhiMax;
        public int PsiMax;
        public int Chi1;
        public int Chi2;
        public int Chi3;
        public int Chi4;
        public float ChiAngle1;
        public float ChiAngle2;
        public float ChiAngle3;
        public float ChiAngle4;
        public float ChiDev1;
        public float ChiDev2;
        public float ChiDev3;
        public float ChiDev4;

        public double Percent;
        public long RotamerId;
    }

    /*
     * Chi count
     * Dihedral atom name list
     * Dihedral angle range to index lookup
     * 
     * 
     * // Range, Name
     */

    class Binner
    {
        static string GetNameTetramer(int? bin)
        {
            switch (bin)
            {
                case 1: return "g+";
                case 2: return "t";
                case 3: return "g-";
            }
            throw new ArgumentException();
        }

        static int[][] GetBoundsTetramer(int? bin)
        {
            switch (bin)
            {
                case 1: return new int[][] { new int[] { 0, 120 } };
                case 2: return new int[][] { new int[] { 120, 240 } };
                case 3: return new int[][] { new int[] { 240, 360 } };
            }
            throw new ArgumentException();
        }

        static string GetNameAsn2Gln3(int? bin)
        {
            switch (bin)
            {
                case 4: return "Og+";
                case 5: return "Ng-";
                case 6: return "Ot";
                case 1: return "Ng+";
                case 2: return "Og-";
                case 3: return "Nt";
            }
            throw new ArgumentException();
        }

        static int[][] GetBoundsAsn2Gln3(int? bin)
        {
            switch (bin)
            {
                case 4: return new int[][] { new int[] { 30, 90 } };
                case 5: return new int[][] { new int[] { 90, 150 } };
                case 6: return new int[][] { new int[] { 150, 210 } };
                case 1: return new int[][] { new int[] { 210, 270 } };
                case 2: return new int[][] { new int[] { 270, 330 } };
                case 3: return new int[][] { new int[] { 330, 30 } };
            }
            throw new ArgumentException();
        }

        static string GetNameAsp2Glu3(int? bin)
        {
            switch (bin)
            {
                case 1: return "g+";
                case 2: return "t";
                case 3: return "g-";
            }
            throw new ArgumentException();
        }

        static int[][] GetBoundsAsp2Glu3(int? bin)
        {
            switch (bin)
            {
                case 1: return new int[][] { new int[] { 30, 90 }, new int[] { 210, 270 } };
                case 2: return new int[][] { new int[] { 150, 210 }, new int[] { 330, 30 } };
                case 3: return new int[][] { new int[] { 90, 150 }, new int[] { 270, 330 } };
            }
            throw new ArgumentException();
        }

        static string GetNameHis2(int? bin)
        {
            switch (bin)
            {
                case 1: return "Ng+";
                case 2: return "Cg-";
                case 3: return "Nt";
                case 4: return "Cg+";
                case 5: return "Ng-";
                case 6: return "Ct";
            }
            throw new ArgumentException();
        }

        static int[][] GetBoundsHis2(int? bin)
        {
            switch (bin)
            {
                case 1: return new int[][] { new int[] { 30, 90 } };
                case 2: return new int[][] { new int[] { 90, 150 } };
                case 3: return new int[][] { new int[] { 150, 210 } };
                case 4: return new int[][] { new int[] { 210, 270 } };
                case 5: return new int[][] { new int[] { 270, 330 } };
                case 6: return new int[][] { new int[] { 330, 30 } };
            }
            throw new ArgumentException();
        }

        static string GetNamePhe2Tyr2(int? bin)
        {
            switch (bin)
            {
                case 1: return "g";
                case 2: return "t";
            }
            throw new ArgumentException();
        }

        static int[][] GetBoundsPhe2Tyr2(int? bin)
        {
            switch (bin)
            {
                case 1: return new int[][] { new int[] { 45, 135 }, new int[] { 225, 315 } };
                case 2: return new int[][] { new int[] { 135, 225}, new int[] { 315, 45 } };
            }
            throw new ArgumentException();
        }

        static string GetNamePro(int? bin)
        {
            switch (bin)
            {
                case 1: return "g+";
                case 2: return "g-";
            }
            throw new ArgumentException();
        }

        static int[][] GetBoundsPro(int? bin)
        {
            switch (bin)
            {
                case 1: return new int[][] { new int[] { 0, 180 } };
                case 2: return new int[][] { new int[] { 180, 360 } };
            }
            throw new ArgumentException();
        }

        static string GetNameTrp2(int? bin)
        {
            switch (bin)
            {
                case 1: return "g+";
                case 2: return "t";
                case 3: return "g-";
            }
            throw new ArgumentException();
        }

        static int[][] GetBoundsTrp2(int? bin)
        {
            switch (bin)
            {
                case 1: return new int[][] { new int[] { 180, 300 } };
                case 2: return new int[][] { new int[] { 300, 60 } };
                case 3: return new int[][] { new int[] { 60, 180 } };
            }
            throw new ArgumentException();
        }

        public static string[] GetNamesOfBins(string residue, int? chi1bin, int? chi2bin, int? chi3bin, int? chi4bin)
        {
            switch(residue)
            {
                case "ALA": break;
                case "ARG": return new String[] { GetNameTetramer(chi1bin), GetNameTetramer(chi2bin), GetNameTetramer(chi3bin), GetNameTetramer(chi4bin) };
                case "ASN": return new String[] { GetNameTetramer(chi1bin), GetNameAsn2Gln3(chi2bin) };
                case "ASP": return new String[] { GetNameTetramer(chi1bin), GetNameAsp2Glu3(chi2bin) };
                case "CYS": 
                case "CYH": return new String[] { GetNameTetramer(chi1bin) };
                case "HID":
                case "HIE":
                case "HIS": return new String[] { GetNameTetramer(chi1bin), GetNameHis2(chi2bin) };
                case "GLN": return new String[] { GetNameTetramer(chi1bin), GetNameTetramer(chi2bin), GetNameAsn2Gln3(chi3bin) };
                case "GLU": return new String[] { GetNameTetramer(chi1bin), GetNameTetramer(chi2bin), GetNameAsp2Glu3(chi3bin) };
                case "ILE":
                case "LEU": return new String[] { GetNameTetramer(chi1bin), GetNameTetramer(chi2bin) };
                case "LYS": return new String[] { GetNameTetramer(chi1bin), GetNameTetramer(chi2bin), GetNameTetramer(chi3bin), GetNameTetramer(chi4bin) };
                case "MET": return new String[] { GetNameTetramer(chi1bin), GetNameTetramer(chi2bin), GetNameTetramer(chi3bin) };
                case "PHE": return new String[] { GetNameTetramer(chi1bin), GetNamePhe2Tyr2(chi2bin) };
                case "PRO": return new String[] { GetNamePro(chi1bin) };
                case "SER":
                case "THR": return new String[] { GetNameTetramer(chi1bin) };
                case "TRP": return new String[] { GetNameTetramer(chi1bin), GetNameTetramer(chi2bin) };
                case "TYR": return new String[] { GetNameTetramer(chi1bin), GetNamePhe2Tyr2(chi2bin) };
                case "VAL": return new String[] { GetNameTetramer(chi1bin) };
            }
            throw new ArgumentException();
        }

        string GetNameOfBin(string residue, int chiAngleIndex, int chiConformationBin)
        {                
            int chi1 = 1;
            int chi2 = 1;
            int chi3 = 1;
            int chi4 = 1;
            switch(chiAngleIndex)
            {
                case 1: chi1 = chiConformationBin; break;
                case 2: chi2 = chiConformationBin; break;
                case 3: chi3 = chiConformationBin; break;
                case 4: chi4 = chiConformationBin; break;
                default: throw new ArgumentException("Chi angle index must be 1 to 4");
            }
            return GetNamesOfBins(residue, chi1, chi2, chi3, chi4)[chiAngleIndex - 1];
        }

        public static int[][][] GetBoundsOfBins(string residue, int? chi1bin, int? chi2bin, int? chi3bin, int? chi4bin)
        {
            switch (residue)
            {
                case "ALA": break;
                case "ARG": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsTetramer(chi2bin), GetBoundsTetramer(chi3bin), GetBoundsTetramer(chi4bin) };
                case "ASN": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsAsn2Gln3(chi2bin) };
                case "ASP": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsAsp2Glu3(chi2bin) };
                case "CYS":                  
                case "CYH": return new int[][][] { GetBoundsTetramer(chi1bin) };
                case "HID":                   
                case "HIE":                   
                case "HIS": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsHis2(chi2bin) };
                case "GLN": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsTetramer(chi2bin), GetBoundsAsn2Gln3(chi3bin) };
                case "GLU": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsTetramer(chi2bin), GetBoundsAsp2Glu3(chi3bin) };
                case "ILE":                  
                case "LEU": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsTetramer(chi2bin) };
                case "LYS": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsTetramer(chi2bin), GetBoundsTetramer(chi3bin), GetBoundsTetramer(chi4bin) };
                case "MET": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsTetramer(chi2bin), GetBoundsTetramer(chi3bin) };
                case "PHE": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsPhe2Tyr2(chi2bin) };
                case "PRO": return new int[][][] { GetBoundsPro(chi1bin) };
                case "SER":                  
                case "THR": return new int[][][] { GetBoundsTetramer(chi1bin) };
                case "TRP": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsTetramer(chi2bin) };
                case "TYR": return new int[][][] { GetBoundsTetramer(chi1bin), GetBoundsPhe2Tyr2(chi2bin) };
                case "VAL": return new int[][][] { GetBoundsTetramer(chi1bin) };
            }
            throw new ArgumentException();
        }

        int[][] GetBoundsOfBin(string residue, int chiAngleIndex, int chiConformationBin)
        {
            int chi1 = 1;
            int chi2 = 1;
            int chi3 = 1;
            int chi4 = 1;
            switch (chiAngleIndex)
            {
                case 1: chi1 = chiConformationBin; break;
                case 2: chi2 = chiConformationBin; break;
                case 3: chi3 = chiConformationBin; break;
                case 4: chi4 = chiConformationBin; break;
                default: throw new ArgumentException("Chi angle index must be 1 to 4");
            }
            return GetBoundsOfBins(residue, chi1, chi2, chi3, chi4)[chiAngleIndex - 1];
        }

        public static int[] GetBinCount(string residue)
        {
            switch (residue)
            {
                case "ALA": break;
                case "ARG": return new int[] { 3, 3, 3, 3 };
                case "ASN": return new int[] { 3, 6 };
                case "ASP": return new int[] { 3, 3 };
                case "CYS":
                case "CYH": return new int[] { 3 };
                case "HID":
                case "HIE":
                case "HIS": return new int[] { 3, 6 };
                case "GLN": return new int[] { 3, 3, 6 };
                case "GLU": return new int[] { 3, 3, 3 };
                case "ILE":                       
                case "LEU": return new int[] { 3, 3 };
                case "LYS": return new int[] { 3, 3, 3, 3 };
                case "MET": return new int[] { 3, 3, 3 };
                case "PHE": return new int[] { 3, 2 };
                case "PRO": return new int[] { 2 };
                case "SER":
                case "THR": return new int[] { 3 };
                case "TRP": return new int[] { 3, 3 };
                case "TYR": return new int[] { 3, 2 };
                case "VAL": return new int[] { 3 };
            }
            throw new ArgumentException();
        }
    }



    class Website
    {
        static readonly string[] chiNames2 = new string[] { "g", "t" };
        static readonly string[] chiNames3 = new string[] { "g+", "t", "g-" };
        static readonly string[] chiNames6 = new string[] { "Og+", "Ng-", "Ot", "Ng+", "Og-", "Nt" };
        static readonly string[] colors = new string[] { "#FFFFFF", "#CCE6FF", "#99CCFF" };
        

        public static void GenerateRotamerCode()
        {
            foreach(String residue in GetRotamerResidues())
            {
                // SQL Rotamer library is missing ASH, but contains ASP. Ditto GLH vs GLU.
                if (residue == "ASH" || residue == "GLH")
                    continue;

                // These are also unexpected residues
                if (residue == "HIN" || residue == "HIP" || residue == "HIS")
                    continue;

                List<string> output = new List<string>();
                List<RotamerRow> rotamers = GetRotamers(residue);
                int colorRepeatCount = -1;
                int chi1Max = rotamers.Max(rotamer => rotamer.Chi1);
                int chi2Max = rotamers.Max(rotamer => rotamer.Chi2);
                int chi3Max = rotamers.Max(rotamer => rotamer.Chi3);
                int chi4Max = rotamers.Max(rotamer => rotamer.Chi4);
                int chiCount = 0;
                if (chi1Max > 0)
                    chiCount = 1;
                if (chi2Max > 0)
                    chiCount = 2;
                if (chi3Max > 0)
                    chiCount = 3;
                if (chi4Max > 0)
                    chiCount = 4;

                switch(chiCount)
                {
                    case 1:
                        colorRepeatCount = 1;
                        break;
                    case 2:
                        colorRepeatCount = chi2Max;
                        break;
                    case 3:
                        colorRepeatCount = chi3Max;
                        break;
                    case 4:
                        colorRepeatCount = chi4Max;
                        break;
                }

                // Disregard ALA, etc.
                if (chiCount == 0)
                    continue;

                int i = 1;
                int colorIndex = 0;
                foreach(RotamerRow rotamer in rotamers)
                {
                    string row = null;
                    string[] chi1List = chi1Max == 6 ? chiNames6 : (chi1Max == 3)? chiNames3 : chiNames2;
                    string[] chi2List = chi2Max == 6 ? chiNames6 : (chi2Max == 3)? chiNames3 : chiNames2;
                    string[] chi3List = chi3Max == 6 ? chiNames6 : (chi3Max == 3)? chiNames3 : chiNames2;
                    string[] chi4List = chi4Max == 6 ? chiNames6 : (chi4Max == 3)? chiNames3 : chiNames2;

                    
                    //RotamerInstanceRow instance = GetRotamerInstance(rotamer.RotamerId);
                    //SaveRotamerInstancePdb(residue + "_2014_" + i.ToString() + ".pdb.txt", instance);

                    switch (chiCount)
                    {
                        case 1:
                            row = String.Format("<TR BGCOLOR='{4}'> <TD> {0} <TD ALIGN='RIGHT'> {1:F4} <TD ALIGN='RIGHT'> {2:F1} <TD ALIGN='CENTER'> <A HREF='{3}'> PDB </A>",
                                chi1List[rotamer.Chi1 - 1],
                                rotamer.Percent,
                                rotamer.ChiAngle1,
                                "../pdbs/" + residue + "_2014_" + i.ToString() + ".pdb.txt",
                                colors[colorIndex]

                                );
                            break;
                        case 2:
                            row = String.Format("<TR BGCOLOR='{6}'> <TD> {0} <TD> {1} <TD ALIGN='RIGHT'> {2:F4} <TD ALIGN='RIGHT'> {3:F1} <TD ALIGN='RIGHT'> {4:F1} <TD ALIGN='CENTER'> <A HREF='{5}'> PDB </A>",
                                chi1List[rotamer.Chi1 - 1],
                                chi2List[rotamer.Chi2 - 1],
                                rotamer.Percent,
                                rotamer.ChiAngle1,
                                rotamer.ChiAngle2,
                                "../pdbs/" + residue + "_2014_" + i.ToString() + ".pdb.txt",
                                colors[colorIndex]
                                );
                            break;
                        case 3:
                            row = String.Format("<TR BGCOLOR='{8}'> <TD> {0} <TD> {1} <TD> {2} <TD ALIGN='RIGHT'> {3:F4} <TD ALIGN='RIGHT'> {4:F1} <TD ALIGN='RIGHT'> {5:F1} <TD ALIGN='RIGHT'> {6:F1} <TD ALIGN='CENTER'> <A HREF='{7}'> PDB </A>",
                                chi1List[rotamer.Chi1 - 1],
                                chi2List[rotamer.Chi2 - 1],
                                chi3List[rotamer.Chi3 - 1],
                                rotamer.Percent,
                                rotamer.ChiAngle1,
                                rotamer.ChiAngle2,
                                rotamer.ChiAngle3,
                                "../pdbs/" + residue + "_2014_" + i.ToString() + ".pdb.txt",
                                colors[colorIndex]
                                );
                            break;
                        case 4:
                            row = String.Format("<TR BGCOLOR='{10}'> <TD> {0} <TD> {1} <TD> {2} <TD> {3} <TD ALIGN='RIGHT'> {4:F4} <TD ALIGN='RIGHT'> {5:F1} <TD ALIGN='RIGHT'> {6:F1} <TD ALIGN='RIGHT'> {7:F1} <TD ALIGN='RIGHT'> {8:F1} <TD ALIGN='CENTER'> <A HREF='{9}'> PDB </A>",
                                chi1List[rotamer.Chi1 - 1],
                                chi2List[rotamer.Chi2 - 1],
                                chi3List[rotamer.Chi3 - 1],
                                chi4List[rotamer.Chi4 - 1],
                                rotamer.Percent,
                                rotamer.ChiAngle1,
                                rotamer.ChiAngle2,
                                rotamer.ChiAngle3,
                                rotamer.ChiAngle4,
                                "../pdbs/" + residue + "_2014_" + i.ToString() + ".pdb.txt",
                                colors[colorIndex]
                                );
                            break;
                        default: throw new Exception("Bug encountered.");
                    }
                    output.Add(row);
                    if(i % colorRepeatCount == 0)
                        colorIndex++;
                    if (colorIndex == 3)
                        colorIndex = 0;
                    i++;
                }

                File.WriteAllLines(residue + ".aspx", output);
            }
        }

        public static void GenerateBBDependentRotamerTextLibrary()
        {
            Console.WriteLine("Residue, Phi Min, Phi Max, Psi Min, Psi Max, Chi1 Name, Chi2 Name, Chi3 Name, Chi4  Name, Chi1 (SD), Chi2 (SD), Chi3 (SD), Chi4 (SD), Percent (within Phi/Psi bin)");
            foreach (String residue in GetRotamerResidues())
            {
                if (residue == "ALA" || residue == "GLY")
                    continue;

                // SQL Rotamer library is missing ASH, but contains ASP. Ditto GLH vs GLU.
                if (residue == "ASH" || residue == "GLH")
                    continue;

                // These are also unexpected residues
                if (residue == "HIN" || residue == "HIP" || residue == "HIS")
                    continue;

                List<RotamerRow> rotamers = GetRotamersBBDep(residue);

                foreach(RotamerRow row in rotamers)
                {
                    int? chi1 = (row.Chi1 == 0)? null : (int?) row.Chi1;
                    int? chi2 = (row.Chi2 == 0)? null : (int?) row.Chi2;
                    int? chi3 = (row.Chi3 == 0)? null : (int?) row.Chi3;
                    int? chi4 = (row.Chi4 == 0)? null : (int?) row.Chi4;
                    
                    string chi1Value = (chi1 == null) || row.Percent == 0? "-" : row.ChiAngle1.ToString("F2");
                    string chi2Value = (chi2 == null) || row.Percent == 0 ? "-" : row.ChiAngle2.ToString("F2");
                    string chi3Value = (chi3 == null) || row.Percent == 0 ? "-" : row.ChiAngle3.ToString("F2");
                    string chi4Value = (chi4 == null) || row.Percent == 0 ? "-" : row.ChiAngle4.ToString("F2");
                    
                    string chi1Dev = (chi1 == null || row.Percent == 0) ? "-" : (row.ChiDev1 < 0.00001)? "0" :  row.ChiDev1.ToString("F2");
                    string chi2Dev = (chi2 == null || row.Percent == 0) ? "-" : (row.ChiDev2 < 0.00001)? "0" :  row.ChiDev2.ToString("F2");
                    string chi3Dev = (chi3 == null || row.Percent == 0) ? "-" : (row.ChiDev3 < 0.00001)? "0" :  row.ChiDev3.ToString("F2");
                    string chi4Dev = (chi4 == null || row.Percent == 0) ? "-" : (row.ChiDev4 < 0.00001)? "0" :  row.ChiDev4.ToString("F2");


                    string[] names = Binner.GetNamesOfBins(residue, row.Chi1, row.Chi2, row.Chi3, row.Chi4);
                    string[] printNames = new string[] { "-", "-", "-", "-" };
                    for(int i = 0; i < printNames.Length; i++)
                    {
                        if (i < names.Length)
                            printNames[i] = names[i];
                    }

                    Console.WriteLine("{0,3}, {1,4}, {2,4}, {3,4}, {4,4}, {5,-3}, {6,-3}, {7,-3}, {8,-3}, {9,6} ({14,6}), {10,6} ({15,6}), {11,6} ({16,6}), {12,6} ({17,6}), {13,6:F2}",
                        residue, row.PhiMin, row.PhiMax, row.PsiMin, row.PsiMax,
                        printNames[0], printNames[1], printNames[2], printNames[3],
                        chi1Value, chi2Value, chi3Value, chi4Value, row.Percent,
                        chi1Dev, chi2Dev, chi3Dev, chi4Dev
                        );
                }
            }
        }

        public static void GenerateBBIndependentRotamerTextLibrary()
        {
            Console.WriteLine("Residue, Chi1 Name, Chi2 Name, Chi3 Name, Chi4  Name, Chi1 (SD), Chi2 (SD), Chi3 (SD), Chi4 (SD), Percent (within Phi/Psi bin)");
            foreach (String residue in GetRotamerResidues())
            {
                if (residue == "ALA" || residue == "GLY")
                    continue;

                // SQL Rotamer library is missing ASH, but contains ASP. Ditto GLH vs GLU.
                if (residue == "ASH" || residue == "GLH")
                    continue;

                // These are also unexpected residues
                if (residue == "HIN" || residue == "HIP" || residue == "HIS")
                    continue;

                List<RotamerRow> rotamers = GetRotamers(residue);

                foreach (RotamerRow row in rotamers)
                {
                    int? chi1 = (row.Chi1 == 0) ? null : (int?)row.Chi1;
                    int? chi2 = (row.Chi2 == 0) ? null : (int?)row.Chi2;
                    int? chi3 = (row.Chi3 == 0) ? null : (int?)row.Chi3;
                    int? chi4 = (row.Chi4 == 0) ? null : (int?)row.Chi4;

                    string chi1Value = (chi1 == null) || row.Percent == 0 ? "-" : row.ChiAngle1.ToString("F2");
                    string chi2Value = (chi2 == null) || row.Percent == 0 ? "-" : row.ChiAngle2.ToString("F2");
                    string chi3Value = (chi3 == null) || row.Percent == 0 ? "-" : row.ChiAngle3.ToString("F2");
                    string chi4Value = (chi4 == null) || row.Percent == 0 ? "-" : row.ChiAngle4.ToString("F2");
                    string chi1Dev = (chi1 == null || row.Percent == 0) ? "-" : (row.ChiDev1 < 0.00001) ? "0" : row.ChiDev1.ToString("F2");
                    string chi2Dev = (chi2 == null || row.Percent == 0) ? "-" : (row.ChiDev2 < 0.00001) ? "0" : row.ChiDev2.ToString("F2");
                    string chi3Dev = (chi3 == null || row.Percent == 0) ? "-" : (row.ChiDev3 < 0.00001) ? "0" : row.ChiDev3.ToString("F2");
                    string chi4Dev = (chi4 == null || row.Percent == 0) ? "-" : (row.ChiDev4 < 0.00001) ? "0" : row.ChiDev4.ToString("F2");

                    string[] names = Binner.GetNamesOfBins(residue, row.Chi1, row.Chi2, row.Chi3, row.Chi4);
                    string[] printNames = new string[] { "-", "-", "-", "-" };
                    for (int i = 0; i < printNames.Length; i++)
                    {
                        if (i < names.Length)
                            printNames[i] = names[i];
                    }

                    Console.WriteLine("{0,3}, {1,-3}, {2,-3}, {3,-3}, {4,-3}, {5,6} ({10,6}), {6,6} ({11,6}), {7,6} ({12,6}), {8,6} ({13,6}), {9,6:F2}",
                        residue, 
                        printNames[0], printNames[1], printNames[2], printNames[3],
                        chi1Value, chi2Value, chi3Value, chi4Value, row.Percent,
                        chi1Dev, chi2Dev, chi3Dev, chi4Dev
                        );
                }
            }
        }

        //private static void SaveRotamerInstancePdb(string filename, RotamerInstanceRow instance)
        //{
        //    try
        //    {
        //        List<AtomSource> atoms = Dynameomics.GetAtomsForStructure(instance.struct_id);
        //        Dynameomics.GetAtomCoordinatesFromSimulation(atoms, instance.sim_id, instance.step, instance.struct_inst);

        //        List<string> lines = new List<string>();
        //        atoms.Sort((a1, a2) => a1.ChainIndex.CompareTo(a2.ChainIndex));
        //        foreach(AtomSource atom in atoms)
        //        {
        //            if(atom.ResidueIndex + 1 != instance.residue_id)
        //                continue;

        //            string line = String.Format(
        //                "{0,-6}{1,5:###} {2,-4}{3,1}{4,3} {5,1}{6,4:####}    {7,8:f3}{8,8:f3}{9,8:f3}                      {10,2}",
        //                "ATOM",             // 0
        //                atom.Index + 1,     // 1
        //                atom.Name,          // 2
        //                "",                 // 3
        //                atom.ResidueName,   // 4
        //                atom.ChainIndex,    // 5
        //                atom.ResidueIndex,  // 6
        //                atom.X,             // 7
        //                atom.Y,             // 8
        //                atom.Z,             // 9
        //                atom.ElementName    // 10
        //            );  
        //            lines.Add(line);    
        //        }
        //        File.WriteAllLines(filename, lines);
                
        //    }
        //    catch
        //    {
        //        Console.WriteLine("Failed to retreive and write " + filename);
        //    }
        //}

        static string GetRotamerIdString(long rotamerId)
        {
            try
            {
                string query = String.Format("SELECT * FROM [Rotamers].[dbo].[RotamerInstances] WHERE rotamer_id = {0}", rotamerId);
                using(SqlConnection connection = Sql.GetConnection("GRIZZLY3", "Rotamers"))
                {
                    using(SqlCommand command = new SqlCommand(query, connection))
                    {
                        using(SqlDataReader reader = command.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                string ret = String.Format(
                                "../pdbtools/PDB_v2.aspx?struct_id={0}&sim_id={1}&struct_inst={2}&time={3}&residue_start={4}&residue_end={5}",
                                (int)reader["struct_id"],
                                (int)reader["sim_id"],
                                (int)reader["struct_inst"],
                                ((int)reader["step"])/500,
                                (int)reader["residue_id"],
                                (int)reader["residue_id"]
                                );
                                return ret;
                            }
                        }
                    }
                }
            }
            catch 
            {
                Console.WriteLine("Failed to get rotamer " + rotamerId.ToString());
            }
            return String.Empty;
        }


        public class RotamerInstanceRow
        {
            public int struct_id;
            public int sim_id;
            public int struct_inst;
            public int step;
            public int residue_id;
        }

        static RotamerInstanceRow GetRotamerInstance(long rotamerId)
        {
            try
            {
                string query = String.Format("SELECT * FROM [Rotamers].[dbo].[RotamerInstances] WHERE rotamer_id = {0}", rotamerId);
                using (SqlConnection connection = Sql.GetConnection("GRIZZLY3", "Rotamers"))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                RotamerInstanceRow instance = new RotamerInstanceRow();
                                instance.struct_id = (int)reader["struct_id"];
                                instance.sim_id = (int)reader["sim_id"];
                                instance.struct_inst = (int)reader["struct_inst"];
                                instance.step = (int)reader["step"];
                                instance.residue_id = (int)reader["residue_id"];
                                return instance;
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Failed to get rotamer " + rotamerId.ToString());
            }
            return null;
        }

        static List<string> GetRotamerResidues()
        {
            try
            {
                string query = "SELECT distinct [residue] FROM [Rotamers].[dbo].[Rotamers] WHERE residue not like 'D%'";
                using(SqlConnection connection = Sql.GetConnection("GRIZZLY3", "Rotamers"))
                {
                    return Sql.ReturnQueryList<string>(connection, query);
                }
            }
            catch (SqlException sqle)
            {
                Console.WriteLine("Could not attach to server contact rotamer server GRIZZLY3: " + sqle.Message);
            }
            return new List<string>();
        }

        static List<RotamerRow> GetRotamers(string residue)
        {
            string query = String.Format(@"SELECT
                                           [residue]
                                          ,[rotamer_id]
                                          ,[rotlib_id]
                                          ,[chi_major]
                                          ,[chi1_bin]
                                          ,[chi2_bin]
                                          ,[chi3_bin]
                                          ,[chi4_bin]
                                          ,[chi1_angle]
                                          ,[chi2_angle]
                                          ,[chi3_angle]
                                          ,[chi4_angle]

                                          ,[chi1_sig]
                                          ,[chi2_sig]
                                          ,[chi3_sig]
                                          ,[chi4_sig]
                                          ,[probability] * 100 as pct
                                           FROM [Rotamers].[dbo].[Rotamers]
                                           WHERE rotlib_id = 4 AND residue = '{0}'
                                           ORDER BY chi_major
                                            ", residue);
            List<RotamerRow> rotamers = new List<RotamerRow>();

            Sql.ExecuteDelegate converter = (SqlDataReader reader, object param /* ignored */) =>
                    {
                        RotamerRow rotamer = new RotamerRow();
                        rotamer.Chi1 = (byte) reader["chi1_bin"];
                        rotamer.Chi2 = (byte) reader["chi2_bin"];
                        rotamer.Chi3 = (byte) reader["chi3_bin"];
                        rotamer.Chi4 = (byte) reader["chi4_bin"];
                        rotamer.ChiAngle1 = (float)reader["chi1_angle"];
                        rotamer.ChiAngle2 = (float)reader["chi2_angle"];
                        rotamer.ChiAngle3 = (float)reader["chi3_angle"];
                        rotamer.ChiAngle4 = (float)reader["chi4_angle"];
                        rotamer.ChiDev1 = (float)reader["chi1_sig"];
                        rotamer.ChiDev2 = (float)reader["chi2_sig"];
                        rotamer.ChiDev3 = (float)reader["chi3_sig"];
                        rotamer.ChiDev4 = (float)reader["chi4_sig"];
                        rotamer.Percent = (double)reader["pct"];
                        rotamer.RotamerId = (int)reader["rotamer_id"];
                        rotamers.Add(rotamer);
                    };
            Sql.ExecuteDelegateOnQuery(Sql.GetConnection("GRIZZLY3", "Rotamers"), query, converter, null);
            return rotamers;
        }

        static List<RotamerRow> GetRotamersBBDep(string residue)
        {
            string query = String.Format(@"SELECT
                                           [residue]
                                          ,[rotamer_id]
                                          ,[phi_bin]
                                          ,[psi_bin]
                                          ,[rotlib_id]
                                          ,[chi_major]
                                          ,[chi1_bin]
                                          ,[chi2_bin]
                                          ,[chi3_bin]
                                          ,[chi4_bin]
                                          ,[chi1_angle]
                                          ,[chi2_angle]
                                          ,[chi3_angle]
                                          ,[chi4_angle]
                                          ,[chi1_sig]
                                          ,[chi2_sig]
                                          ,[chi3_sig]
                                          ,[chi4_sig]
                                          ,[probability] * 100 as pct
                                           FROM [Rotamers].[dbo].[Rotamers]
                                           WHERE rotlib_id = 1 AND residue = '{0}'
                                           ORDER BY phi_bin, psi_bin, chi_major
                                            ", residue);
            List<RotamerRow> rotamers = new List<RotamerRow>();

            Sql.ExecuteDelegate converter = (SqlDataReader reader, object param /* ignored */) =>
            {
                RotamerRow rotamer = new RotamerRow();
                rotamer.PhiMin = (int)reader["phi_bin"];
                rotamer.PsiMin = (int)reader["psi_bin"];
                rotamer.PhiMax = rotamer.PhiMin + 10;
                rotamer.PsiMax = rotamer.PsiMin + 10;
                rotamer.Chi1 = (byte)reader["chi1_bin"];
                rotamer.Chi2 = (byte)reader["chi2_bin"];
                rotamer.Chi3 = (byte)reader["chi3_bin"];
                rotamer.Chi4 = (byte)reader["chi4_bin"];
                rotamer.ChiAngle1 = (float)reader["chi1_angle"];
                rotamer.ChiAngle2 = (float)reader["chi2_angle"];
                rotamer.ChiAngle3 = (float)reader["chi3_angle"];
                rotamer.ChiAngle4 = (float)reader["chi4_angle"];
                rotamer.ChiDev1 = (float)reader["chi1_sig"];
                rotamer.ChiDev2 = (float)reader["chi2_sig"];
                rotamer.ChiDev3 = (float)reader["chi3_sig"];
                rotamer.ChiDev4 = (float)reader["chi4_sig"];
                rotamer.Percent = (double)reader["pct"];
                rotamer.RotamerId = (int)reader["rotamer_id"];
                rotamers.Add(rotamer);
            };
            Sql.ExecuteDelegateOnQuery(Sql.GetConnection("GRIZZLY3", "Rotamers"), query, converter, null);
            return rotamers;
        }
    }
}
