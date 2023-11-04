using Fushigi.Byml;
using Fushigi.ui.widgets;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public class CourseUnit
    {
        public CourseUnit()
        {
            this.mWalls = new List<Rail>();
            this.mBeltRails = new List<Rail>();
            this.mModelType = 0;
            this.mSkinDivision = 0;
        }

        public CourseUnit(BymlHashTable tbl)
        {
            mModelType = BymlUtil.GetNodeData<int>(tbl["ModelType"]);
            mSkinDivision = BymlUtil.GetNodeData<int>(tbl["SkinDivision"]);

            if (tbl.ContainsKey("BeltRails"))
            {
                BymlArrayNode belts = tbl["BeltRails"] as BymlArrayNode;

                foreach (BymlHashTable beltsTbl in belts.Array)
                {
                    Rail belt = new Rail();
                    belt.IsClosed = BymlUtil.GetNodeData<bool>(beltsTbl["IsClosed"]);
                    belt.mPoints = new();

                    BymlArrayNode beltsArr = beltsTbl["Points"] as BymlArrayNode;

                    foreach (BymlHashTable pointsTbl in beltsArr.Array)
                    {
                        belt.mPoints.Add(BymlUtil.GetVector3FromArray((BymlArrayNode)pointsTbl["Translate"]));
                    }

                    mBeltRails.Add(belt);
                }
            }

            if (tbl.ContainsKey("Walls"))
            {
                BymlArrayNode wallsNode = tbl["Walls"] as BymlArrayNode;
                List<Rail> walls = new();

                void LoadRail(BymlHashTable railDict, bool isInternal)
                {
                    BymlArrayNode pointsArr = railDict["Points"] as BymlArrayNode;

                    Rail rail = new();
                    rail.IsClosed = BymlUtil.GetNodeData<bool>(railDict["IsClosed"]);
                    rail.mPoints = new List<System.Numerics.Vector3?>();
                    rail.IsInternal = isInternal;

                    foreach (BymlHashTable pointsTbl in pointsArr.Array)
                    {
                        rail.mPoints.Add(BymlUtil.GetVector3FromArray((BymlArrayNode)pointsTbl["Translate"]));
                    }

                    mWalls.Add(rail);
                }

                foreach (BymlHashTable wallsTbl in wallsNode.Array)
                {
                    if (wallsTbl.ContainsKey("ExternalRail"))
                        LoadRail(wallsTbl["ExternalRail"] as BymlHashTable, false);
                    if (wallsTbl.ContainsKey("InternalRail"))
                        LoadRail(wallsTbl["InternalRail"] as BymlHashTable, true);
                }
            }
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable table = new();
            table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>("ModelType", mModelType), "ModelType");
            table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>("SkinDivision", mSkinDivision), "SkinDivision");

            BymlArrayNode beltsArray = new((uint)mBeltRails.Count);

            foreach(Rail belt in mBeltRails)
            {
                BymlHashTable beltNode = new();
                beltNode.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>("IsClosed", belt.IsClosed), "IsClosed");

                BymlArrayNode pointsArr = new((uint)belt.mPoints.Count);

                foreach (System.Numerics.Vector3 point in belt.mPoints)
                {
                    BymlHashTable pointTbl = new();
                    BymlArrayNode translateNode = new(3);
                    translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("X", point.X));
                    translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Y", point.Y));
                    translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Z", point.Z));

                    //beltNode.AddNode(BymlNodeId.Array, translateNode, "Translate");
                    pointTbl.AddNode(BymlNodeId.Array, translateNode, "Translate");
                    pointsArr.AddNodeToArray(pointTbl);
                }

                beltNode.AddNode(BymlNodeId.Array, pointsArr, "Points");
                beltsArray.AddNodeToArray(beltNode);
            }

            table.AddNode(BymlNodeId.Array, beltsArray, "BeltRails");

            BymlArrayNode wallsArray = new((uint)mWalls.Count);

            foreach (Rail rail in mWalls)
            {
                BymlHashTable railNode = new();
                BymlHashTable externalRailNode = new();
                externalRailNode.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>("IsClosed", rail.IsClosed), "IsClosed");

                BymlArrayNode pointsArrayNode = new((uint)rail.mPoints.Count);

                foreach (System.Numerics.Vector3 point in rail.mPoints)
                {
                    BymlHashTable pointDict = new();
                    BymlArrayNode translateNode = new(3);
                    translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("X", point.X));
                    translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Y", point.Y));
                    translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Z", point.Z));

                    pointDict.AddNode(BymlNodeId.Array, translateNode, "Translate");
                    pointsArrayNode.AddNodeToArray(pointDict);
                }

                externalRailNode.AddNode(BymlNodeId.Array, pointsArrayNode, "Points");

                if (rail.IsInternal)
                    railNode.AddNode(BymlNodeId.Hash, externalRailNode, "InternalRail");
                else
                    railNode.AddNode(BymlNodeId.Hash, externalRailNode, "ExternalRail");
                wallsArray.AddNodeToArray(railNode);
            }

            table.AddNode(BymlNodeId.Array, wallsArray, "Walls");

            return table;
        }

        public struct Rail
        {
            public bool IsClosed;
            public List<System.Numerics.Vector3?> mPoints;
            public bool IsInternal;

            public Rail()
            {
                IsInternal = false;
                IsClosed = true;
                mPoints = new List<System.Numerics.Vector3?>();
            }
        }

        public int mModelType;
        public int mSkinDivision;
        public List<Rail> mWalls = new();
        public List<Rail> mBeltRails = new();

        //Editor render objects
        internal List<UnitRailRenderer> WallUnitRenders = new List<UnitRailRenderer>();
        internal List<UnitRailRenderer> BeltUnitRenders = new List<UnitRailRenderer>();
    }

    public class CourseUnitHolder
    {
        public CourseUnitHolder()
        {

        }

        public CourseUnitHolder(BymlArrayNode array)
        {
            foreach (BymlHashTable tbl in array.Array)
            {
                mUnits.Add(new CourseUnit(tbl));
            }
        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode arr = new((uint)mUnits.Count);

            foreach (CourseUnit unit in mUnits)
            {
                arr.AddNodeToArray(unit.BuildNode());
            }

            return arr;
        }

        public List<CourseUnit> mUnits = new();
    }
}
