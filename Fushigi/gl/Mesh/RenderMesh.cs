using Fushigi.gl.Mesh;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.gl
{
    public class RenderMesh<TVertex> : RenderMeshBase where TVertex : struct
    {
        private RenderAttribute[] attributes = new RenderAttribute[0];
        private VertexArrayObject vao = null;

        public List<BufferObject> VertexBuffers = new List<BufferObject>();

        public RenderMesh(GL gl, TVertex[] vertices, int[] indices = null, 
            PrimitiveType primitiveType = PrimitiveType.Triangles) : base(gl, primitiveType)
        {
            Init(vertices, indices);
        }

        protected override void BindVAO()
        {
            vao.Use();
        }

        protected override void PrepareAttributes(GLShader shader)
        {
            vao.Enable(shader);
        }

        protected void Init<TIndex>(TVertex[] vertices, TIndex[] indices = null) where TIndex : unmanaged
        {
            //Search for attributes in the given vertex type
            attributes = RenderAttribute.GetAttributes<TVertex>();
            //Set the vertex stride
            var vertexStride = attributes.Sum(x => x.Size);
            //Set the draw count as number of vertices for direct array types
            DrawCount = vertices.Length;

            //Setup indices if used
            if (indices != null)
            {
                indexBufferData = new BufferObject(_gl,  BufferTargetARB.ElementArrayBuffer);
                indexBufferData.SetData(indices, BufferUsageARB.StaticDraw);
                //Use index count for draw amount
                DrawCount = indices.Length;
            }

            BufferObject vertexBuffer = new BufferObject(_gl, BufferTargetARB.ArrayBuffer);
            vertexBuffer.SetData(vertices, BufferUsageARB.StaticDraw);
            this.VertexBuffers.Add(vertexBuffer);

            //Init the attributes into gl data
            if (indexBufferData != null)
                vao = new VertexArrayObject(_gl, VertexBuffers, indexBufferData);
            else
                vao = new VertexArrayObject(_gl, VertexBuffers);

            foreach (var att in attributes)
            {
                if (!string.IsNullOrEmpty(att.Name))
                    vao.AddAttribute(att.Name, att.ElementCount, att.Type, att.Normalized, vertexStride, att.Offset.Value);
                else
                    vao.AddAttribute((uint)att.Location, att.ElementCount, att.Type, att.Normalized, vertexStride, att.Offset.Value);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            //Dispose all buffer objects
            foreach (var buffer in this.VertexBuffers)
                buffer?.Dispose();
            //Dispose index buffer
            indexBufferData?.Dispose();
            //Dispose vertex array
            vao?.Dispose();
        }
    }
}
