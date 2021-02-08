using Core.Interfaces;
using Microsoft.Xna.Framework;
using NamespaceUtilities;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Quick.Pattern
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MirrorAa : MirrorArraySource<IAtom>, IAa
    {
        [JsonProperty]
        IAa _template;

        [JsonConstructor]
        MirrorAa() { }

        public MirrorAa(IAa template)
            : base(template)
        {
            _template = template;
        }

        public MirrorAa(IAa template, bool root, ITransformNode parent)
            : base(template, root, false, parent)
        {
            _template = template;
        }

        public IAtom this[string name] => this.Single(atom => atom.Name == name);

        public int ResidueNumber { get => _template.ResidueNumber; set => _template.ResidueNumber = value; }

        public char Letter => _template.Letter;

        public string Name => _template.Name;

        public bool IsNTerminus => _template.IsNTerminus;

        public bool IsCTerminus => _template.IsCTerminus;

        public bool IsMirror => true;

        public int IsNTerminusAsIndex => _template.IsNTerminusAsIndex;

        public int IsCTerminusAsIndex => _template.IsCTerminusAsIndex;

        public void AlignToNCAC(IAa other)
        {
            Vector3 N = other[Aa.N_].Xyz;
            Vector3 CA = other[Aa.CA_].Xyz;
            Vector3 C = other[Aa.C_].Xyz;
            AlignToNCAC(N, CA, C);
        }

        public void AlignToNCAC(Vector3 N, Vector3 CA, Vector3 C)
        {          
            Vector3 moveN = this[Aa.N_].Xyz;
            Vector3 moveCA = this[Aa.CA_].Xyz;
            Vector3 moveC = this[Aa.C_].Xyz;

            Matrix transform = VectorMath.GetRmsdAlignmentMatrix(new Vector3[] { moveN, moveCA, moveC }, false, new Vector3[] { N, CA, C}, false);
            base.Transform(transform);
        }

        public override object DeepCopy()
        {
            IDeepCloneObjectGraph context = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(context);
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph context)
        {
            if (context.TryGetClone(this, out object clone))
                return (IAa)clone;

            MirrorAa aa = new MirrorAa();
            context.Add(this, aa);
            DeepCopyPopulateFields(context, aa);
            return aa;
        }

        public override void DeepCopyPopulateFields(IDeepCloneObjectGraph context, object clone)
        {
            base.DeepCopyPopulateFields(context, clone);

            MirrorAa aa = (MirrorAa)clone;
            aa._template = _template == null? null : (IAa)_template.DeepCopyFindOrCreate(context);
        }

        public IAa GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorAa(this, root, parent);
        }

        public IAa GetMirrorTemplate()
        {
            return _template.GetMirrorTemplate();
        }
    }
}
