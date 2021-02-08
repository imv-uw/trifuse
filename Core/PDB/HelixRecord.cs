using System;
using System.Collections.Generic;

namespace NamespacePdb
{
    public class HelixRecord : Record
    {
        protected static List<FieldDefinition> HelixRecordFieldDefinitions = new List<FieldDefinition>();

        static HelixRecord()
        {
            HelixRecordFieldDefinitions.Add(new FieldDefinition("HELIX ",       1,  6,  typeof(String), true /* mandatory field */));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("serNum",       8,  10, typeof(Int32)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("helixID",      12, 14, typeof(String)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("initResName",  16, 18, typeof(String)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("initChainID",  20, 20, typeof(Char)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("initSeqNum",   22, 25, typeof(Int32), true /* mandatory field */));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("initICode",    26, 26, typeof(Char)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("endResName",   28, 30, typeof(String)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("endChainID",   32, 32, typeof(Char)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("endSeqNum",    34, 37, typeof(Int32), true /* mandatory field */));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("endICode",     38, 38, typeof(Char)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("helixClass",   39, 40, typeof(Int32)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("comment",      41, 70, typeof(String)));
            HelixRecordFieldDefinitions.Add(new FieldDefinition("length",       72, 76, typeof(Int32)));

        }

        public HelixRecord()
        {
            RecordType = RecordType.HELIX;
        }

        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                return HelixRecordFieldDefinitions;
            }
        }

        public Char InitChainId { get { return (Char)this["initChainID"]; } }
        public Char EndChainId { get { return (Char)this["endChainID"]; } }

        public int InitSeqNum { get { return (Int32)this["initSeqNum"]; } }
        public int EndSeqNum { get { return (Int32)this["endSeqNum"]; } }
    }
}
