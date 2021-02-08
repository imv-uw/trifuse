using System;
using System.Collections.Generic;

namespace NamespacePdb
{
    public class HetatmRecord : AtomRecord
    {
        static List<FieldDefinition> HetatmFieldDefinitions = new List<FieldDefinition>();

        static HetatmRecord()
        {
            HetatmFieldDefinitions.Add(new FieldDefinition("RECORD", 1, 6, typeof(String), true /* mandatory field */));
        }

        public HetatmRecord()
        {
            RecordType = RecordType.HETATM;
            if (this.GetType() == typeof(HetatmRecord))
                this["RECORD"] = "HETATM";
        }
        
        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                List<FieldDefinition> fields = new List<FieldDefinition>(AtomFieldDefinitions);
                fields.RemoveAt(0);
                fields.InsertRange(0, HetatmFieldDefinitions);
                return fields;
            }
        }
    }
}
