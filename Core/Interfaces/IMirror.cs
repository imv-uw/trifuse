using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IMirror<T>
    {
        /// <summary>
        /// Create a mirror element, whose transform hierarchy ends at the given root
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        T GetMirroredElement(bool root, ITransformNode parent);
        /// <summary>
        /// Returns the underlying element being mirrored, if one exists, otherwise self
        /// </summary>
        /// <returns></returns>
        T GetMirrorTemplate();
    }
}
