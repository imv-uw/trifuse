using Core.Interfaces;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Quick.Pattern
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MainchainMirrorAa : Aa
    {
        [JsonProperty]
        IAa _template;

        [JsonConstructor]
        MainchainMirrorAa() { }

        public MainchainMirrorAa(IAa template, int residueNumber)
            : this(template, residueNumber, false, false)
        {
        }

        public MainchainMirrorAa(IAa template, int residueNumber, bool nTerminus, bool cTerminus) 
            : base(residueNumber, nTerminus, cTerminus)
        {
            Trace.Assert(template != null);
            _template = template;
            base.Parent = template;
        }

        [JsonProperty]
        public override ITransformNode Parent { get; set; }

        //public bool MirrorFinalPlacement { get; set; } = true;

        public override Matrix NodeTransform { get => _template.NodeTransform; set => _template.NodeTransform = value; }

        public override Matrix TotalParentTransform => _template.TotalParentTransform;

        public override Matrix TotalTransform => _template.TotalTransform;

        public override IAa GetMirrorTemplate()
        {
            return _template.GetMirrorTemplate();
        }
        //public override Matrix TotalParentTransform => MirrorFinalPlacement? _template.TotalParentTransform : (Parent == null? Matrix.Identity : Parent.TotalTransform);

        //public override Matrix TotalTransform => MirrorFinalPlacement? _template.TotalTransform : NodeTransform * TotalParentTransform;
    }
}
