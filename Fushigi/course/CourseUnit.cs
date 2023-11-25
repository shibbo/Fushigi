using Fushigi.Byml;
using Fushigi.course.terrain_processing;
using Fushigi.util;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;

namespace Fushigi.course
{
    public class CourseUnit
    {
        public readonly static string[] ModelTypeNames = Enum.GetNames(typeof(ModelType));
        public readonly static string[] SkinDivisionNames = Enum.GetNames(typeof(SkinDivision));

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
            this.mModelType = ModelType.Solid;
            this.mSkinDivision = SkinDivision.FieldA;
        }

        public CourseUnit(BymlHashTable tbl)
        {
            mModelType = (ModelType)BymlUtil.GetNodeData<int>(tbl["ModelType"]);
            mSkinDivision = (SkinDivision)BymlUtil.GetNodeData<int>(tbl["SkinDivision"]);

            if (tbl.ContainsKey("BeltRails"))
            {
                BymlArrayNode belts = (BymlArrayNode)tbl["BeltRails"];

                foreach (BymlHashTable beltsTbl in belts.Array)
                {
                    mBeltRails.Add(LoadRailNode(beltsTbl));
                }
            }

            if (tbl.ContainsKey("Walls"))
            {
                BymlArrayNode wallsNode = (BymlArrayNode)tbl["Walls"];
                this.Walls = new List<Wall>();

                foreach (BymlHashTable wallsTbl in wallsNode.Array)
                {
                    Wall wall = new Wall(this);
                    this.Walls.Add(wall);

                    if (wallsTbl.ContainsKey("ExternalRail"))
                        wall.ExternalRail = LoadRailNode((BymlHashTable)wallsTbl["ExternalRail"]);
                    if (wallsTbl.ContainsKey("InternalRails"))
                    {
                        var railList = (BymlArrayNode)wallsTbl["InternalRails"];
                        foreach (BymlHashTable rail in railList.Array)
                            wall.InternalRails.Add(LoadRailNode(rail, true));
                    }
                }
            }

            GenerateTileSubUnits();
        }

        private BGUnitRail LoadRailNode(BymlHashTable railDict, bool isInternal = false)
        {
            BymlArrayNode pointsArr = (BymlArrayNode)railDict["Points"];

            BGUnitRail rail = new(this)
            {
                IsClosed = BymlUtil.GetNodeData<bool>(railDict["IsClosed"]),
                IsInternal = isInternal
            };

            foreach (BymlHashTable pointsTbl in pointsArr.Array)
            {
                var position = BymlUtil.GetVector3FromArray((BymlArrayNode)pointsTbl["Translate"]);
                rail.Points.Add(new BGUnitRail.RailPoint(rail, position));
            }

            return rail;
        }

        private BymlHashTable BuildRailNode(BGUnitRail rail)
        {
            BymlHashTable railNode = new();
            railNode.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>(rail.IsClosed), "IsClosed");

            BymlArrayNode pointsArrayNode = new();

