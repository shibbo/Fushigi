using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    internal static class MathUtil
    {
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
}
