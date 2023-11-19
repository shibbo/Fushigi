using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace Fushigi.gl
{
    public class VertexArrayObject : IDisposable
    {
        private uint _handle;
        private GL _gl;
        private Dictionary<object, VertexAttribute> attributes;
        private List<BufferObject> Buffers = new List<BufferObject>();
        private BufferObject IndexBuffer;

        public VertexArrayObject(GL gl, BufferObject vbo)
        {
            Init(gl);
            Buffers.Add(vbo);
        }

        public VertexArrayObject(GL gl, BufferObject vbo, BufferObject ebo)
        {
            Init(gl);
            IndexBuffer = ebo;
            Buffers.Add(vbo);
        }

        public VertexArrayObject(GL gl, List<BufferObject> vbo)
        {
            Init(gl);
            Buffers.AddRange(vbo);
        }

        public VertexArrayObject(GL gl, List<BufferObject> vbo, BufferObject ebo)
        {
            Init(gl);
            Buffers.AddRange(vbo);
            IndexBuffer = ebo;
        }

        private void Init(GL gl)
        {
            _gl = gl;
            attributes = new Dictionary<object, VertexAttribute>();
            _handle = _gl.GenVertexArray();
        }

        public void Clear()
        {
            attributes.Clear();
        }

        public void AddAttribute(uint location, int size, VertexAttribPointerType type, bool normalized, int stride, int offset, int bufferIndex = 0)
        {
            attributes.Add(location, new VertexAttribute(size, type, normalized, stride, offset, normalized, 1, bufferIndex));
        }

        public void AddAttribute(string name, int size, VertexAttribPointerType type, bool normalized, int stride, int offset, int bufferIndex = 0)
        {
            attributes.Add(name, new VertexAttribute(size, type, normalized, stride, offset, normalized, 1, bufferIndex));
        }

        public void Initialize()
        {

        }

        public void Enable(GLShader shader)
        {
            _gl.BindVertexArray(_handle);
            EnableAttributes(shader, attributes);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);
        }

        private unsafe void EnableAttributes(GLShader shader, Dictionary<object, VertexAttribute> attributes)
        {
            foreach (KeyValuePair<object, VertexAttribute> a in attributes)
            {
                //Keys can either be set by an index or string to find in the shader
                if (a.Key is string)
                    VertexAttributePointer(shader, (string)a.Key,a.Value);
                else
                    VertexAttributePointer((uint)a.Key, a.Value);
            }
        }

        private unsafe void VertexAttributePointer(GLShader shader, string name, VertexAttribute attr)
        {
            if (shader == null)
                return;

            var index = shader.GetAttribute((string)name);
            if (index == -1)
                return;

            VertexAttributePointer((uint)index, attr);
        }

        private unsafe void VertexAttributePointer(uint index, VertexAttribute attr)
        {
            if (Buffers[attr.bufferIndex].Target != BufferTargetARB.ArrayBuffer)
                throw new Exception($"Input buffer not an array buffer type!");

            _gl.EnableVertexAttribArray(index);
            Buffers[attr.bufferIndex].Bind();

            if (attr.type == VertexAttribPointerType.Int)
                _gl.VertexAttribIPointer(index, attr.elementCount, (VertexAttribIType)attr.type, attr.stride, (void*)(attr.offset));
            else
                _gl.VertexAttribPointer(index, attr.elementCount, attr.type, attr.normalized, attr.stride, (void*)(attr.offset));

            _gl.VertexAttribDivisor(index, 0);
        }

        public void Use()
        {
            _gl.BindVertexArray(_handle);
            if (IndexBuffer != null)
                IndexBuffer.Bind();
        }


        public void Bind()
        {
            _gl.BindVertexArray(_handle);
        }

        public void Unbind()
        {
            _gl.BindVertexArray(9);
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
        }

        struct VertexAttribute
        {
            public int elementCount;
            public VertexAttribPointerType type;
            public bool normalized;
            public uint stride;
            public uint offset;
            public bool instance;
            public int divisor;
            public int bufferIndex;

            public VertexAttribute(int size, VertexAttribPointerType type, bool normalized, int stride, int offset, bool instance = false, int divisor = 1, int bufferIndex = 0)
            {
                this.elementCount = size;
                this.type = type;
                this.normalized = normalized;
                this.stride = (uint)stride;
                this.offset = (uint)offset;
                this.instance = instance;
                this.divisor = divisor;
                this.bufferIndex = bufferIndex;
            }
        }
    }
}