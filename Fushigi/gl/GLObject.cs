using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class GLObject
    {
        public uint ID { get; private set; }

        public GLObject(uint id)
        {
            ID = id;
        }
    }
}
