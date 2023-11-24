using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    [Flags]
    public enum TileNeighborPattern : byte
    {
        None = 0,
        TL = 0b10000000,
        T = 0b01000000,
        TR = 0b00100000,
        R = 0b00010000,
        BR = 0b00001000,
        B = 0b00000100,
        BL = 0b00000010,
        L = 0b00000001,
        All = 0xFF
    }

    public class InfiniteTileMap
    {
        const int ChunkSize = 8;
        unsafe struct Chunk8x8
        {
            fixed ushort mTiles[ChunkSize * ChunkSize];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetTileAt(Index x, Index y, out int tileID)
            {
                int _x = x.GetOffset(ChunkSize);
                int _y = y.GetOffset(ChunkSize);
                Debug.Assert(0 <= _x && _x < ChunkSize);
                Debug.Assert(0 <= _y && _y < ChunkSize);
                tileID = mTiles[_x + _y * ChunkSize] - 1;
                return tileID != -1;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetTileAt(int x, int y, int? tileID)
            {
                Debug.Assert(0 <= x && x < ChunkSize);
                Debug.Assert(0 <= y && y < ChunkSize);
                mTiles[x + y * ChunkSize] = (byte)(tileID.GetValueOrDefault() + 1);
                if (!tileID.HasValue)
                    mTiles[x + y * ChunkSize] = 0;
            }

            public bool IsEmpty()
            {
                for (int i = 0; i < ChunkSize * ChunkSize; i++)
                {
                    if(mTiles[i] != 0)
                        return false;
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyRowPattern(Span<bool> buffer, Index y)
            {
                int _y = y.GetOffset(ChunkSize);
                Debug.Assert(buffer.Length == ChunkSize);
                Debug.Assert(0 <= _y && _y < ChunkSize);
                for (int i = 0; i < ChunkSize; i++) 
                    buffer[i] = mTiles[i + _y * ChunkSize] > 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyColumnPattern(Span<bool> buffer, Index x)
            {
                int _x = x.GetOffset(ChunkSize);
                Debug.Assert(buffer.Length == ChunkSize);
                Debug.Assert(0 <= _x && _x < ChunkSize);
                for (int i = 0; i < ChunkSize; i++)
                    buffer[i] = mTiles[_x + i * ChunkSize] > 0;
            }
        }

        public static void CalcTileChunkPos(int x, int y, 
            out int chunkX, out int chunkY, out int inChunkX, out int inChunkY)
        {
            inChunkX = ((x % ChunkSize) + ChunkSize) % ChunkSize;
            chunkX = (x - inChunkX) / ChunkSize;
            inChunkY = ((y % ChunkSize) + ChunkSize) % ChunkSize;
            chunkY = (y - inChunkY) / ChunkSize;
        }

        static Chunk8x8 mTempChunk;
        public void AddTile(int x, int y, int tileID = 0)
        {
            CalcTileChunkPos(x, y, out int chunkX, out int chunkY, 
                out int inChunkX, out int inChunkY);

            if (!mChunks.TryGetValue((chunkX, chunkY), out mTempChunk))
                mTempChunk = default;

            mTempChunk.SetTileAt(inChunkX, inChunkY, tileID);

            mChunks[(chunkX, chunkY)] = mTempChunk;
        }

        public void RemoveTile(int x, int y)
        {
            CalcTileChunkPos(x, y, out int chunkX, out int chunkY,
                out int inChunkX, out int inChunkY);

            if (!mChunks.TryGetValue((chunkX, chunkY), out mTempChunk))
                return;

            mTempChunk.SetTileAt(inChunkX, inChunkY, null);


            if(mTempChunk.IsEmpty())
            {
                mChunks.Remove((chunkX, chunkY));
                return;
            }

            mChunks[(chunkX, chunkY)] = mTempChunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChunkInClipRect(int chunkX, int chunkY, Vector2 clipRectMin, Vector2 clipRectMax)
            => (chunkX + 1) * ChunkSize >= clipRectMin.X && chunkX * ChunkSize <= clipRectMax.X &&
               (chunkY + 1) * ChunkSize >= clipRectMin.Y && chunkY * ChunkSize <= clipRectMax.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTileInClipRect(Vector2 pos, Vector2 clipRectMin, Vector2 clipRectMax)
            => pos.X + 1 >= clipRectMin.X && pos.X <= clipRectMax.X &&
               pos.Y + 1 >= clipRectMin.Y && pos.Y <= clipRectMax.Y;



        public IEnumerable<(int tileID, Vector2 position)> GetTiles(Vector2 clipRectMin, Vector2 clipRectMax)
        {
            foreach (var (chunkX, chunkY) in mChunks.Keys)
            {
                _ = mChunks.TryGetValue((chunkX, chunkY), out mTempChunk);

                if (!IsChunkInClipRect(chunkX, chunkY, clipRectMin, clipRectMax))
                    continue;

                for (int i = 0; i < ChunkSize * ChunkSize; i++)
                {
                    int inChunkX = i % ChunkSize; int inChunkY = i / ChunkSize;
                    if (mTempChunk.TryGetTileAt(inChunkX, inChunkY, out int tileID))
                    {
                        var pos = new Vector2(
                            chunkX * ChunkSize + inChunkX,
                            chunkY * ChunkSize + inChunkY);

                        if (IsTileInClipRect(pos, clipRectMin, clipRectMax))
                            yield return (tileID, pos);
                    }
                }
            }
        }

        public void FillTiles(Func<(int x, int y), int?> fillFunction, 
            Vector2 evaluateRectMin, Vector2 evaluateRectMax, bool isSubtract = false)
        {
            int minClippedChunkX = (int)MathF.Floor(evaluateRectMin.X / ChunkSize);
            int minClippedChunkY = (int)MathF.Floor(evaluateRectMin.Y / ChunkSize);
            int maxClippedChunkX = (int)MathF.Ceiling(evaluateRectMax.X / ChunkSize);
            int maxClippedChunkY = (int)MathF.Ceiling(evaluateRectMax.Y / ChunkSize);

            for (int chunkX = minClippedChunkX; chunkX <= maxClippedChunkX; chunkX++)
            {
                for (int chunkY = minClippedChunkY; chunkY <= maxClippedChunkY; chunkY++)
                {
                    mTempChunk = mChunks.GetValueOrDefault((chunkX, chunkY));

                    bool anyTileRemoved = false;
                    bool anyTileAdded = false;

                    for (int i = 0; i < ChunkSize * ChunkSize; i++)
                    {
                        int inChunkX = i % ChunkSize; int inChunkY = i / ChunkSize;

                        var tileX = chunkX * ChunkSize + inChunkX;
                        var tileY = chunkY * ChunkSize + inChunkY;

                        if (IsTileInClipRect(new Vector2(tileX, tileY), evaluateRectMin, evaluateRectMax))
                        {
                            int? tileID = fillFunction.Invoke((tileX, tileY));

                            if(tileID.HasValue && isSubtract)
                            {
                                mTempChunk.SetTileAt(inChunkX, inChunkY, null);
                                anyTileRemoved = true;
                            }
                            else if (tileID.HasValue && !isSubtract)
                            {
                                mTempChunk.SetTileAt(inChunkX, inChunkY, tileID.GetValueOrDefault());
                                anyTileAdded = true;
                            }

                        }
                    }

                    if (anyTileRemoved)
                        mChunks.Remove((chunkX, chunkY));
                    else if (anyTileAdded)
                        mChunks[(chunkX, chunkY)] = mTempChunk;
                }
            }
        }

        public void ConnectTiles(Func<(int x, int y), TileNeighborPattern, int> tileEvaluationFunction,
            (Vector2 min, Vector2 max)? clipRect = null)
        {
            Vector2 clipRectMin = new Vector2(float.NegativeInfinity);
            Vector2 clipRectMax = new Vector2(float.PositiveInfinity);

            if(clipRect.TryGetValue(out var clipRectVal))
            {
                clipRectMin = clipRectVal.min;
                clipRectMax = clipRectVal.max;
            }



            //the algorithm processes each chunk row by row, with a margin of 1
            //to allow easy sampling across chunk borders

            bool[] rowBelow = new bool[ChunkSize + 2];
            bool[] rowCurrent = new bool[ChunkSize + 2];
            bool[] rowAbove = new bool[ChunkSize + 2];

            Span<bool> marginPatternTop = stackalloc bool[ChunkSize + 2];
            Span<bool> marginPatternBottom = stackalloc bool[ChunkSize + 2];
            Span<bool> marginPatternLeft = stackalloc bool[ChunkSize];
            Span<bool> marginPatternRight = stackalloc bool[ChunkSize];

            foreach (var (chunkX, chunkY) in mChunks.Keys)
            {
                if (!IsChunkInClipRect(chunkX, chunkY, clipRectMin, clipRectMax))
                    continue;

                ref Chunk8x8 chunkRef = ref CollectionsMarshal.GetValueRefOrNullRef(mChunks, (chunkX, chunkY));

                bool tl, tr, bl, br;

                //sample all 8 surrounding chunks
                {
                    //horizontal walls
                    mChunks.GetValueOrDefault((chunkX, chunkY + 1))
                        .CopyRowPattern(marginPatternTop[1..^1], 0);
                    mChunks.GetValueOrDefault((chunkX, chunkY - 1))
                        .CopyRowPattern(marginPatternBottom[1..^1], ^1);

                    //vertical walls
                    mChunks.GetValueOrDefault((chunkX + 1, chunkY))
                        .CopyColumnPattern(marginPatternRight, 0);
                    mChunks.GetValueOrDefault((chunkX - 1, chunkY))
                        .CopyColumnPattern(marginPatternLeft, ^1);

                    //corners
                    tr = mChunks.GetValueOrDefault((chunkX + 1, chunkY + 1)).TryGetTileAt(0, 0, out _);
                    tl = mChunks.GetValueOrDefault((chunkX - 1, chunkY + 1)).TryGetTileAt(^1, 0, out _);
                    br = mChunks.GetValueOrDefault((chunkX + 1, chunkY - 1)).TryGetTileAt(0, ^1, out _);
                    bl = mChunks.GetValueOrDefault((chunkX - 1, chunkY - 1)).TryGetTileAt(^1, ^1, out _);

                    marginPatternTop[0] = tl;
                    marginPatternTop[^1] = tr;
                    marginPatternBottom[0] = bl;
                    marginPatternBottom[^1] = br;
                }

                //begin algorithm
                marginPatternBottom.CopyTo(rowBelow);
                chunkRef.CopyRowPattern(rowCurrent.AsSpan()[1..^1], 0);
                rowCurrent[0] = marginPatternLeft[0];
                rowCurrent[^1] = marginPatternRight[0];

                for (int inChunkY = 0; inChunkY < ChunkSize; inChunkY++)
                {
                    if (inChunkY == ChunkSize - 1)
                        marginPatternTop.CopyTo(rowAbove);
                    else
                    {
                        chunkRef.CopyRowPattern(rowAbove.AsSpan()[1..^1], inChunkY + 1);
                        rowAbove[0] = marginPatternLeft[inChunkY + 1];
                        rowAbove[^1] = marginPatternRight[inChunkY + 1];
                    }

                    for (int inChunkX = 0; inChunkX < ChunkSize; inChunkX++)
                    {
                        if (!rowCurrent[1+inChunkX]) //no tile here to evaluate
                            continue;

                        int x = inChunkX + chunkX * ChunkSize;
                        int y = inChunkY + chunkY * ChunkSize;

                        if (!IsTileInClipRect(new Vector2(x, y), clipRectMin, clipRectMax))
                            continue;

                        var neighbors = TileNeighborPattern.None;

                        if (rowAbove[1+inChunkX - 1]) neighbors |= TileNeighborPattern.TL;
                        if (rowAbove[1+inChunkX]) neighbors |= TileNeighborPattern.T;
                        if (rowAbove[1+inChunkX + 1]) neighbors |= TileNeighborPattern.TR;

                        if (rowCurrent[1+inChunkX - 1]) neighbors |= TileNeighborPattern.L;
                        if (rowCurrent[1+inChunkX + 1]) neighbors |= TileNeighborPattern.R;

                        if (rowBelow[1+inChunkX - 1]) neighbors |= TileNeighborPattern.BL;
                        if (rowBelow[1+inChunkX]) neighbors |= TileNeighborPattern.B;
                        if (rowBelow[1+inChunkX + 1]) neighbors |= TileNeighborPattern.BR;

                        var evaluatedTileID = tileEvaluationFunction((x, y), neighbors);
                        chunkRef.SetTileAt(inChunkX, inChunkY, evaluatedTileID);
                    }

                    //cycle row buffers
                    var tmp = rowBelow;
                    rowBelow = rowCurrent;
                    rowCurrent = rowAbove;
                    rowAbove = tmp;
                }
            }
        }

        readonly Dictionary<(int chunkX, int chuckY), Chunk8x8> mChunks = [];
    }
}
