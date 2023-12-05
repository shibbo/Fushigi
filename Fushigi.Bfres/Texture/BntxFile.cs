using Fushigi.Bfres.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    public class BntxFile
    {
        /// <summary>
        /// 
        /// </summary>
        public ResDict<BntxTexture> Textures { get; set; } = new ResDict<BntxTexture>();

        private BinaryHeader BinHeader; //A header shared between bntx and other formats
        private BntxHeader Header; //Bfres header

        public BntxFile() { }

        public BntxFile(string filePath)
        {
            Read(File.OpenRead(filePath));
        }

        public BntxFile(Stream stream)
        {
            Read(stream);
        }

        public void Read(Stream stream)
        {
            var reader = stream.AsBinaryReader();

            stream.Read(Utils.AsSpan(ref BinHeader));
            stream.Read(Utils.AsSpan(ref Header));

            reader.SeekBegin((long)Header.TextureTableOffset);

            ulong[] offsets = new ulong[Header.TextureCount];
            for (int i = 0; i < Header.TextureCount; i++)
                offsets[i] = reader.ReadUInt64();

            for (int i = 0; i < Header.TextureCount; i++)
            {
                reader.SeekBegin((long)offsets[i]);
                var tex = reader.Read<BntxTexture>();
                Textures.Add(tex.Name, tex);
            }
        }
    }
}
