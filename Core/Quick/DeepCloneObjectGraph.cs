using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Quick
{
    public class DeepCopyObjectGraph : IDeepCloneObjectGraph
    {
        Dictionary<object, object> _map = new Dictionary<object, object>();

        public void Add(object source, object clone)
        {
            _map.Add(source, clone);
        }

        public bool TryGetClone(object source, out object clone)
        {
            return _map.TryGetValue(source, out clone);
        }
    }
}
