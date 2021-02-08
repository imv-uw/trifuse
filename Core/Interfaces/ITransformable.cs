using Microsoft.Xna.Framework;

namespace Core.Interfaces
{
    public interface ITransformable : ITransformChild
    {
        /* Rotate, Transform, Translate in node context, i.e. prior to application of parent transforms */
        void RotateNode(Quaternion rotation, Vector3 origin);
        void TransformNode(Matrix transform);
        void TranslateNode(Vector3 translation);

        /* Rotate, Transform, Translate in global/final position context */
        void Rotate(Quaternion rotation, Vector3 origin);
        void Transform(Matrix transform);
        void Translate(Vector3 translation);
    }
}
