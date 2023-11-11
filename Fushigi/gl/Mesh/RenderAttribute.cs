using System;
using System.Collections.Generic;
using System.Reflection;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Fushigi.gl
{
    [AttributeUsage(AttributeTargets.Field)]
    public class RenderAttribute : Attribute
    {
        /// <summary>
        /// The name of the attribute.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The location the attribute is at in the shader.
        /// </summary>
        public int Location { get; protected set; }

        /// <summary>
        /// The format type of the attribute.
        /// </summary>
        public VertexAttribPointerType Type { get; private set; }

        /// <summary>
        /// The offset in the buffer.
        /// </summary>
        public int? Offset { get; protected set; }

        /// <summary>
        /// The total size of the attribute .
        /// </summary>
        public int Size
        {
            get { return ElementCount * GetFormatStride(); }
        }

        /// <summary>
        /// The index of the buffer the data is inside of.
        /// </summary>
        public int BufferIndex { get; set; }

        private int GetFormatStride()
        {
            switch (Type)
            {
                case VertexAttribPointerType.Byte:
                case VertexAttribPointerType.UnsignedByte:
                    return 1;
                case VertexAttribPointerType.HalfFloat:
                case VertexAttribPointerType.Short:
                    return 2;
                case VertexAttribPointerType.Float:
                case VertexAttribPointerType.Int:
                    return 4;
                default:
                    throw new Exception($"Could not set format stride. Format not supported! {Type}");
            }
        }

        /// <summary>
        /// The number of elements in an attribute.
        /// </summary>
        public int ElementCount { get; protected set; }

        /// <summary>
        /// Determines to normalize the attribute data.
        /// </summary>
        public bool Normalized { get; set; }

        public RenderAttribute() { }

        public RenderAttribute(string attributeName, VertexAttribPointerType attributeFormat, int offset, int count)
        {
            Name = attributeName;
            Type = attributeFormat;
            Offset = offset;
            ElementCount = count;
        }

        public RenderAttribute(string attributeName, VertexAttribPointerType attributeFormat = VertexAttribPointerType.Float)
        {
            Name = attributeName;
            Type = attributeFormat;
        }

        public RenderAttribute(string attributeName, VertexAttribPointerType attributeFormat, int offset)
        {
            Name = attributeName;
            Type = attributeFormat;
            Offset = offset;
        }

        public RenderAttribute(int attributeLocation, VertexAttribPointerType attributeFormat, int offset)
        {
            Location = attributeLocation;
            Type = attributeFormat;
            Offset = offset;
        }

        public static RenderAttribute[] GetAttributes<T>()
        {
            List<RenderAttribute> attributes = new List<RenderAttribute>();
            //Direct types
            if (typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector3) || typeof(T) == typeof(Vector4))
            {
                var att = new RenderAttribute(0, VertexAttribPointerType.Float, 0);
                att.ElementCount = CalculateCount(typeof(T));
                attributes.Add(att);
                return attributes.ToArray();
            }

            //Seperate the buffer offsets through dictionaries
            Dictionary<int, int> bufferOffsets = new Dictionary<int, int>();
            var type = typeof(T);
            foreach (var field in type.GetFields())
            {
                RenderAttribute attribute = field.GetCustomAttribute<RenderAttribute>();
                if (attribute == null)
                    continue;

                if (!bufferOffsets.ContainsKey(attribute.BufferIndex))
                    bufferOffsets.Add(attribute.BufferIndex, 0);

                int offset = bufferOffsets[attribute.BufferIndex];

                //Calculate the field size and type amount
                attribute.ElementCount = CalculateCount(field.FieldType);
                //Set offset automatically if necessary
                if (attribute.Offset == null)
                {
                    attribute.Offset += offset;
                    bufferOffsets[attribute.BufferIndex] += attribute.Size;
                }

                attributes.Add(attribute);
            }
            return attributes.ToArray();
        }

        static int CalculateCount(Type type)
        {
            if (type == typeof(Vector2)) return 2;
            if (type == typeof(Vector3)) return 3;
            if (type == typeof(Vector4)) return 4;
            return 1;
        }
    }
}
