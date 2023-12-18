using Fushigi.actor_pack.components;
using Fushigi.Byml.Serializer;
using Fushigi.course;
using Fushigi.course.distance_view;
using Fushigi.gl;
using Fushigi.gl.Bfres;
using Fushigi.gl.Bfres.AreaData;
using Fushigi.param;
using Fushigi.util;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Numerics;
using System.Runtime.CompilerServices;
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
            if (ImGui.GetIO().KeyShift || ImGui.GetIO().KeyCtrl)
            {
                ctx.Select(selectable);
            }
            else  if(!ctx.IsSelected(selectable))
            {
                ctx.WithSuspendUpdateDo(() =>
                {
                    ctx.DeselectAll();
                    ctx.Select(selectable);
                });
            }
            foreach(CourseActor act in ctx.GetSelectedObjects<CourseActor>())
            {
                act.mStartingTrans = act.mTranslation;
            }
        }
    }

    interface ITransformableObject
    {
        Transform Transform { get; }
    }

    [Flags]
    enum KeyboardModifier
    {
        None = 0,
        Shift = 1,
        CtrlCmd = 2,
        Alt = 4
    }

    internal class LevelViewport(CourseArea area, GL gl, CourseAreaScene areaScene)
    {
        public void PreventFurtherRendering() => mIsNoMoreRendering = true;
        private bool mIsNoMoreRendering = false;

        public event Action<IReadOnlyList<object>>? ObjectDeletionRequested;

        readonly CourseArea mArea = area;

        //this is only so BgUnitRail works, TODO make private
        private readonly CourseAreaEditContext mEditContext = areaScene.EditContext;

        ImDrawListPtr mDrawList;
        public EditorMode mEditorMode = EditorMode.Actors;

        public bool IsViewportHovered;
        public bool IsViewportActive;
        public bool IsWonderView;
        public bool PlayAnimations = false;
        public bool ShowGrid = true;

        Vector2 mSize = Vector2.Zero;
      
        public ulong prevSelectVersion { get; private set; } = 0;
        private IDictionary<string, bool>? mLayersVisibility;
        Vector2 mTopLeft = Vector2.Zero;

        public Camera Camera = new Camera();
        public GLFramebuffer Framebuffer; //Draws opengl data into the viewport
        public HDRScreenBuffer HDRScreenBuffer = new HDRScreenBuffer();
        public TileBfresRender TileBfresRenderFieldA;
        public TileBfresRender TileBfresRenderFieldB;
        public AreaResourceManager EnvironmentData = new AreaResourceManager(gl, area.mInitEnvPalette);

        DistantViewManager DistantViewScrollManager = new DistantViewManager(area);

        //TODO make this an ISceneObject? as soon as there's a SceneObj class for each course object
        private object? mHoveredObject;

        public static uint GridColor = 0x77_FF_FF_FF;
        public static float GridLineThickness = 1.5f;

        private (string message, Predicate<object?> predicate,
            TaskCompletionSource<(object? picked, KeyboardModifier modifiers)> promise)? 
            mObjectPickingRequest = null;
        private (string message, string layer, TaskCompletionSource<(Vector3? picked, KeyboardModifier modifiers)> promise)? 
            mPositionPickingRequest = null;

        public enum EditorMode
        {
            Actors,
            Units
        }

        public Task<(object? picked, KeyboardModifier modifiers)> PickObject(string tooltipMessage, 
            Predicate<object?> predicate)
        {
            CancelOngoingPickingRequests();
            var promise = new TaskCompletionSource<(object? picked, KeyboardModifier modifiers)>();
            mObjectPickingRequest = (tooltipMessage, predicate, promise);
            return promise.Task;
        }

        public Task<(Vector3? picked, KeyboardModifier modifiers)> PickPosition(string tooltipMessage, string layer)
        {
            CancelOngoingPickingRequests();
            var promise = new TaskCompletionSource<(Vector3? picked, KeyboardModifier modifiers)>();
            mPositionPickingRequest = (tooltipMessage, layer, promise);
            return promise.Task;
        }

        private void CancelOngoingPickingRequests()
        {
            if (mObjectPickingRequest.TryGetValue(out var objectPickingRequest))
            {
                objectPickingRequest.promise.SetCanceled();
                mObjectPickingRequest = null;
            }
            if (mPositionPickingRequest.TryGetValue(out var positionPickingRequest))
            {
                positionPickingRequest.promise.SetCanceled();
                mPositionPickingRequest = null;
            }
        }

        public bool IsHovered(ISceneObject obj) => mHoveredObject == obj;

        public Matrix4x4 GetCameraMatrix() => Camera.ViewProjectionMatrix;

        public Vector2 GetCameraSizeIn2DWorldSpace() 
        {
            var cameraBoundsSize = ScreenToWorld(mSize) - ScreenToWorld(new Vector2(0));
            return new Vector2(cameraBoundsSize.X, Math.Abs(cameraBoundsSize.Y));
        }

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
            if (ImGui.GetIO().KeyShift || ImGui.GetIO().KeyCtrl)
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
            bool isPanGesture = ImGui.IsMouseDragging(ImGuiMouseButton.Middle) ||
                (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.GetIO().KeyShift && !mEditContext.IsAnySelected<CourseActor>());

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
            if(mIsNoMoreRendering) 
                goto SKIP_RENDERING; //sue me

            mLayersVisibility = layersVisibility;

            if (Framebuffer == null)
                Framebuffer = new GLFramebuffer(gl, FramebufferTarget.Framebuffer, (uint)size.X, (uint)size.Y);

            //Resize if needed
            if (Framebuffer.Width != (uint)size.X || Framebuffer.Height != (uint)size.Y)
                Framebuffer.Resize((uint)size.X, (uint)size.Y);

            RenderStats.Reset();

            //Wonder shader system params
            if (PlayAnimations)
                WonderGameShader.UpdateSystem();

            //Background calculations
            EnvironmentData.UpdateBackground(gl, this.Camera);

            //Render viewport settings for game shaders
            GsysShaderRender.GsysResources.UpdateViewport(this.Camera);
            //Setup light map resources for the currently loaded area
            GsysShaderRender.GsysResources.Lightmaps = EnvironmentData.Lightmaps;
            //Distance view scrol calculations
            DistantViewScrollManager.Calc(this.Camera.Target);
            //Set active area for getting env settings by the materials
            AreaResourceManager.ActiveArea = this.EnvironmentData;

            Framebuffer.Bind();

            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, Framebuffer.Width, Framebuffer.Height);

            gl.Enable(EnableCap.DepthTest);

            //Start drawing the scene. Bfres draw upside down so flip the viewport clip
            gl.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);

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

                foreach (var courseUnit in this.mArea.mUnitHolder.mUnits.Where(x => x.mSkinDivision == division))
                {
                    courseUnit.TilesUpdated += delegate
                    {
                        render.Load(this.mArea.mUnitHolder, this.Camera);
                    };
                }

                return render;
            }
            string? fieldASkin = mArea.mAreaParams.SkinParam?.FieldA;
            string? fieldBSkin = mArea.mAreaParams.SkinParam?.FieldB;

            if (TileBfresRenderFieldA == null && !string.IsNullOrEmpty(fieldASkin))
                TileBfresRenderFieldA = CreateTileRendererForSkin(SkinDivision.FieldA, fieldASkin);

            if (TileBfresRenderFieldB == null && !string.IsNullOrEmpty(fieldBSkin))
                TileBfresRenderFieldB = CreateTileRendererForSkin(SkinDivision.FieldB, fieldBSkin);

            TileBfresRenderFieldA?.Render(gl, this.Camera);
            TileBfresRenderFieldB?.Render(gl, this.Camera);

            //Display skybox
            EnvironmentData.RenderSky(gl, this.Camera);

            foreach (var actor in this.mArea.GetActors())
            {
                if (actor.mActorPack == null || mLayersVisibility.ContainsKey(actor.mLayer) && !mLayersVisibility[actor.mLayer])
                    continue;

                RenderActor(actor, actor.mActorPack.ModelInfoRef);
                RenderActor(actor, actor.mActorPack.DrawArrayModelInfoRef);
            }

            //Reset back to defaults
            gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

            Framebuffer.Unbind();

            //Draw final output in post buffer
            HDRScreenBuffer.Render(gl, (int)size.X, (int)size.Y, (GLTexture2D)Framebuffer.Attachments[0]);

            Framebuffer.Unbind();

        SKIP_RENDERING:
            //Draw framebuffer
            ImGui.SetCursorScreenPos(mTopLeft);
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
                    
            var debugSMat = Matrix4x4.CreateScale(modelInfo.mModelScale != default ? modelInfo.mModelScale:Vector3.One);

            var mat = debugSMat * scaleMat * rotMat * transMat;

            DistantViewScrollManager.UpdateMatrix(actor.mLayer, ref mat);

            var model = render.Models[modelName];

            if(actor.mActorPack.ModelExpandParamRef != null)
            {
                ActorModelExpand(actor, model);

                //TODO SubModels
            }
            //switch for drawing models with different methods easier
            if (actor.mActorPack.DrainPipeRef != null && actor.mActorPack.DrainPipeRef.ModelKeyTop != null &&
            actor.mActorPack.DrainPipeRef.ModelKeyMiddle != null)
            {
                var drainRef = actor.mActorPack.DrainPipeRef;
                var calc = actor.mActorPack.ShapeParams.mCalc;
                var KeyMats = new Dictionary<string, Matrix4x4>{
                    {drainRef.ModelKeyTop ?? "Top", debugSMat *
                        Matrix4x4.CreateScale(actor.mScale.X, actor.mScale.X, actor.mScale.Z) * 
                        Matrix4x4.CreateTranslation(0, (actor.mScale.Y-actor.mScale.X)*(calc.mMax.Y-calc.mMin.Y), 0) *
                        rotMat *
                        transMat},

                    {drainRef.ModelKeyMiddle ?? "Middle", debugSMat *
                        Matrix4x4.CreateScale(actor.mScale.X, (actor.mScale.Y-1)*2, actor.mScale.Z) * 
                        rotMat *
                        transMat}};

                model.Render(gl, render, KeyMats[modelInfo.SearchModelKey], this.Camera);
                if ((modelInfo.SubModels?.Count ?? 0) != 0)
                    render.Models[modelInfo.SubModels[0].FmdbName].Render(gl, render, KeyMats[modelInfo.SubModels[0].SearchModelKey], this.Camera);
            }
            else
            {
                model.Render(gl, render, mat, this.Camera);
            }
        }
        public Vector2 ExpandCalcTypes(string type, Vector2 actScale)
        {
            var result = type switch
            {
                "ActorScale" => actScale,
                "ActorScaleMinus1" => actScale - Vector2.One,
                "ActorScaleMinus2" => actScale - new Vector2(2),
                "ActorScaleDiv2" => actScale/2,
                "ActorScaleDiv4" => actScale/4,
                "ZeroWhenActorScaleOne" => new Vector2(actScale.X == 1 ? 0:1, actScale.Y == 1 ? 0:1),
                "None" => Vector2.One,
                _ => actScale
            };
            return result;
        }

        public Vector2 ExpandScaleTypes(string type, Vector2 scale)
        {
            var result = type switch
            {
                "XAxisOnly" => new (scale.X, 1),
                "YAxisOnly" => new (1, scale.Y),
                "XYAxis" => scale,
                _ => scale
            };
            return result;
        }
        private void ActorModelExpand(CourseActor actor, BfresRender.BfresModel model, string modelKeyName = "")
        {
            //Model Expand Param

            Debug.Assert(actor.mActorPack.ModelExpandParamRef.Settings.Count > 0);

            if (actor.mActorPack.ModelExpandParamRef.Settings.Count == 0)
                return;

            //TODO is that actually how the game does it?
            var setting = actor.mActorPack.ModelExpandParamRef.Settings.FindLast(x=>x.mModelKeyName == modelKeyName);

            //Debug.Assert(setting != null);
            if (setting == null) 
                return;

            var clampedActorScale = new Vector2(
                Math.Max(actor.mScale.X, setting.mMinScale.X),
                Math.Max(actor.mScale.Y, setting.mMinScale.Y)
            );

            Dictionary<string, Vector3> boneScaleLookup = [];

            foreach (var boneParam in setting.mBoneSetting.BoneInfoList)
            {
                Vector2 boneScale;
                if (boneParam.mIsCustomCalc)
                {
                    float a = boneParam.mCustomCalc.A;
                    float b = boneParam.mCustomCalc.B == 0 ? 1:boneParam.mCustomCalc.B;
                    boneScale = (clampedActorScale - new Vector2(a)) / b;
                }
                else
                {
                    boneScale = ExpandCalcTypes(boneParam.mCalcType, clampedActorScale);
                }

                boneScale = ExpandScaleTypes(boneParam.mScalingType, boneScale);

                boneScale.X = Math.Max(boneScale.X, 0);
                boneScale.Y = Math.Max(boneScale.Y, 0);

                boneScaleLookup[boneParam.mBoneName] = new Vector3(boneScale, 1);
            }

            // foreach (var matParam in setting.mMatSetting.MatInfoList)
            // {
            //     var calc = clampedActorScale;
            //     if (matParam.mIsCustomCalc)
            //     {
            //         calc = new Vector2(
            //             Math.Max(actor.mScale.X, matParam.mCustomCalc.A),
            //             Math.Max(actor.mScale.Y, matParam.mCustomCalc.B)
            //         );
            //     }

            //     var mat = ExpandCalcTypes(matParam.mCalcType, calc);
            //     mat = ExpandScaleTypes(matParam.mScalingType, mat);
                
            //     boneScaleLookup[matParam.mMatName] = (
            //         new Vector3(Math.Max(mat.X, 0), Math.Max(mat.Y, 0), 1), 
            //         new Vector3(1/mat.X, 1/mat.Y, 1)
            //     );
            // }

            var rootMatrix = Matrix4x4.CreateScale(
                1 / actor.mScale.X,
                1 / actor.mScale.Y,
                1
                );

            model.Skeleton.Bones[0].WorldMatrix = rootMatrix;

            var nonScaledMatrices = new Matrix4x4[model.Skeleton.Bones.Count];
            nonScaledMatrices[0] = rootMatrix;


            for (int i = 1; i < model.Skeleton.Bones.Count; i++)
            {
                var bone = model.Skeleton.Bones[i];
                bone.WorldMatrix = bone.CalculateLocalMatrix();

                var parent = model.Skeleton.Bones[bone.ParentIndex];

                Vector3 scale;
                if (boneScaleLookup.TryGetValue(parent.Name ?? "", out scale))
                {
                    bone.WorldMatrix.Translation *= scale;
                }

                bone.WorldMatrix *= nonScaledMatrices[bone.ParentIndex];

                nonScaledMatrices[i] = bone.WorldMatrix;
                if (boneScaleLookup.TryGetValue(bone.Name, out scale))
                {
                    bone.WorldMatrix = Matrix4x4.CreateScale(scale) * bone.WorldMatrix;
                }
            }
        }

        public void Draw(Vector2 size, double deltaSeconds, IDictionary<string, bool> layersVisibility)
        {
            if (size.X * size.Y == 0)
                return;

            mLayersVisibility = layersVisibility;
            mTopLeft = ImGui.GetCursorScreenPos();

            ImGui.InvisibleButton("canvas", size,
                ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);

            IsViewportHovered = ImGui.IsItemHovered();
            IsViewportActive = ImGui.IsItemActive();

            KeyboardModifier modifiers = KeyboardModifier.None;

            if(ImGui.GetIO().KeyShift)
                modifiers |= KeyboardModifier.Shift;
            if(ImGui.GetIO().KeyAlt)
                modifiers |= KeyboardModifier.Alt;
            if (OperatingSystem.IsMacOS() ? ImGui.GetIO().KeySuper : ImGui.GetIO().KeyCtrl)
                modifiers |= KeyboardModifier.CtrlCmd;

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

            if (ShowGrid)
                DrawGrid();
            DrawAreaContent();

            if (!IsViewportHovered)
                mHoveredObject = null;

            CourseActor? hoveredActor = mHoveredObject as CourseActor;

            if (hoveredActor != null && 
                mObjectPickingRequest == null && mPositionPickingRequest == null) //prevents tooltip flickering
                ImGui.SetTooltip($"{hoveredActor.mPackName}");

            if (ImGui.IsKeyPressed(ImGuiKey.Z) && modifiers == KeyboardModifier.CtrlCmd)
            {
                mEditContext.Undo();
            }
            if ((ImGui.IsKeyPressed(ImGuiKey.Y) && modifiers == KeyboardModifier.CtrlCmd) ||
                (ImGui.IsKeyPressed(ImGuiKey.Z) && modifiers == (KeyboardModifier.Shift | KeyboardModifier.CtrlCmd)))
            {
                mEditContext.Redo();
            }



            if (ImGui.IsWindowFocused())
                InteractionWithFocus(modifiers);

            ImGui.PopClipRect();
        }

        void InteractionWithFocus(KeyboardModifier modifiers)
        {
            if (IsViewportHovered &&
                mObjectPickingRequest.TryGetValue(out var objectPickingRequest))
            {
                bool isValid = objectPickingRequest.predicate(mHoveredObject);

                string currentlyHoveredObjText = "";
                if (isValid && mHoveredObject is CourseActor hoveredActor)
                    currentlyHoveredObjText = $"\n\nCurrently Hovered: {hoveredActor.mPackName}";

                ImGui.SetTooltip(objectPickingRequest.message + "\nPress Escape to cancel" + 
                    currentlyHoveredObjText);
                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    mObjectPickingRequest = null;
                    objectPickingRequest.promise.SetResult((null, modifiers));
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) &&
                    isValid)
                {
                    mObjectPickingRequest = null;
                    objectPickingRequest.promise.SetResult((mHoveredObject, modifiers));
                }

                return;
            }
            if (IsViewportHovered && 
                mPositionPickingRequest.TryGetValue(out var positionPickingRequest))
            {
                ImGui.SetTooltip(positionPickingRequest.message + "\nPress Escape to cancel");
                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    mPositionPickingRequest = null;
                    positionPickingRequest.promise.SetResult((null, modifiers));
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    //TODO use positionPickingRequest.layer
                    mPositionPickingRequest = null;
                    positionPickingRequest.promise.SetResult((ScreenToWorld(ImGui.GetMousePos()), modifiers));
                }

                return;
            }
          
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (mEditContext.IsAnySelected<CourseActor>())
                {
                    foreach(CourseActor actor in mEditContext.GetSelectedObjects<CourseActor>())
                    {
                        Vector3 posVec = ScreenToWorld(ImGui.GetMousePos());
                        posVec -= ScreenToWorld(ImGui.GetIO().MouseClickedPos[0]) - actor.mStartingTrans;

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
                /* if the user clicked somewhere and it was not hovered over an element, 
                    * we clear our selected actors array */
                if (mHoveredObject == null)
                {
                    if(!ImGui.IsKeyDown(ImGuiKey.LeftShift))
                        mEditContext.DeselectAll();
                }
                else if (mHoveredObject is IViewportSelectable obj)
                {
                    prevSelectVersion = mEditContext.SelectionVersion;
                    obj.OnSelect(mEditContext);
                }
                else
                {
                    //TODO remove this once all course objects have IViewportSelectable SceneObjs
                    prevSelectVersion = mEditContext.SelectionVersion;
                    IViewportSelectable.DefaultSelect(mEditContext, mHoveredObject);
                }
            }

            if(mHoveredObject != null && mHoveredObject is CourseActor &&
            ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                if (ImGui.GetIO().MouseDragMaxDistanceSqr[0] <= ImGui.GetIO().MouseDragThreshold)
                {
                    if(ImGui.IsKeyDown(ImGuiKey.LeftShift)
                    && prevSelectVersion == mEditContext.SelectionVersion)
                    {
                        mEditContext.Deselect(mHoveredObject!);
                    }
                    else if(!ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        mEditContext.DeselectAll();
                        IViewportSelectable.DefaultSelect(mEditContext, mHoveredObject);
                    }
                }
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Delete))
                ObjectDeletionRequested?.Invoke(mEditContext.GetSelectedObjects<CourseActor>().ToList());

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                mEditContext.DeselectAll();
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

            areaScene.ForEach<IViewportDrawable>(obj =>
            {
                bool isNewHoveredObj = false;
                obj.Draw2D(mEditContext, this, mDrawList, ref isNewHoveredObj);
                if (isNewHoveredObj)
                    newHoveredObject = obj;
            });

            if (mArea.mRailHolder.mRails.Count > 0)
            {
                uint color = Color.HotPink.ToAbgr();

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

            foreach (CourseActor actor in mArea.GetActors())
            {
                Vector3 min = new(-.5f);
                Vector3 max = new(.5f);
                Vector3 off = new(0f);
                Vector3 center = new(0f);
                var drawing = "box";

                if (actor.mActorPack.ShapeParams != null)
                {
                    var shapes = actor.mActorPack.ShapeParams;
                    var calc = shapes.mCalc;

                    if(((shapes.mSphere?.Count ??  0) > 0) ||
                        ((shapes.mCapsule?.Count ?? 0) > 0))
                    { 
                        drawing = "sphere";
                    }
                    else if ((shapes.mPoly?.Count ??  0) > 0)
                    { 
                        calc = shapes.mPoly[0].mCalc;
                    }
                    
                    min = calc.mMin;
                    max = calc.mMax;
                    center = calc.mCenter;
                }
                    
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

                    if (actor.mPackName.Contains("CameraArea") || actor.mActorPack?.Category == "AreaObj")
                    {
                        if (actor.mPackName.Contains("CameraArea"))
                            color = ImGui.ColorConvertFloat4ToU32(new(1, 0, 0, 1));
                            
                        off = new(0, .5f, 0);
                    }
                    //topLeft
                    s_actorRectPolygon[0] = WorldToScreen(Vector3.Transform(new Vector3(min.X, max.Y, 0)+off, transform));
                    //topRight
                    s_actorRectPolygon[1] = WorldToScreen(Vector3.Transform(new Vector3(max.X, max.Y, 0)+off, transform));
                    //bottomRight
                    s_actorRectPolygon[2] = WorldToScreen(Vector3.Transform(new Vector3(max.X, min.Y, 0)+off, transform));
                    //bottomLeft
                    s_actorRectPolygon[3] = WorldToScreen(Vector3.Transform(new Vector3(min.X, min.Y, 0)+off, transform));

                    if (mEditContext.IsSelected(actor))
                    {
                        color = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));
                    }

                    bool isHovered = mHoveredObject == actor;

                    switch(drawing)
                    {
                        default:
                            for (int i = 0; i < 4; i++)
                            {
                                mDrawList.AddLine(
                                s_actorRectPolygon[i],
                                s_actorRectPolygon[(i+1) % 4 ],
                                color, isHovered ? 2.5f : 1.5f);
                            }
                            break;
                        case "sphere": 
                            var pos = WorldToScreen(Vector3.Transform(center, transform));
                            var scale = Matrix4x4.CreateScale(actor.mScale);
                            Vector2 rad = (WorldToScreen(Vector3.Transform(max, scale))-WorldToScreen(Vector3.Transform(min, scale)))/2;
                            mDrawList.AddEllipse(pos, Math.Abs(rad.X), Math.Abs(rad.Y), color, -actor.mRotation.Z, 0, isHovered ? 2.5f : 1.5f);
                            
                            break;
                    }
                    if (mEditContext.IsSelected(actor))
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            mDrawList.AddCircleFilled(s_actorRectPolygon[i],
                                pointSize, color);
                            if(drawing == "sphere")
                            {
                                mDrawList.AddLine(
                                s_actorRectPolygon[i],
                                s_actorRectPolygon[(i+1) % 4 ],
                                color, isHovered ? 2.5f : 1.5f);
                            }
                        }
                        mDrawList.AddEllipse(WorldToScreen(transform.Translation), pointSize*3, pointSize*3, color, -actor.mRotation.Z, 4, 2);
                    }

                    string name = actor.mPackName;

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
        public static uint ToAbgr(this Color c) => (uint)(
            c.A << 24 |
            c.B << 16 |
            c.G << 8 |
            c.R);
    }
}
