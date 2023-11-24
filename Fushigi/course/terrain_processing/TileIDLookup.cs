using Fushigi.util;
using static Fushigi.course.terrain_processing.TileInfo;
using static Fushigi.course.terrain_processing.TileNeighborPatternHelper;

namespace Fushigi.course.terrain_processing
{
    internal class TileIDLookup
    {
        private static readonly Dictionary<TileInfo, int> mTileLookup = [];
        public static int GetTileFor(TileInfo tileInfo) => mTileLookup.GetValueOrDefault(tileInfo);

        static TileIDLookup()
        {
            static void AddTile(int tileID, TileNeighborPattern connectors, int corners = default)
            {
                var key = new TileInfo
                {
                    Neighbors = connectors,
                    SlopeCornersRaw = (byte)corners
                };

                mTileLookup[key] = tileID;

                for (int i = 0; i < 4; i++)
                {
                    TileNeighborPattern wall = RotateRight(Floor, i);
                    if (connectors == wall)
                    {
                        var _key = key;
                        //allow diagonal neighbors by saving every possible combinations of TL, TR

                        //using the gray code pattern
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TL, i);
                        mTileLookup[_key] = tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TR, i);
                        mTileLookup[_key] = tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TL, i);
                        mTileLookup[_key] = tileID;
                        break;
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    TileNeighborPattern corner = RotateRight(OuterCornerTL, i);
                    if (connectors == corner)
                    {
                        var _key = key;
                        //allow diagonal neighbors by saving every possible combinations of BL, TL, TR

                        //using the gray code pattern
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.BL, i);
                        mTileLookup[_key] = tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TL, i);
                        mTileLookup[_key] = tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.BL, i);
                        mTileLookup[_key] = tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TR, i);
                        mTileLookup[_key] = tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.BL, i);
                        mTileLookup[_key] = tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TL, i);
                        mTileLookup[_key] = tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.BL, i);
                        mTileLookup[_key] = tileID;
                        break;
                    }
                }


                for (int i = 0; i < 16; i++)
                {
                    var _key = key;
                    if ((i & 0b0001) * (corners & TL_SlopeMask) > 0)
                        _key.Neighbors ^= TileNeighborPattern.TL;
                    if ((i & 0b0010) * (corners & TR_SlopeMask) > 0)
                        _key.Neighbors ^= TileNeighborPattern.TR;
                    if ((i & 0b0100) * (corners & BL_SlopeMask) > 0)
                        _key.Neighbors ^= TileNeighborPattern.BL;
                    if ((i & 0b1000) * (corners & BR_SlopeMask) > 0)
                        _key.Neighbors ^= TileNeighborPattern.BR;

                    mTileLookup[_key] = tileID;
                }
            }

            AddTile(0, InnerFull);

            AddTile(1, OuterCornerTL);
            AddTile(2, OuterCornerTR);
            AddTile(3, OuterCornerBL);
            AddTile(4, OuterCornerBR);

            AddTile(5, WallLeft);
            AddTile(6, WallRight);
            AddTile(7, Floor);
            AddTile(8, Ceiling);

            AddTile(9, InnerCornerTL);
            AddTile(10, InnerCornerTR);
            AddTile(11, InnerCornerBL);
            AddTile(12, InnerCornerBR);
            //13-16 Slopes
            AddTile(17, InnerFull, TL_Slope45);
            AddTile(18, InnerFull, TR_Slope45);
            AddTile(19, InnerFull, BL_Slope45);
            AddTile(20, InnerFull, BR_Slope45);

            AddTile(21, WallLeft, TL_Slope45);
            AddTile(22, WallRight, TR_Slope45);
            AddTile(23, WallLeft, BL_Slope45);
            AddTile(24, WallRight, BR_Slope45);

            AddTile(25, OuterCornerBL, TL_Slope45);
            AddTile(26, OuterCornerBR, TR_Slope45);
            AddTile(27, OuterCornerTL, BL_Slope45);
            AddTile(28, OuterCornerTR, BR_Slope45);

            AddTile(29, WallLeft, TL_Slope45 | BL_Slope45);
            AddTile(30, WallRight, TR_Slope45 | BR_Slope45);

            AddTile(31, InnerFull, TL_Slope45 | BR_Slope45);
            AddTile(32, InnerFull, TR_Slope45 | BL_Slope45);
            //33-40 Slopes
            AddTile(41, InnerFull, TL_Slope30BigPiece);
            AddTile(42, InnerFull, TL_Slope30SmallPiece);
            AddTile(43, InnerFull, TR_Slope30BigPiece);
            AddTile(44, InnerFull, TR_Slope30SmallPiece);
            AddTile(45, InnerFull, BL_Slope30BigPiece);
            AddTile(46, InnerFull, BL_Slope30SmallPiece);
            AddTile(47, InnerFull, BR_Slope30BigPiece);
            AddTile(48, InnerFull, BR_Slope30SmallPiece);

            AddTile(49, WallLeft, TL_Slope30BigPiece);
            AddTile(50, WallRight, TR_Slope30BigPiece);
            AddTile(51, WallLeft, BL_Slope30BigPiece);
            AddTile(52, WallRight, BR_Slope30BigPiece);

            AddTile(53, OuterCornerBL, TL_Slope30BigPiece);
            AddTile(54, Ceiling, TL_Slope30SmallPiece);
            AddTile(55, OuterCornerBR, TR_Slope30BigPiece);
            AddTile(56, Ceiling, TR_Slope30SmallPiece);
            AddTile(57, OuterCornerTL, BL_Slope30BigPiece);
            AddTile(58, Floor, BL_Slope30SmallPiece);
            AddTile(59, OuterCornerTR, BR_Slope30BigPiece);
            AddTile(60, Floor, BR_Slope30SmallPiece);

            AddTile(61, WallLeft, TL_Slope30BigPiece | BL_Slope30BigPiece);
            AddTile(62, InnerFull, TL_Slope30SmallPiece | BL_Slope30SmallPiece);
            AddTile(63, WallRight, TR_Slope30BigPiece | BR_Slope30BigPiece);
            AddTile(64, InnerFull, TR_Slope30SmallPiece | BR_Slope30SmallPiece);

            AddTile(65, InnerFull, TL_Slope30BigPiece | BR_Slope30SmallPiece);
            AddTile(66, InnerFull, TL_Slope30SmallPiece | BR_Slope30BigPiece);
            AddTile(67, InnerFull, TR_Slope30BigPiece | BL_Slope30SmallPiece);
            AddTile(68, InnerFull, TR_Slope30SmallPiece | BL_Slope30BigPiece);
            //69-72 Slopes
            //73-104 Long parts
        }

        public const int TILE_Slope45BR = 13;
        public const int TILE_Slope45BL = 14;
        public const int TILE_Slope45TR = 15;
        public const int TILE_Slope45TL = 16;

        public const int TILE_Slope30BR_1 = 33;
        public const int TILE_Slope30BR_2 = 34;
        public const int TILE_Slope30BL_1 = 36;
        public const int TILE_Slope30BL_2 = 35;
        public const int TILE_Slope30TR_1 = 37;
        public const int TILE_Slope30TR_2 = 38;
        public const int TILE_Slope30TL_1 = 40;
        public const int TILE_Slope30TL_2 = 39;
    }
}
