using Fushigi.gl.Bfres;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class GLTextureDataLoader
    {
        public static void LoadCompressedImage(GL gl, TextureTarget target, uint width, uint height,
        uint depth, InternalFormat format, byte[] data, int mipLevel = 0)
        {
            switch (target)
            {
                case TextureTarget.Texture2D:
                    LoadCompressedImage2D(gl, mipLevel, width, height, format, data);
                    break;
                case TextureTarget.Texture2DArray:
                case TextureTarget.Texture3D:
                case TextureTarget.TextureCubeMapArray:
                    LoadCompressedImage3D(gl, target, mipLevel, depth, width, height, format, data);
                    break;
                case TextureTarget.TextureCubeMap:
                    LoadCompressedImageCubemap2D(gl, mipLevel, depth, width, height, format, data);
                    break;
            }
        }

        public static void LoadImage(GL gl, TextureTarget target, uint width, uint height,
           uint depth, GLFormatHelper.PixelFormatInfo format, byte[] data, int mipLevel = 0)
        {
            switch (target)
            {
                case TextureTarget.Texture2D:
                    LoadImage2D(gl, mipLevel, width, height, format, data);
                    break;
                case TextureTarget.Texture2DArray:
                case TextureTarget.Texture3D:
                case TextureTarget.TextureCubeMapArray:
                    LoadImage3D(gl, target, mipLevel, width, height, depth, format, data);
                    break;
                case TextureTarget.TextureCubeMap:
                    LoadImageCubemap2D(gl, mipLevel, depth, width, height, format, data);
                    break;
            }
        }

        static unsafe void LoadImage2D(GL gl, int mipLevel, uint width, uint height, GLFormatHelper.PixelFormatInfo formatInfo, byte[] data)
        {
            fixed (byte* ptr = data)
            {
                gl.TexImage2D(TextureTarget.Texture2D, mipLevel, formatInfo.InternalFormat, width, height, 0,
              formatInfo.Format, formatInfo.Type, ptr);
            }
        }

        static unsafe void LoadImage3D(GL gl, TextureTarget target, int mipLevel, uint width, uint height, uint depth, GLFormatHelper.PixelFormatInfo formatInfo, byte[] data)
        {
            fixed (byte* ptr = data)
            {
                gl.TexImage3D(target, mipLevel, formatInfo.InternalFormat, width, height, depth, 0,
                   formatInfo.Format, formatInfo.Type, ptr);
            }
        }

        static unsafe void LoadImageCubemap2D(GL gl, int mipLevel, uint array, uint width, uint height, GLFormatHelper.PixelFormatInfo formatInfo, byte[] data)
        {
            fixed (byte* ptr = data)
            {
                gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + (int)array, mipLevel, 
                    formatInfo.InternalFormat, width, height, 0, formatInfo.Format, formatInfo.Type, ptr);
            }
        }

        static unsafe void LoadCompressedImage2D(GL gl, int mipLevel, uint width, uint height, InternalFormat format, byte[] data)
        {
            uint imageSize = GLFormatHelper.CalculateImageSize(width, height, format);

            fixed (byte* ptr = data)
            {
                gl.CompressedTexImage2D(TextureTarget.Texture2D, mipLevel,
                             format, width, height, 0, imageSize, ptr);
            }
        }

        static unsafe void LoadCompressedImageCubemap2D(GL gl, int mipLevel, uint array, uint width, uint height, InternalFormat format, byte[] data)
        {
            uint imageSize = GLFormatHelper.CalculateImageSize(width, height, format);

            fixed (byte* ptr = data)
            {
                gl.CompressedTexImage2D(TextureTarget.TextureCubeMapPositiveX + (int)array, mipLevel,
                             format, width, height, 0, imageSize, ptr);
            }
        }
        
        static unsafe void LoadCompressedImage3D(GL gl, TextureTarget target, int mipLevel, uint depth, uint width, uint height, InternalFormat format, byte[] data)
        {
            uint imageSize = GLFormatHelper.CalculateImageSize(width, height, format);

            fixed (byte* ptr = data)
            {
                gl.CompressedTexImage3D(target, mipLevel,
                     format, width, height, depth, 0, imageSize * depth, ptr);
            }
        }
    }
}
