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
using Fushigi.agl;
using Fushigi.gl;
using ImGuiNET;
using static Fushigi.gl.Bfres.GsysEnvironment;
using System.Diagnostics;

namespace Fushigi.env
{
    public class EnvPalette : BymlObject
    {
        [BymlIgnore]
        public string Name { get; set; }

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

        public EnvPalette()
        {

        }

        public EnvPalette(string name)
        {
            Load(name);
        }

        public void Load(string name)
        {
            Name = name;

            string local_path = Path.Combine("Gyml", "Gfx", "EnvPaletteParam", $"{name}.game__gfx__EnvPaletteParam.bgyml");
            string file_path = FileUtil.FindContentPath(local_path);
            if (!File.Exists(file_path))
            {
                //Debug.Fail(null);
                return;
            }

            var byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(file_path)));
            this.Load((BymlHashTable)byml.Root);
            Console.WriteLine($"EnvPalette {name}");
        }

        public void Lerp(EnvPalette prevPalette, EnvPalette nextPalette, float ratio)
        {
            void LerpFog(EnvFog fogDst, EnvFog fogA, EnvFog fogB)
            {
                if (fogA == null || fogB == null)
                    return;

                fogDst.Start = MathUtil.Lerp(fogA.Start, fogB.Start, ratio);
                fogDst.End = MathUtil.Lerp(fogA.End, fogB.End, ratio);
                fogDst.Damp = MathUtil.Lerp(fogA.Damp, fogB.Damp, ratio);
                fogDst.Color = Color.Lerp(fogA.Color, fogB.Color, ratio);
            }

            void LerpRim(EnvRim a, EnvRim b)
            {
                if (a == null || b == null)
                    return;

                this.Rim.Color = Color.Lerp(a.Color, b.Color, ratio);
                this.Rim.Width = MathUtil.Lerp(a.Width, b.Width, ratio);
                this.Rim.Power = MathUtil.Lerp(a.Power, b.Power, ratio);
                this.Rim.IntensityFieldWall = MathUtil.Lerp(a.IntensityFieldWall, b.IntensityFieldWall, ratio);
                this.Rim.IntensityFieldBand = MathUtil.Lerp(a.IntensityFieldBand, b.IntensityFieldBand, ratio);
                this.Rim.IntensityFieldDeco = MathUtil.Lerp(a.IntensityFieldDeco, b.IntensityFieldDeco, ratio);
                this.Rim.IntensityObject = MathUtil.Lerp(a.IntensityObject, b.IntensityObject, ratio);
                this.Rim.IntensityPlayer = MathUtil.Lerp(a.IntensityPlayer, b.IntensityPlayer, ratio);
                this.Rim.IntensityEnemy = MathUtil.Lerp(a.IntensityEnemy, b.IntensityEnemy, ratio);
                this.Rim.IntensityDV = MathUtil.Lerp(a.IntensityDV, b.IntensityDV, ratio);
                this.Rim.IntensityCloud = MathUtil.Lerp(a.IntensityCloud, b.IntensityCloud, ratio);
            }

            void LerpHemiLight(EnvLightHemisphere dst, EnvLightHemisphere a, EnvLightHemisphere b)
            {
                if (a == null || b == null)
                    return;

                dst.Ground = Color.Lerp(a.Ground, b.Ground, ratio);
                dst.Sky = Color.Lerp(a.Sky, b.Sky, ratio);
                dst.Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, ratio);
            }

            void LerpLight(EnvLightDirectional dst, EnvLightDirectional a, EnvLightDirectional b)
            {
                if (a == null || b == null)
                    return;

                dst.Color = Color.Lerp(a.Color, b.Color, ratio);
                dst.Latitude = MathUtil.Lerp(a.Latitude, b.Latitude, ratio);
                dst.Longitude = MathUtil.Lerp(a.Longitude, b.Longitude, ratio);
                dst.Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, ratio);
            }


            void LerpSkyLut(EnvSkyLut dst, EnvSkyLut a, EnvSkyLut b)
            {
                if (a == null || b == null)
                    return;

                dst.ColorBegin = Color.Lerp(a.ColorBegin, b.ColorBegin, ratio);
                dst.ColorMiddle = Color.Lerp(a.ColorMiddle, b.ColorMiddle, ratio);
                dst.ColorEnd = Color.Lerp(a.ColorEnd, b.ColorEnd, ratio);
                dst.Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, ratio);
                if (ratio == 0.5f)
                {
                    dst.Curve = b.Curve;
                    dst.UseMiddleColor = b.UseMiddleColor;
                }
            }
            void LerpSky(EnvSky a, EnvSky b)
            {
                if (a == null || b == null)
                    return;

                LerpSkyLut(this.Sky.LutTexLeft, a.LutTexLeft, b.LutTexLeft);
                LerpSkyLut(this.Sky.LutTexLeftTop, a.LutTexLeftTop, b.LutTexLeftTop);
                LerpSkyLut(this.Sky.LutTexTop, a.LutTexTop, b.LutTexTop);
                LerpSkyLut(this.Sky.LutTexRightTop, a.LutTexRightTop, b.LutTexRightTop);
            }

            void LerpLightList(EnvLightList dst, EnvLightList a, EnvLightList b)
            {
                if (a == null || b == null)
                    return;

                LerpHemiLight(dst.Hemi, a.Hemi, b.Hemi);
                LerpLight(dst.Main, a.Main, b.Main);
                LerpLight(dst.SubDiff0, a.SubDiff0, b.SubDiff0);
                LerpLight(dst.SubDiff1, a.SubDiff1, b.SubDiff1);
                LerpLight(dst.SubDiff2, a.SubDiff2, b.SubDiff2);
                LerpLight(dst.SubSpec0, a.SubSpec0, b.SubSpec0);
                LerpLight(dst.SubSpec1, a.SubSpec1, b.SubSpec1);
            }

            LerpLightList(this.CharLight, prevPalette.CharLight, nextPalette.CharLight);
            LerpLightList(this.CloudLight, prevPalette.CloudLight, nextPalette.CloudLight);
            LerpLightList(this.FieldLight, prevPalette.FieldLight, nextPalette.FieldLight);
            LerpLightList(this.DvLight, prevPalette.DvLight, nextPalette.DvLight);
            LerpLightList(this.ObjLight, prevPalette.ObjLight, nextPalette.ObjLight);

            LerpFog(this.Fog.Main, prevPalette.Fog.Main, nextPalette.Fog.Main);
            LerpFog(this.Fog.MainWorld, prevPalette.Fog.MainWorld, nextPalette.Fog.MainWorld);
            LerpFog(this.Fog.Cloud, prevPalette.Fog.Cloud, nextPalette.Fog.Cloud);
            LerpFog(this.Fog.CloudWorld, prevPalette.Fog.CloudWorld, nextPalette.Fog.CloudWorld);

            LerpRim(prevPalette.Rim, nextPalette.Rim);

            this.Emission.Color = Color.Lerp(prevPalette.Emission.Color, nextPalette.Emission.Color, ratio);
            this.Shadow.AOColor = Color.Lerp(prevPalette.Shadow.AOColor, nextPalette.Shadow.AOColor, ratio);
            this.Shadow.Longitude = MathUtil.Lerp(prevPalette.Shadow.Longitude, nextPalette.Shadow.Longitude, ratio);
            this.Shadow.Latitude = MathUtil.Lerp(prevPalette.Shadow.Latitude, nextPalette.Shadow.Latitude, ratio);

            this.EnvColor.Color0 = Color.Lerp(prevPalette.EnvColor.Color0, nextPalette.EnvColor.Color0, ratio);
            this.EnvColor.Color1 = Color.Lerp(prevPalette.EnvColor.Color1, nextPalette.EnvColor.Color1, ratio);
            this.EnvColor.Color2 = Color.Lerp(prevPalette.EnvColor.Color2, nextPalette.EnvColor.Color2, ratio);
            this.EnvColor.Color3 = Color.Lerp(prevPalette.EnvColor.Color3, nextPalette.EnvColor.Color3, ratio);
            this.EnvColor.Color4 = Color.Lerp(prevPalette.EnvColor.Color4, nextPalette.EnvColor.Color4, ratio);
            this.EnvColor.Color5 = Color.Lerp(prevPalette.EnvColor.Color5, nextPalette.EnvColor.Color5, ratio);
            this.EnvColor.Color6 = Color.Lerp(prevPalette.EnvColor.Color6, nextPalette.EnvColor.Color6, ratio);
            this.EnvColor.Color7 = Color.Lerp(prevPalette.EnvColor.Color7, nextPalette.EnvColor.Color7, ratio);

            LerpSky(prevPalette.Sky, nextPalette.Sky);
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
            public bool EnableDynamicDepthShadow { get; set; }
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
            public EnvFog Cloud { get; set; } = new EnvFog();
            public EnvFog CloudWorld { get; set; } = new EnvFog();
            public EnvFog Main { get; set; } = new EnvFog();
            public EnvFog MainWorld { get; set; } = new EnvFog();
            public EnvFog Option { get; set; } = new EnvFog();
        }

        public class EnvFog
        {
            public Color Color { get; set; }
            public float Damp { get; set; }
            public float End { get; set; }
            public float Start { get; set; }
        }

        public class EnvLightList
        {
            public EnvLightHemisphere Hemi { get; set; }
            public EnvLightDirectional Main { get; set; }
            public EnvLightDirectional StageSubDiff0 { get; set; }
            public EnvLightDirectional StageSubDiff1 { get; set; }
            public EnvLightDirectional StageSubSpec0 { get; set; }
            public EnvLightDirectional StageSubSpec1 { get; set; }
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
            public EnvSkyLutCurve Curve { get; set; }

            public static byte[] Lerp(EnvSkyLut previous, EnvSkyLut next, float t)
            {
                byte[] data_bytes = new byte[64 * 4];

                float[] rgba32_1 = previous.ComputeRgba32(64);
                float[] rgba32_2 = next.ComputeRgba32(64);

                for (int i = 0; i < rgba32_1.Length; i++)
                {
                    float v = MathUtil.Lerp(rgba32_1[i], rgba32_2[i], t);
                    data_bytes[i] = (byte)(v * 255);
                }
                return data_bytes;
            }

            public byte[] ComputeRgba8(int width = 64)
            {
                byte[] data_bytes = new byte[width * 4];

                float[] rgba32 = ComputeRgba32(width);
                for (int i = 0; i < rgba32.Length; i++)
                    data_bytes[i] = (byte)(rgba32[i] * 255);

                return data_bytes;
            }

            public float[] ComputeRgba32(int width = 64)
            {
                if (Curve == null) // Always means constant?
                {
                    var color = ColorEnd.ToVector4();
                    var tmp = new float[width * 4];
                    for (int i = 0; i < width * 4; i += 4)
                    {
                        tmp[i + 0] = Math.Clamp(color.X, 0, 1f);
                        tmp[i + 1] = Math.Clamp(color.Y, 0, 1f);
                        tmp[i + 2] = Math.Clamp(color.Z, 0, 1f);
                        tmp[i + 3] = Math.Clamp(color.W, 0, 1f);
                    }

                    return tmp;
                }

                var type = Curve.GetCurveType();
                var data = Curve.Data.ToArray();

                Vector4[] colors = UseMiddleColor ?
                    new Vector4[] { ColorBegin.ToVector4(), ColorMiddle.ToVector4(), ColorEnd.ToVector4() } :
                    new Vector4[] { ColorBegin.ToVector4(), ColorEnd.ToVector4(), ColorEnd.ToVector4() };

                float[] buffer = new float[width * 4];

                Vector4 LerpBetweenColors(Vector4 a, Vector4 b, Vector4 c, float t)
                {
                    if (t < 0.5f) // Bottom to middle
                        return Vector4.Lerp(a, b, t / 0.5f);
                    else // Middle to top
                        return Vector4.Lerp(b, c, (t - 0.5f) / 0.5f);
                }

                for (int i = 0; i < width * 4; i += 4)
                {
                    int index = i / 4;
                    float time = index / (width - 1f);
                    float x = MathF.Min(AglCurve.Interpolate(data, type, time), Curve.MaxX);

                    //Use "X" to lerp between the colors to get
                    Vector4 color = LerpBetweenColors(colors[0], colors[1], colors[2], x);

                    buffer[i + 0] = Math.Clamp(color.X, 0, 1f);
                    buffer[i + 1] = Math.Clamp(color.Y, 0, 1f);
                    buffer[i + 2] = Math.Clamp(color.Z, 0, 1f);
                    buffer[i + 3] = Math.Clamp(color.W, 0, 1f);
                }
                return buffer;
            }
        }

        public class EnvSkyLutCurve
        {
            public List<float> Data { get; set; }
            public float MaxX { get; set; }
            public string Type { get; set; } //Matches agl curve type

            public AglCurve.CurveType GetCurveType()
            {
                if (Enum.TryParse(Type, out AglCurve.CurveType curveType))
                    return curveType;
                else
                    throw new ArgumentException("Invalid CurveType");
            }
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

            public Color() { }

            public Color(Vector4 v)
            {
                R = v.X;
                G = v.Y;
                B = v.Z;
                A = v.W;
            }

            public static Color Lerp(Color a, Color b, float t)
            {
                return new Color(Vector4.Lerp(a.ToVector4(), b.ToVector4(), t));
            }
        }
    }
}