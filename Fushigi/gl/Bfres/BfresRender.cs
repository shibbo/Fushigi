using Fushigi.Bfres;
using Fushigi.ui;
using Fushigi.util;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Xml.Linq;

namespace Fushigi.gl.Bfres
{
    public class BfresRender
    {
        public Dictionary<string, BfresTextureRender> Textures = new Dictionary<string, BfresTextureRender>();
        public Dictionary<string, BfresModel> Models = new Dictionary<string, BfresModel>();

        public BfresRender(GL gl, string filePath)
        {
            Init(gl, File.OpenRead(filePath));
        }

        public BfresRender(GL gl, Stream stream)
        {
            Init(gl, stream);
        }

        //Cached
        public BfresRender(BfresRender render)
        {
            foreach (var model in render.Models)
                Models.Add(model.Key, new BfresModel(model.Value));
            foreach (var texture in render.Textures)
                Textures.Add(texture.Key, texture.Value);
        }

        private void Init(GL gl, Stream stream)
        {
            BfresFile file = new BfresFile(stream);
            foreach (var model in file.Models.Values)
                Models.Add(model.Name, new BfresModel(gl, model));
            foreach (var texture in file.TryGetTextureBinary().Textures)
               Textures.Add(texture.Key, new BfresTextureRender(gl, texture.Value));
        }

        internal void Render(GL gl, Matrix4x4 transform, Camera camera)
        {
            foreach (var model in Models.Values)
                model.Render(gl, this, transform, camera);
        }

        public void Dispose()
        {
            foreach (var model in Models.Values)
                model.Dispose();

            foreach (var tex in Textures.Values)
                tex.Dispose();
        }

        public class BfresModel
        {
            public List<BfresMesh> Meshes = new List<BfresMesh>();

            public Skeleton Skeleton = new Skeleton();

            public BoundingBox BoundingBox = new BoundingBox();

            public bool IsVisible = true;

            public UniformBlock SkeletonBuffer; //matrix buffer for bone data

            public BfresModel(GL gl, Model model)
            {
                SkeletonBuffer = new UniformBlock(gl);

                Skeleton = model.Skeleton;
                foreach (var shape in model.Shapes.Values)
                    Meshes.Add(new BfresMesh(gl, this, model, shape));
            }

            //Cached
            public BfresModel(BfresModel bfresModel)
            {
                this.SkeletonBuffer = bfresModel.SkeletonBuffer;

                Skeleton = bfresModel.Skeleton;
                foreach (var mesh in bfresModel.Meshes)
                    Meshes.Add(mesh);
            }

            internal void Render(GL gl, BfresRender render, Matrix4x4 transform, Camera camera)
            {
                UpdateSkeleton(transform);

                foreach (var mesh in Meshes)
                    BoundingBox.Include(mesh.LodMeshes[0].BoundingBox);

                if (!IsVisible || !camera.InFrustum(BoundingBox))
                    return;

                foreach (var mesh in Meshes)
                {
                    if (!mesh.IsVisible)
                        continue;

                    //Cull in camera frustum
                    mesh.LodMeshes[0].BoundingBox.Transform(transform);

                    if (!camera.InFrustum(mesh.LodMeshes[0].BoundingBox, mesh.LodMeshes[0].BoundingRadius))
                        continue;

                    mesh.RenderGameShaders(gl, render, this, transform, camera);
                }
            }

            //Computes the skeleton matrix block
            public void UpdateSkeleton(Matrix4x4 root)
            {
                if (Skeleton.MatrixToBoneList == null)
                    return;

                var mem = new MemoryStream();
                using (var writer = new BinaryWriter(mem))
                {
                    //Smooth skinning using inverse matrices
                    for (int i = 0; i < Skeleton.NumSmoothMatrices; i++)
                    {
                        var bone_index = Skeleton.MatrixToBoneList[i];
                        var value = (Skeleton.Bones[bone_index].InverseMatrix) * root;

                        writer.Write(value.Column0());
                        writer.Write(value.Column1());
                        writer.Write(value.Column2());
                    }
                    //Rigid matrices using direct matrices
                    for (int i = 0; i < Skeleton.NumRigidMatrices; i++)
                    {
                        var bone_index = Skeleton.MatrixToBoneList[Skeleton.NumSmoothMatrices + i];
                        var value = Skeleton.Bones[bone_index].WorldMatrix * root;

                        writer.Write(value.Column0());
                        writer.Write(value.Column1());
                        writer.Write(value.Column2());
                    }
                }
                SkeletonBuffer.SetData(mem.ToArray());
            }