            foreach (BGUnitRail.RailPoint point in rail.Points)
            {
                var position = point.Position;
                BymlHashTable pointDict = new();
                BymlArrayNode translateNode = new(3);
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(position.X));
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(position.Y));
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(position.Z));

                pointDict.AddNode(BymlNodeId.Array, translateNode, "Translate");
                pointsArrayNode.AddNodeToArray(pointDict);
            }

            railNode.AddNode(BymlNodeId.Array, pointsArrayNode, "Points");

            return railNode;
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable table = new();
            table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>((int)mModelType), "ModelType");
            table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>((int)mSkinDivision), "SkinDivision");

            BymlArrayNode beltsArray = new((uint)mBeltRails.Count);

            foreach (var belt in mBeltRails)
            {
                beltsArray.AddNodeToArray(BuildRailNode(belt));
            }

            table.AddNode(BymlNodeId.Array, beltsArray, "BeltRails");

            BymlArrayNode wallsArray = new((uint)this.Walls.Count);

            foreach (Wall wall in this.Walls)
            {
                BymlHashTable wallNode = new();
                if (wall.InternalRails.Count > 0)
                {
                    BymlArrayNode internalRailListNode = new BymlArrayNode();
                    wallNode.AddNode(BymlNodeId.Array, internalRailListNode, "InternalRails");

                    foreach (var rail in wall.InternalRails)
                        internalRailListNode.AddNodeToArray(BuildRailNode(rail));
                }
                wallNode.AddNode(BymlNodeId.Hash, BuildRailNode(wall.ExternalRail), "ExternalRail");
                wallsArray.AddNodeToArray(wallNode);
            }

            table.AddNode(BymlNodeId.Array, wallsArray, "Walls");

            return table;
        }

        public void GenerateTileSubUnits()
        {
            mTileSubUnits.Clear();

            if (mModelType == ModelType.Bridge)
            {
                foreach (var rail in mBeltRails)
                {
                    mTileSubUnits.Add(TileSubUnit.CreateFromRails(rail, Array.Empty<BGUnitRail>(),
                        isBridgeModel: true));
                }
            }
            else
            {
                foreach (var wall in Walls)
                {
                    mTileSubUnits.Add(TileSubUnit.CreateFromRails(wall.ExternalRail, wall.InternalRails,
                        isBridgeModel: false));
                }
            }
        }

        public ModelType mModelType;
        public SkinDivision mSkinDivision;

        internal List<Wall> Walls = [];
        internal List<BGUnitRail> mBeltRails = [];

        internal List<TileSubUnit> mTileSubUnits = [];

        //Editor toggle
        public bool Visible = true;
    }

    public class Wall
    {
        internal BGUnitRail ExternalRail;
        internal List<BGUnitRail> InternalRails = [];

        internal Wall(CourseUnit unit)
        {
            ExternalRail = new BGUnitRail(unit);
        }
    }

    public class BGUnitRail(CourseUnit unit)
    {
        public readonly CourseUnit mCourseUnit = unit;

        public List<RailPoint> Points = [];

        public bool IsClosed = false;

        public bool IsInternal = false;

        public class RailPoint(BGUnitRail rail, Vector3 position)
        {
            public readonly BGUnitRail mRail = rail;
            public Vector3 Position = position;
        }


    }

    public class TileSubUnit
    {
        public enum SlopePositioning
        {
            CornerTL,
            CornerTR,
            CornerBL,
            CornerBR,
        }

        public Vector3 mOrigin;
        public readonly InfiniteTileMap mTileMap = new();
        public readonly List<(int x, int y, int width, int height, SlopePositioning type)> mSlopes = [];

        public IEnumerable<(int? tileIdEdge, int? tileIdGround, Vector2 pos)> GetTiles(Vector2 clipRectMin, Vector2 clipRectMax)
            => mTileMap.GetTiles(clipRectMin, clipRectMax).Select(x =>
            {
                (int tileIdDefault, int tileIdSemiSolidGround) = TileIDLookup.SplitCombinedTileId(x.tileID);
                var (tileIdEdge, tileIdGround) = mCourseUnit.mModelType switch
                {
                    CourseUnit.ModelType.Solid => ((int?)tileIdDefault, (int?)tileIdDefault),
                    CourseUnit.ModelType.SemiSolid =>   (tileIdDefault,       tileIdSemiSolidGround!=TileIDLookup.SEMISOLID_EMPTY?
                                                                              tileIdSemiSolidGround : null),
                    CourseUnit.ModelType.NoCollision => (tileIdDefault,       null),
                    CourseUnit.ModelType.Bridge =>      (null,                tileIdDefault),
                    _ => throw new Exception()
                };

                return (tileIdEdge, tileIdGround, x.position);
            });

        internal static TileSubUnit CreateFromRails(BGUnitRail mainRail, IReadOnlyList<BGUnitRail> internalRails, bool isBridgeModel)
        {
            TileSubUnit component = new();
            component.mCourseUnit = mainRail.mCourseUnit;

            HashSet<(int x, int y)> blockedTiles = [];
            List<(int x, int y)> bridgeTiles = [];

            Vector2[] mainPolyPoints2D = new Vector2[mainRail.Points.Count];
            Vector2[][] subtractPolyPoints2D = new Vector2[internalRails.Count][];

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);

            for (int i = 0; i < mainRail.Points.Count; i++)
            {
                Vector3 point = mainRail.Points[i].Position;
                mainPolyPoints2D[i] = new Vector2(point.X, point.Y);
                min.X = MathF.Min(min.X, point.X);
                min.Y = MathF.Min(min.Y, point.Y);
                min.Z = MathF.Min(min.Z, point.Z);
                max.X = MathF.Max(max.X, point.X);
                max.Y = MathF.Max(max.Y, point.Y);
                max.Z = MathF.Max(max.Z, point.Z);
            }

            for (int iInternalRail = 0; iInternalRail < internalRails.Count; iInternalRail++)
            {
                var internalRail = internalRails[iInternalRail];
                subtractPolyPoints2D[iInternalRail] = new Vector2[internalRail.Points.Count];

                for (int i = 0; i < internalRail.Points.Count; i++)
                {
                    Vector3 point = internalRail.Points[i].Position;
                    var point2D = new Vector2(point.X, point.Y);
                    subtractPolyPoints2D[iInternalRail][i] = point2D;
                }
            }

            var size = max - min;
            var size2D = new Vector2(size.X, size.Y);
            component.mOrigin = min;
            var origin2D = new Vector2(min.X, min.Y);

            #region Slopes (and bridges)
            int segmentCount = mainPolyPoints2D.Length;
            if (!mainRail.IsClosed)
                segmentCount--;

            for (int iSegment = 0; iSegment < segmentCount; iSegment++)
            {
                var p0 = mainPolyPoints2D[iSegment];
                var p1 = mainPolyPoints2D[(iSegment + 1) % mainPolyPoints2D.Length];

                if (
                    MathF.IEEERemainder(p0.X - min.X, 1) != 0 ||
                    MathF.IEEERemainder(p0.Y - min.Y, 1) != 0 ||
                    MathF.IEEERemainder(p1.X - min.X, 1) != 0 ||
                    MathF.IEEERemainder(p1.Y - min.Y, 1) != 0
                    )
                {
                    //slope/bridge cannot be placed off (tile)grid
                    continue;
                }

                var slope = Math.Abs((p1.Y - p0.Y) / (p1.X - p0.X));

                if (slope is not (1 or 0.5f or 0))
                    // slope/bridge angle is not supported by the game
                    continue;

                SlopePositioning slopeType = default;
                if (p0.X < p1.X && p0.Y < p1.Y)
                    slopeType = SlopePositioning.CornerBR;
                else if (p0.X < p1.X && p0.Y > p1.Y)
                    slopeType = SlopePositioning.CornerBL;
                else if (p0.X > p1.X && p0.Y < p1.Y)
                    slopeType = SlopePositioning.CornerTR;
                else if (p0.X > p1.X && p0.Y > p1.Y)
                    slopeType = SlopePositioning.CornerTL;

                int slopeWidth = slope == 0 ? 1 : (int)Math.Max(1f / slope, 1);
                int slopeHeight = (int)Math.Max(slope, 1);

                int slopeCount = (int)Math.Abs(p1.X - p0.X) / slopeWidth;

                for (int iSlope = 0; iSlope < slopeCount; iSlope++)
                {
                    var triP0 = p0 + (p1 - p0) * iSlope / slopeCount - origin2D;
                    var triP1 = p0 + (p1 - p0) * (iSlope + 1) / slopeCount - origin2D;

                    int tileX = Math.Min((int)triP0.X, (int)triP1.X);
                    int tileY = Math.Min((int)triP0.Y, (int)triP1.Y);



                    if (slope == 0.0f)
                    {
                        bridgeTiles.Add((tileX, tileY - 1));
                        continue;
                    }

                    blockedTiles.Add((tileX, tileY));
                    if (isBridgeModel)
                        bridgeTiles.Add((tileX, tileY - 1));

                    if (slopeWidth == 2)
                    {
                        blockedTiles.Add((tileX + 1, tileY));
                        if (isBridgeModel)
                            bridgeTiles.Add((tileX + 1, tileY - 1));
                    }

                    component.mSlopes.Add((tileX, tileY, slopeWidth, slopeHeight, slopeType));
                }
            }
            #endregion

            int? IsInside((int x, int y) tilePos)
            {
                var (x, y) = tilePos;

                int windingNum;
                if (blockedTiles.Contains((x, y)))
                    return null;

                bool isHole = false;

                for (int iInternalRail = 0; iInternalRail < internalRails.Count; iInternalRail++)
                {
                    Array.Reverse(subtractPolyPoints2D[iInternalRail]);

                    windingNum = MathUtil.PolygonWindingNumber(
                    origin2D + new Vector2(x, y) + new Vector2(0.5f), subtractPolyPoints2D[iInternalRail]);

                    Array.Reverse(subtractPolyPoints2D[iInternalRail]);

                    if (windingNum != 0)
                    {
                        isHole = true;
                        break;
                    }
                }

                if (isHole)
                    return null;


                windingNum = MathUtil.PolygonWindingNumber(
                    origin2D + new Vector2(x, y) + new Vector2(0.5f), mainPolyPoints2D);
                bool isInside = windingNum != 0;

                return isInside ? 0 : null;
            }

            if (!isBridgeModel)
            {
                component.mTileMap.FillTiles(IsInside, Vector2.Zero, size2D);

                AutoTilingAlgorithm.Execute(component);

                return component;
            }

            foreach (var (x, y) in bridgeTiles)
            {
                component.mTileMap.AddTile(x, y);
            }

            return component;
        }

        private CourseUnit mCourseUnit;
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
