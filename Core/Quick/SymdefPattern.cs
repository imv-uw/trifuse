using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;
using Core.Symmetry;

namespace Core.Quick
{
    public class SymdefPattern<T> : Pattern<T> where T : class, IMirror<T>, ITransformable, ITransformNode, IDeepCopy
    {
        SymmetryBuilder _symmetry;

        protected SymdefPattern() { }

        public SymdefPattern(string symmetry, string pattern, T item = null)
        {
            _symmetry = SymmetryBuilderFactory.CreateFromSymmetryName(symmetry);
            _symmetry.EnabledUnits = new string[] { pattern };

            OnCoordinateSystemsInitialized();

            if (item != null)
            {
                base.Add(item);
            }
        }

        public SymdefPattern(SymmetryBuilder builder, string pattern, T item = null)
        {
            _symmetry = SymmetryBuilderFactory.Clone(builder);
            _symmetry.EnabledUnits = new string[] { pattern };

            OnCoordinateSystemsInitialized();

            if (item != null)
            {
                base.Add(item);
            }
        }

        public override CoordinateSystem[] GetCoordinateSystems()
        {
            return _symmetry.GetCoordinateSystems(_symmetry.EnabledUnits.First());
        }

        public override object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopy(graph);
        }

        public override object DeepCopy(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            SymdefPattern<T> pattern = new SymdefPattern<T>();
            graph.Add(this, pattern);
            DeepCopy(graph, pattern);
            return pattern;
        }

        public override void DeepCopy(IDeepCloneObjectGraph graph, object clone)
        {
            base.DeepCopy(graph, clone);

            SymdefPattern<T> pattern = (SymdefPattern<T>)clone;
            pattern._symmetry = _symmetry == null ? null : (SymmetryBuilder) _symmetry.DeepCopy(graph);
        }
    }
}
