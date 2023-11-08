using Fushigi.Bfres.Common;
using System.Reflection;

namespace Fushigi.Bfres
{
    public class BfresFile
    {
        public ResDict<Model> Models = new ResDict<Model>();

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

            Models = reader.ReadDictionary<Model>(Header.ModelDictionaryOffset, Header.ModelOffset);

            Init(reader);
        }

        internal void Init(BinaryReader reader)
        {
            foreach (Model model in Models.Values)
                model.Init(reader, this.BufferMemoryPoolInfo);
        }
    }
}
