using System;
using System.Collections.Generic;

namespace NamespacePdb
{
    public class EndmdlRecord : Record
    {
        static List<FieldDefinition> ModelFieldDefinitions = new List<FieldDefinition>();

        static EndmdlRecord()
        {
            ModelFieldDefinitions.Add(new FieldDefinition("RECORD",   1,   6, typeof(String), true /* mandatory field */));
        }

        public EndmdlRecord()
        {
            RecordType = RecordType.ENDMDL;
        }

        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                return ModelFieldDefinitions;
            }
        }
    }
}
