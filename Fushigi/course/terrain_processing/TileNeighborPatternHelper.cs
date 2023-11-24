using Fushigi.util;

namespace Fushigi.course.terrain_processing
{
    public static class TileNeighborPatternHelper
    {
        public static TileNeighborPattern RotateRight(TileNeighborPattern connectors, int n) =>
            (TileNeighborPattern)byte.RotateRight((byte)connectors, 2 * n);

        public static readonly TileNeighborPattern OuterCornerTL =
            TileNeighborPattern.R | TileNeighborPattern.BR | TileNeighborPattern.B;

        public static readonly TileNeighborPattern OuterCornerTR = RotateRight(OuterCornerTL, 1);
        public static readonly TileNeighborPattern OuterCornerBR = RotateRight(OuterCornerTL, 2);
        public static readonly TileNeighborPattern OuterCornerBL = RotateRight(OuterCornerTL, 3);

        public static readonly TileNeighborPattern WallLeft =
            TileNeighborPattern.T | TileNeighborPattern.TR | TileNeighborPattern.R | TileNeighborPattern.BR | TileNeighborPattern.B;

        public static readonly TileNeighborPattern Floor = RotateRight(WallLeft, 1);
        public static readonly TileNeighborPattern WallRight = RotateRight(WallLeft, 2);
        public static readonly TileNeighborPattern Ceiling = RotateRight(WallLeft, 3);

        public static readonly TileNeighborPattern InnerCornerTL =
           ~TileNeighborPattern.TL;

        public static readonly TileNeighborPattern InnerCornerTR = RotateRight(InnerCornerTL, 1);
        public static readonly TileNeighborPattern InnerCornerBR = RotateRight(InnerCornerTL, 2);
        public static readonly TileNeighborPattern InnerCornerBL = RotateRight(InnerCornerTL, 3);

        public static readonly TileNeighborPattern InnerFull = TileNeighborPattern.All;

        public static TileNeighborPattern FlipX(TileNeighborPattern input)
        {
            TileNeighborPattern res = input & (TileNeighborPattern.T | TileNeighborPattern.B);

            if ((input & TileNeighborPattern.TL) > 0) res |= TileNeighborPattern.TR;
            if ((input & TileNeighborPattern.BL) > 0) res |= TileNeighborPattern.BR;
            if ((input & TileNeighborPattern.TR) > 0) res |= TileNeighborPattern.TL;
            if ((input & TileNeighborPattern.BR) > 0) res |= TileNeighborPattern.BL;
            if ((input & TileNeighborPattern.L) > 0) res |= TileNeighborPattern.R;
            if ((input & TileNeighborPattern.R) > 0) res |= TileNeighborPattern.L;

            return res;
        }

        public static TileNeighborPattern FlipY(TileNeighborPattern input)
        {
            TileNeighborPattern res = input & (TileNeighborPattern.L | TileNeighborPattern.R);

            if ((input & TileNeighborPattern.TL) > 0) res |= TileNeighborPattern.BL;
            if ((input & TileNeighborPattern.TR) > 0) res |= TileNeighborPattern.BR;
            if ((input & TileNeighborPattern.BL) > 0) res |= TileNeighborPattern.TL;
            if ((input & TileNeighborPattern.BR) > 0) res |= TileNeighborPattern.TR;
            if ((input & TileNeighborPattern.T) > 0) res |= TileNeighborPattern.B;
            if ((input & TileNeighborPattern.B) > 0) res |= TileNeighborPattern.T;

            return res;
        }
    }
}
