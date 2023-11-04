using Fushigi.Byml;
using Fushigi.course;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Vector3 = System.Numerics.Vector3;
using static Fushigi.course.CourseUnit;
using System.Xml.Linq;
using System.Reflection;

namespace Fushigi.ui.widgets
{
    internal class LevelViewport(CourseArea area)
    {
        readonly CourseArea mArea = area;
        Matrix4x4 mViewProjectionMatrix;
        Matrix4x4 mViewProjectionMatrixInverse;
        ImDrawListPtr mDrawList;
        public EditorMode mEditorMode = EditorMode.Actors;
        public EditorState mEditorState = EditorState.Selecting;

        Vector2 mSize = Vector2.Zero;
        private ISet<CourseActor> mSelectedActors = new HashSet<CourseActor>();
        private Vector3? mSelectedPoint;
        private int mWallIdx = -1;
        private int mUnitIdx = -1;
        private int mPointIdx = -1;
        private IDictionary<string, bool>? mLayersVisibility;
        Vector2 mTopLeft = Vector2.Zero;
        public string mActorToAdd = "";

        public float FOV = MathF.PI / 2;

        public (Quaternion rotation, Vector3 target, float distance) Camera = 
            (Quaternion.Identity, Vector3.Zero, 10);

        public CourseActor? HoveredActor;
        public Vector3? HoveredPoint;

        public uint GridColor = 0x77_FF_FF_FF;
        public float GridLineThickness = 1.5f;

        bool mSelectionChanged = false;

        public enum EditorState
        {
            Selecting,
            AddingActor,
            DeletingActor
        }

        public enum EditorMode
        {
            Actors,
            Units
        }

        public Vector2 WorldToScreen(Vector3 pos) => WorldToScreen(pos, out _);
        public Vector2 WorldToScreen(Vector3 pos, out float ndcDepth)
        {
            var ndc = Vector4.Transform(pos, mViewProjectionMatrix);
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

            var world = Vector4.Transform(ndc, mViewProjectionMatrixInverse);
            world /= world.W;

            return new (world.X, world.Y, world.Z);
        }

        public void FrameSelectedActor(CourseActor actor)
        {
            this.Camera.target = new Vector3(actor.mTranslation.X, actor.mTranslation.Y, 0);
        }

