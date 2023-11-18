using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class RenderStats
    {
        public static int NumDrawCalls;
        public static int NumTriangles;

        public static void Reset()
        {
            NumDrawCalls = 0;
            NumTriangles = 0;
        }
    }
}
