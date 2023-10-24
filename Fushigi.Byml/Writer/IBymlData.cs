namespace Fushigi.Byml.Writer
{
    public interface IBymlData
    {
        void MakeIndex();
        int CalcPackSize();
        BymlNodeId GetTypeCode();
        bool IsContainer();
        void Write(Stream stream);
    }
}
