using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class BoundingBox
    {
        public Matrix4x4 TranformMatrix { get; set; } = Matrix4x4.Identity;

        private Vector3 min;
        private Vector3 max;

        /// <summary>
        /// The minimum point in the box.
        /// </summary>
        public Vector3 Min
        {
            get { return Vector3.Transform(min, TranformMatrix); }
            set { min = value; }
        }

        /// <summary>
        /// The maximum point in the box.
        /// </summary>
        public Vector3 Max
        {
            get { return Vector3.Transform(max, TranformMatrix); }
            set { max = value; }
        }

        /// <summary>
        /// Gets the center of the bounding box.
        /// </summary>
        public Vector3 GetCenter()
        {
            return (Min + Max) * 0.5f;
        }

        /// <summary>
        /// Gets the extent of the bounding box.
        /// </summary>
        public Vector3 GetExtent()
        {
            return GetSize() * 0.5f;
        }

        /// <summary>
        /// Gets the size of the bounding box.
        /// </summary>
        public Vector3 GetSize()
        {
            return Max - Min;
        }

        public void Include(BoundingBox box)
        {
            this.min.X = MathF.Min(Min.X, box.Min.X);
            this.min.Y = MathF.Min(Min.Y, box.Min.Y);
            this.min.Z = MathF.Min(Min.Z, box.Min.Z);
            this.max.X = MathF.Max(Max.X, box.Max.X);
            this.max.Y = MathF.Max(Max.Y, box.Max.Y);
            this.max.Z = MathF.Max(Max.Z, box.Max.Z);
        }

        public void Transform(Matrix4x4 matrix)
        {
            TranformMatrix = matrix;
        }

        public void Set(Vector4[] vertices)
        {
            Vector3 max = new Vector3(float.MinValue);
            Vector3 min = new Vector3(float.MaxValue);
            for (int i = 0; i < vertices.Length; i++)
            {
                max.X = MathF.Max(max.X, vertices[i].X);
                max.Y = MathF.Max(max.Y, vertices[i].Y);
                max.Z = MathF.Max(max.Z, vertices[i].Z);
                min.X = MathF.Min(min.X, vertices[i].X);
                min.Y = MathF.Min(min.Y, vertices[i].Y);
                min.Z = MathF.Min(min.Z, vertices[i].Z);
            }
            Min = min;
            Max = max;
        }
    }
}
