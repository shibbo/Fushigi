using Fushigi.Bfres;
using Fushigi.Bfres.Texture;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class BfresTextureRender : GLTexture
    {
        public State TextureState = State.Loading;

        private Task<byte[]> Decoder;

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

            int[] mask = new int[4]
            {
                    GetSwizzle(texture.ChannelRed),
                    GetSwizzle(texture.ChannelGreen),
                    GetSwizzle(texture.ChannelBlue),
                    GetSwizzle(texture.ChannelAlpha),
            };
            _gl.TexParameter(Target, TextureParameterName.TextureSwizzleRgba, mask);

            for (int i = 0; i < texture.ArrayCount; i++)
            {
                //Load each surface
                var surface = texture.DeswizzleSurface(0, 0);
                var mipLevel = 0;
                var depthLevel = (uint)i;

                if (texture.IsAstc)
                {
                    Decoder = Task.Run(() =>
                    {
                        var decodedBuffer = texture.DecodeAstc(surface).ToArray();

                        TextureState = State.Decoded;

                        //This clears any resources.
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        return decodedBuffer;
                    });
                }
                else if (texture.IsBCNCompressed())
                {
                    var internalFormat = GLFormatHelper.ConvertCompressedFormat(texture.Format, true);
                    GLTextureDataLoader.LoadCompressedImage(_gl, this.Target, Width, Height, depthLevel, internalFormat, surface, mipLevel);

                    TextureState = State.Finished;
                }
                else
                {
                    var formatInfo = GLFormatHelper.ConvertPixelFormat(texture.Format);
                    GLTextureDataLoader.LoadImage(_gl, this.Target, Width, Height, depthLevel, formatInfo, surface, mipLevel);

                    TextureState = State.Finished;
                }
            }

            if (texture.MipCount > 1)
            {
                _gl.GenerateMipmap(this.Target);
            }
            this.Unbind();
        }

        static int GetSwizzle(ChannelType channel)
        {
            switch (channel)
            {
                case ChannelType.Red: return (int)GLEnum.Red;
                case ChannelType.Green: return (int)GLEnum.Green;
                case ChannelType.Blue: return (int)GLEnum.Blue;
                case ChannelType.Alpha: return (int)GLEnum.Alpha;
                case ChannelType.One: return (int)GLEnum.One;
                case ChannelType.Zero: return (int)GLEnum.Zero;
                default: return 0;
            }
        }

        public void CheckState()
        {
            if (TextureState == State.Decoded && Decoder.IsCompleted)
            {
                Bind();

                var formatInfo = GLFormatHelper.ConvertPixelFormat(SurfaceFormat.R8_G8_B8_A8_UNORM);
                GLTextureDataLoader.LoadImage(_gl, this.Target, Width, Height, 0, formatInfo, Decoder.Result, 0);

               // var internalFormat = GLFormatHelper.ConvertCompressedFormat(SurfaceFormat.BC7_UNORM, true);
             //   GLTextureDataLoader.LoadCompressedImage(_gl, this.Target, Width, Height, 0, internalFormat, Decoder.Result, 0);

                Unbind();

                TextureState = State.Finished;
            }
        }

        public enum State
        {
            Loading,
            Decoded,
            Finished,
        }
    }
}