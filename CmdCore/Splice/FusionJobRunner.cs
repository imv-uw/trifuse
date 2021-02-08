using Microsoft.Xna.Framework;
using CmdCore.Splice;
using NamespaceUtilities;
using Fuse;
using Core;
using Core.PDB;
using Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Tools;
using Core.Interfaces;
using Core.Symmetry;

namespace CmdCore
{
    public class FusionJobRunner
    {
        static object lockThreadCount = new object();
        static object lockFileIO = new object();

#if DEBUG
        static int _threadCount = 1;
        //static int _threadCount = Environment.ProcessorCount;
#else
        static int _threadCount = Environment.ProcessorCount;
#endif
        static Semaphore _threadCountSemaphore = new Semaphore(_threadCount, _threadCount);

        public static bool Run(string[] args)
        {
            switch (args.FirstOrDefault())
            {
                case CxrcxFusionFlags.BaseFlag:
                    {
                        CxrcxFusionFlags options = new CxrcxFusionFlags();
                        if (options.ParseArgs(args))
                            StartJobsAngleCXRCX(options);
                        else
                            return false;
                    }
                    break;
                case CxcxFusionFlags.BaseFlag:
                    {
                        CxcxFusionFlags options = new CxcxFusionFlags();
                        if (options.ParseArgs(args))
                            StartJobsAngleCXCX(options);
                        else
                            return false;
                    }
                    break;
                case AsymmetricFusionFlags.BaseFlag:
                    {
                        AsymmetricFusionFlags options = new AsymmetricFusionFlags();
                        if (options.ParseArgs(args))
                            StartJobsAsymmetricPair(options);
                        else return false;
                    }
                    break;

                default: return false;
            }
            return true;
        }

