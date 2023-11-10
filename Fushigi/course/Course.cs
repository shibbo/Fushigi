
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using ImGuiNET;
using Fushigi.util;
using Fushigi.windowing;
using Fushigi.Byml;
using Fushigi.Byml.Writer;
using Fushigi.Byml.Writer.Primitives;
using Fushigi;
using Fushigi.course;
using Fushigi.rstb;

namespace Fushigi.course
{
    public class Course
    {
        public Course(string courseName)
        {
            mCourseName = courseName;
            mAreas = new List<CourseArea>();
            LoadFromRomFS();
        }

        public string GetName()
        {
            return mCourseName;
        }

        public void LoadFromRomFS()
        {
            var courseFilePath = FileUtil.FindContentPath(
                Path.Combine("BancMapUnit", $"{mCourseName}.bcett.byml.zs")
                );
            var stageParamFilePath = FileUtil.FindContentPath(
                Path.Combine("Stage", "StageParam", $"{mCourseName}.game__stage__StageParam.bgyml")
                );
            /* grab our course information file */
            Byml.Byml courseInfo = new Byml.Byml(new MemoryStream(FileUtil.DecompressFile(courseFilePath)));
            Byml.Byml stageParam = new Byml.Byml(new MemoryStream(File.ReadAllBytes(stageParamFilePath)));

            var stageParamRoot = (BymlHashTable)stageParam.Root;
            var root = (BymlHashTable)courseInfo.Root;

            if (((BymlNode<string>)stageParamRoot["Category"]).Data == "Course1Area") {
                mAreas.Add(new CourseArea(mCourseName));
            }
            else
            {
                var stageList = (BymlArrayNode)root["RefStages"];

                for (int i = 0; i < stageList.Length; i++)
                {
                    string stageParamPath = ((BymlNode<string>)stageList[i]).Data.Replace("Work/", "").Replace(".gyml", ".bgyml");
                    string stageName = Path.GetFileName(stageParamPath).Split(".game")[0];
                    mAreas.Add(new CourseArea(stageName));
                }
            }

            if (root.ContainsKey("Links"))
            {
                var linksArr = root["Links"] as BymlArrayNode;
                mGlobalLinks = new(linksArr);
            }
            else
            {
                mGlobalLinks = new(new BymlArrayNode());
            }
        }

        public List<CourseArea> GetAreas()
        {
            return mAreas;
        }

        public CourseArea GetArea(int idx)
        {
            return mAreas.ElementAt(idx);
        }

        public CourseArea? GetArea(string name)
        {
            foreach (CourseArea area in mAreas)
            {
                if (area.GetName() == name)
                {
                    return area;
                }
            }

            return null;
        }

        public int GetAreaCount()
        {
            return mAreas.Count;
        }

        public void AddGlobalLink()
        {
            CourseLink link = new("Reference");
            mGlobalLinks.GetLinks().Add(link);
        }

        public CourseLinkHolder GetGlobalLinks()
        {
            return mGlobalLinks;
        }

        public void Save()
        {
            RSTB resource_table = new RSTB();
            resource_table.Load();

            SaveAreas(resource_table);

            resource_table.Save();
        }

        public void SaveAreas(RSTB resTable)
        {
            //Save each course area to current romfs folder
            foreach (var area in GetAreas())
            {
                Console.WriteLine($"Saving area {area.GetName()}...");

                area.Save(resTable);
            }
        }

        string mCourseName;
        List<CourseArea> mAreas;
        CourseLinkHolder mGlobalLinks;
    }
}
