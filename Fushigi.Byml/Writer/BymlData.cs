namespace Fushigi.Byml.Writer
{
    public abstract class BymlData : IBymlData
    {
        /* Default to sizeof(uint) */
        public virtual int CalcPackSize() => 4;
        public abstract BymlNodeId GetTypeCode();
        public virtual bool IsContainer() => false;
        public void MakeIndex() { }
        public abstract void Write(Stream stream);
    }
}
