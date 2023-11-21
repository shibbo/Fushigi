using Fushigi.util;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    [StructLayout(LayoutKind.Explicit, Size = 2048)]
    public struct GSysContext
    {
        [FieldOffset(0)]
        public Matrix3x4Struct cView;

        [FieldOffset(48)]
        public Matrix4x4Struct cViewProj;

        [FieldOffset(112)]
        public Matrix4x4Struct cProj;

        [FieldOffset(176)]
        public Matrix3x4Struct cViewInv;

        [FieldOffset(224)]
        public Vector4 cNearFar;

        [FieldOffset(240)]
        public Vector4 cAspect;

        [FieldOffset(256)]
        public Vector4 cZDistance;

        [FieldOffset(272)]
        public Vector4 cFov;

        [FieldOffset(288)]
        public Vector4 cFrameBuffer;

        [FieldOffset(288)]
        public Vector4 cCanvas;

        [FieldOffset(304)]
        public Vector4 cFrame;

        [FieldOffset(320)]
        public Matrix3x4Struct cPrevView;

        [FieldOffset(368)]
        public Matrix4x4Struct cPrevViewProj;

        [FieldOffset(432)]
        public Matrix4x4Struct cPrevProj;

        [FieldOffset(496)]
        public Matrix3x4Struct cPrevViewInv;

        [FieldOffset(544)]
        public Matrix3x4Struct[] ProjTextureMtx = new Matrix3x4Struct[2];

        [FieldOffset(640)]
        public Vector4 ProjTextureDensity;

        [FieldOffset(656)]
        public Matrix4x4[] DepthShadowProjMtx = new Matrix4x4[4];

        [FieldOffset(1024)]
        public Vector4 CubemapParams;

        public GSysContext() {
        }

        public void Update(Camera camera)
        {
            float znear = -10000;
            float zfar = 10000;
            float zRange = zfar - znear;
            float cTan = camera.AspectRatio * MathF.Tan(camera.Fov / 2);
            float cTan2 = MathF.Tan(camera.Fov / 2);

            Matrix4x4.Invert(camera.ViewMatrix, out Matrix4x4 invView);

            cView = new Matrix3x4Struct(camera.ViewMatrix);
            cViewProj = new Matrix4x4Struct(camera.ViewMatrix * camera.ProjectionMatrix);
            cProj     = new Matrix4x4Struct(camera.ProjectionMatrix);
            cViewInv  = new Matrix3x4Struct(invView);

            cNearFar   = new Vector4(znear, zfar, zfar / znear, 1.0f - znear / zfar);
            cAspect    = new Vector4(0.00003f, znear / zRange, camera.AspectRatio, 1.0f / camera.AspectRatio);
            cZDistance = new Vector4(zRange, 0, 0, 0);
            cFov       = new Vector4(cTan, cTan2, camera.Fov, 0.00f);

            cFrameBuffer = new Vector4(camera.Width, camera.Height, 1.0f / camera.Width, 1.0f / camera.Height);
            cCanvas = new Vector4(camera.Width, camera.Height, 1.0f / camera.Width, 1.0f / camera.Height);

            cFrame = new Vector4(38);

            CubemapParams = new Vector4(1024.0f, 4.0f, 1.0f, 1.0f);
        }

        public void Set(UniformBlock block)
        {
            var mem = new MemoryStream();
            using (var writer = new BinaryWriter(mem))
            {
                writer.Write(cView.Columns[0]);
                writer.Write(cView.Columns[1]);
                writer.Write(cView.Columns[2]);

                writer.Write(cViewProj.Columns[0]);
                writer.Write(cViewProj.Columns[1]);
                writer.Write(cViewProj.Columns[2]);
                writer.Write(cViewProj.Columns[3]);

                writer.Write(cProj.Columns[0]);
                writer.Write(cProj.Columns[1]);
                writer.Write(cProj.Columns[2]);
                writer.Write(cProj.Columns[3]);

                writer.Write(cViewInv.Columns[0]);
                writer.Write(cViewInv.Columns[1]);
                writer.Write(cViewInv.Columns[2]);

                writer.Write(cNearFar);
                writer.Write(cAspect);
                writer.Write(cZDistance);
                writer.Write(cFov);
                writer.Write(cFrameBuffer);
                writer.Write(cCanvas);
                writer.Write(cFrame);

                writer.Seek(1024, SeekOrigin.Begin);
                writer.Write(CubemapParams);
            }
            block.SetData(mem.ToArray());
        }
    }

    public class GsysEnvironment
    {
        public Vector4 AmbientColor;
        public Vector4 HemiSkyColor;
        public Vector4 HemiGroundColor;
        public Vector4 HemiDirection;
        public Vector4 LightDirection0;
        public Vector4 LightColor;
        public Vector4 LightSpecColor;

        public Vector4 LightDirection1;
        public Vector4 LightColor1;
        public Vector4 LightSpecColor1;

        public Vector4 LightDirection0World;
        public Vector4 HemiDirectionWorld;
        public Vector4 LightDirection1World;

        //Normal, world space, mask, effect fog
        public Fog[] FogList = new Fog[4];

        public Vector4[] LightBuffer = new Vector4[7];

        public GsysEnvironment()
        {
            AmbientColor = new Vector4(0, 0, 0, 1);
            HemiSkyColor = new Vector4(0.5019608f, 0.5019608f, 0.5019608f, 1);
            HemiGroundColor = new Vector4(0.5019608f, 0.5019608f, 0.5019608f, 1);

            HemiGroundColor = new Vector4(1, 0, 0, 1);

            HemiDirection = new Vector4(-0.01f, 0.9999935f, -0.003626907f, 0);
            LightDirection0 = new Vector4(0.8175079f, -0.4740172f, -0.3270913f, 4);
            LightColor = new Vector4(4.4f, 3.96f, 3.74f, 4);
            LightSpecColor = new Vector4(4);

            LightDirection1 = new Vector4(0, 1, 0, 0);
            LightColor1 = new Vector4(0, 0, 0, 1);
            LightSpecColor1 = new Vector4(0, 0, 0, 1);

            LightDirection0World = new Vector4(0, 1, 0, 0);
            HemiDirectionWorld = new Vector4(0.292369f, -0.9563048f, -0.001275723f, 0);
            LightDirection1World = new Vector4(0, 1, 0, 0);

            for (int i = 0; i < FogList.Length; i++)
                FogList[i] = new Fog();

            LightBuffer[0] = new Vector4(-0.07066349f, 0.0104383f, 0.01484864f, 0.3762342f);
            LightBuffer[1] = new Vector4(-0.07834358f, 0.022879f, 0.00286472f, 0.3644175f);
            LightBuffer[2] = new Vector4(-0.08234646f, 0.07531384f, -0.001357785f, 0.3541074f);
            LightBuffer[3] = new Vector4(-0.03421519f, -0.01010032f, -0.07968342f, 0.001463851f);
            LightBuffer[4] = new Vector4(-0.03502026f, -0.004357998f, -0.08492773f, -0.003487148f);
            LightBuffer[5] = new Vector4(-0.04025555f, 0.002667315f, -0.0995281f, -0.01150249f);
            LightBuffer[6] = new Vector4(-0.02927722f, -0.03509886f, -0.04652298f, 1.0f);
        }

        public void Set(UniformBlock block)
        {
            var mem = new System.IO.MemoryStream();
            using (var writer = new BinaryWriter(mem))
            {
                writer.Write(AmbientColor);
                writer.Write(HemiSkyColor);
                writer.Write(HemiGroundColor);
                writer.Write(HemiDirection);
                writer.Write(LightDirection0);
                writer.Write(LightColor);
                writer.Write(LightSpecColor);
                writer.Write(LightDirection1);
                writer.Write(LightColor1);
                writer.Write(LightSpecColor1);

                //vec4[10]

                writer.Seek(10 * 16, SeekOrigin.Begin);
                for (int i = 0; i < FogList.Length; i++)
                {
                    writer.Write(FogList[i].Color);
                    writer.Write(FogList[i].Direciton.X);
                    writer.Write(FogList[i].Direciton.Y);
                    writer.Write(FogList[i].Direciton.Z);
                    writer.Write(FogList[i].StartC);
                    writer.Write(FogList[i].EndC);
                    writer.Write(1f);
                    writer.Write(0);
                    writer.Write(0);
                }

                //vec4[21]

                writer.Write(LightDirection0World);
                writer.Write(HemiDirectionWorld);
                writer.Write(LightDirection1World);

                //vec4[24]

                writer.Write(LightBuffer[0]);
                writer.Write(LightBuffer[1]);
                writer.Write(LightBuffer[2]);
                writer.Write(LightBuffer[3]);

                writer.Write(LightBuffer[4]);
                writer.Write(LightBuffer[5]);
                writer.Write(LightBuffer[6]);

                //Blank out the rest below. Prevents black output in TOTK
                //Todo figure out why this is needed
                writer.Seek(46 * 16, SeekOrigin.Begin);
                for (int i = 0; i < 100; i++)
                {
                    writer.Write(new Vector4(0));
                }
            }
            block.SetData(mem.ToArray());
        }

        public class Fog
        {
            public float End = 100000;
            public float Start = 1000;

            public float StartC => -Start / (End - Start);
            public float EndC => 1.0f / (End - Start);

            public Vector3 Direciton = new Vector3(0, 0, 0);
            public Vector4 Color = new Vector4(0);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Matrix3x4Struct
    {
        public Vector4[] Columns = new Vector4[3];

        public Matrix3x4Struct(System.Numerics.Matrix4x4 matrix)
        {
            Columns[0] = matrix.Column0();
            Columns[1] = matrix.Column1();
            Columns[2] = matrix.Column2();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Matrix4x4Struct
    {
        public Vector4[] Columns = new Vector4[4];

        public Matrix4x4Struct(System.Numerics.Matrix4x4 matrix)
        {
            Columns[0] = matrix.Column0();
            Columns[1] = matrix.Column1();
            Columns[2] = matrix.Column2();
            Columns[3] = matrix.Column3();
        }
    }
}