        static void StartJobsAngleCXRCX(CxrcxFusionFlags options)
        {
            SymmetryBuilder symmetry = SymmetryBuilderFactory.CreateFromSymmetryName(options.Architecture);

            List<string> filesOligomer1 = GetPdbFiles(Directory.GetCurrentDirectory(), options.OligomerRegex1).ToList();
            List<string> filesOligomer2 = GetPdbFiles(Directory.GetCurrentDirectory(), options.OligomerRegex2).ToList();
            List<string> filesRepeat = GetPdbFiles(Directory.GetCurrentDirectory(), options.RepeatRegex).ToList();
            ThreadSafeJobCounter sharedCounter = new ThreadSafeJobCounter();
            sharedCounter.Total = filesOligomer1.Count * filesOligomer2.Count * filesRepeat.Count;

            Console.WriteLine("Using the following spacer repeat proteins:");
            filesRepeat.ForEach(file => Console.WriteLine(file));

            Console.WriteLine("\nUsing the following files for homo-oligomer 1:");
            filesOligomer1.ForEach(file => Console.WriteLine(file));

            Console.WriteLine("\nUsing the following files for homo-oligomer 2:");
            filesOligomer2.ForEach(file => Console.WriteLine(file));

            float angleDegrees = (float)VectorMath.GetAngleDegrees(symmetry.GetPrincipalCoordinateSystem(options.UnitId1).UnitX, Vector3.Zero, symmetry.GetPrincipalCoordinateSystem(options.UnitId2).UnitX);
            Console.WriteLine("Angle for {0}:{1}:{2} calculated to: {3:F4}", options.Architecture, options.UnitId1, options.UnitId2, angleDegrees);

            // Parse oligomer 1 and offset it to positive Z
            foreach (string fileOligomer1 in filesOligomer1)
            {
                string pdbCodeOligomer1 = PdbQuick.CodeFromFilePath(fileOligomer1);
                IChain peptide1 = PdbQuick.ChainFromFileOrCode(fileOligomer1);

                if (peptide1 == null) { 
                    Console.WriteLine("Pdb parsing failed for {0}", fileOligomer1);
                    continue;
                }

                peptide1.Translate(new Vector3(0, 0, 20 - Rmsd.GetBackboneCentroid(peptide1).Z));

                // Parse oligomer 2 and offset it to positive Z
                foreach (string fileOligomer2 in filesOligomer2)
                {
                    string pdbCodeOligomer2 = PdbQuick.CodeFromFilePath(fileOligomer2);
                    IChain peptide2 = PdbQuick.ChainFromFileOrCode(fileOligomer2);

                    if (peptide2 == null) { 
                        Console.WriteLine("Pdb parsing failed for {0}", fileOligomer2);
                        continue;
                    }

                    peptide2.Translate(new Vector3(0, 0, 20 - Rmsd.GetBackboneCentroid(peptide2).Z));

                    // Parse the repeat and offset it to positive Z
                    foreach (string fileRepeat in filesRepeat)
                    {
                        IChain repeat = PdbQuick.ChainFromFileOrCode(fileRepeat);
                        string pdbCodeRepeat = PdbQuick.CodeFromFilePath(fileRepeat);

                        if (repeat == null) { 
                            Console.WriteLine("Pdb parsing failed for {0}", repeat);
                            continue;
                        }

                        repeat.Translate(new Vector3(0, 0, 20 - Rmsd.GetBackboneCentroid(repeat).Z));

                        JobStartParamsCXRCX startParams = new JobStartParamsCXRCX();
                        // things taken directly from user options
                        startParams.OutputPrefix = options.Architecture + "-" + options.UnitId1 + "-" + options.UnitId2;            
                        startParams.UnitId1 = options.UnitId1;
                        startParams.UnitId2 = options.UnitId2;
                        startParams.ChainCount1 = options.Oligomerization1;
                        startParams.ChainCount2 = options.Oligomerization2;
                        startParams.TopX = options.TopX;
                        // everything else:
                        startParams.Symmetry = SymmetryBuilderFactory.CreateFromSymmetryName(options.Architecture); 
                        startParams.PdbCodeBundle1 = pdbCodeOligomer1;
                        startParams.PdbCodeBundle2 = pdbCodeOligomer2;
                        startParams.PdbCodeRepeat = pdbCodeRepeat;
                        startParams.Cx1 = peptide1;
                        startParams.Cx2 = peptide2;
                        startParams.Repeat = repeat;
                        startParams.AngleDegrees = angleDegrees;
                        startParams.Counter = sharedCounter; // Shared counter across queued jobs
                        Debug.Assert(startParams.Validate(), "JobStartParamsCXRCX validation failure");

                        // Wait for free threads
                        _threadCountSemaphore.WaitOne();
                        sharedCounter.IncrementQueued();

                        // Start the job
                        Thread thread = new Thread(new ParameterizedThreadStart(RunJobAngleCXRCX));
                        thread.Start(startParams);
                        Console.WriteLine("Queuing triplet [Bundle {0}]:[Repeat {1}]:[Bundle {2}], {3:F2} degrees, {4:F2} % ({5}/{6})", pdbCodeOligomer1, pdbCodeRepeat, pdbCodeOligomer2, angleDegrees, startParams.Counter.PercentQueued, startParams.Counter.Queued, startParams.Counter.Total);
                    }
                }
            }
        }      

