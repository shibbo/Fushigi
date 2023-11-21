using Fushigi.gl.Textures;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    /// <summary>
    /// A cache for storing all the gsys resources including uniform blocks and textures.
    /// </summary>
    public class GsysResources
    {
        public UniformBlock ContextBlock;
        public UniformBlock EnvironmentBlock;
        public UniformBlock SceneMaterialBlock;

        public UniformBlock UserBlock0;
        public UniformBlock UserBlock1;
        public UniformBlock UserBlock2;

        public GLTexture CubeMap;

        public GLTexture DiffuseLightmap;
        public GLTexture SpecularLightmap;

        public GLTexture ColorBuffer;
        public GLTexture DepthBuffer;

        public GLTexture UserTexture0;
        public GLTexture UserTexture1;
        public GLTexture UserTexture2;
        public GLTexture UserTexture3;
        public GLTexture UserTexture4;
        public GLTexture UserTexture5;

        public GLTexture Projection0;
        public GLTexture Projection1;

        public GLTexture LinearNormalizedDepth;
        public GLTexture HalfLinearNormalizedDepth;

        public GLTexture VolumeFog;

        public GLTexture SSAO;
        public GLTexture DepthShadow;

        private bool init = false;

        public void UpdateViewport(Camera camera)
        {
            if (ContextBlock == null)
                return;

            GSysContext context = new GSysContext();
            context.Update(camera);
            context.Set(ContextBlock);
        }

        public void UpdateEnvironment()
        {
            GsysEnvironment env = new GsysEnvironment();
            env.Set(EnvironmentBlock);
        }

        public void Init(GL gl)
        {
            if (init)
                return;

            init = true;

            if (ContextBlock == null)
                ContextBlock = new UniformBlock(gl);

            if (EnvironmentBlock == null)
                EnvironmentBlock = new UniformBlock(gl);

            if (SceneMaterialBlock == null)
                SceneMaterialBlock = new UniformBlock(gl);

            if (UserBlock0 ==  null)
                UserBlock0 = new UniformBlock(gl);
            if (UserBlock1 == null)
                UserBlock1 = new UniformBlock(gl);
            if (UserBlock2 == null)
                UserBlock2 = new UniformBlock(gl);

            EnvironmentBlock.SetData(File.ReadAllBytes("SMWEnv.bin"));

          //  CubeMap = GLTextureCubeArray.CreateEmpty(gl, 4, 4, 1);
           // DiffuseLightmap = GLTextureCube.CreateEmpty(gl, 4);
            SpecularLightmap = GLTextureCube.CreateEmpty(gl, 4);

            CubeMap = new DDSTextureRender(gl, Path.Combine("res", "bfres", "CubemapHDR.dds"), TextureTarget.TextureCubeMapArray);
            DiffuseLightmap = new DDSTextureRender(gl, Path.Combine("res", "bfres", "CubemapLightmapShadow.dds"), TextureTarget.TextureCubeMap);

            DiffuseLightmap.Bind();
            DiffuseLightmap.WrapS = TextureWrapMode.ClampToEdge;
            DiffuseLightmap.WrapT = TextureWrapMode.ClampToEdge;
            DiffuseLightmap.WrapR = TextureWrapMode.ClampToEdge;
            DiffuseLightmap.MinFilter = TextureMinFilter.LinearMipmapLinear;
            DiffuseLightmap.MagFilter = TextureMagFilter.Linear;
            DiffuseLightmap.UpdateParameters();
            DiffuseLightmap.Unbind();

            CubeMap.Bind();
            CubeMap.WrapS = TextureWrapMode.ClampToEdge;
            CubeMap.WrapT = TextureWrapMode.ClampToEdge;
            CubeMap.WrapR = TextureWrapMode.ClampToEdge;
            CubeMap.MinFilter = TextureMinFilter.LinearMipmapLinear;
            CubeMap.MagFilter = TextureMagFilter.Linear;
            CubeMap.UpdateParameters();
            CubeMap.Unbind();

            VolumeFog = GLTexture3D.CreateEmpty(gl, 4, 4, 1);

            UserTexture0 = GLTexture2D.CreateUncompressedTexture(gl, 4, 4);
            UserTexture1 = GLTexture2D.CreateWhiteTex(gl, 4, 4);
            UserTexture2 = GLTexture2D.CreateUncompressedTexture(gl, 4, 4);
            UserTexture3 = GLTexture2D.CreateWhiteTex(gl, 4, 4);
            UserTexture4 = GLTexture2D.CreateUncompressedTexture(gl, 4, 4);
            UserTexture5 = GLTexture2D.CreateUncompressedTexture(gl, 4, 4);

            SSAO = GLTexture2D.CreateWhiteTex(gl, 4, 4);
            DepthShadow = GLTexture2D.CreateWhiteTex(gl, 4, 4);

            Projection0 = GLTexture2D.CreateWhiteTex(gl, 4, 4);
            Projection1 = GLTexture2D.CreateWhiteTex(gl, 4, 4);

            ColorBuffer = GLTexture2D.CreateUncompressedTexture(gl, 4, 4);
            DepthBuffer = GLTexture2D.CreateWhiteTex(gl, 4, 4);

            LinearNormalizedDepth = GLTexture2D.CreateWhiteTex(gl, 4, 4);
            HalfLinearNormalizedDepth = GLTexture2D.CreateWhiteTex(gl, 4, 4);

            GLUtil.Label(gl, ObjectIdentifier.Buffer, SSAO.ID, "SSAO");
            GLUtil.Label(gl, ObjectIdentifier.Buffer, VolumeFog.ID, "VolumeFog");

            GLUtil.Label(gl, ObjectIdentifier.Buffer, ContextBlock.ID, "ContextBlock");
            GLUtil.Label(gl, ObjectIdentifier.Buffer, EnvironmentBlock.ID, "EnvironmentBlock");
            GLUtil.Label(gl, ObjectIdentifier.Buffer, SceneMaterialBlock.ID, "SceneMaterialBlock");
            GLUtil.Label(gl, ObjectIdentifier.Buffer, UserBlock0.ID, "UserBlock0");
            GLUtil.Label(gl, ObjectIdentifier.Buffer, UserBlock1.ID, "UserBlock1");
            GLUtil.Label(gl, ObjectIdentifier.Buffer, UserBlock2.ID, "UserBlock2");

            GLUtil.Label(gl, ObjectIdentifier.Texture, CubeMap.ID, "CubeMap Array");
            GLUtil.Label(gl, ObjectIdentifier.Texture, DiffuseLightmap.ID, "DiffuseLightmap");
            GLUtil.Label(gl, ObjectIdentifier.Texture, SpecularLightmap.ID, "SpecularLightmap");
            GLUtil.Label(gl, ObjectIdentifier.Texture, Projection0.ID, "Projection0");
            GLUtil.Label(gl, ObjectIdentifier.Texture, Projection1.ID, "Projection1");
            GLUtil.Label(gl, ObjectIdentifier.Texture, LinearNormalizedDepth.ID, "LinearNormalizedDepth");
            GLUtil.Label(gl, ObjectIdentifier.Texture, HalfLinearNormalizedDepth.ID, "HalfLinearNormalizedDepth");

            GLUtil.Label(gl, ObjectIdentifier.Texture, UserTexture0.ID, "UserTexture0");
            GLUtil.Label(gl, ObjectIdentifier.Texture, UserTexture1.ID, "UserTexture1");
            GLUtil.Label(gl, ObjectIdentifier.Texture, UserTexture2.ID, "UserTexture2");
            GLUtil.Label(gl, ObjectIdentifier.Texture, UserTexture3.ID, "UserTexture3");

            GLUtil.Label(gl, ObjectIdentifier.Texture, ColorBuffer.ID, "ColorBuffer");
            GLUtil.Label(gl, ObjectIdentifier.Texture, DepthBuffer.ID, "DepthBuffer");
        }
    }
}
