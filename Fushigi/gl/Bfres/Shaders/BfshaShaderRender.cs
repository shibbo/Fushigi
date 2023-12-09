using Fushigi.Bfres;
using Fushigi.Bfres.Shaders;
using Fushigi.gl.Shaders;
using Fushigi.util;
using Newtonsoft.Json.Linq;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Fushigi.Bfres.BfshaFile;
using static System.Net.Mime.MediaTypeNames;


namespace Fushigi.gl.Bfres
{
    /// <summary>
    /// A renderer for bfsha shader files/shader models.
    /// </summary>
    public class BfshaShaderRender
    {
        private TegraShaderDecoder.ShaderInfo ShaderInfo;

        private Material Material;

        public BfshaFile ShaderFile;
        public BfshaFile.ShaderModel ShaderModel;

        //Resources
        private BfshaFile.ShaderProgram ShaderProgram;
        private UniformBlock ConstantVSBlock;
        private UniformBlock ConstantFSBlock;

        internal UniformBlock MaterialBlock;
        internal UniformBlock ShapeBlock;
        internal UniformBlock SkeletonBlock;
        internal UniformBlock MaterialOptionBlock;

        //Make this static as it is shared across all materials
        internal static UniformBlock SupportBlock;

        public virtual void Init(GL gl, BfresRender.BfresModel modelRender, BfresRender.BfresMesh meshRender, Shape shape, Material material) {
            Material = material;

            //Search for shader archive
            ShaderFile = TryGetShader(material.ShaderAssign.ShaderArchiveName, material.ShaderAssign.ShadingModelName);

            //Search for shader model
            if (ShaderFile != null && ShaderFile.ShaderModels.ContainsKey(material.ShaderAssign.ShadingModelName))
                ShaderModel = ShaderFile.ShaderModels[material.ShaderAssign.ShadingModelName];

            if (ShaderModel == null)
                return;

            MaterialBlock = new UniformBlock(gl);
            ShapeBlock = new UniformBlock(gl);
            MaterialOptionBlock = new UniformBlock(gl);

            if (SupportBlock == null)
            {
                SupportBlock = new UniformBlock(gl);
                this.LoadSupportingBlock();
            }

            GLUtil.Label(gl, ObjectIdentifier.Buffer, MaterialBlock.ID,  "Material Block");
            GLUtil.Label(gl, ObjectIdentifier.Buffer, ShapeBlock.ID, "Shape Block");
            GLUtil.Label(gl, ObjectIdentifier.Buffer, MaterialOptionBlock.ID,  "Material Option");

            SetMaterialBlock(MaterialBlock, material);
            SetShapeBlock(ShapeBlock, Matrix4x4.Identity);

            Dictionary<string, int> attributeLocations = new Dictionary<string, int>();
            for (int i = 0; i < ShaderModel.Attributes.Count; i++)
            {
                string key = ShaderModel.Attributes.GetKey(i);
                int location = ShaderModel.Attributes[i].Location;
                attributeLocations.Add(key, location);
            }
            if (attributeLocations.Count == 0)
            {
                attributeLocations.Add("_p0", 0);
                attributeLocations.Add("_u0", 1);
            }
            meshRender.InitGameShaderVbo(gl, material, shape, attributeLocations);

            ShaderProgram = ReloadProgram(shape.SkinCount);
        }

        public virtual void ReloadMaterialBlock()
        {
            SetMaterialBlock(this.MaterialBlock, this.Material);
        }


        public virtual void SetMaterialBlock(UniformBlock block, Material material)
        {
            byte[] data = ShaderUtil.GenerateShaderParamBuffer(ShaderModel, material);
            block.SetData(data);
        }

        public virtual void SetShapeBlock(UniformBlock block, Matrix4x4 mat)
        {
            var mem = new MemoryStream();
            using (var writer = new BinaryWriter(mem))
            {
                writer.Write(mat.Column0());
                writer.Write(mat.Column1());
                writer.Write(mat.Column2());

                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
            }
            block.SetData(mem.ToArray());
        }

        public virtual void SetSkeletonBlock(UniformBlock block, BfresRender.BfresModel model, Matrix4x4 root)
        {
          
        }

        /// <summary>
        /// Finds the shader to use via material options and mesh skin count. 
        /// </summary>
        public virtual BfshaFile.ShaderProgram ReloadProgram(byte skinCount)
        {
            return null; 
        }

        /// <summary>
        /// Looks for a texture to use that is not present in the bfres.
        /// </summary>
        public virtual GLTexture GetExternalTexture(GL gl, string sampler)
        {
            return null;
        }

        /// <summary>
        /// Looks for the uniform block resource to bind.
        /// </summary>
        public virtual UniformBlock GetUniformBlock(string name)
        {
            return null;
        }