        static void StartJobsAngleCXCX(CxcxFusionFlags options)
        {
            SymmetryBuilder symmetry = SymmetryBuilderFactory.CreateFromSymmetryName(options.Architecture);
            List<string> filesOligomer1 = GetPdbFiles(Directory.GetCurrentDirectory(), options.OligomerRegex1).ToList();
            List<string> filesOligomer2 = GetPdbFiles(Directory.GetCurrentDirectory(), options.OligomerRegex2).ToList();
            ThreadSafeJobCounter sharedCounter = new ThreadSafeJobCounter();
            sharedCounter.Total = filesOligomer1.Count * filesOligomer2.Count;

            Console.WriteLine("\nUsing the following files for homo-oligomer 1:");
            filesOligomer1.ForEach(file => Console.WriteLine(file));

            Console.WriteLine("\nUsing the following files for homo-oligomer 2:");
            filesOligomer2.ForEach(file => Console.WriteLine(file));

            float angleDegrees = (float)VectorMath.GetAngleDegrees(symmetry.GetPrincipalCoordinateSystem(options.UnitId1).UnitX, Vector3.Zero, symmetry.GetPrincipalCoordinateSystem(options.UnitId2).UnitX);
            Console.WriteLine("Angle for {0}:{1}:{2} calculated to: {3:F4}", options.Architecture, options.UnitId1, options.UnitId2, angleDegrees);

            // Parse oligomer 1 and offset it to positive Z
            foreach (string fileOligomer1 in filesOligomer1)
            {
                string pdbCode1 = PdbQuick.CodeFromFilePath(fileOligomer1);
                IChain peptide1 = PdbQuick.ChainFromFileOrCode(fileOligomer1);
                peptide1.Translate(new Vector3(0, 0, 20 - Rmsd.GetBackboneCentroid(peptide1).Z));

                // Parse oligomer 2 and offset it to positive Z
                foreach (string fileOligomer2 in filesOligomer2)
                {
                    string pdbCode2 = PdbQuick.CodeFromFilePath(fileOligomer2);
                    IChain peptide2 = PdbQuick.ChainFromFileOrCode(fileOligomer2);
                    peptide2.Translate(new Vector3(0, 0, 20 - Rmsd.GetBackboneCentroid(peptide2).Z));

                    if (peptide1 == null || peptide2 == null)
                    {
                        Console.WriteLine("Pdb parsing failed for one of [{0}, {1}]", fileOligomer1, fileOligomer2);
                        continue;
                    }

                    JobStartParamsCXCX startParams = new JobStartParamsCXCX();
                    // things taken directly from user options
                    startParams.OutputPrefix = options.Architecture + "-" + options.UnitId1 + "-" + options.UnitId2;
                    startParams.UnitId1 = options.UnitId1;
                    startParams.UnitId2 = options.UnitId2;
                    startParams.ChainCount1 = options.Oligomerization1;
                    startParams.ChainCount2 = options.Oligomerization2;
                    startParams.TopX = options.TopX;
                    // everything else:
                    startParams.Symmetry = SymmetryBuilderFactory.CreateFromSymmetryName(options.Architecture);
                    startParams.PdbCodeBundle1 = pdbCode1;
                    startParams.PdbCodeBundle2 = pdbCode2;
                    startParams.Cx1 = peptide1;
                    startParams.Cx2 = peptide2;
                    startParams.AngleDegrees = angleDegrees;
                    startParams.Counter = sharedCounter; // Shared counter across queued jobs
                    Debug.Assert(startParams.Validate(), "JobStartParamsCXRCX validation failure");
                    
                    _threadCountSemaphore.WaitOne();
                    sharedCounter.IncrementQueued();

                    Thread thread = new Thread(new ParameterizedThreadStart(RunJobAngleCXCX));
                    thread.Start(startParams);
                    Console.WriteLine("Queuing triplet [Bundle {0}]:[Bundle {1}], {2:F2} degrees, {3:F2} % ({4}/{5})", pdbCode1, pdbCode2, angleDegrees, startParams.Counter.PercentQueued, startParams.Counter.Queued, startParams.Counter.Total);
                }
            }
        }

