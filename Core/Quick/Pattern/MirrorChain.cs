using Core.Interfaces;
using Core.Tools;
using Microsoft.Xna.Framework;
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
    public class MirrorChain : MirrorNodeArraySource<IAa>, IChain
    {
        [JsonProperty] IChain _template;

        [JsonConstructor]
        protected MirrorChain() { }

        public MirrorChain(IChain template, bool root, ITransformNode parent)
            : base(template, root, false, parent)
        {
            _template = template;
        }

        public IReadOnlyList<IAtom> Atoms => this.SelectMany(iaa => iaa).ToArray();

        public IChain GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorChain(this, root, parent);
        }

        public IChain GetMirrorTemplate()
        {
            return _template.GetMirrorTemplate();
        }

        public double GetPhiDegrees(int index)
        {
            return _template.GetPhiDegrees(index);
        }

        public double GetPhiRadians(int index)
        {
            return _template.GetPhiRadians(index);
        }

        public double GetPsiDegrees(int index)
        {
            return _template.GetPsiDegrees(index);
        }

        public double GetPsiRadians(int index)
        {
            return _template.GetPsiRadians(index);
        }

        public string GetSequence1()
        {
            return _template.GetSequence1();
        }

        public void Mutate(int index, int aa)
        {
            _template.Mutate(index, aa);
        }

        public void Mutate(int index, char aa)
        {
            _template.Mutate(index, aa);
        }

        public void Mutate(int index, string aa)
        {
            _template.Mutate(index, aa);
        }

        public void RemoveAt(int removeIndex, bool reposition)
        {
            _template.RemoveAt(removeIndex, true);
        }

        public void RotateRadians(Vector3 origin, Vector3 direction, double radians)
        {
            Quaternion rotation = Quaternion.CreateFromAxisAngle(direction, (float) radians);
            Matrix transform = MatrixUtil.GetRotation(ref rotation, ref origin);
            base.Transform(transform);
        }

        public override object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(graph);
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph context)
        {
            if (context.TryGetClone(this, out object clone))
                return (MirrorChain)clone;

            MirrorChain chain = new MirrorChain();
            context.Add(this, chain);
            DeepCopyPopulateFields(context, chain);
            return chain;
        }

        public override void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            base.DeepCopyPopulateFields(graph, clone);

            MirrorChain mirrorArraySource = (MirrorChain)clone;
            mirrorArraySource._template = _template == null ? null : (IChain)_template.DeepCopyFindOrCreate(graph);
        }
    }
}
