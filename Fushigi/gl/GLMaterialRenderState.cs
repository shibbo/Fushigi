using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using System.Numerics;
using Silk.NET.SDL;

namespace Fushigi.gl
{
    public class GLMaterialRenderState
    {
        public static readonly GLMaterialRenderState TranslucentAlphaOne = new GLMaterialRenderState()
        {
            AlphaSrc = BlendingFactor.One,
            AlphaDst = BlendingFactor.One,
            EnableBlending = true,
            DepthWrite = false,
        };

        public static readonly GLMaterialRenderState Translucent = new GLMaterialRenderState()
        {
            EnableBlending = true,
            AlphaTest = false,
            ColorSrc = BlendingFactor.SrcAlpha,
            ColorDst = BlendingFactor.OneMinusSrcAlpha,
            ColorOp = BlendEquationModeEXT.FuncAdd,
            AlphaSrc = BlendingFactor.One,
            AlphaDst = BlendingFactor.Zero,
            AlphaOp = BlendEquationModeEXT.FuncAdd,
            State = BlendState.Translucent,
            DepthWrite = false,
        };

        public static readonly GLMaterialRenderState Opaque = new GLMaterialRenderState()
        {
            EnableBlending = false,
            AlphaTest = false,
            State = BlendState.Opaque,
        };

        public bool CullFront = false;
        public bool CullBack = true;

        public bool DepthTest = true;
        public DepthFunction DepthFunction = DepthFunction.Lequal;
        public bool DepthWrite = true;

        public bool AlphaTest = true;
        public AlphaFunction AlphaFunction = AlphaFunction.Gequal;
        public float AlphaValue = 0.5f;

        public BlendingFactor ColorSrc = BlendingFactor.SrcAlpha;
        public BlendingFactor ColorDst = BlendingFactor.OneMinusSrcAlpha;
        public BlendEquationModeEXT ColorOp = BlendEquationModeEXT.FuncAdd;

        public BlendingFactor AlphaSrc = BlendingFactor.One;
        public BlendingFactor AlphaDst = BlendingFactor.Zero;
        public BlendEquationModeEXT AlphaOp = BlendEquationModeEXT.FuncAdd;

        public BlendState State = BlendState.Opaque;

        public Vector4 BlendColor = Vector4.Zero;

        public bool EnableBlending = false;

        public enum BlendState
        {
            Opaque,
            Mask,
            Translucent,
            Custom,
        }

        public void RenderDepthTest(GL gl)
        {
            if (DepthTest)
            {
                gl.Enable(EnableCap.DepthTest);
                gl.DepthFunc(DepthFunction);
                gl.DepthMask(DepthWrite);
            }
            else
                gl.Disable(EnableCap.DepthTest);
        }

        public void RenderAlphaTest(GL gl)
        {
            //This should be done in shaders as it is a legacy feature
        }

        public void RenderBlendState(GL gl)
        {
            if (EnableBlending)
            {
                gl.Enable(EnableCap.Blend);
                gl.BlendFuncSeparate(ColorSrc, ColorDst, AlphaSrc, AlphaDst);
                gl.BlendEquationSeparate(ColorOp, AlphaOp);
                gl.BlendColor(BlendColor.X, BlendColor.Y, BlendColor.Z, BlendColor.W);
            }
            else
                gl.Disable(EnableCap.Blend);
        }

        public void RenderPolygonState(GL gl)
        {
            if (this.CullBack && this.CullFront)
                gl.CullFace(TriangleFace.FrontAndBack);
            else if (this.CullBack)
                gl.CullFace(TriangleFace.Back);
            else if (this.CullFront)
                gl.CullFace(TriangleFace.Front);
            else
            {
                gl.Disable(EnableCap.CullFace);
                gl.CullFace(TriangleFace.Back);
            }
        }
    }
}
