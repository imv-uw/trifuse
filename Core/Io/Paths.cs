using System;
using System.Diagnostics;
using System.IO;

namespace Core.Database
{
    public static class Paths
    {
        public static readonly string Directory = "Database";

        public static class Json
        {
            public static readonly string Directory = Path.Combine(Paths.Directory, "Json");

            public static readonly string ResiduesPath = Path.Combine(Directory, "residues.json");
        }
        
        public static class Rotamer
        {
            public static readonly string Directory = Path.Combine(Paths.Directory, "Rotamer");
            public static readonly string JsonRotamerDefinitions = Path.Combine(Directory, "rotamer_definition.json");
            public static readonly string JsonRotamerInstances = Path.Combine(Directory, "rotamer_coordinates.json");
        }

        public static class Pdb
        {
            public static readonly string DirResidues = Path.Combine(Paths.Directory, "Residues");
        }
    }
}
