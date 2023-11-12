using Fushigi.util;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class BfresCache
    {
        public static Dictionary<string, BfresRender> Cache = new Dictionary<string, BfresRender>();

        public static BfresRender Load(GL gl, string projectName)
        {
            var path = FileUtil.FindContentPath(Path.Combine("Model", projectName + ".bfres.zs"));
            if (!File.Exists(path))
                return null;

            if (!Cache.ContainsKey(projectName))
                Cache.Add(projectName, new BfresRender(gl, new MemoryStream(FileUtil.DecompressFile(path))));

            return Cache[projectName];
        }
    }
}
