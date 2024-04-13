using Fushigi.Bfres.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.Bfres
{
    public class VertexBuffer : IResData
    {
        public Vector4[] GetPositions() => TryGetAttributeData("_p0");
        public Vector4[] GetNormals() => TryGetAttributeData("_n0");
        public Vector4[] GetTexCoords(int channel) => TryGetAttributeData($"_u{channel}");
        public Vector4[] GetColors(int channel) => TryGetAttributeData($"_c{channel}");
        public Vector4[] GetBoneWeights(int channel = 0) => TryGetAttributeData($"_w{channel}");
        public Vector4[] GetBoneIndices(int channel = 0) => TryGetAttributeData($"_i{channel}");
        public Vector4[] GetTangents() => TryGetAttributeData($"_t0");
        public Vector4[] GetBitangent() => TryGetAttributeData($"_b0");

        /// <summary>
        /// The buffer data used to store attribute data.
        /// </summary>
        public List<BufferData> Buffers { get; set; } = new List<BufferData>();

        /// <summary>
        /// A list of vertex attributes for handling vertex data.
        /// </summary>
        public ResDict<VertexAttribute> Attributes { get; set; } = new ResDict<VertexAttribute>();

        /// <summary>
        /// The total number of vertices used in the buffer.
        /// </summary>
        public uint VertexCount => header.VertexCount;

        private VertexBufferHeader header = new VertexBufferHeader();

        private List<VertexBufferInfo> BufferInfo = new List<VertexBufferInfo>();
        private List<VertexStrideInfo> BufferStrides = new List<VertexStrideInfo>();

        public void Read(BinaryReader reader)
        {
            header = new VertexBufferHeader();
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            long pos = reader.BaseStream.Position;

            Attributes = reader.ReadDictionary<VertexAttribute>(header.AttributeArrayDictionary, header.AttributeArrayOffset);
            BufferInfo = reader.ReadArray<VertexBufferInfo>(header.VertexBufferInfoArray, header.VertexBufferCount);
            BufferStrides = reader.ReadArray<VertexStrideInfo>(header.VertexBufferStrideArray, header.VertexBufferCount);

            reader.SeekBegin(pos);
        }

        public void InitBuffers(BinaryReader reader, BufferMemoryPool bufferPoolInfo)
        {
            reader.SeekBegin((long)bufferPoolInfo.Offset + (int)header.BufferOffset);
            for (int buff = 0; buff < header.VertexBufferCount; buff++)
            {
                reader.Align(8);

                BufferData buffer = new BufferData();
                buffer.Data = reader.ReadBytes((int)BufferInfo[buff].Size);
                buffer.Stride = (ushort)BufferStrides[buff].Stride;
                this.Buffers.Add(buffer);
            }
        }


        private Vector4[] TryGetAttributeData(string name)
        {
            if (this.Attributes.ContainsKey(name))
                return Attributes[name].GetData(this);

            return new Vector4[0];
        }
    }

    public class BufferData
    {
        /// <summary>
        /// Tj
        /// </summary>
        public int Stride;

        public byte[] Data;
    }

    public class VertexBufferInfo : IResData
    {
        public uint Size; //Size in bytes of buffer

        public void Read(BinaryReader reader)
        {
            Size = reader.ReadUInt32();
            reader.BaseStream.Seek(12, SeekOrigin.Current);
        }
    }

    public class VertexStrideInfo : IResData
    {
        public uint Stride;

        public void Read(BinaryReader reader)
        {
            Stride = reader.ReadUInt32();
            reader.BaseStream.Seek(12, SeekOrigin.Current);
        }
    }

    public class VertexAttribute : IResData, INamed
    {
        /// <summary>
        /// The name of the vertex attribute.
        /// These typically format as _p0, where p is the type (position) and 0 is the channel number.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The format the data is read.
        /// </summary>
        public BfresAttribFormat Format { get; set; }
        /// <summary>
        /// The attribute offset when the buffer is interleaved.
        /// </summary>
        public ushort Offset { get; set; }
        /// <summary>
        /// The buffer index to load the attribute data.
        /// </summary>
        public ushort BufferIndex { get; set; }

        public void Read(BinaryReader reader)
        {
            Name = reader.ReadStringOffset(reader.ReadUInt64());
            Format = (BfresAttribFormat)reader.ReadUInt16BigEndian();
            reader.ReadUInt16();
            Offset = reader.ReadUInt16();
            BufferIndex = reader.ReadUInt16();
        }

        public override string ToString()
        {
            return $"Buffer[{BufferIndex}] Offset {Offset} Attribute {Name} Format {Format}";
        }

        /// <summary>
        /// Gets the buffer data in vecto4[] form as raw floats.
        /// </summary>
        public Vector4[] GetData(VertexBuffer vertexBuffer)
        {
            Vector4[] data = new Vector4[vertexBuffer.VertexCount];

            var buffer = vertexBuffer.Buffers[BufferIndex];
            using (var reader = new BinaryReader(new MemoryStream(buffer.Data)))
            {
                for (int i = 0; i < vertexBuffer.VertexCount; i++)
                {
                    reader.SeekBegin(i * buffer.Stride + Offset );
                    data[i] = reader.ReadAttribute(this.Format);
                }
            }
            return data;
        }
    }

    /// <summary>
    /// A shape for rendering and handling mesh data.
    /// </summary>
    public class Shape : IResData, INamed
    {
        /// <summary>
        /// The shape name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte SkinCount => header.MaxSkinInfluence;

        /// <summary>
        /// The index for the vertex buffer.
        /// </summary>
        internal int VertexBufferIndex
        {
            get { return header.VertexBufferIndex; }
        }

        /// <summary>
        /// The index for the material data.
        /// </summary>
        public int MaterialIndex
        {
            get { return header.MaterialIndex; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int BoneIndex
        {
            get { return header.BoneIndex; }
        }
        
        /// <summary>
        /// The mesh list for handling level of details.
        /// The first mesh is used by default without LODs.
        /// </summary>
        public List<Mesh> Meshes { get; set; } = new List<Mesh>();

        /// <summary>
        /// The vertex buffer for handling vertices.
        /// </summary>
        public VertexBuffer VertexBuffer { get; set; } = new VertexBuffer();

        /// <summary>
        /// Bounding boxes used to frustum check mesh LODs and submeshes.
        /// </summary>
        public List<ShapeBounding> BoundingBoxes { get; set; } = new List<ShapeBounding>();
        
        /// <summary>
        /// List of spheres for culling either per mesh or per bone for rigged models.
        /// </summary>
        public List<Vector4> BoundingSpheres { get; set; } = new List<Vector4>();

        private ShapeHeader header;

        public void Read(BinaryReader reader)
        {
            header = new ShapeHeader();
            reader.BaseStream.Read(Utils.AsSpan(ref header));
            long pos = reader.BaseStream.Position;

            Name = reader.ReadStringOffset(header.NameOffset);
            Meshes = reader.ReadArray<Mesh>(header.MeshArrayOffset, header.MeshCount);

            var numBounding = (int)Meshes.Sum(x => x.SubMeshes.Count + 1);

            BoundingSpheres = reader.ReadCustom(() =>
            {
                //Only load per mesh for now
                //Rigs can use per bone
                var numRadius = this.Meshes.Count; 

                Vector4[] values = new Vector4[numRadius];
                for (int i = 0; i < values.Length; i++)
                    values[i] = reader.ReadVector4();

                return values.ToList();
            }, header.BoundingSphereOffset);

            BoundingBoxes = reader.ReadArray<ShapeBounding>(header.BoundingBoxOffset, numBounding);
            reader.SeekBegin(pos);
        }

        public void Init(BinaryReader reader, VertexBuffer vertexBuffer, BufferMemoryPool bufferInfo)
        {
            this.VertexBuffer = vertexBuffer;

            vertexBuffer.InitBuffers(reader, bufferInfo);
            foreach (var mesh in Meshes)
                mesh.InitBuffers(reader, bufferInfo);
        }
    }
    public class Mesh : IResData
    {
        public uint IndexCount => header.IndexCount;
        public BfresIndexFormat IndexFormat => header.IndexFormat;
        public BfresPrimitiveType PrimitiveType => header.PrimType;

        /// <summary>
        /// The raw mesh buffer.
        /// </summary>
        public byte[] IndexBuffer;

        /// <summary>
        /// Gets the indices as raw uint[] format.
        /// </summary>
        /// <returns></returns>
        public uint[] GetIndices()
        {
            using (var reader = new BinaryReader(new MemoryStream(IndexBuffer)))
            {
                uint[] indices = new uint[header.IndexCount];
                for (int i = 0; i < header.IndexCount; i++)
                {
                    switch (header.IndexFormat)
                    {
                        case BfresIndexFormat.UnsignedByte: indices[i] = reader.ReadByte(); break;
                        case BfresIndexFormat.UInt16: indices[i] = reader.ReadUInt16(); break;
                        case BfresIndexFormat.UInt32: indices[i] = reader.ReadUInt32(); break;
                    }
                }
                return indices;
            }
        }

        /// <summary>
        /// Gets a list of sub meshes for culling individual regions of an entire model.
        /// </summary>
        public List<SubMesh> SubMeshes = new List<Bfres.SubMesh>();

        private IndexBufferInfo BufferInfo;

        private MeshHeader header;

        public void Read(BinaryReader reader)
        {
            header = new MeshHeader();
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            reader.SeekBegin(header.BufferInfoOffset);
            BufferInfo = reader.Read<IndexBufferInfo>();

            reader.SeekBegin(header.SubMeshArrayOffset);
            SubMeshes = reader.ReadArray<SubMesh>(header.SubMeshCount);
        }

        public void InitBuffers(BinaryReader reader, BufferMemoryPool bufferPoolInfo)
        {
            reader.SeekBegin((long)bufferPoolInfo.Offset + (int)header.BufferOffset);
            this.IndexBuffer = reader.ReadBytes((int)this.BufferInfo.Size);
        }
    }

    public class ShapeBounding : IResData
    {
        public Vector3 Center;
        public Vector3 Extent;

        public void Read(BinaryReader reader)
        {
            Center = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Extent = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public class IndexBufferInfo : IResData
    {
        public uint Size;
        public uint Flag;

        public void Read(BinaryReader reader)
        {
            Size = reader.ReadUInt32();
            Flag = reader.ReadUInt32();
            reader.BaseStream.Seek(40, SeekOrigin.Current);
        }
    }

    public class SubMesh : IResData
    {
        public uint Offset;
        public uint Count;

        public void Read(BinaryReader reader)
        {
            Offset = reader.ReadUInt32();
            Count = reader.ReadUInt32();
        }
    }
}
