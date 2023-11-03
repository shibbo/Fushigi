using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    internal class GLTexture2D : GLTexture
    {
        public GLTexture2D(GL gl) : base(gl)
        {
            Target = TextureTarget.Texture2D;
        }

        public static GLTexture2D Load(GL gl, string filePath)
        {
            GLTexture2D tex = new GLTexture2D(gl);
            tex.Load(filePath);
            return tex;
        }

        public void Load(string filePath)
        {
            byte[] buffer = File.ReadAllBytes(filePath);
            ImageResult image = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);

            this.Width = (uint)image.Width;
            this.Height = (uint)image.Height;

            this.InternalFormat = InternalFormat.Rgba;
            this.PixelFormat = Silk.NET.OpenGL.PixelFormat.Rgba;
            this.PixelType = Silk.NET.OpenGL.PixelType.UnsignedByte;

            LoadImage(image.Data);
        }

        public unsafe void LoadImage(byte[] image)
        {
            Bind();

            fixed (byte* ptr = image)
            {
                _gl.TexImage2D(Target, 0, InternalFormat, Width, Height, 0,
                    PixelFormat, PixelType, ptr);
            }

            _gl.TextureParameter(ID, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TextureParameter(ID, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            _gl.TextureParameter(ID, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TextureParameter(ID, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            _gl.GenerateMipmap(Target);

            Unbind();
        }

        public void GenerateMipmaps()
        {
            Bind();
            _gl.GenerateMipmap(Target);
        }
    }
}
