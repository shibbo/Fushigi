using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi
{
    public class RomFS
    {
        public static void SetRoot(string root)
        {
            sRomFSRoot = root;
        }

        public static string GetRoot()
        {
            return sRomFSRoot;
        }

        public static bool DirectoryExists(string path) {
            return Directory.Exists($"{sRomFSRoot}/{path}");
        }

        public static string[] GetFiles(string path)
        {
            return Directory.GetFiles($"{sRomFSRoot}/{path}");
        }

        public static byte[] GetFileBytes(string path)
        {
            return File.ReadAllBytes($"{sRomFSRoot}/{path}");
        }

        private static string sRomFSRoot;
    }
}
