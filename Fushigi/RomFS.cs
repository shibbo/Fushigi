using Fushigi.Bfres;
using Fushigi.Byml;
using Fushigi.gl.Bfres;
using Fushigi.util;
using Silk.NET.OpenGL;
using System.Diagnostics;

namespace Fushigi
{
    public class RomFS
    {
        public static void SetRoot(string root)
        {           
            if (!IsValidRoot(root))
            {
                return;
            }

            sRomFSRoot = root;
            CacheCourseFiles();
        }

        public static string GetRoot()
        {
            return sRomFSRoot;
        }

        public static bool IsValidRoot(string root)
        {
            /* common paths to check */
            return Directory.Exists(Path.Combine(root, "BancMapUnit")) && 
                Directory.Exists(Path.Combine(root, "Model")) && 
                Directory.Exists(Path.Combine(root, "Stage"));
        }

        public static Dictionary<string, Dictionary<string, CourseEntry>> GetCourseEntries()
        {
            return sCourseEntries;
        }

        public static bool DirectoryExists(string path) {
            return Directory.Exists(Path.Combine(sRomFSRoot, path));
        }

        public static string[] GetFiles(string path)
        {
            return Directory.GetFiles(Path.Combine(sRomFSRoot, path));
        }

        public static byte[] GetFileBytes(string path)
        {
            Console.WriteLine($"RomFS::GetFileBytes() -- {path}");
            return File.ReadAllBytes(Path.Combine(sRomFSRoot, path));
        }    

        private static void CacheCourseFiles()
        {
            sCourseEntries.Clear();
            string[] loadFiles = GetFiles(Path.Combine("Stage", "WorldMapInfo"));
            foreach (string loadFile in loadFiles)
            {
                string worldName = Path.GetFileName(loadFile).Split(".game")[0];
                Dictionary<string, CourseEntry> courseLocationList = new();
                Byml.Byml byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(loadFile)));
                var root = (BymlHashTable)byml.Root;
                var courseList = (BymlArrayNode)root["CourseTable"];

                for (int i = 0; i < courseList.Length; i++)
                {
                    var course = (BymlHashTable)courseList[i];
                    string derp = ((BymlNode<string>)course["StagePath"]).Data;

                    // we need to "fix" our StagePath so it points to our course
                    string courseLocation = Path.GetFileName(derp).Split(".game")[0];

                    courseLocationList.Add(courseLocation, new());
                }
                if (!sCourseEntries.ContainsKey(worldName))
                {
                    sCourseEntries.Add(worldName, courseLocationList);
                }
            }
        }

        public static void CacheCourseThumbnails(GL gl, string world)
        {
            var thumbnailFolder = Path.Combine(GetRoot(), "UI", "Tex", "Thumbnail");

            foreach (var course in sCourseEntries[world].Keys)
            {
                // Skip the process if this course's thumbnail is already cached
                if (sCourseEntries[world][course].thumbnail != 0)
                {
                    continue;
                }

                var path = Path.Combine(thumbnailFolder, $"{course}.bntx.zs");

                if (!File.Exists(path))
                {
                    path = Path.Combine(thumbnailFolder, "Default.bntx.zs");
                }

                Debug.WriteLine($"Getting Thumbnail {path}");

                byte[] fileBytes = FileUtil.DecompressFile(path);
                var bntx = new BntxFile(new MemoryStream(fileBytes));
                var render = new BfresTextureRender(gl, bntx.Textures[0]);

                sCourseEntries[world][course].thumbnail = (IntPtr)render.ID;
            }
        }

        public class CourseEntry
        {
            public string name;
            public IntPtr thumbnail;
        }
        
        private static string sRomFSRoot;
        private static Dictionary<string, Dictionary<string, CourseEntry>> sCourseEntries = [];
    }
}
