using Fushigi.gl.Shaders;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class BasicMaterial
    {
        public GLShader Shader;

        public int TextureID = -1;

        public void Render(GL gl, Matrix4x4 matrix)
        {
            Shader = GLShaderCache.GetShader(gl, "Basic", 
                Path.Combine(AppContext.BaseDirectory, "res", "shaders", "Basic.vert"),
                Path.Combine(AppContext.BaseDirectory, "res", "shaders", "Basic.frag"));

            Shader.Use();
            Shader.SetUniform("mtxCam", matrix);

            Shader.SetUniform("hasTexture", TextureID != -1 ? 1 : 0);

            if (TextureID != -1)
            {
                gl.ActiveTexture(TextureUnit.Texture1);
                Shader.SetUniform("image", 1);
                gl.BindTexture(TextureTarget.Texture2D, (uint)TextureID);
            }
        }
    }
}
