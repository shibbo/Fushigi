namespace Fushigi.Byml.Writer.Primitives
{
    public class BymlUInt64Data : BymlBigData
    {
        private readonly ulong Value;
        public BymlUInt64Data(ulong value, BymlBigDataList parentList) : base(parentList)
        {
            Value = value;
        }

        public override int CalcBigDataSize() => 8;
        public override BymlNodeId GetTypeCode() => BymlNodeId.UInt64;
        public override void WriteBigData(Stream stream)
        {
            stream.AsBinaryWriter().Write(Value);
        }
    }
}
