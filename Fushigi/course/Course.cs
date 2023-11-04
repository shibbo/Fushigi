
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using ImGuiNET;
using Fushigi.util;
using Fushigi.windowing;
using Silk.NET.OpenGL.Extensions.ImGui;
using Fushigi.Byml;
using Fushigi.Byml.Writer;
using Fushigi.Byml.Writer.Primitives;
using Fushigi;
using Fushigi.course;

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
            var courseFilePath = FileUtil.FindContentPath($"BancMapUnit{Path.DirectorySeparatorChar}{mCourseName}.bcett.byml.zs");
            var stageParamFilePath = FileUtil.FindContentPath($"Stage{Path.DirectorySeparatorChar}StageParam{Path.DirectorySeparatorChar}{mCourseName}.game__stage__StageParam.bgyml");
            /* grab our course information file */
            Byml.Byml courseInfo = new Byml.Byml(new MemoryStream(FileUtil.DecompressFile(courseFilePath)));
            Byml.Byml stageParam = new Byml.Byml(new MemoryStream(File.ReadAllBytes(stageParamFilePath)));

            var stageParamRoot = (BymlHashTable)stageParam.Root;

            if (((BymlNode<string>)stageParamRoot["Category"]).Data == "Course1Area") {
                mAreas.Add(new CourseArea(mCourseName));
            }
            else
            {
                var root = (BymlHashTable)courseInfo.Root;
                var stageList = (BymlArrayNode)root["RefStages"];

                for (int i = 0; i < stageList.Length; i++)
                {
                    string stageParamPath = ((BymlNode<string>)stageList[i]).Data.Replace($"Work{Path.DirectorySeparatorChar}", "").Replace(".gyml", ".bgyml");
                    string stageName = Path.GetFileName(stageParamPath).Split(".game")[0];
                    mAreas.Add(new CourseArea(stageName));
                }
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

        string mCourseName;
        List<CourseArea> mAreas;
    }
}
