using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres.Texture
{
    public class AstcFile
    {
        const int MagicFileConstant = 0x5CA1AB13;

        public static void SaveAstc(string filePath, SurfaceFormat format, uint width, uint height, byte[] data)
        {
            byte blockDimX = (byte)TegraX1Swizzle.GetBlockWidth(format);
            byte blockDimY = (byte)TegraX1Swizzle.GetBlockHeight(format);

            SaveAstc(filePath, blockDimX, blockDimY, width, height, data);
        }

        public static void SaveAstc(string filePath, byte blockDimX, byte blockDimY, uint width, uint height, byte[] data)
        {
            var mem = new MemoryStream();
            var writer = mem.AsBinaryWriter();

            writer.Write(MagicFileConstant);
            writer.Write(blockDimX);
            writer.Write(blockDimY);
            writer.Write((byte)1);
            writer.Write(IntTo3Bytes((int)width));
            writer.Write(IntTo3Bytes((int)height));
            writer.Write(IntTo3Bytes((int)1));
            writer.Write(data);

            File.WriteAllBytes(filePath, mem.ToArray());
        }

        private static byte[] IntTo3Bytes(int value)
        {
            byte[] newValue = new byte[3];
            newValue[0] = (byte)(value & 0xFF);
            newValue[1] = (byte)((value >> 8) & 0xFF);
            newValue[2] = (byte)((value >> 16) & 0xFF);
            return newValue;
        }
    }
}
