using Fushigi.Bfres;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    /// <summary>
    /// A cache for loading bfsha files.
    /// </summary>
    public class BfshaShaderCache
    {
        public static List<BfshaFile> Shaders = new List<BfshaFile>();

        //General shader cache for loading bfsha files from the ShaderCache/Archive folder
        public static BfshaFile GetShader(string name, string modelName)
        {
            //Init
            if (Shaders.Count == 0)
            {
                foreach (var file in Directory.GetFiles(Path.Combine("ShaderCache", "Archives")))
                {
                    var shader = new BfshaFile(file);
                    Shaders.Add(shader);
                }
            }

            foreach (var shader in Shaders)
            {
                foreach (var model in shader.ShaderModels)
                {
                    if (name == shader.Name && model.Key == modelName)
                        return shader;
                }
            }

            return null;
        }
    }
}
