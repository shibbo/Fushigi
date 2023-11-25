using Fushigi.Bfres;
using Fushigi.course;
using Fushigi.ui;
using Fushigi.util;
using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static Fushigi.course.CourseUnit;
using static Fushigi.gl.Bfres.TileBfresRender;
using static Fushigi.gl.Bfres.WonderGameShader;

namespace Fushigi.gl.Bfres
{
    public class TileBfresRender
    {
        private TileParamBlock TileParams;

        private BfresRender BfresRender;
        private Matrix4x4 Transform = Matrix4x4.Identity;

        private List<Model> Models = new List<Model>();

        public TileBfresRender(GL gl)
        {
            Load(gl);
        }

        private void Load(GL gl, string name = "UnitHajimariSougen")
        {
            var file_path = FileUtil.FindContentPath(Path.Combine("Model", $"{name}.bfres.zs"));
            if (!File.Exists(file_path))
                return;

            BfresRender?.Dispose();
            BfresRender = new BfresRender(gl, FileUtil.DecompressAsStream(file_path));

            Models.Clear();
            foreach (var model in BfresRender.Models.Values)
                Models.Add(new Model(gl, BfresRender, model));
        }

        public void Load(CourseUnitHolder mUnitHolder, Camera camera)
        {
            foreach (var unit in mUnitHolder.mUnits)
            {
                if (!unit.Visible)
                    continue;

                //var model = this.Models[0];
                //model.TileManager.Clear();

                var model = unit.mModelType switch
                {
                    ModelType.Solid => this.Models[0],
                    ModelType.SemiSolid => this.Models[2],
                    ModelType.NoCollision => this.Models[1],
                    _ => null
                };

                if (model == null)
                    continue;

                if (unit.mTileSubUnits.Count > 0)
                {
                    var clipMin = new Vector2(float.NegativeInfinity);
                    var clipMax = new Vector2(float.PositiveInfinity);

                    var nearZ = unit.mTileSubUnits.Min(x => x.mOrigin.Z);
                    var farZ = unit.mTileSubUnits.Max(x => x.mOrigin.Z);

                    foreach (TileSubUnit subUnit in unit.mTileSubUnits)
                    {
                        var origin2D = new Vector2(subUnit.mOrigin.X, subUnit.mOrigin.Y);

                        foreach (var (tileIDEdge, tileIDGround, position) in subUnit.GetTiles(clipMin - origin2D, clipMax - origin2D))
                        {
                            var pos = subUnit.mOrigin + new Vector3(position, subUnit.mOrigin.Z);
                            if(tileIDEdge == 0)
                            {
                                model.TileManager.AddWallTile(pos, 0);
                            }
                            else
                            {
                                model.TileManager.AddEdgeTile(pos, tileIDEdge.GetValueOrDefault());

                                if(tileIDGround.TryGetValue(out int tileIDGroundValue))
                                    model.TileManager.AddGroundTile(pos, tileIDGroundValue);
                            }

                            //     var bb = new BoundingBox(pos - new Vector3(0.5f), pos + new Vector3(0.5f));
                            //     if (camera.InFrustum(bb))
                        }

                        foreach (var (x, y, width, height, type) in subUnit.mSlopes)
                        {
                            var bbMin = subUnit.mOrigin + new Vector3(x, y, 0);
                            var bbMax = bbMin + new Vector3(width, height, 0);

                            var bbTL = new Vector3(bbMin.X, bbMax.Y, 0);
                            var bbTR = new Vector3(bbMax.X, bbMax.Y, 0);
                            var bbBL = new Vector3(bbMin.X, bbMin.Y, 0);
                            var bbBR = new Vector3(bbMax.X, bbMin.Y, 0);
                        }
                    }
                }
                model.TileManager.UpdateTileParameters();
            }
        }

        public void Render(GL gl, Camera camera)
        {
            //zen, nashi, han skin models
            Models[0].Render(gl, camera);
            Models[1].Render(gl, camera);
            Models[2].Render(gl, camera);
        }


        public class TileManager
        {
            public List<TileParamBlock.Tile> WallTiles = new List<TileParamBlock.Tile>();
            public List<TileParamBlock.Tile> EdgeTiles = new List<TileParamBlock.Tile>();
            public List<TileParamBlock.Tile> GroundTiles = new List<TileParamBlock.Tile>();

