using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.Bfres
{
    public class Model : IResData, INamed
    {
        /// <summary>
        /// The name of the model.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A list of shapes for drawing meshes.
        /// </summary>
        public List<Shape> Shapes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<Material> Materials { get; set; }

        /// <summary>
        /// A list of vertex buffers used for loading vertex data for shapes.
        /// </summary>
        internal List<VertexBuffer> VertexBuffers { get; set; }

        public void Read(BinaryReader reader)
        {
            var header = new ModelHeader();
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            long pos = reader.BaseStream.Position;

            Name = reader.ReadStringOffset(header.NameOffset);
            Console.WriteLine($"Model - {Name} -");

            VertexBuffers = reader.ReadArray<VertexBuffer>(header.VertexArrayOffset, header.VertexBufferCount);
            Shapes = reader.ReadArray<Shape>(header.ShapeArrayOffset, header.ShapeCount);
            Materials = reader.ReadArray<Material>(header.MaterialArrayOffset, header.MaterialCount);

            //return
            reader.SeekBegin(pos);
        }

        internal void Init(BinaryReader reader, BufferMemoryPool memoryInfo)
        {
            //Prepare each shape and setup the memory for each buffer
            foreach (var shape in Shapes)
                shape.Init(reader, VertexBuffers[shape.VertexBufferIndex], memoryInfo);
        }
    }
}
