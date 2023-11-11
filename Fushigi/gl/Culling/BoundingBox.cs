using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class BoundingBox
    {
        /// <summary>
        /// The minimum point in the box.
        /// </summary>
        public Vector3 Min { get; set; }

        /// <summary>
        /// The maximum point in the box.
        /// </summary>
        public Vector3 Max { get; set; }

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
