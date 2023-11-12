using Fushigi.Bfres;
using Silk.NET.OpenGL;
using System.IO;
using System.Numerics;


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

        private void Init(GL gl, Stream stream)
        {
            try
            {
                BfresFile file = new BfresFile(stream);
                foreach (var model in file.Models.Values)
                    Models.Add(model.Name, new BfresModel(gl, model));
                foreach (var texture in file.TryGetTextureBinary().Textures)
                    Textures.Add(texture.Key, new BfresTextureRender(gl, texture.Value));
            }
            catch
            {

            }
        }

        internal void Render(GL gl, Matrix4x4 transform, Camera camera)
        {
            foreach (var model in Models.Values)
                model.Render(gl, this, transform, camera);
        }

        public class BfresModel
        {
            public List<BfresMesh> Meshes = new List<BfresMesh>();

            public bool IsVisible = true;

            public BfresModel(GL gl, Model model)
            {
                foreach (var shape in model.Shapes.Values)
                    Meshes.Add(new BfresMesh(gl, model, shape));
            }

            internal void Render(GL gl, BfresRender render, Matrix4x4 transform, Camera camera)
            {
                if (!IsVisible)
                    return;

                foreach (var mesh in Meshes)
                {
                    if (!mesh.IsVisible)
                        continue;

                    mesh.Render(gl, render, transform, camera.ViewProjectionMatrix);
                }
            }
        }

        public class BfresMesh
        {
            public bool IsVisible = true;

            private List<DetailLevel> LodMeshes = new List<DetailLevel>();

            public BfresMaterialRender MaterialRender = new BfresMaterialRender();

            //Resources
            private List<BufferObject> Buffers = new List<BufferObject>();
            private BufferObject IndexBuffer;
            private VertexArrayObject vbo;

            public BfresMesh(GL gl, Model model, Shape shape)
            {
                var material = model.Materials[shape.MaterialIndex];
                MaterialRender.Init(gl, material);

                IndexBuffer = new BufferObject(gl, BufferTargetARB.ElementArrayBuffer);
                IndexBuffer.SetData(shape.Meshes[0].IndexBuffer);

                foreach (var buffer in shape.VertexBuffer.Buffers)
                {
                    BufferObject bufferObject = new BufferObject(gl, BufferTargetARB.ArrayBuffer);
                    Buffers.Add(bufferObject);
                    bufferObject.SetData(buffer.Data);
                }

                vbo = new VertexArrayObject(gl, this.Buffers, IndexBuffer);

                foreach (var attr in shape.VertexBuffer.Attributes.Values)
                {
                    if (!AttributeNames.ContainsKey(attr.Name) || !FormatList.ContainsKey(attr.Format))
                        continue;

                    string name = AttributeNames[attr.Name];
                    var format = FormatList[attr.Format].Type;
                    var count = FormatList[attr.Format].Count;
                    var stride = shape.VertexBuffer.Buffers[attr.BufferIndex].Stride;
                    var normalized = FormatList[attr.Format].Normalized;

                    //Force normalize on normals, tangents, bitangents
                    if (attr.Name == "_n0" || attr.Name == "_t0" || attr.Name == "_b0")
                        normalized = true;

                    vbo.AddAttribute(name, count, format, normalized, stride, attr.Offset, attr.BufferIndex);
                }

                LodMeshes.Add(new DetailLevel()
                {
                    IndexCount = (int)shape.Meshes[0].IndexCount,
                    Type = ElementTypes[shape.Meshes[0].IndexFormat],
                    PrimitiveType = PrimitiveTypes[shape.Meshes[0].PrimitiveType],
                });
            }

            public void Render(GL gl, BfresRender renderer, Matrix4x4 transform, Matrix4x4 cameraMatrix)
            {
                var mesh = this.LodMeshes[0]; //only use first level of detail

                MaterialRender.Render(gl, renderer, transform, cameraMatrix);

                vbo.Enable(MaterialRender.Shader);
                vbo.Use();

                unsafe
                {
                    gl.DrawElements(mesh.PrimitiveType, (uint)mesh.IndexCount, mesh.Type, (void*)0);
                }
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
            }

            class DetailLevel
            {
                public int Offset;
                public int IndexCount;

                public DrawElementsType Type = DrawElementsType.UnsignedInt;

                public PrimitiveType PrimitiveType = PrimitiveType.Triangles;
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
                { "_t0", "aTangent" },
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

                //Ints
                { BfresAttribFormat.Format_10_10_10_2_UInt, new FormatInfo(4, true, VertexAttribIType.UnsignedInt) },
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
