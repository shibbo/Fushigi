using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Shaders
{
    public class GLShaderCache
    {
        static Dictionary<string, GLShader> Shaders = new Dictionary<string, GLShader>();

        public static GLShader GetShader(GL gl, string key, string vertPath, string fragPath)
        {
            if (Shaders.ContainsKey(key)) return Shaders[key];

            var shader = GLShader.FromFilePath(gl, vertPath, fragPath);
            Shaders.Add(key, shader);

            return shader;
        }

        public static void Dispose(string key)
        {
            if (Shaders.ContainsKey(key))
                Shaders[key]?.Dispose();
        }

        public static void DisposeAll()
        {
            foreach (var shader in Shaders)
                shader.Value?.Dispose();
        }
    }
}
