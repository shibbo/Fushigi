using Fushigi.Bfres;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Fushigi.gl.Bfres
{
    /// <summary>
    /// Calculates render state info using the bfres render info section. 
    /// </summary>
    public class GsysRenderState
    {
        public GLMaterialRenderState State = new GLMaterialRenderState();

        public void Render(GL gl)
        {
            State.RenderAlphaTest(gl);
            State.RenderDepthTest(gl);
            State.RenderBlendState(gl);
            State.RenderPolygonState(gl);
        }

        public void Init(Material material) 
        {
            SetAlphaState(material);
            SetDepthState(material);
            SetBlendState(material);
            SetPolygonState(material);
        }

        public void LoadRenderStateOptions(Dictionary<string, string> options)
        {
            string renderMode = "";
            string alphaFunc = "";

            switch (this.State.State)
            {
                case GLMaterialRenderState.BlendState.Opaque: renderMode = "0"; break;
                case GLMaterialRenderState.BlendState.Mask: renderMode = "1"; break;
                case GLMaterialRenderState.BlendState.Translucent: renderMode = "2"; break;
                case GLMaterialRenderState.BlendState.Custom: renderMode = "3"; break;
            }

            switch (this.State.AlphaFunction)
            {
                case AlphaFunction.Gequal: alphaFunc = "6"; break;
                case AlphaFunction.Lequal: alphaFunc = "3"; break;
                case AlphaFunction.Less: alphaFunc = "1"; break;
                case AlphaFunction.Never: alphaFunc = "0"; break;
                case AlphaFunction.Greater: alphaFunc = "4"; break;
            }

            if (!string.IsNullOrEmpty(renderMode))
                options["gsys_renderstate"] = renderMode;

            if (State.AlphaTest)
            {
                options["gsys_alpha_test_func"] = alphaFunc;
                options["gsys_alpha_test_enable"] = "1";
            }
        }

        private void SetAlphaState(Material material)
        {
            //Alpha test
            string alphaTest = material.GetRenderInfoString("gsys_alpha_test_enable");
            string alphaFunc = material.GetRenderInfoString("gsys_alpha_test_func");
            float alphaValue = material.GetRenderInfoFloat("gsys_alpha_test_value");

            this.State.AlphaFunction = ConvertAlphaFunction(alphaFunc);
            this.State.AlphaTest = alphaTest == "true";
            this.State.AlphaValue = alphaValue;
        }

        private void SetDepthState(Material material)
        {
            string depthTest = material.GetRenderInfoString("gsys_depth_test_enable");
            string depthTestFunc = material.GetRenderInfoString("gsys_depth_test_func");
            string depthWrite = material.GetRenderInfoString("gsys_depth_test_write");

            this.State.DepthFunction = ConvertDepthFunction(depthTestFunc);
            this.State.DepthTest = depthTest == "true";
            this.State.DepthWrite = depthWrite == "true";
        }

        private void SetBlendState(Material material)
        {
            string colorOp = material.GetRenderInfoString("gsys_color_blend_rgb_op");
            string colorDst = material.GetRenderInfoString("gsys_color_blend_rgb_dst_func");
            string colorSrc = material.GetRenderInfoString("gsys_color_blend_rgb_src_func");
            float[] blendColorF32 = material.RenderInfos["gsys_color_blend_const_color"].GetValueSingles();

            string alphaOp = material.GetRenderInfoString("gsys_color_blend_alpha_op");
            string alphaDst = material.GetRenderInfoString("gsys_color_blend_alpha_dst_func");
            string alphaSrc = material.GetRenderInfoString("gsys_color_blend_alpha_src_func");

            string blend = material.GetRenderInfoString("gsys_render_state_blend_mode");
            string state = material.GetRenderInfoString("gsys_render_state_mode");

            if (blendColorF32.Length == 4)
                this.State.BlendColor = new Vector4(blendColorF32[0], blendColorF32[1], blendColorF32[2], blendColorF32[3]);

            this.State.EnableBlending = blend == "color";
            this.State.ColorSrc = ConvertBlend(colorSrc);
            this.State.ColorDst = ConvertBlend(colorDst);
            this.State.AlphaSrc = ConvertBlend(alphaSrc);
            this.State.AlphaDst = ConvertBlend(alphaDst);
            this.State.ColorOp = ConvertBlendEquationMode(colorOp);
            this.State.AlphaOp = ConvertBlendEquationMode(alphaOp);

            switch (state)
            { 
                case "opaque":
                    this.State.State = GLMaterialRenderState.BlendState.Opaque;
                    break;
                case "mask":
                    this.State.State = GLMaterialRenderState.BlendState.Mask;
                    break;
                case "translucent":
                    this.State.State = GLMaterialRenderState.BlendState.Translucent;
                    this.State.EnableBlending = true;
                    break;
                case "custom":
                    this.State.State = GLMaterialRenderState.BlendState.Custom;
                    this.State.EnableBlending = true;
                    break;
            }
        }

        private void SetPolygonState(Material material)
        {
            string pass = material.GetRenderInfoString("gsys_pass"); //affects polygon offset

            if (pass == "seal") //Draws over to prevent clipping
            {
                this.State.PolygonOffsetFactor = -1f;
                this.State.PolygonOffsetUnits = 1f;
            }

            string displayFace = material.GetRenderInfoString("gsys_render_state_display_face");
            if (displayFace == "front")
            {
                State.CullFront = false;
                State.CullBack = true;
            }
            if (displayFace == "back")
            {
                State.CullFront = true;
                State.CullBack = false;
            }
            if (displayFace == "both")
            {
                State.CullFront = false;
                State.CullBack = false;
            }
            if (displayFace == "none")
            {
                State.CullFront = true;
                State.CullBack = true;
            }
        }

        static BlendEquationModeEXT ConvertBlendEquationMode(string mode)
        {
            switch (mode)
            {
                case "add": return BlendEquationModeEXT.FuncAdd;
            }
            return BlendEquationModeEXT.FuncAdd;
        }

        static DepthFunction ConvertDepthFunction(string func)
        {
            switch (func)
            {
                case "always": return DepthFunction.Always;
                case "equal": return DepthFunction.Equal;
                case "lequal": return DepthFunction.Lequal;
                case "gequal": return DepthFunction.Gequal;
                case "less": return DepthFunction.Less;
                case "greater": return DepthFunction.Greater;
                case "never": return DepthFunction.Never;
            }
            return DepthFunction.Lequal;
        }

        static AlphaFunction ConvertAlphaFunction(string func)
        {
            switch (func)
            {
                case "always": return AlphaFunction.Always;
                case "equal": return AlphaFunction.Equal;
                case "lequal": return AlphaFunction.Lequal;
                case "gequal": return AlphaFunction.Gequal;
                case "less": return AlphaFunction.Less;
                case "greater": return AlphaFunction.Greater;
                case "never": return AlphaFunction.Never;
            }
            return AlphaFunction.Lequal;
        }

        static BlendingFactor ConvertBlend(string func)
        {
            switch (func)
            {
                case "one": return BlendingFactor.One;
                case "zero": return BlendingFactor.Zero;
                case "src_alpha": return BlendingFactor.SrcAlpha;
                case "one_minus_src_alpha": return BlendingFactor.OneMinusSrcAlpha;
                case "one_minus_src_color": return BlendingFactor.OneMinusSrcColor;
            }
            return BlendingFactor.One;
        }
    }
}
