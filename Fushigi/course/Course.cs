
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
        public Course(string courseName, RomFS romFS)
        {
            mCourseName = courseName;
            mAreas = new List<CourseArea>();
            mRomFs = romFS;
            LoadFromRomFS();   
        }

        public void LoadFromRomFS()
        {
            byte[] courseBytes = mRomFs.GetFileBytes($"BancMapUnit/{mCourseName}.bcett.byml.zs");
            /* grab our course information file */
            Byml.Byml courseInfo = new Byml.Byml(new MemoryStream(FileUtil.DecompressData(courseBytes)));

            var root = (BymlHashTable)courseInfo.Root;
            var stageList = (BymlArrayNode)root["RefStages"];

            for (int i = 0; i < stageList.Length; i++)
            {
                string stageParamPath = ((BymlNode<string>)stageList[i]).Data.Replace("Work/", "").Replace(".gyml", ".bgyml");
                string stageName = Path.GetFileName(stageParamPath).Split(".game")[0];
                mAreas.Add(new CourseArea(stageName, mRomFs));
            }
        }

        public CourseArea GetArea(int idx)
        {
            return mAreas.ElementAt(idx);
        }

        public CourseArea GetArea(string name)
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
        private RomFS mRomFs;
    }
}
