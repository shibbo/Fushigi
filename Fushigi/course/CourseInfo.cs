using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    [Serializable]
    public class CourseInfo : BymlObject
    {
        public string CourseDifficulty { get; set; }
        public string CourseNameLabel { get; set; }
        public string CourseScreenCaptureMainActor { get; set; }
        public string CourseStartXLinkKey { get; set; }
        public string CourseThumbnailPath { get; set; }
        public int CourseTimer { get; set; }
        public int GlobalCourseId { get; set; }
        public string NeedBadgeIdEnterCourse { get; set; }
        public List<string> SuggestBadgeList { get; set; }
        public List<string> TipsTags { get; set; }
        public List<TipInfo> TipsInfo { get; set; }

        public CourseInfo(string name)
        {
            var courseFilePath = FileUtil.FindContentPath(Path.Combine("Stage", "CourseInfo", $"{name}.game__stage__CourseInfo.bgyml"));
            var byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(courseFilePath)));

            this.Load((BymlHashTable)byml.Root);

            Console.WriteLine();
        }

        public void Save(string name)
        {
            var root = this.Serialize();

            var courseFilePath = FileUtil.FindContentPath(Path.Combine("Stage", "CourseInfo", $"{name}.game__stage__CourseInfo.bgyml"));
            using (var fs = new FileStream(courseFilePath, FileMode.Create, FileAccess.Write))
            {
                new Byml.Byml(root).Save(fs);
            }
        }

        [Serializable]
        public class TipInfo
        {
            public string Label { get; set; }
        }
    }
}
