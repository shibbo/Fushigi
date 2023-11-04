using Fushigi.Byml;
using Fushigi.rstb;
using Fushigi.ui.widgets;
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
            string areaParamPath = FileUtil.FindContentPath(Path.Combine("Stage", "AreaParam", $"{mAreaName}.game__stage__AreaParam.bgyml"));
            mAreaParams = new AreaParam(new Byml.Byml(new MemoryStream(File.ReadAllBytes(areaParamPath))));

            string levelPath = FileUtil.FindContentPath(Path.Combine("BancMapUnit", $"{mAreaName}.bcett.byml.zs"));
            byte[] levelBytes = FileUtil.DecompressFile(levelPath);
            var byml = new Byml.Byml(new MemoryStream(levelBytes));

            BymlHashTable? root = byml.Root as BymlHashTable;

            mRootHash = BymlUtil.GetNodeData<uint>(root["RootAreaHash"]);
            mStageParams = BymlUtil.GetNodeData<string>(root["StageParam"]);

            if (root.ContainsKey("Actors"))
            {
                BymlArrayNode actorsArray = (BymlArrayNode)root["Actors"];
                mActorHolder = new CourseActorHolder(actorsArray);
            }
            else
            {
                mActorHolder = new();
            }

            if (root.ContainsKey("Rails"))
            {
                BymlArrayNode railsArray = (BymlArrayNode)root["Rails"];
                mRailHolder = new(railsArray);
            }
            else
            {
                mRailHolder = new();
            }

            if (root.ContainsKey("ActorToRailLinks"))
            {
                BymlArrayNode? actorLinksArray = root["ActorToRailLinks"] as BymlArrayNode;
                mRailLinks = new CourseActorToRailLinks(actorLinksArray, mActorHolder, mRailHolder);
            }
            else
            {
                mRailLinks = new();
            }

            if (root.ContainsKey("Links"))
            {
                BymlArrayNode? linksArray = root["Links"] as BymlArrayNode;
                mLinkHolder = new(linksArray, mActorHolder);
            }
            else
            { 
                mLinkHolder = new();
            }

            if (root.ContainsKey("SimultaneousGroups"))
            {
                BymlArrayNode? groupsArray = root["SimultaneousGroups"] as BymlArrayNode;
                mGroups = new CourseGroupHolder(groupsArray, mActorHolder);
            }
            else
            {
                mGroups = new();
            }

            if (root.ContainsKey("BgUnits"))
            {
                BymlArrayNode? unitsArray = root["BgUnits"] as BymlArrayNode;
                mUnitHolder = new CourseUnitHolder(unitsArray);
            }
            else
            {
                mUnitHolder = new();
            }

            //Load rail render tools
            foreach (var unit in mUnitHolder.mUnits)
            {
                unit.WallUnitRenders.Clear();
                unit.BeltUnitRenders.Clear();

                foreach (var wall in unit.mWalls)
                    unit.WallUnitRenders.Add(new UnitRailRenderer(unit, wall));
                foreach (var wall in unit.mBeltRails)
                    unit.BeltUnitRenders.Add(new UnitRailRenderer(unit, wall));
            }
        }

        public void Save(RSTB resource_table)
        {
            //Save each wall back attached to the renderer
            //Clear each course unit holder
            foreach (var unit in mUnitHolder.mUnits)
            {
                unit.mWalls.Clear();
                unit.mBeltRails.Clear();

                //Add each wall back from the renderer
                foreach (var wall_rail in unit.WallUnitRenders)
                    unit.mWalls.Add(wall_rail.Save());
                foreach (var belt_rail in unit.BeltUnitRenders)
                    unit.mBeltRails.Add(belt_rail.Save());
            }

            //Save using the configured mod romfs path
            Save(resource_table, Path.Combine(UserSettings.GetModRomFSPath(), "BancMapUnit"));
        }

        public void Save(RSTB resource_table, string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            BymlHashTable root = new();
            root.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>("RootAreaHash", mRootHash), "RootAreaHash");
            root.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("StageParam", mStageParams), "StageParam");
            root.AddNode(BymlNodeId.Array, mRailLinks.SerializeToArray(), "ActorToRailLinks");
            root.AddNode(BymlNodeId.Array, mActorHolder.SerializeToArray(), "Actors");

            if (mUnitHolder != null)
            {
                root.AddNode(BymlNodeId.Array, mUnitHolder.SerializeToArray(), "BgUnits");
            }

            root.AddNode(BymlNodeId.Array, mLinkHolder.SerializeToArray(), "Links");
            root.AddNode(BymlNodeId.Array, mRailHolder.SerializeToArray(), "Rails");
            root.AddNode(BymlNodeId.Array, mGroups.SerializeToArray(), "SimultaneousGroups");

            var byml = new Byml.Byml(root);
            var mem = new MemoryStream();
            byml.Save(mem);

            var decomp_size = (uint)mem.Length;

            //Compress and save the course area
            string levelPath = $"{folder}/{mAreaName}.bcett.byml.zs";
            File.WriteAllBytes(levelPath, FileUtil.CompressData(mem.ToArray()));

            //Update resource table
            resource_table.SetResource($"BancMapUnit/{mAreaName}.bcett.byml", decomp_size);
        }

        public string GetName()
        {
            return mAreaName;
        }

        public List<CourseActor> GetActors()
        {
            return mActorHolder.GetActors();
        }

        string mAreaName;
        public uint mRootHash;
        string mStageParams;
        public AreaParam mAreaParams;
        public CourseActorHolder mActorHolder;
        public CourseRailHolder mRailHolder;
        public CourseActorToRailLinks mRailLinks;
        public CourseLinkHolder mLinkHolder;
        //public List<CourseLink> mLinks;
        public CourseGroupHolder mGroups;
        public CourseUnitHolder mUnitHolder;

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
