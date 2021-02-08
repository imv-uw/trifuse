using System;
using System.Collections.Generic;

namespace NamespacePdb
{
    public class ConectRecord : Record
    {
        static List<FieldDefinition> ConectFieldDefinitions = new List<FieldDefinition>();

        static ConectRecord()
        {
            ConectFieldDefinitions.Add(new FieldDefinition("RECORD",  1,  6,  typeof(String), true /* mandatory field */));
            ConectFieldDefinitions.Add(new FieldDefinition("serial1", 7,  11, typeof(Int32)));
            ConectFieldDefinitions.Add(new FieldDefinition("serial2", 12, 16, typeof(Int32)));
            ConectFieldDefinitions.Add(new FieldDefinition("serial3", 17, 21, typeof(Int32)));
            ConectFieldDefinitions.Add(new FieldDefinition("serial4", 22, 26, typeof(Int32)));
            ConectFieldDefinitions.Add(new FieldDefinition("serial5", 27, 31, typeof(Int32)));
        }

        public ConectRecord()
        {
            RecordType = RecordType.CONECT;
        }

        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                return ConectFieldDefinitions;
            }
        }
    }
}
