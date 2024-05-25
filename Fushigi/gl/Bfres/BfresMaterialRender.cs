using Fushigi.Bfres;
using Fushigi.course;
using Fushigi.gl.Shaders;
using Fushigi.util;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.gl.Bfres
{
    public class BfresMaterialRender
    {
        public GLShader Shader;

        public string Name {  get; set; }

        public GsysRenderState GsysRenderState = new GsysRenderState();

        public WonderGameShader GsysShaderRender = new WonderGameShader();

        private Material Material;

        public void Init(GL gl, BfresRender.BfresModel modelRender, BfresRender.BfresMesh meshRender, Shape shape, Material material) {
            Material = material;
            Name = material.Name;

            Shader = GLShaderCache.GetShader(gl, "Bfres",
               Path.Combine(AppContext.BaseDirectory, "res", "shaders", "Bfres.vert"),
               Path.Combine(AppContext.BaseDirectory, "res", "shaders", "Bfres.frag"));

            GsysRenderState.Init(material);
            GsysShaderRender.Init(gl, modelRender, meshRender, shape, material);
        }

        public void SetParam(string name, float value)
        {
            if (this.Material.ShaderParams.ContainsKey(name))
            {
                this.Material.ShaderParams[name].DataValue = value;
                GsysShaderRender.ReloadMaterialBlock();
            }
        }

        public void SetParam(string name, ShaderParam.TexSrt value)
        {
            if (this.Material.ShaderParams.ContainsKey(name))
            {
                this.Material.ShaderParams[name].DataValue = value;
                GsysShaderRender.ReloadMaterialBlock();
            }
        }

        public void SetTexture(string name, string sampler)
        {
            int index = this.Material.Samplers.Keys.ToList().IndexOf(sampler);
            if (index != -1)
                Material.Textures[index] = name;
        }

        public void RenderGameShaders(GL gl, BfresRender renderer, BfresRender.BfresModel model, System.Numerics.Matrix4x4 transform, Camera camera)
        {
            gl.Enable(EnableCap.TextureCubeMapSeamless);
            GsysShaderRender.Render(gl, renderer, model, transform, camera);
        }

        public void Render(GL gl, BfresRender renderer, BfresRender.BfresModel model, System.Numerics.Matrix4x4 transform, Camera camera)
        {
            if (UserSettings.UseGameShaders())
                this.RenderGameShaders(gl, renderer, model, transform, camera);
            else
                this.RenderDefault(gl, renderer, model, transform, camera);
        }

        public void RenderDefault(GL gl, BfresRender renderer, BfresRender.BfresModel model, System.Numerics.Matrix4x4 transform, Camera camera)
        {
            GsysRenderState.Render(gl);

            Shader.Use();
            Shader.SetUniform("mtxCam", camera.ViewProjectionMatrix);
            Shader.SetUniform("mtxMdl", transform);
            Shader.SetUniform("hasAlbedoMap", 0);
            Shader.SetUniform("hasNormalMap", 0);
            Shader.SetUniform("hasEmissionMap", 0);
            Shader.SetUniform("const_color0", Vector4.One);
            Shader.SetUniform("const_color1", Vector4.Zero);

            Shader.SetUniform("tile_id", 0);

            Shader.SetUniform("alpha_test", this.GsysRenderState.State.AlphaTest ? 1 : 0);
            Shader.SetUniform("alpha_ref", this.GsysRenderState.State.AlphaValue);
            Shader.SetUniform("alpha_test_func", (int)this.GsysRenderState.State.AlphaFunction);

            Vector3 dir = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0f, 0f, -1f), camera.ViewProjectionMatrixInverse));
            Shader.SetUniform("const_color0", dir);

            if (this.Material.ShaderParams.ContainsKey("const_color0"))
            {
                var color = (float[])this.Material.ShaderParams["const_color0"].DataValue;
                Shader.SetUniform("const_color0", new Vector4(color[0], color[1], color[2], color[3]));
            }
            if (this.Material.ShaderParams.ContainsKey("const_color1"))
            {
                var color = (float[])this.Material.ShaderParams["const_color1"].DataValue;
                Shader.SetUniform("const_color1", new Vector4(color[0], color[1], color[2], color[3]));
            }

            int unit_slot = 2;

            for (int i = 0; i < this.Material.Samplers.Count; i++)
            {
                var sampler = this.Material.Samplers.GetKey(i);
                var texName = this.Material.Textures[i];

                string sampler_usage = "";
                string uniform = "";

                switch (sampler)
                {
                    case "_a0":
                        sampler_usage = "hasAlbedoMap";
                        uniform = "albedo_texture";
                        break;
                    case "_n0":
                        sampler_usage = "hasNormalMap";
                        uniform = "normal_texture";
                        break;
                }

                if (!string.IsNullOrEmpty(uniform))
                {
                    var tex = TryBindTexture(gl, renderer, texName);
                    if (tex != null)
                    {
                    /*    if (tex.Target == TextureTarget.Texture2DArray)
                        {
                            sampler_usage += "Array"; //add array suffix used in shader
                            uniform += "_array";
                        }*/

                        Shader.SetUniform(sampler_usage, 1);
                        Shader.SetTexture(uniform, tex, unit_slot);
                        unit_slot++;
                    }
                }
            }
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.BindTexture(TextureTarget.Texture2DArray, 0);
        }

        private GLTexture TryBindTexture(GL gl, BfresRender renderer, string texName)
        {
            if (renderer.Textures.ContainsKey(texName))
            {
                var texture = renderer.Textures[texName];

                if (!(texture is BfresTextureRender))
                    return texture; //GL texture generated at runtime

                ((BfresTextureRender)texture).CheckState();
                if (((BfresTextureRender)texture).TextureState == BfresTextureRender.State.Finished)
                {
                    return texture;
                }

            }
            return GLImageCache.GetDefaultTexture(gl);
        }
    }
}
