using Fushigi.Bfres.Common;
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
        public ResDict<Shape> Shapes { get; set; } = new ResDict<Shape>();

        /// <summary>
        /// 
        /// </summary>
        public ResDict<Material> Materials { get; set; } = new ResDict<Material>();

        /// <summary>
        /// A list of vertex buffers used for loading vertex data for shapes.
        /// </summary>
        internal List<VertexBuffer> VertexBuffers { get; set; } = new List<VertexBuffer>();

        public void Read(BinaryReader reader)
        {
            var header = new ModelHeader();
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            long pos = reader.BaseStream.Position;

            Name = reader.ReadStringOffset(header.NameOffset);
            Console.WriteLine($"Model - {Name} -");

            VertexBuffers = reader.ReadArray<VertexBuffer>(header.VertexArrayOffset, header.VertexBufferCount);

            Shapes = reader.ReadDictionary<Shape>(header.ShapeDictionaryOffset, header.ShapeArrayOffset);
            Materials = reader.ReadDictionary<Material>(header.MaterialDictionaryOffset, header.MaterialArrayOffset);

            //return
            reader.SeekBegin(pos);
        }

        internal void Init(BinaryReader reader, BufferMemoryPool memoryInfo)
        {
            //Prepare each shape and setup the memory for each buffer
            foreach (Shape shape in Shapes.Values)
                shape.Init(reader, VertexBuffers[shape.VertexBufferIndex], memoryInfo);
        }
    }
}
