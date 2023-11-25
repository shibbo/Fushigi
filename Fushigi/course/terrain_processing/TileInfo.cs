using Fushigi.util;
using static Fushigi.course.terrain_processing.SlopeCornerType;
using static Fushigi.course.terrain_processing.TileNeighborPatternHelper;

namespace Fushigi.course.terrain_processing
{
    public struct TileInfo : IEquatable<TileInfo>
    {
        public TileNeighborPattern Neighbors;
        public byte SlopeCornersRaw;

        public const int BitPerSlopeCorner = 2;
        public static readonly byte TL_SlopeMask = 0b11;
        public static readonly byte TL_Slope45 = (byte)Slope45;
        public static readonly byte TL_Slope30BigPiece = (byte)Slope30BigPiece;
        public static readonly byte TL_Slope30SmallPiece = (byte)Slope30SmallPiece;

        public static readonly byte TR_SlopeMask = 0b11 << 2;
        public static readonly byte TR_Slope45 = (byte)Slope45 << 2;
        public static readonly byte TR_Slope30BigPiece = (byte)Slope30BigPiece << 2;
        public static readonly byte TR_Slope30SmallPiece = (byte)Slope30SmallPiece << 2;

        public static readonly byte BL_SlopeMask = 0b11 << 4;
        public static readonly byte BL_Slope45 = (byte)Slope45 << 4;
        public static readonly byte BL_Slope30BigPiece = (byte)Slope30BigPiece << 4;
        public static readonly byte BL_Slope30SmallPiece = (byte)Slope30SmallPiece << 4;

        public static readonly byte BR_SlopeMask = 0b11 << 6;
        public static readonly byte BR_Slope45 = (byte)Slope45 << 6;
        public static readonly byte BR_Slope30BigPiece = (byte)Slope30BigPiece << 6;
        public static readonly byte BR_Slope30SmallPiece = (byte)Slope30SmallPiece << 6;

        public SlopeCornerType SlopeCornerTL
        { readonly get => (SlopeCornerType)(SlopeCornersRaw >> 0 & 0b11); set => SlopeCornersRaw |= (byte)((byte)value << 0); }
        public SlopeCornerType SlopeCornerTR
        { readonly get => (SlopeCornerType)(SlopeCornersRaw >> 2 & 0b11); set => SlopeCornersRaw |= (byte)((byte)value << 2); }
        public SlopeCornerType SlopeCornerBL
        { readonly get => (SlopeCornerType)(SlopeCornersRaw >> 4 & 0b11); set => SlopeCornersRaw |= (byte)((byte)value << 4); }
        public SlopeCornerType SlopeCornerBR
        { readonly get => (SlopeCornerType)(SlopeCornersRaw >> 6 & 0b11); set => SlopeCornersRaw |= (byte)((byte)value << 6); }

        public readonly TileInfo FlippedX()
        {
            return new TileInfo
            {
                Neighbors = FlipX(Neighbors),
                SlopeCornerTL = SlopeCornerTR,
                SlopeCornerTR = SlopeCornerTL,
                SlopeCornerBL = SlopeCornerBR,
                SlopeCornerBR = SlopeCornerBL
            };
        }

        public readonly TileInfo FlippedY()
        {
            return new TileInfo
            {
                Neighbors = FlipY(Neighbors),
                SlopeCornerTL = SlopeCornerBL,
                SlopeCornerBL = SlopeCornerTL,
                SlopeCornerTR = SlopeCornerBR,
                SlopeCornerBR = SlopeCornerTR
            };
        }

        public void MergeWith(TileInfo other)
        {
            Neighbors |= other.Neighbors;
            SlopeCornersRaw |= other.SlopeCornersRaw; //this should be fine (unless a Slope45 and Slope30BigPiece overlap)
        }

        public override bool Equals(object? obj)
        {
            return obj is TileInfo info && Equals(info);
        }

        public bool Equals(TileInfo other)
        {
            return Neighbors == other.Neighbors &&
                   SlopeCornersRaw == other.SlopeCornersRaw;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Neighbors, SlopeCornersRaw);
        }

        public static bool operator ==(TileInfo left, TileInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TileInfo left, TileInfo right)
        {
            return !(left == right);
        }
    }
}
