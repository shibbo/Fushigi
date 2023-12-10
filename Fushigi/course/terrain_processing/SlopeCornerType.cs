namespace Fushigi.course.terrain_processing
{
    /// <summary>
    /// A corner placed below/above a slope
    /// </summary>
    public enum SlopeCornerType
    {
        None,

        Slope45,
        /*
          Slope30
                    *
                *···| <- Slope pieces
            *·······*
        *·······*   |
        |···*       | <- Corner pieces
        | big |small| 
                     */
        Slope30BigPiece,
        Slope30SmallPiece,
    }
}
