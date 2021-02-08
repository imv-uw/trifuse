using Microsoft.Xna.Framework;

namespace Core.Interfaces
{
    public interface ITransformNode : ITransformChild, ICoordinateFrame, IDeepCopy
    {
        bool IsMirror { get; }


        Matrix NodeTransform { get; set; }
        Matrix TotalParentTransform { get; }
        Matrix TotalTransform { get; }

        void DisconnectDependent(object dependent);
    }
}
