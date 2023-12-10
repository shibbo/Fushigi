using Silk.NET.OpenGL;
using static Fushigi.gl.Bfres.GLFormatHelper;

namespace Fushigi.gl
{
    public class DDSFormatHelper
    {
        public static PixelFormatInfo ConvertPixelFormat(DDS.DXGI_FORMAT format)
        {
            if (!PixelFormatList.ContainsKey(format))
                return PixelFormatList[DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM];

            return PixelFormatList[format];
        }

        public static InternalFormat ConvertCompressedFormat(DDS.DXGI_FORMAT format, bool useSRGB)
        {
            return InternalFormatList[format];
        }

        public static uint CalculateImageSize(uint width, uint height, DDS.DXGI_FORMAT format)
        {
            var compFormat = ConvertCompressedFormat(format, true);
            return CalculateImageSize(width, height, compFormat);
        }

        public static uint CalculateImageSize(uint width, uint height, InternalFormat format)
        {
            if (format == InternalFormat.Rgba8)
                return width * height * 4;

            int blockSize = blockSizeByFormat[format];

            int imageSize = blockSize * (int)Math.Ceiling(width / 4.0) * (int)Math.Ceiling(height / 4.0);
            return (uint)imageSize;
        }

        static readonly Dictionary<DDS.DXGI_FORMAT, PixelFormatInfo> PixelFormatList = new Dictionary<DDS.DXGI_FORMAT, PixelFormatInfo>
        {
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R11G11B10_FLOAT, new PixelFormatInfo(InternalFormat.R11fG11fB10fExt, PixelFormat.Rgb, PixelType.UnsignedInt10f11f11fRev) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM, new PixelFormatInfo(InternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT, new PixelFormatInfo(InternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat) },

            { DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM, new PixelFormatInfo(InternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB, new PixelFormatInfo(InternalFormat.SrgbAlpha, PixelFormat.Rgba, PixelType.UnsignedByte) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT, new PixelFormatInfo(InternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R8_UNORM, new PixelFormatInfo(InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM, new PixelFormatInfo(InternalFormat.RG8, PixelFormat.RG, PixelType.UnsignedByte) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM, new PixelFormatInfo(InternalFormat.RG8SNorm, PixelFormat.RG, PixelType.Byte) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R16_UNORM, new PixelFormatInfo(InternalFormat.RG16f, PixelFormat.RG, PixelType.HalfFloat) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM, new PixelFormatInfo( InternalFormat.Rgb565, PixelFormat.Rgb, PixelType.UnsignedShort565Rev) },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_R9G9B9E5_SHAREDEXP, new PixelFormatInfo( InternalFormat.Rgb9E5, PixelFormat.Rgb, PixelType.UnsignedInt5999Rev) },
        };

        static readonly Dictionary<DDS.DXGI_FORMAT, InternalFormat> InternalFormatList = new Dictionary<DDS.DXGI_FORMAT, InternalFormat>
        {
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, InternalFormat.CompressedRgbaS3TCDxt1Ext },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB, InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, InternalFormat.CompressedRgbaS3TCDxt3Ext },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB, InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, InternalFormat.CompressedRgbaS3TCDxt5Ext },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB, InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM, InternalFormat.CompressedRedRgtc1 },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM, InternalFormat.CompressedSignedRedRgtc1 },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM, InternalFormat.CompressedRGRgtc2 },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM, InternalFormat.CompressedSignedRGRgtc2 },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16, InternalFormat.CompressedRgbBptcUnsignedFloat },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16, InternalFormat.CompressedRgbBptcSignedFloat },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM, InternalFormat.CompressedRgbaBptcUnorm },
            { DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB, InternalFormat.CompressedSrgbAlphaBptcUnorm },
        };

        static readonly Dictionary<InternalFormat, int> blockSizeByFormat = new Dictionary<InternalFormat, int>
        {
            //BC1 - BC3
            { InternalFormat.CompressedRgbaS3TCDxt1Ext, 8 },
            { InternalFormat.CompressedRgbaS3TCDxt3Ext, 16 },
            { InternalFormat.CompressedRgbaS3TCDxt5Ext, 16 },
            //BC1 - BC3 SRGB
            { InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext, 8 },
            { InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext, 16 },
            { InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext, 16 },
            //BC4
            { InternalFormat.CompressedRedRgtc1, 8 },
            { InternalFormat.CompressedSignedRedRgtc1, 8 },
            //BC5
            { InternalFormat.CompressedRGRgtc2, 8 },
            { InternalFormat.CompressedSignedRGRgtc2, 8 },
            //BC6
            { InternalFormat.CompressedRgbBptcUnsignedFloat, 16 },
            { InternalFormat.CompressedRgbBptcSignedFloat, 16 },
            //BC7
            { InternalFormat.CompressedRgbaBptcUnorm, 16 },
            { InternalFormat.CompressedSrgbAlphaBptcUnorm, 16 },
        };
    }
}
