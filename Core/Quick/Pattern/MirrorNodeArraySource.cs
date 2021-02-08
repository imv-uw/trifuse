#define DEBUG_TEMPLATE_ADD_IN_PLACE

using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace Core.Quick.Pattern
{
    public class MirrorNodeArraySource<T> : MirrorArraySource<T>, INodeArraySource<T> where T : class, IMirror<T>, ITransformNode, ITransformable, IDeepCopy
    {
        INodeArraySource<T> _template;

        public T this[int index, bool placed] { set => _template[index, placed] = value; }

        protected MirrorNodeArraySource() { }

        public MirrorNodeArraySource(INodeArraySource<T> template)
            : base(template)
        {
            _template = template;
        }

        public MirrorNodeArraySource(INodeArraySource<T> template, bool root, bool childIsRoot, ITransformNode parent)
            : base(template, root, childIsRoot, parent)
        {
            _template = template;
        }

        public void AddInPlace(T item)
        {
#if DEBUG_TEMPLATE_ADD_IN_PLACE
            Matrix current = item.TotalTransform;
#endif
            Matrix mirrorTransform = (Matrix.Invert(_template.TotalTransform) * TotalTransform);
            Matrix fix = Matrix.Invert(mirrorTransform);

            item.Transform(fix);
            //_template.AddInPlace(item);

#if DEBUG_TEMPLATE_ADD_IN_PLACE
            Debug.Assert((item.TotalTransform - current * fix).Translation.Length() < 0.1);


            Matrix beforeAddInPlace = item.TotalTransform;
#endif
            _template.AddInPlace(item);

#if DEBUG_TEMPLATE_ADD_IN_PLACE
            Matrix afterAddInPlace = item.TotalTransform;
            Debug.Assert((beforeAddInPlace - afterAddInPlace).Translation.Length() < 0.1);
#endif

#if DEBUG
            // This is wrong because it's not the item whose transform should be correct, but the mirror item
            //Matrix final = item.TotalTransform * mirrorTransform;
            //Debug.Assert((final - current).Translation.Length() < 0.1);
#endif
        }

        public void AddRangeInPlace(IEnumerable<T> items)
        {
            Matrix inversion = Matrix.Invert(TotalTransform);
            foreach (T item in items)
            {
                item.Transform(inversion);
            }
            _template.AddRangeInPlace(items);
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return (MirrorArraySource<T>)clone;

            MirrorNodeArraySource<T> mirrorArraySource = new MirrorNodeArraySource<T>();
            graph.Add(this, mirrorArraySource);
            DeepCopyPopulateFields(graph, mirrorArraySource);
            return mirrorArraySource;
        }

        public override void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            MirrorNodeArraySource<T> mirrorArraySource = (MirrorNodeArraySource<T>)clone;
            mirrorArraySource._template = _template == null ? null : (INodeArraySource<T>)_template.DeepCopyFindOrCreate(graph);
            base.DeepCopyPopulateFields(graph, mirrorArraySource);
        }
    }
}
