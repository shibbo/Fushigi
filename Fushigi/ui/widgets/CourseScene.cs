using Fushigi.Byml;
using Fushigi.course;
using Fushigi.param;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace Fushigi.ui.widgets
{
    class CourseScene
    {
        readonly Course course;
        CourseArea selectedArea;

        private const int gridBasePixelsPerUnit = 32;

        ImDrawListPtr drawList;

        Vector2 areaScenePan = new();
        float areaSceneZoom = 1;
        float gridPixelsPerUnit;

        //canvas viewport coordinates
        Vector2 canvasMin;
        Vector2 canvasSize;
        Vector2 canvasMax;
        Vector2 canvasMidpoint;

        Vector2 panOrigin;
        readonly Dictionary<string, bool> mLayersVisibility = [];
        bool mHasFilledLayers = false;
        readonly IWindow mParentWindow;
        bool mAllLayersVisible = true;

        BymlHashTable? mSelectedActor = null;

        public CourseScene(Course course, IWindow window)
        {
            this.course = course;
            selectedArea = course.GetArea(0);
            mParentWindow = window;
        }

        public void DrawUI()
        {
            bool status = ImGui.Begin("Course");

            CourseTabBar();

            LevelViewport();

            AreaParameterPanel();

            ActorsPanel();

            ActorParameterPanel();

            LayersPanel();

            if (status)
            {
                ImGui.End();
            }
        }

        public void Save()
        {
            //Save each course area to current romfs folder
            foreach (var area in this.course.GetAreas())
                area.Save();
        }

        public void Save(string folder)
        {
            //Save each course area to a specific folder
            foreach (var area in this.course.GetAreas())
                area.Save(folder);
        }

        private void CourseTabBar()
        {
            bool tabStatus = ImGui.BeginTabBar("Courses TabBar"); // Not sure what the string argument is for

            foreach (var area in course.GetAreas())
            {
                if (ImGui.BeginTabItem(area.GetName()))
                {
                    selectedArea = area;

                    ImGui.EndTabItem();
                }
            }

            if (tabStatus)
            {
                ImGui.EndTabBar();
            }
        }

        private void LayersPanel()
        {
            bool status = ImGui.Begin("Layers");

            if (ImGui.Checkbox("All Layers", ref mAllLayersVisible))
            {
                foreach (string layer in mLayersVisibility.Keys)
                {
                    mLayersVisibility[layer] = mAllLayersVisible;
                }
            }

            foreach (string layer in mLayersVisibility.Keys)
            {
                bool isVisible = mLayersVisibility[layer];
                if (ImGui.Checkbox(layer, ref isVisible))
                {
                    mLayersVisibility[layer] = isVisible;
                }
            }

            if (status)
            {
                ImGui.End();
            }
        }

        private void ActorsPanel()
        {
            var root = selectedArea.GetRootNode();

            ImGui.Begin("Actors");

            // actors are in an array
            BymlArrayNode actorArray = (BymlArrayNode)((BymlHashTable)root)["Actors"];

            //CourseActorsTreeView(actorArray);
            CourseActorsLayerView(actorArray);

            ImGui.End();
        }

        private void ActorParameterPanel()
        {
            if (mSelectedActor is null)
            {
                return;
            }

            bool status = ImGui.Begin("Actor Parameters");

            string actorName = ((BymlNode<string>)mSelectedActor["Gyaml"]).Data;

            ImGui.Text(actorName);

            if (ImGui.BeginChild("Placement"))
            {
                PlacementNode(mSelectedActor);
            }

            /* actor parameters are loaded from the dynamic node */
            if (mSelectedActor.ContainsKey("Dynamic"))
            {
                DynamicParamNode(mSelectedActor, actorName);
            }
            else
            {
                ImGui.TreePop();
            }

            if (status)
            {
                ImGui.End();
            }
        }

        private void AreaParameterPanel()
        {
            bool status = ImGui.Begin("Course Area Parameters");

            ImGui.Text(selectedArea.GetName());

            AreaParameters(selectedArea.mAreaParams);

            if (status)
            {
                ImGui.End();
            }
        }

        private void LevelViewport()
        {
            UpdateCanvasSizes();

            drawList = ImGui.GetWindowDrawList();

            //canvas background
            drawList.AddRectFilled(canvasMin, canvasMax, 0xFF323232);

            //controls
            HandleViewportInput();

            //grid lines
            DrawGridLines();

            //level
            PopulateArea();

            drawList.AddRect(canvasMin, canvasMax, 0xFFFFFFFF);
        }

        private static void AreaParameters(CourseArea.AreaParam area)
        {
            ParamHolder areaParams = ParamLoader.GetHolder("AreaParam");

            foreach (string key in areaParams.Keys)
            {
                string paramType = areaParams[key];

                if (!area.ContainsParam(key))
                {
                    continue;
                }

                switch (paramType)
                {
                    case "String":
                        string value = (string)area.GetParam(area.GetRoot(), key, paramType);
                        ImGui.InputText(key, ref value, 1024);

                        break;
                }
            }
        }

        private void FillLayers(BymlArrayNode actorArray)
        {
            foreach (BymlHashTable node in actorArray.Array.Cast<BymlHashTable>())
            {
                string actorLayer = ((BymlNode<string>)node["Layer"]).Data;
                mLayersVisibility[actorLayer] = true;
            }
            mHasFilledLayers = true;
        }

        private void CourseActorsLayerView(BymlArrayNode actorArray)
        {
            if (!mHasFilledLayers)
            {
                FillLayers(actorArray);
            }

            if (ImGui.Checkbox("All Layers", ref mAllLayersVisible))
            {
                foreach (string layer in mLayersVisibility.Keys)
                {
                    mLayersVisibility[layer] = mAllLayersVisible;
                }
            }

            foreach (string layer in mLayersVisibility.Keys)
            {
                bool isVisible = mLayersVisibility[layer];
                if (ImGui.Checkbox("##" + layer, ref isVisible))
                {
                    mLayersVisibility[layer] = isVisible;
                }

                ImGui.SameLine();

                if (!isVisible)
                {
                    ImGui.BeginDisabled();
                }

                if (ImGui.CollapsingHeader(layer, ImGuiTreeNodeFlags.Selected))
                {
                    ImGui.Indent();
                    ImGui.PushItemWidth(ImGui.GetColumnWidth());
                    if (ImGui.BeginListBox("##" + layer))
                    {
                        foreach (BymlHashTable node in actorArray.Array.Cast<BymlHashTable>())
                        {
                            string actorName = ((BymlNode<string>)node["Gyaml"]).Data;
                            ulong actorHash = ((BymlBigDataNode<ulong>)node["Hash"]).Data;
                            string actorLayer = ((BymlNode<string>)node["Layer"]).Data;

                            if (actorLayer != layer)
                            {
                                continue;
                            }

                            bool isSelected = (node == mSelectedActor);

                            ImGui.PushID(actorHash.ToString());
                            if (ImGui.Selectable(actorName, isSelected))
                            {
                                mSelectedActor = node;
                            }
                            ImGui.PopID();
                        }
                        ImGui.EndListBox();
                    }
                    ImGui.Unindent();
                }

                if (!isVisible)
                {
                    ImGui.EndDisabled();
                }
            }
        }

        private static void PlacementNode(BymlHashTable node)
        {
            var pos = (BymlArrayNode)node["Translate"];
            var rot = (BymlArrayNode)node["Rotate"];
            var scale = (BymlArrayNode)node["Scale"];

            if (ImGui.CollapsingHeader("Position"))
            {
                ImGui.PushID("Position");
                ImGui.Indent();
                ImGui.InputFloat("X", ref ((BymlNode<float>)pos[0]).Data);
                ImGui.InputFloat("Y", ref ((BymlNode<float>)pos[1]).Data);
                ImGui.InputFloat("Z", ref ((BymlNode<float>)pos[2]).Data);
                ImGui.Unindent();
                ImGui.PopID();
            }

            if (ImGui.CollapsingHeader("Rotation"))
            {
                ImGui.PushID("Rotation");
                ImGui.Indent();
                ImGui.InputFloat("X", ref ((BymlNode<float>)rot[0]).Data);
                ImGui.InputFloat("Y", ref ((BymlNode<float>)rot[1]).Data);
                ImGui.InputFloat("Z", ref ((BymlNode<float>)rot[2]).Data);
                ImGui.Unindent();
                ImGui.PopID();
            }

            if (ImGui.CollapsingHeader("Scale"))
            {
                ImGui.PushID("Scale");
                ImGui.Indent();
                ImGui.InputFloat("X", ref ((BymlNode<float>)scale[0]).Data);
                ImGui.InputFloat("Y", ref ((BymlNode<float>)scale[1]).Data);
                ImGui.InputFloat("Z", ref ((BymlNode<float>)scale[2]).Data);
                ImGui.Unindent();
                ImGui.PopID();
            }

        }

        private void DynamicParamNode(BymlHashTable node, string actorName)
        {
            List<string> actorParams = ParamDB.GetActorComponents(actorName);
            var dynamicNode = (BymlHashTable)node["Dynamic"];

            foreach (string param in actorParams)
            {
                Dictionary<string, ParamDB.ComponentParam> dict = ParamDB.GetComponentParams(param);

                if (dict.Keys.Count == 0)
                {
                    continue;
                }

                if (ImGui.CollapsingHeader(param))
                {
                    ImGui.Indent();

                    foreach (KeyValuePair<string, ParamDB.ComponentParam> pair in ParamDB.GetComponentParams(param))
                    {
                        if (dynamicNode.ContainsKey(pair.Key))
                        {
                            var paramNode = dynamicNode[pair.Key];

                            switch (pair.Value.Type)
                            {
                                case "S16":
                                case "S32":
                                    ImGui.InputInt(pair.Key, ref ((BymlNode<int>)paramNode).Data);
                                    break;
                                case "Bool":
                                    ImGui.Checkbox(pair.Key, ref ((BymlNode<bool>)paramNode).Data);
                                    break;
                                case "F32":
                                    ImGui.InputFloat(pair.Key, ref ((BymlNode<float>)paramNode).Data);
                                    break;
                                case "String":
                                    ImGui.InputText(pair.Key, ref ((BymlNode<string>)paramNode).Data, 1024);
                                    break;
                                case "F64":
                                    double val = ((BymlBigDataNode<double>)paramNode).Data;
                                    ImGui.InputDouble(pair.Key, ref val);
                                    break;
                            }
                        }
                        else
                        {
                            //object initValue = ;
                            switch (pair.Value.Type)
                            {
                                case "S16":
                                case "S32":
                                case "U32":
                                    {
                                        int val = Convert.ToInt32(pair.Value.InitValue);
                                        ImGui.InputInt(pair.Key, ref val);
                                        break;
                                    }

                                case "Bool":
                                    {
                                        bool val = (bool)pair.Value.InitValue;
                                        ImGui.Checkbox(pair.Key, ref val);
                                        break;
                                    }
                                case "F32":
                                    {
                                        float val = Convert.ToSingle(pair.Value.InitValue);
                                        ImGui.InputFloat(pair.Key, ref val);
                                        break;
                                    }

                                case "F64":
                                    {
                                        double val = Convert.ToDouble(pair.Value.InitValue);
                                        if (ImGui.InputDouble(pair.Key, ref val))
                                        {

                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    ImGui.Unindent();
                }
            }
        }

        private void UpdateCanvasSizes()
        {
            canvasSize = Vector2.Max(ImGui.GetContentRegionAvail(), new Vector2(50, 50));
            canvasMin = ImGui.GetCursorScreenPos();
            canvasMax = canvasMin + canvasSize;
            canvasMidpoint = canvasMin + (canvasSize * new Vector2(0.5f));
        }

        private void HandleViewportInput()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            //mouse hover and click detection
            ImGui.InvisibleButton("canvas", canvasSize, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);
            bool mouseHover = ImGui.IsItemHovered();
            bool mouseActive = ImGui.IsItemActive();

            // panning with middle mouse click
            if (mouseActive && ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
            {
                areaScenePan += io.MouseDelta;
            }
            panOrigin = canvasMidpoint + areaScenePan;

            // zooming with scroll wheel
            if (mouseHover && io.MouseWheel != 0)
            {
                Vector2 prevMouseGridCoordinates = (io.MousePos - panOrigin) / new Vector2(gridBasePixelsPerUnit * areaSceneZoom);
                areaSceneZoom += io.MouseWheel * 0.1f * areaSceneZoom;
                areaSceneZoom = MathF.Max(MathF.Min(areaSceneZoom, 5), 0.1f);
                Vector2 newMouseGridCoordinates = (io.MousePos - panOrigin) / new Vector2(gridBasePixelsPerUnit * areaSceneZoom);
                areaScenePan += (newMouseGridCoordinates - prevMouseGridCoordinates) * new Vector2(gridBasePixelsPerUnit * areaSceneZoom);
                panOrigin = canvasMidpoint + areaScenePan;
            }
            gridPixelsPerUnit = gridBasePixelsPerUnit * areaSceneZoom;
        }

        private void DrawGridLines()
        {
            drawList.PushClipRect(canvasMin, canvasMax, true);

            Vector2 gridStart = (canvasSize * new Vector2(0.5f)) + areaScenePan;
            for (float x = gridStart.X % gridPixelsPerUnit; x < canvasSize.X; x += gridPixelsPerUnit)
            {
                drawList.AddLine(new Vector2(canvasMin.X + x, canvasMin.Y), new Vector2(canvasMin.X + x, canvasMax.Y), MathF.Abs((canvasMin.X + x) - panOrigin.X) < 0.01 ? 0xFF008000 : 0xFF505050);
            }
            for (float y = gridStart.Y % gridPixelsPerUnit; y < canvasSize.Y; y += gridPixelsPerUnit)
            {
                drawList.AddLine(new Vector2(canvasMin.X, canvasMin.Y + y), new Vector2(canvasMax.X, canvasMin.Y + y), MathF.Abs((canvasMin.Y + y) - panOrigin.Y) < 0.01 ? 0xFF000080 : 0xFF505050);
            }
        }

        private void PopulateArea()
        {
            var root = selectedArea.GetRootNode();

            if (((BymlHashTable)root).ContainsKey("BgUnits"))
            {
                //BgUnits are in an array.
                BymlArrayNode bgUnitsArray = (BymlArrayNode)((BymlHashTable)root)["BgUnits"];

                foreach (BymlHashTable bgUnit in bgUnitsArray.Array)
                {
                    if (bgUnit.ContainsKey("Walls"))
                    {
                        BymlArrayNode wallsArray = (BymlArrayNode)((BymlHashTable)bgUnit)["Walls"];

                        foreach (BymlHashTable walls in wallsArray.Array)
                        {
                            BymlHashTable externalRail = (BymlHashTable)walls["ExternalRail"];
                            BymlArrayNode pointsArray = (BymlArrayNode)externalRail["Points"];
                            List<Vector2> pointsList = new();
                            foreach (BymlHashTable points in pointsArray.Array)
                            {
                                var pos = (BymlArrayNode)points["Translate"];
                                float x = ((BymlNode<float>)pos[0]).Data;
                                float y = ((BymlNode<float>)pos[1]).Data;
                                AddPoint(new Vector2(x, y), 0xFFFFFFFF);
                                pointsList.Add(new Vector2(x, y));
                            }
                            for (int i = 0; i < pointsList.Count - 1; i++)
                            {
                                DrawLine(pointsList[i], pointsList[i + 1], 0xFFFFFFFF);
                            }
                            bool isClosed = ((BymlNode<bool>)externalRail["IsClosed"]).Data;
                            if (isClosed)
                            {
                                DrawLine(pointsList[pointsList.Count - 1], pointsList[0], 0xFFFFFFFF);
                            }
                        }
                    }
                }
            }

            BymlArrayNode actorArray = (BymlArrayNode)((BymlHashTable)root)["Actors"];
 
            foreach (BymlHashTable actor in actorArray.Array)
            {
                BymlArrayNode translationArr = (BymlArrayNode)actor["Translate"];

                string layer = ((BymlNode<string>)actor["Layer"]).Data;

                if (mHasFilledLayers)
                {
                    if (mLayersVisibility.TryGetValue(layer, out bool isVisible) && isVisible)
                    {
                        float x = ((BymlNode<float>)translationArr[0]).Data;
                        float y = ((BymlNode<float>)translationArr[1]).Data;
                        Vector2 topLeft = new Vector2(x - 0.5f, y + 0.5f);
                        Vector2 bottomLeft = new Vector2(x - 0.5f, y - 0.5f);

                        AddPoint(topLeft, (uint)Color.SpringGreen.ToArgb());
                        AddPoint(bottomLeft, (uint)Color.SpringGreen.ToArgb());
                        DrawLine(topLeft, bottomLeft, (uint)Color.SpringGreen.ToArgb());

                        Vector2 topRight = new Vector2(x + 0.5f, y + 0.5f);
                        Vector2 bottomRight = new Vector2(x + 0.5f, y - 0.5f);

                        AddPoint(topRight, (uint)Color.SpringGreen.ToArgb());
                        AddPoint(bottomRight, (uint)Color.SpringGreen.ToArgb());
                        DrawLine(topRight, bottomRight, (uint)Color.SpringGreen.ToArgb());

                        DrawLine(topLeft, topRight, (uint)Color.SpringGreen.ToArgb());
                        DrawLine(bottomLeft, bottomRight, (uint)Color.SpringGreen.ToArgb());
                    }
                }
            }
        }

        private void AddPoint(Vector2 gridPos, uint colour)
        {
            Vector2 modPos = panOrigin + (gridPos * new Vector2(gridPixelsPerUnit, -gridPixelsPerUnit));
            drawList.AddCircleFilled(modPos, 2, colour);
            //drawList.AddText(modPos, 0xFFFFFFFF, gridPos.ToString());
        }

        private void DrawLine(Vector2 gridPos1, Vector2 gridPos2, uint colour)
        {
            Vector2 modPos1 = panOrigin + (gridPos1 * new Vector2(gridPixelsPerUnit, -gridPixelsPerUnit));
            Vector2 modPos2 = panOrigin + (gridPos2 * new Vector2(gridPixelsPerUnit, -gridPixelsPerUnit));
            drawList.AddLine(modPos1, modPos2, colour);
        }

        public Course GetCourse()
        {
            return course;
        }
    }
}
