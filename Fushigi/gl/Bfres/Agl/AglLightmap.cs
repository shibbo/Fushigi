using Fushigi.env;
using Fushigi.gl.Shaders;
using Fushigi.gl.Textures;
using Fushigi.util;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class AglLightmap
    {
        public List<LightSource> Lights = new List<LightSource>();

        public bool IsSpecular = false;

        public float RimAngle = 1f;
        public float RimWidth = 1f;

        //Resources (fixed and only set once)
        static DDSTextureRender LUTTexture;
        static DDSTextureRender NormalsTexture;
        static GLFramebuffer Framebuffer;
        static ScreenQuad ScreenQuadRender;
        //Final output
        public GLTexture Output;

        public static void Init(GL gl)
        {
            DrawBufferMode[] buffers = new DrawBufferMode[6]
            {
                DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1,
                DrawBufferMode.ColorAttachment2, DrawBufferMode.ColorAttachment3,
                DrawBufferMode.ColorAttachment4, DrawBufferMode.ColorAttachment5,
            };

            Framebuffer = new GLFramebuffer(gl, FramebufferTarget.Framebuffer);
            Framebuffer.SetDrawBuffers(buffers);

            NormalsTexture = new DDSTextureRender(gl, Path.Combine(AppContext.BaseDirectory, "res", "bfres", "normals.dds"));
            LUTTexture = new DDSTextureRender(gl, Path.Combine(AppContext.BaseDirectory, "res", "bfres", "gradient.dds"));

            ScreenQuadRender = new ScreenQuad(gl, 1f);
        }

        public AglLightmap(GL gl, string name = "Lightmap")
        {
            Output = GLTextureCube.CreateEmpty(gl, 128, InternalFormat.Rgb32f);
            GLUtil.Label(gl, ObjectIdentifier.Texture, Output.ID, name);
        }

        public void Render(GL gl)
        {
            if (Framebuffer == null)
                Init(gl);

            RenderLevel(gl, Output, 0);
        }

        public void RenderLevel(GL gl, GLTexture output, int mip_level)
        {
            var size = output.Width / (uint)Math.Pow(2, mip_level);

            var shader = GLShaderCache.GetShader(gl, "Lightmap", 
                Path.Combine(AppContext.BaseDirectory, "res", "shaders", "Lightmap.vert"),
                Path.Combine(AppContext.BaseDirectory, "res", "shaders", "Lightmap.frag"));

            shader.Use();
            Framebuffer.Bind();
            gl.Viewport(0, 0, size, size);

            UpdateUniforms(shader);

            //Attach to each surface of the cube map to output
            for (int i = 0; i < 6; i++)
            {
                gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                   FramebufferAttachment.ColorAttachment0 + i,
                   TextureTarget.TextureCubeMapPositiveX + i, output.ID, mip_level);
            }

            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            ScreenQuadRender.Draw();

            gl.UseProgram(0);
            Framebuffer.Unbind();
        }

        public void Dispose() { Output?.Dispose(); }

        void UpdateUniforms(GLShader shader)
        {
            shader.SetTexture("uNormalTex", NormalsTexture, 1);
            shader.SetTexture("uLutTex", LUTTexture, 2);

            shader.SetUniform($"settings.rim_angle", RimAngle);
            shader.SetUniform($"settings.rim_width", RimWidth);
            shader.SetUniform($"settings.type", 0);
            shader.SetUniform($"settings.is_specular", IsSpecular ? 1 : 0);

            //Reset previous draw
            for (int i = 0; i < 6; i++)
            {
                shader.SetUniform($"lights[{i}].dir", new Vector3(0));
                shader.SetUniform($"lights[{i}].lowerColor", new Vector4(0));
                shader.SetUniform($"lights[{i}].upperColor", new Vector4(0));
                shader.SetUniform($"lights[{i}].lutIndex", 0);
            }
            //Set light sources
            for (int i = 0; i < this.Lights.Count; i++)
            {
                shader.SetUniform($"lights[{i}].dir", this.Lights[i].Direction);
                shader.SetUniform($"lights[{i}].lowerColor", this.Lights[i].LowerColor);
                shader.SetUniform($"lights[{i}].upperColor", this.Lights[i].UpperColor);
                shader.SetUniform($"lights[{i}].lutIndex", this.Lights[i].LutIndex);
            }
        }

        public class LightSource
        {
            public Vector3 Direction = new Vector3(0, 0, 0);

            public Vector4 LowerColor = new Vector4(0, 0, 0, 1);
            public Vector4 UpperColor = new Vector4(0, 0, 0, 1);

            //The index for the LUT texture (determines the y texture coordinate)
            public float LutIndex = 0.01563f;
        }
    }
}
