using System.ComponentModel.DataAnnotations.Schema;

namespace Fushigi.Byml
{
    public class BymlBigDataNode<T> : IBymlNode
    {
        public BymlNodeId Id { get; }
        public T Data { get; set; }

        public BymlBigDataNode(BymlNodeId id, BinaryReader reader, Func<BinaryReader, T> valueReader)
        {
            Id = id;
            using (reader.BaseStream.TemporarySeek(reader.ReadUInt32(), SeekOrigin.Begin))
            {
                Data = valueReader(reader);
            }
        }

        public BymlBigDataNode(BymlNodeId id, T data)
        {
            Id = id;
            Data = data;
        }

        public void SetData(T data)
        {
            Data = data;
        }
    }
}
