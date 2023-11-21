using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.gl
{
    public class UniformBlock : BufferObject
    {
        private GL _gl;

        public UniformBlock(GL gl) : base(gl, BufferTargetARB.UniformBuffer)
        {
            _gl = gl;
        }

        public void Render(uint programID, string name, int binding = -1)
        {
            uint index = _gl.GetUniformBlockIndex(programID, name);
            Render(programID, index, binding);
        }

        public void Render(uint programID, uint index, int binding = -1)
        {
            if (index == uint.MaxValue)
                return;

            binding = binding != -1 ? binding : (int)index;

            _gl.UniformBlockBinding(programID, index, (uint)binding);
            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)binding, ID);
        }
    }
}
