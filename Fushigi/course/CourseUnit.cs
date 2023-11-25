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

            TerrainProcessor.ProcessAll(this, mTileSubUnits);

            GenerateCorrectTiles();
        }

        public void GenerateCorrectTiles()
        {
            if(mModelType != ModelType.Bridge)
            {
                foreach (var tileUnit in mTileSubUnits)
                    AutoTilingAlgorithm.Execute(tileUnit);
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

    public class TileSubUnit(CourseUnit courseUnit)
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
                var (tileIdEdge, tileIdGround) = courseUnit.mModelType switch
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