        /// <summary>
        /// Compiles the given bfsha program to "ShaderInfo" with glsl code.
        /// </summary>
        public void CompileShader(GL gl, BfshaFile.ShaderProgram program)
        {
            var variation = ShaderModel.GetShaderVariation(program);
            if (variation == null)
                throw new Exception($"Failed to load shader variation!");

            ShaderInfo = TegraShaderDecoder.LoadShaderProgram(gl, variation);

            if (ShaderInfo.VertexConstants != null)
            {
                ConstantVSBlock = new UniformBlock(gl);
                ConstantVSBlock.SetData(ShaderInfo.VertexConstants);
                GLUtil.Label(gl, ObjectIdentifier.Buffer, ConstantVSBlock.ID, "Constants VS Block");
            }
            if (ShaderInfo.FragmentConstants != null)
            {
                ConstantFSBlock = new UniformBlock(gl);
                ConstantFSBlock.SetData(ShaderInfo.FragmentConstants);
                GLUtil.Label(gl, ObjectIdentifier.Buffer, ConstantFSBlock.ID, "Constants FS Block");
            }
        }

        public virtual void Render(GL gl, BfresRender renderer, BfresRender.BfresModel model, Matrix4x4 transform, Camera camera)
        {          
            if (ShaderProgram == null)
                return;

            SkeletonBlock = model.SkeletonBuffer;

            this.SetShapeBlock(ShapeBlock, transform);
            SetSkeletonBlock(SkeletonBlock, model, transform);

            if (ShaderInfo == null)
                CompileShader(gl, ShaderProgram);

            //Shader failed, skip
            if (ShaderInfo == null)
                return;

            ShaderInfo.Shader.Use();
            BindUniformBlocks(gl);
            BindSamplers(gl, renderer);
        }

        //Bind uniform block data
        private void BindUniformBlocks(GL gl)
        {
            int ubo_bind = 1;

            //Constants
            if (this.ConstantVSBlock != null)
                ConstantVSBlock.Render(this.ShaderInfo.Shader.ID, "_vp_c1", ubo_bind++);
            if (this.ConstantFSBlock != null)
                ConstantFSBlock.Render(this.ShaderInfo.Shader.ID, "_fp_c1", ubo_bind++);

            SupportBlock.Render(this.ShaderInfo.Shader.ID, "_support_buffer", ubo_bind++);

            ubo_bind = 5;

            //Bind the uniform blocks to the shader
            for (int i = 0; i < ShaderModel.UniformBlocks.Count; i++)
            {
                string name = ShaderModel.UniformBlocks.GetKey(i);
                //Get the shader location info
                var locationInfo = this.ShaderProgram.UniformBlockIndices[i];
                int fragLocation = locationInfo.FragmentLocation;
                int vertLocation = locationInfo.VertexLocation;

                //Block unused for this program so skip it
                if (vertLocation == -1)
                    continue;

                //Get the block. If one does not exist, this method will create one by default.
                //This can be overriden for sharing existing blocks between scenes and files.
                var shaderBlock = GetUniformBlock(name);
                if (shaderBlock == null)
                    continue;

                //Bind uniform data to the vertex and/or pixel location and prepare a binding ID
                //Prepare a unique binding per stage
                BindUniformBlock(shaderBlock, this.ShaderInfo.Shader.ID, vertLocation, -1, ubo_bind++);
            }

            for (int i = 0; i < ShaderModel.UniformBlocks.Count; i++)
            {
                string name = ShaderModel.UniformBlocks.GetKey(i);
                //Get the shader location info
                var locationInfo = this.ShaderProgram.UniformBlockIndices[i];
                int fragLocation = locationInfo.FragmentLocation;
                int vertLocation = locationInfo.VertexLocation;

                //Block unused for this program so skip it
                if (fragLocation == -1)
                    continue;

                //Get the block. If one does not exist, this method will create one by default.
                //This can be overriden for sharing existing blocks between scenes and files.
                var shaderBlock = GetUniformBlock(name);
                if (shaderBlock == null)
                    continue;

                //Bind uniform data to the vertex and/or pixel location and prepare a binding ID
                //Prepare a unique binding per stage
                BindUniformBlock(shaderBlock, this.ShaderInfo.Shader.ID, -1, fragLocation, ubo_bind++);
            }
        }

