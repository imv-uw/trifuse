using System;
using System.Collections.Generic;

namespace NamespacePdb
{
    public class SeqresRecord : Record
    {
        protected static List<FieldDefinition> SeqresFieldDefinitions = new List<FieldDefinition>();

        static SeqresRecord()
        {
            SeqresFieldDefinitions.Add(new FieldDefinition("SEQRES",    1,  6,  typeof(String), true /* mandatory field */));
            SeqresFieldDefinitions.Add(new FieldDefinition("serNum",    9,  10, typeof(Int32)));
            SeqresFieldDefinitions.Add(new FieldDefinition("chainID",   12, 12, typeof(Char)));
            SeqresFieldDefinitions.Add(new FieldDefinition("numRes",    14, 17, typeof(Int32), true /* mandatory field */));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName1",  20, 22, typeof(string), true /* mandatory field */, TextAlign.Right));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName2",  24, 26, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName3",  28, 30, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName4",  32, 34, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName5",  36, 38, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName6",  40, 42, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName7",  44, 46, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName8",  48, 50, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName9",  52, 54, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName10", 56, 58, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName11", 60, 62, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName12", 64, 66, typeof(string)));
            SeqresFieldDefinitions.Add(new FieldDefinition("resName13", 68, 70, typeof(string)));
        }

        public SeqresRecord()
        {
            RecordType = RecordType.SEQRES;
        }

        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                return SeqresFieldDefinitions;
            }
        }

        public Char ChainId { get { return (Char)this["chainID"]; } }
        public int NumRes { get { return (Int32)this["numRes"]; } }
    }
}
