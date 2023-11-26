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
            Init();
        }

        public void Init()
        {
            AmbientColor = new Vector4(0, 0, 0, 1);
            HemiSkyColor = new Vector4(0.75f, 0.5625f, 1.125f, 1.5f);
            HemiGroundColor = new Vector4(1.1895f, 0.951f, 0.519f, 1.5f);

            HemiDirection = new Vector4(1, 0, 0, 1);

            LightDirection0 = new Vector4(-0.5714403f, -0.3136818f, -0.7583269f, 3.8f);
            LightColor = new Vector4(3.8f, 3.3516f, 2.8842f, 3.8f);
            LightSpecColor = new Vector4(3.8f);

            LightDirection1 = new Vector4(0, 1, 0, 0);
            LightColor1 = new Vector4(0, 0, 0, 1);
            LightSpecColor1 = new Vector4(0, 0, 0, 1);

            LightDirection0World = new Vector4(0, 1, 0, 0);
            HemiDirectionWorld = new Vector4(-0.5714403f, -0.3136818f, -0.7583269f, 0);
            LightDirection1World = new Vector4(0, 1, 0, 0);

            for (int i = 0; i < FogList.Length; i++)
                FogList[i] = new Fog();
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
                    writer.Write(FogList[i].Damp);
                    writer.Write(0);
                    writer.Write(0);
                }

                //vec4[21]
                writer.Write(LightDirection0World);
                writer.Write(HemiDirectionWorld);
                writer.Write(LightDirection1World);

                writer.Seek(400, SeekOrigin.Begin);

                //vec4[24]
                WriteExtended(writer);
            }
            block.SetData(mem.ToArray());
        }

        public virtual void WriteExtended(BinaryWriter writer)
        {

        }

        public class Fog
        {
            public float End = 100000;
            public float Start = 1000;
            public float Damp = 1;

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
