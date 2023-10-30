using Fushigi.Byml;
using TinyDialogsNet;

namespace Fushigi
{
    public class RomFS
    {
        public static bool SetRoot(string root)
        {
            sRomFSRoot = root;

            /* common paths to check */
            if (!RomFS.DirectoryExists("BancMapUnit") || !RomFS.DirectoryExists("Model") || !RomFS.DirectoryExists("Stage"))
            {
                Dialogs.MessageBox(Dialogs.MessageBoxButtons.Ok, Dialogs.MessageBoxIconType.Error, Dialogs.MessageBoxDefaultButton.OkYes, "Invalid RomFS Path", "The path you have selected is invalid. Please select a RomFS path that contains BancMapUnit, Model, and Stage.");
                return false;
            }

            CacheCourseFiles();
            return true;
        }

        public static string GetRoot()
        {
            return sRomFSRoot;
        }

        public static Dictionary<string, string[]> GetCourseEntries()
        {
            return sCourseEntries;
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

        private static void CacheCourseFiles()
        {
            sCourseEntries.Clear();
            string[] loadFiles = RomFS.GetFiles("/Stage/WorldMapInfo");
            foreach (string loadFile in loadFiles)
            {
                string worldName = Path.GetFileName(loadFile).Split(".game")[0];
                List<string> courseLocationList = new();
                Byml.Byml byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(loadFile)));
                var root = (BymlHashTable)byml.Root;
                var courseList = (BymlArrayNode)root["CourseTable"];

                for (int i = 0; i < courseList.Length; i++)
                {
                    var course = (BymlHashTable)courseList[i];
                    string derp = ((BymlNode<string>)course["StagePath"]).Data;

                    // we need to "fix" our StagePath so it points to our course
                    string courseLocation = Path.GetFileName(derp).Split(".game")[0];

                    courseLocationList.Add(courseLocation);
                }
                if (!sCourseEntries.ContainsKey(worldName))
                {
                    sCourseEntries.Add(worldName, courseLocationList.ToArray());
                }
            }
        }

        private static string sRomFSRoot;
        private static Dictionary<string, string[]> sCourseEntries = [];
    }
}
