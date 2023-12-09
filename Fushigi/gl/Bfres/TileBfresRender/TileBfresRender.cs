using Fushigi.actor_pack.components;
using Fushigi.Bfres;
using Fushigi.course;
using Fushigi.util;
using Silk.NET.OpenGL;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using static Fushigi.course.CourseUnit;

namespace Fushigi.gl.Bfres
{
    public class TileBfresRender
    {
        private TileParamBlock TileParams;

        private BfresRender BfresRender;
        private Matrix4x4 Transform = Matrix4x4.Identity;
        private SkinDivision mSkinDivision;

        public record struct MaterialNames(string? Edge, string? Wall, string? Ground);
        public record struct UnitPackNames(string FullHit, string HalfHit, string NoHit, string Bridge);

        private Model SolidModel;
        private Model SemisolidModel;
        private Model NoCollisionModel;
        private Model BridgeModel;

        public TileBfresRender(GL gl, UnitPackNames packNames, SkinDivision skinDivision)
        {
            mSkinDivision = skinDivision;
            Load(gl, packNames);
        }

        private void Load(GL gl, UnitPackNames packNames)
        {
            BgUnitInfo? fullHitInfo = ActorPackCache.Load(packNames.FullHit)?.BgUnitInfo;
            BgUnitInfo? halfHitInfo = ActorPackCache.Load(packNames.HalfHit)?.BgUnitInfo;
            BgUnitInfo? noHitInfo   = ActorPackCache.Load(packNames.NoHit)?.BgUnitInfo;
            BgUnitInfo? bridgeInfo  = ActorPackCache.Load(packNames.Bridge)?.BgUnitInfo;

            if (fullHitInfo is null ||
                halfHitInfo is null ||
                noHitInfo is null ||
                bridgeInfo is null)
            {
                Debug.Fail("BgUnit Packs could not be loaded");
                return;
            }


            SolidModel?.Dispose();
            SolidModel = CreateModel(gl, fullHitInfo);

            SemisolidModel?.Dispose();
            SemisolidModel = CreateModel(gl, halfHitInfo);

            NoCollisionModel?.Dispose();
            NoCollisionModel = CreateModel(gl, noHitInfo);

            BridgeModel?.Dispose();
            BridgeModel = CreateModel(gl, bridgeInfo);
        }

        private static Model CreateModel(GL gl, BgUnitInfo bgUnitInfo)
        {
            var modelInfo = bgUnitInfo.GetModelInfo();
            var materialNames = bgUnitInfo.GetMaterialNames();

            var render = new BfresRender(BfresCache.Load(gl, modelInfo.bfresName));
            var model = render.Models[modelInfo.modelName];

            return new Model(gl, render, model, new MaterialNames(
                    Edge: materialNames.GetValueOrDefault("Edge"),
                    Wall: materialNames.GetValueOrDefault("Wall"),
                    Ground: materialNames.GetValueOrDefault("Belt")
                ));
        }

        public void Load(CourseUnitHolder unitHolder, Camera camera)
        {
            SolidModel.TileManager.Clear();
            SemisolidModel.TileManager.Clear();
            NoCollisionModel.TileManager.Clear();
            BridgeModel.TileManager.Clear();

            foreach (var unit in unitHolder.mUnits)
            {
                if (!unit.Visible)
                    continue;

                if (unit.mSkinDivision != this.mSkinDivision)
                    continue;

                var model = unit.mModelType switch
                {
                    ModelType.Solid => this.SolidModel,
                    ModelType.SemiSolid => this.SemisolidModel,
                    ModelType.NoCollision => this.NoCollisionModel,
                    ModelType.Bridge => this.BridgeModel,
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
                            if (tileIDEdge == 0)
                            {
                                model.TileManager.AddWallTile(pos, 0);
                            }
                            else
                            {
                                model.TileManager.AddEdgeTile(pos, tileIDEdge.GetValueOrDefault());

                                if (tileIDGround.TryGetValue(out int tileIDGroundValue))
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
            SolidModel.Render(gl, camera);
            SemisolidModel.Render(gl, camera);
            NoCollisionModel.Render(gl, camera);
            BridgeModel.Render(gl, camera);
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

        public sealed class Model : IDisposable
        {
            public BfresMaterialRender Wall;
            public BfresMaterialRender Ground;
            public BfresMaterialRender Edge;

            public TileManager TileManager = new TileManager();

            private BfresRender BfresRender;
            private BfresRender.BfresModel BfresModelRender;
            private bool isDisposed;

            public Model(GL gl, BfresRender render, BfresRender.BfresModel model, MaterialNames materialNames)
            {
                BfresRender = render;
                BfresModelRender = model;

                foreach (var mesh in model.Meshes)
                {
                    if (mesh.MaterialRender.Name == materialNames.Wall)
                        Wall = mesh.MaterialRender;
                    if (mesh.MaterialRender.Name == materialNames.Ground)
                        Ground = mesh.MaterialRender;
                    if (mesh.MaterialRender.Name == materialNames.Edge)
                        Edge = mesh.MaterialRender;
                }

                TileManager.Init(gl);
            }

            public void Render(GL gl, Camera camera)
            {
                GsysShaderRender.GsysResources.UserBlock1 = TileManager.TileParams;

                BfresModelRender.UpdateSkeleton(Matrix4x4.Identity);

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
                if (Wall == null) return;

                Wall.Render(gl, BfresRender, BfresModelRender, Matrix4x4.Identity, camera);
                TileManager.DrawWall();
            }

            private void DrawGround(GL gl, Camera camera)
            {
                if (Ground == null) return;

                Ground.Render(gl, BfresRender, BfresModelRender, Matrix4x4.Identity, camera);
                TileManager.DrawGround();

            }

            private void DrawEdge(GL gl, Camera camera)
            {
                if (Edge == null) return;

                Edge.Render(gl, BfresRender, BfresModelRender, Matrix4x4.Identity, camera);
                TileManager.DrawEdge();
            }
            public void Dispose()
            {
                if (isDisposed)
                    return;

                foreach (var model in BfresRender.Models.Values) model.Dispose();
                    
                isDisposed = true;
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
