using Fushigi.Bfres;
using Fushigi.gl.Shaders;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class BfresMaterialRender
    {
        public GLShader Shader;

        private Material Material;

        public void Init(GL gl, Material material) {
            Material = material;

            Shader = GLShaderCache.GetShader(gl, "Bfres",
               Path.Combine("res", "shaders", "Bfres.vert"),
               Path.Combine("res", "shaders", "Bfres.frag"));
        }

        public void Render(GL gl, BfresRender renderer, Matrix4x4 transform, Matrix4x4 cameraMatrix)
        {
            Shader.Use();
            Shader.SetUniform("mtxCam", cameraMatrix);
            Shader.SetUniform("mtxMdl", transform);

            Shader.SetUniform("mtxMdl", transform);
            Shader.SetUniform("hasTexture", 0);

            int unit_slot = 1;

            for (int i = 0; i < this.Material.Samplers.Count; i++)
            {
                var sampler = this.Material.Samplers.GetKey(i);
                var texName = this.Material.Textures[i];

                switch (sampler)
                {
                    case "_a0":
                        Shader.SetUniform("hasTexture", 1);
                        var tex = TryBindTexture(renderer, texName);
                        Shader.SetTexture("image", tex, unit_slot);

                        unit_slot++;
                        break;
                }
            }
        }

        private GLTexture TryBindTexture(BfresRender renderer, string texName)
        {
            if (renderer.Textures.ContainsKey(texName))
            {
                var texture = renderer.Textures[texName];

                texture.Bind();

                return texture;
            }
            return null;
        }
    }
}
