using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class GLTexture : GLObject, IFramebufferAttachment
    {
        public TextureTarget Target { get; set; }

        public uint Width { get; set; }
        public uint Height { get; set; }

        public InternalFormat InternalFormat { get; internal set; }
        public PixelFormat PixelFormat { get; internal set; }
        public PixelType PixelType { get; internal set; }

        //Image parameters
        public TextureMinFilter MinFilter { get; set; }
        public TextureMagFilter MagFilter { get; set; }
        public TextureWrapMode WrapS { get; set; }
        public TextureWrapMode WrapT { get; set; }
        public TextureWrapMode WrapR { get; set; }

        internal GL _gl { get; private set; }

        public GLTexture(GL gl) : base(gl.GenTexture())
        {
            _gl = gl;

            Target = TextureTarget.Texture2D;
            InternalFormat = InternalFormat.Rgba;
            PixelFormat = Silk.NET.OpenGL.PixelFormat.Rgba;
            PixelType = Silk.NET.OpenGL.PixelType.UnsignedByte;
        }

        public void Bind()
        {
            _gl.BindTexture(Target, ID);
        }

        public void Unbind()
        {
            _gl.BindTexture(Target, 0);
        }

        public void Attach(FramebufferAttachment attachment, GLFramebuffer target)
        {
            target.Bind();
            _gl.FramebufferTexture(target.Target, attachment, ID, 0);
        }

        public void UpdateParameters()
        {
            _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)MagFilter);
            _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)MinFilter);
            _gl.TexParameter(Target, TextureParameterName.TextureWrapS, (int)WrapS);
            _gl.TexParameter(Target, TextureParameterName.TextureWrapT, (int)WrapT);
            _gl.TexParameter(Target, TextureParameterName.TextureWrapR, (int)WrapR);
        }

        public void Dispose()
        {
        }
    }
}
