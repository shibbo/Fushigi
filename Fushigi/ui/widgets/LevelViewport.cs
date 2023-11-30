using Fushigi.actor_pack.components;
using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using Fushigi.course;
using Fushigi.course.distance_view;
using Fushigi.gl;
using Fushigi.gl.Bfres;
using Fushigi.param;
using Fushigi.util;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using static Fushigi.course.CourseUnit;
using Vector3 = System.Numerics.Vector3;

namespace Fushigi.ui.widgets
{
    interface IViewportDrawable
    {
        void Draw2D(CourseAreaEditContext editContext, LevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj);
    }

    interface IViewportSelectable
    {
        void OnSelect(CourseAreaEditContext editContext);
        public static void DefaultSelect(CourseAreaEditContext ctx, object selectable)
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift))
            {
                ctx.Select(selectable);
            }
            else
            {
                ctx.WithSuspendUpdateDo(() =>
                {
                    ctx.DeselectAll();
                    ctx.Select(selectable);
                });

            }
        }
    }

    interface ITransformableObject
    {
        Transform Transform { get; }
    }

    internal class LevelViewport(CourseArea area, GL gl, CourseAreaScene areaScene)
    {
        readonly CourseArea mArea = area;

        //this is only so BgUnitRail works, TODO make private
        private readonly CourseAreaEditContext mEditContext = areaScene.EditContext;

        ImDrawListPtr mDrawList;
        public EditorMode mEditorMode = EditorMode.Actors;
        public EditorState mEditorState = EditorState.Selecting;

        public bool IsViewportHovered;
        public bool IsViewportActive;

        Vector2 mSize = Vector2.Zero;

        private Vector3? mSelectedPoint;
        private int mWallIdx = -1;
        private int mUnitIdx = -1;
        private int mPointIdx = -1;
        private IDictionary<string, bool>? mLayersVisibility;
        Vector2 mTopLeft = Vector2.Zero;
        public string mActorToAdd = "";
        public string mLayerToAdd = "PlayArea1";
        public bool mIsLinkNew = false;
        public string mNewLinkType = "";

        public Camera Camera = new Camera();
        public GLFramebuffer Framebuffer; //Draws opengl data into the viewport
        public HDRScreenBuffer HDRScreenBuffer = new HDRScreenBuffer();
        public VRSkybox VRSkybox;
        public TileBfresRender TileBfresRenderFieldA;
        public TileBfresRender TileBfresRenderFieldB;

        DistantViewManager DistantViewScrollManager = new DistantViewManager(area);

        //TODO make this an ISceneObject? as soon as there's a SceneObj class for each course object
        private object? mHoveredObject;
        public CourseLink? CurCourseLink = null;
        public Vector3? HoveredPoint;

        public uint GridColor = 0x77_FF_FF_FF;
        public float GridLineThickness = 1.5f;

        public enum EditorState
        {
            Selecting,
            SelectingActorLayer,
            AddingActor,
            DeleteActorLinkCheck,
            DeletingActor,
            SelectingLinkSource,
            SelectingLinkDest
        }

        public enum EditorMode
        {
            Actors,
            Units
        }

        public bool IsHovered(ISceneObject obj) => mHoveredObject == obj;

        public Matrix4x4 GetCameraMatrix() => Camera.ViewProjectionMatrix;

        public Vector2 WorldToScreen(Vector3 pos) => WorldToScreen(pos, out _);
        public Vector2 WorldToScreen(Vector3 pos, out float ndcDepth)
        {
            var ndc = Vector4.Transform(pos, Camera.ViewProjectionMatrix);
            ndc /= ndc.W;

            ndcDepth = ndc.Z;

            return mTopLeft + new Vector2(
                (ndc.X * .5f + .5f) * mSize.X,
                (1 - (ndc.Y * .5f + .5f)) * mSize.Y
            );
        }

        public Vector3 ScreenToWorld(Vector2 pos, float ndcDepth = 0)
        {
            pos -= mTopLeft;

            var ndc = new Vector3(
                (pos.X / mSize.X) * 2 - 1,
                (1 - (pos.Y / mSize.Y)) * 2 - 1,
                ndcDepth
            );

            var world = Vector4.Transform(ndc, Camera.ViewProjectionMatrixInverse);
            world /= world.W;

            return new(world.X, world.Y, world.Z);
        }

        public void FrameSelectedActor(CourseActor actor)
        {
            this.Camera.Target = new Vector3(actor.mTranslation.X, actor.mTranslation.Y, 0);
        }

        public void SelectBGUnit(BGUnitRail rail)
        {
            mEditContext.DeselectAllOfType<BGUnitRail>();
            mEditContext.Select(rail);
        }

        public void SelectedActor(CourseActor actor)
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift))
            {
                mEditContext.Select(actor);
            }
            else
            {
                mEditContext.DeselectAll();
                mEditContext.Select(actor);
            }
        }

        public void HandleCameraControls(double deltaSeconds)
        {
            bool isPanGesture = (ImGui.IsMouseDragging(ImGuiMouseButton.Middle)) ||
                (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.GetIO().KeyShift);

            if (IsViewportActive && isPanGesture)
            {
                Camera.Target += ScreenToWorld(ImGui.GetMousePos() - ImGui.GetIO().MouseDelta) -
                    ScreenToWorld(ImGui.GetMousePos());
            }

            if (IsViewportHovered)
            {
                Camera.Distance *= MathF.Pow(2, -ImGui.GetIO().MouseWheel / 10);

                // Default camera distance is 10, so speed is constant until 0.5 at 20
                const float baseCameraSpeed = 0.25f * 60;
                const float scalingRate = 10.0f;
                var zoomSpeedFactor = Math.Max(Camera.Distance / scalingRate, 1);
                var zoomedCameraSpeed = MathF.Floor(zoomSpeedFactor) * baseCameraSpeed;
                var dt = (float)deltaSeconds;

                if (ImGui.IsKeyDown(ImGuiKey.LeftArrow) || ImGui.IsKeyDown(ImGuiKey.A))
                {
                    Camera.Target.X -= zoomedCameraSpeed * dt;
                }

                if (ImGui.IsKeyDown(ImGuiKey.RightArrow) || ImGui.IsKeyDown(ImGuiKey.D))
                {
                    Camera.Target.X += zoomedCameraSpeed * dt;
                }

                if (ImGui.IsKeyDown(ImGuiKey.UpArrow) || ImGui.IsKeyDown(ImGuiKey.W))
                {
                    Camera.Target.Y += zoomedCameraSpeed * dt;
                }

                if (ImGui.IsKeyDown(ImGuiKey.DownArrow) || ImGui.IsKeyDown(ImGuiKey.S))
                {
                    Camera.Target.Y -= zoomedCameraSpeed * dt;
                }
            }
        }

        public void DrawScene3D(Vector2 size, IDictionary<string, bool> layersVisibility)
        {
            mLayersVisibility = layersVisibility;

            if (Framebuffer == null)
                Framebuffer = new GLFramebuffer(gl, FramebufferTarget.Framebuffer, (uint)size.X, (uint)size.Y, InternalFormat.Rgba16f);

            if (VRSkybox == null)
                VRSkybox = new VRSkybox(gl);


            //TODO put this somewhere else and maybe cache this
            TileBfresRender CreateTileRendererForSkin(SkinDivision division, string skinName)
            {
                var bootupPack = RomFS.GetOrLoadBootUpPack();

                var bytes = bootupPack.OpenFile(
                    "System/CombinationDataTableData/DefaultBgUnitSkinConfigTable.pp__CombinationDataTableData.bgyml");
                var table = BymlSerialize.Deserialize<DefaultBgUnitSkinConfigTable>(bytes);


                var render = new TileBfresRender(gl,
                    new TileBfresRender.UnitPackNames(
                        FullHit: table.GetPackName(skinName, "FullHit"),
                        HalfHit: table.GetPackName(skinName, "HalfHit"),
                        NoHit: table.GetPackName(skinName, "NoHit"),
                        Bridge: table.GetPackName(skinName, "Bridge")
                    ), division);
                render.Load(this.mArea.mUnitHolder, this.Camera);

                foreach (var courseUnit in this.mArea.mUnitHolder.mUnits.Where(x=>x.mSkinDivision == division))
                {
                    courseUnit.TilesUpdated += delegate
                    {
                        render.Load(this.mArea.mUnitHolder, this.Camera);
                    };
                }

                this.Camera.OnCameraChanged += delegate
                {
                    //  render.Load(this.mArea.mUnitHolder, this.Camera);
                };

                return render;
            }
            string? fieldASkin = mArea.mAreaParams.SkinParam?.FieldA;
            string? fieldBSkin = mArea.mAreaParams.SkinParam?.FieldB;

            if (TileBfresRenderFieldA == null && !string.IsNullOrEmpty(fieldASkin))
                TileBfresRenderFieldA = CreateTileRendererForSkin(SkinDivision.FieldA, fieldASkin);

            if (TileBfresRenderFieldB == null && !string.IsNullOrEmpty(fieldBSkin))
                TileBfresRenderFieldB = CreateTileRendererForSkin(SkinDivision.FieldB, fieldBSkin);

            //Resize if needed
            if (Framebuffer.Width != (uint)size.X || Framebuffer.Height != (uint)size.Y)
                Framebuffer.Resize((uint)size.X, (uint)size.Y);

            Framebuffer.Bind();

            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, Framebuffer.Width, Framebuffer.Height);

            gl.Enable(EnableCap.DepthTest);

            RenderStats.Reset();

            //Update the gsys viewport uniforms once per frame
            GsysShaderRender.GsysResources.UpdateViewport(this.Camera);
            //Wonder shader system params
            WonderGameShader.UpdateSystem();

            //Distance view calculations
            DistantViewScrollManager.Calc(this.Camera.Target);

            TileBfresRenderFieldA?.Render(gl, this.Camera);
            TileBfresRenderFieldB?.Render(gl, this.Camera);

            foreach (var actor in this.mArea.GetActors())
            {
                if (actor.mActorPack == null || mLayersVisibility.ContainsKey(actor.mLayer) && !mLayersVisibility[actor.mLayer])
                    continue;

                RenderActor(actor, actor.mActorPack.ModelInfoRef);
                RenderActor(actor, actor.mActorPack.DrawArrayModelInfoRef);
            }

            //  VRSkybox.Render(gl, this.Camera);

            Framebuffer.Unbind();

            ImGui.SetCursorScreenPos(mTopLeft);

            //Draw final output in post buffer
            HDRScreenBuffer.Render(gl, (int)size.X, (int)size.Y, (GLTexture2D)Framebuffer.Attachments[0]);

            Framebuffer.Unbind();

            //Draw framebuffer
            ImGui.Image((IntPtr)HDRScreenBuffer.GetOutput().ID, new Vector2(size.X, size.Y));

            ImGui.SetNextItemAllowOverlap();

            //Temp quick fix, add canvas below for handling 2d pick later
            ImGui.SetCursorScreenPos(mTopLeft);
            ImGui.InvisibleButton("canvas", size,
                  ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);
        }

        private void RenderActor(CourseActor actor, ModelInfo modelInfo)
        {
            if (modelInfo == null || modelInfo.mFilePath == null)
                return;

            var resourceName = modelInfo.mFilePath;
            var modelName = modelInfo.mModelName;

            var render = BfresCache.Load(gl, resourceName);
            if (render == null || !render.Models.ContainsKey(modelName))
                return;

            var transMat = Matrix4x4.CreateTranslation(actor.mTranslation);
            var scaleMat = Matrix4x4.CreateScale(actor.mScale);
            var rotMat = Matrix4x4.CreateRotationX(actor.mRotation.X) *
                     Matrix4x4.CreateRotationY(actor.mRotation.Y) *
                    Matrix4x4.CreateRotationZ(actor.mRotation.Z);

            var mat = scaleMat * rotMat * transMat;

            DistantViewScrollManager.UpdateMatrix(actor.mLayer, ref mat);

            var model = render.Models[modelName];
            //switch for drawing models with different methods easier
            switch (modelName)
            {
                case "DokanTop":
                    var matPTop =
                    Matrix4x4.CreateScale(actor.mScale.X, actor.mScale.X, actor.mScale.Z) *
                    Matrix4x4.CreateTranslation(0, (actor.mScale.Y - actor.mScale.X) * 2, 0) *
                    rotMat *
                    transMat;

                    var matPMid =
                    Matrix4x4.CreateScale(actor.mScale.X, actor.mScale.Y * 2, actor.mScale.Z) *
                    rotMat *
                    transMat;

                    model.Render(gl, render, matPTop, this.Camera);
                    render.Models["DokanMiddle"].Render(gl, render, matPMid, this.Camera);
                    break;
                default:
                    model.Render(gl, render, mat, this.Camera);
                    break;
            }
        }

        public void Draw(Vector2 size, double deltaSeconds, IDictionary<string, bool> layersVisibility)
        {
            mLayersVisibility = layersVisibility;
            mTopLeft = ImGui.GetCursorScreenPos();

            ImGui.InvisibleButton("canvas", size,
                ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);

            IsViewportHovered = ImGui.IsItemHovered();
            IsViewportActive = ImGui.IsItemActive();

            if (size.X * size.Y == 0)
                return;

            mSize = size;
            mDrawList = ImGui.GetWindowDrawList();

            ImGui.PushClipRect(mTopLeft, mTopLeft + size, true);

            HandleCameraControls(deltaSeconds);

            if (Camera.Width != mSize.X || Camera.Height != mSize.Y)
            {
                Camera.Width = mSize.X;
                Camera.Height = mSize.Y;
            }

            if (!Camera.UpdateMatrices())
                return;

            this.DrawScene3D(size, mLayersVisibility);

            DrawGrid();
            DrawAreaContent();

            if (!IsViewportHovered)
                mHoveredObject = null;

            CourseActor? hoveredActor = mHoveredObject as CourseActor;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (hoveredActor != null)
                    ImGui.SetTooltip($"{hoveredActor.mActorName}");

                if (ImGui.IsKeyPressed(ImGuiKey.Z) && ImGui.GetIO().KeySuper)
                {
                    mEditContext.Undo();
                }
                if (ImGui.IsKeyPressed(ImGuiKey.Y) && ImGui.GetIO().KeySuper || ImGui.IsKeyPressed(ImGuiKey.Z) && ImGui.GetIO().KeyShift && ImGui.GetIO().KeySuper)
                {
                    mEditContext.Redo();
                }
            }
            else
            {
                if (hoveredActor != null)
                    ImGui.SetTooltip($"{hoveredActor.mActorName}");

                if (ImGui.IsKeyPressed(ImGuiKey.Z) && ImGui.GetIO().KeyCtrl)
                {
                    mEditContext.Undo();
                }
                if (ImGui.IsKeyPressed(ImGuiKey.Y) && ImGui.GetIO().KeyCtrl || ImGui.IsKeyPressed(ImGuiKey.Z) && ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl)
                {
                    mEditContext.Redo();
                }
            };



            bool isFocused = ImGui.IsWindowFocused();

            if (isFocused && mEditorState == EditorState.Selecting)
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    if (mEditContext.IsSingleObjectSelected(out CourseActor? actor))
                    {
                        Vector3 posVec = ScreenToWorld(ImGui.GetMousePos());

                        if (ImGui.GetIO().KeyShift)
                        {
                            actor.mTranslation = posVec;
                        }
                        else
                        {
                            posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                            posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                            posVec.Z = actor.mTranslation.Z;
                            actor.mTranslation = posVec;
                        }
                    }
                    if (mEditContext.IsSingleObjectSelected(out CourseRail.CourseRailPoint? rail))
                    {
                        Vector3 posVec = ScreenToWorld(ImGui.GetMousePos());

                        if (ImGui.GetIO().KeyShift)
                        {
                            rail.mTranslate = posVec;
                        }
                        else
                        {
                            posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                            posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                            posVec.Z = rail.mTranslate.Z;
                            rail.mTranslate = posVec;
                        }
                    }
                }

                if (ImGui.IsItemClicked())
                {
                    bool isModeActor = mHoveredObject != null;
                    bool isModeUnit = HoveredPoint != null;

                    if (isModeActor && !isModeUnit)
                    {
                        mEditorMode = EditorMode.Actors;
                    }

                    if (isModeUnit && !isModeActor)
                    {
                        mEditorMode = EditorMode.Units;
                    }

                    /* if the user clicked somewhere and it was not hovered over an element, 
                        * we clear our selected actors array */
                    if (mHoveredObject == null)
                    {
                        mEditContext.DeselectAll();
                    }
                    else if (mHoveredObject is IViewportSelectable obj)
                    {
                        obj.OnSelect(mEditContext);
                    }
                    else
                    {
                        //TODO remove this once all course objects have IViewportSelectable SceneObjs
                        IViewportSelectable.DefaultSelect(mEditContext, mHoveredObject);
                    }

                    if (HoveredPoint == null)
                    {
                        mSelectedPoint = null;
                    }
                    else
                    {
                        mSelectedPoint = HoveredPoint;
                    }
                }

                if (ImGui.IsKeyPressed(ImGuiKey.Delete))
                {
                    mEditorState = EditorState.DeleteActorLinkCheck;
                }

                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    mEditContext.DeselectAll();
                    mSelectedPoint = null;
                }
            }
            else if (isFocused && mEditorState == EditorState.AddingActor)
            {
                ImGui.SetTooltip($"Placing actor {mActorToAdd} -- Hold SHIFT to place multiple, ESCAPE to cancel.");

                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    mEditorState = EditorState.Selecting;
                }

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    CourseActor actor = new CourseActor(mActorToAdd, mArea.mRootHash, mLayerToAdd);

                    Vector3 posVec = ScreenToWorld(ImGui.GetMousePos());
                    posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                    posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                    posVec.Z = 0.0f;
                    actor.mTranslation = posVec;

                    mEditContext.AddActor(actor);

                    if (!ImGui.GetIO().KeyShift)
                    {
                        mActorToAdd = "";
                        mEditorState = EditorState.Selecting;
                    }
                }
            }
            else if (isFocused && mEditorState == EditorState.DeleteActorLinkCheck)
            {

            }
            else if (isFocused && mEditorState == EditorState.DeletingActor)
            {
                if (!isFocused)
                {
                    ImGui.SetWindowFocus();
                }

                if (mEditContext.IsAnySelected<CourseActor>())
                {
                    mEditContext.DeleteSelectedActors();
                    mEditorState = EditorState.Selecting;
                }
                else
                {
                    if (hoveredActor != null)
                        ImGui.SetTooltip($"""
                            Click to delete {hoveredActor.mActorName}.
                            Hold SHIFT to delete multiple actors, ESCAPE to cancel.
                            """);
                    else
                        ImGui.SetTooltip("""
                            Click on any actor to delete it.
                            Hold SHIFT to delete multiple actors, ESCAPE to cancel.
                            """);

                    if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                    {
                        mEditorState = EditorState.Selecting;
                    }

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        if (hoveredActor != null)
                        {
                            mEditContext.DeleteActor(hoveredActor);

                            if (!ImGui.GetIO().KeyShift)
                            {
                                mEditorState = EditorState.Selecting;
                            }
                        }
                    }
                }
            }
            else if (isFocused && (mEditorState == EditorState.SelectingLinkSource || mEditorState == EditorState.SelectingLinkDest))
            {
                /* when we are begining to select a link, we will not always be immediately focused */
                if (!isFocused)
                {
                    ImGui.SetWindowFocus();
                }

                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    mEditorState = EditorState.Selecting;
                }

                if (mEditorState == EditorState.SelectingLinkSource)
                {
                    if (hoveredActor != null)
                    {
                        ImGui.SetTooltip($"Select the source actor you wish to link to. Press ESCAPE to cancel.\n Currently Hovered: {hoveredActor.mActorName}");
                    }
                    else
                    {
                        ImGui.SetTooltip($"Select the source actor you wish to link to. Press ESCAPE to cancel.");
                    }
                }

                if (mEditorState == EditorState.SelectingLinkDest)
                {
                    if (hoveredActor != null)
                    {
                        ImGui.SetTooltip($"Select the destination actor you wish to link to. Press ESCAPE to cancel.\n Currently Hovered: {hoveredActor.mActorName}");
                    }
                    else
                    {
                        ImGui.SetTooltip($"Select the destination actor you wish to link to. Press ESCAPE to cancel.");
                    }
                }

                /* if our link is new, it means that we don't have to check for hovered actors for the source designation */
                if (mIsLinkNew)
                {
                    CurCourseLink = new(mNewLinkType);
                    CourseActor selActor = mEditContext.GetSelectedObjects<CourseActor>().ElementAt(0);
                    CurCourseLink.mSource = selActor.GetHash();
                    mIsLinkNew = false;
                }

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (mEditorState == EditorState.SelectingLinkDest)
                    {
                        if (hoveredActor != null)
                        {
                            /* new links have a destination of 0 because there is no hash associated with a null actor */
                            bool isNewLink = CurCourseLink.GetDestHash() == 0;
                            ulong hash = hoveredActor.GetHash();
                            CurCourseLink.mDest = hash;

                            if (isNewLink)
                            {
                                mEditContext.AddLink(CurCourseLink);
                            }

                            mEditorState = EditorState.Selecting;
                        }
                    }

                }
            }

            ImGui.PopClipRect();
        }

        void DrawGrid()
        {
            DrawGridLines(false, 20f, 10);
            DrawGridLines(true, 20f, 10);
        }

        void DrawGridLines(bool is_vertical,
                                  float min_minor_tick_size,
                                  int major_tick_interval)
        {
            // grid lines are drawn in intervals from a to b
            // the 0 and 1 coordinates represent the line ends at a and b
            Vector2 a0, a1, b0, b1;
            float min_value, max_value, a, b;

            Vector2 min = mTopLeft;
            Vector2 max = mTopLeft + mSize;

            Vector3 minWorld = ScreenToWorld(min);
            Vector3 maxWorld = ScreenToWorld(max);

            if (is_vertical)
            {
                min_value = maxWorld.Y;
                max_value = minWorld.Y;

                a = max.Y;
                b = min.Y;

                a0 = new Vector2(min.X, a);
                a1 = new Vector2(max.X, a);
                b0 = new Vector2(min.X, b);
                b1 = new Vector2(max.X, b);
            }
            else
            {
                min_value = minWorld.X;
                max_value = maxWorld.X;

                a = min.X;
                b = max.X;

                a0 = new Vector2(a, min.Y);
                a1 = new Vector2(a, max.Y);
                b0 = new Vector2(b, min.Y);
                b1 = new Vector2(b, max.Y);
            }

            float ideal_tick_interval =
                min_minor_tick_size * (max_value - min_value) / MathF.Abs(b - a);
            float tick_interval_log = MathF.Log(ideal_tick_interval) / MathF.Log(major_tick_interval);
            float tick_interval = MathF.Pow(major_tick_interval, MathF.Ceiling(tick_interval_log));
            float blend = 1 - (tick_interval_log - MathF.Floor(tick_interval_log));

            float min_tick_value = MathF.Ceiling(min_value / tick_interval) * tick_interval;
            int tick_offset = (int)MathF.Ceiling(min_value / tick_interval);
            int tick_count = (int)MathF.Floor(max_value / tick_interval) -
                             (int)MathF.Floor(min_value / tick_interval) + 1;

            for (int i = 0; i < tick_count; i++)
            {
                bool is_major_tick = (i + tick_offset) % major_tick_interval == 0;

                float t = ((min_tick_value + i * tick_interval) - min_value) / (max_value - min_value);

                Vector4 colorVec = ImGui.ColorConvertU32ToFloat4(GridColor);
                colorVec.W *= is_major_tick ? 1f : blend;
                mDrawList.AddLine(a0 * (1 - t) + b0 * t, a1 * (1 - t) + b1 * t,
                              ImGui.ColorConvertFloat4ToU32(colorVec), GridLineThickness);
            }
        }

        private static Vector2[] s_actorRectPolygon = new Vector2[4];

        void DrawAreaContent()
        {
            const float pointSize = 3.0f;
            Vector3? newHoveredPoint = null;
            object? newHoveredObject = null;

            foreach (var unit in this.mArea.mUnitHolder.mUnits)
            {
                if (!unit.Visible)
                    continue;
/*
                if (unit.mTileSubUnits.Count > 0)
                {
                    var clipMin = new Vector2(float.NegativeInfinity);
                    var clipMax = new Vector2(float.PositiveInfinity);

                    var nearZ = unit.mTileSubUnits.Min(x => x.mOrigin.Z);
                    var farZ = unit.mTileSubUnits.Max(x => x.mOrigin.Z);

                    foreach (TileSubUnits subUnit in unit.mTileSubUnits.OrderBy(x => x.mOrigin.Z))
                    {
                        float blend = ((subUnit.mOrigin.Z - farZ) / (nearZ - farZ));
                        if (float.IsNaN(blend)) blend = 0;

                        uint color = ImGui.ColorConvertFloat4ToU32(
                            (1 - blend) * new Vector4(0f, 0f, 1f, 1f) +
                            blend * new Vector4(0f, 1f, 1f, 1f)
                            );

                        var origin2D = new Vector2(subUnit.mOrigin.X, subUnit.mOrigin.Y);

                        foreach (var (tileID, position) in subUnit.mTileMap.GetTiles(clipMin - origin2D, clipMax - origin2D))
                        {
                            mDrawList.AddRectFilled(
                                WorldToScreen(subUnit.mOrigin + new Vector3(position, 0) +
                                new Vector3(0, unit.mModelType == ModelType.Bridge ? .5f : 0, 0)),
                                WorldToScreen(subUnit.mOrigin + new Vector3(position, 0) +
                                new Vector3(1, 1, 0)),
                                color
                                );
                        }

                        foreach (var (x, y, width, height, type) in subUnit.mSlopes)
                        {
                            var bbMin = subUnit.mOrigin + new Vector3(x, y, 0);
                            var bbMax = bbMin + new Vector3(width, height, 0);

                            var bbTL = new Vector3(bbMin.X, bbMax.Y, 0);
                            var bbTR = new Vector3(bbMax.X, bbMax.Y, 0);
                            var bbBL = new Vector3(bbMin.X, bbMin.Y, 0);
                            var bbBR = new Vector3(bbMax.X, bbMin.Y, 0);

                            switch (type)
                            {
                                case TileSubUnits.SlopeType.UpperLeft:
                                    mDrawList.AddTriangleFilled(
                                        WorldToScreen(bbTL),
                                        WorldToScreen(bbBL),
                                        WorldToScreen(bbTR),
                                        color
                                    );
                                    break;
                                case TileSubUnits.SlopeType.UpperRight:
                                    mDrawList.AddTriangleFilled(
                                        WorldToScreen(bbTL),
                                        WorldToScreen(bbBR),
                                        WorldToScreen(bbTR),
                                        color
                                    );
                                    break;
                                case TileSubUnits.SlopeType.LowerLeft:
                                    mDrawList.AddTriangleFilled(
                                        WorldToScreen(bbTL),
                                        WorldToScreen(bbBL),
                                        WorldToScreen(bbBR),
                                        color
                                    );
                                    break;
                                case TileSubUnits.SlopeType.LowerRight:
                                    mDrawList.AddTriangleFilled(
                                        WorldToScreen(bbTR),
                                        WorldToScreen(bbBL),
                                        WorldToScreen(bbBR),
                                        color
                                    );
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }*/
            }

            areaScene.ForEach<IViewportDrawable>(obj =>
            {
                bool isNewHoveredObj = false;
                obj.Draw2D(mEditContext, this, mDrawList, ref isNewHoveredObj);
                if (isNewHoveredObj)
                    newHoveredObject = obj;
            });

            if (mArea.mRailHolder.mRails.Count > 0)
            {
                uint color = System.Drawing.Color.HotPink.ToAbgr();

                foreach (CourseRail rail in mArea.mRailHolder.mRails)
                {
                    bool rail_selected = mEditContext.IsSelected(rail);

                    Vector2[] GetPoints()
                    {
                        Vector2[] points = new Vector2[rail.mPoints.Count];
                        for (int i = 0; i < rail.mPoints.Count; i++)
                        {
                            Vector3 p = rail.mPoints[i].mTranslate;
                            points[i] = WorldToScreen(new(p.X, p.Y, p.Z));
                        }
                        return points;
                    }

                    bool isSelected = mEditContext.IsSelected(rail);
                    bool hovered = LevelViewport.HitTestLineLoopPoint(GetPoints(), 10f, ImGui.GetMousePos());

                    CourseRail.CourseRailPoint selectedPoint = null;

                    foreach (var point in rail.mPoints)
                    {
                        var pos2D = this.WorldToScreen(new(point.mTranslate.X, point.mTranslate.Y, point.mTranslate.Z));
                        Vector2 pnt = new(pos2D.X, pos2D.Y);
                        bool isHovered = (ImGui.GetMousePos() - pnt).Length() < 6.0f;
                        if (isHovered)
                            newHoveredObject = point;

                        bool selected = mEditContext.IsSelected(point);
                        if (selected)
                            selectedPoint = point;
                    }

                    //Delete selected
                    if (selectedPoint != null && ImGui.IsKeyPressed(ImGuiKey.Delete))
                    {
                        rail.mPoints.Remove(selectedPoint);
                    }
                    if (selectedPoint != null && ImGui.IsMouseReleased(0))
                    {
                        //Check if point matches an existing point, remove if intersected
                        var matching = rail.mPoints.Where(x => x.mTranslate == selectedPoint.mTranslate).ToList();
                        if (matching.Count > 1)
                            rail.mPoints.Remove(selectedPoint);
                    }

                    bool add_point = ImGui.IsMouseClicked(0) && ImGui.IsMouseDown(0) && ImGui.GetIO().KeyAlt;

                    //Insert point to selected
                    if (selectedPoint != null && add_point)
                    {
                        Vector3 posVec = this.ScreenToWorld(ImGui.GetMousePos());

                        var index = rail.mPoints.IndexOf(selectedPoint);
                        var newPoint = new CourseRail.CourseRailPoint(selectedPoint);
                        newPoint.mTranslate = new(
                             MathF.Round(posVec.X, MidpointRounding.AwayFromZero),
                             MathF.Round(posVec.Y, MidpointRounding.AwayFromZero),
                             selectedPoint.mTranslate.Z);

                        if (rail.mPoints.Count - 1 == index)
                            rail.mPoints.Add(newPoint);
                        else
                            rail.mPoints.Insert(index, newPoint);

                        this.mEditContext.DeselectAll();
                        this.mEditContext.Select(newPoint);
                        newHoveredObject = newPoint;
                    }
                    else if (rail_selected && add_point)
                    {
                        Vector3 posVec = this.ScreenToWorld(ImGui.GetMousePos());

                        var newPoint = new CourseRail.CourseRailPoint();
                        newPoint.mTranslate = new(
                             MathF.Round(posVec.X, MidpointRounding.AwayFromZero),
                             MathF.Round(posVec.Y, MidpointRounding.AwayFromZero),
                             0);

                        rail.mPoints.Add(newPoint);

                        this.mEditContext.DeselectAll();
                        this.mEditContext.Select(newPoint);
                        newHoveredObject = newPoint;
                    }

                    //Rail selection disabled for now as it conflicts with point selection
                    // if (hovered)
                    //   newHoveredObject = rail;
                }

                foreach (CourseRail rail in mArea.mRailHolder.mRails)
                {
                    if (rail.mPoints.Count == 0)
                        continue;

                    bool selected = mEditContext.IsSelected(rail);
                    var rail_color = selected ? ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) : color;

                    List<Vector2> pointsList = [];
                    foreach (CourseRail.CourseRailPoint pnt in rail.mPoints)
                    {
                        bool point_selected = mEditContext.IsSelected(pnt);
                        var rail_point_color = point_selected ? ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) : color;
                        var size = newHoveredObject == pnt ? pointSize * 1.5f : pointSize;

                        var pos2D = WorldToScreen(pnt.mTranslate);
                        mDrawList.AddCircleFilled(pos2D, size, rail_point_color);
                        pointsList.Add(pos2D);
                    }

                    var segmentCount = rail.mPoints.Count;
                    if (!rail.mIsClosed)
                        segmentCount--;

                    mDrawList.PathLineTo(WorldToScreen(rail.mPoints[0].mTranslate));
                    for (int i = 0; i < segmentCount; i++)
                    {
                        var pointA = rail.mPoints[i];
                        var pointB = rail.mPoints[(i + 1) % rail.mPoints.Count];

                        var posA2D = WorldToScreen(pointA.mTranslate);
                        var posB2D = WorldToScreen(pointB.mTranslate);

                        Vector2 cpOutA2D = posA2D;
                        Vector2 cpInB2D = posB2D;

                        if (pointA.mControl.TryGetValue(out Vector3 control))
                            cpOutA2D = WorldToScreen(control);

                        if (pointB.mControl.TryGetValue(out control))
                            //invert control point
                            cpInB2D = WorldToScreen(pointB.mTranslate - (control - pointB.mTranslate));

                        if (cpOutA2D == posA2D && cpInB2D == posB2D)
                        {
                            mDrawList.PathLineTo(posB2D);
                            continue;
                        }

                        mDrawList.PathBezierCubicCurveTo(cpOutA2D, cpInB2D, posB2D);
                    }

                    float thickness = newHoveredObject == rail ? 3f : 2.5f;

                    mDrawList.PathStroke(rail_color, ImDrawFlags.None, thickness);
                }
            }

            foreach (CourseActor actor in mEditContext.GetActorHolder().GetActors())
            {
                string layer = actor.mLayer;

                if (mLayersVisibility!.TryGetValue(layer, out bool isVisible) && isVisible)
                {
                    Matrix4x4 transform =
                        Matrix4x4.CreateScale(actor.mScale.X, actor.mScale.Y, actor.mScale.Z
                        ) *
                        Matrix4x4.CreateRotationZ(
                            actor.mRotation.Z
                        ) *
                        Matrix4x4.CreateTranslation(
                            actor.mTranslation.X,
                            actor.mTranslation.Y,
                            actor.mTranslation.Z
                        ); ;

                    uint color = ImGui.ColorConvertFloat4ToU32(new(0.5f, 1, 0, 1));

                    if (actor.mActorPack != null)
                    {
                        //Choose a less distracting box visual for models
                        if (!string.IsNullOrEmpty(actor.mActorPack.GetModelFileName()))
                            color = ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 0.5f));
                    }

                    if (actor.mActorName.Contains("CameraArea") || actor.mActorPack?.Category == "AreaObj")
                    {
                        if (actor.mActorName.Contains("CameraArea"))
                            color = ImGui.ColorConvertFloat4ToU32(new(1, 0, 0, 1));

                        //topLeft
                        s_actorRectPolygon[0] = WorldToScreen(Vector3.Transform(new(-0.5f, 1f, 0), transform));
                        //topRight
                        s_actorRectPolygon[1] = WorldToScreen(Vector3.Transform(new(0.5f, 1f, 0), transform));
                        //bottomRight
                        s_actorRectPolygon[2] = WorldToScreen(Vector3.Transform(new(0.5f, 0, 0), transform));
                        //bottomLeft
                        s_actorRectPolygon[3] = WorldToScreen(Vector3.Transform(new(-0.5f, 0, 0), transform));
                    }
                    else
                    {
                        //topLeft
                        s_actorRectPolygon[0] = WorldToScreen(Vector3.Transform(new(-0.5f, 0.5f, 0), transform));
                        //topRight
                        s_actorRectPolygon[1] = WorldToScreen(Vector3.Transform(new(0.5f, 0.5f, 0), transform));
                        //bottomRight
                        s_actorRectPolygon[2] = WorldToScreen(Vector3.Transform(new(0.5f, -0.5f, 0), transform));
                        //bottomLeft
                        s_actorRectPolygon[3] = WorldToScreen(Vector3.Transform(new(-0.5f, -0.5f, 0), transform));
                    }

                    if (mEditContext.IsSelected(actor))
                    {
                        color = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));
                    }

                    bool isHovered = mHoveredObject == actor;

                    for (int i = 0; i < 4; i++)
                    {
                        if (mEditContext.IsSelected(actor))
                        {
                            mDrawList.AddCircleFilled(s_actorRectPolygon[i],
                                pointSize, color);
                        }
                        mDrawList.AddLine(
                            s_actorRectPolygon[i],
                            s_actorRectPolygon[(i + 1) % 4],
                            color, isHovered ? 2.5f : 1.5f);
                    }

                    string name = actor.mActorName;

                    isHovered = HitTestConvexPolygonPoint(s_actorRectPolygon, ImGui.GetMousePos());

                    if (name.Contains("Area"))
                    {
                        isHovered = HitTestLineLoopPoint(s_actorRectPolygon, 4f,
                            ImGui.GetMousePos());
                    }

                    if (isHovered)
                    {
                        newHoveredObject = actor;
                    }
                }
            }

            mHoveredObject = newHoveredObject;
        }

        /// <summary>
        /// Does a collision check between a convex polygon and a point
        /// </summary>
        /// <param name="polygon">Points of Polygon a in Clockwise orientation (in screen coordinates)</param>
        /// <param name="point">Point</param>
        /// <returns></returns>
        public static bool HitTestConvexPolygonPoint(ReadOnlySpan<Vector2> polygon, Vector2 point)
        {
            // separating axis theorem (lite)
            // we can view the point as a polygon with 0 sides and 1 point
            for (int i = 0; i < polygon.Length; i++)
            {
                var p1 = polygon[i];
                var p2 = polygon[(i + 1) % polygon.Length];
                var vec = (p2 - p1);
                var normal = new Vector2(vec.Y, -vec.X);

                (Vector2 origin, Vector2 normal) edge = (p1, normal);

                if (Vector2.Dot(point - edge.origin, edge.normal) >= 0)
                    return false;
            }

            //no separating axis found -> collision
            return true;
        }

        /// <summary>
        /// Does a collision check between a LineLoop and a point
        /// </summary>
        /// <param name="polygon">Points of a LineLoop</param>
        /// <param name="point">Point</param>
        /// <returns></returns>
        public static bool HitTestLineLoopPoint(ReadOnlySpan<Vector2> points, float thickness, Vector2 point)
        {
            for (int i = 0; i < points.Length; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Length];
                if (HitTestPointLine(point,
                    p1, p2, thickness))
                    return true;
            }

            return false;
        }

        static bool HitTestPointLine(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Math.Clamp(Vector2.Dot(pa, ba) /
                      Vector2.Dot(ba, ba), 0, 1);
            return (pa - ba * h).Length() < thickness / 2;
        }
    }

    static class ColorExtensions
    {
        public static uint ToAbgr(this System.Drawing.Color c) => (uint)(
            c.A << 24 |
            c.B << 16 |
            c.G << 8 |
            c.R);
    }
}
