using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

namespace Core
{

    public enum Terminus
    {
        [EnumMember(Value = "always")]
        Always,
        [EnumMember(Value = "nterm")]
        NTerminus,
        [EnumMember(Value = "!nterm")]
        NotNTerminus,
        [EnumMember(Value = "cterm")]
        CTerminus,
        [EnumMember(Value = "!cterm")]
        NotCTerminus
    }

    [Serializable]
    public class AtomDefinition
    {
        [JsonProperty("name")]
        public readonly string Name;
        [JsonProperty("mass")]
        public readonly float Mass;
        [JsonProperty("element")]
        public readonly Element Element;
        [JsonProperty("sidechain")]
        public readonly bool IsSidechain;

        [JsonConverter(typeof(StringEnumConverter)), JsonProperty("case")]
        public readonly Terminus TerminusCase;

        [JsonProperty("X")]
        public readonly float X;
        [JsonProperty("Y")]
        public readonly float Y;
        [JsonProperty("Z")]
        public readonly float Z;

        [JsonIgnore]
        public Vector3 Xyz { get { return new Vector3(X, Y, Z); } }

        public AtomDefinition() { }
    }


}