        static void StartJobsAsymmetricPair(AsymmetricFusionFlags options)
        {
            List<string> files1 = GetPdbFiles("./", options.PeptideRegex1).ToList();
            List<string> files2 = GetPdbFiles("./", options.PeptideRegex2).ToList();
            ThreadSafeJobCounter sharedCounter = new ThreadSafeJobCounter();
            sharedCounter.Total = files1.Count * files2.Count;

            Console.WriteLine("\nUsing the following files for unit 1:");
            files1.ForEach(file => Console.WriteLine(file));

            Console.WriteLine("\nUsing the following files for unit 2:");
            files2.ForEach(file => Console.WriteLine(file));

            // Parse n-peptide and offset it to positive Z
            foreach (string file1 in files1)
            {
                string pdbCode1 = PdbQuick.CodeFromFilePath(file1);
                IChain peptide1 = PdbQuick.ChainFromFileOrCode(file1);
                peptide1.Translate(-Rmsd.GetBackboneCentroid(peptide1));

                // Parse c-peptide and offset it to positive Z
                foreach (string file2 in files2)
                {
                    string pdbCode2 = PdbQuick.CodeFromFilePath(file2);
                    IChain peptide2 = PdbQuick.ChainFromFileOrCode(file2);
                    peptide2.Translate(-Rmsd.GetBackboneCentroid(peptide2));

                    if (peptide1 == null || peptide2 == null)
                    {
                        Console.WriteLine("Pdb parsing failed for one of [{0}, {1}]", file1, file2);
                        continue;
                    }

                    JobStartParamsAsymmetricPair startParams = new JobStartParamsAsymmetricPair();
                    startParams.OutputPrefix = "PAIR";
                    startParams.PdbCodeN = pdbCode1;
                    startParams.PdbCodeC = pdbCode2;
                    startParams.PeptideN = peptide1;
                    startParams.PeptideC = peptide2;
                    startParams.RangeN = options.Range1 != null ? (Range)options.Range1 : new Range(0, peptide1.Count - 1);
                    startParams.RangeC = options.Range2 != null ? (Range)options.Range2 : new Range(0, peptide2.Count - 1);
                    startParams.TopX = options.TopX;
                    startParams.Counter = sharedCounter; // Shared counter across queued jobs
                    Debug.Assert(startParams.Validate(), "JobStartParamsAsymmetricPair validation failure");

                    _threadCountSemaphore.WaitOne();
                    sharedCounter.IncrementQueued();

                    Console.WriteLine("Queuing triplet [Bundle {0}]:[Bundle {1}], {2:F2} % ({3}/{4})", pdbCode1, pdbCode2, startParams.Counter.PercentQueued, startParams.Counter.Queued, startParams.Counter.Total);

                    Thread thread = new Thread(new ParameterizedThreadStart(RunJobAsymmetricPair));
                    thread.Start(startParams);
                }
            }
        }

