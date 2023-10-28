using Fushigi.Byml;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
            string areaParamPath = $"{RomFS.GetRoot()}/Stage/AreaParam/{mAreaName}.game__stage__AreaParam.bgyml";
            mAreaParams = new AreaParam(new Byml.Byml(new MemoryStream(File.ReadAllBytes(areaParamPath))));
        }

        public string GetName()
        {
            return mAreaName;
        }

        string mAreaName;
        public AreaParam mAreaParams;

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
