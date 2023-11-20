using Microsoft.VisualBasic;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BinaryHeader //A header shared between bntx and other formats
    {
        public ulong Magic; //MAGIC + padding

        public byte VersionMicro;
        public byte VersionMinor;
        public ushort VersionMajor;

        public ushort ByteOrder;
        public byte Alignment;
        public byte TargetAddressSize;
        public uint NameOffset;
        public ushort Flag;
        public ushort BlockOffset;
        public uint RelocationTableOffset;
        public uint FileSize;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ResHeader
    {
        public ulong NameOffset;

        public ulong ModelOffset;
        public ulong ModelDictionaryOffset;

        public ulong Reserved0;
        public ulong Reserved1;
        public ulong Reserved2;
        public ulong Reserved3;

        public ulong SkeletalAnimOffset;
        public ulong SkeletalAnimDictionaryOffset;
        public ulong MaterialAnimOffset;
        public ulong MaterialAnimDictionarymOffset;
        public ulong BoneVisAnimOffset;
        public ulong BoneVisAnimDictionarymOffset;
        public ulong ShapeAnimOffset;
        public ulong ShapeAnimDictionarymOffset;
        public ulong SceneAnimOffset;
        public ulong SceneAnimDictionarymOffset;

        public ulong MemoryPoolOffset;
        public ulong MemoryPoolInfoOffset;

        public ulong EmbeddedFilesOffset;
        public ulong EmbeddedFilesDictionaryOffset;

        public ulong UserPointer;

        public ulong StringTableOffset;
        public uint StringTableSize;

        public ushort ModelCount;

        public ushort Reserved4;
        public ushort Reserved5;

        public ushort SkeletalAnimCount;
        public ushort MaterialAnimCount;
        public ushort BoneVisAnimCount;
        public ushort ShapeAnimCount;
        public ushort SceneAnimCount;
        public ushort EmbeddedFileCount;

        public byte ExternalFlags;
        public byte Reserved6;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BufferMemoryPool
    {
        public uint Flag;
        public uint Size;
        public ulong Offset;

        public ulong Reserved1;
        public ulong Reserved2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ModelHeader
    {
        public uint Magic;
        public uint Reserved;
        public ulong NameOffset;
        public ulong PathOffset;

        public ulong SkeletonOffset;
        public ulong VertexArrayOffset;
        public ulong ShapeArrayOffset;
        public ulong ShapeDictionaryOffset;
        public ulong MaterialArrayOffset;
        public ulong MaterialDictionaryOffset;
        public ulong ShaderAssignArrayOffset;

        public ulong UserDataArrayOffset;
        public ulong UserDataDictionaryOffset;

        public ulong UserPointer;

        public ushort VertexBufferCount;
        public ushort ShapeCount;
        public ushort MaterialCount;
        public ushort ShaderAssignCount;
        public ushort UserDataCount;

        public ushort Reserved1;
        public uint Reserved2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct VertexBufferHeader
    {
        public uint Magic;
        public uint Reserved;

        public ulong AttributeArrayOffset;
        public ulong AttributeArrayDictionary;

        public ulong MemoryPoolPointer;

        public ulong RuntimeBufferArray;
        public ulong UserBufferArray;

        public ulong VertexBufferInfoArray;
        public ulong VertexBufferStrideArray;
        public ulong UserPointer;

        public uint BufferOffset;
        public byte VertexAttributeCount;
        public byte VertexBufferCount;

        public ushort Index;
        public uint VertexCount;

        public ushort Reserved1;
        public ushort VertexBufferAlignment;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ShapeHeader
    {
        public uint Magic;
        public uint Flags;
        public ulong NameOffset;
        public ulong PathOffset;

        public ulong MeshArrayOffset;
        public ulong SkinBoneIndicesOffset;

        public ulong KeyShapeArrayOffset;
        public ulong KeyShapeDictionaryOffset;

        public ulong BoundingBoxOffset;
        public ulong BoundingSphereOffset;

        public ulong UserPointer;

        public ushort Index;
        public ushort MaterialIndex;
        public ushort BoneIndex;
        public ushort VertexBufferIndex;
        public ushort SkinBoneIndex;

        public byte MaxSkinInfluence;
        public byte MeshCount;
        public byte KeyShapeCount;
        public byte KeyAttributeCount;

        public ushort Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ShapeRadius
    {
        public float CenterX;
        public float CenterY;
        public float CenterZ;

        public float Radius;
    }


    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct MeshHeader
    {
        public ulong SubMeshArrayOffset;
        public ulong MemoryPoolOffset;
        public ulong BufferRuntimeOffset;
        public ulong BufferInfoOffset;

        public uint BufferOffset;

        public BfresPrimitiveType PrimType;
        public BfresIndexFormat IndexFormat;
        public uint IndexCount;
        public uint BaseIndex;
        public ushort SubMeshCount;
        public ushort Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct SkeletonHeader
    {
        public uint Magic;
        public uint Flags;
        public ulong BoneDictionaryOffset;
        public ulong BoneArrayOffset;
        public ulong MatrixToBoneListOffset;
        public ulong InverseModelMatricesOffset;
        public ulong Reserved;
        public ulong UserPointer;

        public ushort NumBones;
        public ushort NumSmoothMatrices;
        public ushort NumRigidMatrices;

        public ushort Padding1;
        public uint Padding2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BoneHeader
    {
        public ulong NameOffset;
        public ulong UserDataDictionaryOffset;
        public ulong UserDataArrayOffset;
        public ulong Reserved;

        public ushort Index;

        public short ParentIndex;
        public short SmoothMatrixIndex;
        public short RigidMatrixIndex;
        public short BillboardIndex;

        public ushort NumUserData;

        public uint Flags;

        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        public float RotationX;
        public float RotationY;
        public float RotationZ;
        public float RotationW;

        public float PositionX;
        public float PositionY;
        public float PositionZ;
    }


    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct MaterialHeader
    {
        public uint Magic; //FMAT
        public uint Flags;
        public ulong NameOffset;

        public ulong ShaderInfoOffset;

        public ulong TextureRuntimeDataOffset;
        public ulong TextureNamesOffset;
        public ulong SamplerRuntimeDataOffset;
        public ulong SamplerOffset;
        public ulong SamplerDictionaryOffset;
        public ulong RenderInfoBufferOffset;
        public ulong RenderInfoNumOffset;
        public ulong RenderInfoDataOffsetTable;
        public ulong ParamDataOffset;
        public ulong ParamIndicesOffset;
        public ulong Reserved;
        public ulong UserDataOffset;
        public ulong UserDataDictionaryOffset;
        public ulong VolatileFlagsOffset;
        public ulong UserPointer;
        public ulong SamplerIndicesOffset;
        public ulong TextureIndicesOffset;

        public ushort Index;
        public byte SamplerCount;
        public byte TextureRefCount;
        public ushort Reserved1;
        public ushort UserDataCount;

        public ushort RenderInfoDataSize;
        public ushort Reserved2;
        public uint Reserved3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ShaderInfoHeader
    {
        public ulong ShaderAssignOffset;
        public ulong AttributeAssignOffset;
        public ulong AttributeAssignIndicesOffset;
        public ulong SamplerAssignOffset;
        public ulong SamplerAssignIndicesOffset;
        public ulong OptionBoolChoiceOffset;
        public ulong OptionStringChoiceOffset;
        public ulong OptionIndicesOffset;

        public uint Padding;

        public byte NumAttributeAssign;
        public byte NumSamplerAssign;
        public ushort NumOptionBooleans;
        public ushort NumOptions;

        public ushort Padding2;
        public uint Padding3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ShaderAssignHeader
    {
        public ulong ShaderArchiveNameOffset;
        public ulong ShaderModelNameOffset;
        public ulong RenderInfoOffset;
        public ulong RenderInfoDictOffset;
        public ulong ShaderParamOffset;
        public ulong ShaderParamDictOffset;
        public ulong AttributeAssignDictOffset;
        public ulong SamplerAssignDictOffset;
        public ulong OptionsDictOffset;

        public ushort RenderInfoCount;
        public ushort ParamCount;
        public ushort ShaderParamSize;
        public ushort Padding1;
        public uint Padding2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BntxHeader
    {
        public uint Target; //NX 
        public uint TextureCount;
        public ulong TextureTableOffset;
        public ulong TextureArrayOffset;
        public ulong TextureDictionaryOffset;
        public ulong MemoryPoolOffset;
        public ulong RuntimePointer;
        public uint Padding1;
        public uint Padding2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct TextureHeader
    {
        public uint Magic; //BRTI  
        public uint NextBlockOffset;
        public uint BlockSize;
        public uint Reserved;

        public byte Flag;
        public Dim Dim;

        public TileMode TileMode;
        public ushort Swizzle;
        public ushort MipCount;
        public uint SampleCount;

        public SurfaceFormat Format;
        public AccessFlags AccessFlags;
        public uint Width;
        public uint Height;
        public uint Depth;
        public uint ArrayCount;

        public uint TextureLayout1;
        public uint TextureLayout2;

        public ulong Reserved1;
        public ulong Reserved2;

        public uint ImageSize;
        public uint Alignment;

        public uint ChannelSwizzle;

        public SurfaceDim TextureDim;
        public byte Padding0;
        public ushort Padding2;

        public ulong NameOffset;
        public ulong TextureContainerOffset;
        public ulong ImageDataTableOffset;
        public ulong UserDataOffset;
        public ulong RuntimePointer;
        public ulong RuntimeViewPointer;
        public ulong DescriptorPointer;
        public ulong UserDataDictionaryOffset;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BfshaHeader
    {
        public ulong ShaderArchiveOffset; 
        public ulong StringPoolOffset;
        public uint StringPoolSize;
        public uint Padding;

        public ulong NameOffset;
        public ulong PathOffset;
        public ulong ShaderModelOffset;
        public ulong ShaderModelDictionaryOffset;
        public ulong UserPointer;
        public ulong Unknown2;
        public ulong Unknown3;
        public ushort NumShaderModels;
        public ushort Flags;
        public uint Padding2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ShaderModelHeader
    {
        public ulong NameOffset;
        public ulong StaticOptionsArrayOffset;
        public ulong StaticOptionsDictionaryOffset;
        public ulong DynamicOptionsArrayOffset;
        public ulong DynamicOptionsDictionaryOffset;
        public ulong AttributesArrayOffset;
        public ulong AttributesDictionaryOffset;
        public ulong SamplerArrayOffset;
        public ulong SamplerDictionaryOffset;

        //Guess? Haven't seen image types used yet
        //For version >= 8
        //
        public ulong ImageArrayOffset;
        public ulong ImageDictionaryOffset;
        //

        public ulong UniformBlockArrayOffset;
        public ulong UniformBlockDictionaryOffset;
        public ulong UniformArrayOffset;

        //For version >= 7
        //
        public ulong StorageBlockArrayOffset;
        public ulong StorageBlockDictionaryOffset;
        public ulong Unknown0;
        //

        public ulong ShaderProgramArrayOffset;
        public ulong KeyTableOffset;

        public ulong ParentArchiveOffset;

        public ulong Unknown1;
        public ulong BnshOffset;

        public ulong Unknown2;
        public ulong Unknown3;
        public ulong Unknown4;

        //For version >= 7
        //
        public ulong Unknown5;
        public ulong Unknown6;
        //

        public uint NumUniforms;
        public uint NumStorageBlocks; //version >= 7

        public int DefaultProgramIndex;

        public ushort NumStaticOptions;
        public ushort NumDynamicOptions;
        public ushort NumShaderPrograms;

        public byte StaticKeyLength;
        public byte DynamicKeyLength;

        public byte NumAttributes;
        public byte NumSamplers;
        public byte NumImages; //version >= 8
        public byte NumUniformBlocks;
        public byte Unknown;

        public uint Unknown8;

        public ulong Padding1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ShaderOptionHeader
    {
        public ulong NameOffset;
        public ulong ChoiceDictionaryOffset;
        public ulong ChoiceArrayOffset;

        public ushort NumChoices;
        public ushort BlockOffset;
        public ushort Padding;
        public byte Flag;
        public byte KeyOffset;

        public uint Bit32Mask;
        public byte Bit32Index;
        public byte Bit32Shift;
        public ushort Padding2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ShaderProgramHeader
    {
        public ulong SamplerIndexTableOffset;
        public ulong ImageIndexTableOffset;
        public ulong UniformIndexTableBlockOffset;
        public ulong StorageBufferIndexTableOffset;
        public ulong VariationOffset;
        public ulong ParentModelOffset;

        public uint UsedAttributeFlags; //bits for what is used
        public ushort Flags;
        public ushort NumSamplers;

        public ushort NumImages;
        public ushort NumBlocks;
        public ushort NumStorageBuffers;
        public ushort Padding;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ShaderUniformBlockHeader
    {
        public ulong UniformArrayOffset;
        public ulong UniformDictionaryOffset;
        public ulong DefaultOffset;

        public byte Index;
        public byte Type;
        public ushort Size;
        public ushort NumUniforms;
        public ushort Padding;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BnshHeader
    {
        public uint Magic;
        public uint BlockOffset;
        public uint BlockSize;
        public uint Padding;

        public uint Version;
        public uint CodeTarget;
        public uint CompilerVersion;

        public uint NumVariation;
        public ulong VariationStartOffset;

        public ulong Unknown1;
        public ulong Unknown2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct VariationHeader
    {
        public ulong Offset1;
        public ulong Offset2;
        public ulong BinaryOffset;
        public ulong ParentBnshOffset;

        public ulong Padding1;
        public ulong Padding2;
        public ulong Padding3;
        public ulong Padding4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BnshShaderProgramHeader
    {
        public byte Flags;
        public byte CodeType;
        public byte Format;
        public byte Padding;
        public uint BinaryFormat;

        public ulong VertexShaderOffset;
        public ulong HullShaderOffset;
        public ulong DomainShaderOffset;
        public ulong GeometryShaderOffset;
        public ulong FragmentShaderOffset;
        public ulong ComputeShaderOffset;

        public ulong Reserved0;
        public ulong Reserved1;
        public ulong Reserved2;
        public ulong Reserved3;
        public ulong Reserved4;

        public uint ObjectSize;
        public uint Padding1;

        public ulong ObjectOffset;
        public ulong ParentVariationOffset;
        public ulong ShaderReflectionOffset;

        public ulong BinaryOffset;
        public ulong ParentBnshOffset;

        public ulong Reserved5;
        public ulong Reserved6;
        public ulong Reserved7;
        public ulong Reserved8;
    }
}
