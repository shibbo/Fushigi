using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class UniformBlock : BufferObject
    {
        public UniformBlock(GL gl) : base(gl, BufferTargetARB.UniformBuffer)
        {

        }
    }
}
