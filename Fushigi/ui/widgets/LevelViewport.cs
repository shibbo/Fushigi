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

namespace Fushigi.ui.widgets
{
    internal class LevelViewport(CourseArea area)
    {
        readonly CourseArea mArea = area;
        Matrix4x4 mViewProjectionMatrix;
        Matrix4x4 mViewProjectionMatrixInverse;
        ImDrawListPtr mDrawList;
        public EditorState mEditorState = EditorState.Picking;

        Vector2 mSize = Vector2.Zero;
        private ISet<BymlHashTable> mSelectedActors = new HashSet<BymlHashTable>();
        private IDictionary<string, bool>? mLayersVisibility;
        Vector2 mTopLeft = Vector2.Zero;
        public string mActorToAdd = "";

        public float FOV = MathF.PI / 2;

        public (Quaternion rotation, Vector3 target, float distance) Camera = 
            (Quaternion.Identity, Vector3.Zero, 10);

        public BymlHashTable? HoveredActor;

        public uint GridColor = 0x77_FF_FF_FF;
        public float GridLineThickness = 1.5f;

        bool mSelectionChanged = false;

        public enum EditorState
        {
            Picking,
            AddingActor,
            DeletingActor
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

        public void HandleCameraControls(bool mouseHover, bool mouseActive)
        {
            if (ImGui.IsWindowFocused())
            {
                Camera.distance *= MathF.Pow(2, -ImGui.GetIO().MouseWheel / 10);

                if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle) && mouseActive)
                {
                    Camera.target += ScreenToWorld(ImGui.GetMousePos() - ImGui.GetIO().MouseDelta) -
                        ScreenToWorld(ImGui.GetMousePos());
                }

                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    if (ImGui.GetIO().KeyAlt)
                    {
                        Camera.target += ScreenToWorld(ImGui.GetMousePos() - ImGui.GetIO().MouseDelta) -
                        ScreenToWorld(ImGui.GetMousePos());
                    }
                }

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

        public void Draw(Vector2 size, IDictionary<string, bool> layersVisibility, ISet<BymlHashTable> selectedActors)
        {
            //mSelectedActors = selectedActors;
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

            if (mEditorState == EditorState.Picking)
            {
                /* if the user clicked somewhere and it was not hovered over an element, we clear our selected actors array */
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (HoveredActor == null)
                    {
                        mSelectionChanged = false;
                        mSelectedActors.Clear();
                    }
                }

                if (ImGui.IsItemClicked())
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

                if (ImGui.IsKeyDown(ImGuiKey.Delete))
                {
                    mEditorState = EditorState.DeletingActor;
                }

                if (ImGui.IsKeyDown(ImGuiKey.Escape))
                {
                    mSelectedActors.Clear();
                }
            }
            else if (mEditorState == EditorState.AddingActor)
            {
                if (ImGui.IsKeyDown(ImGuiKey.Escape))
                {
                    mEditorState = EditorState.Picking;
                }

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    /* for adding actors, we want to avoid making new nodes as much as possible so we want to try copying existing ones with the same name and setting default values */
                    /* so let's find an actor that shares our selected name */
                    BymlHashTable root = (BymlHashTable)mArea.GetRootNode();
                    BymlArrayNode actorArray = (BymlArrayNode)root["Actors"];

                    BymlHashTable? newActor = null;

                    foreach (BymlHashTable actor in actorArray.Array)
                    {
                        string actorName = ((BymlNode<string>)actor["Gyaml"]).Data;

                        if (actorName == mActorToAdd)
                        {
                            newActor = new BymlHashTable(actor);
                            break;
                        }
                    }

                    if (newActor != null)
                    {
                        Vector3 posVec = ScreenToWorld(ImGui.GetMousePos());

                        posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                        posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;

                        var posNode = (BymlArrayNode)newActor["Translate"];
                        var rotNode = (BymlArrayNode)newActor["Rotate"];

                        var bytes = new byte[sizeof(UInt64)];
                        RNGCryptoServiceProvider Gen = new RNGCryptoServiceProvider();
                        Gen.GetBytes(bytes);
                        ulong hash = BitConverter.ToUInt64(bytes, 0);

                        ((BymlBigDataNode<ulong>)newActor["Hash"]).Data = hash;

                        BymlNode<float> rot_x = new BymlNode<float>(BymlNodeId.Float, 0.0f);
                        BymlNode<float> rot_y = new BymlNode<float>(BymlNodeId.Float, 0.0f);
                        BymlNode<float> rot_z = new BymlNode<float>(BymlNodeId.Float, 0.0f);

                        rotNode.SetNodeAtIdx(rot_x, 0);
                        rotNode.SetNodeAtIdx(rot_y, 0);
                        rotNode.SetNodeAtIdx(rot_z, 0);

                        BymlNode<float> x = new BymlNode<float>(BymlNodeId.Float, posVec.X);
                        BymlNode<float> y = new BymlNode<float>(BymlNodeId.Float, posVec.Y);

                        posNode.SetNodeAtIdx(x, 0);
                        posNode.SetNodeAtIdx(y, 1);

                        actorArray.AddNodeToArray(newActor);
                    }

                    mEditorState = EditorState.Picking;
                }
            }
            else if (mEditorState == EditorState.DeletingActor)
            {
                if (mSelectedActors.Count > 0)
                {
                    BymlHashTable root = (BymlHashTable)mArea.GetRootNode();
                    BymlArrayNode actorArray = (BymlArrayNode)root["Actors"];

                    int idx = -1;

                    foreach (BymlHashTable actor in mSelectedActors)
                    {
                        if (actorArray.Array.Contains(actor))
                        {
                            actorArray.Array.Remove(actor);
                        }
                    }

                    mEditorState = EditorState.Picking;
                }
                else
                {
                    // blah blah we wait for user to select actors blah blah
                    mEditorState = EditorState.Picking;
                }
            }


