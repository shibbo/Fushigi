using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public class FileUtil
    {
        public static byte[] DecompressFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("FileUtil::DecompressFile -- File not found.");
            }

            var compressedBytes = File.ReadAllBytes(filePath);
            byte[] decompressedData;

            using (var decompressor = new ZstdNet.Decompressor())
            {
                decompressedData = decompressor.Unwrap(compressedBytes);
            }

            return decompressedData;
        }

        public static byte[] DecompressData(byte[] fileBytes)
        {
            byte[] decompressedData;

            using (var decompressor = new ZstdNet.Decompressor())
            {
                decompressedData = decompressor.Unwrap(fileBytes);
            }

            return decompressedData;
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
