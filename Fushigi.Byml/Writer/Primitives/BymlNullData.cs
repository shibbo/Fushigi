namespace Fushigi.Byml.Writer.Primitives
{
    public class BymlNullData : BymlData
    {
        public override BymlNodeId GetTypeCode() => BymlNodeId.Null;
        public override void Write(Stream stream)
        {
            stream.AsBinaryWriter().Write((uint)0);
        }
    }
}
