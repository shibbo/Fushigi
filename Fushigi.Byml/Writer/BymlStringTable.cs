using System.Text;

namespace Fushigi.Byml.Writer
{
    public class BymlStringTable
    {
        private readonly List<string> StringList = new();

        public int CalcContentSize()
        {
            return Utils.AlignUp(StringList.Sum(x => Encoding.UTF8.GetByteCount(x) + 1), 4);
        }

        public int CalcHeaderSize()
        {
            return 8 + (4 * StringList.Count);
        }

        public int CalcIndex(string str)
        {
            return StringList.IndexOf(str);
        }

        public int CalcPackSize()
        {
            if (StringList.Count == 0)
                return 0;

            return CalcHeaderSize() + CalcContentSize();
        }

        public bool IsEmpty() => StringList.Count == 0;

        public void TryAdd(string str)
        {
            int idx = Utils.BinarySearch(StringList, str);

            /* Don't add it if we already have it. */
            if (idx >= 0)
                return;
            idx = ~idx;

            StringList.Insert(idx, str);
        }

        public void Write(Stream stream)
        {
            /* Don't write if there's nothing to write. */
            if (IsEmpty())
                return;

            var writer = stream.AsBinaryWriter();
            writer.Write((byte) BymlNodeId.StringTable);
            writer.WriteUInt24((uint)StringList.Count);

            int offset = CalcHeaderSize();
            foreach(var str in StringList)
            {
                writer.Write(offset);

                var length = Encoding.UTF8.GetByteCount(str) + 1;
                offset += length;
            }
            /* Write ending offset. */
            writer.Write(offset);

            foreach (var str in StringList)
            {
                var data = Encoding.UTF8.GetBytes(str);
                writer.Write(data);
                writer.Write('\0');
            }

            /* Align by 4 bytes. */
            while (stream.Position % 4 != 0)
                stream.Position++;
        }
    }
}
