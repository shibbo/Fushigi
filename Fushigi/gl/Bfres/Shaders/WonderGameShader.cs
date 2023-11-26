using Fushigi.Bfres;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Fushigi.util;
using Fushigi.env;
using System.Runtime.Intrinsics.Arm;
using static Fushigi.gl.Bfres.GsysEnvironment;

namespace Fushigi.gl.Bfres
{
    public class WonderGameShader : GsysShaderRender
    {
        ShapeBlock ShpBlock;

        public override void Init(GL gl, BfresRender.BfresModel modelRender, BfresRender.BfresMesh meshRender, Shape shape, Material material)
        {
            ShpBlock = new ShapeBlock();

            if (GsysResources.UserBlock0 == null)
            {
               var sysBlock = new SystemParamBlock(gl);
                sysBlock.Update();
                GsysResources.UserBlock0 = sysBlock;
            }
            if (GsysResources.EnvironmentParams == null)
                GsysResources.EnvironmentParams = new EnvironmentBlockExtended();

            //Some kind of blur screen effect
            if (GsysResources.UserTexture1 == null)
                GsysResources.UserTexture1 = GLTexture2D.CreateWhiteTex(gl, 4, 4);

            //Skybox
            if (GsysResources.UserTexture3 == null)
                GsysResources.UserTexture3 = GLTexture2D.CreateWhiteTex(gl, 4, 4);


            base.Init(gl, modelRender, meshRender, shape, material);

            SceneMaterialBlock scene = new SceneMaterialBlock();
            scene.Update(GsysResources.SceneMaterialBlock);
        }

        public static void UpdateSystem()
        {
            if (GsysResources.UserBlock0 != null)
                ((SystemParamBlock)GsysResources.UserBlock0).UpdateTime();
        }

        public override void SetShapeBlock(UniformBlock block, Matrix4x4 mat)
        {
            ShpBlock.Update(block, mat);
        }

        public override void BindBindlessTextures(GL gl, GLShader shader)
        {
            //ssao but not directly binded in bfsha at times
            gl.ActiveTexture(TextureUnit.Texture0 + 12);
            GsysResources.SSAO.Bind();
            shader.SetUniform("fp_t_tcb_1E", 12);

            //A 3d buffer texture, maybe fog related
            gl.ActiveTexture(TextureUnit.Texture0 + 40);
            GsysResources.VolumeFog.Bind();
            shader.SetUniform("fp_t_cb7_20", 40);
        }

        class SceneMaterialBlock 
        {
            public void Update(UniformBlock block)
            {
                var mem = new MemoryStream();
                using (var writer = new BinaryWriter(mem))
                {
                    writer.Write(Vector4.One); //global_color
                }
                block.SetData(mem.ToArray());
            }
        }

        class ShapeBlock 
        {
            public Matrix4x4 ShapeMatrix = Matrix4x4.Identity;

            public int WeightCount;
            public float InstanceValue;
            public int Index;
            public uint DbgColor = 0;

            public Vector4 DifLight = Vector4.Zero;
            public Vector4 SpecLight = Vector4.Zero;

            public float XLUAlpha = 1f;

            public Vector4 Reserved = Vector4.Zero;

            public Vector4[] UserArea = new Vector4[8];

            public void Update(UniformBlock block, Matrix4x4 matrix)
            {
                var mem = new MemoryStream();
                using (var writer = new BinaryWriter(mem))
                {
                    //64 bytes
                    writer.Write(matrix.Column0());
                    writer.Write(matrix.Column1());
                    writer.Write(matrix.Column2());

                    writer.Write((int)WeightCount);
                    writer.Write(InstanceValue);
                    writer.Write(Index);
                    writer.Write(DbgColor);

                    //64 bytes
                    writer.Write(DifLight);
                    writer.Write(SpecLight);
                    writer.Write(new Vector4(XLUAlpha, 0, 0, 0));
                    writer.Write(Reserved); //reserved

                    //128 bytes
                    for (int i = 0; i < 8; i++)
                        writer.Write(UserArea[i]);

                    if (writer.BaseStream.Length != 256)
                        throw new Exception();
                }
                block.SetData(mem.ToArray());
            }
        }

        class SystemParamBlock : UniformBlock
        {
            const int NUM_HANDLES = 128;
            const int NUM_GROUPS = 32;

            public Vector4[] Reserved = new Vector4[8];

            public ulong[] TextureHandles = new ulong[NUM_HANDLES];

            public Vector4[] GroupScale = new Vector4[NUM_GROUPS];
            public Vector4[] GroupTranslate = new Vector4[NUM_GROUPS];

