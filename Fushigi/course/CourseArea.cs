using Fushigi.Byml;
using Fushigi.util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZstdNet;

namespace Fushigi.course
{
    public class CourseArea
    {
        public CourseArea(string areaName) {
            mAreaName = areaName;
            Load();
        }

        public void Load()
        {
            string areaParamPath = FileUtil.FindContentPath($"Stage/AreaParam/{mAreaName}.game__stage__AreaParam.bgyml");
            mAreaParams = new AreaParam(new Byml.Byml(new MemoryStream(File.ReadAllBytes(areaParamPath))));

            string levelPath = FileUtil.FindContentPath($"BancMapUnit/{mAreaName}.bcett.byml.zs");
            byte[] levelBytes = FileUtil.DecompressFile(levelPath);
            mLevelByml = new Byml.Byml(new MemoryStream(levelBytes));
        }

        public void Save()
        {
            //Save using the configured mod romfs path
            Save($"{UserSettings.GetModRomFSPath()}/BancMapUnit");
        }

        public void Save(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var byml = new Byml.Byml(this.GetRootNode());
            //Save byml into memory to be compressed
            var mem = new MemoryStream();
            byml.Save(mem);

            //Compress and save the course area
            string levelPath = $"{folder}/{mAreaName}.bcett.byml.zs";
            File.WriteAllBytes(levelPath, FileUtil.CompressData(mem.ToArray()));
        }

        public string GetName()
        {
            return mAreaName;
        }

        public IBymlNode GetRootNode()
        {
            return mLevelByml.Root;
        }

        string mAreaName;
        public AreaParam mAreaParams;
        Byml.Byml mLevelByml;

        public class AreaParam
        {
            public AreaParam(Byml.Byml byml)
            {
                mByml = byml;
            }

            public bool ContainsParam(string param)
            {
                return ((BymlHashTable)mByml.Root).ContainsKey(param);
            }

            public object GetParam(BymlHashTable node, string paramName, string paramType)
            {
                switch (paramType)
                {
                    case "String":
                        return ((BymlNode<string>)node[paramName]).Data;
                    case "Bool":
                        return ((BymlNode<bool>)node[paramName]).Data;
                    case "Float":
                        return ((BymlNode<float>)node[paramName]).Data;
                }

                return null;
            }

            public BymlHashTable GetRoot()
            {
                return (BymlHashTable)mByml.Root;
            }

            /*
            public bool ContainsSkinParam(string param)
            {
                return ((BymlHashTable)((BymlHashTable)mByml.Root)["SkinParam"]).ContainsKey(param);
            }
            */

            public class SkinParam
            {
                public bool mDisableBgUnitDecoA;
                public string mFieldA;
                public string mFieldB;
                public string mObject;
            }

            Byml.Byml mByml;
        }
    }
}
