using Silk.NET.OpenGL;

namespace Fushigi.gl.Textures
{
    public class GLTextureCubeArray : GLTexture
    {
        public GLTextureCubeArray(GL gl) : base(gl)
        {
            Target = TextureTarget.TextureCubeMapArray;
        }

        public static GLTextureCubeArray CreateEmpty(GL gl, uint width, uint height, uint depth,
            InternalFormat format = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            GLTextureCubeArray texture = new GLTextureCubeArray(gl);
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.Width = width;
            texture.Height = height;
            texture.Depth = depth;
            texture.InternalFormat = format;
            texture.Bind();

            for (uint j = 0; j < 1; j++)
            {
                uint mip_width = CalculateMipDimension(texture.Width, j);
                uint mip_height = CalculateMipDimension(texture.Height, j);

                unsafe
                {
                    gl.TexImage3D(texture.Target, (int)j, texture.InternalFormat, mip_width, mip_height, texture.Depth * 6, 0,
                        texture.PixelFormat, texture.PixelType, null);
                }
            }

            texture.MagFilter = TextureMagFilter.Linear;
            texture.MinFilter = TextureMinFilter.Linear;
            texture.UpdateParameters();

            texture.UpdateParameters();
            texture.Unbind();
            return texture;
        }
    }
}
