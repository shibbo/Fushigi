using Fushigi.Byml;
using Fushigi.course;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi
{
    public class RomFS
    {
        private static string _sRootPath = "";
        private static Dictionary<string, string[]> _sCourseEntries = [];

        public static void setRoot(string rootPath)
        {
            _sRootPath = rootPath;
            _sCourseEntries = CacheCourseFiles();
        }

        public static string GetRoot()
        {
            return _sRootPath;
        }

        public static Dictionary<string, string[]> GetCourseEntries()
        {
            return _sCourseEntries;
        }

        public static Course GetCourse(string courseName)
        {
            return new Course(courseName);
        }

        private static Dictionary<string, string[]> CacheCourseFiles()
        {
            /* common paths to check */
            if (!DirectoryExists("BancMapUnit") || !DirectoryExists("Model") || !DirectoryExists("Stage"))
            {
                throw new Exception("RomFS -- Required folders not found.");
            }

            Dictionary<string, string[]> courseEntries = [];
            string[] loadFiles = GetFiles("/Stage/WorldMapInfo");
            foreach (string loadFile in loadFiles)
            {
                string worldName = Path.GetFileName(loadFile).Split(".game")[0];
                List<string> courseLocationList = new();
                Byml.Byml byml = new(new MemoryStream(File.ReadAllBytes(loadFile)));
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
                if (!courseEntries.ContainsKey(worldName))
                {
                    courseEntries.Add(worldName, courseLocationList.ToArray());
                }
            }

            return courseEntries;
        }

        private static bool DirectoryExists(string path) {
            return Directory.Exists($"{_sRootPath}/{path}");
        }

        private static string[] GetFiles(string path)
        {
            return Directory.GetFiles($"{_sRootPath}/{path}");
        }

        public static byte[] GetFileBytes(string path)
        {
            return File.ReadAllBytes($"{_sRootPath}/{path}");
        }
    }
}
