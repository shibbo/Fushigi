using Fushigi.util;
using Silk.NET.Input;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Fushigi.course.terrain_processing.TileInfo;
using static Fushigi.course.terrain_processing.TileNeighborPatternHelper;

namespace Fushigi.course.terrain_processing
{
    internal class TileIDLookup
    {
        public const int SEMISOLID_EMPTY = 0xFF;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int tileIdDefault, int tileIdSemiSolidGround) SplitCombinedTileId(int tileID) 
            => (tileID & 0xFF, (tileID >> 8) & 0xFF);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CombinedTileID(int tileIdDefault, int tileIdSemiSolidGround = SEMISOLID_EMPTY) 
            => tileIdDefault | (tileIdSemiSolidGround << 8);

        private static readonly Dictionary<TileInfo, int> mTileLookup = [];
        public static int GetCombinedTileIDFor(TileInfo tileInfo) => mTileLookup.GetValueOrDefault(tileInfo, CombinedTileID(0));

        static TileIDLookup()
        {
            static void AddTile(int tileID, TileNeighborPattern connectors, int corners = default, bool isForSemiSolidGround = false)
            {
                int _tileID = isForSemiSolidGround ? tileID << 8 : tileID;
                var key = new TileInfo
                {
                    Neighbors = connectors,
                    SlopeCornersRaw = (byte)corners
                };

                CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, key, out _) |= _tileID;

                for (int i = 0; i < 4; i++)
                {
                    TileNeighborPattern wall = RotateRight(Floor, i);
                    if (connectors == wall)
                    {
                        var _key = key;
                        //allow diagonal neighbors by saving every possible combinations of TL, TR

                        //using the gray code pattern
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TL, i);
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TR, i);
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                        _key.Neighbors ^= RotateRight(TileNeighborPattern.TL, i);
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
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
                        TileNeighborPattern _001 = RotateRight(TileNeighborPattern.BL, i);
                        TileNeighborPattern _010 = RotateRight(TileNeighborPattern.TL, i);
                        TileNeighborPattern _100 = RotateRight(TileNeighborPattern.TR, i);

                        _key.Neighbors ^= _001;
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                        _key.Neighbors ^= _010;
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                        _key.Neighbors ^= _001;
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                        _key.Neighbors ^= _100;
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                        _key.Neighbors ^= _001;
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                        _key.Neighbors ^= _010;
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                        _key.Neighbors ^= _001;
                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
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

                    CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                }

                if (isForSemiSolidGround)
                {
                    //ignore everything on the bottom by saving every possible combination
                    for (int i = 0; i < (1 << 7); i++)
                    {
                        int neighbors = i >> 4;
                        int slopeCorners = (i & 0xF);
                        var _key = key;
                        if ((neighbors & 0b001) > 0)
                            _key.Neighbors ^= TileNeighborPattern.BL;
                        if ((neighbors & 0b010) > 0)
                            _key.Neighbors ^= TileNeighborPattern.B;
                        if ((neighbors & 0b100) > 0)
                            _key.Neighbors ^= TileNeighborPattern.BR;

                        _key.SlopeCornerBL = (SlopeCornerType)(slopeCorners&0b11);
                        _key.SlopeCornerBR = (SlopeCornerType)((slopeCorners>>2)&0b11);

                        CollectionsMarshal.GetValueRefOrAddDefault(mTileLookup, _key, out _) |= _tileID;
                    }
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
            //69-72 Slope Duplicates
            //73-104 Long parts

            void AddSemisolid(int tileID, TileNeighborPattern connectors, int corners = default) =>
                AddTile(tileID, connectors, corners, isForSemiSolidGround: true);

            const int _0 = 0xFE; //we have to escape 0 because 0 means not set

            AddSemisolid(_0, Floor);
            AddSemisolid(1, OuterCornerTL);
            AddSemisolid(2, OuterCornerTR);
            AddSemisolid(3, InnerCornerTL);
            AddSemisolid(4, InnerCornerTR);
            // 5,6 Slopes
            AddSemisolid(7, InnerFull, TL_Slope45);
            AddSemisolid(8, InnerFull, TR_Slope45);
            AddSemisolid(9, WallLeft, TL_Slope45);
            AddSemisolid(10, WallRight, TR_Slope45);
            // 11-14 Slopes
            AddSemisolid(15, InnerFull, TL_Slope30BigPiece);
            AddSemisolid(16, InnerFull, TL_Slope30SmallPiece);
            AddSemisolid(17, InnerFull, TR_Slope30BigPiece);
            AddSemisolid(18, InnerFull, TR_Slope30SmallPiece);
            AddSemisolid(19, WallLeft, TL_Slope30BigPiece);
            AddSemisolid(20, WallRight, TR_Slope30BigPiece);
            // 21-24 Slope Duplicates
            // 25-32 Long parts

            foreach (var key in mTileLookup.Keys)
            {
                ref int value = ref CollectionsMarshal.GetValueRefOrNullRef(mTileLookup, key);
                if ((value & 0xFF00) == 0)
                    value |= SEMISOLID_EMPTY << 8;
                else if ((value & 0xFF00) == (_0 << 8))
                    value &= 0x00FF; //make an escaped 0 into a true 0
            }
        }

        public static readonly int TILE_Slope45BR = CombinedTileID(13, 5);
        public static readonly int TILE_Slope45BL = CombinedTileID(14, 6);
        public static readonly int TILE_Slope45TR = CombinedTileID(15, SEMISOLID_EMPTY);
        public static readonly int TILE_Slope45TL = CombinedTileID(16, SEMISOLID_EMPTY);

        public static readonly int TILE_Slope30BR_1 = CombinedTileID(33, 11);
        public static readonly int TILE_Slope30BR_2 = CombinedTileID(34, 12);
        public static readonly int TILE_Slope30BL_1 = CombinedTileID(36, 14);
        public static readonly int TILE_Slope30BL_2 = CombinedTileID(35, 13);
        public static readonly int TILE_Slope30TR_1 = CombinedTileID(37, SEMISOLID_EMPTY);
        public static readonly int TILE_Slope30TR_2 = CombinedTileID(38, SEMISOLID_EMPTY);
        public static readonly int TILE_Slope30TL_1 = CombinedTileID(40, SEMISOLID_EMPTY);
        public static readonly int TILE_Slope30TL_2 = CombinedTileID(39, SEMISOLID_EMPTY);
    }
}
