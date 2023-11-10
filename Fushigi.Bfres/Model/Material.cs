using Fushigi.Bfres.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    public class Material : IResData, INamed
    {
        public string Name { get; set; }

        public bool Visible => header.Flags == 1;

        public ResDict<Sampler> Samplers { get; set; }

        public List<string> Textures = new List<string>();

        private MaterialHeader header;

        public void Read(BinaryReader reader)
        {
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            Name = reader.ReadStringOffset(header.NameOffset);

            long pos = reader.BaseStream.Position;

            reader.SeekBegin((long)header.TextureNamesOffset);
            Textures = reader.ReadStringOffsets(header.TextureRefCount);

            Samplers = reader.ReadDictionary<Sampler>(
                header.SamplerDictionaryOffset, header.SamplerOffset);

            reader.SeekBegin(pos);
        }
    }

    public class Sampler : IResData
    {
        private const ushort _flagsShrinkMask = 0b00000000_00110000;
        private const ushort _flagsExpandMask = 0b00000000_00001100;
        private const ushort _flagsMipmapMask = 0b00000000_00000011;

        public ushort _filterFlags;

        public MipFilterModes Mipmap
        {
            get { return (MipFilterModes)(_filterFlags & _flagsMipmapMask); }
            set { _filterFlags = (ushort)(_filterFlags & ~_flagsMipmapMask | (ushort)value); }
        }

        public ExpandFilterModes MagFilter
        {
            get { return (ExpandFilterModes)(_filterFlags & _flagsExpandMask); }
            set { _filterFlags = (ushort)(_filterFlags & ~_flagsExpandMask | (ushort)value); }
        }

        public ShrinkFilterModes MinFilter
        {
            get { return (ShrinkFilterModes)(_filterFlags & _flagsShrinkMask); }
            set { _filterFlags = (ushort)(_filterFlags & ~_flagsShrinkMask | (ushort)value); }
        }

        public TexWrap WrapModeU;
        public TexWrap WrapModeV;
        public TexWrap WrapModeW;

        public CompareFunction CompareFunc;
        public TexBorderType BorderColorType;
        public MaxAnisotropic Anisotropic;

        public float MinLOD;
        public float MaxLOD;
        public float LODBias;

        public void Read(BinaryReader reader)
        {
            WrapModeU = (TexWrap)reader.ReadByte();
            WrapModeV = (TexWrap)reader.ReadByte();
            WrapModeW = (TexWrap)reader.ReadByte();
            CompareFunc = (CompareFunction)reader.ReadByte();
            BorderColorType = (TexBorderType)reader.ReadByte();
            Anisotropic = (MaxAnisotropic)reader.ReadByte();
            _filterFlags = reader.ReadUInt16();
            MinLOD = reader.ReadSingle();
            MaxLOD = reader.ReadSingle();
            LODBias = reader.ReadSingle();
            reader.Seek(12);
        }
    }
}
