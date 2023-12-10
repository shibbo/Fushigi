using Fushigi.Bfres;
using Fushigi.env;
using Fushigi.util;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class VRSkybox : IDisposable
    {
        const string ModelName = "VRModel";

        private Matrix4x4 Transform = Matrix4x4.Identity;

        //Resources
        public GLTexture SkyTexture => (GLTexture)RenderedSkyFbo.Attachments[0];

        private BfresRender BfresRender;

        private GLTexture2D TopTexture;
        private GLTexture2D LeftTexture;
        private GLTexture2D TopLeftTexture;
        private GLTexture2D TopRightTexture;

        private GLFramebuffer RenderedSkyFbo;

        public VRSkybox(GL gl, string file_name = $"VRModel")
        {
            Init(gl, file_name);
        }

        private void Init(GL gl, string file_name)
        {
            var file_path = FileUtil.FindContentPath(Path.Combine("Model", $"{file_name}.bfres.zs"));
            if (!File.Exists(file_path))
                return;

            BfresRender = new BfresRender(gl, FileUtil.DecompressAsStream(file_path));
            this.Transform = Matrix4x4.CreateScale(10000);

            TopTexture = GLTexture2D.CreateUncompressedTexture(gl, 128, 1);
            LeftTexture = GLTexture2D.CreateUncompressedTexture(gl, 128, 1);
            TopLeftTexture = GLTexture2D.CreateUncompressedTexture(gl, 128, 1);
            TopRightTexture = GLTexture2D.CreateUncompressedTexture(gl, 128, 1);

            BfresRender.Textures.Clear();
            BfresRender.Textures.Add("Top", TopTexture);
            BfresRender.Textures.Add("Left", LeftTexture);
            BfresRender.Textures.Add("TopLeft", TopLeftTexture);
            BfresRender.Textures.Add("TopRight", TopRightTexture);

            foreach (var mesh in BfresRender.Models.SelectMany(x => x.Value.Meshes))
            {
                mesh.MaterialRender.SetTexture("Top", "grad_t");
                mesh.MaterialRender.SetTexture("Left", "grad_l");
                mesh.MaterialRender.SetTexture("TopLeft", "grad_lt");
                mesh.MaterialRender.SetTexture("TopRight", "grad_rt");
            }
        }

        /// <summary>
        /// Sets the skybox using interpolated env palettes.
        /// This is used for transitions. 
        /// </summary>
        public void SetPaletteLerp(EnvPalette previous, EnvPalette next, float t)
        {
            if (next.Sky == null)
                return;

            var prevSky = previous.Sky;
            var nextSky = next.Sky;

            TopTexture.Load(64, 1, EnvPalette.EnvSkyLut.Lerp(prevSky.LutTexTop, nextSky.LutTexTop, t));
            LeftTexture.Load(64, 1, EnvPalette.EnvSkyLut.Lerp(prevSky.LutTexLeft, nextSky.LutTexLeft, t));
            TopLeftTexture.Load(64, 1, EnvPalette.EnvSkyLut.Lerp(prevSky.LutTexLeftTop, nextSky.LutTexLeftTop, t));
            TopRightTexture.Load(64, 1, EnvPalette.EnvSkyLut.Lerp(prevSky.LutTexRightTop, nextSky.LutTexRightTop, t));

            float horizontal_offset = MathUtil.Lerp(prevSky.HorizontalOffset, nextSky.HorizontalOffset, t);
            float rotDegLeftTop = MathUtil.Lerp(prevSky.RotDegLeftTop, nextSky.RotDegLeftTop, t);
            float rotDegRightTop = MathUtil.Lerp(prevSky.RotDegRightTop, nextSky.RotDegRightTop, t);

            SetMaterialParams(rotDegLeftTop, rotDegRightTop, horizontal_offset);
        }

        /// <summary>
        /// Sets the skybox using an env palette.
        /// </summary>
        public void SetPalette(EnvPalette palette)
        {
            if (palette.Sky == null)
                return;

            TopTexture.Load(64, 1, palette.Sky.LutTexTop.ComputeRgba8());
            LeftTexture.Load(64, 1, palette.Sky.LutTexLeft.ComputeRgba8());
            TopLeftTexture.Load(64, 1, palette.Sky.LutTexLeftTop.ComputeRgba8());
            TopRightTexture.Load(64, 1, palette.Sky.LutTexRightTop.ComputeRgba8());

            SetMaterialParams(palette.Sky.RotDegLeftTop, palette.Sky.RotDegRightTop, palette.Sky.HorizontalOffset);
        }

        //Sets the bfres material parameters
        private void SetMaterialParams(float rotTopLeft, float rotTopRight, float horizontal_offset)
        {
            var model = BfresRender.Models.Values.FirstOrDefault();
            if (model == null) return;

            foreach (var mesh in model.Meshes)
            {
                mesh.MaterialRender.SetParam("const_float2", 1f); //offset
                mesh.MaterialRender.SetParam("const_float3", 0f); //top rotation
                mesh.MaterialRender.SetParam("const_float4", rotTopLeft);
                mesh.MaterialRender.SetParam("const_float5", rotTopRight);
                mesh.MaterialRender.SetParam("const_float6", 0f); //another offset set to 0
                mesh.MaterialRender.SetParam("const_float7", horizontal_offset - 0.5f);
            }
        }

        /// <summary>
        /// Renders the sky gradient as a texture to be used in certain materials.
        /// </summary>
        public void RenderToTexture(GL gl)
        {
            if (RenderedSkyFbo == null)
                RenderedSkyFbo = new GLFramebuffer(gl, FramebufferTarget.Framebuffer, 1080, 1080);

            Camera cam = new Camera() { Width = RenderedSkyFbo.Width, Height = RenderedSkyFbo.Height, Distance = 10, Target = new Vector3(0, 0, 10) };
            cam.UpdateMatrices();

            RenderedSkyFbo.Bind();
            gl.Viewport(0, 0, RenderedSkyFbo.Width, RenderedSkyFbo.Height);
            gl.ClearColor(1, 0, 0, 1);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Render(gl, cam);

            RenderedSkyFbo.Unbind();
        }

        /// <summary>
        /// Draws the skybox in the scene.
        /// </summary>
        public void Render(GL gl, Camera camera)
        {
            BfresRender.Render(gl, Transform, camera);
        }

        /// <summary>
        /// Disposes the sky.
        /// </summary>
        public void Dispose()
        {
            this.BfresRender?.Dispose();
            this.SkyTexture?.Dispose();
            this.LeftTexture?.Dispose();
            this.TopRightTexture?.Dispose();
            this.TopLeftTexture?.Dispose();
            this.TopTexture?.Dispose();
            this.RenderedSkyFbo?.Dispose();
        }
    }
}
