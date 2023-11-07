using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public class FileUtil
    {
        public static string FindContentPath(string path)
        {
            //Check for mod folder, then fall to romfs path
            string modPath = Path.Combine(UserSettings.GetModRomFSPath(), path);
            if (File.Exists(modPath))
                return modPath;

            return Path.Combine(UserSettings.GetRomFSPath(), path);
        }

        public static byte[] DecompressFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception($"FileUtil::DecompressFile -- File not found. ({filePath})");
            }

            var compressedBytes = File.ReadAllBytes(filePath);
            byte[] decompressedData = DecompressData(compressedBytes);
            return decompressedData;
        }

        public static byte[] DecompressData(byte[] fileBytes)
        {
            byte[] decompressedData;

            if (!IsFileCompressed(fileBytes)) {
                throw new Exception("FileUtil::DecompressData -- File not ZSTD Compressed.");
            }
            using (var decompressor = new ZstdNet.Decompressor())
            {
                decompressedData = decompressor.Unwrap(fileBytes);
            }

            return decompressedData;
        }

        public static bool IsFileCompressed(byte[] fileBytes)
        {
            /* Zstandard frame metadata */
            if (fileBytes[0] == 0x28 && fileBytes[1] == 0xb5 && fileBytes[2] == 0x2f && fileBytes[3] == 0xfd) {
                return true;
            }
            /* Skippable frame metadata, skips the first byte because it can be variable */
            else if (fileBytes[1] == 0x2a && fileBytes[1] == 0x4d && fileBytes[2] == 0x18) {
                return true;
            }
            else {
                return false;
            }
        }

        public static byte[] CompressData(byte[] fileBytes)
        {
            byte[] compressedData;

            using (var compressor = new ZstdNet.Compressor(new ZstdNet.CompressionOptions(19)))
            {
                compressedData = compressor.Wrap(fileBytes);
            }

            return compressedData;
        }

        public static bool TryGetFileInfo(string filename, out FileInfo fileInfo)
        {
            try
            {
                fileInfo = new FileInfo(filename);
                return true;
            }
            catch
            {
                fileInfo = null;
                return false;
            }
        }
    }
}
