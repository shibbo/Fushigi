namespace Fushigi.Byml
{
    public class BymlArrayNode : IBymlNode
    {
        public BymlNodeId Id => BymlNodeId.Array;

        public IBymlNode[] Array;

        public int Length => Array.Length;

        public IBymlNode this[int i] => Array[i];

        public BymlArrayNode(Byml by, Stream stream)
        {
            BinaryReader reader = new(stream);
            var position = stream.Position;

            var count = reader.ReadUInt24();

            var typesData = reader.ReadBytes((int)count);
            var types = typesData.Where(Byml.IsValidBymlNodeId).Cast<BymlNodeId>().ToArray();

            if (types.Length != count)
                throw new InvalidDataException("Invalid node type!");

            /* Align by 4 bytes. */
            while (stream.Position % 4 != 0)
                stream.Position++;

            Array = new IBymlNode[count];
            for (var i = 0; i < count; i++)
            {
                var id = types[i];
                Array[i] = by.ReadNode(reader, id);
            }
        }
    }
}
