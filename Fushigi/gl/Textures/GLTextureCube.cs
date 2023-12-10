using Silk.NET.OpenGL;

namespace Fushigi.gl.Textures
{
    public class GLTextureCube : GLTexture
    {
        public GLTextureCube(GL gl) : base(gl)
        {
            Target = TextureTarget.TextureCubeMap;
        }

        public static GLTextureCube CreateEmpty(GL gl, uint size = 4,
            InternalFormat format = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            GLTextureCube texture = new GLTextureCube(gl);
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.Width = size; texture.Height = size;
            texture.Depth = 1;
            texture.InternalFormat = format;
            texture.Bind();

            for (uint i = 0; i < 6; i++)
            {
                for (uint j = 0; j < 1; j++)
                {
                    uint mip_width = CalculateMipDimension(texture.Width, j);
                    uint mip_height = CalculateMipDimension(texture.Height, j);

                    unsafe
                    {
                        gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + (int)i, (int)j,
                            texture.InternalFormat, mip_width, mip_height, 0,
                            texture.PixelFormat, texture.PixelType, null);
                    }
                }
            }

            texture.WrapS = TextureWrapMode.ClampToEdge;
            texture.WrapT = TextureWrapMode.ClampToEdge;
            texture.MagFilter = TextureMagFilter.Linear;
            texture.MinFilter = TextureMinFilter.Linear;
            texture.UpdateParameters();

            texture.Unbind();
            return texture;
        }
    }
}
