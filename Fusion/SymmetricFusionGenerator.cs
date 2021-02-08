//#define NO_CLASH_CHECK
#define ANKBINDER_KLUDGE

using Microsoft.Xna.Framework;
using NamespaceUtilities;
using Core;
using Core.Tools.Analysis;
using Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tools;
using Core.Interfaces;
using Core.Quick.Pattern;
using Core.Symmetry;
using Core.Collections;

namespace Fuse
{
    public class SymmetricFusionGenerator
    {
        // Identifiers for selections returned in fusion output Models
        public const string AaSelectionAsuInterface = "AaSelectionAsuInterface";
        public const string AaSelectionAsuSubstructures = "AaSelectionAsuSubstructures";
        public const string AaSelectionAsuSubstructureInterfaces = "AaSelectionAsuSubstructureInterfaces";
        public const string AaSelectionAsuRemovedContacts = "AaSelectionAsuRemovedContacts";
        public const string AaSelectionAsuDesignable = "AaSelectionAsuDesignable";
        public const string AaSelectionAsuClash = "AaSelectionAsuClash";

        public const float DefaultAngleToleranceDegrees = 5f;
        public const int DefaultSubstructureLengthMin = 55;
        public const int DefaultInterfaceTruncationMax = 0;

        class FusionInfoCXCX
        {
            public readonly IChain Cx1;
            public readonly IChain Cx2;
            public readonly Vector3 AxisCoordinate1;
            public readonly Vector3 AxisDirection1;
            public readonly Vector3 AxisCoordinate2;
            public readonly Vector3 AxisDirection2;
            public readonly int AlignmentStartIndex1;
            public readonly int AlignmentEndIndex1;
            public readonly int AlignmentStartIndex2;
            public readonly int AlignmentEndIndex2;

            public FusionInfoCXCX(IChain oligo1, IChain oligo2, Vector3 axisCoordinate1, Vector3 axisDirection1, Vector3 axisCoordinate2, Vector3 axisDirection2, int alignmentStartIndex1, int alignmentEndIndex1, int alignmentStartIndex2, int alignmentEndIndex2)
            {
                Cx1 = oligo1;
                Cx2 = oligo2;
                AxisCoordinate1 = axisCoordinate1;
                AxisDirection1 = Vector3.Normalize(axisDirection1);
                AxisCoordinate2 = axisCoordinate2;
                AxisDirection2 = Vector3.Normalize(axisDirection2);
                AlignmentStartIndex1 = alignmentStartIndex1;
                AlignmentEndIndex1 = alignmentEndIndex1;
                AlignmentStartIndex2 = alignmentStartIndex2;
                AlignmentEndIndex2 = alignmentEndIndex2;
            }
        }