            public void Dispose()
            {
                foreach (var mesh in Meshes)
                    mesh.Dispose();
            }
        }

        public class BfresMesh
        {
            public bool IsVisible = true;

            public bool TransparentPass = false;

            public int BoneIndex = 0;

            public List<DetailLevel> LodMeshes = new List<DetailLevel>();

            public BfresMaterialRender MaterialRender = new BfresMaterialRender();

            //Resources
            private List<BufferObject> Buffers = new List<BufferObject>();
            private BufferObject IndexBuffer;
            private VertexArrayObject vbo;
            private VertexArrayObject vbo_game_shaders; //game shaders map attributes differently

            public BfresMesh(GL gl, BfresModel modelRender, Model model, Shape shape)
            {
                var material = model.Materials[shape.MaterialIndex];

                BoneIndex = shape.BoneIndex;

                TransparentPass = MaterialRender.GsysRenderState.State.EnableBlending;

                IndexBuffer = new BufferObject(gl, BufferTargetARB.ElementArrayBuffer);
                IndexBuffer.SetData(shape.Meshes[0].IndexBuffer);

                foreach (var buffer in shape.VertexBuffer.Buffers)
                {
                    BufferObject bufferObject = new BufferObject(gl, BufferTargetARB.ArrayBuffer);
                    Buffers.Add(bufferObject);
                    bufferObject.SetData(buffer.Data);
                }

                vbo = new VertexArrayObject(gl, this.Buffers, IndexBuffer);

                MaterialRender.Init(gl, modelRender, this, shape, material);

                foreach (var attr in shape.VertexBuffer.Attributes.Values)
                {
                    if (!AttributeNames.ContainsKey(attr.Name) || !FormatList.ContainsKey(attr.Format))
                        continue;

                    string name = AttributeNames[attr.Name];
                    var format = FormatList[attr.Format].Type;
                    var count = FormatList[attr.Format].Count;
                    var stride = shape.VertexBuffer.Buffers[attr.BufferIndex].Stride;
                    var normalized = FormatList[attr.Format].Normalized;

                    vbo.AddAttribute(name, count, format, normalized, stride, attr.Offset, attr.BufferIndex);

                }

                //Calculate the min/max from vertex positions
                Vector3 min = new Vector3(float.MaxValue);
                Vector3 max = new Vector3(float.MinValue);

                var positions = shape.VertexBuffer.GetPositions();
                var bone_indices = shape.VertexBuffer.GetBoneIndices();

                var indices = shape.Meshes[0].GetIndices();

                for (int i = 0; i < indices.Length; i++)
                {
                    var index = indices[i];
                    var position = new Vector3(positions[index].X, positions[index].Y, positions[index].Z);

                    if (shape.SkinCount == 0)
                    {
                        var bone_index = (int)shape.BoneIndex;
                        position = Vector3.Transform(position, model.Skeleton.Bones[bone_index].WorldMatrix);
                    }
                    if (shape.SkinCount == 1 && bone_indices.Length > 0)
                    {
                        var bone_index = (int)bone_indices[index].X;
                        position = Vector3.Transform(position, model.Skeleton.Bones[bone_index].WorldMatrix);
                    }

                    min.X = MathF.Min(min.X, position.X);
                    min.Y = MathF.Min(min.Y, position.Y);
                    min.Z = MathF.Min(min.Z, position.Z);
                    max.X = MathF.Max(max.X, position.X);
                    max.Y = MathF.Max(max.Y, position.Y);
                    max.Z = MathF.Max(max.Z, position.Z);
                }

                LodMeshes.Add(new DetailLevel()
                {
                    IndexCount = (int)shape.Meshes[0].IndexCount,
                    Type = ElementTypes[shape.Meshes[0].IndexFormat],
                    PrimitiveType = PrimitiveTypes[shape.Meshes[0].PrimitiveType],
                    BoundingBox = new BoundingBox()
                    {
                        Min = min,
                        Max = max,
                    },
                    BoundingRadius = 1F, //center xyz, w = radius size
                });
            }

