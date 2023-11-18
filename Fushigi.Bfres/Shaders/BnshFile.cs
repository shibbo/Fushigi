using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    public class BnshFile
    {
        public string Name { get; set; }

        public List<ShaderVariation> Variations { get; set; }

        private BinaryHeader BinHeader; //A header shared between bnsh and other formats
        private BnshHeader Header; //Bnsh header

        public BnshFile() { }

        public BnshFile(string filePath) {
            Read(File.OpenRead(filePath));
        }

        public BnshFile(Stream stream) {
            Read(stream);
        }

        public void Read(Stream stream)
        {
            var reader = stream.AsBinaryReader();

            stream.Read(Utils.AsSpan(ref BinHeader));
            reader.ReadBytes(64); //padding

            if (BinHeader.NameOffset != 0)
                Name = reader.ReadStringOffset(BinHeader.NameOffset - 2);

            //GRSC header
            reader.BaseStream.Read(Utils.AsSpan(ref Header));

            Variations = reader.ReadArray<ShaderVariation>(Header.VariationStartOffset, (int)Header.NumVariation);
        }

        public class ShaderVariation : IResData
        {
            public BnshShaderProgram BinaryProgram { get; set; }

            internal long Position;

            private VariationHeader header; 

            public void Read(BinaryReader reader)
            {
                Position = reader.BaseStream.Position;

                reader.BaseStream.Read(Utils.AsSpan(ref header));
                var pos = reader.BaseStream.Position;

                BinaryProgram = reader.Read<BnshShaderProgram>(header.BinaryOffset);

                reader.SeekBegin(pos);
            }
        }

        public class BnshShaderProgram : IResData
        {
            public ShaderCode VertexShader { get; set; }
            public ShaderCode FragmentShader { get; set; }
            public ShaderCode GeometryShader { get; set; }

            private BnshShaderProgramHeader header;

            public void Read(BinaryReader reader)
            {
                reader.BaseStream.Read(Utils.AsSpan(ref header));
                var pos = reader.BaseStream.Position;

                VertexShader = reader.Read<ShaderCode>(header.VertexShaderOffset);
                FragmentShader = reader.Read<ShaderCode>(header.FragmentShaderOffset);
                GeometryShader = reader.Read<ShaderCode>(header.GeometryShaderOffset);

                reader.SeekBegin(pos);
            }
        }

        public class ShaderCode : IResData
        {
            public byte[] ControlCode;
            public byte[] ByteCode;

            public void Read(BinaryReader reader)
            {
                reader.ReadBytes(8); //always empty
                ulong controlCodeOffset = reader.ReadUInt64();
                ulong byteCodeOffset = reader.ReadUInt64();
                uint byteCodeSize = reader.ReadUInt32();
                uint controlCodeSize = reader.ReadUInt32();
                reader.ReadBytes(32); //padding

                ControlCode = reader.ReadCustom(() =>
                {
                    return reader.ReadBytes((int)controlCodeSize);
                }, controlCodeOffset);

                ByteCode = reader.ReadCustom(() =>
                {
                    return reader.ReadBytes((int)byteCodeSize);
                }, byteCodeOffset);
            }
        }
    }
}
