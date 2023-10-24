namespace Fushigi.Byml.Writer.Primitives
{
    public class BymlInt64Data : BymlBigData
    {
        private readonly long Value;
        public BymlInt64Data(long value, BymlBigDataList parentList) : base(parentList)
        {
            Value = value;
        }

        public override int CalcBigDataSize() => 8;
        public override BymlNodeId GetTypeCode() => BymlNodeId.Int64;
        public override void WriteBigData(Stream stream)
        {
            stream.AsBinaryWriter().Write(Value);
        }
    }
}
