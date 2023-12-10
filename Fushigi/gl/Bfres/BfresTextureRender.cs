using Fushigi.Bfres;
using Fushigi.Bfres.Texture;
using Fushigi.Byml.Serializer;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Fushigi.gl.Bfres
{
    public class BfresTextureRender : GLTexture
    {
        public State TextureState = State.Loading;

        public string Name;

        private Task<byte[]> Decoder;

        public bool IsSrgb = false;

        private uint ArrayCount = 1;

        public BfresTextureRender(GL gl, BntxTexture texture) : base(gl)
        {
            Load(texture);
        }


        public BfresTextureRender(GL gl) : base(gl)
        {
        }

        public void Load(BntxTexture texture)
        {
            this.Target = TextureTarget.Texture2D;
            this.IsSrgb = texture.IsSrgb;
            this.Name = texture.Name;

            if (texture.SurfaceDim == SurfaceDim.Dim2DArray)
                this.Target = TextureTarget.Texture2DArray;

            this.Width = texture.Width;
            this.Height = texture.Height;
            this.ArrayCount = texture.ArrayCount;
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

            void PushImageData(byte[] surface, int mipLevel, int depthLevel)
            {
                if (texture.IsBCNCompressed())
                {
                    var internalFormat = GLFormatHelper.ConvertCompressedFormat(texture.Format, true);
                    GLTextureDataLoader.LoadCompressedImage(_gl, this.Target, Width, Height, (uint)depthLevel, internalFormat, surface, mipLevel);

                    this.InternalFormat = internalFormat;
                }
                else
                {
                    var formatInfo = GLFormatHelper.ConvertPixelFormat(texture.Format);
                    GLTextureDataLoader.LoadImage(_gl, this.Target, Width, Height, (uint)depthLevel, formatInfo, surface, mipLevel);

                    this.InternalFormat = formatInfo.InternalFormat;
                    this.PixelType = formatInfo.Type;
                    this.PixelFormat = formatInfo.Format;
                }

                TextureState = State.Finished;
            }

            int numMips = 1;
            for (int mipLevel = 0; mipLevel < numMips; mipLevel++)
            {
                if (this.Target == TextureTarget.TextureCubeMap)
                {
                    for (int j = 0; j < texture.ArrayCount; j++)
                    {
                        var data = texture.DeswizzleSurface(j, mipLevel);
                        PushImageData(data, mipLevel, j);
                    }
                }
                else
                {
                    if (texture.IsAstc)
                    {
                        //Full data to hash check cache
                        var data = texture.TextureData.SelectMany(x => 
                                    x.ToArray()
                                    ).ToArray();

                        if (!BfresTextureCache.LoadCache(this, data, ArrayCount, 0))
                        {
                            Decoder = Task.Run(() =>
                            {
                                List<byte> levels = new List<byte>();
                                for (int j = 0; j < texture.ArrayCount; j++)
                                {
                                    var surface = texture.DeswizzleSurface(j, 0);
                                    var dec = texture.DecodeAstc(surface);

                                    //BC7 re encode for texture cache
                                    if (BfresTextureCache.Enable)
                                        dec = BCEncoder.Encode(dec.ToArray(), (int)this.Width, (int)this.Height);

                                    levels.AddRange(dec);
                                }
                                var decodedBuffer = levels.ToArray();

                                if (BfresTextureCache.Enable)
                                    BfresTextureCache.SaveCache(this, data, decodedBuffer);

                                TextureState = State.Decoded;

                                //This clears any resources.
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();

                                return decodedBuffer;
                            });
                        }
                    }
                    else
                    {
                        var surface = GetTextureBuffer(texture, mipLevel);
                        PushImageData(surface, mipLevel, (int)texture.ArrayCount);
                    }
                }
            }

            if (texture.MipCount > 1)
            {
                _gl.GenerateMipmap(this.Target);
            }
            this.Unbind();
        }

        private byte[] GetTextureBuffer(BntxTexture tex, int mipLevel)
        {
            //Combine all array levels into one single buffer
            if (tex.ArrayCount > 1)
            {
                List<byte> levels = new List<byte>();
                for (int j = 0; j < tex.ArrayCount; j++)
                {
                    var data = tex.DeswizzleSurface(j, mipLevel);
                    levels.AddRange(data);
                }
                return levels.ToArray();
            }
            else
                return tex.DeswizzleSurface(0, mipLevel);
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

        public void CheckState(bool useSrgb = true)
        {
            if (TextureState == State.Decoded && Decoder.IsCompleted && !this.IsDisposed)
            {
                Bind();

                // TODO - useSrgb argument is a temporary solution for the CourseSelect thumbnails
                //        should find a more permanent solution for this

                if (BfresTextureCache.Enable) //cache uses BC7
                {
                    var format = useSrgb && IsSrgb ? SurfaceFormat.BC7_SRGB : SurfaceFormat.BC7_UNORM;
                    var formatInfo = GLFormatHelper.ConvertCompressedFormat(format, true);
                    GLTextureDataLoader.LoadCompressedImage(_gl, this.Target, Width, Height, ArrayCount, formatInfo, Decoder.Result, 0);
                    this.InternalFormat = formatInfo;
                }
                else
                {
                    var format = useSrgb && IsSrgb ? SurfaceFormat.R8_G8_B8_A8_SRGB : SurfaceFormat.R8_G8_B8_A8_UNORM;

                    var formatInfo = GLFormatHelper.ConvertPixelFormat(format);
                    GLTextureDataLoader.LoadImage(_gl, this.Target, Width, Height, ArrayCount, formatInfo, Decoder.Result, 0);

                    this.InternalFormat = formatInfo.InternalFormat;
                    this.PixelType = formatInfo.Type;
                    this.PixelFormat = formatInfo.Format;
                }

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