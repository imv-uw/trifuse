using Newtonsoft.Json;

namespace Core.Quick
{
    public class AaDefinition
    {
        public const int IndexN = 0;
        public const int IndexCA = 1;
        public const int IndexC = 2;
        public const int IndexO = 3;
        //public const int IndexH = 4;

        [JsonProperty("name")]
        public readonly string Name;

        [JsonProperty("letter")]
        public readonly char Letter;

        [JsonProperty("atoms")]
        public readonly AtomDefinition[] Atoms;

        public AaDefinition() { }

        public AaDefinition(string name, char letter, AtomDefinition[] atoms)
        {
            Name = name;
            Letter = letter;
            Atoms = atoms;


        }
    }
}