        /// To reduce alignment repetition, bundle1 will be aligned in all non-clashing manners with the repeat and the resultant bundle1 Z symmetry axis vectors will be recorded. The same will be done with
        /// bundle2 and repeat. Then, an all to all comparison of bundle1 to bundle2 symmetry axes is made and tested for their angle and nearest approach. Finally, the bundles are perfectly aligned\
        /// to the desired angle (irrespective of the repeat protein alignment) and the repeat protein is placed to minimize the RMSD of of the overlapping helices.
        /// </summary>
        /// <param name="olig1"></param>
        /// <param name="olig2"></param>
        /// <param name="spacer"></param>
        /// <param name="angleDegrees"></param>
        /// <returns></returns>
        public static List<Model> CnSpacerCn(
            Structure cn1,
            int cnIndex1,
            Structure cn2,
            int cnIndex2,
            Structure spacer,
            int spacerIndex1,
            SymmetryBuilder builder,
            string axisId1,
            string axisId2,
            float angleToleranceDegrees = DefaultAngleToleranceDegrees,
            int topX = 10)
        {
            int multiplicity1 = builder.GetMultiplicity(axisId1);
            int multiplicity2 = builder.GetMultiplicity(axisId2);
            TwoAxisRealignment axisRealignment = AxisRealignmentFactory.Create(builder, axisId1, axisId2);
            List<Model> results = new List<Model>();
            IChain chain1 = cn1[cnIndex1];
            IChain chain2 = cn2[cnIndex2];
            IChain chainSpacer = spacer[spacerIndex1];
            Selection selfInterface1 = Interface.GetCnInterAsuContacts(cn1, multiplicity1, Vector3.UnitZ);
            Selection selfInterface2 = Interface.GetCnInterAsuContacts(cn2, multiplicity2, Vector3.UnitZ);
            int? lastInterfacePositionCn1 = selfInterface1.Aas.Select(aa => chain1.IndexOf(aa)).OrderBy(val => val).LastOrDefault();
            int? firstInterfacePositionCn2 = selfInterface2.Aas.Select(aa => chain2.IndexOf(aa)).OrderBy(val => val).FirstOrDefault();
            Selection cnAllowedFusionPositions1 = lastInterfacePositionCn1 == null || lastInterfacePositionCn1 == chain1.Count - 1? 
                new Selection() : 
                new Selection(chain1[(int)lastInterfacePositionCn1 + 1, chain1.Count - 1].Cast<IAa>());
            Selection cnAllowedFusionPositions2 = firstInterfacePositionCn2 == null ?
                new Selection() :
                new Selection(chain2[0, (int)firstInterfacePositionCn2].Cast<IAa>());
            
            int minSpacerLength = 30;
            int spacerRepeatLength = 0;
            Sequence.TryGetInternalRepeatLength(chainSpacer, out spacerRepeatLength);

            // WARNING: This arrangment of N-chain splicing to the first repeat and C-chain splicing to any repeat only makes sense if the spacer is the same chain
            // in both cases. This will have to be updated when the function allows splicing to different chains in the spacer.
            List<SequenceAlignment> alignmentsC1S = Fusion.GetFilteredSequenceAlignments(chain1, false, chainSpacer, true, SS.Helix | SS.Extended);     // Only allow chain1 to fuse to the first repeat of the spacer
            List<SequenceAlignment> alignmentsSC2 = Fusion.GetFilteredSequenceAlignments(chainSpacer, false, chain2, false, SS.Helix | SS.Extended);    // Allow chain2 to fuse to any repeat in the spacer

            // 1) Would remove a homo-oligomer interface residue
            alignmentsC1S.RemoveAll(a => !cnAllowedFusionPositions1.Aas.Contains(chain1[a.Range1.End]));
            alignmentsSC2.RemoveAll(a => !cnAllowedFusionPositions2.Aas.Contains(chain2[a.Range2.Start]));

            // Record the rotation centers and new symmetry axes that result from alignments of oligomer to spacer
            Dictionary<SequenceAlignment, Vector3> rotationCentersC1S = new Dictionary<SequenceAlignment, Vector3>();
            Dictionary<SequenceAlignment, Vector3> rotationCentersSC2 = new Dictionary<SequenceAlignment, Vector3>();
            Dictionary<SequenceAlignment, LineTrackingCoordinateSystem> symmetryAxesC1S = new Dictionary<SequenceAlignment, LineTrackingCoordinateSystem>();
            Dictionary<SequenceAlignment, LineTrackingCoordinateSystem> symmetryAxesSC2 = new Dictionary<SequenceAlignment, LineTrackingCoordinateSystem>();

            foreach (SequenceAlignment alignmentC1S in alignmentsC1S)
            {
                Vector3 rotationCenter = Geometry.GetCenterNCAC(chainSpacer[alignmentC1S.Range2.Start, alignmentC1S.Range2.End]);
                Matrix alignmentTransform = Rmsd.GetRmsdTransformForResidues(chain1, alignmentC1S.Range1.Start, alignmentC1S.Range1.End, chainSpacer, alignmentC1S.Range2.Start, alignmentC1S.Range2.End);
                LineTrackingCoordinateSystem axis = LineTrackingCoordinateSystem.CreateFromPointDirection(Vector3.Zero, Vector3.UnitZ);

                rotationCentersC1S[alignmentC1S] = rotationCenter;
                symmetryAxesC1S[alignmentC1S] = axis;
                axis.ApplyTransform(alignmentTransform);
            }

            foreach (SequenceAlignment alignmentSC2 in alignmentsSC2)
            {
                Vector3 rotationCenter = Geometry.GetCenterNCAC(chainSpacer[alignmentSC2.Range1.Start, alignmentSC2.Range1.End]);
                Matrix alignmentTransform = Rmsd.GetRmsdTransformForResidues(chain2, alignmentSC2.Range2.Start, alignmentSC2.Range2.End, chainSpacer, alignmentSC2.Range1.Start, alignmentSC2.Range1.End);
                LineTrackingCoordinateSystem axis = LineTrackingCoordinateSystem.CreateFromPointDirection(Vector3.Zero, Vector3.UnitZ);

                rotationCentersSC2[alignmentSC2] = rotationCenter;
                symmetryAxesSC2[alignmentSC2] = axis;
                axis.ApplyTransform(alignmentTransform);
            }

            foreach (SequenceAlignment alignmentC1S in alignmentsC1S)
            {
                foreach (SequenceAlignment alignmentSC2 in alignmentsSC2)
                {
                    // Skip alignments where the spacer portion is less than 30 residues or 1.5 repeats
                    if (alignmentSC2.Range1.End - alignmentC1S.Range2.Start < Math.Max(minSpacerLength, (int)(1.5 * spacerRepeatLength)))
                        continue;

                    // Test the angle between 
                    LineTrackingCoordinateSystem axis1 = symmetryAxesC1S[alignmentC1S];
                    LineTrackingCoordinateSystem axis2 = symmetryAxesSC2[alignmentSC2];
                    Vector3 rotationCenter1 = rotationCentersC1S[alignmentC1S];
                    Vector3 rotationCenter2 = rotationCentersSC2[alignmentSC2];
                    LineTrackingCoordinateSystem realignedAxis1 = null;
                    LineTrackingCoordinateSystem realignedAxis2 = null;

                    float errorDegrees = 0;
                    if (!axisRealignment.GetLocalRealignmentWithTwoRotationCenters(axis1, rotationCenter1, axis2, rotationCenter2, out realignedAxis1, out realignedAxis2, out errorDegrees))
                        continue;

                    if (errorDegrees > angleToleranceDegrees)
                        continue;

                    IChain realignedChain1 = new Chain(chain1); realignedChain1.Transform(realignedAxis1.Transform);
                    IChain realignedChain2 = new Chain(chain2); realignedChain2.Transform(realignedAxis2.Transform);

                    // Test for clashes along the backbone with 5 regions defined as [ 1 --------- ] [ splice1 ] [ 2 --------- ] [ splice2 ] [ 3 --------- ]
                    // Check 1:2:3
                    if(Clash.AnyContact( new IAa[][] {
                            realignedChain1[0, alignmentC1S.Range1.Start].Cast<IAa>().ToArray(),
                            chainSpacer[alignmentC1S.Range2.End, alignmentSC2.Range1.Start].Cast<IAa>().ToArray(),
                            realignedChain2[alignmentSC2.Range2.End, realignedChain2.Count - 1].Cast<IAa>().ToArray()
                        }, Clash.ContactType.MainchainMainchainClash))
                    { continue; }

                    // Check 1:splice2
                    if (Clash.AnyContact( new IAa[][] {
                            realignedChain1[0, alignmentC1S.Range1.Start].Cast<IAa>().ToArray(),
                            realignedChain2[alignmentSC2.Range2.Start, alignmentSC2.Range2.End].Cast<IAa>().ToArray()
                        }, Clash.ContactType.MainchainMainchainClash))
                    { continue; }

                    // Check splice1:3
                    if (Clash.AnyContact( new IAa[][] {
                            realignedChain1[alignmentC1S.Range1.Start,alignmentC1S.Range1.End].Cast<IAa>().ToArray(),
                            realignedChain2[alignmentSC2.Range2.End, realignedChain2.Count - 1].Cast<IAa>().ToArray()
                        }, Clash.ContactType.MainchainMainchainClash))
                    { continue; }

                    // Create the spliced peptide and test for clashes about the two symmetry axes
                    IChain chainFusion = Fusion.GetChain(
                        new IChain[] { realignedChain1, chainSpacer, realignedChain2 },
                        new SequenceAlignment[] { alignmentC1S, alignmentSC2 },
                        Selection.Union(selfInterface1, selfInterface2));

                    // Pattern the ASU about each axis and check whether the backbones clash. Backbone clashes are disallowed, but sidechains clashes
                    // are allowable because they can potentially be designed away.
                    Structure placed1 = new Structure(cn1);
                    placed1.RemoveAt(cnIndex1);
                    placed1.Transform(realignedAxis1.Transform);
                    
                    Structure placed2 = new Structure(cn2);
                    placed2.RemoveAt(cnIndex2);
                    placed2.Transform(realignedAxis2.Transform);
                    
                    Structure asym = new Structure(placed1.Union(placed2).Union(new IChain[] { chainFusion }));
                    AxisPattern<IStructure> pattern1 = new AxisPattern<IStructure>(realignedAxis1, multiplicity1, asym);
                    AxisPattern<IStructure> pattern2 = new AxisPattern<IStructure>(realignedAxis2, multiplicity2, asym);
                    IStructure[] neighbors1 = pattern1[1, multiplicity1 - 1].ToArray();
                    IStructure[] neighbors2 = pattern2[1, multiplicity2 - 1].ToArray();

                    //List<IStructure> patternAxis1 = CxUtilities.Pattern(asym, Line.CreateFrom(realignedAxis1), multiplicity1, new int[] { 0 } /* skip 0th non-moved copy */, true).ToList();
                    //List<IStructure> patternAxis2 = CxUtilities.Pattern(asym, Line.CreateFrom(realignedAxis2), multiplicity2, new int[] { 0 } /* skip 0th non-moved copy */, true).ToList();

                    if (Clash.AnyContact(new IStructure[] { asym }, neighbors1, Clash.ContactType.MainchainMainchainClash))
                        continue;
                    if (Clash.AnyContact(new IStructure[] { asym }, neighbors2, Clash.ContactType.MainchainMainchainClash))
                        continue;

                    Matrix twoAxisAlignmentMatrix = Matrix.Identity;
                    Trace.Assert(axisRealignment.GetGlobalRealignment(Line.CreateFrom(realignedAxis1), Line.CreateFrom(realignedAxis2), out twoAxisAlignmentMatrix));
                    asym.Transform(twoAxisAlignmentMatrix);

                    // Pattern the ASU about the overall symmetry and check whether the backbones clash
                    IStructure total = builder.Pattern(axisId1, asym);
                    if (Clash.AnyContact(total[0, asym.Count - 1].Select(ichain => (IChain)ichain), total[asym.Count, total.Count - 1].Select(ichain => (IChain)ichain), Clash.ContactType.MainchainMainchainClash))
                        continue;

                    // Assemble the output model
                    // -- structure, asu, symmetry
                    Model model = new Model(builder, asym);

                    // -- substructure selection
                    int spacerStart = alignmentC1S.Range1.Start; // Attribute both splice sites and intervening sequence to the spacer
                    int spacerEnd = spacerStart + alignmentSC2.Range1.End - alignmentC1S.Range2.Start;
                    Selection selectionAsuSpacer = new Selection(chainFusion[spacerStart, spacerEnd].Cast<IAa>());
                    Selection selectionAsuCn1 = new Selection(placed1.SelectMany(chain => chain).Union(chainFusion[0, spacerStart - 1].Cast<IAa>()));
                    Selection selectionAsuCn2 = new Selection(placed2.SelectMany(chain => chain).Union(chainFusion[spacerEnd + 1, chainFusion.Count - 1].Cast<IAa>()));
                    Selection[] selectionSubstructures = new Selection[] { selectionAsuCn1, selectionAsuSpacer, selectionAsuCn2 };
                    model.SelectionSets.Add(AaSelectionAsuSubstructures, selectionSubstructures);

                    // -- substructure interface selection
                    Selection selectionSubstructureInterface = Clash.GetContactSelection(selectionSubstructures, Clash.ContactType.Atomic | Clash.ContactType.VectorCACB);
                    model.Selections.Add(AaSelectionAsuSubstructureInterfaces, selectionSubstructureInterface);

                    // --self-interface about either axis
                    Selection selectionInterface1 = Interface.GetCnInterAsuContacts(placed1, multiplicity1, axisRealignment.Axis1);
                    Selection selectionInterface2 = Interface.GetCnInterAsuContacts(placed2, multiplicity1, axisRealignment.Axis2);
                    model.Selections.Add(AaSelectionAsuInterface, Selection.Union(selectionInterface1, selectionInterface2));

                    results.Add(model);
#if DEBUG
                    return results;
#endif
                }
            }
            
            return results;
        }