            ImGui.PopClipRect();
        }

        public bool HasSelectionChanged()
        {
            return mSelectionChanged;
        }

        public ISet<BymlHashTable> GetSelectedActors()
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
            const float pointSize = 3;

            var root = (BymlHashTable)mArea.GetRootNode();

            if (root.ContainsKey("BgUnits"))
            {
                //BgUnits are in an array.
                BymlArrayNode bgUnitsArray = (BymlArrayNode)root["BgUnits"];

                foreach (BymlHashTable bgUnit in bgUnitsArray.Array)
                {
                    if (bgUnit.ContainsKey("Walls"))
                    {
                        BymlArrayNode wallsArray = (BymlArrayNode)((BymlHashTable)bgUnit)["Walls"];

                        foreach (BymlHashTable walls in wallsArray.Array)
                        {
                            BymlHashTable externalRail = (BymlHashTable)walls["ExternalRail"];
                            BymlArrayNode pointsArray = (BymlArrayNode)externalRail["Points"];
                            List<Vector2> pointsList = [];
                            foreach (BymlHashTable points in pointsArray.Array)
                            {
                                var pos = (BymlArrayNode)points["Translate"];
                                float x = ((BymlNode<float>)pos[0]).Data;
                                float y = ((BymlNode<float>)pos[1]).Data;
                                float z = ((BymlNode<float>)pos[2]).Data;

                                var pos2D = WorldToScreen(new(x, y, z));
                                mDrawList.AddCircleFilled(
                                    pos2D, pointSize, 0xFFFFFFFF);
                                pointsList.Add(pos2D);
                            }
                            for (int i = 0; i < pointsList.Count - 1; i++)
                            {
                                mDrawList.AddLine(pointsList[i], pointsList[i + 1], 0xFFFFFFFF, 2.5f);
                            }
                            bool isClosed = ((BymlNode<bool>)externalRail["IsClosed"]).Data;
                            if (isClosed)
                            {
                                mDrawList.AddLine(pointsList[pointsList.Count - 1], pointsList[0], 0xFFFFFFFF, 2.5f);
                            }
                        }
                    }
                }
            }

            BymlArrayNode actorArray = (BymlArrayNode)root["Actors"];

            BymlHashTable? newHoveredActor = null;

            foreach (BymlHashTable actor in actorArray.Array)
            {
                BymlArrayNode translationArr = (BymlArrayNode)actor["Translate"];
                BymlArrayNode scaleArr = (BymlArrayNode)actor["Scale"];
                BymlArrayNode rotationArr = (BymlArrayNode)actor["Rotate"];

                string layer = ((BymlNode<string>)actor["Layer"]).Data;

                if (mLayersVisibility!.TryGetValue(layer, out bool isVisible) && isVisible)
                {
                    Matrix4x4 transform =
                        Matrix4x4.CreateScale(
                            ((BymlNode<float>)scaleArr[0]).Data,
                            ((BymlNode<float>)scaleArr[1]).Data,
                            ((BymlNode<float>)scaleArr[2]).Data
                        ) *
                        Matrix4x4.CreateRotationZ(
                            ((BymlNode<float>)rotationArr[2]).Data
                        ) *
                        Matrix4x4.CreateTranslation(
                            ((BymlNode<float>)translationArr[0]).Data,
                            ((BymlNode<float>)translationArr[1]).Data,
                            ((BymlNode<float>)translationArr[2]).Data
                        );

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

                    string name = ((BymlNode<string>)actor["Gyaml"]).Data;
                    
                   isHovered = HitTestConvexPolygonPoint(s_actorRectPolygon, ImGui.GetMousePos());

                    if(name.Contains("Area"))
                    {
                        isHovered = HitTestLineLoopPoint(s_actorRectPolygon, 4f,
                            ImGui.GetMousePos());
                    }

                    if (isHovered)
                    {
                        newHoveredActor = actor;

                        ImGui.BeginTooltip();
                        ImGui.SetTooltip($"{name}");
                        ImGui.EndTooltip();
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
