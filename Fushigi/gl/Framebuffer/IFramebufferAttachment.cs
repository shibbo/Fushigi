using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public interface IFramebufferAttachment
    {
        uint Width { get; }

        uint Height { get; }

        void Attach(FramebufferAttachment attachment, GLFramebuffer target);

        void Dispose();
    }
}
