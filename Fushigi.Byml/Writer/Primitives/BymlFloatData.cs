namespace Fushigi.Byml.Writer.Primitives
{
    public class BymlFloatData : BymlData
    {
        private readonly float Value;
        public BymlFloatData(float value)
        {
            Value = value;
        }

        public override BymlNodeId GetTypeCode() => BymlNodeId.Float;

        public override void Write(Stream stream)
        {
            stream.AsBinaryWriter().Write(Value);
        }
    }
}
