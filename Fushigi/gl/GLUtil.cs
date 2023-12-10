using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class GLUtil
    {
        public static void Label(GL gl, ObjectIdentifier type, uint id, string text)
        {

            gl.ObjectLabel(type, id, (uint)text.Length, text);
        }
    }
}
