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

            uint numArrays = texture.ArrayCount;

            //Pass all data at once
            if (this.Target == TextureTarget.TextureCubeMapArray || 
                this.Target == TextureTarget.Texture2DArray ||
                this.Target == TextureTarget.Texture3D)
            {
                //Allocate mip data
                if (texture.MainHeader.MipCount > 1)
                    _gl.GenerateMipmap(Target);

                for (int j = 0; j < texture.MainHeader.MipCount; j++)
                {
                    var surface = texture.GetSurfaces(j);
                    var mipLevel = j;

                    var mipWidth = CalculateMipDimension(this.Width, (uint)j);
                    var mipHeight = CalculateMipDimension(this.Height, (uint)j);

                    if (texture.IsBCNCompressed())
                    {
                        var internalFormat = DDSFormatHelper.ConvertCompressedFormat(texture.Format, true);
                        GLTextureDataLoader.LoadCompressedImage(_gl, this.Target, mipWidth, mipHeight, numArrays, internalFormat, surface, mipLevel);
                    }
                    else
                    {
                        var formatInfo = DDSFormatHelper.ConvertPixelFormat(texture.Format);
                        GLTextureDataLoader.LoadImage(_gl, this.Target, mipWidth, mipHeight, numArrays, formatInfo, surface, mipLevel);
                    }
                }
            }
            else //insert slices of data
            {
                //Allocate mip data
                if (texture.MainHeader.MipCount > 1)
                    _gl.GenerateMipmap(Target);

                for (int i = 0; i < numArrays; i++)
                {
                    for (int j = 0; j < texture.MainHeader.MipCount; j++)
                    {
                        //Load each surface
                        var surface = texture.GetSurface(i, j);
                        var mipLevel = j;
                        var depthLevel = (uint)i;

                        var mipWidth = CalculateMipDimension(this.Width, (uint)j);
                        var mipHeight = CalculateMipDimension(this.Height, (uint)j);

                        if (texture.IsBCNCompressed())
                        {
                            var internalFormat = DDSFormatHelper.ConvertCompressedFormat(texture.Format, true);
                            GLTextureDataLoader.LoadCompressedImage(_gl, this.Target, mipWidth, mipHeight, depthLevel, internalFormat, surface, mipLevel);
                        }
                        else
                        {
                            var formatInfo = DDSFormatHelper.ConvertPixelFormat(texture.Format);
                            GLTextureDataLoader.LoadImage(_gl, this.Target, mipWidth, mipHeight, depthLevel, formatInfo, surface, mipLevel);
                        }
                    }
                }
            }

            this.Unbind();
        }
    }
}
