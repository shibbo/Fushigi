using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public class InfiniteTileMap
    {
        const int ChunkSize = 8;
        unsafe struct Chunk8x8
        {
            fixed ushort mTiles[ChunkSize * ChunkSize];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetTileAt(int x, int y, out int tileID)
            {
                Debug.Assert(0 <= x && x < ChunkSize);
                Debug.Assert(0 <= y && y < ChunkSize);
                tileID = mTiles[x + y * ChunkSize] - 1;
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
                    if (!mChunks.TryGetValue((chunkX, chunkY), out mTempChunk))
                        mTempChunk = default;

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

        readonly Dictionary<(int chunkX, int chuckY), Chunk8x8> mChunks = [];
    }
}
