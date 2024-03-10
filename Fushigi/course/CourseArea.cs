using Fushigi.Byml;
using Fushigi.env;
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
            
            string areaParamPath = FileUtil.FindContentPath(
                Path.Combine("Stage", "AreaParam", $"{mAreaName}.game__stage__AreaParam.bgyml")
                );
            mAreaParams = new AreaParam(new Byml.Byml(new MemoryStream(File.ReadAllBytes(areaParamPath))));

            //Load env settings
            if (mAreaParams.EnvPaletteSetting != null && mAreaParams.EnvPaletteSetting.InitPaletteBaseName != null)
                mInitEnvPalette = new EnvPalette(mAreaParams.EnvPaletteSetting.InitPaletteBaseName);
            else
                mInitEnvPalette = new EnvPalette();


            string levelPath = FileUtil.FindContentPath(
                Path.Combine("BancMapUnit", $"{mAreaName}.bcett.byml.zs")
                );
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
                mRailLinksHolder = new CourseActorToRailLinksHolder(actorLinksArray, mActorHolder, mRailHolder);
            }
            else
            {
                mRailLinksHolder = new();
            }

            if (root.ContainsKey("Links"))
            {
                BymlArrayNode? linksArray = root["Links"] as BymlArrayNode;
                mLinkHolder = new(linksArray);
            }
            else
            { 
                mLinkHolder = new();
            }

            if (root.ContainsKey("SimultaneousGroups"))
            {
                BymlArrayNode? groupsArray = root["SimultaneousGroups"] as BymlArrayNode;
                mGroupsHolder = new CourseGroupHolder(groupsArray);
            }
            else
            {
                mGroupsHolder = new();
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
        }

        public void Save(RSTB resource_table)
        {
            //Save using the configured mod romfs path
            Save(resource_table, Path.Combine(UserSettings.GetModRomFSPath(), "BancMapUnit"));
        }

        public void Save(RSTB resource_table, string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            BymlHashTable root = new();
            root.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(mRootHash), "RootAreaHash");
            root.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mStageParams), "StageParam");
            root.AddNode(BymlNodeId.Array, mRailLinksHolder.SerializeToArray(), "ActorToRailLinks");
            root.AddNode(BymlNodeId.Array, mActorHolder.SerializeToArray(mLinkHolder), "Actors");

            if (mUnitHolder != null)
            {
                root.AddNode(BymlNodeId.Array, mUnitHolder.SerializeToArray(), "BgUnits");
            }

            root.AddNode(BymlNodeId.Array, mLinkHolder.SerializeToArray(), "Links");
            root.AddNode(BymlNodeId.Array, mRailHolder.SerializeToArray(), "Rails");
            root.AddNode(BymlNodeId.Array, mGroupsHolder.SerializeToArray(), "SimultaneousGroups");

            var byml = new Byml.Byml(root);
            var mem = new MemoryStream();
            byml.Save(mem);

            var decomp_size = (uint)mem.Length;

            //Compress and save the course area           
            string levelPath = Path.Combine(folder, $"{mAreaName}.bcett.byml.zs");
            File.WriteAllBytes(levelPath, FileUtil.CompressData(mem.ToArray()));

            //Update resource table
            // filePath is a key not an actual path so we cannot use Path.Combine
            resource_table.SetResource($"BancMapUnit/{mAreaName}.bcett.byml", decomp_size);
        }

        public string GetName()
        {
            return mAreaName;
        }

        public IReadOnlyList<CourseActor> GetActors()
        {
            return mActorHolder.mActors;
        }

        public IReadOnlyList<CourseActor> GetSortedActors()
        {
            if (!mActorHolder.mActors.TrueForAll(mActorHolder.mSortedActors.Contains))
            {
                mActorHolder.mSortedActors = new List<CourseActor>(mActorHolder.mActors);
                mActorHolder.mSortedActors.Sort((x, y) => x.mTranslation.Z.CompareTo(y.mTranslation.Z));
            }   
            return mActorHolder.mSortedActors;
        }

        public string mAreaName;
        public uint mRootHash;
        string mStageParams;
        public AreaParam mAreaParams;
        public CourseActorHolder mActorHolder;
        public CourseRailHolder mRailHolder;
        public CourseActorToRailLinksHolder mRailLinksHolder;
        public CourseLinkHolder mLinkHolder;
        public CourseGroupHolder mGroupsHolder;
        public CourseUnitHolder mUnitHolder;
        public EnvPalette mInitEnvPalette;
    }
}
