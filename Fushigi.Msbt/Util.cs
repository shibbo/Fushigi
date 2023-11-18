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
            while (writer.BaseStream.Position % amount != 0 && writer.BaseStream.Position != writer.BaseStream.Length)
                writer.Write((byte)0);
        }
    }
}
