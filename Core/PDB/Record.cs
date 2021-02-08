using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Reflection;

namespace NamespacePdb
{
    public enum RecordType
    {
        INVALID = 0,
        END,
        TER,
        ATOM,
        HETATM,
        HELIX,
        SEQRES,
        CONECT,
        MODEL,
        ENDMDL
    }

    public enum TextAlign
    {
        Left,
        Center,
        CenterLeft = Center,
        CenterRight,
        Right
    }

    public class FieldDefinition
    {
        public String Name;
        public int StartIndex;
        public int EndIndex;
        public Type DataType;
        public bool Mandatory;
        public TextAlign Alignment;

        public delegate string TextFormatter(string input);
        public TextFormatter Formatter = null;

        public FieldDefinition(String name, int startIdx, int endIndex, Type dataType, bool mandatory = false, TextAlign alignment = TextAlign.Left)
        {
            Name = name;
            StartIndex = startIdx;
            EndIndex = endIndex;
            DataType = dataType;
            Mandatory = mandatory;
            Alignment = alignment;
            Formatter = null;
        }

        public FieldDefinition(String name, int startIdx, int endIndex, Type dataType, TextFormatter formatter, bool mandatory = false)
        {
            Name = name;
            StartIndex = startIdx;
            EndIndex = endIndex;
            DataType = dataType;
            Mandatory = mandatory;
            Alignment = TextAlign.Left;
            Formatter = formatter;
        }

        public int Length { get { return EndIndex - StartIndex + 1; } }
    }

    public abstract class Record
    {
        static Dictionary<String, Type> RecordTypeDictionary = new Dictionary<String, Type>();

        public RecordType RecordType;
        public Dictionary<String, object> Values = new Dictionary<String, object>();
        
        // C# has a limitation where you can't require subclasses to implement static fields
        // through the use of the keyword 'abstract'.
        // I am getting around that by having the subclass instance return a static field.
        public abstract List<FieldDefinition> FieldDefinitions { get; }

        public String Text { get; protected set; }

        public Record()
        {
            Text = String.Empty;
        }

        public static Record GetRecord(string line)
        {
            Type type = GetType(line);
            if (type == null)
                return null;

            ConstructorInfo info = type.GetConstructor(Type.EmptyTypes);
            if (info != null)
            {
                Record record = (Record)info.Invoke(null);
                record.ParseRecord(line);
                return record;
            }
            return null;
        }

        public FieldDefinition GetField(string fieldName)
        {
            return FieldDefinitions.SingleOrDefault(field => field.Name.Equals(fieldName));
        }

        static Record()
        {
            // Set the list of known PDB fields. This would ideally include everything in the spec,
            // but types will be added as necessary.
            RecordTypeDictionary.Add("ATOM  ", typeof(AtomRecord));
            RecordTypeDictionary.Add("CONECT", typeof(ConectRecord));
            RecordTypeDictionary.Add("END   ", typeof(EndRecord));
            RecordTypeDictionary.Add("ENDMDL", typeof(EndmdlRecord));
            RecordTypeDictionary.Add("HELIX ", typeof(HelixRecord));
            RecordTypeDictionary.Add("HETATM", typeof(HetatmRecord));
            RecordTypeDictionary.Add("MODEL ", typeof(ModelRecord));
            RecordTypeDictionary.Add("SEQRES", typeof(SeqresRecord));
            RecordTypeDictionary.Add("TER   ", typeof(TerRecord));
            
            

            
        }

        static Type GetType(string record)
        {
            if(record.Length < 6)
                record = record.PadRight(6, ' ');

            // The indexes are 1-based (from pdb.org): Record Type is 1-6
            string substring = record.Substring(1 - 1 /* start index */, 6 /* length */);
            if (!RecordTypeDictionary.ContainsKey(substring))
            {
                return null;
            }
            return RecordTypeDictionary[substring];
        }