            public Vector4[] LocalPlayerPos = new Vector4[4];
            public Vector4 TimeParam = new Vector4();
            public Vector4 ShadowFadeParam = new Vector4();

            public SystemParamBlock(GL gl) : base(gl) 
            {
                //These seem to be set as one
                Reserved[2] = Vector4.One;
                Reserved[3] = Vector4.One;
                Reserved[6] = Vector4.One;
                Reserved[7] = Vector4.One;
                //Texture handles for some kind of hardcoded textures
                TextureHandles[0] = 4653586905; //cTexFogNoise
                TextureHandles[1] = 4653586906; //cTexBubbleNoise
                TextureHandles[2] = 0; //cTexModelUserNoise
                TextureHandles[3] = 4653586907; //cTexFogNoise

                TextureHandles[5] = 4679801256; //cTexInteractionMap
                TextureHandles[6] = 4565506476; //cTexErosionMask
                TextureHandles[7] = 4565509760; //cTexWaterMap
                TextureHandles[8] = 4565500170; //cTexBlurWaterMap

                TextureHandles[10] = 4565501485; //cTexMaterialMRT
                TextureHandles[11] = 4565500162; //cTexErosionBG

                //Set default scale as 1.0
                for (int i = 0; i < GroupScale.Length; i++)
                    GroupScale[i] = Vector4.One;
                for (int i = 0; i < GroupTranslate.Length; i++)
                    GroupTranslate[i] = Vector4.Zero;

                TimeParam = new Vector4(3.116667f, 4.815866f, 0, 0);

                this.LocalPlayerPos[0] = new Vector4(-1.5f, 2f, 0, 1f);
                this.LocalPlayerPos[1] = new Vector4(-1.5f, 2f, 0, 1f);
                this.LocalPlayerPos[2] = new Vector4(-1.5f, 2f, 0, 1f);
                this.LocalPlayerPos[3] = new Vector4(-1.5f, 2f, 0, 1f);
            }

            static float Time;

            public void UpdateTime()
            {
                Time += 1f / 60f;
                if (Time >= 1000) Time = 0;

                TimeParam = new Vector4(Time, Time, 0, 0);

                this.SetSubData(TimeParam, 2240);
            }

            public void Update()
            {
                var mem = new MemoryStream();
                using (var writer = new BinaryWriter(mem))
                {
                    for (int i = 0; i < 8; i++)
                        writer.Write(Reserved[i]);

                    for (int i = 0; i < TextureHandles.Length; i++)
                        writer.Write(TextureHandles[i]);

                    for (int i = 0; i < NUM_GROUPS; i++)
                        writer.Write(GroupScale[i]);
                    for (int i = 0; i < NUM_GROUPS; i++)
                        writer.Write(GroupTranslate[i]);

                    for (int i = 0; i < 4; i++)
                        writer.Write(LocalPlayerPos[i]);

                    writer.Write(TimeParam);
                    writer.Write(ShadowFadeParam);

                    if (writer.BaseStream.Length != 2272)
                        throw new Exception();
                }
                this.SetData(mem.ToArray());
            }
        }
    }

    public class EnvironmentBlockExtended : GsysEnvironment
    {
        public Vector4[] EnvColor = new Vector4[8];

        public Vector4 Unknown1 = new Vector4();
        public Vector4 Unknown2 = new Vector4();

        public Vector4 RimColor = new Vector4();
        public Vector4 RimParams = new Vector4(1, 5, 0, 0); //width, intensity, padding

        public Vector4 RimIntensty1 = new Vector4(0.2f, 0.2f, 0.5f, 0.3f);
        public Vector4 RimIntensty2 = new Vector4(0.1f, 0.4f, 0.5f, 0.2f);

        public Vector4 AOColor = new Vector4(0, 0.048f, 0.133f, 1);

        public EnvironmentBlockExtended()
        {
            Reset();
        }

        public void Reset()
        {
            this.Init();

            EnvColor[0] = new Vector4(1, 1, 1, 1);
            EnvColor[1] = new Vector4(1.3f, 1, 1, 1);
            EnvColor[2] = new Vector4(0.34375f, 0.65625f, 1, 1);
            EnvColor[3] = new Vector4(0.12500f, 0.25000f, 0.50000f, 0.72000f);
            EnvColor[4] = new Vector4(0, 0, 0, 1);
            EnvColor[5] = new Vector4(0.81250f, 0.90625f, 1, 1);
            EnvColor[6] = new Vector4(0.40625f, 0.81250f, 0.93750f, 1);
            EnvColor[7] = new Vector4(0.10000f, 0.21875f, 0.75000f, 1);

            Unknown1 = new Vector4(0, 1, 0.4f, 0.5f);
            Unknown2 = new Vector4(10, 1.5f, 2f, 20f);
        }

