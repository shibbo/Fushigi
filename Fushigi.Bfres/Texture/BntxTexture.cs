using Ryujinx.Graphics.Texture.Astc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    public class BntxTexture : IResData
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SurfaceFormat Format
        {
            get { return Header.Format; }
            set { Header.Format = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public SurfaceDim SurfaceDim
        {
            get { return Header.TextureDim; }
            set { Header.TextureDim = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint Width
        {
            get { return Header.Width; }
            set { Header.Width = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint Height
        {
            get { return Header.Height; }
            set { Header.Height = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint Depth
        {
            get { return Header.Depth; }
            set { Header.Depth = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort MipCount
        {
            get { return Header.MipCount; }
            set { Header.MipCount = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint ArrayCount
        {
            get { return Header.ArrayCount; }
            set { Header.ArrayCount = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint Alignment
        {
            get { return Header.Alignment; }
            set { Header.Alignment = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public TileMode TileMode
        {
            get { return Header.TileMode; }
            set { Header.TileMode = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort Swizzle
        {
            get { return Header.Swizzle; }
            set { Header.Swizzle = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint BlockHeightLog2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<Memory<byte>> TextureData { get; set; } = new List<Memory<byte>>();

        /// <summary>
        /// 
        /// </summary>
        public ulong[] MipOffsets { get; set; } = new ulong[0];

        /// <summary>
        /// 
        /// </summary>
        public ChannelType ChannelRed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ChannelType ChannelGreen { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ChannelType ChannelBlue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ChannelType ChannelAlpha { get; set; }

        public bool IsAstc => this.Format.ToString().ToLower().Contains("astc");

        public bool IsSrgb => this.Format.ToString().ToLower().Contains("srgb");

        public bool IsBCNCompressed() => this.Format.ToString().StartsWith("BC");

        TextureHeader Header;

        public void Read(BinaryReader reader)
        {
            reader.BaseStream.Read(Utils.AsSpan(ref Header));

            Name = reader.ReadStringOffset(Header.NameOffset);

            MipOffsets = reader.ReadCustom(() => reader.ReadUInt64s((int)MipCount), Header.ImageDataTableOffset);

            ChannelRed = (ChannelType)((Header.ChannelSwizzle >> 0) & 0xff);
            ChannelGreen = (ChannelType)((Header.ChannelSwizzle >> 8) & 0xff);
            ChannelBlue = (ChannelType)((Header.ChannelSwizzle >> 16) & 0xff);
            ChannelAlpha = (ChannelType)((Header.ChannelSwizzle >> 24) & 0xff);

            BlockHeightLog2 = Header.TextureLayout1 & 7;

            int ArrayOffset = 0;
            for (int a = 0; a < Header.ArrayCount; a++)
            {
                reader.SeekBegin(ArrayOffset + (long)MipOffsets[0]);

                int size = (int)(Header.ImageSize / Header.ArrayCount);
                TextureData.Add(reader.ReadBytes(size));

                ArrayOffset += size;
            }
        }

        public byte[] DeswizzleSurface(int surface_level = 0, int mip_level = 0)
        {
            return TegraX1Swizzle.GetSurface(this, surface_level, mip_level);
        }

        public uint GetBlockWidth() => TegraX1Swizzle.GetBlockWidth(this.Format);
        public uint GetBlockHeight() => TegraX1Swizzle.GetBlockHeight(this.Format);
        public uint GetBytesPerPixel() => TegraX1Swizzle.GetBytesPerPixel(this.Format);

        public Span<byte> DecodeAstc(int array_level = 0, int mip_level = 0)
        {
            return DecodeAstc(this.DeswizzleSurface(array_level, mip_level));
        }

        public Span<byte> DecodeAstc(byte[] deswizzled)
        {
            AstcDecoder.TryDecodeToRgba8(
                deswizzled,
            (int)this.GetBlockWidth(),
            (int)this.GetBlockHeight(),
            (int)this.Width,
            (int)this.Height, 1, 1, 1, out Span<byte> decoded);

            return decoded;
        }
    }
}
