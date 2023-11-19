using Fushigi.gl.Shaders;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class HDRScreenBuffer
    {
        public GLTexture2D GetOutput() => (GLTexture2D)Framebuffer.Attachments[0];

        private GLFramebuffer Framebuffer;

        private ScreenQuad ScreenQuad;

        public void Render(GL gl, int width, int height, GLTexture2D input)
        {
            if (Framebuffer == null)
                Framebuffer = new GLFramebuffer(gl, FramebufferTarget.Framebuffer, (uint)width, (uint)height, InternalFormat.Rgba);

            //Resize if needed
            if (Framebuffer.Width != (uint)width || Framebuffer.Height != (uint)height)
                Framebuffer.Resize((uint)width, (uint)height);

            Framebuffer.Bind();

            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, Framebuffer.Width, Framebuffer.Height);

            var shader = GLShaderCache.GetShader(gl, "PostEffect",
                Path.Combine("res", "shaders", "screen.vert"),
                Path.Combine("res", "shaders", "screen.frag"));

            shader.Use();
            shader.SetTexture("screenTexture", input, 1);

            if (ScreenQuad == null) ScreenQuad = new ScreenQuad(gl, 1f);

            ScreenQuad.Draw(shader);

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}
