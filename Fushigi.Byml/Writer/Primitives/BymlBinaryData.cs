namespace Fushigi.Byml.Writer.Primitives
{
    public class BymlBinaryData : BymlBigData
    {
        private readonly byte[] Data;

        public BymlBinaryData(byte[] data, BymlBigDataList parentList) : base(parentList)
        {
            Data = data;
        }

        /* + 4 to accommodate storing the length. */
        public override int CalcBigDataSize() => Data.Length + 4;
        public override BymlNodeId GetTypeCode() => BymlNodeId.Bin;

        public override void WriteBigData(Stream stream)
        {
            var writer = stream.AsBinaryWriter();
            writer.Write(Data.Length);
            writer.Write(Data);
        }
    }
}