        // Fuses two cyclic homo-oligomers and one or more spacers to generate the desired 
        // symmetric architecture, if such alignments can be found. The chains that may be 
        // fused must be specified per structure.
        public static List<Model> CnSnCn(
            IStructure cn1,
            IStructure cn2,
            IStructure[] spacers,
            int[/* chain index */] allowedCnIndices1,
            int[/* chain index */] allowedCnIndices2,
            int[ /* structure index */][/* chain index */] allowedSpacerIndices,
            SymmetryBuilder symmetry,
            string axis1,
            string axis2,
            float angleToleranceDegrees = DefaultAngleToleranceDegrees,  // The degree of rotation that is permitted to fix non-ideality of the fusion to the desired geometry. Applied at both fusion sites.
            int maxInterfaceTruncation1 = DefaultInterfaceTruncationMax, // Possibly allow the deletion of a few homo-oligomer interface residues (default disallowed, i.e. 0)
            int maxInterfaceTruncation2 = DefaultInterfaceTruncationMax, // Possibly allow the deletion of a few homo-oligomer interface residues (default disallowed, i.e. 0)
            int minSubstructureLength = DefaultSubstructureLengthMin     // Don't permit super-short substructures to result from truncation as they won't have any significant stabilizing core
            )
        {
            List<Model> outputs = new List<Model>();

            // Build up data structures necessary to analyze fusions
            // --track axes multiplicity
            int multiplicity1 = symmetry.GetMultiplicity(axis1);
            int multiplicity2 = symmetry.GetMultiplicity(axis2);
            TwoAxisRealignment axisRealignment = AxisRealignmentFactory.Create(symmetry, axis1, axis2);

            // --access structures from a single array
            IStructure[] structures = new IStructure[spacers.Length + 2];
            structures[0] = cn1;
            structures[spacers.Length + 1] = cn2;
            spacers.CopyTo(structures, 1);

            // --track which chains are allowed for fusion
            int[][] allowedChains = new int[spacers.Length + 2][]; // indices: [structure][chain]
            allowedSpacerIndices.CopyTo(allowedChains, 1);
            allowedChains[0] = allowedCnIndices1;
            allowedChains[spacers.Length + 1] = allowedCnIndices2;

#if ANKBINDER_KLUDGE
            allowedChains[0] = new int[] { 0 }; // ANK-binder if chain 0/A and GFP is chain 1/B. ANK should be the strut.
#endif

            // --track allowable N and C-terminal fusion positions, i.e. require non-removal of interface residues and maintain a minimum length
            bool[][][] expendableTowardsN = new bool[structures.Length][][]; // indices: [structure][chain][aa]
            bool[][][] expendableTowardsC = new bool[structures.Length][][];
            spacers.Select(s => Interface.GetExpendableTowardsN(s)).ToArray().CopyTo(expendableTowardsN, 1);
            spacers.Select(s => Interface.GetExpendableTowardsC(s)).ToArray().CopyTo(expendableTowardsC, 1);
            expendableTowardsN[0] = Interface.GetExpendableCnAsuTowardsN(cn1, multiplicity1, maxInterfaceTruncation1);
            expendableTowardsN[structures.Length - 1] = Interface.GetExpendableCnAsuTowardsN(cn2, multiplicity2, maxInterfaceTruncation2);
            expendableTowardsC[0] = Interface.GetExpendableCnAsuTowardsC(cn1, multiplicity1, maxInterfaceTruncation1);
            expendableTowardsC[structures.Length - 1] = Interface.GetExpendableCnAsuTowardsC(cn2, multiplicity2, maxInterfaceTruncation2);

            // --compute alignments
            // --filter out those where both the N->C and C->N fusions clash about the Cn axis because they can never work
            TransformSequenceAlignment[][] alignments = Fusion.GetRepeatFilteredTransformAlignments(structures, allowedChains);
            alignments[0] = alignments[0].Where(a => !Fusion.TestClashCn(multiplicity1, cn1, structures[1], a, true, true, minSubstructureLength)).ToArray();
            alignments[alignments.Length - 1] = alignments.Last().Where(a => !Fusion.TestClashCn(multiplicity2, cn2, structures[structures.Length - 2], TransformSequenceAlignment.Reversed(a), true, true, minSubstructureLength)).ToArray();
            int[] alignmentCounts = alignments.Select(a => a.Length).ToArray();

            // Iterate each combination of alignments and identify those near the desired angle
            long combinations = GetAlignmentCombinationCount(alignments);// talignmentCounts.Aggregate((long)1, (a, b) => a * b);
            for (long combination = 0; combination < combinations; combination++)
            {
                GetAlignmentCombination(combination, alignments, out TransformSequenceAlignment[] transformAlignments, out Matrix[] transforms);

                // Determine whether the axis directions would be within the angle tolerance if they were already intersecting. This is
                // a weaker check than necessary, but helps to eliminate many possibilities without doing more expensive geometry checks.
                float dot = Math.Abs(Vector3.Dot(Vector3.Transform(Vector3.UnitZ, transforms.Last()), Vector3.UnitZ));
                float angle1 = (float)(Math.Acos(-dot) * 180 / Math.PI);
                float angle2 = (float) (Math.Acos(dot) * 180 / Math.PI);
                if (Math.Abs(angle1 - axisRealignment.AngleDegrees) > angleToleranceDegrees && Math.Abs(angle2 - axisRealignment.AngleDegrees) > angleToleranceDegrees)
                    continue;

                // Determine the angular deviation from ideality and transforms that idealize the symmetry
                // when applied to the first and last component only
                Vector3 rotationCenter1 = transformAlignments[0].Centroid1;
                Vector3 rotationCenter2 = Vector3.Transform(transformAlignments.Last().Centroid2, transforms.Last());
                LineTrackingCoordinateSystem positionedAxis1 = new LineTrackingCoordinateSystem(Vector3.Zero, Vector3.UnitZ);
                LineTrackingCoordinateSystem positionedAxis2 = new LineTrackingCoordinateSystem(Vector3.Zero, Vector3.UnitZ); positionedAxis2.ApplyTransform(transforms.Last());
                if (!axisRealignment.GetLocalRealignmentWithTwoRotationCenters(positionedAxis1, rotationCenter1, positionedAxis2, rotationCenter2, out LineTrackingCoordinateSystem realigned1, out LineTrackingCoordinateSystem realigned2, out float errorDegrees))
                    continue;

                if (errorDegrees > angleToleranceDegrees)
                    continue;

                // Iterate possibilities for N->C and C->N of each alignment and determine whether the fusions are allowed
                // --each chain can be N or C-terminal only once
                // --fusion residues cannot be removed by a prior splice
                // --direction choice must not remove any self-interface (cyclic or other symmetry) residues
                for(int ncDirectionCombination = 0; ncDirectionCombination < Math.Pow(2, transformAlignments.Length); ncDirectionCombination++)
                {
                    bool[] ncDirections = Enumerable.Range(0, transformAlignments.Length).Select(shift => ((ncDirectionCombination >> shift & 1) == 1)).ToArray();
                    if (!TestFusionDirections(structures, transformAlignments, ncDirections, expendableTowardsN, expendableTowardsC, minSubstructureLength))
                        continue;

                    // Position all structures as per the alignment transforms and then reposition the first and last to make
                    // their axes meet the angle/intersect requirement
                    IStructure[] copies = structures.Select(s => s.DeepCopy()).Cast<IStructure>().ToArray();
                    Enumerable.Range(1, structures.Length - 2).ToList().ForEach(i => copies[i].Transform(transforms[i]));
                    copies[0].Transform(realigned1.Transform);
                    copies[structures.Length - 1].Transform(realigned2.Transform);

                    // Test clashes between each possible set of residues except for junctions against their participating structures
                    Selection[] fusionSelections;
                    Selection[] nonfusionSelections;
                    Selection[] removedSelections;
                    Fusion.GetSelections(copies, transformAlignments, ncDirections, out nonfusionSelections, out fusionSelections, out removedSelections);

                    Trace.Assert(fusionSelections.Length == nonfusionSelections.Length - 1);

                    // --junction vs junction
                    if (Clash.AnyContact(fusionSelections, Clash.ContactType.MainchainMainchainClash))
                        continue;

                    // --non-junction vs non-junction
                    if (Clash.AnyContact(nonfusionSelections, Clash.ContactType.MainchainMainchainClash))
                        continue;

                    // --junction vs non-participating
                    for (int i = 0; i < fusionSelections.Length; i++)
                    {
                        for (int j = 0; j < nonfusionSelections.Length; j++)
                        {
                            // skip comparison of junctions vs structures that make up the junction
                            if (i == j || i == j - 1)
                                continue;

                            if (Clash.AnyContact(new Selection[] { fusionSelections[i], nonfusionSelections[j] }, Clash.ContactType.MainchainMainchainClash))
                                goto ENDLOOP_NEXT_DIRECTION_COMBINATION;
                        }
                    }

                    // Asu substructures don't clash, so now test collisions between ASUs and build up the full many-copy structure
                    Matrix globalRealignment;
                    IStructure asymmetricUnit = Fusion.GetStructure(copies, transformAlignments, ncDirections, false, out IChain partialChain);
                    axisRealignment.GetGlobalRealignment(Line.CreateFrom(realigned1), Line.CreateFrom(realigned2), out globalRealignment);
                    asymmetricUnit.Transform(globalRealignment);

#if DEBUG && false
                    // DEBUG
                    // --pattern about each axis
                    AxisPattern<IStructure> patternAxis1 = new AxisPattern<IStructure>(realigned1, multiplicity1, asymmetricUnit);
                    PdbQuick.Save("pattern1.pdb", patternAxis1);

                    AxisPattern<IStructure> patternAxis2 = new AxisPattern<IStructure>(realigned2, multiplicity2, asymmetricUnit);
                    PdbQuick.Save("pattern2.pdb", patternAxis2);
#endif
                    

                    SymmetryBuilder symmetryClone = SymmetryBuilderFactory.Clone(symmetry);
                    symmetryClone.EnabledUnits = new string[] { axis1 };
                    //symmetryClone = new IdentitySymmetryBuilder();
                    Model output = new Model(symmetryClone, asymmetricUnit);

                    // Test for clashes against the original ASU
                    IStructure structure = output.Structure;
                    for (int asuChainIndex = 0; asuChainIndex < asymmetricUnit.Count; asuChainIndex++)
                    {
                        for(int neighborChainIndex = asymmetricUnit.Count; neighborChainIndex < structure.Count; neighborChainIndex++)
                        {
                            if(Clash.AnyContact(structure[asuChainIndex], structure[neighborChainIndex], Clash.ContactType.MainchainMainchainClash))
                                goto ENDLOOP_NEXT_DIRECTION_COMBINATION;
                        }
                    }

                    // Save selections that are relevant for downstream design
                    {
                        // 1) parts of structure that previously had contact with removed positions
                        Selection lostContacts = new Selection();
                        for(int i = 0; i < structures.Length; i++)
                        {
                            IAa[] remaining = nonfusionSelections[i].Aas.ToArray();
                            IAa[] removed = removedSelections[i].Aas.ToArray();

                            // Application of this transform is no longer necessary because the AAs don't even move at all, since the asu is
                            // comprised not of these aas, but of their mirrors
                            //// The removed residues were never transformed - make sure that they are repositioned
                            //// prior to checking for contact
                            //foreach(IAa aa in removed)
                            //{
                            //    aa.Transform(globalRealignment);
                            //}

                            Selection affected = Clash.GetContactSelectionInFocusSet(remaining, removed);
                            lostContacts.UnionWith(affected);   
                        }
                        output.Selections.Add(AaSelectionAsuRemovedContacts, lostContacts);
                    }

                    {
                        // 2) new inter-substructure contacts
                        Selection newContacts = new Selection();
                        
                        // substructure block vs block
                        for(int i = 0; i < structures.Length; i++)
                        {
                            IAa[] substructure1 = nonfusionSelections[i].Aas.ToArray();
                            for(int j = i + 1; j < structures.Length; j++)
                            {
                                IAa[] substructure2 = nonfusionSelections[j].Aas.ToArray();
                                Selection affected = Clash.GetContactSelection(substructure1, substructure2, Clash.ContactType.Atomic | Clash.ContactType.VectorCACB | Clash.ContactType.IgnoreInvalidCoordinates);
                                newContacts.UnionWith(affected);
                            }
                        }

                        // fusion region vs block
                        for (int i = 0; i < fusionSelections.Length; i++) 
                        {
                            IAa[] fusion = fusionSelections[i].Aas.ToArray();
                            for (int j = 0; j < nonfusionSelections.Length; j++)
                            {
                                IAa[] block = nonfusionSelections[j].Aas.ToArray();
                                Selection affected = Clash.GetContactSelection(fusion, block, Clash.ContactType.Atomic | Clash.ContactType.VectorCACB | Clash.ContactType.IgnoreInvalidCoordinates);
                                newContacts.UnionWith(affected);
                            }
                        }

                        output.Selections.Add(AaSelectionAsuSubstructureInterfaces, newContacts);
                    }

                    {
                        // 3) self-interface about axes are not designable if they originate from oligomers on those axes
                        Selection axisInterface1 = Interface.GetCnInterAsuContacts(asymmetricUnit, multiplicity1, axisRealignment.Axis1);
                        Selection axisInterface2 = Interface.GetCnInterAsuContacts(asymmetricUnit, multiplicity2, axisRealignment.Axis2);
                        Selection clashAxis1 = Interface.GetCnInterAsuContacts(asymmetricUnit, multiplicity1, axisRealignment.Axis1, Clash.ContactType.AtomicClash);
                        Selection clashAxis2 = Interface.GetCnInterAsuContacts(asymmetricUnit, multiplicity2, axisRealignment.Axis2, Clash.ContactType.AtomicClash);

                        Selection originalAxisInterface1 = new Selection(axisInterface1);                                              // axis contacts
                        originalAxisInterface1.Aas.IntersectWith(nonfusionSelections.First().Aas.Union(fusionSelections.First().Aas)); // limited to the oligomer

                        Selection originalAxisInterface2 = new Selection(axisInterface2);                                              // axis contacts
                        originalAxisInterface2.Aas.IntersectWith(nonfusionSelections.Last().Aas.Union(fusionSelections.Last().Aas));   // limited to the oligomer

                        output.Selections.Add(AaSelectionAsuInterface, Selection.Union(originalAxisInterface1, originalAxisInterface2));
                        output.Selections.Add(AaSelectionAsuClash, Selection.Union(clashAxis1, clashAxis2));
                        Debug.Assert(axisInterface1.Aas.Count > 0);
                        Debug.Assert(axisInterface2.Aas.Count > 0);

                        Selection fixedAxisInterface1 = Selection.Except(originalAxisInterface1, clashAxis1);   // the fixed residues are oligomer contacts - clashes
                        Selection fixedAxisInterface2 = Selection.Except(originalAxisInterface2, clashAxis2);                                                                 

                        // 4) contacts about the axis-interface are designable only if they are from the oligomer and non-clashing
                        Selection designableAxisInterface = new Selection();
                        designableAxisInterface.UnionWith(axisInterface1);          // self interface
                        designableAxisInterface.UnionWith(axisInterface2);
                        designableAxisInterface.ExceptWith(fixedAxisInterface1);    // minus non-clashing oligomer interface
                        designableAxisInterface.ExceptWith(fixedAxisInterface2);

                        // 5) final designable positions = substructure interfaces + lost contacts - self-interface
                        Selection designable = new Selection(designableAxisInterface);
                        designable.UnionWith(output.Selections[AaSelectionAsuSubstructureInterfaces]);
                        designable.UnionWith(output.Selections[AaSelectionAsuRemovedContacts]);
                        designable.ExceptWith(fixedAxisInterface1);
                        designable.ExceptWith(fixedAxisInterface2);
                        designable.ExceptWith(output.Selections[AaSelectionAsuInterface]);
                        output.Selections.Add(AaSelectionAsuDesignable, designable);
                    }

                    // The above selections are for the ASU's mid-level residues, not its output/mirror residues. So, map both the top-level ASU
                    // residues and the mid-level to the underlying templates and take those top-level ASU residues that match.
                    {
                        KeyValuePair<string, Selection>[] selections = output.Selections.ToArray();
                        output.Selections.Clear();

                        foreach (KeyValuePair<string, Selection> selection in selections)
                        {
                            HashSet<IAa> selectionTemplateAas = new HashSet<IAa>(selection.Value.Aas.Select(aa => aa.GetMirrorTemplate()));
                            Selection topLevelSelection = new Selection(output.AsymmetricUnit.SelectMany(chain => chain).Where(aa => selectionTemplateAas.Contains(aa.GetMirrorTemplate())));
                            output.Selections.Add(selection.Key, topLevelSelection);
                            Debug.Assert(selectionTemplateAas.Count == 0 || topLevelSelection.Aas.Count > 0);
                        }

                        foreach (KeyValuePair<string, Selection> selection in output.Selections)
                        {
                            foreach (IAa aa in selection.Value.Aas)
                            {
                                Trace.Assert(output.AsymmetricUnit.Any(chain => chain.Contains(aa)));
                            }
                        }
                    }

                    outputs.Add(output);

                    // Jump from inner clash test loop
                    ENDLOOP_NEXT_DIRECTION_COMBINATION:;
                }
            }

            return outputs;
        }

