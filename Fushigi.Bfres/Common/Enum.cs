using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    public enum BfresIndexFormat : uint
    {
        UnsignedByte = 0,
        UInt16 = 1,
        UInt32 = 2,
    }

    public enum BfresPrimitiveType: uint
    {
        Triangles = 0x03,
        TriangleStrip = 0x04,
    }

    public enum BfresAttribFormat : uint
    {
        // 8 bits (8 x 1)
        Format_8_UNorm = 0x00000102, //
        Format_8_UInt = 0x00000302, //
        Format_8_SNorm = 0x00000202, //
        Format_8_SInt = 0x00000402, //
        Format_8_UIntToSingle = 0x00000802,
        Format_8_SIntToSingle = 0x00000A02,
        // 8 bits (4 x 2)
        Format_4_4_UNorm = 0x00000001,
        // 16 bits (16 x 1)
        Format_16_UNorm = 0x0000010A,
        Format_16_UInt = 0x0000020A,
        Format_16_SNorm = 0x0000030A,
        Format_16_SInt = 0x0000040A,
        Format_16_Single = 0x0000050A,
        Format_16_UIntToSingle = 0x00000803,
        Format_16_SIntToSingle = 0x00000A03,
        // 16 bits (8 x 2)
        Format_8_8_UNorm = 0x00000109, //
        Format_8_8_UInt = 0x00000309, //
        Format_8_8_SNorm = 0x00000209, //
        Format_8_8_SInt = 0x00000409, //
        Format_8_8_UIntToSingle = 0x00000804,
        Format_8_8_SIntToSingle = 0x00000A04,
        // 32 bits (16 x 2)
        Format_16_16_UNorm = 0x00000112, //
        Format_16_16_SNorm = 0x00000212, //
        Format_16_16_UInt = 0x00000312,
        Format_16_16_SInt = 0x00000412,
        Format_16_16_Single = 0x00000512, //
        Format_16_16_UIntToSingle = 0x00000807,
        Format_16_16_SIntToSingle = 0x00000A07,
        // 32 bits (10/11 x 3)
        Format_10_11_11_Single = 0x00000809,
        // 32 bits (8 x 4)
        Format_8_8_8_8_UNorm = 0x0000010B, //
        Format_8_8_8_8_SNorm = 0x0000020B, //
        Format_8_8_8_8_UInt = 0x0000030B, //
        Format_8_8_8_8_SInt = 0x0000040B, //
        Format_8_8_8_8_UIntToSingle = 0x0000080B,
        Format_8_8_8_8_SIntToSingle = 0x00000A0B,
        // 32 bits (10 x 3 + 2)
        Format_10_10_10_2_UNorm = 0x0000000B,
        Format_10_10_10_2_UInt = 0x0000090B,
        Format_10_10_10_2_SNorm = 0x0000020E, // High 2 bits are UNorm //
        Format_10_10_10_2_SInt = 0x0000099B,
        // 64 bits (16 x 4)
        Format_16_16_16_16_UNorm = 0x00000115, //
        Format_16_16_16_16_SNorm = 0x00000215, //
        Format_16_16_16_16_UInt = 0x00000315, //
        Format_16_16_16_16_SInt = 0x00000415, //
        Format_16_16_16_16_Single = 0x00000515, //
        Format_16_16_16_16_UIntToSingle = 0x0000080E,
        Format_16_16_16_16_SIntToSingle = 0x00000A0E,
        // 32 bits (32 x 1)
        Format_32_UInt = 0x00000314,
        Format_32_SInt = 0x00000416,
        Format_32_Single = 0x00000516,
        // 64 bits (32 x 2)
        Format_32_32_UInt = 0x00000317, //
        Format_32_32_SInt = 0x00000417, //
        Format_32_32_Single = 0x00000517, //
                                          // 96 bits (32 x 3)
        Format_32_32_32_UInt = 0x00000318, //
        Format_32_32_32_SInt = 0x00000418, //
        Format_32_32_32_Single = 0x00000518, //
                                             // 128 bits (32 x 4)
        Format_32_32_32_32_UInt = 0x00000319, //
        Format_32_32_32_32_SInt = 0x00000419, //
        Format_32_32_32_32_Single = 0x00000519 //
    }

    public enum MaxAnisotropic : byte
    {
        Ratio_1_1 = 0x1,
        Ratio_2_1 = 0x2,
        Ratio_4_1 = 0x4,
        Ratio_8_1 = 0x8,
        Ratio_16_1 = 0x10,
    }

    public enum MipFilterModes : ushort
    {
        None = 0,
        Points = 1,
        Linear = 2,
    }

    public enum ExpandFilterModes : ushort
    {
        Points = 1 << 2,
        Linear = 2 << 2,
    }

    public enum ShrinkFilterModes : ushort
    {
        Points = 1 << 4,
        Linear = 2 << 4,
    }
    public enum CompareFunction : byte
    {
        Never,
        Less,
        Equal,
        LessOrEqual,
        Greater,
        NotEqual,
        GreaterOrEqual,
        Always
    }
    public enum TexBorderType : byte
    {
        White,
        Transparent,
        Opaque,
    }

    public enum TexWrap : sbyte
    {
        Repeat,
        Mirror,
        Clamp,
        ClampToEdge,
        MirrorOnce,
        MirrorOnceClampToEdge,
    }

    //BNTX

    /// <summary>
    /// Represents desired texture, color-buffer, depth-buffer, or scan-buffer formats.
    /// </summary>
    public enum SurfaceFormat : uint
    {
        Invalid = 0x0000,
        R4_G4_UNORM = 0x0101,
        R8_UNORM = 0x0201,
        R4_G4_B4_A4_UNORM = 0x0301,
        A4_B4_G4_R4_UNORM = 0x0401,
        R5_G5_B5_A1_UNORM = 0x0501,
        A1_B5_G5_R5_UNORM = 0x0601,
        R5_G6_B5_UNORM = 0x0701,
        B5_G6_R5_UNORM = 0x0801,
        R8_G8_UNORM = 0x0901,
        R8_G8_SNORM = 0x0902,
        R16_UNORM = 0x0a01,
        R16_UINT = 0x0a05,
        R8_G8_B8_A8_UNORM = 0x0b01,
        R8_G8_B8_A8_SNORM = 0x0b02,
        R8_G8_B8_A8_SRGB = 0x0b06,
        B8_G8_R8_A8_UNORM = 0x0c01,
        B8_G8_R8_A8_SRGB = 0x0c06,
        R9_G9_B9_E5_UNORM = 0x0d01,
        R10_G10_B10_A2_UNORM = 0x0e01,
        R11_G11_B10_UNORM = 0x0f01,
        R11_G11_B10_UINT = 0x0f05,
        B10_G11_R11_UNORM = 0x1001,
        R16_G16_UNORM = 0x1101,
        R24_G8_UNORM = 0x1201,
        R32_UNORM = 0x1301,
        R16_G16_B16_A16_UNORM = 0x1401,
        R32_G8_X24_UNORM = 0x1501,
        D32_FLOAT_S8X24_UINT = 0x1505,
        R32_G32_UNORM = 0x1601,
        R32_G32_B32_UNORM = 0x1701,
        R32_G32_B32_A32_UNORM = 0x1801,
        BC1_UNORM = 0x1a01,
        BC1_SRGB = 0x1a06,
        BC2_UNORM = 0x1b01,
        BC2_SRGB = 0x1b06,
        BC3_UNORM = 0x1c01,
        BC3_SRGB = 0x1c06,
        BC4_UNORM = 0x1d01,
        BC4_SNORM = 0x1d02,
        BC5_UNORM = 0x1e01,
        BC5_SNORM = 0x1e02,
        BC6_FLOAT = 0x1f05,
        BC6_UFLOAT = 0x1f0a,
        BC7_UNORM = 0x2001,
        BC7_SRGB = 0x2006,
        EAC_R11_UNORM = 0x2101,
        EAC_R11_G11_UNORM = 0x2201,
        ETC1_UNORM = 0x2301,
        ETC1_SRGB = 0x2306,
        ETC2_UNORM = 0x2401,
        ETC2_SRGB = 0x2406,
        ETC2_MASK_UNORM = 0x2501,
        ETC2_MASK_SRGB = 0x2506,
        ETC2_ALPHA_UNORM = 0x2601,
        ETC2_ALPHA_SRGB = 0x2606,
        PVRTC1_28PP_UNORM = 0x2701,
        PVRTC1_48PP_UNORM = 0x2801,
        PVRTC1_ALPHA_28PP_UNORM = 0x2901,
        PVRTC1_ALPHA_48PP_UNORM = 0x2a01,
        PVRTC2_ALPHA_28PP_UNORM = 0x2b01,
        PVRTC2_ALPHA_48PP_UNORM = 0x2c01,
        ASTC_4x4_UNORM = 0x2d01,
        ASTC_4x4_SRGB = 0x2d06,
        ASTC_5x4_UNORM = 0x2e01,
        ASTC_5x4_SRGB = 0x2e06,
        ASTC_5x5_UNORM = 0x2f01,
        ASTC_5x5_SRGB = 0x2f06,
        ASTC_6x5_UNORM = 0x3001,
        ASTC_6x5_SRGB = 0x3006,
        ASTC_6x6_UNORM = 0x3101,
        ASTC_6x6_SRGB = 0x3106,
        ASTC_8x5_UNORM = 0x3201,
        ASTC_8x5_SRGB = 0x3206,
        ASTC_8x6_UNORM = 0x3301,
        ASTC_8x6_SRGB = 0x3306,
        ASTC_8x8_UNORM = 0x3401,
        ASTC_8x8_SRGB = 0x3406,
        ASTC_10x5_UNORM = 0x3501,
        ASTC_10x5_SRGB = 0x3506,
        ASTC_10x6_UNORM = 0x3601,
        ASTC_10x6_SRGB = 0x3606,
        ASTC_10x8_UNORM = 0x3701,
        ASTC_10x8_SRGB = 0x3706,
        ASTC_10x10_UNORM = 0x3801,
        ASTC_10x10_SRGB = 0x3806,
        ASTC_12x10_UNORM = 0x3901,
        ASTC_12x10_SRGB = 0x3906,
        ASTC_12x12_UNORM = 0x3a01,
        ASTC_12x12_SRGB = 0x3a06,
        B5_G5_R5_A1_UNORM = 0x3b01,
    }

    /// <summary>
    /// Represents shapes of a given surface or texture.
    /// </summary>
    public enum Dim : sbyte
    {
        Undefined,
        Dim1D,
        Dim2D,
        Dim3D,
    }

    /// <summary>
    /// Represents shapes of a given surface or texture.
    /// </summary>
    public enum SurfaceDim : byte
    {
        Dim1D,
        Dim2D,
        Dim3D,
        DimCube,
        Dim1DArray,
        Dim2DArray,
        Dim2DMsaa,
        Dim2DMsaaArray,
        DimCubeArray,
    }

    [Flags]
    public enum ChannelType
    {
        Zero,
        One,
        Red,
        Green,
        Blue,
        Alpha
    }

    [Flags]
    public enum AccessFlags : uint
    {
        Texture = 0x20,
    }

    /// <summary>
    /// Represents the desired tiling modes for a surface.
    /// </summary>
    public enum TileMode : ushort
    {
        Default,
        LinearAligned,
    }
}
