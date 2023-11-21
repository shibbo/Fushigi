using Fushigi.Bfres;
using Fushigi.util;
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
                LoadWonderShaders();

                //Load any custom archives here
                string folder = Path.Combine("ShaderCache", "Archives");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                foreach (var file in Directory.GetFiles(folder))
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

        static void LoadWonderShaders()
        {
            string wonder_shader = FileUtil.FindContentPath(Path.Combine("Shader", "SecredUber.Product.100.product.Nin_NX_NVN.bfsha.zs"));
            string wonder_sarc = FileUtil.FindContentPath(Path.Combine("Shader", "Secred.Nin_NX_NVN.release.sarc.zs"));

            if (File.Exists(wonder_shader))
                Shaders.Add(new BfshaFile(FileUtil.DecompressAsStream(wonder_shader)));

            if (File.Exists(wonder_sarc))
            {
                var sarc = new SARC.SARC(FileUtil.DecompressAsStream(wonder_sarc));
                if (sarc.FileExists("SecredUber.Nin_NX_NVN.bfsha"))
                {
                    var shader = new BfshaFile(new MemoryStream(sarc.OpenFile("SecredUber.Nin_NX_NVN.bfsha")));
                    Shaders.Add(shader);
                }
            }
        }
    }
}
