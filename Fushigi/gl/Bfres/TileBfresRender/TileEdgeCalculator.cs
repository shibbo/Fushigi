using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class TileEdgeCalculator
    {
        public enum TileEdgeType
        {
            CornerTL = 1,
            CornerTR = 2,
            CornerBL = 3,
            CornerBR = 4,
            WallL = 5,
            WallR = 6,
            Floor = 7,
            Ceiling = 8,

            //Unsure where this is used, a square corner tile
            EdgeCornerTL = 9,
            EdgeCornerTR = 10,
            EdgeCornerBL = 11,
            EdgeCornerBR = 12,

            //45 degree slopes use 1x2 tiles
            SlopeTL_45_1_1 = 13,
            SlopeTR_45_1_1 = 14,
            SlopeTL_45_1_2 = 17, //bottom of 13
            SlopeTR_45_1_2 = 18, //bottom of 14

            SlopeBL_45_1_1 = 15,
            SlopeBR_45_1_1 = 16,
            SlopeBL_45_1_2 = 19, //above 15
            SlopeBR_45_1_2 = 20, //above 16

            //Wall to top or bottom slope, maybe for 30 degrees?
            WallL_To_TL_Slope = 21,
            WallR_To_TR_Slope = 22,
            WallL_To_BL_Slope = 23,
            WallR_To_BR_Slope = 24,

            //Corner to top or bottom slope, maybe for 30 degrees?
            CornerTL_To_TL_Slope = 25,
            CornerTR_To_TR_Slope = 26,
            CornerBL_To_BL_Slope = 27,
            CornerBR_To_BR_Slope = 28,

            //Connects 2 slopes together as a wall
            WallL_To_TopBottom_Slope = 29,
            WallR_To_TopBottom_Slope = 30,

            Unknown1 = 31, //used for intersecting edges
            Unknown2 = 32, //used for intersecting edges

            //30 degree slopes use 2x2 tiles

            //33 - 48 tiles

            //Top left
            SlopeTL_30_1_2 = 33,
            SlopeTL_30_2_2 = 34,
            SlopeTL_30_1_1 = 41,
            SlopeTL_30_2_1 = 42,

            //Top right
            SlopeTR_30_1_2 = 35,
            SlopeTR_30_2_2 = 36,
            SlopeTR_30_1_1 = 43,
            SlopeTR_30_2_1 = 44,

            //Bottom left
            SlopeBL_30_1_2 = 37,
            SlopeBL_30_2_2 = 38,
            SlopeBL_30_1_1 = 45,
            SlopeBL_30_2_1 = 46,

            //Bottom right
            SlopeBR_30_1_2 = 39,
            SlopeBR_30_2_2 = 40,
            SlopeBR_30_1_1 = 47,
            SlopeBR_30_2_1 = 48,

            //Another to slope wall, maybe for 45 degree?
            WallL_To_TL_SlopeVar2 = 49,
            WallR_To_TR_SlopeVar2 = 50,
            WallL_To_BL_SlopeVar2 = 51,
            WallR_To_BR_SlopeVar2 = 52,

            //All these connected may be used to form a hole
            WallL_Varient3 = 51,
            WallR_Varient3 = 52,
            CornerBL_Varient3 = 53,
            CeilingBL_Varient3 = 54,
            CornerBR_Varient3 = 55,
            CeilingBR_Varient3 = 56,
            CeilingTL_Varient3 = 57,
            FloorTL_Varient3 = 59,
            CeilingTR_Varient3 = 59,
            FloorTR_Varient3 = 60,

            //More hole types, more advanced and complex
            WallL_Varient4_1_1 = 61, //Wall
            WallL_Varient4_2_1 = 62, //Small edge marks for 61

            WallR_Varient4_1_1 = 63, //Wall
            WallR_Varient4_2_1 = 64, //Small edge marks for 63
           

            Ground_2 = 81, //Connects to 82
            Ground_3 = 82, //Ground to slope right
            Ground_4 = 83, //Ground to slope left
            Ground_5 = 84,//Connects from 83

            Ground_Ceiling_2 = 85, //Connects to 86
            Ground_Ceiling_3 = 86, //Ground to slope right
            Ground_Ceiling_4 = 87, //Ground to slope left
            Ground_Ceiling_5 = 88,//Connects from 87

            Ground_6 = 97,  //Ground to slope right
            Ground_7 = 98,  //Ground to slope left
            Ground_8 = 99,  //Ground to slope right
            Ground_9 = 100,  //Ground to slope left

            Ground_Ceiling_6 = 101,  //Ground to slope right
            Ground_Ceiling_7 = 102,  //Ground to slope left
            Ground_Ceiling_8 = 103,  //Ground to slope right
            Ground_Ceiling_9 = 104,  //Ground to slope left
        }
    }
}
