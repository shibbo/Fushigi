namespace Fushigi.Byml.Writer
{
    public class BymlBigDataList
    {
        private readonly List<BymlBigData> Internal = new();

        public void AddData(BymlBigData data)
        {
            Internal.Add(data);
        }

        public int CalcPackSize()
        {   
            /* Sum up big data sizes and round up by 4. */
            return Utils.AlignUp(Internal.Sum(x => x.CalcBigDataSize()), 4);
        }

        public int SetOffset(int offset)
        {
            foreach(var data in Internal)
            {
                data.Offset = offset;
                offset += data.CalcBigDataSize();
            }
            return Utils.AlignUp(offset, 4);
        }

        public void Write(Stream stream)
        {
            foreach(var data in Internal)
            {
                using (stream.TemporarySeek())
                    data.WriteBigData(stream);
                stream.Position += data.CalcBigDataSize();
            }

            /* Align by 4 bytes. */
            while (stream.Position % 4 != 0)
                stream.Position++;
        }
    }
}
