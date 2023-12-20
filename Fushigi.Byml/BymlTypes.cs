using System.Runtime.InteropServices;

namespace Fushigi.Byml
{
    public enum BymlNodeId : byte
    {
        String = 0xA0,
        Bin = 0xA1,
        Array = 0xC0,
        Hash = 0xC1,
        StringTable = 0xC2,
        PathArray = 0xC3,   /* Obscure, only observed in MK8DX. */
        Bool = 0xD0,
        Int = 0xD1,
        Float = 0xD2,
        UInt = 0xD3,
        Int64 = 0xD4,
        UInt64 = 0xD5,
        Double = 0xD6,
        Null = 0xFF,
    };

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BymlHeader
    {
        public ushort Magic;
        public ushort Version;
        public uint HashKeyOffset;
        public uint StringTableOffset;
        public uint RootOrPathArrayOffset;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0xC)]
    public struct Vector3
    {
        public float X, Y, Z;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x1C)]

    public struct BymlPathPoint
    {
        public Vector3 Position;
        public Vector3 Normal;
        public uint Unk;
    }

    public struct BymlHashPair : IComparable<string>, IComparable<BymlHashPair>
    {
        public string Name;
        public BymlNodeId Id;
        public IBymlNode Value;

        public int CompareTo(string? other)
        {
            if (other == null) return 1;
            return string.CompareOrdinal(Name, other);
        }

        public int CompareTo(BymlHashPair other)
        {
            return CompareTo(other.Name);
        }
    }

    public interface IBymlNode
    {
        public BymlNodeId Id { get; }
    }

    public interface IBymlValueNode
    {
        public object GetValue();
    }

    public class BymlNode<T> : IBymlNode, IBymlValueNode
    {
        public BymlNodeId Id { get; }
        public T Data;
        object IBymlValueNode.GetValue() => Data!;

        public BymlNode(BymlNodeId id, T data)
        {
            Id = id;
            Data = data;
        }
    }
}
