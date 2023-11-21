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
        public uint Depth { get; set; }

        public InternalFormat InternalFormat { get; internal set; }
        public PixelFormat PixelFormat { get; internal set; }
        public PixelType PixelType { get; internal set; }

        //Image parameters
        public TextureMinFilter MinFilter { get; set; }
        public TextureMagFilter MagFilter { get; set; }
        public TextureWrapMode WrapS { get; set; }
        public TextureWrapMode WrapT { get; set; }
        public TextureWrapMode WrapR { get; set; }

        //
        public List<GLTexture> SubTextures = new List<GLTexture>();

        internal GL _gl { get; private set; }

        public bool IsDisposed = false;

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


        public unsafe static GLTexture ToCopy(GL gl, GLTexture texture, TextureTarget target)
        {
            //Src
            byte[] buffer = new byte[texture.Width * texture.Height * 4];

            var dest_format = InternalFormat.Rgba;

            texture.Bind();
            gl.GetTexImage(texture.Target, 0, texture.PixelFormat, texture.PixelType, buffer.AsSpan());

            //Use srgb if enabled
            if (texture.InternalFormat == InternalFormat.SrgbAlpha)
                dest_format = InternalFormat.SrgbAlpha;


            GLTexture tex = new GLTexture(gl);
            tex.Target = target;
            tex.Width = texture.Width;
            tex.Height = texture.Height;
            tex.MinFilter = texture.MinFilter;
            tex.MagFilter = texture.MagFilter;
            tex.InternalFormat = dest_format;
            tex.PixelFormat = PixelFormat.Rgba;
            tex.PixelType = PixelType.UnsignedByte;

            //Dst
            tex.Bind();

            //Copy
            fixed (byte* ptr = buffer)
            {
                gl.TexImage3D(tex.Target, 0, tex.InternalFormat, tex.Width, tex.Height, 1, 0,
                     tex.PixelFormat, tex.PixelType, ptr);
            }

            gl.GenerateMipmap(tex.Target);

            //Check for errors
            var error = gl.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine($"OpenGL Error: {error}");
               // throw new Exception();
            }

            //unbind
            tex.Unbind();

            return tex;
        }

        public void Dispose()
        {
            IsDisposed = true;
            _gl.DeleteTexture(ID);
        }

        internal static uint CalculateMipDimension(uint baseLevelDimension, uint mipLevel)
        {
            return baseLevelDimension / (uint)Math.Pow(2, mipLevel);
        }
    }
}
