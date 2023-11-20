using Fushigi.Bfres.Common;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Fushigi.Bfres.ShaderParam;
using static System.Net.Mime.MediaTypeNames;

namespace Fushigi.Bfres
{
    public class Material : IResData, INamed
    {
        public string Name { get; set; }

        public bool Visible => header.Flags == 1;

        public ResDict<Sampler> Samplers { get; set; }
        public ResDict<ShaderParam> ShaderParams { get; set; }
        public ResDict<RenderInfo> RenderInfos { get; set; }

        public List<string> Textures = new List<string>();

        public ShaderAssign ShaderAssign { get; set; } = new ShaderAssign();

        private MaterialHeader header;
        private ShaderInfoHeader shaderInfoHeader;
        private ShaderAssignHeader shaderAssignHeader;

        public string GetRenderInfoString(string key)
        {
            if (this.RenderInfos.ContainsKey(key))
                return this.RenderInfos[key].GetValueStrings().FirstOrDefault();
            return "";
        }

        public float GetRenderInfoFloat(string key)
        {
            if (this.RenderInfos.ContainsKey(key))
                return this.RenderInfos[key].GetValueSingles().FirstOrDefault();
            return 1f;
        }

        public int GetRenderInfoInt(string key)
        {
            if (this.RenderInfos.ContainsKey(key))
                return this.RenderInfos[key].GetValueInt32s().FirstOrDefault();
            return 1;
        }

        public void Read(BinaryReader reader)
        {
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            Name = reader.ReadStringOffset(header.NameOffset);

            long pos = reader.BaseStream.Position;

            reader.SeekBegin((long)header.TextureNamesOffset);
            Textures = reader.ReadStringOffsets(header.TextureRefCount);

            Samplers = reader.ReadDictionary<Sampler>(
                header.SamplerDictionaryOffset, header.SamplerOffset);

            //Read shader info
            reader.SeekBegin((long)header.ShaderInfoOffset);
            reader.BaseStream.Read(Utils.AsSpan(ref shaderInfoHeader));

            //Read shader assign
            reader.SeekBegin((long)shaderInfoHeader.ShaderAssignOffset);
            reader.BaseStream.Read(Utils.AsSpan(ref shaderAssignHeader));

            ReadShaderOptions(reader);
            ReadRenderInfo(reader);
            ReadShaderParameters(reader);

            ShaderAssign.ShaderArchiveName = reader.ReadStringOffset(shaderAssignHeader.ShaderArchiveNameOffset);
            ShaderAssign.ShadingModelName = reader.ReadStringOffset(shaderAssignHeader.ShaderModelNameOffset);

            ShaderAssign.SamplerAssign = ReadAssign(reader, 
                shaderInfoHeader.SamplerAssignOffset,
                shaderAssignHeader.SamplerAssignDictOffset,
                shaderInfoHeader.SamplerAssignIndicesOffset,
                shaderInfoHeader.NumSamplerAssign);

            ShaderAssign.AttributeAssign = ReadAssign(reader,
                shaderInfoHeader.AttributeAssignOffset,
                shaderAssignHeader.AttributeAssignDictOffset,
                shaderInfoHeader.AttributeAssignIndicesOffset,
                shaderInfoHeader.NumAttributeAssign);

            reader.SeekBegin(pos);
        }

        private void ReadShaderOptions(BinaryReader reader)
        {
            //Shader options

            //boolean choices
            var numBoolChoices = shaderInfoHeader.NumOptionBooleans;
            var _optionBooleans = reader.ReadCustom(() => reader.ReadBooleanBits(numBoolChoices),
                 (uint)shaderInfoHeader.OptionBoolChoiceOffset);

            //string choices
            var numChoiceValues = shaderInfoHeader.NumOptions - shaderInfoHeader.NumOptionBooleans;
            var _optionStrings = reader.ReadCustom(() =>
                        reader.ReadStringOffsets((int)numChoiceValues), (uint)shaderInfoHeader.OptionStringChoiceOffset);

            //Option value indices
            var _optionIndices = reader.ReadCustom(() =>
                    reader.ReadSbytes((int)shaderInfoHeader.NumOptions), (uint)shaderInfoHeader.OptionIndicesOffset);

            //Option key list
            var _optionKeys = reader.ReadDictionary<ResString>(shaderAssignHeader.OptionsDictOffset);

            List<string> choices = new List<string>();
            for (int i = 0; i < _optionBooleans.Length; i++)
                choices.Add(_optionBooleans[i] ? "True" : "False");
            if (_optionStrings != null)
                choices.AddRange(_optionStrings);

            for (int i = 0; i < _optionKeys.Count; i++)
            {
                int idx = _optionIndices?.Length > 0 ? _optionIndices[i] : i;
                if (idx == -1)
                    continue;

                var value = choices[idx];
                var key = _optionKeys.GetKey(i);

                ShaderAssign.ShaderOptions.Add(key, value);
            }
        }

