using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fushigi.Bfres;
using Fushigi.course;
using Silk.NET.OpenGL;

namespace Fushigi.gl.Bfres
{
    public class GLFormatHelper
    {
        public static PixelFormatInfo ConvertPixelFormat(SurfaceFormat format)
        {
            if (!PixelFormatList.ContainsKey(format))
                return PixelFormatList[SurfaceFormat.R8_G8_B8_A8_UNORM];

            return PixelFormatList[format];
        }

        public static InternalFormat ConvertCompressedFormat(SurfaceFormat format, bool useSRGB)
        {
            return InternalFormatList[format];
        }

        public static uint CalculateImageSize(uint width, uint height, InternalFormat format)
        {
            if (format == InternalFormat.Rgba8)
                return width * height * 4;

            int blockSize = blockSizeByFormat[format];

            int imageSize = blockSize * (int)Math.Ceiling(width / 4.0) * (int)Math.Ceiling(height / 4.0);
            return (uint)imageSize;
        }

        static readonly Dictionary<SurfaceFormat, PixelFormatInfo> PixelFormatList = new Dictionary<SurfaceFormat, PixelFormatInfo>
        {
            { SurfaceFormat.R11_G11_B10_UNORM, new PixelFormatInfo(InternalFormat.R11fG11fB10fExt, PixelFormat.Rgb, PixelType.UnsignedInt10f11f11fRev) },
            { SurfaceFormat.R16_G16_B16_A16_UNORM, new PixelFormatInfo(InternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat) },

            { SurfaceFormat.R8_G8_B8_A8_UNORM, new PixelFormatInfo(InternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte) },
            { SurfaceFormat.R8_G8_B8_A8_SRGB, new PixelFormatInfo(InternalFormat.SrgbAlpha, PixelFormat.Rgba, PixelType.UnsignedByte) },
            { SurfaceFormat.R32_G32_B32_A32_UNORM, new PixelFormatInfo(InternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float) },
            { SurfaceFormat.R8_UNORM, new PixelFormatInfo(InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte) },
            { SurfaceFormat.R8_G8_UNORM, new PixelFormatInfo(InternalFormat.RG8, PixelFormat.RG, PixelType.UnsignedByte) },
            { SurfaceFormat.R8_G8_SNORM, new PixelFormatInfo(InternalFormat.RG8SNorm, PixelFormat.RG, PixelType.Byte) },
            { SurfaceFormat.R16_UNORM, new PixelFormatInfo(InternalFormat.RG16f, PixelFormat.RG, PixelType.HalfFloat) },
            { SurfaceFormat.B5_G6_R5_UNORM, new PixelFormatInfo( InternalFormat.Rgb565, PixelFormat.Rgb, PixelType.UnsignedShort565Rev) },
            { SurfaceFormat.R9_G9_B9_E5_UNORM, new PixelFormatInfo( InternalFormat.Rgb9E5, PixelFormat.Rgb, PixelType.UnsignedInt5999Rev) },
        };

        static readonly Dictionary<SurfaceFormat, InternalFormat> InternalFormatList = new Dictionary<SurfaceFormat, InternalFormat>
        {
            { SurfaceFormat.BC1_UNORM, InternalFormat.CompressedRgbaS3TCDxt1Ext },
            { SurfaceFormat.BC1_SRGB, InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext },
            { SurfaceFormat.BC2_UNORM, InternalFormat.CompressedRgbaS3TCDxt3Ext },
            { SurfaceFormat.BC2_SRGB, InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext },
            { SurfaceFormat.BC3_UNORM, InternalFormat.CompressedRgbaS3TCDxt5Ext },
            { SurfaceFormat.BC3_SRGB, InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext },
            { SurfaceFormat.BC4_UNORM, InternalFormat.CompressedRedRgtc1 },
            { SurfaceFormat.BC4_SNORM, InternalFormat.CompressedSignedRedRgtc1 },
            { SurfaceFormat.BC5_UNORM, InternalFormat.CompressedRGRgtc2 },
            { SurfaceFormat.BC5_SNORM, InternalFormat.CompressedSignedRGRgtc2 },
            { SurfaceFormat.BC6_UFLOAT, InternalFormat.CompressedRgbBptcUnsignedFloat },
            { SurfaceFormat.BC6_FLOAT, InternalFormat.CompressedRgbBptcSignedFloat },
            { SurfaceFormat.BC7_UNORM, InternalFormat.CompressedRgbaBptcUnorm },
            { SurfaceFormat.BC7_SRGB, InternalFormat.CompressedSrgbAlphaBptcUnorm },
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

        public class PixelFormatInfo
        {
            public PixelFormat Format { get; set; }
            public InternalFormat InternalFormat { get; set; }
            public PixelType Type { get; set; }

            public PixelFormatInfo(InternalFormat internalFormat, PixelFormat format, PixelType type)
            {
                InternalFormat = (InternalFormat)internalFormat;
                Format = format;
                Type = type;
            }
        }
    }
}