        static bool TestFusionDirections(IStructure[] structures, SequenceAlignment[] sequenceAlignments, bool[] ncDirections, bool[][][] expendableTowardsN, bool[][][] expendableTowardsC, int minSubstructureLength = DefaultSubstructureLengthMin)
        {

            Dictionary<IChain, int> removedTowardsN = new Dictionary<IChain, int>(); // The chain and the first (C-most) position removed in the indicated direction
            Dictionary<IChain, int> removedTowardsC = new Dictionary<IChain, int>(); // The chain and the first (N-most) position removed in the indicated direction

            for (int alignmentIndex = 0; alignmentIndex < ncDirections.Length; alignmentIndex++)
            {
                // Check that interface residues are not being removed
                bool ncDirection = ncDirections[alignmentIndex];
                SequenceAlignment alignment = sequenceAlignments[alignmentIndex];
                int nStructureIndex = alignmentIndex + (ncDirection ? 0 : 1);
                int cStructureIndex = alignmentIndex + (ncDirection ? 1 : 0);
                int nChainIndex = ncDirection ? alignment.ChainIndex1 : alignment.ChainIndex2;
                int cChainIndex = ncDirection ? alignment.ChainIndex2 : alignment.ChainIndex1;
                int nFirstRemovedIndex = ncDirection ? alignment.Range1.End : alignment.Range2.End;
                int cFirstRemovedIndex = ncDirection ? alignment.Range2.Start : alignment.Range1.Start;

                if (!expendableTowardsC[nStructureIndex][nChainIndex][nFirstRemovedIndex])
                    return false;

                if (!expendableTowardsN[cStructureIndex][cChainIndex][cFirstRemovedIndex])
                    return false;

                // Ensure the fused indices were not removed by a previous fusion
                IChain nChain = structures[nStructureIndex][nChainIndex];
                IChain cChain = structures[cStructureIndex][cChainIndex];

                if (removedTowardsN.ContainsKey(cChain))
                    return false;

                if (removedTowardsC.ContainsKey(nChain))
                    return false;

                if (removedTowardsN.ContainsKey(nChain) && nFirstRemovedIndex < removedTowardsN[nChain])
                    return false;

                if (removedTowardsC.ContainsKey(cChain) && cFirstRemovedIndex > removedTowardsC[cChain])
                    return false;

                removedTowardsN[cChain] = cFirstRemovedIndex;
                removedTowardsC[nChain] = nFirstRemovedIndex;
            }

            // Check that remaining chain lengths are not too small
            foreach (IChain chain in removedTowardsN.Keys.Union(removedTowardsC.Keys))
            {
                // Only perform checks on structures that were already sufficiently large, i.e.
                // ignore the check for inputs that are already really small
                if (chain.Count < minSubstructureLength)
                    continue;

                int remainingLength = chain.Count;

                if (removedTowardsN.ContainsKey(chain))
                    remainingLength -= removedTowardsN[chain];

                if (removedTowardsC.ContainsKey(chain))
                    remainingLength -= (chain.Count - removedTowardsC[chain]);

                if (remainingLength < minSubstructureLength)
                {
                    return false;
                }
            }
            return true;
        }

