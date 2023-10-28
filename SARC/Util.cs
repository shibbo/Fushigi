using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.SARC
{

    internal static class Utils
    {
        public static BinaryReader AsBinaryReader(this Stream stream)
        {
            return new BinaryReader(stream);
        }

        public static string ReadString(this BinaryReader reader)
        {
            List<byte> bytes = new List<byte>();

            byte cur;

            while ((cur = reader.ReadByte()) != 0)
            {
                bytes.Add(cur);
            }

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        public static byte[] ReadBytes(this BinaryReader reader, uint length)
        {
            byte[] arr = new byte[length];

            for (uint i = 0; i < length; i++)
            {
                arr[i] = reader.ReadByte();
            }

            return arr;
        }

        public static Span<byte> AsSpan<T>(ref T val) where T : unmanaged
        {
            Span<T> valSpan = MemoryMarshal.CreateSpan(ref val, 1);
            return MemoryMarshal.Cast<T, byte>(valSpan);
        }
    }
}
