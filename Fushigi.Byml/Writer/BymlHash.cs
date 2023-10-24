using Fushigi.Byml.Writer.Primitives;

namespace Fushigi.Byml.Writer
{
    public class BymlHash : BymlContainer
    {
        private readonly BymlStringTable HashKeyStringTable;
        private readonly BymlStringTable StringTable;

        private readonly List<Pair> PairList = new();

        private struct Pair : IComparable<string>, IComparable<Pair>
        {
            public string Key;
            public BymlData Data;

            public int CompareTo(string? other)
            {
                if (other == null) return 1;
                return string.CompareOrdinal(Key, other);
            }

            public int CompareTo(Pair other)
            {
                return CompareTo(other.Key);
            }
        }

        public BymlHash(BymlStringTable hashKeyStringTable, BymlStringTable stringTable)
        {
            HashKeyStringTable = hashKeyStringTable;
            StringTable = stringTable;
        }

        private void AddData(string key, BymlData data)
        {
            HashKeyStringTable.TryAdd(key);

            Pair pair = new()
            {
                Key = key,
                Data = data
            };

            var idx = Utils.BinarySearch(PairList, pair);
            if (idx >= 0)
                throw new Exception("Duplicate key!");

            idx = ~idx;

            PairList.Insert(idx, pair);
        }
        public override void AddBool(string key, bool value) => AddData(key, new BymlBoolData(value));
        public override void AddInt(string key, int value) => AddData(key, new BymlIntData(value));
        public override void AddUInt(string key, uint value) => AddData(key, new BymlUIntData(value));
        public override void AddFloat(string key, float value) => AddData(key, new BymlFloatData(value));
        public override void AddInt64(string key, long value, BymlBigDataList bigDataList) => AddData(key, new BymlInt64Data(value, bigDataList));
        public override void AddUInt64(string key, ulong value, BymlBigDataList bigDataList) => AddData(key, new BymlUInt64Data(value, bigDataList));
        public override void AddDouble(string key, double value, BymlBigDataList bigDataList) => AddData(key, new BymlDoubleData(value, bigDataList));
        public override void AddBinary(string key, byte[] value, BymlBigDataList bigDataList) => AddData(key, new BymlBinaryData(value, bigDataList));
        public override void AddString(string key, string value) => AddData(key, new BymlStringData(value, StringTable));
        public override void AddHash(string key, BymlHash hash) => AddData(key, hash);
        public override void AddArray(string key, BymlArray array) => AddData(key, array);
        public override void AddNull(string key) => AddData(key, new BymlNullData());

        public override int CalcPackSize()
        {
            return (PairList.Count * 8) | 4;
        }

        public override void WriteContainer(Stream stream)
        {
            var writer = stream.AsBinaryWriter();

            writer.Write((byte)GetTypeCode());
            writer.WriteUInt24((uint)PairList.Count);

            foreach (var pair in PairList)
            {
                var idx = HashKeyStringTable.CalcIndex(pair.Key);
                writer.WriteUInt24((uint)idx);
                writer.Write((byte)pair.Data.GetTypeCode());
                pair.Data.Write(stream);
            }
        }
        public override bool IsHash() => true;
        public override bool IsArray() => false;
        public override void DeleteData()
        {
            /* TODO: check this is right? */
            PairList.Clear();
        }
        public override BymlNodeId GetTypeCode() => BymlNodeId.Hash;

        public override void Write(Stream stream)
        {
            stream.AsBinaryWriter().Write(Offset);
        }
    }
}
