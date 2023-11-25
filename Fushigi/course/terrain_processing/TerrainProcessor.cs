using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Fushigi.course.TileSubUnit;

namespace Fushigi.course.terrain_processing
{
    internal class TerrainProcessor
    {
        private record struct Polygon2D(Vector2[] Points, bool IsClosed, Vector2 BoundingBoxMin, Vector2 BoundingBoxMax);
        private record struct Wall2D(Polygon2D ExternalRailPoly, IReadOnlyList<Polygon2D> InternalRailPolys);

        public static void ProcessAll(CourseUnit courseUnit, IList<TileSubUnit> output)
        {
            bool isBridgeModel = courseUnit.mModelType == CourseUnit.ModelType.Bridge;

            Dictionary<(Vector2 gridOffset, float depth),
                (BoundingBox3D bb, List<Wall2D> walls, List<Polygon2D> beltRails)>
                dict = [];

            foreach (var wall in courseUnit.Walls)
            {
                var wall2D = WallToWall2D(wall, out BoundingBox3D bb);

                var key = (new Vector2(MathF.IEEERemainder(bb.Min.X, 1),
                                       MathF.IEEERemainder(bb.Min.Y, 1)),
                                       bb.Min.Z);
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
                if (!exists)
                    entry = (BoundingBox3D.Empty, [], []);

                entry.bb.Include(bb);
                entry.walls.Add(wall2D);
            }

            foreach (var belt in courseUnit.mBeltRails)
            {
                var beltRail2D = RailToPolygon2D(belt, out BoundingBox3D bb);

                var key = (new Vector2(MathF.IEEERemainder(bb.Min.X, 1),
                                       MathF.IEEERemainder(bb.Min.Y, 1)),
                                       bb.Min.Z);
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
                if (!exists)
                    entry = (BoundingBox3D.Empty, [], []);

                entry.bb.Include(bb);
                entry.beltRails.Add(beltRail2D);
            }

            foreach (var (bb, walls, beltRails) in dict.Values)
            {
                var processer = new TerrainPartProcessor(courseUnit, origin: bb.Min);

                if (isBridgeModel)
                {
                    foreach (var beltRail in beltRails)
                        processer.ProcessBelt(beltRail, isBridgeModel: true);
                }
                else
                {
                    //since belts are unreliable in custom levels we'll ignore them
                    //for none bridges and treat the walls rails as belts instead

                    //belts need to be processed before walls
                    foreach (var wall in walls)
                    {
                        processer.ProcessBelt(wall.ExternalRailPoly);
                        foreach (var rail in wall.InternalRailPolys)
                            processer.ProcessBelt(rail);
                    }

                    foreach (var wall in walls)
                        processer.ProcessWall(wall.ExternalRailPoly, wall.InternalRailPolys);
                }

                output.Add(processer.GetTileUnit());
            }
        }

        private static Wall2D WallToWall2D(Wall wall, out BoundingBox3D bb)
        {
            Polygon2D externalRail = RailToPolygon2D(wall.ExternalRail, out bb);

            Polygon2D[] internalRails = new Polygon2D[wall.InternalRails.Count];

            for (int i = 0; i < internalRails.Length; i++)
                internalRails[i] = RailToPolygon2D(wall.InternalRails[i], out _);

            return new Wall2D(externalRail, internalRails);
        }
        private static Polygon2D RailToPolygon2D(BGUnitRail rail, out BoundingBox3D bb)
        {
            Vector2[] points = new Vector2[rail.Points.Count];

            bb = BoundingBox3D.Empty;

            for (int i = 0; i < rail.Points.Count; i++)
            {
                Vector3 point = rail.Points[i].Position;
                points[i] = new Vector2(point.X, point.Y);
                bb.Include(point);
            }

            return new Polygon2D(points, rail.IsClosed,
                new Vector2(bb.Min.X, bb.Min.Y), new Vector2(bb.Max.X, bb.Max.Y));
        }




