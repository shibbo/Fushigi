using System.Reflection;

namespace Fushigi.Bfres
{
    public class BfresFile
    {
        public Dictionary<string, Model> Models = new Dictionary<string, Model>();

        public string Name;

        private BinaryHeader BinHeader; //A header shared between bntx and other formats
        private ResHeader Header; //Bfres header

        private BufferMemoryPool BufferMemoryPoolInfo;

        public BfresFile(string filePath) {
            Read(File.OpenRead(filePath));
        }

        public BfresFile(Stream stream) {
            Read(stream);
        }

        public void Read(Stream stream)
        {
            var reader = stream.AsBinaryReader();

             stream.Read(Utils.AsSpan(ref BinHeader));
             stream.Read(Utils.AsSpan(ref Header));

            Name = reader.ReadStringOffset(Header.NameOffset);


            reader.Seek(Header.MemoryPoolInfoOffset);
            stream.Read(Utils.AsSpan(ref BufferMemoryPoolInfo));

            reader.Seek(Header.ModelOffset);
            var models = reader.ReadArray<Model>(Header.ModelCount);

            for (int i = 0; i < models.Count; i++)
                Models.Add(models[i].Name, models[i]);

            Init(reader);
        }

        internal void Init(BinaryReader reader)
        {
            foreach (var model in Models.Values)
                model.Init(reader, this.BufferMemoryPoolInfo);
        }
    }
}