            private RenderMesh<Vertex> WallMesh;
            private RenderMesh<Vertex> GroundMesh;
            private RenderMesh<Vertex> EdgeMesh;

            public TileParamBlock TileParams;

            public void AddWallTile(Vector3 pos, int tileID)
            {
                WallTiles.Add(new TileParamBlock.Tile()
                {
                    Position = pos,
                    TileTextureID = tileID,
                });
            }

            public void AddGroundTile(Vector3 pos, int tileID)
            {
                GroundTiles.Add(new TileParamBlock.Tile()
                {
                    Position = pos,
                    TileTextureID = tileID,
                });
            }

            public void AddEdgeTile(Vector3 pos, int tileID)
            {
                EdgeTiles.Add(new TileParamBlock.Tile()
                {
                    Position = pos,
                    TileTextureID = tileID,
                });
            }

            public void DrawWall() => WallMesh.Draw();
            public void DrawEdge() => EdgeMesh.Draw();
            public void DrawGround() => GroundMesh.Draw();

            public void Clear()
            {
                this.WallTiles.Clear();
                this.EdgeTiles.Clear();
                this.GroundTiles.Clear();
            }

            public void Init(GL gl)
            {
                TileParams = new TileParamBlock(gl);
                WallMesh = new RenderMesh<Vertex>(gl, new Vertex[0], new int[0]);
                GroundMesh = new RenderMesh<Vertex>(gl, new Vertex[0], new int[0]);
                EdgeMesh = new RenderMesh<Vertex>(gl, new Vertex[0], new int[0]);

                UpdateTileParameters();
            }

            private void UpdateIndices()
            {
               // List<int> wall_indices = this.WallTiles.Select(x => TileParams.TileBuffer.IndexOf(x)).ToList();
               // List<int> ground_indices = this.GroundTiles.Select(x => TileParams.TileBuffer.IndexOf(x)).ToList();
               // List<int> edge_indices = this.EdgeTiles.Select(x => TileParams.TileBuffer.IndexOf(x)).ToList();
            }

            public void UpdateTileParameters()
            {
                /*  TileParams.TileBuffer.Clear();
                  TileParams.TileBuffer.AddRange(this.WallTiles);
                  TileParams.TileBuffer.AddRange(this.GroundTiles);
                  TileParams.TileBuffer.AddRange(this.EdgeTiles);
                  TileParams.Update();*/

                UpdateMesh(WallMesh, this.WallTiles);
                UpdateMesh(GroundMesh, this.GroundTiles);
                UpdateMesh(EdgeMesh, this.EdgeTiles);

               // UpdateIndices();
            }

            private void UpdateMesh(RenderMesh<Vertex> mesh, List<TileParamBlock.Tile> tile_ind_params)
            {
                List<Vertex> vertices = new List<Vertex>();
                List<int> indices = new List<int>();
                int[] quad_indices = new int[6] { 0, 1, 2, 2, 3, 0 };

                var offset = new Vector3(0.5f, 0.5f, 0);
              //  Vector2 offset = new Vector2(0, 0);

                int index = 0;
                for (int i = 0; i < tile_ind_params.Count; i++)
                {
                    //The game actually transforms tiles in the tile buffer, but to keep things simple, do this per quad
                    //This helps keep the uniform block memory to be much lower
                    var pos = offset + tile_ind_params[i].Position;
                    Vertex[] quad = new Vertex[4];
                    //4 vertex positions as quad in local space
                    quad[0] = new Vertex(new Vector3(-0.5f, -0.5f, 0) + pos, new Vector2(0, 1), tile_ind_params[i].TileTextureID);
                    quad[1] = new Vertex(new Vector3(0.5f, -0.5f, 0) + pos, new Vector2(1, 1), tile_ind_params[i].TileTextureID);
                    quad[2] = new Vertex(new Vector3(0.5f, 0.5f, 0) + pos, new Vector2(1, 0), tile_ind_params[i].TileTextureID);
                    quad[3] = new Vertex(new Vector3(-0.5f, 0.5f, 0) + pos, new Vector2(0, 0), tile_ind_params[i].TileTextureID);
                    vertices.AddRange(quad);

                    //Indices
                    for (int j = 0; j < quad_indices.Length; j++)
                        indices.Add(index + quad_indices[j]);

                    index += 4;
                }
                mesh.VertexBuffers[0].SetData(vertices.ToArray());
                mesh.indexBufferData.SetData(indices.ToArray());

                mesh.DrawCount = indices.Count;
            }
        }

