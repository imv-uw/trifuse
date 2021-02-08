using Core.Interfaces;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Core.Quick.Pattern
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MirrorStructure : MirrorArraySource<IChain>, IStructure
    {
        [JsonProperty]
        IStructure _template;

        [JsonConstructor]
        public MirrorStructure() { }

        public MirrorStructure(IStructure template)
            : base(template)
        {
            _template = template;
        }

        public MirrorStructure(IStructure template, bool root, ITransformNode parent)
            : base(template, root, false, parent)
        {
            _template = template;
        }

        public IChain this[int index, bool placed] { set => _template[index, placed] = value; }

        public IStructure GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorStructure(this, root, parent);
        }

        public IStructure GetMirrorTemplate()
        {
            return _template.GetMirrorTemplate();
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return (MirrorStructure)clone;

            MirrorStructure mirrorArraySource = new MirrorStructure();
            graph.Add(this, mirrorArraySource);
            DeepCopyPopulateFields(graph, mirrorArraySource);
            return mirrorArraySource;
        }

        public override void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            base.DeepCopyPopulateFields(graph, clone);

            MirrorStructure structure = (MirrorStructure)clone;
            structure._template = _template == null? null : (IStructure)_template.DeepCopyFindOrCreate(graph);
        }

        public void AddInPlace(IChain item)
        {
            _template.AddInPlace(item);
        }

        public void AddRangeInPlace(IEnumerable<IChain> items)
        {
            _template.AddRangeInPlace(items);
        }
    }
}
