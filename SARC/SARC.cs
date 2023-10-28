using System.Runtime.InteropServices;

namespace Fushigi.SARC
{
    public class SARC
    {
        [StructLayout(LayoutKind.Sequential, Size = 0x14)]
        public struct SARCHeader
        {
            public uint Magic;
            public ushort HeaderSize;
            public ushort BOM;
            public uint FileSize;
            public uint DataOffset;
            public ushort Version;
            public ushort Padding;
        }

        public struct SFATHeader
        {
            public uint Magic;
            public ushort HeaderSize;
            public ushort NodeCount;
            public uint HashKey;
        }

        public struct SARCFile
        {
            public uint EntryOffs;
            public uint NameHash;
            public uint NameOffs;
            public uint Offs;
            public uint Size;
            public string FileName;
        }

        SARCHeader Header;
        SFATHeader SFAT_Header;
        Dictionary<string, SARCFile> Files = new Dictionary<string, SARCFile>();
        MemoryStream Stream;

        public SARC(MemoryStream stream)
        {
            Stream = stream;
            stream.Read(Utils.AsSpan(ref Header));

            if (Header.Magic != 0x43524153)
            {
                throw new InvalidDataException("SARC::SARC() -- Invalid magic.");
            }

            long sFatOffset = stream.Position;

            stream.Read(Utils.AsSpan(ref SFAT_Header));

            if (SFAT_Header.Magic != 0x54414653)
            {
                throw new InvalidDataException("SARC::SARC() -- Invalid SFAT magic.");
            }

            long sFNTOffset = sFatOffset + 0xC + (SFAT_Header.NodeCount * 0x10);

            for (uint i = 0; i < SFAT_Header.NodeCount; i++)
            {
                stream.Seek((long)(sFatOffset + 0xC + (i * 0x10)), SeekOrigin.Begin);
                SARCFile file = new SARCFile();
                file.EntryOffs = (uint)stream.Position;
                file.NameHash = stream.AsBinaryReader().ReadUInt32();
                file.NameOffs = (stream.AsBinaryReader().ReadUInt32() & 0xFFFFFF) << 2;
                file.Offs = stream.AsBinaryReader().ReadUInt32();
                file.Size = stream.AsBinaryReader().ReadUInt32() - file.Offs;

                stream.Seek((long)(sFNTOffset + 0x8 + file.NameOffs), SeekOrigin.Begin);
                file.FileName = Utils.ReadString(stream.AsBinaryReader());

                Files.Add(file.FileName, file);
            }
        }

        public bool DirectoryExists(string path)
        {
            foreach (string file in Files.Keys)
            {
                if (file.StartsWith(path))
                {
                    return true;
                }
            }

            return false;
        }

        public bool FileExists(string path)
        {
            foreach (string file in Files.Keys)
            {
                if (file == path)
                {
                    return true;
                }
            }

            return false;
        }

        public string[] GetFiles(string path)
        {
            List<string> files = new List<string>();

            foreach(string file in Files.Keys)
            {
                if (file.StartsWith(path))
                {
                    files.Add(file);
                }
            }

            return files.ToArray();
        }

        public byte[] OpenFile(string path)
        {
            if (!FileExists(path))
            {
                return null;
            }

            SARCFile file = Files[path];

            Stream.Seek(Header.DataOffset + file.Offs, SeekOrigin.Begin);
            return Utils.ReadBytes(Stream.AsBinaryReader(), file.Size);
        }
    }
}
