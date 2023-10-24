using System.Runtime.CompilerServices;

namespace Fushigi.Byml
{
    public class BymlPathArrayNode : IBymlNode
    {
        public BymlNodeId Id => BymlNodeId.PathArray;
        public BymlPathPoint[][] Arrays;

        public BymlPathArrayNode(BinaryReader reader)
        {
            var stream = reader.BaseStream;
            var startOfNode = stream.Position - 1;
            var listCount = reader.ReadUInt24();

            Arrays = new BymlPathPoint[listCount][];

            var offsets = stream.ReadArray<uint>(listCount + 1);

            for (var i = 0; i < listCount; i++)
            {
                var count = (offsets[i + 1] - offsets[i]) / Unsafe.SizeOf<BymlPathPoint>();
                using (stream.TemporarySeek(startOfNode + offsets[i], SeekOrigin.Begin))
                {
                    Arrays[i] = stream.ReadArray<BymlPathPoint>((uint)count);
                }
            }
        }
    }
}
