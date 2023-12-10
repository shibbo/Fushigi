using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class RenderStats
    {
        public static int NumDrawCalls;
        public static int NumTriangles;
        public static int NumShaders;
        public static int NumUniformBlocks;
        public static int NumTextures;

        public static void Reset()
        {
            NumDrawCalls = 0;
            NumTriangles = 0;
        }
    }
}
