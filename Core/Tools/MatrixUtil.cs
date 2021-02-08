using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Core.Tools
{
    static class MatrixUtil
    {
        public static Matrix GetRotation(ref Quaternion rotation, ref Vector3 origin)
        {
            Matrix result = Matrix.Identity;
            result.Translation -= origin;
            result = Matrix.Multiply(result, Matrix.CreateFromQuaternion(rotation));
            result.Translation += origin;
            return result;
        }
    }
}
