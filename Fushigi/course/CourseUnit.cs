using Fushigi.Byml;
using Fushigi.ui.widgets;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Fushigi.course.CourseUnit;

namespace Fushigi.course
{
    public class CourseUnit
    {
        public CourseUnit()
        {
            this.Walls = new List<Wall>();
            this.mBeltRails = new List<BGUnitRail>();
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
                    this.mBeltRails.Add(new BGUnitRail(this, belt));
                }
            }

            if (tbl.ContainsKey("Walls"))
            {
                BymlArrayNode wallsNode = tbl["Walls"] as BymlArrayNode;
                this.Walls = new List<Wall>();

                BGUnitRail LoadRail(BymlHashTable railDict, bool isInternal = false)
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

                    return new BGUnitRail(this, rail);
                }

                foreach (BymlHashTable wallsTbl in wallsNode.Array)
                {
                    Wall wall = new Wall(this);
                    this.Walls.Add(wall);

                    if (wallsTbl.ContainsKey("ExternalRail"))
                        wall.ExternalRail = LoadRail(wallsTbl["ExternalRail"] as BymlHashTable);
                    if (wallsTbl.ContainsKey("InternalRails"))
                    {
                        var railList = wallsTbl["InternalRails"] as BymlArrayNode;
                        foreach (BymlHashTable rail in railList.Array)
                            wall.InternalRails.Add(LoadRail(rail, true));
                    }
                }
            }
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable table = new();
            table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>("ModelType", mModelType), "ModelType");
            table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>("SkinDivision", mSkinDivision), "SkinDivision");

            BymlArrayNode beltsArray = new((uint)mBeltRails.Count);

            foreach(var belt in mBeltRails)
            {
                var rail = belt.Save();

                BymlHashTable beltNode = new();
                beltNode.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>("IsClosed", belt.IsClosed), "IsClosed");

                BymlArrayNode pointsArr = new((uint)rail.mPoints.Count);

                foreach (System.Numerics.Vector3 point in rail.mPoints)
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

            BymlArrayNode wallsArray = new((uint)this.Walls.Count);

            foreach (Wall wall in this.Walls)
            {
                BymlHashTable SaveRail(Rail rail)
                {
                    BymlHashTable railNode = new();
                    railNode.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>("IsClosed", rail.IsClosed), "IsClosed");

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

                    railNode.AddNode(BymlNodeId.Array, pointsArrayNode, "Points");

                    return railNode;
                }

                BymlHashTable wallNode = new();
                if (wall.InternalRails.Count > 0)
                {
                    BymlArrayNode internaiRailListNode = new BymlArrayNode();
                    wallNode.AddNode(BymlNodeId.Array, internaiRailListNode, "InternalRails");

                    foreach (var rail in wall.InternalRails)
                        internaiRailListNode.AddNodeToArray(SaveRail(rail.Save()));
                }
                wallNode.AddNode(BymlNodeId.Hash, SaveRail(wall.ExternalRail.Save()), "ExternalRail");
                wallsArray.AddNodeToArray(wallNode);
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

        //Editor render objects
        internal List<Wall> Walls = new List<Wall>();
        internal List<BGUnitRail> mBeltRails = new List<BGUnitRail>();

        //Editor toggle
        public bool Visible = true;
    }

    public class Wall
    {
        internal BGUnitRail ExternalRail;
        internal List<BGUnitRail> InternalRails = new List<BGUnitRail>();

        internal Wall(CourseUnit unit)
        {
            ExternalRail = new BGUnitRail(unit, new CourseUnit.Rail());
        }
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
