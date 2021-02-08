## Synopsis

This project implements a tripartite protein fusion method in C# .NET Core, originally developed and described [here](https://doi.org/10.1101/2020.07.18.210294). It can be used to generate protein nanoparticle atomic models that are then input to [Rosetta](https://www.rosettacommons.org/software/) for sidechain redesign at the fusion sites. The basic concept is to fuse two homo-oligomers to one another via an intermediate spacer repeat-protein.

## Motivation

Protein nanoparticles and lattices are of interest in medicine and nanotechnology. A longstanding  design approach uses genetic fusion to join protein homo-oligomer subunits [directly via α-helical linkers](https://doi.org/10.1073/pnas.041614998) to form more complex symmetric assemblies, but linker flexibility and few geometric solutions can be problematic. The tripartite fusion approach implemented here addresses these issues by significantly increasing the number of geometric solutions and filtering for additional inter-building-block contacts to improve rigidity.

## Build

1. Download the project files and open `Cpd.sln` with Visual Studio 2019 or later on Windows. 
2. Right click the solution file within the Solution Explorer and select "Restore NuGet Packages". The option might not be available until a build is attempted and fails on account of missing NuGet packages.
3. Build in Release mode for production use.

## Example command-line

1. Build in Visual Studio, Release mode
2. `cd <project_dir>\CmdCore\bin\Release`
3. `CmdCore.exe -cx_r_cx -arch D3 -axis1 C2X -axis2 C3 -regex_oligomer1 ".\Database\Scaffolds\Denovo\C2\*" -regex_oligomer2 ".\Database\Scaffolds\Denovo\C3\*" -regex_repeat ".\Database\Scaffolds\Denovo\repeats\saxs_and_crystal\*"`

This will generate dihedral (D3) atomic models from protein scaffolds (homo-oligomers and repeat-proteins) included among the project files under 'Database\Scaffolds\Denovo\'. Other architectures can be specified, for example two-fold dihedral (D2), tetrahedral (T) or icosahedral (I).

The models should then be redesigned with Rosetta at the positions specified by the corresponding resfiles. The [supplementary information](https://doi.org/10.1101/2020.07.18.210294) contains D2 and D3 symmetry definition (symdef) files and example Rosetta command-line. 

## Example program

One-off programs may be useful when more control is desired than what is provided by the command-line options. `Test.csproj` provides a single-threaded example for the tetrahedral architecture in which the fusion method is called directly. Direct method access allows control over the angular error tolerance, minimum substructure length, number of homo-oligomer interface residues permitted for deletion, and chain indexes searched.

## License

MIT License

## A few additional notes:

1. <b>The cyclic homo-oligomer scaffolds are expected in the form of Z-axis aligned asymmetric units !!</b>
2. The process runs multi-threaded on all available cores (Release mode) or single-threaded (Debug mode).
3. The outputs are filtered to ensure a degree of inter-subunit interaction, but further visual/manual filtering and post-Rosetta-design filtering by energy metrics are useful.
4. The output PDBs include residues named CYH, HIE, HID (indicating protonation state) which should be renamed prior to use in Rosetta. This can be done quickly:
<br/>
    `sed -i 's/CYH/CYS/' *pdb` <br/>
    `sed -i 's/HIE/HIS/' *pdb` <br/>
    `sed -i 's/HID/HIS/' *pdb` <br/>
 