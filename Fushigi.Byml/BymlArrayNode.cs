namespace Fushigi.Byml
{
    public class BymlArrayNode : IBymlNode
    {
        public BymlNodeId Id => BymlNodeId.Array;

        public List<IBymlNode> Array;

        public int Length => Array.Count;

        public IBymlNode this[int i] => Array[i];

        public BymlArrayNode() {
            Array = new List<IBymlNode>();
        }

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

            Array = new List<IBymlNode>();
            for (var i = 0; i < count; i++)
            {
                var id = types[i];
                Array.Add(by.ReadNode(reader, id));
            }
        }

        public void SetNodeAtIdx(IBymlNode node, int idx)
        {
            Array[idx] = node;
        }

        public BymlArrayNode(uint count)
        {
            Array = new List<IBymlNode>();
        }

        public void AddNodeToArray(IBymlNode node)
        {
            Array.Add(node);
        }
    }
}
