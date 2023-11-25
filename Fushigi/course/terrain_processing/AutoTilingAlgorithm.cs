using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Fushigi.course.TileSubUnit;
using static Fushigi.course.terrain_processing.TileIDLookup;

namespace Fushigi.course.terrain_processing
{
    internal class AutoTilingAlgorithm
    {
        public static void Execute(TileSubUnit unit)
        {
            Dictionary<(int x, int y), TileInfo> mSetTileInfos = [];

            //set tile infos for every slopes surrounding tiles
            foreach (var slope in unit.mSlopes)
            {
                var (x, y, width, height, slopePositioning) = slope;

                void SetTileInfo(int x, int y, TileInfo tileInfo) =>
                CollectionsMarshal.GetValueRefOrAddDefault(mSetTileInfos,
                        (x, y), out _).MergeWith(tileInfo);

                var (factX, factY) = slopePositioning switch
                {
                    SlopePositioning.CornerBR => (1, 1),
                    SlopePositioning.CornerBL => (-1, 1),
                    SlopePositioning.CornerTR => (1, -1),
                    SlopePositioning.CornerTL => (-1, -1),
                    _ => throw new Exception()
                };

                //for bottom right Slope45
                //[S]outh -> below  and [E]ast -> to the right

                TileInfo tileS1, tileS2 = default; //need to be assigned individually for each slope type
                var tileSE = new TileInfo
                {
                    Neighbors = TileNeighborPattern.TL,
                };
                var tileE = new TileInfo
                {
                    Neighbors = TileNeighborPattern.L,
                };

                void FlipTilesIfNeeded()
                {
                    if (factX == -1)
                    {
                        tileS1 = tileS1.FlippedX();
                        tileS2 = tileS2.FlippedX();
                        tileSE = tileSE.FlippedX();
                        tileE = tileE.FlippedX();
                    }
                    if (factY == -1)
                    {
                        tileS1 = tileS1.FlippedY();
                        tileS2 = tileS2.FlippedY();
                        tileSE = tileSE.FlippedY();
                        tileE = tileE.FlippedY();
                    }
                }

                if (width == 1 && height == 1)
                {
                    tileS1 = new TileInfo
                    {
                        Neighbors = TileNeighborPattern.T | TileNeighborPattern.TR,
                        SlopeCornerTL = SlopeCornerType.Slope45
                    };
                    FlipTilesIfNeeded();

                    SetTileInfo(x, y - factY * 1, tileS1);

                    SetTileInfo(x + factX * 1, y - factY * 1, tileSE);

                    SetTileInfo(x + factX * 1, y, tileE);
                }
                else if (width == 2 && height == 1)
                {
                    tileS1 = new TileInfo
                    {
                        Neighbors = TileNeighborPattern.T | TileNeighborPattern.TR,
                        SlopeCornerTL = SlopeCornerType.Slope30BigPiece
                    };
                    tileS2 = new TileInfo
                    {
                        Neighbors = TileNeighborPattern.TL | TileNeighborPattern.T,
                        SlopeCornerTL = SlopeCornerType.Slope30SmallPiece
                    };
                    FlipTilesIfNeeded();

                    if (factX == -1) x++; //move "origin" to the right

                    SetTileInfo(x, y - factY * 1, tileS1);

                    SetTileInfo(x + factX * 1, y - factY * 1, tileS2);

                    SetTileInfo(x + factX * 2, y - factY * 1, tileSE);

                    SetTileInfo(x + factX * 2, y, tileE);
                }
                else
                {
                    Debug.Fail("Unsupported Slope Type");
                    return;
                }
            }

            unit.mTileMap.ConnectTiles(
                ((int x, int y) tilePos, TileNeighborPattern neighbors) =>
                {
                    var tileInfo = mSetTileInfos.GetValueOrDefault(tilePos);
                    tileInfo.Neighbors |= neighbors;

                    return TileIDLookup.GetCombinedTileIDFor(tileInfo);
                }
            );

            foreach (var (x, y, width, height, slopePositioning) in unit.mSlopes)
            {
                var (slope30_1, slope30_2, slope45) = slopePositioning switch
                {
                    SlopePositioning.CornerTL => (TILE_Slope30TL_1, TILE_Slope30TL_2, TILE_Slope45TL),
                    SlopePositioning.CornerTR => (TILE_Slope30TR_1, TILE_Slope30TR_2, TILE_Slope45TR),
                    SlopePositioning.CornerBL => (TILE_Slope30BL_1, TILE_Slope30BL_2, TILE_Slope45BL),
                    SlopePositioning.CornerBR => (TILE_Slope30BR_1, TILE_Slope30BR_2, TILE_Slope45BR),
                    _ => throw new Exception()
                };

                if(width == 2 && height == 1)
                {
                    unit.mTileMap.AddTile(x, y, slope30_1);
                    unit.mTileMap.AddTile(x + 1, y, slope30_2);
                }
                else if (width == 1 && height == 1)
                {
                    unit.mTileMap.AddTile(x, y, slope45);
                }
            }
        }

        public static void ExecuteForBridges(TileSubUnit unit)
        {
            foreach (var (x, y, width, height, slopePositioning) in unit.mSlopes)
            {
                int slope30_T1, slope30_T2, slope30_B1, slope30_B2, slope45_T, slope45_B;

                if (slopePositioning == SlopePositioning.CornerBL)
                {
                    slope30_T1 = BRIDGE_TILE_Slope30BL_T1;
                    slope30_T2 = BRIDGE_TILE_Slope30BL_T2;
                    slope30_B1 = BRIDGE_TILE_Slope30BL_B1;
                    slope30_B2 = BRIDGE_TILE_Slope30BL_B2;
                    slope45_T  = BRIDGE_TILE_Slope45BL_T;
                    slope45_B  = BRIDGE_TILE_Slope45BL_B;
                }
                else if(slopePositioning == SlopePositioning.CornerBR)
                {
                    slope30_T1 = BRIDGE_TILE_Slope30BR_T1;
                    slope30_T2 = BRIDGE_TILE_Slope30BR_T2;
                    slope30_B1 = BRIDGE_TILE_Slope30BR_B1;
                    slope30_B2 = BRIDGE_TILE_Slope30BR_B2;
                    slope45_T  = BRIDGE_TILE_Slope45BR_T;
                    slope45_B  = BRIDGE_TILE_Slope45BR_B;
                }
                else
                {
                    continue;
                }

                if (width == 2 && height == 1)
                {
                    unit.mTileMap.AddTile(x, y, slope30_T1);
                    unit.mTileMap.AddTile(x + 1, y, slope30_T2);
                    unit.mTileMap.AddTile(x, y - 1, slope30_B1);
                    unit.mTileMap.AddTile(x + 1, y - 1, slope30_B2);
                }
                else if (width == 1 && height == 1)
                {
                    unit.mTileMap.AddTile(x, y, slope45_T);
                    unit.mTileMap.AddTile(x, y - 1, slope45_B);
                }
            }
        }
    }
}
