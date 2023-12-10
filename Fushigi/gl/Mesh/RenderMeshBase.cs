using Fushigi.ui.widgets;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Mesh
{
    public class RenderMeshBase
    {
        public int DrawCount { get; internal set; }
        public bool IsDisposed { get; private set; }

        private PrimitiveType primitiveType;

        internal BufferObject indexBufferData = null;

        internal GL _gl;

        public RenderMeshBase(GL gl, PrimitiveType type) {
            primitiveType = type;
            _gl = gl;
        }

        public void UpdatePrimitiveType(PrimitiveType type) {
            primitiveType = type;
        }

        internal void DrawSolidColor(LevelViewport viewport)
        {
            BasicMaterial material = new BasicMaterial();
            material.Render(_gl, viewport.GetCameraMatrix());

            Draw(material.Shader);
        }

        public void Draw(GLShader shader = null)
        {
            Draw(shader, DrawCount, 0);
        }

        public unsafe void Draw(GLShader shader, int count, int offset = 0)
        {
            //Skip if count is empty
            if (count == 0 || IsDisposed)
                return;

            PrepareAttributes(shader);
            BindVAO();

            if (indexBufferData != null)
                _gl.DrawElements(primitiveType, (uint)count, DrawElementsType.UnsignedInt, (void*)offset);
            else
                _gl.DrawArrays(primitiveType, offset, (uint)count);
        }

        protected virtual void BindVAO()
        {
        }

        protected virtual void PrepareAttributes(GLShader shader)
        {
        }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }
    }
}