        private void ReadRenderInfo(BinaryReader reader)
        {
            this.RenderInfos = new ResDict<RenderInfo>();

            var dict = reader.ReadDictionary<ResString>(shaderAssignHeader.RenderInfoDictOffset);
            if (dict.Count == 0) return;

            for (int i = 0; i < dict.Count; i++)
            {
                RenderInfo renderInfo = new RenderInfo();

                //Info table
                reader.SeekBegin(((long)shaderAssignHeader.RenderInfoOffset + i * 16));
                renderInfo.Name = reader.ReadStringOffset(reader.ReadUInt64()); //name offset
                renderInfo.Type = (RenderInfo.RenderInfoType)reader.ReadByte();

                //Count table
                reader.SeekBegin((int)header.RenderInfoNumOffset + i * 2);
                ushort count = reader.ReadUInt16();

                //Offset table
                reader.SeekBegin((int)header.RenderInfoDataOffsetTable + i * 2);
                ushort dataOffset = reader.ReadUInt16();

                //Raw data table
                reader.SeekBegin((int)header.RenderInfoBufferOffset + dataOffset);
                renderInfo.ReadData(reader, count);

                this.RenderInfos.Add(renderInfo.Name, renderInfo);
            }
        }

        private void ReadShaderParameters(BinaryReader reader)
        {
            this.ShaderParams = new ResDict<ShaderParam>();

            var dict = reader.ReadDictionary<ResString>(shaderAssignHeader.ShaderParamDictOffset);
            if (dict.Count == 0) return;

            reader.SeekBegin((long)shaderAssignHeader.ShaderParamOffset);
            for (int i = 0; i < dict.Count; i++)
            {
                ShaderParam param = new ShaderParam();
                param.Read(reader);
                this.ShaderParams.Add(param.Name, param);
            }

            //Param data
            var paramData = reader.ReadCustom(() =>
                        reader.ReadBytes(shaderAssignHeader.ShaderParamSize), (uint)header.ParamDataOffset);

            using (var dataReader = new BinaryReader(new MemoryStream(paramData)))
            {
                foreach (var param in this.ShaderParams.Values)
                    param.ReadShaderParamData(dataReader);
            }
        }

        private ResDict<ResString> ReadAssign(BinaryReader reader, ulong stringListOffst, 
            ulong stringDictOffset, ulong indicesOffset, int numValues)
        {
            ResDict<ResString> dict = new ResDict<ResString>();
            if (numValues == 0) return dict;

            //string values
            var _valueStrings = reader.ReadCustom(() =>
                        reader.ReadStringOffsets((int)numValues), (uint)stringListOffst);

            //Value indices
            var _valueIndices = reader.ReadCustom(() =>
                    reader.ReadSbytes((int)numValues), (uint)indicesOffset);

            //key list
            var _optionKeys = reader.ReadDictionary<ResString>(stringDictOffset);

            for (int i = 0; i < _valueStrings.Count; i++)
            {
                int idx = _valueIndices?.Length > 0 ? _valueIndices[i] : i;
                if (idx == -1) //value unused, skip
                    continue;

                var value = _valueStrings[i];
                var key = _optionKeys.GetKey(idx);

                dict.Add(key, value);
            }
            return dict;
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
            reader.ReadBytes(12);
        }
    }

    public class ShaderAssign
    {
       public ResDict<ResString> ShaderOptions { get; set; } = new ResDict<ResString>();
        public ResDict<ResString> SamplerAssign { get; set; } = new ResDict<ResString>();
        public ResDict<ResString> AttributeAssign { get; set; } = new ResDict<ResString>();

        public string ShaderArchiveName;
        public string ShadingModelName;
    }

    public class RenderInfo : IResData
    {
        public string Name { get; set; }

        public RenderInfoType Type { get; set; }

        private object _value;
        public object Data
        {
            get { return _value; }
            set { _value = value; }
        }

        public float[] GetValueSingles() {
            return _value != null ? (float[])_value : new float[0];
        }

        public int[] GetValueInt32s() {
            return _value != null ? (int[])_value : new int[0];
        }

        public string[] GetValueStrings() {
            return _value != null ? (string[])_value : new string[0];
        }

        public dynamic GetValue(int index = 0)
        {
            switch (Type)
            {
                case RenderInfoType.Int32:
                    {
                        var values = GetValueInt32s();
                        if (values.Length > index)
                            return values[index];
                        return 0;
                    }
                case RenderInfoType.Single:
                    {
                        var values = GetValueSingles();
                        if (values.Length > index)
                            return values[index];
                        return 0f;
                    }
                case RenderInfoType.String:
                    {
                        var values = GetValueStrings();
                        if (values.Length > index)
                            return values[index];
                        return "";
                    }
            }
            return null;
        }

        public void Read(BinaryReader reader)
        {
        }

        public void ReadData(BinaryReader reader, int count)
        {
            switch (Type)
            {
                case RenderInfoType.Int32:
                    _value = reader.ReadInt32s(count);
                    break;
                case RenderInfoType.Single:
                    _value = reader.ReadSingles(count);
                    break;
                case RenderInfoType.String:
                    _value = reader.ReadStringOffsets(count).ToArray();
                    break;
            }
        }

        public enum RenderInfoType : byte
        {
            Int32,
            Single,
            String
        }
    }

