using Fushigi.Byml.Serializer;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fushigi.Byml;
using System.Collections;
using System.Numerics;

namespace Fushigi.env
{
    public class EnvPalette : BymlObject
    {
        public EnvBloom Bloom { get; set; }
        public EnvDepthOfField DOF { get; set; }
        public EnvColorList EnvColor { get; set; }
        public EnvEmission Emission { get; set; }
        public EnvShadow Shadow { get; set; }
        public EnvRim Rim { get; set; }
        public EnvSky Sky { get; set; }
        public EnvFogList Fog { get; set; }

        public EnvLightList CharLight { get; set; }
        public EnvLightList CloudLight { get; set; }
        public EnvLightList DvLight { get; set; }
        public EnvLightList FieldLight { get; set; }
        public EnvLightList ObjLight { get; set; }

        public bool IsApplyFog { get; set; }
        public bool IsApplyEnvColor { get; set; }
        public bool IsApplyObjLight { get; set; }

        public EnvPalette(string name)
        {
            string local_path = Path.Combine("Gyml", "Gfx", "EnvPaletteParam", $"{name}.game__gfx__EnvPaletteParam.bgyml");
            string file_path = FileUtil.FindContentPath(local_path);
            if (!File.Exists(file_path))
                return;

            var byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(file_path)));
            this.Load((BymlHashTable)byml.Root);
            Console.WriteLine($"EnvPalette {name}");
        }

        public class EnvBloom
        {
            public float Intensity { get; set; }
            public float MaskEnd { get; set; }
            public float MaskRatio { get; set; }
            public float Threshold { get; set; }
        }

        public class EnvMapList
        {
            public EnvLightMap Ground0 { get; set; }
            public EnvLightMap Ground1 { get; set; }
            public EnvLightMap Horizon { get; set; }
            public EnvLightMap Sky0 { get; set; }
            public EnvLightMap Sky1 { get; set; }
        }

        public class EnvLightMap
        {
            public Color Color { get; set; }
            public float Intensity { get; set; }
        }

        public class EnvRim
        {
            public Color Color { get; set; }

            public float IntensityCloud { get; set; }
            public float IntensityDV { get; set; }
            public float IntensityEnemy { get; set; }
            public float IntensityFieldBand { get; set; }
            public float IntensityFieldDeco { get; set; }
            public float IntensityFieldWall { get; set; }
            public float IntensityObject { get; set; }
            public float IntensityPlayer { get; set; }

            public float Power { get; set; }
            public float Width { get; set; }
        }

        public class EnvShadow
        {
            public Color AOColor { get; set; }
            public bool EnableDynamicDepthShadow {  get; set; }
            public float Latitude { get; set; }
            public float Longitude { get; set; }
        }

        public class EnvDepthOfField
        {
            public bool Enable { get; set; }
            public float End { get; set; }
            public float MipLevelMax { get; set; }
            public float Start { get; set; }
        }

        public class EnvEmission
        {
            public Color Color { get; set; }
        }

        public class EnvColorList
        {
            public Color Color0 { get; set; }
            public Color Color1 { get; set; }
            public Color Color2 { get; set; }
            public Color Color3 { get; set; }
            public Color Color4 { get; set; }
            public Color Color5 { get; set; }
            public Color Color6 { get; set; }
            public Color Color7 { get; set; }
        }

        public class EnvFogList
        {
            public EnvFog Cloud { get; set; }
            public EnvFog CloudWorld { get; set; }
            public EnvFog Main { get; set; }
            public EnvFog MainWorld { get; set; }
            public EnvFog Option { get; set; }
        }

        public class EnvFog
        {
            public Color Color { get; set; }
            public float Damp { get; set; }
            public float End { get; set; }
            public float Start { get; set;}
        }

        public class EnvLightList
        {
            public EnvLightHemisphere Hemi { get; set; }
            public EnvLightDirectional Main { get; set; }
            public EnvLightDirectional SubDiff0 { get; set; }
            public EnvLightDirectional SubDiff1 { get; set; }
            public EnvLightDirectional SubDiff2 { get; set; }
            public EnvLightDirectional SubSpec0 { get; set; }
            public EnvLightDirectional SubSpec1 { get; set; }
            public EnvLightDirectional SubSpec2 { get; set; }
        }

        public class EnvLightDirectional
        {
            public Color Color { get; set; }
            public float Intensity { get; set; }
            public float Latitude { get; set; } //degrees
            public float Longitude { get; set; } //degrees
        }

        public class EnvLightHemisphere
        {
            public Color Ground { get; set; }
            public float Intensity { get; set; }
            public Color Sky { get; set; }
        }

        public class EnvSky
        {
            public float HorizontalOffset { get; set; } //Updates skybox mat params
            public float RotDegLeftTop { get; set; } //Updates skybox mat params
            public float RotDegRightTop { get; set; } //Updates skybox mat params

            public EnvSkyLut LutTexLeft { get; set; }
            public EnvSkyLut LutTexLeftTop { get; set; }
            public EnvSkyLut LutTexRightTop { get; set; }
            public EnvSkyLut LutTexTop { get; set; }
        }

        public class EnvSkyLut
        {
            public Color ColorBegin { get; set; }
            public Color ColorEnd { get; set; }
            public Color ColorMiddle { get; set; }
            public float Intensity { get; set; }
            public bool UseMiddleColor { get; set; }
        }

        public class EnvSkyLutCurve
        {
            public List<float> Data { get; set; }
            public float MaxX { get; set; }
            public string Type { get; set; } //Matches agl curve type
        }

        public struct Color
        {
            public float R { get; set; }
            public float G { get; set; }
            public float B { get; set; }
            public float A { get; set; }

            public Vector4 ToVector4() => new Vector4(R, G, B, A);

            public override string ToString()
            {
                return $"R {R} G {G} B {B} A {A}";
            }
        }
    }
}