        static int[] GetSetIndices(long index, int[] setSizes)
        {
            int[] indices = new int[setSizes.Length];
            for(int i = 0; i < setSizes.Length; i++)
            {
                indices[i] = (int)(index % setSizes[i]);

                index /= setSizes[i];
            }
            return indices;
        }

        // TODO: cache backbone clashes by pairwise index and don't go through the trouble of aligning/splicing structures that clash. Clashes
        // can be recreated by the subtle changes in alignment optimization, but the prefilter should be helpful.
        /// <summary>
        /// ~100,000 alignments max - testing all combinations brute force seems doable:
        ///  for all terminal, accessible helices in bundle1 (2 max)
        ///     for all helices in first two repeats (6 max) x all helices further down (40 max?)
        ///         for all terminal, accessible helices in bundle2 (2 max)
        ///             for all possible alignments of helix 1 (10) and helix 2 (10)
        ///                 check 
        ///                     -angle
        ///                     -nearest approach between transformed Z axes
        ///
        /// To reduce alignment repetition, bundle1 will be aligned in all non-clashing manners with the repeat and the resultant bundle1 Z symmetry axis vectors will be recorded. The same will be done with
        /// bundle2 and repeat. Then, an all to all comparison of bundle1 to bundle2 symmetry axes is made and tested for their angle and nearest approach. Finally, the bundles are perfectly aligned\
        /// to the desired angle (irrespective of the repeat protein alignment) and the repeat protein is placed to minimize the RMSD of of the overlapping helices.
        /// </summary>
        /// <param name="olig1"></param>
        /// <param name="olig2"></param>
        /// <param name="angleDegrees"></param>
        /// <returns></returns>
        public static List<FusionDesignInfo> CnCn(IChain olig1, IChain olig2, int oligomerization1, int oligomerization2, double angleDegrees, TwoAxisRealignment axisAlignmentHelper)
        {
            List<FusionDesignInfo> fusions = new List<FusionDesignInfo>();
            List<int> oligo1InterfaceResidues = Interface.GetCnInterAsuContactIndices(olig1, oligomerization1, Vector3.UnitZ);
            List<int> oligo2InterfaceResidues = Interface.GetCnInterAsuContactIndices(olig2, oligomerization2, Vector3.UnitZ);
            int oligo1MaxInterfaceResidue = oligo1InterfaceResidues.Max();
            int oligo2MinInterfaceResidue = oligo2InterfaceResidues.Min();
            Vector3 oligo1CenterOfMassZ = new Vector3(0, 0, Rmsd.GetBackboneCentroid(olig1).Z);
            Vector3 oligo2CenterOfMassZ = new Vector3(0, 0, Rmsd.GetBackboneCentroid(olig2).Z);
            List<FusionInfoCXCX> bundle2Fusions = new List<FusionInfoCXCX>();
            //int maxInterfaceRemoval = 3;
            int maxThirdHelixShortening = 5;
            int minAlignmentLength = 10;
            int maxOvershoot = 7;
            float maxRmsd = 0.4f;

            // Find all alignments and remove those that:
            // ------------------------------------------
            List<SequenceAlignment> alignments = Fusion.GetAlignmentsPreservingFullSsBlocks(olig1, olig2, SS.Helix | SS.Extended, minAlignmentLength, maxRmsd);

            // 1) Would remove a homo-oligomer interface residue
            alignments.RemoveAll(a => a.Range1.End < oligo1MaxInterfaceResidue);
            alignments.RemoveAll(a => a.Range2.Start > oligo2MinInterfaceResidue);

            // 3) Would remove the third helix of a 3-helix bundle at either terminus, which would probably destabilize the oligomer
            List<SSBlock> nBlocks = SecondaryStructure.GetPhiPsiSSBlocksOfType(olig1, SS.Helix | SS.Extended, minAlignmentLength);
            List<SSBlock> cBlocks = SecondaryStructure.GetPhiPsiSSBlocksOfType(olig2, SS.Helix | SS.Extended, minAlignmentLength);
            if (nBlocks.Count > 2)
            {
                alignments.RemoveAll(a => a.Range1.Start < nBlocks[1].End);
            }
            if (cBlocks.Count > 2)
            {
                alignments.RemoveAll(a => a.Range2.End > cBlocks[cBlocks.Count - 2].Start);
            }

            // 4) Would shorten the third helix of a 3-helix bundle by more than X amount, because that would also probably destabilize the oligomer
            if (nBlocks.Count > 2)
            {
                int removeCount = alignments.RemoveAll(a => a.Range1.End < nBlocks[2].End - maxThirdHelixShortening);
                //if (removeCount > 0)
                //    Console.WriteLine("Removed {0} items", removeCount);
            }
            if (cBlocks.Count > 2)
            {
                int removeCount = alignments.RemoveAll(a => a.Range2.Start > cBlocks[cBlocks.Count - 3].Start + maxThirdHelixShortening);
                //if (removeCount > 0)
                //    Console.WriteLine("Removed {0} items", removeCount);
            }

            // 5) Also remove alignments where one secondary structure overshoots the other by more than 2 helix turns. It is not considered overshoot if one
            // contains the other, however.
            List<SequenceAlignment> removeOvershoots = new List<SequenceAlignment>();
            foreach (SequenceAlignment alignment in alignments)
            {
                SSBlock blockN = nBlocks.Single(block => block.Contains(alignment.Range1.Start) && block.Contains(alignment.Range1.End));
                SSBlock blockC = cBlocks.Single(block => block.Contains(alignment.Range2.Start) && block.Contains(alignment.Range2.End));

                if ((blockN.Start < alignment.Range1.Start - maxOvershoot) && (alignment.Range2.End + maxOvershoot < blockC.End))
                    removeOvershoots.Add(alignment);
            }
            alignments.RemoveAll(alignment => removeOvershoots.Contains(alignment));

            // Record the rotation centers and symmetry axis of 2 aligned onto 1
            Dictionary<SequenceAlignment, Vector3> rotationCenters = new Dictionary<SequenceAlignment, Vector3>();
            Dictionary<SequenceAlignment, LineTrackingCoordinateSystem> symmetryAxes2 = new Dictionary<SequenceAlignment, LineTrackingCoordinateSystem>();
            foreach (SequenceAlignment alignment in alignments)
            {
                Vector3 rotationCenter = Geometry.GetCenterNCAC(olig1[alignment.Range1.Start, alignment.Range1.End]);
                Matrix alignmentTransform = Rmsd.GetRmsdTransformForResidues(olig2, alignment.Range2.Start, alignment.Range2.End, olig1, alignment.Range1.Start, alignment.Range1.End);
                LineTrackingCoordinateSystem axis = LineTrackingCoordinateSystem.CreateFromPointDirection(oligo2CenterOfMassZ, Vector3.UnitZ);
                axis.ApplyTransform(alignmentTransform);

                rotationCenters[alignment] = rotationCenter;
                symmetryAxes2[alignment] = axis;
            }

            foreach (SequenceAlignment alignment in alignments)
            {
                LineTrackingCoordinateSystem axis1 = LineTrackingCoordinateSystem.CreateFromPointDirection(oligo1CenterOfMassZ, Vector3.UnitZ);
                LineTrackingCoordinateSystem axis2 = symmetryAxes2[alignment];
                Vector3 rotationCenter = rotationCenters[alignment];
                LineTrackingCoordinateSystem realignedAxis1 = null;
                LineTrackingCoordinateSystem realignedAxis2 = null;

                float errorDegrees = 0;
                if (! axisAlignmentHelper.GetLocalRealignmentWithOneRotationCenter(axis1, rotationCenter, axis2, out realignedAxis1, out realignedAxis2, out errorDegrees))
                    continue;

                if (Math.Abs(errorDegrees) > 3.0f)
                    continue;

                IChain realignedPeptide1 = new Chain(olig1); realignedPeptide1.Transform(realignedAxis1.Transform);
                IChain realignedPeptide2 = new Chain(olig2); realignedPeptide2.Transform(realignedAxis2.Transform);
                //IChain realignedPeptide2 = new Chain(olig2); realignedPeptide2.Transform(axis2.Transform * realignedAxis2.Transform);

#if DEBUG
                float degrees = (float) VectorMath.GetAngleDegrees(realignedAxis1.Point, Line.GetNearestPointOnLine(Line.CreateFrom(realignedAxis2), Line.CreateFrom(realignedAxis1)), realignedAxis2.Point);
                Debug.Assert(Math.Abs(degrees - angleDegrees) < 5 || Math.Abs(180 - degrees - angleDegrees) < 5);
#endif

                // Create the spliced peptide and test for clashes about the two symmetry axes
                IChain splicedPeptide = Fusion.GetChain(
                    new IChain[] { realignedPeptide1, realignedPeptide2 },
                    new SequenceAlignment[] { alignment },
                    new IEnumerable<int>[] { oligo1InterfaceResidues, oligo2InterfaceResidues });

                // Test for clashes intra-peptide after alignment within the spliced ranges, ignoring the overlayed helices
                // Clash checks for [ 1 --------- ] [ splice1 ] [ 2 --------- ] [ splice2 ] [ 3 --------- ]
                {
                    // Check: 1 vs 2 vs 3
                    IChain[] clashCheckPeptides = new IChain[] { realignedPeptide1, realignedPeptide2 };
                    int[] rangeStarts = new int[] { 0, alignment.Range2.End};
                    int[] rangeEnds = new int[] { alignment.Range1.Start, realignedPeptide2.Count - 1 };

#if (!NO_CLASH_CHECK)
                    if (Clash.GetInterSetBackboneClashes(clashCheckPeptides, rangeStarts, rangeEnds))
                        continue;
#endif
                }

                AxisPattern<IChain> pattern1 = new AxisPattern<IChain>(realignedAxis1, oligomerization1, splicedPeptide);
                AxisPattern<IChain> pattern2 = new AxisPattern<IChain>(realignedAxis2, oligomerization2, splicedPeptide);
                IChain[] neighbors1 = pattern1[1, oligomerization1 - 1].ToArray();
                IChain[] neighbors2 = pattern2[1, oligomerization2 - 1].ToArray();

                //IChain[] peptidesAxis1 = CxUtilities.Pattern(splicedPeptide, realignedAxis1, oligomerization1, new int[] { 0 }, true); // skip entry 0, exclude null entries in array
                //IChain[] peptidesAxis2 = CxUtilities.Pattern(splicedPeptide, realignedAxis2, oligomerization2, new int[] { 0 }, true); // skip entry 0, exclude null entries in array

#if (!NO_CLASH_CHECK)
                if (Clash.AnyContact(new IChain[] { splicedPeptide }, neighbors1, Clash.ContactType.MainchainMainchainClash))
                    continue;
                if (Clash.AnyContact(new IChain[] { splicedPeptide }, neighbors2, Clash.ContactType.MainchainMainchainClash))
                    continue;
#endif

                Range[] originalRanges = null;
                Range[] finalRanges = null;
                Fusion.GetIdentityRanges(
                    new IChain[] { realignedPeptide1, realignedPeptide2 },
                    new SequenceAlignment[] { alignment },
                    out originalRanges,
                    out finalRanges);

                IStructure structure = new Structure();
                structure.Add(splicedPeptide);
                structure.AddRange(neighbors1);
                structure.AddRange(neighbors2);
                //List<IChain> allChains = new List<IChain>();
                //allChains.Add(splicedPeptide);
                //allChains.AddRange(neighbors1);
                //allChains.AddRange(neighbors2);

                Matrix twoAxisAlignmentMatrix = Matrix.Identity;
                if (!axisAlignmentHelper.GetGlobalRealignment(Line.CreateFrom(realignedAxis1), Line.CreateFrom(realignedAxis2), out twoAxisAlignmentMatrix))
                    continue;

                structure.Transform(twoAxisAlignmentMatrix);

                FusionDesignInfo result = new FusionDesignInfo();
                result.Peptide = splicedPeptide;
                //result.Score = scoreDot;
                result.PdbOutputChains = structure.ToArray();
                result.IdentityRanges = finalRanges;
                result.OriginalRanges = originalRanges;
                result.OriginalChains = new IChain[] { olig1, olig2 };
                result.ImmutablePositions = oligo2InterfaceResidues.Select(index => index + (finalRanges[1].Start - originalRanges[1].Start)).Union(oligo1InterfaceResidues).OrderBy(val => val).ToArray();

                fusions.Add(result);
                fusions.Sort((a, b) => b.Score.CompareTo(a.Score));


                Debug.WriteLine("error degrees = " + errorDegrees.ToString());
            }

            return fusions;
        }

