using Fushigi.Bfres.Common;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Fushigi.Bfres
{
    internal readonly ref struct TemporarySeekHandle
        (Stream stream, long retpos)
    {
        private readonly Stream Stream = stream;
        private readonly long RetPos = retpos;

        public readonly void Dispose()
        {
            Stream.Seek(RetPos, SeekOrigin.Begin);
        }
    }

    internal static class Utils
    {
        public static Span<byte> AsSpan<T>(ref T val) where T : unmanaged
        {
            Span<T> valSpan = MemoryMarshal.CreateSpan(ref val, 1);
            return MemoryMarshal.Cast<T, byte>(valSpan);
        }

        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static Vector4 ReadVector4(this BinaryReader reader)
        {
            return new Vector4(reader.ReadSingle(), reader.ReadSingle(), 
                reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this BinaryWriter writer, float[] values)
        {
            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static void Write(this BinaryWriter writer, uint[] values)
        {
            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static void Write(this BinaryWriter writer, int[] values)
        {
            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static void Write(this BinaryWriter writer, bool[] values)
        {
            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i]);
        }

        public static void AlignBytes(this BinaryWriter writer, int align)
        {
            var num = (writer.BaseStream.Position + (align - 1)) & ~(align - 1);
            writer.Write(new byte[num]);
        }

        public static sbyte[] ReadSbytes(this BinaryReader reader, int count)
        {
            sbyte[] values = new sbyte[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadSByte();
            return values;
        }

        public static bool[] ReadBooleans(this BinaryReader reader, int count)
        {
            bool[] values = new bool[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadBoolean();
            return values;
        }

        public static float[] ReadSingles(this BinaryReader reader, int count)
        {
            float[] values = new float[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadSingle();
            return values;
        }

        public static ushort[] ReadUInt16s(this BinaryReader reader, int count)
        {
            ushort[] values = new ushort[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadUInt16();
            return values;
        }

        public static int[] ReadInt32s(this BinaryReader reader, int count)
        {
            int[] values = new int[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadInt32();
            return values;
        }

        public static uint[] ReadUInt32s(this BinaryReader reader, int count)
        {
            uint[] values = new uint[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadUInt32();
            return values;
        }

        public static long[] ReadInt64s(this BinaryReader reader, int count)
        {
            long[] values = new long[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadInt64();
            return values;
        }

        public static ulong[] ReadUInt64s(this BinaryReader reader, int count)
        {
            ulong[] values = new ulong[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadUInt64();
            return values;
        }

        public static T ReadCustom<T>(this BinaryReader reader, Func<T> value, ulong offset)
        {
            if (offset == 0) return default(T);

            long pos = reader.BaseStream.Position;

            reader.SeekBegin((long)offset);

            var result = value.Invoke();

            reader.SeekBegin((long)pos);

            return result;
        }

        public static ResDict<T> ReadDictionary<T>(this BinaryReader reader, ulong offset) where T : IResData, new()
        {
            if (offset == 0) return new ResDict<T>();

            reader.SeekBegin((long)offset);
            return ReadDictionary<T>(reader);
        }

        public static ResDict<T> ReadDictionary<T>(this BinaryReader reader, ulong offset, ulong valueOffset) where T : IResData, new()
        {
            if (offset == 0)
                return new ResDict<T>();

            reader.SeekBegin((long)offset);
            var dict = ReadDictionary<T>(reader);

            var list = ReadArray<T>(reader, valueOffset, dict.Keys.Count);

            for (int i = 0; i < list.Count; i++)
            {
                string key = dict.GetKey(i);
                dict[key] = list[i];
            }
            return dict;
        }

        public static ResDict<T> ReadDictionary<T>(this BinaryReader reader) where T : IResData, new()
        {
            return reader.Read<ResDict<T>>();
        }

        public static List<T> ReadArray<T>(this BinaryReader reader, ulong offset, int count) where T : IResData
        {
            long pos = reader.BaseStream.Position;

            reader.SeekBegin(offset);
            var list = ReadArray<T>(reader, count);

            //seek back
            reader.SeekBegin(pos);

            return list;
        }

        public static void Align(this BinaryReader reader, int alignment) 
        {
            reader.BaseStream.Seek((-reader.BaseStream.Position % alignment + alignment) % alignment, SeekOrigin.Current);
        }

        public static List<T> ReadArray<T>(this BinaryReader reader, int count) where T : IResData
        {
            List<T> list = new();
            for (int i = 0; i < count; i++)
                list.Add(reader.Read<T>());
            return list;
        }

        public static T Read<T>(this BinaryReader reader, ulong offset) where T : IResData
        {
            T instance = (T)Activator.CreateInstance(typeof(T));

            if (offset == 0)
                return instance;

            reader.SeekBegin((long)offset);
            instance.Read(reader);
            return instance;
        }

        public static T Read<T>(this BinaryReader reader) where T : IResData
        {
            T instance = (T)Activator.CreateInstance(typeof(T));
            instance.Read(reader);
            return instance;
        }

        public static void SeekBegin(this BinaryReader reader, long offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public static void SeekBegin(this BinaryReader reader, ulong offset)
        {
            reader.BaseStream.Seek((long)offset, SeekOrigin.Begin);
        }

        public static ushort ReadUInt16BigEndian(this BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(2);
            Array.Reverse(bytes); //Reverse bytes
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static bool[] ReadBooleanBits(this BinaryReader reader, int count)
        {
            bool[] booleans = new bool[count];

            int idx = 0;
            var bitFlags = reader.ReadInt64s(1 + count / 64);
            for (int i = 0; i < count; i++)
            {
                if (i != 0 && i % 64 == 0)
                    idx++;

                booleans[i] = (bitFlags[idx] & ((long)1 << i)) != 0;
            }
            return booleans;
        }

        public static List<string> ReadStringOffsets(this BinaryReader reader, int count)
        {
            string[] strings = new string[count];
            for (int i = 0; i < count; i++)
            {
                var offset = reader.ReadUInt64();
                strings[i] = reader.ReadStringOffset(offset);
            }
            return strings.ToList();
        }

        public static string ReadStringOffset(this BinaryReader reader, ulong offset)
        {
            long pos = reader.BaseStream.Position;

            reader.SeekBegin(offset);

            ushort size = reader.ReadUInt16();
            string value = reader.ReadUtf8Z();
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);

            return value;
        }

        public static uint ReadUInt24(this BinaryReader reader)
        {
            /* Read out 3 bytes into a sizeof(uint) buffer. */
            Span<byte> bytes = stackalloc byte[4];
            reader.BaseStream.Read(bytes[..^1]);

            bytes[3] = 0;
            /* Convert buffer into uint. */
            uint v = BitConverter.ToUInt32(bytes);

            return v;
        }
        public static void WriteUInt24(this BinaryWriter writer, uint value)
        {
            /* Build a byte array from the value. */
            Span<byte> bytes =
            [
                (byte)(value & 0xFF),
                (byte)(value >> 8 & 0xFF),
                (byte)(value >> 16 & 0xFF),
            ];

            /* Write array. */
            writer.BaseStream.Write(bytes);
        }
        public static T[] ReadArray<T>(this Stream stream, uint count) where T : struct
        {
            /* Read data. */
            T[] data = new T[count];

            /* Read into casted span. */
            stream.Read(MemoryMarshal.Cast<T, byte>(data));

            return data;
        }

        public static void WriteArray<T>(this Stream stream, ReadOnlySpan<T> array) where T : struct
        {
            stream.Write(MemoryMarshal.Cast<T, byte>(array));
        }

        public static TemporarySeekHandle TemporarySeek(this Stream stream)
        {
            return stream.TemporarySeek(0, SeekOrigin.Begin);
        }

        public static TemporarySeekHandle TemporarySeek(this Stream stream, long offset, SeekOrigin origin)
        {
            long ret = stream.Position;
            stream.Seek(offset, origin);
            return new TemporarySeekHandle(stream, ret);
        }

        public static int BinarySearch<T, K>(IList<T> arr, K v) where T : IComparable<K>
        {
            var start = 0;
            var end = arr.Count - 1;

            while (start <= end)
            {
                var mid = (start + end) / 2;
                var entry = arr[mid];
                var cmp = entry.CompareTo(v);

                if (cmp == 0)
                    return mid;
                if (cmp > 0)
                    end = mid - 1;
                else /* if (cmp < 0) */
                    start = mid + 1;
            }

            return ~start;
        }
        public static BinaryReader AsBinaryReader(this Stream stream)
        {
            return new BinaryReader(stream);
        }
        public static BinaryWriter AsBinaryWriter(this Stream stream)
        {
            return new BinaryWriter(stream);
        }

        public static string ReadUtf8(this BinaryReader reader, int size)
        {
            return Encoding.UTF8.GetString(reader.ReadBytes(size), 0, size);
        }

        public static string ReadUtf8Z(this BinaryReader reader, int maxLength = int.MaxValue)
        {
            long start = reader.BaseStream.Position;
            int size = 0;

            // Read until we hit the end of the stream (-1) or a zero
            while (reader.BaseStream.ReadByte() - 1 > 0 && size < maxLength)
            {
                size++;
            }

            reader.BaseStream.Position = start;
            string text = reader.ReadUtf8(size);
            reader.BaseStream.Position++; // Skip the null byte
            return text;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AlignUp(uint num, uint align)
        {
            return (num + (align - 1)) & ~(align - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignUp(int num, int align)
        {
            return (num + (align - 1)) & ~(align - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AlignUp(ulong num, ulong align)
        {
            return (num + (align - 1)) & ~(align - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long AlignUp(long num, long align)
        {
            return (num + (align - 1)) & ~(align - 1);
        }

        public static float ReadHalfFloat(this BinaryReader binaryReader)
        {
            return (float)binaryReader.ReadHalf();
        }
    }
}
