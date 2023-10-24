using Fushigi.Byml.Writer.Primitives;

namespace Fushigi.Byml.Writer
{
    public class BymlArray : BymlContainer
    {
        private readonly BymlStringTable StringTable;

        private readonly List<BymlData> DataList = new();

        public BymlArray(BymlStringTable stringTable)
        {
            StringTable = stringTable;
        }
        public override int CalcPackSize()
        {
            return (int)(((DataList.Count + 7) & -4u) + 4 * DataList.Count);
        }

        private void AddData(BymlData data)
        {
            DataList.Add(data);
        }

        public override void AddNull(string key) => AddData(new BymlNullData());
        public override void AddBool(bool value) => AddData(new BymlBoolData(value));
        public override void AddInt(int value) => AddData(new BymlIntData(value));
        public override void AddUInt(uint value) => AddData(new BymlUIntData(value));
        public override void AddFloat(float value) => AddData(new BymlFloatData(value));
        public override void AddInt64(long value, BymlBigDataList bigDataList) => AddData(new BymlInt64Data(value, bigDataList));
        public override void AddUInt64(ulong value, BymlBigDataList bigDataList) => AddData(new BymlUInt64Data(value, bigDataList));
        public override void AddDouble(double value, BymlBigDataList bigDataList) => AddData(new BymlDoubleData(value, bigDataList));
        public override void AddBinary(byte[] value, BymlBigDataList bigDataList) => AddData(new BymlBinaryData(value, bigDataList));
        public override void AddString(string value) => AddData(new BymlStringData(value, StringTable));
        public override void AddHash(BymlHash hash) => AddData(hash);
        public override void AddArray(BymlArray array) => AddData(array);
        public override void AddNull() => AddData(new BymlNullData());
        public override void WriteContainer(Stream stream)
        {
            var writer = stream.AsBinaryWriter();

            writer.Write((byte)GetTypeCode());
            writer.WriteUInt24((uint)DataList.Count);

            foreach (var data in DataList)
            {
                writer.Write((byte)data.GetTypeCode());
            }

            /* Align by 4 bytes. */
            while (stream.Position % 4 != 0)
                stream.Position++;

            foreach (var data in DataList)
            {
                data.Write(stream);
            }
        }
        public override bool IsHash() => false;
        public override bool IsArray() => true;
        public override void DeleteData()
        {
            /* TODO: check this is right? */
            DataList.Clear();
        }
        public override BymlNodeId GetTypeCode() => BymlNodeId.Array;

        public override void Write(Stream stream)
        {
            stream.AsBinaryWriter().Write(Offset);
        }

    }
}
