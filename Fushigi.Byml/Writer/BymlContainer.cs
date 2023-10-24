namespace Fushigi.Byml.Writer
{
    public abstract class BymlContainer : BymlData
    {
        public uint Offset;
        public override bool IsContainer() => true;

        public abstract override int CalcPackSize();

        public virtual void AddBool(string key, bool value) { }
        public virtual void AddInt(string key, int value) { }
        public virtual void AddUInt(string key, uint value) { }
        public virtual void AddFloat(string key, float value) { }
        public virtual void AddInt64(string key, long value, BymlBigDataList bigDataList) { }
        public virtual void AddUInt64(string key, ulong value, BymlBigDataList bigDataList) { }
        public virtual void AddDouble(string key, double value, BymlBigDataList bigDataList) { }
        public virtual void AddBinary(string key, byte[] value, BymlBigDataList bigDataList) { }
        public virtual void AddString(string key, string value) { }
        public virtual void AddHash(string key, BymlHash hash) { }
        public virtual void AddArray(string key, BymlArray array) { }
        public virtual void AddNull(string key) { }
        public virtual void AddBool(bool value) { }
        public virtual void AddInt(int value) { }
        public virtual void AddUInt(uint value) { }
        public virtual void AddFloat(float value) { }
        public virtual void AddInt64(long value, BymlBigDataList bigDataList) { }
        public virtual void AddUInt64(ulong value, BymlBigDataList bigDataList) { }
        public virtual void AddDouble(double value, BymlBigDataList bigDataList) { }
        public virtual void AddBinary(byte[] value, BymlBigDataList bigDataList) { }
        public virtual void AddString(string value) { }
        public virtual void AddHash(BymlHash hash) { }
        public virtual void AddArray(BymlArray array) { }
        public virtual void AddNull() { }
        public abstract void WriteContainer(Stream stream);
        public abstract bool IsHash();
        public abstract bool IsArray();
        public abstract void DeleteData();
    }
}