        static void GetAlignmentCombination(long combinationIndex, TransformSequenceAlignment[][] alignmentOptions, out TransformSequenceAlignment[] alignments, out Matrix[] cumulativeTransforms)
        {
            int[] alignmentOptionCounts = alignmentOptions.Select(a => a.Length).ToArray();
            int[] setIndices = GetSetIndices(combinationIndex, alignmentOptionCounts); // Indices of each alignment in its respective set
            alignments = Enumerable.Range(0, setIndices.Length).Select(setIndex => alignmentOptions[setIndex][setIndices[setIndex]]).ToArray();
            cumulativeTransforms = new Matrix[alignmentOptions.Length + 1];
            cumulativeTransforms[0] = Matrix.Identity;
            for (int index = 1; index < alignmentOptions.Length + 1; index++)
            {
                Matrix previous = cumulativeTransforms[index - 1];
                Matrix current = alignments[index - 1].Align2;
                current *= previous; 
                cumulativeTransforms[index] = current;
            }
        }

        static long GetAlignmentCombinationCount(TransformSequenceAlignment[][] alignmentOptions)
        {
            int[] alignmentCounts = alignmentOptions.Select(a => a.Length).ToArray();
            long combinations = alignmentCounts.Aggregate((long)1, (a, b) => a * b);
            return combinations;
        }

