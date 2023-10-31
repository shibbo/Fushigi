using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public static class UserSettings
    {
        public static readonly string SettingsPath = "UserSettings.json";
        public static readonly int MaxRecents = 10;
        static Settings AppSettings;

        struct Settings
        {
            public string RomFSPath;
            public Dictionary<string, string> ModPaths;
            public List<string> RecentCourses;
        }

        public static void Load()
        {
            if (!File.Exists(SettingsPath))
            {
                AppSettings.RomFSPath = "";
                AppSettings.ModPaths = new();
                AppSettings.RecentCourses = new List<string>(MaxRecents);
            }
            else
            {
                AppSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsPath));

                RomFS.SetRoot(AppSettings.RomFSPath);
            }
        }

        public static void Save()
        {
            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(AppSettings, Formatting.Indented));
        }

        public static void SetRomFSPath(string path)
        {
            AppSettings.RomFSPath = path;
        }

        public static string GetRomFSPath()
        {
            return AppSettings.RomFSPath;
        }

        public static void AppendModPath(string modname, string path)
        {
            AppSettings.ModPaths.Add(modname, path);
        }

        public static void AppendRecentFile(string path)
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
                AppSettings.RecentCourses[MaxRecents - 1] = path;
            }
            else
            {
                AppSettings.RecentCourses.Add(path);
            }
        }
    }
}