    public class ShaderParam : IResData
    {
        public object DataValue;

        public string Name;

        public ushort DataOffset;

        public ShaderParamType Type;

        public void Read(BinaryReader reader)
        {
            reader.ReadUInt64(); //padding
            Name = reader.ReadStringOffset(reader.ReadUInt64()); //name offset
            DataOffset = reader.ReadUInt16(); //padding
            Type = (ShaderParamType)reader.ReadUInt16(); //type
            reader.ReadUInt32(); //padding
        }

        public void ReadShaderParamData(BinaryReader reader)
        {
            reader.SeekBegin(this.DataOffset);
            this.DataValue = ReadParamData(this.Type, reader);
        }

        public override string ToString()
        {
            return $"{this.Type} | {this.DataValue}";
        }

        private object ReadParamData(ShaderParamType type, BinaryReader reader)
        {
            switch (type)
            {
                case ShaderParamType.Bool: return reader.ReadBoolean();
                case ShaderParamType.Bool2: return reader.ReadBooleans(2);
                case ShaderParamType.Bool3: return reader.ReadBooleans(3);
                case ShaderParamType.Bool4: return reader.ReadBooleans(4);
                case ShaderParamType.Float: return reader.ReadSingle();
                case ShaderParamType.Float2: return reader.ReadSingles(2);
                case ShaderParamType.Float2x2: return reader.ReadSingles(2 * 2);
                case ShaderParamType.Float2x3: return reader.ReadSingles(2 * 3);
                case ShaderParamType.Float2x4: return reader.ReadSingles(2 * 4);
                case ShaderParamType.Float3: return reader.ReadSingles(3);
                case ShaderParamType.Float3x2: return reader.ReadSingles(3 * 2);
                case ShaderParamType.Float3x3: return reader.ReadSingles(3 * 3);
                case ShaderParamType.Float3x4: return reader.ReadSingles(3 * 4);
                case ShaderParamType.Float4: return reader.ReadSingles(4);
                case ShaderParamType.Float4x2: return reader.ReadSingles(4 * 2);
                case ShaderParamType.Float4x3: return reader.ReadSingles(4 * 3);
                case ShaderParamType.Float4x4: return reader.ReadSingles(4 * 4);
                case ShaderParamType.Int: return reader.ReadInt32();
                case ShaderParamType.Int2: return reader.ReadInt32s(2);
                case ShaderParamType.Int3: return reader.ReadInt32s(3);
                case ShaderParamType.Int4: return reader.ReadInt32s(4);
                case ShaderParamType.UInt: return reader.ReadInt32();
                case ShaderParamType.UInt2: return reader.ReadInt32s(2);
                case ShaderParamType.UInt3: return reader.ReadInt32s(3);
                case ShaderParamType.UInt4: return reader.ReadInt32s(4);
                case ShaderParamType.Reserved2: return reader.ReadBytes(2);
                case ShaderParamType.Reserved3: return reader.ReadBytes(3);
                case ShaderParamType.Reserved4: return reader.ReadBytes(4);
                case ShaderParamType.Srt2D:
                    return new Srt2D()
                    {
                        Scaling = reader.ReadVector2(),
                        Rotation = reader.ReadSingle(),
                        Translation = reader.ReadVector2(),
                    };
                case ShaderParamType.Srt3D:
                    return new Srt3D()
                    {
                        Scaling = reader.ReadVector3(),
                        Rotation = reader.ReadVector3(),
                        Translation = reader.ReadVector3(),
                    };
                case ShaderParamType.TexSrt:
                case ShaderParamType.TexSrtEx:
                    return new TexSrt()
                    {
                        Mode = (TexSrt.TexSrtMode)reader.ReadInt32(),
                        Scaling = reader.ReadVector2(),
                        Rotation = reader.ReadSingle(),
                        Translation = reader.ReadVector2(),
                    };
            }
            return 0;
        }

        public class Srt2D
        {
            public Vector2 Scaling;
            public float Rotation;
            public Vector2 Translation;
        }

        public class Srt3D
        {
            public Vector3 Scaling;
            public Vector3 Rotation;
            public Vector3 Translation;
        }

        public class TexSrt
        {
            public TexSrtMode Mode;

            public Vector2 Scaling;
            public float Rotation;
            public Vector2 Translation;

            public enum TexSrtMode : uint
            {
                ModeMaya,
                Mode3dsMax,
                ModeSoftimage
            }
        }

        public enum ShaderParamType : byte
        {
            Bool,
            Bool2,
            Bool3,
            Bool4,
            Int,
            Int2,
            Int3,
            Int4,
            UInt,
            UInt2,
            UInt3,
            UInt4,
            Float,
            Float2,
            Float3,
            Float4,
            Reserved2,
            Float2x2,
            Float2x3,
            Float2x4,
            Reserved3,
            Float3x2,
            Float3x3,
            Float3x4,
            Reserved4,
            Float4x2,
            Float4x3,
            Float4x4,
            Srt2D,
            Srt3D,
            TexSrt,
            TexSrtEx
        }
    }
}
