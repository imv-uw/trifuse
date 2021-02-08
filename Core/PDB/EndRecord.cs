using System.Collections.Generic;

namespace NamespacePdb
{
    class EndRecord : Record
    {
        public static List<FieldDefinition> EndFields = new List<FieldDefinition>();

        public EndRecord()
        {
            RecordType = RecordType.END;
        }

        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                return EndFields;
            }
        }
    }
}
