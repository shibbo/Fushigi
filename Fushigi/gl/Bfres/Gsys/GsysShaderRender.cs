using Fushigi.Bfres;
using Fushigi.gl.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Fushigi.gl.Bfres
{
    public class GsysShaderRender : BfshaShaderRender
    {
        //GSYS render info state (blend, polygon, alpha test)
        public GsysRenderState RenderState = new GsysRenderState();
        //GSYS render info parameters (shadow cast, CubeMap info)
        public GsysRenderParameters RenderParameters = new GsysRenderParameters();
        //GSYS shared resources (uniform blocks, cubemap textures, samplers)
        public static GsysResources GsysResources = new GsysResources();

        private Material Material;

        public override void Init(GL gl, BfresRender.BfresModel modelRender, BfresRender.BfresMesh meshRender, Shape shape, Material material)
        {
            Material = material;

            //Prepare the parameters
            RenderParameters.Init(material);
            RenderState.Init(material);

            meshRender.TransparentPass = false;

            if (RenderState.State.EnableBlending)
                meshRender.TransparentPass = true;

            //Init shader data
            base.Init(gl, modelRender, meshRender, shape, material);

            GsysResources.Init(gl);
        }

        /// <summary>
        /// Finds the shader to use via material options and mesh skin count. 
        /// </summary>
        public override BfshaFile.ShaderProgram ReloadProgram(byte skinCount)
        {            
            //Find program index via option choices
            Dictionary<string, string> options = new Dictionary<string, string>();

            foreach (var op in Material.ShaderAssign.ShaderOptions)
            {
                if (!ShaderModel.StaticShaderOptions.ContainsKey(op.Key))
                    continue;

                string choice = op.Value;
                switch (choice)
                {
                    case "True": //boolean type
                        choice = "1";
                        break;
                    case "False": //boolean type
                        choice = "0";
                        break;
                }

                var shaderOp = ShaderModel.StaticShaderOptions[op.Key];
                if (!shaderOp.Choices.ContainsKey(choice))
                    continue;

                options.Add(op.Key, choice);
            }

            //Update option from render state
            this.RenderState.LoadRenderStateOptions(options);

            //Dynamic options.
            options.Add("gsys_weight", skinCount.ToString());
            //Material pass
            options.Add("gsys_assign_type", "gsys_assign_material");

            //Get program index
            int programIndex = ShaderModel.GetProgramIndex(options);
          //  Console.WriteLine($"{this.Material.Name} programIndex {programIndex}");

            if (programIndex == -1)
                return null;

            return ShaderModel.Programs[programIndex];
        }

        public override GLTexture GetExternalTexture(GL gl, string sampler)
        {
            switch (sampler)
            {
                case "gsys_cube_map": return GsysResources.CubeMap;
                case "gsys_lightmap_diffuse": return GsysResources.DiffuseLightmap;
                case "gsys_lightmap_specular": return GsysResources.SpecularLightmap;

                case "gsys_user0": return GsysResources.UserTexture0;
                case "gsys_user1": return GsysResources.UserTexture1;
                case "gsys_user2": return GsysResources.UserTexture2;
                case "gsys_user3": return GsysResources.UserTexture3;
                case "gsys_user4": return GsysResources.UserTexture4;
                case "gsys_user5": return GsysResources.UserTexture5;
                //Projected textures like clouds, decals
                case "gsys_projection0": return GsysResources.Projection0;
                case "gsys_projection1": return GsysResources.Projection1;

                case "gsys_ssao": return GsysResources.SSAO;
                case "gsys_depth_shadow": return GsysResources.DepthShadow;

                case "gsys_normalized_linear_depth":return GsysResources.LinearNormalizedDepth;
                case "gsys_half_normalized_linear_depth": return GsysResources.HalfLinearNormalizedDepth;
            }
            return base.GetExternalTexture(gl, sampler);
        }

        public override UniformBlock GetUniformBlock(string name)
        {
            //Use global uniform data updated via scene
            switch (name)
            {
                //Shared resources
                case "gsys_context": return GsysResources.ContextBlock;
                case "gsys_environment": return GsysResources.EnvironmentBlock;
                case "gsys_scene_material": return GsysResources.SceneMaterialBlock;
                case "gsys_user0": return GsysResources.UserBlock0;
                case "gsys_user1": return GsysResources.UserBlock1;
                case "gsys_user2": return GsysResources.UserBlock2;
                //Per draw call
                case "gsys_material": return this.MaterialBlock;
                case "gsys_shape": return this.ShapeBlock;
                case "gsys_skeleton": return this.SkeletonBlock;
                case "gsys_shader_option": return this.MaterialOptionBlock;
            }
            return base.GetUniformBlock(name);
        }

        public override void Render(GL gl, BfresRender renderer, BfresRender.BfresModel model, Matrix4x4 transform, Camera camera)
        {
            //Render state
            RenderState.Render(gl);
            //Render shader data
            base.Render(gl, renderer, model, transform, camera);
        }
    }
}
