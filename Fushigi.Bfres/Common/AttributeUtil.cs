using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    internal static class AttributeUtil
    {
        public static Vector4 ReadAttribute(this BinaryReader reader, BfresAttribFormat format)
        {
            //TODO clean this up better

            switch (format)
            {
                case BfresAttribFormat.Format_10_10_10_2_SNorm: return Read_10_10_10_2_SNorm(reader);

                case BfresAttribFormat.Format_8_UNorm: return new Vector4(Read_8_UNorm(reader), 0, 0, 0);
                case BfresAttribFormat.Format_8_UInt: return new Vector4(Read_8_Uint(reader), 0, 0, 0);
                case BfresAttribFormat.Format_8_SNorm: return new Vector4(Read_8_Snorm(reader), 0, 0, 0);
                case BfresAttribFormat.Format_8_SInt: return new Vector4(Read_8_Sint(reader), 0, 0, 0);
                case BfresAttribFormat.Format_8_SIntToSingle: return new Vector4(Read_8_Sint(reader), 0, 0, 0);
                case BfresAttribFormat.Format_8_UIntToSingle: return new Vector4(Read_8_Uint(reader), 0, 0, 0);

                case BfresAttribFormat.Format_16_UNorm: return new Vector4(Read_16_UNorm(reader), 0, 0, 0);
                case BfresAttribFormat.Format_16_UInt: return new Vector4(Read_16_Uint(reader), 0, 0, 0);
                case BfresAttribFormat.Format_16_SNorm: return new Vector4(Read_16_Snorm(reader), 0, 0, 0);
                case BfresAttribFormat.Format_16_SInt: return new Vector4(Read_16_Sint(reader), 0, 0, 0);
                case BfresAttribFormat.Format_16_SIntToSingle: return new Vector4(Read_16_Sint(reader), 0, 0, 0);
                case BfresAttribFormat.Format_16_UIntToSingle: return new Vector4(Read_16_Uint(reader), 0, 0, 0);

                case BfresAttribFormat.Format_8_8_UNorm: return new Vector4(Read_8_UNorm(reader), Read_8_UNorm(reader), 0, 0);
                case BfresAttribFormat.Format_8_8_UInt: return new Vector4(Read_8_Uint(reader), Read_8_Uint(reader), 0, 0);
                case BfresAttribFormat.Format_8_8_SNorm: return new Vector4(Read_8_Snorm(reader), Read_8_Snorm(reader), 0, 0);
                case BfresAttribFormat.Format_8_8_SInt: return new Vector4(Read_8_Sint(reader), Read_8_Sint(reader), 0, 0);
                case BfresAttribFormat.Format_8_8_SIntToSingle: return new Vector4(Read_8_Sint(reader), Read_8_Sint(reader), 0, 0);
                case BfresAttribFormat.Format_8_8_UIntToSingle: return new Vector4(Read_8_Uint(reader), Read_8_UNorm(reader), 0, 0);

                case BfresAttribFormat.Format_8_8_8_8_UNorm: return new Vector4(Read_8_UNorm(reader), Read_8_UNorm(reader), Read_8_UNorm(reader), Read_8_UNorm(reader));
                case BfresAttribFormat.Format_8_8_8_8_UInt: return new Vector4(Read_8_Uint(reader), Read_8_Uint(reader), Read_8_Uint(reader), Read_8_Uint(reader));
                case BfresAttribFormat.Format_8_8_8_8_SNorm: return new Vector4(Read_8_Snorm(reader), Read_8_Snorm(reader), Read_8_Snorm(reader), Read_8_Snorm(reader));
                case BfresAttribFormat.Format_8_8_8_8_SInt: return new Vector4(Read_8_Sint(reader), Read_8_Sint(reader), Read_8_Sint(reader), Read_8_Sint(reader));
                case BfresAttribFormat.Format_8_8_8_8_SIntToSingle: return new Vector4(Read_8_Sint(reader), Read_8_Sint(reader), Read_8_Sint(reader), Read_8_Sint(reader));
                case BfresAttribFormat.Format_8_8_8_8_UIntToSingle: return new Vector4(Read_8_Uint(reader), Read_8_Uint(reader), Read_8_Uint(reader), Read_8_Uint(reader));

                case BfresAttribFormat.Format_16_16_UNorm: return new Vector4(Read_16_UNorm(reader), Read_16_UNorm(reader), 0, 0);
                case BfresAttribFormat.Format_16_16_UInt: return new Vector4(Read_16_Uint(reader), Read_16_Uint(reader), 0, 0);
                case BfresAttribFormat.Format_16_16_SNorm: return new Vector4(Read_16_Snorm(reader), Read_16_Snorm(reader), 0, 0);
                case BfresAttribFormat.Format_16_16_SInt: return new Vector4(Read_16_Sint(reader), Read_16_Sint(reader), 0, 0);
                case BfresAttribFormat.Format_16_16_SIntToSingle: return new Vector4(Read_16_Sint(reader), Read_16_Sint(reader), 0, 0);
                case BfresAttribFormat.Format_16_16_UIntToSingle: return new Vector4(Read_16_Uint(reader), Read_16_Uint(reader), 0, 0);

                case BfresAttribFormat.Format_16_16_Single: return new Vector4(reader.ReadHalfFloat(), reader.ReadHalfFloat(), 0, 0);
                case BfresAttribFormat.Format_16_16_16_16_Single: return new Vector4(reader.ReadHalfFloat(), reader.ReadHalfFloat(), reader.ReadHalfFloat(), reader.ReadHalfFloat());

                case BfresAttribFormat.Format_32_UInt: return new Vector4(reader.ReadUInt32(), 0, 0, 0);
                case BfresAttribFormat.Format_32_SInt: return new Vector4(reader.ReadInt32(), 0, 0, 0);
                case BfresAttribFormat.Format_32_Single: return new Vector4(reader.ReadSingle(), 0, 0, 0);

                case BfresAttribFormat.Format_32_32_UInt: return new Vector4(reader.ReadUInt32(), reader.ReadUInt32(), 0, 0);
                case BfresAttribFormat.Format_32_32_SInt: return new Vector4(reader.ReadInt32(), reader.ReadInt32(), 0, 0);
                case BfresAttribFormat.Format_32_32_Single: return new Vector4(reader.ReadSingle(), reader.ReadSingle(), 0, 0);

                case BfresAttribFormat.Format_32_32_32_UInt: return new Vector4(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), 0);
                case BfresAttribFormat.Format_32_32_32_SInt: return new Vector4(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), 0);
                case BfresAttribFormat.Format_32_32_32_Single: return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), 0);

                case BfresAttribFormat.Format_32_32_32_32_UInt: return new Vector4(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
                case BfresAttribFormat.Format_32_32_32_32_SInt: return new Vector4(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                case BfresAttribFormat.Format_32_32_32_32_Single: return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            return Vector4.Zero;
        }

        private static Vector4 Read_10_10_10_2_SNorm(BinaryReader reader)
        {
            int value = reader.ReadInt32();
            return new Vector4(
                (value << 22 >> 22) / 511f,
                (value << 12 >> 22) / 511f,
                (value << 2 >> 22) / 511f,
                value >> 30);
        }

        private static float Read_8_UNorm(BinaryReader reader) => reader.ReadByte() / 255f;
        private static float Read_8_Uint(BinaryReader reader) => reader.ReadByte();
        private static float Read_8_Snorm(BinaryReader reader) => reader.ReadInt16() / 127f;
        private static float Read_8_Sint(BinaryReader reader) => reader.ReadSByte();

        private static float Read_16_UNorm(BinaryReader reader) => reader.ReadUInt16() / 65535f;
        private static float Read_16_Uint(BinaryReader reader) => reader.ReadUInt16();
        private static float Read_16_Snorm(BinaryReader reader) => reader.ReadSByte() / 32767f;
        private static float Read_16_Sint(BinaryReader reader) => reader.ReadInt16();
    }
}
