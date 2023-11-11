using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public static class Matrix4x4Extension
    {
        public static Matrix4x4 Create(Vector4 c0, Vector4 c1, Vector4 c2, Vector4 c3)
        {
            return new Matrix4x4(
                c0.X, c0.Y, c0.Z, c0.W,
                c1.X, c1.Y, c1.Z, c1.W,
                c2.X, c2.Y, c2.Z, c2.W,
                c3.X, c3.Y, c3.Z, c3.W);
        }

        public static Vector4 Column0(this Matrix4x4 matrix) {
            return new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41);
        }

        public static Vector4 Column1(this Matrix4x4 matrix) {
            return new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42);
        }

        public static Vector4 Column2(this Matrix4x4 matrix) {
            return new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43);
        }

        public static Vector4 Column3(this Matrix4x4 matrix) {
            return new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44);
        }

        public static Vector4 Row0(this Matrix4x4 matrix) {
            return new Vector4(matrix.M11, matrix.M12, matrix.M13, matrix.M14);
        }

        public static Vector4 Row1(this Matrix4x4 matrix) {
            return new Vector4(matrix.M21, matrix.M22, matrix.M23, matrix.M24);
        }

        public static Vector4 Row2(this Matrix4x4 matrix) {
            return new Vector4(matrix.M31, matrix.M32, matrix.M33, matrix.M34);
        }
        public static Vector4 Row3(this Matrix4x4 matrix) {
            return new Vector4(matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }
    }
}