        //Bind sampler data
        private void BindSamplers(GL gl, BfresRender renderer)
        {
            int id = 1;


            BindBindlessTextures(gl, ShaderInfo.Shader);

            for (int i = 0; i < ShaderModel.Samplers.Count; i++)
            {
                var slot = i + 1;
                gl.ActiveTexture(TextureUnit.Texture0 + slot);
                gl.BindTexture(TextureTarget.Texture2D, 0);
                gl.BindTexture(TextureTarget.Texture2DArray, 0);
                gl.BindTexture(TextureTarget.TextureCubeMap, 0);
                gl.BindTexture(TextureTarget.TextureCubeMapArray, 0);
                gl.BindTexture(TextureTarget.Texture3D, 0);
            }

            for (int i = 0; i < ShaderModel.Samplers.Count; i++)
            {
                var locationInfo = this.ShaderProgram.SamplerIndices[i];
                //Currently only using the vertex and fragment stages
                if (locationInfo.VertexLocation == -1 && locationInfo.FragmentLocation == -1)
                    continue;

                string sampler = ShaderModel.Samplers.GetKey(i);
                var textureIndex = -1;

             //   Console.WriteLine($"sampler {sampler} loc {locationInfo.FragmentLocation} slot {i} uniform {ConvertSamplerID(locationInfo.FragmentLocation)}");

                //Sampler assign has a key list of fragment shader samplers, value list of bfres material samplers
                if (this.Material.ShaderAssign.SamplerAssign.ContainsKey(sampler))
                {
                    //Get the resource sampler
                    //Important to note, fragment samplers are unique while material samplers can be the same
                    //So we need to lookup which material sampler the current fragment sampler uses.
                    string resSampler = this.Material.ShaderAssign.SamplerAssign[sampler].String;
                    //Find a texture using the sampler
                    textureIndex = this.Material.Samplers.Keys.ToList().FindIndex(x => x == resSampler);
                }

                var slot = i + 1;
                   var tex = GLImageCache.GetDefaultTexture(gl);
              //  GLTexture tex = null;

                if (textureIndex == -1) //Cannot find the texture so try loading it from an external source
                {
                    //Get external textures (ie shadow maps, cubemaps, etc)
                    var external_texture = GetExternalTexture(gl, sampler);
                    if (external_texture != null)
                        tex = external_texture;
                }
                else //Bind texture inside the bfres material
                {
                    var texMap = this.Material.Textures[textureIndex];
                    tex = TryGetTexture(gl, renderer, texMap);
                }

                //Verify type
                if (tex != null)
                    tex = CheckTargetType(gl, tex, ShaderInfo.Shader, locationInfo.FragmentLocation);

                if (tex != null)
                {
                    gl.ActiveTexture(TextureUnit.Texture0 + slot);
                    tex.Bind();

                    if (textureIndex != -1)
                    {
                        var samplerConfig = this.Material.Samplers[textureIndex];
                        SetupSampler(gl, tex, samplerConfig);
                    }

                    SetUniformSampler(ShaderInfo.Shader, locationInfo.VertexLocation, locationInfo.FragmentLocation, ref slot);
                }
            }

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.BindTexture(TextureTarget.TextureCubeMap, 0);
            gl.BindTexture(TextureTarget.TextureCubeMapArray, 0);
            gl.BindTexture(TextureTarget.Texture2DArray, 0);
            gl.BindTexture(TextureTarget.Texture3D, 0);
        }

        private GLTexture CheckTargetType(GL gl, GLTexture tex, GLShader shader, int location)
        {
            string uniform_name = ConvertSamplerID(location, false);
            if (!shader.UniformInfo.ContainsKey(uniform_name))
                return tex;

            var type = shader.UniformInfo[uniform_name];
            //Check if type is correct
            if (tex.Target == TextureTarget.Texture2D && type == UniformType.Sampler2DArray)
            {
                //Type is wrong, find or create a sub texture with the correct type usage
                foreach (var t in tex.SubTextures)
                {
                    if (t.Target == TextureTarget.Texture2DArray)
                        return t;
                }
                var copy = GLTexture.ToCopy(gl, tex, TextureTarget.Texture2DArray);
                tex.SubTextures.Add(copy);

                return copy;
            }
            return tex;
        }

        private GLTexture TryGetTexture(GL gl, BfresRender renderer, string texName)
        {
            if (renderer.Textures.ContainsKey(texName))
            {
                var texture = renderer.Textures[texName];

                if (!(texture is BfresTextureRender))
                    return texture; //GL texture generated at runtime

                ((BfresTextureRender)texture).CheckState();
                if (((BfresTextureRender)texture).TextureState == BfresTextureRender.State.Finished)
                {
                    return texture;
                }
            }
            return GLImageCache.GetDefaultTexture(gl);
        }

