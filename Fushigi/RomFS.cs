using Fushigi.Bfres;
using Fushigi.Byml;
using Fushigi.gl.Bfres;
using Fushigi.SARC;
using Fushigi.Msbt;
using Fushigi.util;
using Silk.NET.OpenGL;
using System.Diagnostics;
using Fushigi.course;
using Silk.NET.Input;

namespace Fushigi
{
    public class RomFS
    {
        public static void SetRoot(string root, GL gl)
        {           
            if (!IsValidRoot(root))
            {
                return;
            }

            sRomFSRoot = root;
            CacheCourseFiles();
            CacheCourseThumbnails(gl);
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
                Directory.Exists(Path.Combine(root, "UI")) &&
                Directory.Exists(Path.Combine(root, "Mals")) &&
                Directory.Exists(Path.Combine(root, "Stage"));
        }

        public static Dictionary<string, WorldEntry> GetCourseEntries()
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

            var path = Path.Combine(GetRoot(), "Mals", "USen.Product.100.sarc.zs");


            Dictionary<string, string> courseNames = new();
            Dictionary<string, string> worldNames = new();

            if (File.Exists(path))
            {
                var sarc = new SARC.SARC(new(FileUtil.DecompressFile(path)));
                courseNames = new MsbtFile(new MemoryStream(sarc.OpenFile("GameMsg/Name_CourseRemoveLineFeed.msbt"))).Messages;
                worldNames = new MsbtFile(new MemoryStream(sarc.OpenFile("GameMsg/Name_World.msbt"))).Messages;
            }

            string[] loadFiles = GetFiles(Path.Combine("Stage", "WorldMapInfo"));
            foreach (string loadFile in loadFiles)
            {
                string worldName = Path.GetFileName(loadFile).Split(".game")[0];

                if (sCourseEntries.ContainsKey(worldName))
                {
                    return;
                }

                WorldEntry worldEntry = new();
                worldEntry.name = worldName;

                var worldKey = worldName.Replace("World", "WorldNameOrigin");              
                if (worldNames.ContainsKey(worldKey))
                {
                    worldEntry.name = worldNames[worldKey];
                }

                Dictionary<string, WorldEntry.CourseEntry> courseLocationList = new();
                Byml.Byml byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(loadFile)));
                var root = (BymlHashTable)byml.Root;
                var courseList = (BymlArrayNode)root["CourseTable"];
                for (int i = 0; i < courseList.Length; i++)
                {
                    var course = (BymlHashTable)courseList[i];
                    string derp = ((BymlNode<string>)course["StagePath"]).Data;

                    // we need to "fix" our StagePath so it points to our course
                    string courseLocation = Path.GetFileName(derp).Split(".game")[0];

                    WorldEntry.CourseEntry courseEntry = new();
                    var courseInfo = new CourseInfo(courseLocation);
                    if (courseInfo.CourseNameLabel != null && 
                        courseNames.ContainsKey(courseInfo.CourseNameLabel))
                    {
                        courseEntry.name = courseNames[courseInfo.CourseNameLabel];
                    }
                    else
                    {
                        courseEntry.name = "Name not found";
                    }

                    courseLocationList.Add(courseLocation, courseEntry);
                }

                worldEntry.courseEntries = courseLocationList;

                sCourseEntries.Add(worldName, worldEntry);
            }
        }

        public static void CacheCourseThumbnails(GL gl)
        {
            foreach (var world in sCourseEntries.Keys)
            {
                CacheCourseThumbnails(gl, world);
            }
        }

        public static void CacheCourseThumbnails(GL gl, string world)
        {
            var thumbnailFolder = Path.Combine(GetRoot(), "UI", "Tex", "Thumbnail");

            foreach (var course in sCourseEntries[world].courseEntries!.Keys)
            {
                // Skip the process if this course's thumbnail is already cached
                if (sCourseEntries[world].courseEntries![course].thumbnail != null)
                {
                    continue;
                }

                var path = Path.Combine(thumbnailFolder, $"{course}.bntx.zs");

                if (!File.Exists(path))
                {
                    path = Path.Combine(thumbnailFolder, "Default.bntx.zs");
                }

                byte[] fileBytes = FileUtil.DecompressFile(path);
                var bntx = new BntxFile(new MemoryStream(fileBytes));
                var render = new BfresTextureRender(gl, bntx.Textures[0]);

                sCourseEntries[world].courseEntries![course].thumbnail = render;
            }
        }

        public class WorldEntry
        {
            public class CourseEntry
            {
                public string? name;
                public BfresTextureRender? thumbnail;
            }

            public string? name;
            public Dictionary<string, CourseEntry>? courseEntries;
        }

        
        
        private static string sRomFSRoot;
        private static Dictionary<string, WorldEntry> sCourseEntries = [];
    }
}
