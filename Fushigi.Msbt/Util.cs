using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Msbt
{

    internal static class Utils
    {
        public static Span<byte> AsSpan<T>(ref T val) where T : unmanaged
        {
            Span<T> valSpan = MemoryMarshal.CreateSpan(ref val, 1);
            return MemoryMarshal.Cast<T, byte>(valSpan);
        }

        public static void Align(this BinaryReader reader, uint amount)
        {
            while (reader.BaseStream.Position % amount != 0 && reader.BaseStream.Position != reader.BaseStream.Length)
                reader.ReadByte();
        }

        public static void Align(this BinaryWriter writer, uint amount)
        {
            writer.Write(new byte[(int)(-writer.BaseStream.Position % amount + amount) % amount]);
        }

        public static void WriteOffset(this BinaryWriter writer, long pos, long startPosition)
        {
            var base_pos = writer.BaseStream.Position;

            writer.BaseStream.Seek(pos, SeekOrigin.Begin);
            writer.Write((uint)(base_pos - startPosition));

            writer.BaseStream.Seek(base_pos, SeekOrigin.Begin);
        }

        public static void WriteSize(this BinaryWriter writer, long pos, uint size)
        {
            var base_pos = writer.BaseStream.Position;

            writer.BaseStream.Seek(pos, SeekOrigin.Begin);
            writer.Write(size);

            writer.BaseStream.Seek(base_pos, SeekOrigin.Begin);
        }
    }
}
