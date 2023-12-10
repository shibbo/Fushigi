using Fushigi.Bfres.Common;


namespace Fushigi.Bfres
{
    public class BfshaFile
    {
        public string Name { get; set; }

        public ResDict<ShaderModel> ShaderModels { get; set; }

        private BinaryHeader BinHeader; //A header shared between bfsha and other formats
        private BfshaHeader Header; //Bfsha header

        public BfshaFile() { }

        public BfshaFile(string filePath) {
            Read(File.OpenRead(filePath));
        }

        public BfshaFile(Stream stream) {
            Read(stream);
        }

        public void Read(Stream stream)
        {
            var reader = stream.AsBinaryReader();

            stream.Read(Utils.AsSpan(ref BinHeader));
            stream.Read(Utils.AsSpan(ref Header));

            Name = reader.ReadStringOffset(Header.NameOffset);

            Console.WriteLine($"Bfsha {Name}");

            ShaderModels = reader.ReadDictionary<ShaderModel>(
                Header.ShaderModelDictionaryOffset,
                Header.ShaderModelOffset);
        }

        public class ShaderModel : IResData
        {
            public string Name { get; set; }

            public ResDict<ShaderOption> StaticShaderOptions { get; set; }
            public ResDict<ShaderOption> DynamicShaderOptions { get; set; }
            public List<ShaderProgram> Programs { get; set; }

            public ResDict<ShaderStorageBuffer> StorageBuffers { get; set; }
            public ResDict<ShaderUniformBlock> UniformBlocks { get; set; }

            public ResDict<Sampler> Samplers { get; set; }
            public ResDict<Attribute> Attributes { get; set; }

            public BnshFile BnshFile { get; set; }

            private int[] KeyTable { get; set; }

            private ShaderModelHeader header;

            private Stream Stream;

            public void Read(BinaryReader reader)
            {
                Stream = reader.BaseStream;

                reader.BaseStream.Read(Utils.AsSpan(ref header));
                long pos = reader.BaseStream.Position;

                Name = reader.ReadStringOffset(header.NameOffset);

                StaticShaderOptions = reader.ReadDictionary<ShaderOption>(
                      header.StaticOptionsDictionaryOffset,
                      header.StaticOptionsArrayOffset);

                DynamicShaderOptions = reader.ReadDictionary<ShaderOption>(
                      header.DynamicOptionsDictionaryOffset,
                      header.DynamicOptionsArrayOffset);

                Attributes = reader.ReadDictionary<Attribute>(
                    header.AttributesDictionaryOffset,
                    header.AttributesArrayOffset);

                Samplers = reader.ReadDictionary<Sampler>(
                    header.SamplerDictionaryOffset,
                    header.SamplerArrayOffset);

                UniformBlocks = reader.ReadDictionary<ShaderUniformBlock>(
                     header.UniformBlockDictionaryOffset,
                     header.UniformBlockArrayOffset);

                Programs = reader.ReadArray<ShaderProgram>(
                    header.ShaderProgramArrayOffset,
                    header.NumShaderPrograms);

                KeyTable = reader.ReadCustom(() =>
                {
                    int numKeysPerProgram = header.StaticKeyLength + header.DynamicKeyLength;

                    return reader.ReadInt32s(numKeysPerProgram * this.Programs.Count);
                }, header.KeyTableOffset);

                reader.SeekBegin((long)header.BnshOffset + 0x1C);
                var bnshSize = (int)reader.ReadUInt32();

                reader.SeekBegin(pos);
            }

            public BnshFile.ShaderVariation GetShaderVariation(ShaderProgram program)
            {
                Stream.Position = 0;

                var sub = new SubStream(Stream, (long)header.BnshOffset);
                var reader = sub.AsBinaryReader();

                reader.SeekBegin((long)program.VariationOffset - (long)header.BnshOffset);

                var v = new BnshFile.ShaderVariation();
                v.Read(reader);
                return v;
            }

            public int GetProgramIndex(Dictionary<string, string> options)
            {
                for (int i = 0; i < Programs.Count; i++)
                {
                    if (IsValidProgram(i, options))
                        return i;
                }
                return -1;
            }

            public bool IsValidProgram(int programIndex, Dictionary<string, string> options)
            {
                //The amount of keys used per program
                int numKeysPerProgram = header.StaticKeyLength + header.DynamicKeyLength;

                //Static key (total * program index)
                int baseIndex = numKeysPerProgram * programIndex;

                for (int j = 0; j < this.StaticShaderOptions.Count; j++)
                {
                    var option = this.StaticShaderOptions[j];
                    //The options must be the same between bfres and bfsha
                    if (!options.ContainsKey(option.Name))
                        continue;

                    //Get key in table
                    int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + option.Bit32Index]);
                    if (choiceIndex > option.Choices.Count)
                        throw new Exception($"Invalid choice index in key table! Option {option.Name} choice {options[option.Name]}");

                    //If the choice is not in the program, then skip the current program
                    var choice = option.Choices.GetKey(choiceIndex);
                    if (options[option.Name] != choice)
                        return false;
                }