        public static void AsymIntoCn(
            BlockingCollectionEnqueue<Model> enqueue, 
            IStructure[] structures, 
            int[][] allowedChains, 
            int multiplicity, 
            float angleToleranceDegrees = DefaultAngleToleranceDegrees, 
            int minSubstructureLength = DefaultSubstructureLengthMin
            )
        {
            IStructure[] structuresCycle = new IStructure[structures.Length + 1];
            structures.CopyTo(structuresCycle, 0);
            structuresCycle[structuresCycle.Length - 1] = structures[0];
            structures = structuresCycle;

            Trace.Assert(multiplicity > 1);

            List<FusionDesignInfo> fusions = new List<FusionDesignInfo>();
            bool[][][] expendableTowardsN = structures.Select(s => Interface.GetExpendableTowardsN(s)).ToArray(); // indices: [structure][chain][aa]
            bool[][][] expendableTowardsC = structures.Select(s => Interface.GetExpendableTowardsC(s)).ToArray();
            allowedChains = allowedChains ?? structures.Select(s => Enumerable.Range(0, s.Count).ToArray()).ToArray();
            SymmetryBuilder symmetry = SymmetryBuilderFactory.CreateFromSymmetryName("C" + multiplicity);
            string axis1 = symmetry.GetAxesWithMultiplicity(multiplicity).First();
            CyclizeAngleRealignment realignment = new CyclizeAngleRealignment(360.0f / multiplicity);

            TransformSequenceAlignment[][] alignments = Fusion.GetRepeatFilteredTransformAlignments(structures, allowedChains);

            // Iterate each combination of alignments and identify those near the desired angle
            long combinations = GetAlignmentCombinationCount(alignments);// talignmentCounts.Aggregate((long)1, (a, b) => a * b);
            for (long combination = 0; combination < combinations; combination++)
            {
                if (enqueue.Token.IsCancellationRequested)
                    return;

                GetAlignmentCombination(combination, alignments, out TransformSequenceAlignment[] transformAlignments, out Matrix[] transforms);

                // Before computing the realignment, test whether some combination of fusion directions is allowed, because it is faster
                List<int> allowedDirectionCombinations = new List<int>();
                for (int ncDirectionCombination = 0; ncDirectionCombination < Math.Pow(2, transformAlignments.Length); ncDirectionCombination++)
                {
                    bool[] ncDirections = Enumerable.Range(0, transformAlignments.Length).Select(shift => ((ncDirectionCombination >> shift & 1) == 1)).ToArray();
                    if (!TestFusionDirections(structures, transformAlignments, ncDirections, expendableTowardsN, expendableTowardsC, minSubstructureLength))
                        continue;
                    allowedDirectionCombinations.Add(ncDirectionCombination);
                }

                if (allowedDirectionCombinations.Count == 0)
                    continue;

                Vector3 rotationCenter1 = transformAlignments[0].Centroid1;
                Vector3 rotationCenter2 = Vector3.Transform(transformAlignments.Last().Centroid2, transforms.Last());
                CoordinateSystem cs1 = new CoordinateSystem();
                CoordinateSystem cs2 = new CoordinateSystem(); cs2.ApplyTransform(transforms.Last());
                if (!realignment.GetLocalRealignmentWithTwoRotations(cs1, rotationCenter1, cs2, rotationCenter2, out CoordinateSystem realigned1, out CoordinateSystem realigned2, out float errorDegrees))
                    continue;

                if (errorDegrees > angleToleranceDegrees)
                    continue;

#if DEBUG
                //#if DEBUG && BROKEN
                Vector3 symmetrizedAxis = Vector3.Transform(VectorMath.GetRotationVector(VectorMath.GetLocalRotation(realigned1.Rotation, realigned2.Rotation)), realigned1.Rotation);
                Debug.Assert(Math.Abs(Vector3.Dot(realigned1.Translation, symmetrizedAxis) - Vector3.Dot(realigned2.Translation, symmetrizedAxis)) < 0.2);
#endif
                // Iterate possibilities for N->C and C->N of each alignment and determine whether the fusions are allowed
                // --each chain can be N or C-terminal only once
                // --fusion residues cannot be removed by a prior splice
                // --direction choice must not remove any self-interface (cyclic or other symmetry) residues
                foreach (int ncDirectionCombination in allowedDirectionCombinations)//= 0; ncDirectionCombination < Math.Pow(2, transformAlignments.Length); ncDirectionCombination++)
                {
                    bool[] ncDirections = Enumerable.Range(0, transformAlignments.Length).Select(shift => ((ncDirectionCombination >> shift & 1) == 1)).ToArray();
                    //if (!TestFusionDirections(structures, transformAlignments, ncDirections, expendableTowardsN, expendableTowardsC))
                    //    continue;

                    // Position all structures as per the alignment transforms and then reposition the first and last to make
                    // their axes meet the angle/intersect requirement
                    Structure[] copies = structures.Select(s => s.DeepCopy()).Cast<Structure>().ToArray();
                    Enumerable.Range(1, structures.Length - 2).ToList().ForEach(i => copies[i].Transform(transforms[i]));
                    copies[0].Transform(realigned1.Transform);
                    copies[structures.Length - 1].Transform(realigned2.Transform);

                    // Test clashes between each possible set of residues except for junctions against their participating structures
                    Selection[] fusionSelections;
                    Selection[] nonfusionSelections;
                    Selection[] removedSelections;
                    Fusion.GetSelections(copies, transformAlignments, ncDirections, out nonfusionSelections, out fusionSelections, out removedSelections);

                    Trace.Assert(fusionSelections.Length == nonfusionSelections.Length - 1);

//#if false 
                    // --junction vs junction
                    if (Clash.AnyContact(fusionSelections, Clash.ContactType.MainchainMainchainClash))
                        continue;

                    // --non-junction vs non-junction
                    if (Clash.AnyContact(nonfusionSelections, Clash.ContactType.MainchainMainchainClash))
                        continue;

                    // --junction vs non-participating
                    for (int i = 0; i < fusionSelections.Length; i++)
                    {
                        for (int j = 0; j < nonfusionSelections.Length; j++)
                        {
                            // skip comparison of junctions vs structures that make up the junction
                            if (i == j || i == j - 1)
                                continue;

                            if (Clash.AnyContact(new Selection[] { fusionSelections[i], nonfusionSelections[j] }, Clash.ContactType.MainchainMainchainClash))
                                goto ENDLOOP_NEXT_DIRECTION_COMBINATION;
                        }
                    }
//#endif

                    // Asu substructures don't clash, so now test collisions between ASUs and build up the full many-copy structure
                    Matrix globalRealignment;
                    IStructure asymmetricUnit = Fusion.GetStructure(copies, transformAlignments, ncDirections, true, out IChain partialChain);
                    realignment.GetGlobalRealignmentToZ(realigned1, realigned2, out globalRealignment);
                    asymmetricUnit.Transform(globalRealignment);

#if DEBUG && false
                    // DEBUG
                    // --pattern about each axis
                    AxisPattern<IStructure> patternAxis1 = new AxisPattern<IStructure>(realigned1, multiplicity1, asymmetricUnit);
                    PdbQuick.Save("pattern1.pdb", patternAxis1);

                    AxisPattern<IStructure> patternAxis2 = new AxisPattern<IStructure>(realigned2, multiplicity2, asymmetricUnit);
                    PdbQuick.Save("pattern2.pdb", patternAxis2);
#endif
                    // --pattern ASUs one at a time (to terminate early on failure) and test for clashes against the original ASU
                    IStructure placedUnit0 = symmetry.Pattern(axis1, 0, asymmetricUnit);
                    Structure structure = new Structure();
                    structure.AddArraySource(placedUnit0);
                    int partialChainIndex = partialChain != null ? asymmetricUnit.IndexOf(partialChain) : -1;
                    for (int copyIndex = 1; copyIndex < symmetry.GetCoordinateSystemsCount(axis1); copyIndex++)
                    {
                        IStructure copy = symmetry.Pattern(axis1, copyIndex, asymmetricUnit);
                        if(partialChain == null)
                        {
                            if (Clash.AnyContact(placedUnit0, copy, Clash.ContactType.MainchainMainchainClash))
                                //;
                            goto ENDLOOP_NEXT_DIRECTION_COMBINATION;
                        }
                        else
                        {
                            HashSet<IAa> ignoreClash = new HashSet<IAa>();
                            ignoreClash.UnionWith(placedUnit0[partialChainIndex][0, 3]);
                            ignoreClash.UnionWith(placedUnit0[partialChainIndex][partialChain.Count - 4, partialChain.Count - 1]);
                            ignoreClash.UnionWith(copy[partialChainIndex][0, 3]);
                            ignoreClash.UnionWith(copy[partialChainIndex][partialChain.Count - 4, partialChain.Count - 1]);
                            if (Clash.AnyContact(placedUnit0, copy, ignoreClash, Clash.ContactType.MainchainMainchainClash))
                                //;
                            goto ENDLOOP_NEXT_DIRECTION_COMBINATION;
                        }



                        structure.AddArraySource(copy);
                    }

                    Model output = new Model(symmetry, asymmetricUnit);


                    /*
                        public const string AaSelectionAsuInterface = "AaSelectionAsuInterface";
                        public const string AaSelectionAsuSubstructures = "AaSelectionAsuSubstructures";
                        public const string AaSelectionAsuSubstructureInterfaces = "AaSelectionAsuSubstructureInterfaces";
                        public const string AaSelectionAsuRemovedContacts = "AaSelectionAsuRemovedContacts";
                        public const string AaSelectionAsuDesignable = "AaSelectionAsuDesignable";
                     */

                    // Save selections that are relevant for downstream design
                    // --parts of structure that previously had contact with removed positions
                    {
                        Selection lostContacts = new Selection();
                        for (int i = 0; i < structures.Length; i++)
                        {
                            IAa[] remaining = nonfusionSelections[i].Aas.ToArray();
                            IAa[] removed = removedSelections[i].Aas.ToArray();
                            // The removed residues were never transformed - make sure that they are repositioned
                            // prior to checking for contact
                            foreach (IAa aa in removed)
                            {
                                aa.Transform(globalRealignment);
                            }

                            Selection affected = Clash.GetContactSelectionInFocusSet(remaining, removed);
                            lostContacts.UnionWith(affected);
                        }
                        output.Selections.Add(AaSelectionAsuRemovedContacts, lostContacts);
                    }

                    // --new inter-substructure contacts
                    {
                        Selection newContacts = new Selection();

                        // substructure block vs block
                        for (int i = 0; i < structures.Length; i++)
                        {
                            IAa[] substructure1 = nonfusionSelections[i].Aas.ToArray();
                            for (int j = i + 1; j < structures.Length; j++)
                            {
                                IAa[] substructure2 = nonfusionSelections[j].Aas.ToArray();
                                Selection affected = Clash.GetContactSelection(substructure1, substructure2, Clash.ContactType.Atomic | Clash.ContactType.VectorCACB | Clash.ContactType.IgnoreInvalidCoordinates);
                                newContacts.UnionWith(affected);
                            }
                        }

                        // fusion region vs block
                        for (int i = 0; i < fusionSelections.Length; i++)
                        {
                            IAa[] fusion = fusionSelections[i].Aas.ToArray();
                            for (int j = 0; j < nonfusionSelections.Length; j++)
                            {
                                IAa[] block = nonfusionSelections[j].Aas.ToArray();
                                Selection affected = Clash.GetContactSelection(fusion, block, Clash.ContactType.Atomic | Clash.ContactType.VectorCACB | Clash.ContactType.IgnoreInvalidCoordinates);
                                newContacts.UnionWith(affected);
                            }
                        }

                        output.Selections.Add(AaSelectionAsuSubstructureInterfaces, newContacts);
                    }

                    // --self-interface about either axis
                    {
                        Selection selectionInterface = Interface.GetCnInterAsuContacts(asymmetricUnit, multiplicity, Vector3.UnitZ);
                        //Selection selectionInterface2 = Interface.GetCnInterAsuContacts(asymmetricUnit, multiplicity2, axisRealignment.Axis2);
                        //output.Selections.Add(AaSelectionAsuInterface, Selection.Union(selectionInterface1, selectionInterface2));
                        output.Selections.Add(AaSelectionAsuInterface, selectionInterface);
                    }

                    // --final designable positions = substructure interfaces + lost contacts +/- self-interface, depending on whether the interface splits a single chain
                    {
                        Selection designable = new Selection();
                        designable.UnionWith(output.Selections[AaSelectionAsuSubstructureInterfaces]);
                        designable.UnionWith(output.Selections[AaSelectionAsuRemovedContacts]);
                        if(partialChain == null)
                            designable.ExceptWith(output.Selections[AaSelectionAsuInterface]);
                        else
                            designable.UnionWith(output.Selections[AaSelectionAsuInterface]);

                        
                        output.Selections.Add(AaSelectionAsuDesignable, designable);
                    }

                    

                    enqueue.TryAdd(output);
                    //spliceInfo.Peptide = splicedChain;
                    //spliceInfo.OriginalChains = new IChain[] { assembly[chainIndex1], spacer, assembly[chainIndex2] };
                    //spliceInfo.PdbOutputChains = fullAssembly.ToArray();
                    //spliceInfo.OriginalRanges = originalRanges;
                    //spliceInfo.IdentityRanges = finalRanges;
                    //spliceInfo.ImmutablePositions = interfaceResidues2.Select(index => index + (finalRanges[2].Start - originalRanges[2].Start)).Union(interfaceResidues1).OrderBy(val => val).ToArray();
                    //fusions.Add(spliceInfo);
                    // Jump from inner clash test loop
                    ENDLOOP_NEXT_DIRECTION_COMBINATION:;
                }
            }
        }
    }
}
