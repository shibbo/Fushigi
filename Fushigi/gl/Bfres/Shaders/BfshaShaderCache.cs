using Fushigi.Bfres;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class BfshaShaderCache
    {
        public static Dictionary<string, BfshaFile> Shaders = new Dictionary<string, BfshaFile>();

        //General shader cache for loading bfsha files from the ShaderCache/Archive folder
        public static BfshaFile GetShader(string name)
        {
            string file_path = Path.Combine("ShaderCache", "Archives", $"{name}.bfsha");

            if (Shaders.ContainsKey(name))
                return Shaders[name];
            else if (File.Exists(file_path))
            {
                var bfsha = new BfshaFile(file_path);
                Shaders.Add(name, bfsha);

                return Shaders[name];
            }

            return null;
        }
    }
}
