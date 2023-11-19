using Fushigi.Bfres.Common;
using System.Reflection;

namespace Fushigi.Bfres
{
    public class BfresFile
    {
        /// <summary>
        /// 
        /// </summary>
        public ResDict<Model> Models { get; set; } = new ResDict<Model>();

        /// <summary>
        /// 
        /// </summary>
        public ResDict<EmbeddedFile> EmbeddedFiles { get; set; } = new ResDict<EmbeddedFile>();

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the bntx binary from the embedded file list if one exists.
        /// Returns an empty one if none is found.
        /// </summary>
        /// <returns></returns>
        public BntxFile TryGetTextureBinary()
        {
            if (!EmbeddedFiles.ContainsKey("textures.bntx"))
                return new BntxFile();

            return new BntxFile(new MemoryStream(EmbeddedFiles["textures.bntx"].Data));
        }

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
            EmbeddedFiles = reader.ReadDictionary<EmbeddedFile>(Header.EmbeddedFilesDictionaryOffset, Header.EmbeddedFilesOffset);

            Init(reader);
        }

        internal void Init(BinaryReader reader)
        {
            foreach (Model model in Models.Values)
                model.Init(reader, this.BufferMemoryPoolInfo);
        }
    }
}
