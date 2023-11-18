using Silk.NET.OpenGL;

namespace Fushigi.gl
{
    public class DDSTextureRender : GLTexture
    {
        public DDSTextureRender(GL gl, string filePath, TextureTarget target = TextureTarget.Texture2D) : base(gl)
        {
            this.Target = target;
            Load(new DDS(filePath));
        }

        public DDSTextureRender(GL gl, Stream stream, TextureTarget target = TextureTarget.Texture2D) : base(gl)
        {
            this.Target = target;
            Load(new DDS(stream));
        }

        public DDSTextureRender(GL gl, DDS texture, TextureTarget target = TextureTarget.Texture2D) : base(gl)
        {
            this.Target = target;
            Load(texture);
        }

        public void Load(DDS texture)
        {
            this.Width = texture.MainHeader.Width;
            this.Height = texture.MainHeader.Height;
            this.Bind();

            //Default to linear min/mag filters
            this.MagFilter = TextureMagFilter.Linear;
            this.MinFilter = TextureMinFilter.Linear;
            //Repeat by default
            this.WrapT = TextureWrapMode.Repeat;
            this.WrapR = TextureWrapMode.Repeat;
            this.UpdateParameters();

            uint numArrays = !texture.IsDX10 ? 1u : texture.Dx10Header.ArrayCount;

            for (int i = 0; i < numArrays; i++)
            {
                //Load each surface
                var surface = texture.GetSurface(i);
                var mipLevel = 0;
                var depthLevel = (uint)i;

                if (texture.IsBCNCompressed())
                {
                    var internalFormat = DDSFormatHelper.ConvertCompressedFormat(texture.Format, true);
                    GLTextureDataLoader.LoadCompressedImage(_gl, this.Target, Width, Height, depthLevel, internalFormat, surface, mipLevel);
                }
                else
                {
                    var formatInfo = DDSFormatHelper.ConvertPixelFormat(texture.Format);
                    GLTextureDataLoader.LoadImage(_gl, this.Target, Width, Height, depthLevel, formatInfo, surface, mipLevel);
                }
            }

            if (texture.MainHeader.MipCount > 1) {
                _gl.GenerateMipmap(this.Target);
            }
            this.Unbind();
        }
    }
}