                for (int j = 0; j < this.DynamicShaderOptions.Count; j++)
                {
                    var option = this.DynamicShaderOptions[j];
                    if (!options.ContainsKey(option.Name))
                        continue;

                    int ind = option.Bit32Index - option.KeyOffset;
                    int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + header.StaticKeyLength + ind]);
                    if (choiceIndex > option.Choices.Count)
                        throw new Exception($"Invalid choice index in key table!");

                    var choice = option.Choices.GetKey(choiceIndex);
                    if (options[option.Name] != choice)
                        return false;
                }
                return true;
            }
        }

        public class ShaderOption : IResData
        {
            public string Name { get; set; }

            public ResDict<ResUint32> Choices { get; set; }

            public int KeyOffset => header.KeyOffset;
            public int Bit32Index => header.Bit32Index;

            private ShaderOptionHeader header;

            public void Read(BinaryReader reader)
            {
                reader.BaseStream.Read(Utils.AsSpan(ref header));

                long pos = reader.BaseStream.Position;

                Name = reader.ReadStringOffset(header.NameOffset);

                Choices = reader.ReadDictionary<ResUint32>(header.ChoiceDictionaryOffset,
                    header.ChoiceArrayOffset);

                reader.SeekBegin(pos);
            }


            public int GetChoiceIndex(int key)
            {
                //Find choice index with mask and shift
                return (int)((key & header.Bit32Mask) >> header.Bit32Shift);
            }

            public int GetStaticKey()
            {
                var key = header.Bit32Index;
                return (int)((key & header.Bit32Mask) >> header.Bit32Shift);
            }

            public int GetDynamicKey()
            {
                var key = header.Bit32Index - header.KeyOffset;
                return (int)((key & header.Bit32Mask) >> header.Bit32Shift);
            }
        }

        public class ResUint32 : IResData
        {
            public uint Value { get; set; }

            public void Read(BinaryReader reader)
            {
                Value = reader.ReadUInt32();
            }
        }

        public class ShaderUniformBlock : IResData
        {
            public ushort Size => header.Size;
            public byte Index => header.Index;
            public byte Type => header.Type;

            public ResDict<ShaderUniform> Uniforms { get; set; }

            private ShaderUniformBlockHeader header;

            public void Read(BinaryReader reader)
            {
                reader.BaseStream.Read(Utils.AsSpan(ref header));

                long pos = reader.BaseStream.Position;

                Uniforms = reader.ReadDictionary<ShaderUniform>(
                    header.UniformDictionaryOffset, header.UniformArrayOffset);

                reader.SeekBegin(pos);
            }
        }

        public class ShaderUniform : IResData
        {
            public int Index { get; set; }
            public ushort DataOffset { get; set; }
            public byte BlockIndex { get; set; }

            public void Read(BinaryReader reader)
            {
                reader.ReadUInt64();
                Index = reader.ReadInt32();
                DataOffset = reader.ReadUInt16();
                BlockIndex = reader.ReadByte();
                reader.ReadByte(); //padding
            }
        }

        public class ShaderStorageBuffer : IResData
        {
            public void Read(BinaryReader reader)
            {

            }
        }

        public class Sampler : IResData
        {
            public string Annotation;

            public byte Index;

            public void Read(BinaryReader reader)
            {
                Annotation = reader.ReadStringOffset(reader.ReadUInt64());
                Index = reader.ReadByte();
                reader.ReadBytes(7); //padding
            }
        }

        public class Attribute : IResData
        {
            public byte Index;
            public sbyte Location;

            public void Read(BinaryReader reader)
            {
                Index = reader.ReadByte();
                Location = reader.ReadSByte();
            }
        }

        public class ShaderProgram : IResData
        {
            public List<ShaderIndexHeader> UniformBlockIndices = new List<ShaderIndexHeader>();
            public List<ShaderIndexHeader> SamplerIndices = new List<ShaderIndexHeader>();
            public List<ShaderIndexHeader> StorageBufferIndices = new List<ShaderIndexHeader>();

            internal ulong VariationOffset => header.VariationOffset;

            private ShaderProgramHeader header;

            public void Read(BinaryReader reader)
            {
                reader.BaseStream.Read(Utils.AsSpan(ref header));

                long pos = reader.BaseStream.Position;

                UniformBlockIndices = reader.ReadArray<ShaderIndexHeader>(header.UniformIndexTableBlockOffset, header.NumBlocks);
                SamplerIndices = reader.ReadArray<ShaderIndexHeader>(header.SamplerIndexTableOffset, header.NumSamplers);
                StorageBufferIndices = reader.ReadArray<ShaderIndexHeader>(header.StorageBufferIndexTableOffset, header.NumStorageBuffers);

                reader.SeekBegin(pos);
            }
        }


        public struct ShaderIndexHeader : IResData
        {
            public int VertexLocation;
            public int FragmentLocation;

            public void Read(BinaryReader reader)
            {
                VertexLocation = reader.ReadInt32();
                FragmentLocation = reader.ReadInt32();
            }
        }
    }
}
