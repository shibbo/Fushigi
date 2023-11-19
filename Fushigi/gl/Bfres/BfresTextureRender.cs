using Fushigi.Bfres;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class BfresTextureRender : GLTexture
    {
        public BfresTextureRender(GL gl, BntxTexture texture) : base(gl)
        {
            Load(texture);
        }

        public void Load(BntxTexture texture)
        {
            this.Target = TextureTarget.Texture2D;

            if (texture.SurfaceDim == SurfaceDim.Dim2DArray)
                this.Target = TextureTarget.Texture2DArray;

            this.Width = texture.Width;
            this.Height = texture.Height;
            this.Bind();

            //Default to linear min/mag filters
            this.MagFilter = TextureMagFilter.Linear;
            this.MinFilter = TextureMinFilter.Linear;
            //Repeat by default
            this.WrapT = TextureWrapMode.Repeat;
            this.WrapR = TextureWrapMode.Repeat;
            this.UpdateParameters();

            for (int i = 0; i < texture.ArrayCount; i++)
            {
                //Load each surface
                var surface = texture.DeswizzleSurface(0, 0);
                var mipLevel = 0;
                var depthLevel = (uint)i;

                if (texture.IsAstc)
                {
                    var formatInfo = GLFormatHelper.ConvertPixelFormat(SurfaceFormat.R8_G8_B8_A8_UNORM);
                    var data = texture.DecodeAstc(surface);
                    GLTextureDataLoader.LoadImage(_gl, this.Target, Width, Height, depthLevel, formatInfo, data.ToArray(), mipLevel);
                }
                else if (texture.IsBCNCompressed())
                {
                    var internalFormat = GLFormatHelper.ConvertCompressedFormat(texture.Format, true);
                    GLTextureDataLoader.LoadCompressedImage(_gl, this.Target, Width, Height, depthLevel, internalFormat, surface, mipLevel);
                }
                else
                {
                    var formatInfo = GLFormatHelper.ConvertPixelFormat(texture.Format);
                    GLTextureDataLoader.LoadImage(_gl, this.Target, Width, Height, depthLevel, formatInfo, surface, mipLevel);
                }
            }

            if (texture.MipCount > 1) {
                _gl.GenerateMipmap(this.Target);
            }
            this.Unbind();
        }
    }
}
