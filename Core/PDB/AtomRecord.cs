using System;
using System.Collections.Generic;
using Core;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Core.Interfaces;

namespace NamespacePdb
{
    public class AtomRecord : Record
    {
        protected static List<FieldDefinition> AtomFieldDefinitions = new List<FieldDefinition>();

        static AtomRecord()
        {
            AtomFieldDefinitions.Add(new FieldDefinition("RECORD", 1, 6, typeof(String), true /* mandatory field */));
            AtomFieldDefinitions.Add(new FieldDefinition("serial", 7, 11, typeof(Int32), true));
            //AtomFieldDefinitions.Add(new FieldDefinition("name", 13, 16, typeof(String), FormatAlignAtomName, true));
            AtomFieldDefinitions.Add(new FieldDefinition("name", 13, 16, typeof(String), true, TextAlign.CenterRight));
            AtomFieldDefinitions.Add(new FieldDefinition("altLoc", 17, 17, typeof(Char)));
            AtomFieldDefinitions.Add(new FieldDefinition("resName", 18, 20, typeof(string), true /* mandatory field */, TextAlign.Right));
            AtomFieldDefinitions.Add(new FieldDefinition("chainID", 22, 22, typeof(Char), true /* mandatory field */));
            AtomFieldDefinitions.Add(new FieldDefinition("resSeq", 23, 26, typeof(Int32), true /* mandatory field */, TextAlign.Right));
            AtomFieldDefinitions.Add(new FieldDefinition("iCode", 27, 27, typeof(Char)));
            AtomFieldDefinitions.Add(new FieldDefinition("x", 31, 38, typeof(float), true /* mandatory field */, TextAlign.Right));
            AtomFieldDefinitions.Add(new FieldDefinition("y", 39, 46, typeof(float), true /* mandatory field */, TextAlign.Right));
            AtomFieldDefinitions.Add(new FieldDefinition("z", 47, 54, typeof(float), true /* mandatory field */, TextAlign.Right));
            AtomFieldDefinitions.Add(new FieldDefinition("occupancy", 55, 60, typeof(float)));
            AtomFieldDefinitions.Add(new FieldDefinition("tempFactor", 61, 66, typeof(float)));
            AtomFieldDefinitions.Add(new FieldDefinition("element", 77, 78, typeof(String), false /* mandatory field */));
            AtomFieldDefinitions.Add(new FieldDefinition("charge", 79, 80, typeof(String)));
        }

        // TODO - use it or lose it
        static string FormatAlignAtomName(string name)
        {
            if(string.IsNullOrEmpty(name))
                throw new ArgumentException("Atom name is null or empty");

            if(name.Length > 4)
                throw new ArgumentException("PDB atom name must be 4 characters at most - '{0}' is invalid", name);

            if(name.Length == 4)
                return name;

            if (name.StartsWith("FE"))
                return name.PadRight(4);

            if(name.Length == 3 && name.StartsWith("H"))
                return " " + name;
            
            return name;
        }

        public AtomRecord()
        {
            RecordType = RecordType.ATOM;
            if (this.GetType() == typeof(AtomRecord))
                this["RECORD"] = "ATOM";
        }
        
        public string Name { get { return (string)this["name"]; } set { this["name"] = value; } }
        //public int NumRes { get { return (Int32)this["numRes"]; } }
        public AtomRecord(IAtom atom, Char chainId, string residueName3, int residueSequenceNumber, int atomNumber)
        {
            foreach (FieldDefinition field in AtomFieldDefinitions)
            {
                string value = null;
                switch (field.Name)
                {
                    case "RECORD":
                        value = "ATOM  ";
                        break;
                    case "serial":
                        value = atomNumber.ToString();
                        break;
                    case "chainID":
                        value = chainId.ToString();
                        break;
                    case "resSeq":
                        value = residueSequenceNumber.ToString();
                        break;
                    case "resName":
                        value = residueName3;
                        break;
                    case "name":
                        // Chimera for some reason displays atoms with incorrect colors and doesn't connect
                        // them properly if spacing isn't as follows
                        // -if the overall atom name is length 4, the whole field is used and no other rules apply.
                        // -the first two characters are for the atom element, right-justified. Thus iron would
                        // be "FE  " and carbon would be " C  ".
                        value = atom.Name;
                        if (atom.Name.Length == 4)
                        {
                            value = atom.Name;
                        }
                        else if (atom.Element.ToString().Length == 2)
                        {
                            value = atom.Name.PadRight(4);
                        }
                        else
                        {
                            value = (" " + atom.Name).PadRight(4);
                        }
                        break;
                    case "x":
                        value = atom.Xyz.X.ToString("F3");
                        if (value.Length == 9 || value.Length == 10)
                            value = value.Substring(0, 8);
                        break;
                    case "y":
                        value = atom.Xyz.Y.ToString("F3");
                        if (value.Length == 9 || value.Length == 10)
                            value = value.Substring(0, 8);
                        break;
                    case "z":
                        value = atom.Xyz.Z.ToString("F3");
                        if (value.Length == 9 || value.Length == 10)
                            value = value.Substring(0, 8);
                        break;
                    case "element":
                        value = atom.Element.ToString();
                        break;
                    default:
                        break;
                }

                if (value != null)
                {
                    //string padded = value.PadRight(255).Substring(0, field.Length);
                    AssignField(field, value);
                }
                else
                {
                    Debug.Assert(field.Mandatory == false);
                }
            }
        }

