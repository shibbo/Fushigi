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
    
        /// <summary>
        /// Does a collision check between a convex polygon and a point
        /// </summary>
        /// <param name="polygon">Points of Polygon a in Clockwise orientation (in screen coordinates)</param>
        /// <param name="point">Point</param>
        /// <returns></returns>
        public static bool HitTestConvexPolygonPoint(ReadOnlySpan<Vector2> polygon, Vector2 point)
        {
            // separating axis theorem (lite)
            // we can view the point as a polygon with 0 sides and 1 point
            for (int i = 0; i < polygon.Length; i++)
            {
                var p1 = polygon[i];
                var p2 = polygon[(i + 1) % polygon.Length];
                var vec = (p2 - p1);
                var normal = new Vector2(vec.Y, -vec.X);

                (Vector2 origin, Vector2 normal) edge = (p1, normal);

                if (Vector2.Dot(point - edge.origin, edge.normal) >= 0)
                    return false;
            }

            //no separating axis found -> collision
            return true;
        }

        public static bool HitTestConvexQuad(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 point) 
        {
            return HitTestConvexPolygonPoint([p1, p2, p3, p4], point);
        }

        /// <summary>
        /// Does a collision check between a LineLoop and a point
        /// </summary>
        /// <param name="polygon">Points of a LineLoop</param>
        /// <param name="point">Point</param>
        /// <returns></returns>
        public static bool HitTestLineLoopPoint(ReadOnlySpan<Vector2> points, float thickness, Vector2 point)
        {
            for (int i = 0; i < points.Length; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Length];
                if (HitTestPointLine(point,
                    p1, p2, thickness))
                    return true;
            }

            return false;
        }

        static bool HitTestPointLine(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Math.Clamp(Vector2.Dot(pa, ba) /
                      Vector2.Dot(ba, ba), 0, 1);
            return (pa - ba * h).Length() < thickness / 2;
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
