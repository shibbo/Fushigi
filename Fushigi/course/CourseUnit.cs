using Fushigi.Byml;
using Fushigi.ui.widgets;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Fushigi.course.CourseUnit;
using Vector3 = System.Numerics.Vector3;

namespace Fushigi.course
{
    public class CourseUnit
    {
        public enum ModelType
        {
            Solid,
            SemiSolid,
            NoCollision,
            Bridge
        }

        public enum SkinDivision
        {
            FieldA,
            FieldB
        }

        public CourseUnit()
        {
            this.Walls = new List<Wall>();
            this.mBeltRails = new List<BGUnitRail>();
            this.mModelType = 0;
            this.mSkinDivision = 0;
        }

        public CourseUnit(BymlHashTable tbl)
        {
            mModelType = (ModelType)BymlUtil.GetNodeData<int>(tbl["ModelType"]);
            mSkinDivision = (SkinDivision)BymlUtil.GetNodeData<int>(tbl["SkinDivision"]);

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
            table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>("ModelType", (int)mModelType), "ModelType");
            table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>("SkinDivision", (int)mSkinDivision), "SkinDivision");

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

                    BymlArrayNode pointsArrayNode = new();

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
            public List<Vector3?> mPoints;
            public bool IsInternal;

            public Rail()
            {
                IsInternal = false;
                IsClosed = true;
                mPoints = new List<Vector3?>();
            }
        }

        public ModelType mModelType;
        public SkinDivision mSkinDivision;

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

    public class TilesComponent
    {
        public enum SlopeType
        {
            UpperLeft,
            UpperRight,
            LowerLeft,
            LowerRight,
        }

        public Vector3 mOrigin;
        public readonly InfiniteTileMap mTileMap = new();
        public readonly List<(int width, int height, SlopeType type)> mSlopes = [];

        public static TilesComponent CreateFromWall(Wall wall)
        {
            TilesComponent component = new TilesComponent();

            HashSet<(int x, int y)> blockedTiles = [];

            Vector2[] externPoints2D = new Vector2[wall.ExternalRail.Points.Count];
            Vector2[][] internPoints2D = new Vector2[wall.InternalRails.Count][];

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);

            for (int i = 0; i < wall.ExternalRail.Points.Count; i++)
            {
                BGUnitRail.RailPoint? point = wall.ExternalRail.Points[i];
                externPoints2D[i] = new Vector2(point.Position.X, point.Position.Y);
                min.X = MathF.Min(min.X, point.Position.X);
                min.Y = MathF.Min(min.Y, point.Position.Y);
                min.Z = MathF.Min(min.Z, point.Position.Z);
                max.X = MathF.Max(max.X, point.Position.X);
                max.Y = MathF.Max(max.Y, point.Position.Y);
                max.Z = MathF.Max(max.Z, point.Position.Z);
            }

            for (int iInternalRail = 0; iInternalRail < wall.InternalRails.Count; iInternalRail++)
            {
                var internalRail = wall.InternalRails[iInternalRail];
                internPoints2D[iInternalRail] = new Vector2[internalRail.Points.Count];

                for (int i = 0; i < internalRail.Points.Count; i++)
                {
                    BGUnitRail.RailPoint? point = internalRail.Points[i];
                    var point2D = new Vector2(point.Position.X, point.Position.Y);
                    internPoints2D[iInternalRail][i] = point2D;
                    min.X = MathF.Min(min.X, point.Position.X);
                    min.Y = MathF.Min(min.Y, point.Position.Y);
                    min.Z = MathF.Min(min.Z, point.Position.Z);
                    max.X = MathF.Max(max.X, point.Position.X);
                    max.Y = MathF.Max(max.Y, point.Position.Y);
                    max.Z = MathF.Max(max.Z, point.Position.Z);
                }
            }

            var size = max - min;
            var size2D = new Vector2(size.X, size.Y);
            component.mOrigin = min;
            var origin2D = new Vector2(min.X, min.Y);

            #region Slopes
            for (int iSegment = 0; iSegment < externPoints2D.Length; iSegment++)
            {
                var p0 = externPoints2D[iSegment];
                var p1 = externPoints2D[(iSegment + 1) % externPoints2D.Length];

                if (
                    MathF.IEEERemainder(p0.X - min.X, 1) != 0 ||
                    MathF.IEEERemainder(p0.Y - min.Y, 1) != 0 ||
                    MathF.IEEERemainder(p1.X - min.X, 1) != 0 ||
                    MathF.IEEERemainder(p1.Y - min.Y, 1) != 0
                    )
                {
                    //slope cannot be placed off (tile)grid
                    continue;
                }

                var slope = Math.Abs((p1.Y - p0.Y) / (p1.X - p0.X));

                if (slope is not (1 or 0.5f))
                    //slope is not supported by the game
                    continue;

                SlopeType slopeType = default;
                if (p0.X < p1.X && p0.Y < p1.Y)
                    slopeType = SlopeType.LowerRight;
                else if (p0.X < p1.X && p0.Y > p1.Y)
                    slopeType = SlopeType.LowerLeft;
                else if (p0.X > p1.X && p0.Y < p1.Y)
                    slopeType = SlopeType.UpperRight;
                else if (p0.X > p1.X && p0.Y > p1.Y)
                    slopeType = SlopeType.UpperLeft;

                int slopeWidth = (int)Math.Min(slope, 1);
                int slopeHeight = (int)slope;

                int slopeCount = (int)Math.Abs(p1.X - p0.X) / slopeWidth;

                for (int iSlope = 0; iSlope < slopeCount; iSlope++)
                {
                    var triP0 = p0 + (p1 - p0) * iSlope / slopeCount - origin2D;
                    var triP1 = p0 + (p1 - p0) * (iSlope + 1) / slopeCount - origin2D;
                    var triP2 =
                        (slopeType is SlopeType.LowerRight or SlopeType.UpperLeft) ?
                        new Vector2(triP0.X, triP1.Y) :
                        new Vector2(triP1.X, triP0.Y);

                    int tileX = Math.Min((int)triP0.X, (int)triP1.X);
                    int tileY = Math.Min((int)triP0.Y, (int)triP1.Y);

                    blockedTiles.Add((tileX, tileY));

                    if (slope == 0.5f)
                        blockedTiles.Add((tileX + 1, tileY));

                    component.mSlopes.Add((slopeWidth, slopeHeight, slopeType));
                }
            }
            #endregion

            int? FillFunction((int x, int y) tilePos)
            {
                var (x, y) = tilePos;

                int windingNum;
                if (blockedTiles.Contains((x, y)))
                    return null;

                bool isHole = false;

                for (int iInternalRail = 0; iInternalRail < wall.InternalRails.Count; iInternalRail++)
                {
                    Array.Reverse(internPoints2D[iInternalRail]);

                    windingNum = MathUtil.PolygonWindingNumber(
                    origin2D + new Vector2(x, y) + new Vector2(0.5f), internPoints2D[iInternalRail]);

                    Array.Reverse(internPoints2D[iInternalRail]);

                    if (windingNum != 0)
                    {
                        isHole = true;
                        break;
                    }
                }

                if (isHole)
                    return null;


                windingNum = MathUtil.PolygonWindingNumber(
                    origin2D + new Vector2(x, y) + new Vector2(0.5f), externPoints2D);
                bool isInside = windingNum != 0;

                return isInside ? 0 : null;
            }

            component.mTileMap.FillTiles(FillFunction, Vector2.Zero, size2D);

            return component;
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
