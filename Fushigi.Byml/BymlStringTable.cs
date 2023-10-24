namespace Fushigi.Byml
{
    public class BymlStringTable : IBymlNode
    {
        public BymlNodeId Id => BymlNodeId.StringTable;

        public readonly string[] Strings;
        public BymlStringTable(Stream stream)
        {
            var startOfNode = stream.Position - 1;
            BinaryReader reader = new(stream);

            var count = reader.ReadUInt24();
            var indexes = stream.ReadArray<uint>(count + 1);

            Strings = new string[count];

            for (var i = 0; i < count; i++)
            {
                var start = indexes[i];
                var end = indexes[i + 1]; /* Index table is count+1, so this is fine. */

                using (stream.TemporarySeek(startOfNode + start, SeekOrigin.Begin))
                    Strings[i] = reader.ReadUtf8Z((int)(end - start));
            }
        }

    }
}
