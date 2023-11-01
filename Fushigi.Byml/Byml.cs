namespace Fushigi.Byml
{
    public class Byml
    {
        private const BymlNodeId HashKeyTableId = BymlNodeId.StringTable;
        private const BymlNodeId StringTableId = BymlNodeId.StringTable;
        public static bool IsValidBymlNodeId(byte id) => Enum.IsDefined(typeof(BymlNodeId), id);

        /* Value nodes only store their data adjacent to the node. */
        public static bool IsValueBymlNode(BymlNodeId id)
        {
            return id switch
            {
                BymlNodeId.String or
                BymlNodeId.Bool or
                BymlNodeId.Int or
                BymlNodeId.Float or
                BymlNodeId.UInt or
                BymlNodeId.UInt64 or
                BymlNodeId.Int64 or
                BymlNodeId.Null => true,
                _ => false,
            };
        }

        public static bool HasTypePrefix(BymlNodeId id)
        {
            return id switch
            {
                BymlNodeId.StringTable or
                BymlNodeId.PathArray or
                BymlNodeId.Hash or
                BymlNodeId.Array => true,
                _ => false,
            };
        }

        public static bool IsValidRootNode(BymlNodeId? id)
        {
            return id is BymlNodeId.Array or BymlNodeId.Hash;
        }

        public static bool IsValidPathArrayNode(byte id)
        {
            return id == 0 || (BymlNodeId)id == BymlNodeId.PathArray;
        }

        private readonly BymlStringTable? HashKeyTable;
        private readonly BymlStringTable? StringTable;

        public IBymlNode Root;
        public IBymlNode? PathArray;

        private BymlHeader Header;
        public Byml(MemoryStream stream)
        {
            stream.Read(Utils.AsSpan(ref Header));

            if (Header.Magic != 0x4259)
                throw new Exception("Invalid BYML header! (Big endian is not supported)");

            /* Try to infer if there's another node offset that is actually the root node. */
            var possibleRootNodeOffset = stream.AsBinaryReader().ReadUInt32();
            var shiftedRootNodeProbable = false;
            if (0 < possibleRootNodeOffset && possibleRootNodeOffset < stream.Length - 1)
            {
                using (stream.TemporarySeek(possibleRootNodeOffset, SeekOrigin.Begin))
                {
                    shiftedRootNodeProbable = IsValidRootNode(ReadNodeId(stream));
                }
            }

            /* Try to get the hash key table. */
            if (Header.HashKeyOffset != 0)
                using (stream.TemporarySeek(Header.HashKeyOffset, SeekOrigin.Begin))
                {
                    if (ReadNodeId(stream) == HashKeyTableId)
                        HashKeyTable = (BymlStringTable)ParseNode(stream, HashKeyTableId);
                }

            /* Try to get the string table. */
            if (Header.StringTableOffset != 0)
                using (stream.TemporarySeek(Header.StringTableOffset, SeekOrigin.Begin))
                {
                    if (ReadNodeId(stream) == StringTableId)
                        StringTable = (BymlStringTable)ParseNode(stream, StringTableId);
                }

            void ParseRootNode(uint offset)
            {
                using (stream.TemporarySeek(offset, SeekOrigin.Begin))
                {
                    var id = ReadNodeId(stream);
                    if (!id.HasValue)
                        throw new InvalidDataException($"Invalid BYML node ID!");
                    if (!IsValidRootNode(id))
                        throw new InvalidDataException("Root node must be array or hash!");
                    Root = ParseNode(stream, id.Value);
                }
            }

            if (shiftedRootNodeProbable)
            {
                /* Let's really see if this is PathArray... */
                using (stream.TemporarySeek(Header.RootOrPathArrayOffset, SeekOrigin.Begin))
                {
                    var id = (byte)stream.ReadByte();
                    /* See if this is really a PathArray... */
                    if (IsValidPathArrayNode(id))
                    {
                        /* There is a PathArray, so use the other offset for root. */
                        PathArray = new BymlPathArrayNode(stream.AsBinaryReader());
                        ParseRootNode(possibleRootNodeOffset);
                    }
                    else
                    {
                        /* This is just the root node. */
                        ParseRootNode(Header.RootOrPathArrayOffset);
                    }
                }
            }
            else
            {
                /* This is just the root node. */
                ParseRootNode(Header.RootOrPathArrayOffset);
            }
        }
        public Byml(IBymlNode root)
        {
            this.Root = root;
        }

        public void Save(Stream stream)
        {
            BymlWriter writer = new BymlWriter();
            writer.PushIter(this.Root);
            writer.Write(stream);
        }

        internal static BymlNodeId? ReadNodeId(Stream stream)
        {
            byte bId = (byte)stream.ReadByte();
            if (!IsValidBymlNodeId(bId))
                return null;
            return (BymlNodeId)bId;
        }

        internal string GetFromStringTable(uint index)
        {
            if (StringTable == null)
                throw new InvalidDataException("No string table present!");
            if (StringTable.Strings.Count < index)
                throw new InvalidDataException("Out of bounds reference to the string table!");

            return StringTable.Strings[(int)index];
        }

        internal string GetFromHashKeyTable(uint index)
        {
            if (HashKeyTable == null)
                throw new InvalidDataException("No hash key table present!");
            if (HashKeyTable.Strings.Count < index)
                throw new InvalidDataException("Out of bounds reference to the hash key table!");

            return HashKeyTable.Strings[(int)index];
        }

        private class CachedNode
        {
            public IBymlNode Node;
            public long Length;

            public CachedNode(IBymlNode node, long length)
            {
                Node = node;
                Length = length;
            }
        }

        private readonly Dictionary<long, CachedNode> NodeCache = new();

        internal IBymlNode ReadNode(BinaryReader reader, BymlNodeId id)
        {
            var stream = reader.BaseStream;
            /* Regular nodes are just put right after. */
            if (IsValueBymlNode(id))
                return ParseNode(stream, id);
            else
            {
                /* Go to offset where the node is. */
                using (stream.TemporarySeek(reader.ReadUInt32(), SeekOrigin.Begin))
                {
                    /* Skip ID of node.*/
                    if (HasTypePrefix(id))
                        stream.Position++;

                    return ParseNode(stream, id);
                }
            }
        }

        internal IBymlNode ParseNode(Stream stream, BymlNodeId id)
        {
            BinaryReader reader = new(stream);

            /* Check if we've parsed this before. */
            if (NodeCache.TryGetValue(stream.Position, out var val))
            {
                stream.Seek(val.Length, SeekOrigin.Current);
                return val.Node;
            }

            var pos = stream.Position;
            IBymlNode node = id switch
            {
                BymlNodeId.String => new BymlNode<string>(id, GetFromStringTable(reader.ReadUInt32())),
                BymlNodeId.Bin => new BymlNode<byte[]>(id, reader.ReadBytes(reader.ReadInt32())),
                BymlNodeId.Array => new BymlArrayNode(this, stream),
                BymlNodeId.Hash => new BymlHashTable(this, stream),
                BymlNodeId.StringTable => new BymlStringTable(stream),
                BymlNodeId.PathArray => new BymlPathArrayNode(reader),
                BymlNodeId.Bool => new BymlNode<bool>(id, (reader.ReadUInt32() & 1) == 1),
                BymlNodeId.Int => new BymlNode<int>(id, reader.ReadInt32()),
                BymlNodeId.Float => new BymlNode<float>(id, reader.ReadSingle()),
                BymlNodeId.UInt => new BymlNode<uint>(id, reader.ReadUInt32()),
                BymlNodeId.Null => new BymlNode<uint>(id, reader.ReadUInt32()),
                BymlNodeId.Int64 => new BymlBigDataNode<long>(id, reader, r => r.ReadInt64()),
                BymlNodeId.UInt64 => new BymlBigDataNode<ulong>(id, reader, r => r.ReadUInt64()),
                BymlNodeId.Double => new BymlBigDataNode<double>(id, reader, r => r.ReadDouble()),
                _ => throw new NotImplementedException(),
            };

            var length = stream.Position - pos;

            /* Cache parsed value. */
            NodeCache[pos] = new CachedNode(node, length);

            return node;
        }
    }
}
