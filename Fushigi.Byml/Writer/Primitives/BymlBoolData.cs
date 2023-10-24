namespace Fushigi.Byml.Writer.Primitives
{
    public class BymlBoolData : BymlData
    {
        private readonly bool Value;
        public BymlBoolData(bool value)
        {
            Value = value;
        }

        public override BymlNodeId GetTypeCode() => BymlNodeId.Bool;

        public override void Write(Stream stream)
        {
            /* Explicitly cast to sizeof 4 bytes. */
            stream.AsBinaryWriter().Write(Value ? 1u : 0u);
        }
    }
}
