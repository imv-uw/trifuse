using Microsoft.Xna.Framework;

namespace Core.Interfaces
{
    public interface ICoordinateFrame
    {
        /* get/set XyzLocal, get XyzInFrameOf(ancestor),  */
        Vector3 UnitX { get; }
        Vector3 UnitY { get; }
        Vector3 UnitZ { get; }
        Vector3 Origin { get; }
        void GetCoordinateSystem(out Vector3 origin, out Vector3 unitX, out Vector3 unitY, out Vector3 unitZ);
    }
}
