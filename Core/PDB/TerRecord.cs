using System.Collections.Generic;

namespace NamespacePdb
{
    public class TerRecord : Record
    {
        public static List<FieldDefinition> TerFields = new List<FieldDefinition>();

        public TerRecord ()
        {
            RecordType = RecordType.TER;
            Text = "TER   ";
        }
        public override List<FieldDefinition> FieldDefinitions
        {
            get
            {
                return TerFields;
            }
        }
    }
}
