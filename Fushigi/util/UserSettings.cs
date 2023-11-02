using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public static class UserSettings
    {
        public static readonly string SettingsDir = 
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static readonly string SettingsFile = Path.Combine(SettingsDir, "UserSettings.json");
        public static readonly int MaxRecents = 10;
        static Settings AppSettings;

        struct Settings
        {
            public string RomFSPath;
            public string RomFSModPath;
            public Dictionary<string, string> ModPaths;
            public List<string> RecentCourses;
        }

        public static void Load()
        {
            if (!File.Exists(SettingsFile))
            {
                AppSettings.RomFSPath = "";
                AppSettings.ModPaths = new();
                AppSettings.RomFSModPath = "";
                AppSettings.RecentCourses = new List<string>(MaxRecents);
            }
            else
            {
                AppSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsFile));
            }
        }

        public static void Save()
        {
            if (!Directory.Exists(SettingsDir))
            {
                Directory.CreateDirectory(SettingsDir);
            }

            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(AppSettings, Formatting.Indented));
        }

        public static void SetRomFSPath(string path)
        {
            AppSettings.RomFSPath = path;
            Save(); //save setting
        }

        public static void SetModRomFSPath(string path)
        {
            AppSettings.RomFSModPath = path;
            Save(); //save setting
        }

        public static string GetRomFSPath()
        {
            return AppSettings.RomFSPath;
        }

        public static string GetModRomFSPath()
        {
            return AppSettings.RomFSModPath;
        }

        public static void AppendModPath(string modname, string path)
        {
            AppSettings.ModPaths.Add(modname, path);
        }

        public static void AppendRecentCourse(string courseName)
        {
            // please let me know if this isn't a good implementation
            if (AppSettings.RecentCourses.Count == MaxRecents)
            {
                // since we only store the last 10, we push our array once to the left
                // then our new entry is appended on the 9th index
                var oldArray = AppSettings.RecentCourses.ToArray();
                var newArray = new string?[oldArray.Length];
                Array.Copy(oldArray, 1, newArray, 0, oldArray.Length - 1);

                AppSettings.RecentCourses = [.. newArray];
                // put our brand new path at 9
                AppSettings.RecentCourses[MaxRecents - 1] = courseName;
            }
            else
            {
                AppSettings.RecentCourses.Add(courseName);
            }
        }

        public static string? GetLatestCourse()
        {
            int size = AppSettings.RecentCourses.Count;
            
            if (size == 0)
            {
                return null;
            }

            return AppSettings.RecentCourses[size - 1];
        }
    }
}