        public void Setup(EnvPalette envPalette, Kind kind = Kind.Obj)
        {
            Reset();
            if (envPalette.IsApplyFog)
                SetFog(envPalette.Fog.Main, envPalette.Fog.MainWorld);

            if (envPalette.IsApplyEnvColor)
                SetEnvColor(envPalette);

            switch (kind)
            {
                case Kind.Obj:
                    SetLights(envPalette.ObjLight);
                    break;
                case Kind.Char:
                    SetLights(envPalette.CharLight);
                    break;
                case Kind.Cloud:
                    SetLights(envPalette.CloudLight);
                    break;
                case Kind.Dv:
                    SetLights(envPalette.DvLight);
                    break;
                case Kind.Field:
                    SetLights(envPalette.FieldLight);
                    break;
            }

            this.SetRim(envPalette.Rim);
            this.SetShadow(envPalette.Shadow);
        }

        public void SetShadow(EnvPalette.EnvShadow shadow)
        {
            if (shadow == null) return;

            this.AOColor = shadow.AOColor.ToVector4();
        }

        public void SetLights(EnvPalette.EnvLightList lightList)
        {
            if (lightList == null) return;

            this.HemiSkyColor = lightList.Hemi.Sky.ToVector4() * lightList.Hemi.Intensity;
            this.HemiGroundColor = lightList.Hemi.Ground.ToVector4() * lightList.Hemi.Intensity;
            this.HemiDirection = new Vector4(0, 1, 0, 0);

            this.LightColor = lightList.Main.Color.ToVector4() * lightList.Main.Intensity;
            LightSpecColor = new Vector4(lightList.Main.Intensity);
        }

        public void SetFog(EnvPalette.EnvFog fog, EnvPalette.EnvFog fogWorld)
        {
            if (fog == null) return;

            this.FogList[0].Start = fog.Start;
            this.FogList[0].End = fog.End;
            this.FogList[0].Damp = fog.Damp;
            this.FogList[0].Direciton = new Vector3(0, 0, -1f);

            this.FogList[1].Start = fogWorld.Start;
            this.FogList[1].End = fogWorld.End;
            this.FogList[1].Damp = fogWorld.Damp;
            this.FogList[1].Direciton = new Vector3(0, -1f, 0);
        }

        public void SetEnvColor(EnvPalette envPalette)
        {
            if (envPalette.EnvColor == null) return;

            EnvColor[0] = envPalette.EnvColor.Color0.ToVector4();
            EnvColor[1] = envPalette.EnvColor.Color1.ToVector4();
            EnvColor[2] = envPalette.EnvColor.Color2.ToVector4();
            EnvColor[3] = envPalette.EnvColor.Color3.ToVector4();
            EnvColor[4] = envPalette.EnvColor.Color4.ToVector4();
            EnvColor[5] = envPalette.EnvColor.Color5.ToVector4();
            EnvColor[6] = envPalette.EnvColor.Color6.ToVector4();
            EnvColor[7] = envPalette.EnvColor.Color7.ToVector4();
        }

        public void SetRim(EnvPalette.EnvRim rim)
        {
            if (rim == null) return;

            this.RimColor = rim.Color.ToVector4();
            this.RimParams = new Vector4(rim.Width, rim.Power, 0, 0);
            this.RimIntensty1 = new Vector4(
                rim.IntensityCloud,
                rim.IntensityEnemy,
                rim.IntensityDV,
                rim.IntensityFieldWall); //cloud, enemy, dv, wall

            this.RimIntensty2 = new Vector4(
                rim.IntensityFieldBand,
                rim.IntensityFieldDeco,
                rim.IntensityObject,
                rim.IntensityPlayer); //field band, deco, object, player
        }

        public override void WriteExtended(BinaryWriter writer)
        {
            for (int i = 0; i < EnvColor.Length; i++)
                writer.Write(EnvColor[i]);

            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(RimColor);
            writer.Write(RimParams);
            writer.Write(RimIntensty1); //cloud, enemy, dv, wall
            writer.Write(RimIntensty2); //field band, deco, object, player

            writer.Write(Vector4.One); //unk
            writer.Write(new Vector4(1, 1, 0, 0)); //unk
            writer.Write(Vector4.Zero); //unk
            writer.Write(Vector4.One); //unk
            writer.Write(AOColor);

            for (int i = 0; i < 22; i++)
                writer.Write(Vector4.One);
        }

        public enum Kind
        {
            Cloud,
            Char,
            Dv,
            Obj,
            Field,
        }
    }
}
