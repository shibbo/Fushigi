using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class Renderbuffer : GLObject, IFramebufferAttachment
    {
        public uint Width { get; }

        public uint Height { get; }

        public InternalFormat InternalFormat { get; private set; }

        private GL _gl;

        public Renderbuffer(GL gl, uint width, uint height, InternalFormat internalFormat)
            : base(gl.GenRenderbuffer())
        {
            _gl = gl;
            Width = width;
            Height = height;
            InternalFormat = internalFormat;

            // Allocate storage for the renderbuffer.
            Bind();
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, internalFormat, width, height);
        }

        public Renderbuffer(GL gl, uint width, uint height, uint samples, InternalFormat internalFormat)
          : base(gl.GenRenderbuffer())
        {
            _gl = gl;
            Width = width;
            Height = height;
            InternalFormat = internalFormat;

            // Allocate storage for the renderbuffer.
            Bind();
            gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples,
                internalFormat, width, height);
        }

        public void Bind() {
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, ID);
        }

        public void Unbind() {
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        public void Attach(FramebufferAttachment attachment, GLFramebuffer target) {
            target.Bind();
            _gl.FramebufferRenderbuffer(target.Target, attachment, RenderbufferTarget.Renderbuffer, ID);
        }

        public void Dispose() {
            _gl.DeleteRenderbuffer(ID);
        }
    }
}