        bool ParseRecord(string line)
        {
            Text = line;

            foreach (FieldDefinition field in FieldDefinitions)
            {
                if (line.Length < field.StartIndex)
                {
                    if (field.Mandatory)
                        throw new Exception(String.Format("Required field \"{0}\" omitted in record: \"{1}\"", field.Name, line));
                    else
                        continue;
                }

                // StartIndex and EndIndex are 1-indexed in accordance with the PDB format
                string substring = line.Length < field.EndIndex ? line.Substring(field.StartIndex - 1) : line.Substring(field.StartIndex - 1, field.EndIndex - field.StartIndex + 1);
                switch (field.DataType.ToString())
                {
                    case "System.String":
                        Values[field.Name] = substring.Trim();
                        break;
                    case "System.Int32":
                        {
                            Int32 value = -1;
                            if(Int32.TryParse(substring, out value))
                                Values[field.Name] = value;
                        }
                        break;
                    case "System.Char":
                        {
                            char value = ' ';
                            if(Char.TryParse(substring, out value))
                                Values[field.Name] = value;
                        }
                        break;
                    case "System.float":
                    case "System.Single":
                        {
                            float value = -1F;
                            if(float.TryParse(substring, out value))
                                Values[field.Name] = value;
                            else if (field.Mandatory)
                                throw new Exception(String.Format("Error parsing required field \"{0}\", substring: \"{1}\", full line: \"{2}\"", field.Name, substring, line));
                        }
                        break;
                    default:
                        if (field.Mandatory)
                            throw new Exception(String.Format("Record contains an unknown field type: \"{0}\"", field.DataType.ToString()));
                        break;
                }
            }
            return true;
        }

        protected void AssignField(FieldDefinition field, string fieldValue)
        {
            Debug.Assert(fieldValue.Length <= field.Length);

            // Update the field value itself
            switch (field.DataType.ToString())
            {
                case "System.String":
                    Values[field.Name] = fieldValue;
                    break;
                case "System.Int32":
                    {
                        Int32 value = -1;
                        if (Int32.TryParse(fieldValue, out value))
                            Values[field.Name] = value;
                    }
                    break;
                case "System.Char":
                    {
                        char value = ' ';
                        if (Char.TryParse(fieldValue, out value))
                            Values[field.Name] = value;
                    }
                    break;
                case "System.float":
                case "System.Single":
                    {
                        float value = -1F;
                        if (float.TryParse(fieldValue, out value))
                            Values[field.Name] = value;
                        else if (field.Mandatory)
                            throw new Exception(String.Format("Error setting required field \"{0}\", value: \"{1}\"", field.Name, fieldValue));
                    }
                    break;
                default:
                    if (field.Mandatory)
                        throw new Exception(String.Format("Record contains an unknown field type: \"{0}\"", field.DataType.ToString()));
                    break;
            }

            // Update the record text
            if (Text.Length < field.EndIndex)
                Text = Text.PadRight(field.EndIndex);

            if(fieldValue.Length < field.Length)
            {
                    switch(field.Alignment)
                    {
                        case TextAlign.Left:
                            fieldValue = fieldValue.PadRight(field.Length);
                            break;
                        case TextAlign.Right:
                            fieldValue = fieldValue.PadLeft(field.Length);
                            break;
                        case TextAlign.Center:
                            int padLeft = (field.Length - fieldValue.Length) / 2;
                            fieldValue = fieldValue.PadLeft(fieldValue.Length + padLeft);
                            fieldValue = fieldValue.PadRight(field.Length);
                            break;
                        case TextAlign.CenterRight:
                            int padRight = (field.Length - fieldValue.Length) / 2;
                            fieldValue = fieldValue.PadRight(fieldValue.Length + padRight);
                            fieldValue = fieldValue.PadLeft(field.Length);
                            break;
                        default:
                            throw new Exception();
                    }
            }

            Text = Text.Substring(0, field.StartIndex - 1) + fieldValue.PadRight(field.Length) + Text.Substring(field.EndIndex, Text.Length - field.EndIndex);
        }

        public object this[string lookup]
        {
            get
            {
                return Values[lookup];
            }
            set
            {
                FieldDefinition field = FieldDefinitions.Single(f => f.Name == lookup);
                string valueString = value.ToString();
                if(valueString.Length > field.Length)
                {
                    valueString = valueString.Substring(0, field.Length);
                }
                AssignField(field, valueString);
            }
        }
    }
}
