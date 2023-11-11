using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using Fushigi.util;

namespace Fushigi.gl
{
    public class FrustumHelper
    {
        public static Vector4[] ExtractFrustum(Matrix4x4 viewProjection, bool normalize = true)
        {
            //Todo verify as System.Numerics may require different order

            Vector4[] planes = new Vector4[6];
            //Left
            planes[0] = viewProjection.Column3() + viewProjection.Column0();
            //Right
            planes[1] = viewProjection.Column3() - viewProjection.Column0();
            //Up
            planes[2] = viewProjection.Column3() - viewProjection.Column1();
            //Down
            planes[3] = viewProjection.Column3() + viewProjection.Column1();
            //Near
            planes[4] = viewProjection.Column3() + viewProjection.Column2();
            //Far
            planes[5] = viewProjection.Column3() - viewProjection.Column2();

            if (normalize)
            {
                for (int i = 0; i < 6; i++)
                    planes[i] = Vector4.Normalize(planes[i]);
            }
            return planes;
        }

        public static Vector3[] GetFrustumCorners(Vector4[] frustum)
        {
            var points = new Vector3[8];

            points[0] = getFrustumCorner(frustum[4], frustum[3], frustum[0]);
            points[1] = getFrustumCorner(frustum[4], frustum[3], frustum[1]);
            points[2] = getFrustumCorner(frustum[4], frustum[2], frustum[0]);
            points[3] = getFrustumCorner(frustum[4], frustum[2], frustum[1]);
            points[4] = getFrustumCorner(frustum[5], frustum[3], frustum[0]);
            points[5] = getFrustumCorner(frustum[5], frustum[3], frustum[1]);
            points[6] = getFrustumCorner(frustum[5], frustum[2], frustum[0]);
            points[7] = getFrustumCorner(frustum[5], frustum[2], frustum[1]);

            return points;
        }

        //From https://github.com/larsjarlvik/larx/blob/48647b2a4b76daed34317cb3d0a67ce75fce7528/src/Frustum.cs#L8
        private static Vector3 getFrustumCorner(Vector4 f1, Vector4 f2, Vector4 f3)
        {
            var normals = Matrix4x4Extension.Create(f1, f2, f3, new Vector4(0, 0, 0, 1f));
            var det = normals.GetDeterminant();

            var v1 = Vector3.Cross(f2.Xyz(), f3.Xyz()) * -f1.W;
            var v2 = Vector3.Cross(f3.Xyz(), f1.Xyz()) * -f2.W;
            var v3 = Vector3.Cross(f1.Xyz(), f2.Xyz()) * -f3.W;

            return (v1 + v2 + v3) / det;
        }

        public static bool CubeInFrustum(Vector4[] f, Vector3 c, float s)
        {
            if (f == null) return true;

            for (var i = 0; i < 6; i++)
            {
                if (f[i].X * (c.X - s) + f[i].Y * (c.Y - s) + f[i].Z * (c.Z - s) + f[i].W > 0) continue;
                if (f[i].X * (c.X + s) + f[i].Y * (c.Y - s) + f[i].Z * (c.Z - s) + f[i].W > 0) continue;
                if (f[i].X * (c.X - s) + f[i].Y * (c.Y + s) + f[i].Z * (c.Z - s) + f[i].W > 0) continue;
                if (f[i].X * (c.X + s) + f[i].Y * (c.Y + s) + f[i].Z * (c.Z - s) + f[i].W > 0) continue;
                if (f[i].X * (c.X - s) + f[i].Y * (c.Y - s) + f[i].Z * (c.Z + s) + f[i].W > 0) continue;
                if (f[i].X * (c.X + s) + f[i].Y * (c.Y - s) + f[i].Z * (c.Z + s) + f[i].W > 0) continue;
                if (f[i].X * (c.X - s) + f[i].Y * (c.Y + s) + f[i].Z * (c.Z + s) + f[i].W > 0) continue;
                if (f[i].X * (c.X + s) + f[i].Y * (c.Y + s) + f[i].Z * (c.Z + s) + f[i].W > 0) continue;
                return false;
            }

            return true;
        }
    }
}
