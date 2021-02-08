using Core.Interfaces;
using Microsoft.Xna.Framework;
using NamespaceUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;
using Core.Symmetry;

namespace Core.Quick.Pattern
{
    public class AxisPattern<T> : Pattern<T> where T : class, IMirror<T>, ITransformable, ITransformNode, IDeepCopy
    {
        CoordinateSystem[] _coordinateSystems;

        protected AxisPattern() { }

        public AxisPattern(LineTrackingCoordinateSystem axis, int multiplicity, T item = null, IEnumerable<int> skipIndices = null)
        {
            int[] skip = skipIndices == null ? new int[] { } : skipIndices.Distinct().ToArray();
            _coordinateSystems = new CoordinateSystem[multiplicity - skip.Length];

            for (int i = 0, coordinateSystemIndex = 0; i < multiplicity; i++)
            {
                if (skip.Contains(i))
                    continue;

                LineTrackingCoordinateSystem rotated = new LineTrackingCoordinateSystem(axis);
                rotated.RotateDegrees(axis.Origin, axis.Direction, i * 360 / multiplicity);
                _coordinateSystems[coordinateSystemIndex++] = rotated;
            }

            base.CoordinateSystems = _coordinateSystems;

            if (item != null)
            {
                base.Add(item);
            }
        }

        public AxisPattern(Vector3 axis, int multiplicity, T item = null, IEnumerable<int> skipIndices = null)
            : this(Vector3.Zero, axis, multiplicity, item, skipIndices)
        {
        }

        public AxisPattern(Vector3 origin, Vector3 axis, int multiplicity, T item = null, IEnumerable<int> skipIndices = null)
        {
            axis.Normalize();
            Trace.Assert(VectorMath.IsValid(axis));

            int[] skip = skipIndices == null ? new int[] { } : skipIndices.Distinct().ToArray();
            _coordinateSystems = new CoordinateSystem[multiplicity - skip.Length];

            for(int i = 0, coordinateSystemIndex = 0; i < multiplicity; i++)
            {
                if (skip.Contains(i))
                    continue;

                LineTrackingCoordinateSystem rotated = new LineTrackingCoordinateSystem(origin, axis);
                rotated.RotateDegrees(origin, axis, i * 360 / multiplicity);
                _coordinateSystems[coordinateSystemIndex++] = rotated;
            }

            base.CoordinateSystems = _coordinateSystems; 

            if (item != null)
            {
                base.Add(item);
            }
        }

        public override CoordinateSystem[] GetCoordinateSystems()
        {
            return _coordinateSystems;
        }

        public override object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(graph);
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            AxisPattern<T> pattern = new AxisPattern<T>();
            graph.Add(this, pattern);
            DeepCopyPopulateFields(graph, pattern);
            return pattern;
        }

        public override void DeepCopyPopulateFields(IDeepCloneObjectGraph context, object clone)
        {
            base.DeepCopyPopulateFields(context, clone);

            AxisPattern<T> pattern = (AxisPattern<T>)clone;
            pattern._coordinateSystems = (CoordinateSystem[])_coordinateSystems.Clone();
        }
    }
}