        public AtomSource ToAtom()
        {
            AtomSource a = new AtomSource();
            a.Name = (string)Values["name"];
            a.X             = (float)Values["x"];
            a.Y             = (float)Values["y"];
            a.Z             = (float)Values["z"];
            a.ChainIndex    = (Char)Values["chainID"];
            a.ResidueName   = (String)Values["resName"];
            a.PdbResidueIndex = (int)Values["resSeq"];
            a.ResidueIndex  = (int)Values["resSeq"] - 1;
            a.Index         = (int)Values["serial"] - 1;
            a.PdbAltLoc     = (Char)Values["altLoc"];
            
            a.Element       = Element.Undefined;
            if (Values.ContainsKey("element"))
            {
                a.ElementName = (string)Values["element"];
                a.Element = GetElement((string)Values["element"]);
            }
            else if (!String.IsNullOrEmpty(a.Name))
                // PDBs generated by ilmm don't have 'element' fields, even though
                // they should.
                a.Element = GetElement(a.Name.Substring(0, 1));
                
            return a;
        }

        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                return AtomFieldDefinitions;
            }
        }

        public static Element GetElement(String name)
        {
            Element element = Element.Undefined;
            Enum.TryParse<Element>(name, /* ignore case */ true, out element);
            return element;
        }

        public float X
        {
            get
            {
                if (Values.ContainsKey("x"))
                    return (float)Values["x"];
                return float.NaN;
            }
            set
            {
                this["x"] = Math.Round(value, 3);
            }
        }

        public float Y
        {
            get
            {
                if (Values.ContainsKey("y"))
                    return (float)Values["y"];
                return float.NaN;
            }
            set
            {
                this["y"] = Math.Round(value, 3);
            }
        }

        public float Z
        {
            get
            {
                if (Values.ContainsKey("z"))
                    return (float) Values["z"];
                return float.NaN;
            }
            set
            {
                this["z"] = Math.Round(value, 3);
            }
        }

        public Vector3 XYZ
        {
            get
            {
                Vector3 value = new Vector3(X, Y, Z);
                return value;
            }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public int Serial
        {
            get
            {
                if (Values.ContainsKey("serial"))
                    return (int)Values["serial"];
                return -1;
            }
            set
            {
                this["serial"] = value;
            }
        }

        public string ResidueName
        {
            get
            {
                return (string) Values["resName"];
            }
            set
            {
                Trace.Assert(value.Length < 4);
                this["resName"] = value;
            }
        }

        public char ChainId
        {
            get
            {
                if (Values.ContainsKey("chainID"))
                    return (char) Values["chainID"];
                return ' ';
            }

            set
            {
                Values["chainID"] = value;
            }
        }

        public Element Element
        {
            get
            {
                if (Values.ContainsKey("element"))
                    return (Element) Enum.Parse(typeof(Element), (string) this["element"]);
                return Element.Undefined;
            }
            set
            {
                this["element"] = value.ToString();
            }
        }

        public int ResidueSequenceNumber
        {
            get
            {
                if (Values.ContainsKey("resSeq"))
                    return (int)this["resSeq"];
                return -1;
            }
            set
            {
                this["resSeq"] = value;
            }
        }
    }
}
