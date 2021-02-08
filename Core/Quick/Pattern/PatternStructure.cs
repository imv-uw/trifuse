using Core.Collections;
using Core.Interfaces;
using Core.Symmetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Quick.Pattern
{
    public class PatternStructure : Pattern<IChain>, IStructure
    {
        INodeArraySource<IChain> _in;
        INodeArraySource<IChain> _out;

        public PatternStructure()
        {
        }

        public PatternStructure(SymmetryBuilder symmetry)
        {
            SetSymmetry(symmetry);
        }

        public override IArraySource<IChain> GetArraySourceInput()
        {
            if(_in == null)
                _in = new NodeArraySource<IChain>();
            return _in;
        }
        public override IArraySource<IChain> GetArraySourceExit()
        {
            if(_out == null)
                _out = new NodeArraySource<IChain>(this);
            return _out;
        }

        public IChain this[int index, bool placed] { set => _out[index, placed] = value; }

        public void AddInPlace(IChain item)
        {
            _in.AddInPlace(item);
        }

        public void AddRangeInPlace(IEnumerable<IChain> items)
        {
            _in.AddRangeInPlace(items);
        }

        public override object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return clone;

            PatternStructure structure = new PatternStructure();
            graph.Add(this, structure);
            DeepCopyPopulateFields(graph, structure);
            return structure;
        }

        public override void DeepCopyPopulateFields(IDeepCloneObjectGraph context, object clone)
        {
            PatternStructure pattern = (PatternStructure) clone;
            pattern._in = (INodeArraySource<IChain>) _in.DeepCopyFindOrCreate(context);
            pattern._out = (INodeArraySource<IChain>)_out.DeepCopyFindOrCreate(context);

            base.DeepCopyPopulateFields(context, clone);
        }

        public IStructure GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorStructure(this, root, parent);
        }

        public IStructure GetMirrorTemplate()
        {
            return this;
        }
    }
}