            public void InitGameShaderVbo(GL gl, Material material, Shape shape, Dictionary<string, int> Attributes)
            {
                vbo_game_shaders = new VertexArrayObject(gl, this.Buffers, IndexBuffer);
                foreach (var attr in shape.VertexBuffer.Attributes.Values)
                {
                    //Unsupported format or not used by material, skip
                    if (material.ShaderAssign.AttributeAssign.Count > 0)
                    {
                        if (!FormatList.ContainsKey(attr.Format) || !material.ShaderAssign.AttributeAssign.ContainsKey(attr.Name))
                            continue;

                        //Use shader assign to map bfres attributes to shader
                        string attribute = material.ShaderAssign.AttributeAssign[attr.Name];

                        //Not in shader attribute list, skip
                        if (!Attributes.ContainsKey(attribute))
                            continue;
                    }
                    else
                    {
                        if (!Attributes.ContainsKey(attr.Name))
                            continue;
                    }

                    var location = Attributes[attr.Name];
                    var format = FormatList[attr.Format].Type;
                    var count = FormatList[attr.Format].Count;
                    var stride = shape.VertexBuffer.Buffers[attr.BufferIndex].Stride;
                    var normalized = FormatList[attr.Format].Normalized;

                    vbo_game_shaders.AddAttribute((uint)location, count, format, normalized, stride, attr.Offset, attr.BufferIndex);
                }
            }

            public void Render(GL gl, BfresRender renderer, BfresModel modelRender, Matrix4x4 transform, Camera camera)
            {
                var worldTransform = modelRender.Skeleton.Bones[this.BoneIndex].WorldMatrix * transform;

                MaterialRender.Render(gl, renderer, modelRender, worldTransform, camera);

                vbo.Enable(MaterialRender.Shader);
                vbo.Use();

                Draw(gl);
            }

            public void RenderMaterialOnly(GL gl, BfresRender renderer, BfresModel modelRender, Matrix4x4 transform, Camera camera)
            {
                var worldTransform = modelRender.Skeleton.Bones[this.BoneIndex].WorldMatrix * transform;
                MaterialRender.RenderGameShaders(gl, renderer, modelRender, worldTransform, camera);
            }

            public void RenderGameShaders(GL gl, BfresRender renderer, BfresModel modelRender, Matrix4x4 transform, Camera camera)
            {
                if (vbo_game_shaders == null)
                    return;

                var worldTransform = modelRender.Skeleton.Bones[this.BoneIndex].WorldMatrix * transform;
                MaterialRender.RenderGameShaders(gl, renderer, modelRender, worldTransform, camera);

                vbo_game_shaders.Enable(MaterialRender.Shader);
                vbo_game_shaders.Use();

                Draw(gl);
            }

            private void Draw(GL gl)
            {
                var mesh = this.LodMeshes[0]; //only use first level of detail

                unsafe
                {
                    gl.DrawElements(mesh.PrimitiveType, (uint)mesh.IndexCount, mesh.Type, (void*)0);
                }

                RenderStats.NumDrawCalls += 1;
                RenderStats.NumTriangles += mesh.IndexCount;

                //Reset render state
                GLMaterialRenderState.Reset(gl);
            }

            public void Dispose()
            {
                //Dispose all buffer objects
                foreach (var buffer in this.Buffers)
                    buffer?.Dispose();
                //Dispose index buffer
                IndexBuffer?.Dispose();
                //Dispose vertex array
                vbo?.Dispose();
                vbo_game_shaders?.Dispose();
            }

            public class DetailLevel
            {
                public int Offset;
                public int IndexCount;

                public DrawElementsType Type = DrawElementsType.UnsignedInt;

                public PrimitiveType PrimitiveType = PrimitiveType.Triangles;

                public BoundingBox BoundingBox;
                public float BoundingRadius;
            }

            Dictionary<BfresIndexFormat, DrawElementsType> ElementTypes = new Dictionary<BfresIndexFormat, DrawElementsType>()
            {
                { BfresIndexFormat.UInt16, DrawElementsType.UnsignedShort },
                { BfresIndexFormat.UInt32, DrawElementsType.UnsignedInt },
                { BfresIndexFormat.UnsignedByte, DrawElementsType.UnsignedByte },
            };

            Dictionary<BfresPrimitiveType, PrimitiveType> PrimitiveTypes = new Dictionary<BfresPrimitiveType, PrimitiveType>()
            {
                { BfresPrimitiveType.Triangles, PrimitiveType.Triangles },
                { BfresPrimitiveType.TriangleStrip, PrimitiveType.TriangleStrip },
            };