        private class TerrainPartProcessor
        {
            public void ProcessBelt(Polygon2D beltRailPoly, bool isBridgeModel = false)
            {
                var origin2D = new Vector2(mTileUnit.mOrigin.X, mTileUnit.mOrigin.Y);

                int segmentCount = beltRailPoly.Points.Length;
                if (!beltRailPoly.IsClosed)
                    segmentCount--;

                for (int iSegment = 0; iSegment < segmentCount; iSegment++)
                {
                    var p0 = beltRailPoly.Points[iSegment];
                    var p1 = beltRailPoly.Points[(iSegment + 1) % beltRailPoly.Points.Length];

                    if (
                        MathF.IEEERemainder(p0.X - origin2D.X, 1) != 0 ||
                        MathF.IEEERemainder(p0.Y - origin2D.Y, 1) != 0 ||
                        MathF.IEEERemainder(p1.X - origin2D.X, 1) != 0 ||
                        MathF.IEEERemainder(p1.Y - origin2D.Y, 1) != 0
                        )
                    {
                        //slope/bridge cannot be placed off (tile)grid
                        continue;
                    }

                    var slope = Math.Abs((p1.Y - p0.Y) / (p1.X - p0.X));

                    if (slope is not (1 or 0.5f or 0))
                        // slope/bridge angle is not supported by the game
                        continue;

                    if (slope == 0 && !isBridgeModel)
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

                        if (isBridgeModel)
                            mTileUnit.mTileMap.AddTile(tileX, tileY - 1, 0);

                        mBlockedTiles.Add((tileX, tileY));

                        if (slopeWidth == 2)
                        {
                            mBlockedTiles.Add((tileX + 1, tileY));
                            if (isBridgeModel)
                                mTileUnit.mTileMap.AddTile(tileX, tileY - 1, 0);
                        }

                        mTileUnit.mSlopes.Add((tileX, tileY, slopeWidth, slopeHeight, slopeType));
                    }
                }
            }

            public void ProcessWall(Polygon2D externalRailPoly, IReadOnlyList<Polygon2D> internalRailPolys)
            {
                var origin2D = new Vector2(mTileUnit.mOrigin.X, mTileUnit.mOrigin.Y);

                int? IsInside((int x, int y) tilePos)
                {
                    var (x, y) = tilePos;

                    int windingNum;
                    if (mBlockedTiles.Contains((x, y)))
                        return null;

                    bool isHole = false;

                    for (int iInternalRail = 0; iInternalRail < internalRailPolys.Count; iInternalRail++)
                    {
                        Array.Reverse(internalRailPolys[iInternalRail].Points);

                        windingNum = MathUtil.PolygonWindingNumber(
                        origin2D + new Vector2(x, y) + new Vector2(0.5f),
                        internalRailPolys[iInternalRail].Points);

                        Array.Reverse(internalRailPolys[iInternalRail].Points);

                        if (windingNum != 0)
                        {
                            isHole = true;
                            break;
                        }
                    }

                    if (isHole)
                        return null;


                    windingNum = MathUtil.PolygonWindingNumber(
                        origin2D + new Vector2(x, y) + new Vector2(0.5f), externalRailPoly.Points);
                    bool isInside = windingNum != 0;

                    return isInside ? 0 : null;
                }

                mTileUnit.mTileMap.FillTiles(IsInside,
                    externalRailPoly.BoundingBoxMin - origin2D, externalRailPoly.BoundingBoxMax - origin2D);
            }

            public TileSubUnit GetTileUnit() => mTileUnit;

            private readonly HashSet<(int x, int y)> mBlockedTiles = [];
            private readonly Dictionary<(int x, int y), TileInfo> mSetTileInfos = [];

            private readonly TileSubUnit mTileUnit;

            public TerrainPartProcessor(CourseUnit unit, Vector3 origin)
            {
                mTileUnit = new TileSubUnit(unit)
                {
                    mOrigin = origin
                };
            }
        }
    }
}