        static void RunJobAngleCXRCX(object obj)
        {
            JobStartParamsCXRCX start = (JobStartParamsCXRCX) obj;
            int count = 0;

            // Get a bunch of fusions with the homo-oligomer axes positioned such that after transformation by the symmetry definition's first coordinate system transformation,
            // the axes overlap the first and second repeating unit axes
            SymmetryBuilder builder = start.Symmetry;
            CoordinateSystem coordinateSystem1 = builder.GetPrincipalCoordinateSystem(start.UnitId1);
            CoordinateSystem coordinateSystem2 = builder.GetPrincipalCoordinateSystem(start.UnitId2);
            Structure cnAsymmetricUnit1 = new Structure(start.Cx1);
            Structure cnAsymmetricUnit2 = new Structure(start.Cx2);
            Structure spacer = new Structure(start.Repeat);

            Line principalAxis1 = Line.CreateFromPointAndDirection(coordinateSystem1.Translation, coordinateSystem1.UnitX); // The rosetta symdefs axes are along X of each transformed coordinate system, but something not requiring foreknowledge would be nice.
            Line principalAxis2 = Line.CreateFromPointAndDirection(coordinateSystem2.Translation, coordinateSystem2.UnitX); // The rosetta symdefs axes are along X of each transformed coordinate system, but something not requiring foreknowledge would be nice.
            //Quaternion premultiply = Quaternion.Inverse(coordinateSystem1.Transform.Rotation);
            //principalAxis1.Direction = Vector3.Transform(principalAxis1.Direction, premultiply);
            //principalAxis2.Direction = Vector3.Transform(principalAxis2.Direction, premultiply);
            //principalAxis1.Point = Vector3.Transform(principalAxis1.Point, premultiply);
            //principalAxis2.Point = Vector3.Transform(principalAxis2.Point, premultiply);
            //AxisRealignment axisAlignmentUtility = AxisRealignment.Create((float) start.AngleDegrees, principalAxis1, principalAxis2);
            TwoAxisRealignment axisAlignmentUtility = AxisRealignmentFactory.Create(builder.GetArchitecture(), start.UnitId1, start.UnitId2);

            for(int chainIndexCn1 = 0; chainIndexCn1 < cnAsymmetricUnit1.Count; chainIndexCn1++)
            {
                for (int chainIndexCn2 = 0; chainIndexCn2 < cnAsymmetricUnit2.Count; chainIndexCn2++)
                {
                    for(int chainIndexSpacer = 0; chainIndexSpacer < spacer.Count; chainIndexSpacer++)
                    {
                        List<Model> outputs = Fuse.SymmetricFusionGenerator.CnSnCn(
                            (IStructure) cnAsymmetricUnit1.DeepCopy(),
                            (IStructure) cnAsymmetricUnit2.DeepCopy(),
                            new IStructure[] { (IStructure) spacer.DeepCopy() },
                            new int[] { chainIndexCn1 }, 
                            new int[] { chainIndexCn2 }, 
                            new int[][] { new int[] { chainIndexSpacer } },
                            builder, start.UnitId1, start.UnitId2);

                        int writtenOutCount = 0;
                        foreach (Model output in outputs)
                        {
                            // Output files
                            lock (lockFileIO)
                            {
                                string prefixFullStructure = String.Format("CXRCX_{0}_{1}_{2}_{3}_{4}", start.OutputPrefix, start.PdbCodeBundle1, start.PdbCodeRepeat, start.PdbCodeBundle2, count++);
                                string prefixAsymmetricUnit = "ASU_" + prefixFullStructure;

                                PdbQuick.Save(prefixFullStructure + ".pdb", output.Structure);
                                PdbQuick.Save(prefixAsymmetricUnit + ".pdb", output.AsymmetricUnit);

                                // Determine which residues should be designable: include residues whose contacts have been removed and 
                                // those with new contacts between previously separate substructures, but not existing interface residues
                                Selection designable = new Selection(output.Selections[SymmetricFusionGenerator.AaSelectionAsuDesignable]);
                                
                                // Save a resfile identifying the designable residues
                                Resfile resfileDesignable = new Resfile();
                                resfileDesignable.SetDesignOperation(output.AsymmetricUnit, designable, ResfileDesignOperation.NOTAA, new char[] { 'W', 'M', 'P', 'C' });
                                File.AppendAllLines(prefixAsymmetricUnit + ".resfile", resfileDesignable.GetFileText());                                
                            }
                            writtenOutCount++;
                        }
                    }
                }
            }            

            start.Counter.IncrementComplete();
            Console.WriteLine("Completed triplet [Bundle {0}]:[Repeat {1}]:[Bundle {2}], {3:F2} degrees, {4:F2}% ({5}/{6})", start.PdbCodeBundle1, start.PdbCodeRepeat, start.PdbCodeBundle2, start.AngleDegrees, start.Counter.PercentComplete, start.Counter.Complete, start.Counter.Total);
            _threadCountSemaphore.Release();
        }

