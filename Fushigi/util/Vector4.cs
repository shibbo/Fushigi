using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public static class Vector4Extension
    {
        public static Vector3 Xyz(this Vector4 v) {
            return new Vector3(v.X, v.Y, v.Z);
         }
    }
}
