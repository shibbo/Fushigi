namespace Fushigi.Byml.Writer.Primitives
{
    public class BymlDoubleData : BymlBigData
    {
        private readonly double Value;
        public BymlDoubleData(double value, BymlBigDataList parentList) : base(parentList)
        {
            Value = value;
        }

        public override int CalcBigDataSize() => 8;
        public override BymlNodeId GetTypeCode() => BymlNodeId.Double;
        public override void WriteBigData(Stream stream)
        {
            stream.AsBinaryWriter().Write(Value);
        }
    }
}