        static void RunJobAngleCXCX(object start)
        {
            JobStartParamsCXCX startParams = (JobStartParamsCXCX)start;
            int count = 0;

            // Get a bunch of fusions with the homo-oligomer axes positioned such that after transformation by the symmetry definition's first coordinate system transformation,
            // the axes overlap the first and second repeating unit axes
            SymmetryBuilder builder = startParams.Symmetry;
            CoordinateSystem coordinateSystem1 = builder.GetPrincipalCoordinateSystem(startParams.UnitId1);
            CoordinateSystem coordinateSystem2 = builder.GetPrincipalCoordinateSystem(startParams.UnitId2);
            Line principalAxis1 = Line.CreateFromPointAndDirection(coordinateSystem1.Translation, coordinateSystem1.UnitX); // The rosetta symdefs axes are along X of each transformed coordinate system, but something not requiring foreknowledge would be nice.
            Line principalAxis2 = Line.CreateFromPointAndDirection(coordinateSystem2.Translation, coordinateSystem2.UnitX); // The rosetta symdefs axes are along X of each transformed coordinate system, but something not requiring foreknowledge would be nice.
            Quaternion premultiply = Quaternion.Inverse(coordinateSystem1.Transform.Rotation);
            principalAxis1.Direction = Vector3.Transform(principalAxis1.Direction, premultiply);
            principalAxis2.Direction = Vector3.Transform(principalAxis2.Direction, premultiply);
            principalAxis1.Point = Vector3.Transform(principalAxis1.Point, premultiply);
            principalAxis2.Point = Vector3.Transform(principalAxis2.Point, premultiply);
            TwoAxisRealignment axisAlignmentUtility = TwoAxisRealignment.Create((float)startParams.AngleDegrees, principalAxis1, principalAxis2);
            List<FusionDesignInfo> fusions = Fuse.SymmetricFusionGenerator.CnCn(startParams.Cx1, startParams.Cx2, startParams.ChainCount1, startParams.ChainCount2, startParams.AngleDegrees, axisAlignmentUtility);

            int writtenOutCount = 0;
            foreach (FusionDesignInfo fusion in fusions)
            {
                // Create the pdb chains as per the symmetry builder and find whether any of these copies clashes
                bool fullSystemClashes = false;
                List<Chain> outputChains = new List<Chain>();

                if (builder is PxSymmetryBuilder)
                {
                    TwoAxisFusionDesignInfo twoAxisFusion = (TwoAxisFusionDesignInfo)fusion;
                    PxSymmetryBuilder pxBuilder = (PxSymmetryBuilder)builder;
                    pxBuilder.Scale = Line.GetDistance(twoAxisFusion.Axis1, twoAxisFusion.Axis2.Point) / Line.GetDistance(principalAxis1, principalAxis2.Point);
                    //pxBuilder.Scale = 2 * Line.GetDistance(twoAxisFusion.Axis2, twoAxisFusion.Axis1.Point);
                }

                CoordinateSystem[] coordinateSystems = builder.GetCoordinateSystems(startParams.UnitId1);
                for (int i = 0; i < coordinateSystems.Length; i++)
                {
                    outputChains.Add(new Chain(fusion.Peptide));
                    outputChains[i].Transform(coordinateSystems[i].Transform);


                    if (i > 0 && Clash.AnyContact(outputChains[0], outputChains[i], Clash.ContactType.MainchainMainchainClash))
                    {
                        fullSystemClashes = true;
                        break;
                    }

                }

#if DEBUG
                IChain[] originalChains = fusion.PdbOutputChains;
#endif
                fusion.PdbOutputChains = outputChains.ToArray();

                if (fullSystemClashes || writtenOutCount >= startParams.TopX)
                    continue;

                lock (lockFileIO)
                {

                    // Output files
                    string identifier = String.Format("CXCX_{0}_{1}_{2}_{3}", startParams.OutputPrefix, startParams.PdbCodeBundle1, startParams.PdbCodeBundle2, count++);

                    // -resfile
                    Resfile resfile = new Resfile();
                    fusion.DesignablePositions.ToList().ForEach(item => resfile.SetNotAminoAcids(0, item, "MW"));  // Set the designable residues, disallowing Met and Trp entirely
                    fusion.ImmutablePositions.ToList().ForEach(item => resfile.SetNativeRotamer(0, item));         // Set the immutable positions, overriding any designable ones if they conflict
                    File.AppendAllLines(identifier + ".resfile", resfile.GetFileText());

                    // -scores
                    File.AppendAllText("scores_CXCX.txt", String.Format("{0} {1:F2}\n", identifier, fusion.Score));

                    // -forward folding fixed-backbone ranges
                    File.AppendAllText(identifier + ".fixed_ranges", String.Format("fixedbb_range1={0}-{1} fixedbb_range2={2}-{3}", 1, fusion.IdentityRanges[0].End + 1, fusion.IdentityRanges[1].Start + 1, fusion.Peptide.Count));

                    // -pdbs
                    PdbQuick.Save(identifier + "_monomer.pdb", new IChain[] { fusion.Peptide });
                    PdbQuick.Save(identifier + "_multichain.pdb", fusion.PdbOutputChains);
#if DEBUG
                    PdbQuick.Save(identifier + "_2axis.pdb", originalChains);
#endif
                }
            }
            Console.WriteLine("Completed triplet [Bundle {0}]:[Bundle {1}], {2:F2} degrees, {3:F2}% ({4}/{5})", startParams.PdbCodeBundle1, startParams.PdbCodeBundle2, startParams.AngleDegrees, startParams.Counter.PercentComplete, startParams.Counter.Complete, startParams.Counter.Total);

            startParams.Counter.IncrementComplete();
            _threadCountSemaphore.Release();
        }
        static void RunJobAsymmetricPair(object start)
        {
            JobStartParamsAsymmetricPair startParams = (JobStartParamsAsymmetricPair)start;

            int count = 0;
            IEnumerable<FusionDesignInfo> fusions = Fuse.Fusion2.GetSplices(startParams.PeptideN, startParams.RangeN, startParams.PeptideC, startParams.RangeC, (int) startParams.TopX);

            lock (lockFileIO)
            {
                foreach (FusionDesignInfo fusion in fusions)
                {
                    // Output files
                    string identifier = String.Format("PAIR_{0}_{1}_{2}_{3}", startParams.OutputPrefix, startParams.PdbCodeN, startParams.PdbCodeC, count++);

                    // -resfile
                    Resfile resfile = new Resfile();
                    fusion.DesignablePositions.ToList().ForEach(item => resfile.SetNotAminoAcids(0, item, "MW"));  // Set the designable residues, disallowing Met and Trp entirely
                    fusion.ImmutablePositions.ToList().ForEach(item => resfile.SetNativeRotamer(0, item));         // Set the immutable positions, overriding any designable ones if they conflict
                    File.AppendAllLines(identifier + ".resfile", resfile.GetFileText());

                    // -scores
                    File.AppendAllText("scores_CXR.txt", String.Format("{0} {1:F2}\n", identifier, fusion.Score));

                    // -forward folding fixed-backbone ranges
                    File.AppendAllText(identifier + ".fixed_ranges", String.Format("fixedbb_range1={0}-{1} fixedbb_range2={2}-{3}", 1, fusion.IdentityRanges[0].End + 1, fusion.IdentityRanges[1].Start + 1, fusion.Peptide.Count));

                    // -pdbs
                    PdbQuick.Save(identifier + "_monomer.pdb", new IChain[] { fusion.Peptide });
                    PdbQuick.Save(identifier + "_multichain.pdb", fusion.PdbOutputChains);
                }
            }
            Console.WriteLine("Completed triplet [N-term {0}]:[C-term {1}], {2:F2}% ({3}/{4})", startParams.PdbCodeN, startParams.PdbCodeC, (float)startParams.Counter.Complete / startParams.Counter.Total * 100, startParams.Counter.Complete, startParams.Counter.Total);

            startParams.Counter.IncrementComplete();
            _threadCountSemaphore.Release();
        }

        static string[] GetPdbFiles(string defaultRootDirectory, string regex)
        {
            if (regex.Contains(Path.DirectorySeparatorChar))
            {
                defaultRootDirectory = regex.Substring(0, regex.LastIndexOf(Path.DirectorySeparatorChar));
                regex = regex.Substring(regex.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }

            if (!regex.EndsWith(".pdb"))
                regex = regex + ".pdb";

            //string[] files = Directory.EnumerateFiles(defaultRootDirectory, regex, SearchOption.AllDirectories).ToArray();
            string[] files = Directory.EnumerateFiles(defaultRootDirectory, regex, SearchOption.TopDirectoryOnly).ToArray();
            return files;
        }
    }
}
