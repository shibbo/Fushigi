using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.rstb
{
    public class RSTB
    {
        [StructLayout(LayoutKind.Sequential, Size = 0x10)]
        struct Header
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] Magic; //RESTBL

            public uint Version; //1
            public uint StringBlockSize; //160 for wonder
            public uint CrcTableNum;
            public uint NameTableNum;
        }

        /// <summary>
        /// A lookup of file hashes and their resource sizes used.
        /// </summary>
        public Dictionary<uint, uint> HashToResourceSize = new Dictionary<uint, uint>();

        /// <summary>
        /// A lookup of file names and their resource sizes used.
        /// This is only used when hashes conflict and cannot be used for the hash table.
        /// </summary>
        public Dictionary<string, uint> StringToResourceSize = new Dictionary<string, uint>();

        /// <summary>
        /// The RSTB file header.
        /// </summary>
        private Header FileHeader;

        /// <summary>
        /// Sets a resource given a file path and decompressed size.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="decompressed_size"></param>
        public void SetResource(string filePath, uint decompressed_size)
        {
            //Get file name without .zs extension
            string path = filePath.Replace(".zs", "");
            string ext = Path.GetExtension(filePath);
            //Compute hash to find in the resource table
            uint hash = Crc32.Compute(path);
            //Update the resource size
            if (HashToResourceSize.ContainsKey(hash))
                HashToResourceSize[hash] = CalculateResourceSize(decompressed_size, ext);
            else
            {
                Console.WriteLine($"Warning! File {path} not found in resource table!");
            }
        }

        /// <summary>
        /// Loads the resource table from the romfs or saved content path configured in the tool settings.
        /// </summary>
        public void Load()
        {
            string path = FileUtil.FindContentPath($"System{Path.DirectorySeparatorChar}Resource{Path.DirectorySeparatorChar}ResourceSizeTable.Product.100.rsizetable.zs");
            //Failed to find file, skip
            if (!File.Exists(path))
                return;

            Read(new MemoryStream(FileUtil.DecompressFile(path)));
        }

        /// <summary>
        /// Save the resource table to the saved romfs path configured in the tool settings.
        /// </summary>
        public void Save()
        {
            if (HashToResourceSize.Count == 0) //File not loaded, return
                return;

            string dir = $"{UserSettings.GetModRomFSPath()}{Path.DirectorySeparatorChar}System{Path.DirectorySeparatorChar}Resource";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var mem = new MemoryStream();
            Write(mem);
            File.WriteAllBytes($"{dir}{Path.DirectorySeparatorChar}ResourceSizeTable.Product.100.rsizetable.zs", FileUtil.CompressData(mem.ToArray()));
        }

        private void Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                FileHeader = new Header()
                {
                    Magic = reader.ReadBytes(6), //RESTBL
                    Version = reader.ReadUInt32(),
                    StringBlockSize = reader.ReadUInt32(),
                    CrcTableNum = reader.ReadUInt32(),
                    NameTableNum = reader.ReadUInt32(),
                };
                for (int i = 0; i < FileHeader.CrcTableNum; i++)
                {
                    uint hash = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();
                    HashToResourceSize.Add(hash, size);
                }
                for (int i = 0; i < FileHeader.NameTableNum; i++)
                {
                    //Fixed 128 byte string in UTF8 format
                    string name = Encoding.UTF8.GetString(reader.ReadBytes(128)).Replace("\0", string.Empty);
                    uint size = reader.ReadUInt32();
                    StringToResourceSize.Add(name, size);
                }
            }
        }

        private void Write(Stream stream)
        {
            FileHeader.CrcTableNum = (uint)this.HashToResourceSize.Count;

            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(FileHeader.Magic);
                writer.Write(FileHeader.Version);
                writer.Write(FileHeader.StringBlockSize);
                writer.Write(FileHeader.CrcTableNum);
                writer.Write(FileHeader.NameTableNum);

                //Sort by hash
                foreach (var pair in HashToResourceSize.OrderBy(x => x.Key))
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
                foreach (var pair in StringToResourceSize.OrderBy(x => x.Key))
                {
                    writer.Write(new byte[128]); //reserve string space
                    //write string
                    writer.Seek(-128, SeekOrigin.Current);
                    writer.Write(Encoding.UTF8.GetBytes(pair.Key));
                    writer.Write(pair.Value);
                }
            }
        }

        private uint CalculateResourceSize(uint decompressed_size, string ext)
        {
            //According to BOTW wiki, calculation goes like this 
            //(size rounded up to multiple of 32) + CONSTANT + sizeof(ResourceClass) + PARSE_SIZE

            //Round to nearest 32
            var size = (decompressed_size + 31) & -32;

            //Formats which are verified to be the correct size
            switch (ext)
            {
                case ".byml": //For bcett.byml, the total added after rounding is always 0x100 bytes
                    return (uint)size + 0x100;
                case ".pack": //Always 0x180 including actor .pack files
                case ".sarc": //Mal and agl sarc files
                case ".blarc": //Layout sarc files
                    return (uint)size + 0x180;
                case ".genvb": //Tested from Env folder
                    return (uint)size + 0x2000;
            }

            //Default
            return (uint)size + 0x1000;
        }

        /// <summary>
        /// A test to verify the padding size of resource buffers from a folder of files.
        /// </summary>
        private void TestPrintResourceBuffer(string folder)
        {
            foreach (var file in RomFS.GetFiles(folder))
            {
                //Decompressed file size
                var size = 0;
                if (file.EndsWith(".zs"))
                    size = (int)FileUtil.DecompressFile(file).Length;
                else
                    size = (int)new FileInfo(file).Length;

                //Get resource hash
                string path = $"{folder}{Path.DirectorySeparatorChar}{Path.GetFileName(file).Replace(".zs", "")}";
                uint hash = Crc32.Compute(path);

                if (this.HashToResourceSize.ContainsKey(hash))
                {
                    //Round to nearest 32
                    size = (size + 31) & -32;

                    //Find difference in raw file size and resource size
                    uint resource_size = this.HashToResourceSize[hash];
                    int diff = (int)resource_size - size;
                    Console.WriteLine($"decomp_size {size} resource_size difference {diff} path {path}");
                }
            }
        }
    }
}
