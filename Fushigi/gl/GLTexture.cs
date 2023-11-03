using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    internal class GLTexture
    {
        public uint ID { get; private set; }

        public TextureTarget Target { get; set; }

        public uint Width { get; set; }
        public uint Height { get; set; }

        public InternalFormat InternalFormat { get; internal set; }
        public PixelFormat PixelFormat { get; internal set; }
        public PixelType PixelType { get; internal set; }

        internal GL _gl { get; private set; }

        public GLTexture(GL gl)
        {
            _gl = gl;
            ID = gl.GenTexture();

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
    }
}