        private void SetupSampler(GL gl, GLTexture texture, Fushigi.Bfres.Sampler sampler = null)
        {
            if (sampler == null)
                return;

            gl.TexParameter(texture.Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            gl.TexParameter(texture.Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            if (sampler.WrapModeU == TexWrap.Clamp)
                gl.TexParameter(texture.Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);

            if (sampler.WrapModeV == TexWrap.Clamp)
                gl.TexParameter(texture.Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);


            //   gl.TexParameter(texture.Target, TextureParameterName.TextureWrapS, (int)WrapModes[sampler.WrapModeU]);
            //   gl.TexParameter(texture.Target, TextureParameterName.TextureWrapT, (int)WrapModes[sampler.WrapModeV]);
            //   gl.TexParameter(texture.Target, TextureParameterName.TextureWrapS, (int)WrapModes[sampler.WrapModeU]);
            //   gl.TexParameter(texture.Target, TextureParameterName.TextureWrapT, (int)WrapModes[sampler.WrapModeV]);
            //  gl.TexParameter(texture.Target, TextureParameterName.TextureWrapR, (int)WrapModes[sampler.WrapModeW]);
            gl.TexParameter(texture.Target, TextureParameterName.TextureMaxLod, sampler.MaxLOD);
            gl.TexParameter(texture.Target, TextureParameterName.TextureMinLod, sampler.MinLOD);
            gl.TexParameter(texture.Target, TextureParameterName.TextureLodBias, sampler.LODBias);

            if (sampler.MagFilter == ExpandFilterModes.Linear)
                gl.TexParameter(texture.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            else if (sampler.MagFilter == ExpandFilterModes.Points)
                gl.TexParameter(texture.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);


            /*
                                    if (sampler.MinFilter == ShrinkFilterModes.Linear && sampler.Mipmap == MipFilterModes.Linear)
                                        gl.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                                    else if (sampler.MinFilter == ShrinkFilterModes.Linear && sampler.Mipmap == MipFilterModes.Points)
                                        gl.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest);
                                    else if (sampler.MinFilter == ShrinkFilterModes.Linear && sampler.Mipmap == MipFilterModes.None)
                                        gl.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                                    else if (sampler.MinFilter == ShrinkFilterModes.Points && sampler.Mipmap == MipFilterModes.Linear)
                                        gl.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);
                                    else if (sampler.MinFilter == ShrinkFilterModes.Points && sampler.Mipmap == MipFilterModes.Points)
                                        gl.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
                                    else if (sampler.MinFilter == ShrinkFilterModes.Points && sampler.Mipmap == MipFilterModes.None)
                                        gl.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                                    */
        }


        Dictionary<TexWrap, TextureWrapMode> WrapModes = new Dictionary<TexWrap, TextureWrapMode>()
        {
            { TexWrap.Repeat, TextureWrapMode.Repeat },
            { TexWrap.Mirror, TextureWrapMode.MirroredRepeat },
            { TexWrap.MirrorOnce, TextureWrapMode.MirroredRepeat },
            { TexWrap.MirrorOnceClampToEdge, TextureWrapMode.MirroredRepeat },
            { TexWrap.Clamp, TextureWrapMode.ClampToEdge },
            { TexWrap.ClampToEdge, TextureWrapMode.ClampToEdge },
        };
        
        public virtual void BindBindlessTextures(GL gl, GLShader shader)
        {

        }

        public virtual BfshaFile TryGetShader(string archive_name, string shader_name)
        {
            return BfshaShaderCache.GetShader(archive_name, shader_name);
        }

        private void BindUniformBlock(UniformBlock block, uint programID, int vertexLocation, int fragmentLocation, int bind)
        {
            if (vertexLocation != -1)
                block.Render(programID, $"_vp_c{vertexLocation + 3}", bind);

            if (fragmentLocation != -1)
                block.Render(programID, $"_fp_c{fragmentLocation + 3}", bind);
        }

        private void SetUniformSampler(GLShader shader, int vertexLocation, int fragmentLocation, ref int id)
        {
            if (vertexLocation != -1)
                shader.SetUniform(ConvertSamplerID(vertexLocation, true), id);
            if (fragmentLocation != -1)
                shader.SetUniform(ConvertSamplerID(fragmentLocation, false), id);
            //Only increase the slot once as each stage share slots.
            id++;
        }

        private string ConvertSamplerID(int id, bool vertexShader = false)
        {
            if (vertexShader)
                return "vp_t_tcb_" + ((id * 2) + 8).ToString("X1");
            else
                return "fp_t_tcb_" + ((id * 2) + 8).ToString("X1");
        }

        private void LoadSupportingBlock()
        {
            var mem = new System.IO.MemoryStream();
            using (var writer = new BinaryWriter(mem))
            {
                writer.Write(new Vector4(0)); //s_alpha_test
                for (int i = 0; i < 8; i++)
                    writer.Write(new Vector4(0)); //s_is_bgra
                writer.Write(new Vector4(0)); //s_viewport_inverse
                writer.Write(new Vector4(0)); //s_frag_scale_count
                for (int i = 0; i < 71; i++)
                    writer.Write(new Vector4(1.0f)); //s_render_scale
            }
            SupportBlock.SetData(mem.ToArray());
        }
    }
}