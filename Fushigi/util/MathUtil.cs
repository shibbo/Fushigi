using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    internal static class MathUtil
    {
        public const float Deg2Rad = (float)System.Math.PI / 180.0f;
        public const float Rad2Deg = 180.0f / (float)System.Math.PI;

        [Pure]
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a * (1 - t) + b * t;
        }

        public static int PolygonWindingNumber(Vector2 p, Span<Vector2> points)
        {
            static float isLeft(Vector2 p0, Vector2 p1, Vector2 point) =>
                ((p1.X - p0.X) * (point.Y - p0.Y) -
                (point.X - p0.X) * (p1.Y - p0.Y));

            int wn = 0;    // the  winding number counter

            // loop through all edges of the polygon
            for (int i = 0; i < points.Length; i++)
            {   // edge from V[i] to  V[i+1]
                if (points[i].Y <= p.Y)
                {          // start y <= P.y
                    if (points[(i + 1) % points.Length].Y > p.Y)      // an upward crossing
                    {
                        float l = isLeft(points[i], points[(i + 1) % points.Length], p);
                        if (l > 0)  // P left of  edge
                            ++wn;            // have  a valid up intersect
                        else if (l == 0) // boundary
                            return 0;
                    }
                }
                else
                {                        // start y > P.y (no test needed)
                    if (points[(i + 1) % points.Length].Y <= p.Y)     // a downward crossing
                    {
                        float l = isLeft(points[i], points[(i + 1) % points.Length], p);
                        if (l < 0)  // P right of  edge
                            --wn;            // have  a valid down intersect
                        else if (l == 0)
                            return 0;
                    }
                }
            }
            return wn;
        }
    }

    struct BoundingBox2D(Vector2 min, Vector2 max)
    {
        public readonly Vector2 Min => mMin;
        public readonly Vector2 Max => mMax;
        public static readonly BoundingBox2D Empty =
            new(new Vector2(float.PositiveInfinity), new Vector2(float.NegativeInfinity));

        public void Include(Vector2 point)
        {
            mMin.X = MathF.Min(point.X, mMin.X);
            mMin.Y = MathF.Min(point.Y, mMin.Y);

            mMax.X = MathF.Max(point.X, mMax.X);
            mMax.Y = MathF.Max(point.Y, mMax.Y);
        }

        public void Include(BoundingBox2D other)
        {
            Include(other.Min);
            Include(other.Max);
        }

        private Vector2 mMin = min, mMax = max;
    }

    struct BoundingBox3D(Vector3 min, Vector3 max)
    {
        public readonly Vector3 Min => mMin;
        public readonly Vector3 Max => mMax;
        public static readonly BoundingBox3D Empty =
            new(new Vector3(float.PositiveInfinity), new Vector3(float.NegativeInfinity));

        public void Include(Vector3 point)
        {
            mMin.X = MathF.Min(point.X, mMin.X);
            mMin.Y = MathF.Min(point.Y, mMin.Y);
            mMin.Z = MathF.Min(point.Z, mMin.Z);

            mMax.X = MathF.Max(point.X, mMax.X);
            mMax.Y = MathF.Max(point.Y, mMax.Y);
            mMax.Z = MathF.Max(point.Z, mMax.Z);
        }

        public void Include(BoundingBox3D other)
        {
            Include(other.Min);
            Include(other.Max);
        }

        private Vector3 mMin = min, mMax = max;
    }
}
