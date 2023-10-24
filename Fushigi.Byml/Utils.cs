using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Fushigi.Byml
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

        public static uint ReadUInt24(this BinaryReader reader)
        {
            /* Read out 3 bytes into a sizeof(uint) buffer. */
            Span<byte> bytes = stackalloc byte[3];
            reader.BaseStream.Read(bytes[..^1]);

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

        public static T As<T>(this IBymlNode bymlNode)
        {
            return ((BymlNode<T>)bymlNode).Data;
        }

        public static T GetBigValue<T>(this IBymlNode bymlNode)
        {
            return ((BymlBigDataNode<T>)bymlNode).Value;
        }
    }
}