        public void SelectedActor(CourseActor actor)
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift))
            {
                mSelectedActors.Add(actor);
                mSelectionChanged = true;
            }
            else
            {
                mSelectedActors.Clear();
                mSelectedActors.Add(actor);
                mSelectionChanged = true;
            }
        }

        public void HandleCameraControls(bool mouseHover, bool mouseActive)
        {
            bool isPanGesture = (ImGui.IsMouseDragging(ImGuiMouseButton.Middle)) ||
                (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.GetIO().KeyAlt);

            if (mouseActive && isPanGesture)
            {
                Camera.target += ScreenToWorld(ImGui.GetMousePos() - ImGui.GetIO().MouseDelta) -
                    ScreenToWorld(ImGui.GetMousePos());
            }

            if (mouseHover)
            {
                Camera.distance *= MathF.Pow(2, -ImGui.GetIO().MouseWheel / 10);

                if (ImGui.IsKeyDown(ImGuiKey.LeftArrow))
                {
                    Camera.target.X -= 0.25f;
                }

                if (ImGui.IsKeyDown(ImGuiKey.RightArrow))
                {
                    Camera.target.X += 0.25f;
                }

                if (ImGui.IsKeyDown(ImGuiKey.UpArrow))
                {
                    Camera.target.Y += 0.25f;
                }

                if (ImGui.IsKeyDown(ImGuiKey.DownArrow))
                {
                    Camera.target.Y -= 0.25f;
                }
            }
        }

        public void Draw(Vector2 size, IDictionary<string, bool> layersVisibility)
        {
            mLayersVisibility = layersVisibility;
            mTopLeft = ImGui.GetCursorScreenPos();

            ImGui.InvisibleButton("canvas", size, 
                ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);

            bool mouseHover = ImGui.IsItemHovered();
            bool mouseActive = ImGui.IsItemActive();

            if (size.X*size.Y == 0) 
                return;

            mSize = size;
            mDrawList = ImGui.GetWindowDrawList();

            ImGui.PushClipRect(mTopLeft, mTopLeft + size, true);

            HandleCameraControls(mouseHover, mouseActive);

            float ratio = mSize.X / mSize.Y;
            {
                float tanFOV = MathF.Tan(FOV / 2);
                mViewProjectionMatrix =
                    Matrix4x4.CreateTranslation(-Camera.target) *
                    Matrix4x4.CreateOrthographic(ratio * tanFOV * Camera.distance, tanFOV * Camera.distance,
                    -1000, 1000);
            }

            if (!Matrix4x4.Invert(mViewProjectionMatrix, out var inv))
                return;

            mViewProjectionMatrixInverse = inv;

            DrawGrid();

            DrawAreaContent();

            if (!mouseHover)
                HoveredActor = null;

            if(HoveredActor != null)
                ImGui.SetTooltip($"{HoveredActor.mActorName}");

            if (mEditorState == EditorState.Selecting)
            {
                if (ImGui.IsItemClicked())
                {
                    bool isModeActor = HoveredActor != null;
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
                    if (HoveredActor == null)
                    {
                        mSelectionChanged = false;
                        mSelectedActors.Clear();
                    }
                    else
                    {
                        if (ImGui.IsKeyDown(ImGuiKey.LeftShift))
                        {
                            mSelectedActors.Add(HoveredActor);
                            mSelectionChanged = true;
                        }
                        else
                        {
                            mSelectedActors.Clear();
                            mSelectedActors.Add(HoveredActor);
                            mSelectionChanged = true;
                        }
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

                if (ImGui.IsKeyDown(ImGuiKey.Delete))
                {
                    mEditorState = EditorState.DeletingActor;
                }

                if (ImGui.IsKeyDown(ImGuiKey.Escape))
                {
                    mSelectedActors.Clear();
                    mSelectedPoint = null;
                }
            }
            else if (mEditorState == EditorState.AddingActor)
            {
                ImGui.SetTooltip($"Placing actor {mActorToAdd} -- Hold SHIFT to place multiple, ESCAPE to cancel.");

                if (ImGui.IsKeyDown(ImGuiKey.Escape))
                {
                    mEditorState = EditorState.Selecting;
                }

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    CourseActor actor = new CourseActor(mActorToAdd, mArea.mRootHash);

                    Vector3 posVec = ScreenToWorld(ImGui.GetMousePos());
                    posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                    posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                    posVec.Z = 0.0f;
                    actor.mTranslation = posVec;

                    mArea.mActorHolder.AddActor(actor);

                    if (!ImGui.GetIO().KeyShift)
                    {
                        mActorToAdd = "";
                        mEditorState = EditorState.Selecting;
                    }
                }
            }
            else if (mEditorState == EditorState.DeletingActor)
            {
                if (mSelectedActors.Count > 0)
                {
                    //TODO if undo/redo is ever implemented make sure this is just one operation
                    foreach (var actor in mSelectedActors)
                        mArea.mActorHolder.DeleteActor(actor);

                    mEditorState = EditorState.Selecting;
                }
                else
                {
                    if(HoveredActor != null)
                        ImGui.SetTooltip($"""
                            Click to delete {HoveredActor.mActorName}.
                            Hold SHIFT to delete multiple actors, ESCAPE to cancel.
                            """);
                    else
                        ImGui.SetTooltip("""
                            Click on any actor to delete it.
                            Hold SHIFT to delete multiple actors, ESCAPE to cancel.
                            """);

                    if (ImGui.IsKeyDown(ImGuiKey.Escape))
                    {
                        mEditorState = EditorState.Selecting;
                    }

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        if (HoveredActor != null)
                        {
                            mArea.mActorHolder.DeleteActor(HoveredActor);

                            if (!ImGui.GetIO().KeyShift)
                            {
                                mEditorState = EditorState.Selecting;
                            }
                        }
                    }
                }
            }

            ImGui.PopClipRect();
        }

        public bool HasSelectionChanged()
        {
            return mSelectionChanged;
        }

        public ISet<CourseActor> GetSelectedActors()
        {
            mSelectionChanged = false;
            return mSelectedActors;
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

                float t = ((min_tick_value + i * tick_interval) - min_value) / (max_value-min_value);

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

            if (mArea.mUnitHolder.mUnits.Count > 0)
            {
                foreach (CourseUnit unit in mArea.mUnitHolder.mUnits)
                {
                    foreach (ExternalRail wallGeometry in unit.mWalls)
                    {
                        if (wallGeometry.mPoints.Count == 0)
                            continue;

                        Vector2[] pointsList = new Vector2[wallGeometry.mPoints.Count];

                        for (int i = 0; i < wallGeometry.mPoints.Count; i++)
                        {
                            Vector3 point = (Vector3)wallGeometry.mPoints[i];
                            var pos2D = WorldToScreen(new(point.X, point.Y, point.Z));
                            Vector2 pnt = new(pos2D.X, pos2D.Y);

                            bool isHovered = HoveredPoint == point;

                            uint color;

                            if (isHovered || mSelectedPoint == point)
                            {
                                color = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));
                            }
                            else
                            {
                                color = 0xFFFFFFFF;
                            }

                            mDrawList.AddCircleFilled(pos2D, 6.0f, color);
                            pointsList[i] = pos2D;

                            if ((ImGui.GetMousePos() - pnt).Length() < 6.0f)
                            {
                                newHoveredPoint = point;
                                mUnitIdx = mArea.mUnitHolder.mUnits.IndexOf(unit);
                                mWallIdx = unit.mWalls.IndexOf(wallGeometry);
                                mPointIdx = i;
                            }
                        }

                        HoveredPoint = newHoveredPoint;

                        mDrawList.AddPolyline(ref pointsList[0], pointsList.Length, 0xFFFFFFFF,
                            wallGeometry.IsClosed ? ImDrawFlags.Closed : ImDrawFlags.None, 2.5f);
                    }
                }
            }

            if (mArea.mRailHolder.mRails.Count > 0) 
            {
                foreach (CourseRail rail in mArea.mRailHolder.mRails)
                {
                    List<Vector2> pointsList = [];

                    foreach (CourseRail.CourseRailPoint pnt in rail.mPoints)
                    {
                        var pos2D = WorldToScreen(new(pnt.mTranslate.X, pnt.mTranslate.Y, pnt.mTranslate.Z));
                        mDrawList.AddCircleFilled(pos2D, pointSize, (uint)System.Drawing.Color.HotPink.ToArgb());
                        pointsList.Add(pos2D);
                    }

                    for (int i = 0; i < pointsList.Count - 1; i++)
                    {
                        mDrawList.AddLine(pointsList[i], pointsList[i + 1], (uint)System.Drawing.Color.HotPink.ToArgb(), 2.5f);
                    }

                    bool isClosed = rail.mIsClosed;

                    if (isClosed)
                    {
                        mDrawList.AddLine(pointsList[pointsList.Count - 1], pointsList[0], (uint)System.Drawing.Color.HotPink.ToArgb(), 2.5f);
                    }
                }
            }

            CourseActor? newHoveredActor = null;

            foreach (CourseActor actor in mArea.mActorHolder.GetActors())
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
                        );;

                    //topLeft
                    s_actorRectPolygon[0] = WorldToScreen(Vector3.Transform(new(-0.5f, 0.5f, 0), transform));
                    //topRight
                    s_actorRectPolygon[1] = WorldToScreen(Vector3.Transform(new(0.5f, 0.5f, 0), transform));
                    //bottomRight
                    s_actorRectPolygon[2] = WorldToScreen(Vector3.Transform(new(0.5f, -0.5f, 0), transform));
                    //bottomLeft
                    s_actorRectPolygon[3] = WorldToScreen(Vector3.Transform(new(-0.5f, -0.5f, 0), transform));

                    bool isHovered = HoveredActor == actor;

                    uint color = ImGui.ColorConvertFloat4ToU32(new(0.5f, 1, 0, 1));

                    if (mSelectedActors.Contains(actor))
                    {
                        color = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        mDrawList.AddCircleFilled(s_actorRectPolygon[i],
                            pointSize, color);
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
                        newHoveredActor = actor;
                    }
                }
            }

            HoveredActor = newHoveredActor;
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
            Vector2 pa = p-a, ba = b-a;
            float h = Math.Clamp( Vector2.Dot(pa,ba)/
                      Vector2.Dot(ba,ba), 0, 1 );
            return ( pa - ba*h ).Length() < thickness/2;
        }
    }
}
