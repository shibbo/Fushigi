using Ryujinx.Graphics.Texture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres.Texture
{
    public class BCEncoder
    {
        public static byte[] Encode(byte[] decoded, int width, int height)
        {
            return BCnEncoder.EncodeBC7(decoded, width, height, 1, 1, 1);
        }
    }
}
