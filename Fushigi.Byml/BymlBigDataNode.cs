namespace Fushigi.Byml
{
    public class BymlBigDataNode<T> : IBymlNode
    {
        public BymlNodeId Id { get; }
        public T Data { get; }

        public BymlBigDataNode(BymlNodeId id, BinaryReader reader, Func<BinaryReader, T> valueReader)
        {
            Id = id;
            using (reader.BaseStream.TemporarySeek(reader.ReadUInt32(), SeekOrigin.Begin))
            {
                Data = valueReader(reader);
            }
        }
    }
}