            Dictionary<string, string> AttributeNames = new Dictionary<string, string>()
            {
                { "_p0", "aPosition" },
                { "_n0", "aNormal" },
                { "_u0", "aTexCoord0" },
                { "_u1", "aTexCoord1" },
                { "_u2", "aTexCoord2" },
                { "_t0", "aTangent" },
                { "_c0", "aColor" },
            };

            Dictionary<BfresAttribFormat, FormatInfo> FormatList = new Dictionary<BfresAttribFormat, FormatInfo>()
            {
                { BfresAttribFormat.Format_32_32_32_32_Single, new FormatInfo(4, false, VertexAttribPointerType.Float) },
                { BfresAttribFormat.Format_32_32_32_Single, new FormatInfo(3, false, VertexAttribPointerType.Float) },
                { BfresAttribFormat.Format_32_32_Single, new FormatInfo(2, false, VertexAttribPointerType.Float) },
                { BfresAttribFormat.Format_32_Single, new FormatInfo(1, false, VertexAttribPointerType.Float) },

                { BfresAttribFormat.Format_16_16_16_16_Single, new FormatInfo(4, false, VertexAttribPointerType.HalfFloat) },
                { BfresAttribFormat.Format_16_16_Single, new FormatInfo(2, false, VertexAttribPointerType.HalfFloat) },
                { BfresAttribFormat.Format_16_Single, new FormatInfo(2, false, VertexAttribPointerType.HalfFloat) },

                { BfresAttribFormat.Format_16_16_SNorm, new FormatInfo(2, true, VertexAttribPointerType.Short) },
                { BfresAttribFormat.Format_16_16_UNorm, new FormatInfo(2, true, VertexAttribPointerType.UnsignedShort) },

                { BfresAttribFormat.Format_10_10_10_2_SNorm, new FormatInfo(4, true, VertexAttribPointerType.Int2101010Rev) },
                { BfresAttribFormat.Format_10_10_10_2_UNorm, new FormatInfo(4, true, VertexAttribPointerType.UnsignedInt2101010Rev) },

                { BfresAttribFormat.Format_8_8_8_8_SNorm, new FormatInfo(4, true, VertexAttribIType.Byte) },
                { BfresAttribFormat.Format_8_8_8_8_UNorm, new FormatInfo(4, true, VertexAttribIType.UnsignedByte) },

                { BfresAttribFormat.Format_8_8_UNorm, new FormatInfo(2, true, VertexAttribIType.UnsignedByte) },
                { BfresAttribFormat.Format_8_UNorm, new FormatInfo(1, true, VertexAttribIType.UnsignedByte) },

                //Ints
                { BfresAttribFormat.Format_10_10_10_2_UInt, new FormatInfo(4, true, VertexAttribIType.UnsignedInt) },

                { BfresAttribFormat.Format_8_8_8_8_UInt, new FormatInfo(4, false, VertexAttribIType.UnsignedByte) },
                { BfresAttribFormat.Format_8_8_UInt, new FormatInfo(2, false, VertexAttribIType.UnsignedByte) },
                { BfresAttribFormat.Format_8_UInt, new FormatInfo(1, false, VertexAttribIType.UnsignedByte) },

                { BfresAttribFormat.Format_16_16_16_16_UInt, new FormatInfo(4, false, VertexAttribIType.UnsignedShort) },
                { BfresAttribFormat.Format_16_16_UInt, new FormatInfo(2, false, VertexAttribIType.UnsignedShort) },
                { BfresAttribFormat.Format_16_UInt, new FormatInfo(1, false, VertexAttribIType.UnsignedShort) },
            };
           

            class FormatInfo
            {
                public VertexAttribPointerType Type;
                public int Count;
                public bool Normalized = false;

                public bool IsInt = false;

                public FormatInfo(int count, VertexAttribPointerType type)
                {
                    Count = count;
                    Type = type;
                }

                public FormatInfo(int count, bool normalized, VertexAttribPointerType type)
                {
                    Count = count;
                    Type = type;
                    Normalized = normalized;
                }

                public FormatInfo(int count, bool normalized, VertexAttribIType type)
                {
                    Count = count;
                    Type = (VertexAttribPointerType)type;
                    Normalized = normalized;
                    IsInt = true;
                }
            }
        }
    }
}
