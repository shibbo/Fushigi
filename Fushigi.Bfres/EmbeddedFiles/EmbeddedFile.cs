using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    public class EmbeddedFile : IResData
    {
        public byte[] Data;

        public void Read(BinaryReader reader)
        {
            ulong offset = reader.ReadUInt64();
            uint size = reader.ReadUInt32();
            reader.ReadUInt32(); //padding

            Data = reader.ReadCustom(() => reader.ReadBytes((int)size), offset);
        }
    }
}