        public class Model
        {
            public BfresMaterialRender Wall;
            public BfresMaterialRender Ground;
            public BfresMaterialRender Edge;

            public TileManager TileManager = new TileManager();

            private BfresRender BfresRender;
            private BfresRender.BfresModel BfresModelRender;

            public Model(GL gl, BfresRender render, BfresRender.BfresModel model)
            {
                BfresRender = render;
                BfresModelRender = model;

                foreach (var mesh in model.Meshes)
                {
                    if (mesh.MaterialRender.Name.EndsWith("KabeMat"))
                        Wall = mesh.MaterialRender;
                    if (mesh.MaterialRender.Name.EndsWith("ObiMat"))
                        Ground = mesh.MaterialRender;
                    if (mesh.MaterialRender.Name.EndsWith("FuchiMat"))
                        Edge = mesh.MaterialRender;
                }

                TileManager.Init(gl);
            }

            public void Render(GL gl, Camera camera)
            {
                GsysShaderRender.GsysResources.UserBlock1 = TileManager.TileParams;

                BfresModelRender.UpdateSkeleton(Matrix4x4.Identity);
                GsysShaderRender.GsysResources.UpdateViewport(camera);

                if (TileManager.EdgeTiles.Count > 0)
                    DrawEdge(gl, camera);
                if (TileManager.WallTiles.Count > 0)
                    DrawWall(gl, camera);
                if (TileManager.GroundTiles.Count > 0)
                    DrawGround(gl, camera);

                //Reset render state
                GLMaterialRenderState.Reset(gl);
            }

            private void DrawWall(GL gl, Camera camera)
            {
                Wall.RenderGameShaders(gl, BfresRender, BfresModelRender, Matrix4x4.Identity, camera);
                TileManager.DrawWall(); 
            }

            private void DrawGround(GL gl, Camera camera)
            {
                if (Ground == null) return;

                Ground.RenderGameShaders(gl, BfresRender, BfresModelRender, Matrix4x4.Identity, camera);
                TileManager.DrawGround();

            }

            private void DrawEdge(GL gl, Camera camera)
            {
                Edge.RenderGameShaders(gl, BfresRender, BfresModelRender, Matrix4x4.Identity, camera);
                TileManager.DrawEdge();
            }
        }

        public struct Vertex
        {
            [RenderAttribute(0, VertexAttribPointerType.Float, 0)]
            public Vector3 Position;

            [RenderAttribute(1, VertexAttribPointerType.Float, 12)]
            public Vector3 TexCoord; //+ tile id in TileParamBlock

            public Vertex(Vector3 position, Vector2 texCoord, float tileID)
            {
                Position = position;
                TexCoord = new Vector3(texCoord, tileID);
            }
        }
    }

    public class TileParamBlock : UniformBlock
    {
        public List<Tile> TileBuffer = new List<Tile>();

        public TileParamBlock(GL gl) : base(gl)
        {
            //The game assigns a unique slot per tile
            //However to keep things simple, only add 105 entries to access the tile texture id
            for (int i = 0; i < 105; i++)
            {
                TileBuffer.Add(new Tile()
                {
                    Position = new Vector3(0, 0, 2), //We tranform per quad atm
                    CameraDistance = 0,
                    TileTextureID = i, //Unique texture id per quad
                }); 
            }
            Update();
        }

        public void Clear()
        {

        }

        public void Update()
        {
            var mem = new MemoryStream();
            using (var writer = new BinaryWriter(mem))
            {
                writer.Write(new Vector4(0));
                writer.Write(new Vector4(0));
                writer.Write(new Vector4(0));
                writer.Write(new Vector4(0));

                for (int i = 0; i < TileBuffer.Count; i++)
                {
                    writer.Write(new Vector4(TileBuffer[i].Position, TileBuffer[i].CameraDistance));
                    writer.Write(new Vector4(
                        TileBuffer[i].TileTextureID,
                        TileBuffer[i].Unknown,
                        TileBuffer[i].Unknown1,
                        TileBuffer[i].Unknown2));
                }
            }
            this.SetData(mem.ToArray());
        }

        public class Tile
        {
            public Vector3 Position = Vector3.Zero;
            public float CameraDistance = 0; //-154;
            public float TileTextureID = 0;

            public float Unknown1 = 2;
            public float Unknown2 = 1;
            public float Unknown = 1;
        }
    }
}
