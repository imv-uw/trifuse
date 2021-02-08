using System;
using System.Collections.Generic;

namespace NamespacePdb
{
    public class ModelRecord : Record
    {
        static List<FieldDefinition> ModelFieldDefinitions = new List<FieldDefinition>();

        static public readonly string KeySerial = "serial";


        static ModelRecord()
        {
            ModelFieldDefinitions.Add(new FieldDefinition("RECORD",   1,   6, typeof(String), true /* mandatory field */));
            ModelFieldDefinitions.Add(new FieldDefinition("serial", 11,  14, typeof(Int32),  true /* mandatory field */));
        }

        public ModelRecord()
        {
            RecordType = RecordType.MODEL;
        }

        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                return ModelFieldDefinitions;
            }
        }

        public int Serial { get { return (int)Values[KeySerial]; } }
    }
}
