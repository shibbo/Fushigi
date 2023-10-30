using Fushigi.Byml;
using Fushigi.course;
using Fushigi.param;
using ImGuiNET;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.ui.widgets
{
    class CourseScene
    {
        Course course;
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
        Dictionary<string, bool> mLayers = new Dictionary<string, bool>();
        bool mHasFilledLayers = false;
        IWindow mParentWindow;
        bool mAllLayersStatus = true;

        public CourseScene(Course course, IWindow window)
        {
            this.course = course;
            selectedArea = course.GetArea(0);
            mParentWindow = window;
        }

        public void DisplayCourse()
        {
            bool status = ImGui.Begin("Course");

            CreateTabs();

            CreateMap();

            CreateAreaParams();

            CreateActorList();

            CreateLayerList();

            if (status)
            {
                ImGui.End();
            }
        }

        private void CreateTabs()
        {
            bool tabStatus = ImGui.BeginTabBar("Course Areas"); // Not sure what the string argument is for

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

        private void CreateLayerList()
        {
            bool status = ImGui.Begin("Layers");

            if (ImGui.Checkbox("All Layers", ref mAllLayersStatus))
            {
                if (mAllLayersStatus == false)
                {
                    foreach (string layer in mLayers.Keys)
                    {
                        mLayers[layer] = false;
                    }
                }
                else
                {
                    foreach (string layer in mLayers.Keys)
                    {
                        mLayers[layer] = true;
                    }
                }
            }

            foreach (string layer in mLayers.Keys)
            {
                bool isActive = mLayers[layer];
                if (ImGui.Checkbox(layer, ref isActive))
                {
                    mLayers[layer] = isActive;
                }
            }

            if (status)
            {
                ImGui.End();
            }
        }

        private void CreateActorList()
        {
            var root = selectedArea.GetRootNode();

            ImGui.Begin("Actors");
            ImGui.Text(selectedArea.GetName());

            // actors are in an array
            BymlArrayNode actorArray = (BymlArrayNode)((BymlHashTable)root)["Actors"];

            ListActors(actorArray);

            ImGui.End();
        }

        private void CreateAreaParams()
        {
            bool status = ImGui.Begin("Course Area Parameters");

            ImGui.Text(selectedArea.GetName());

            AreaParamLoad(selectedArea.mAreaParams);

            if (status)
            {
                ImGui.End();
            }
        }

        private void CreateMap()
        {
            UpdateCanvasSizes();

            drawList = ImGui.GetWindowDrawList();

            //canvas background
            drawList.AddRectFilled(canvasMin, canvasMax, 0xFF323232);

            //controls
            HandleIO();

            //grid lines
            GridLines();

            //level
            PopulateArea();

            drawList.AddRect(canvasMin, canvasMax, 0xFFFFFFFF);
        }

        private void AreaParamLoad(CourseArea.AreaParam area)
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
                        string? value = area.GetParam(area.GetRoot(), key, paramType) as string;
                        byte[] buf = Encoding.ASCII.GetBytes(value);
                        ImGui.InputText(key, buf, (uint)buf.Length);

                        break;
                }
            }
        }

        private void ListActors(BymlArrayNode actorArray)
        {
            foreach (BymlHashTable node in actorArray.Array)
            {
                string actorName = ((BymlNode<string>)node["Gyaml"]).Data;
                ulong hash = ((BymlBigDataNode<ulong>)node["Hash"]).Data;

                if (!mHasFilledLayers)
                {
                    string layer = ((BymlNode<string>)node["Layer"]).Data;

                    if (!mLayers.ContainsKey(layer))
                    {
                        mLayers.Add(layer, true);
                    }
                }

                ImGui.PushID(hash.ToString());
                if (ImGui.TreeNodeEx(actorName, ImGuiTreeNodeFlags.Selected))
                {
                    if (ImGui.TreeNode("Placement"))
                    {
                        PlacementNode(node);
                    }

                    /* actor parameters are loaded from the dynamic node */
                    if (node.ContainsKey("Dynamic"))
                    {
                        DynamicNode(node, actorName);
                    }
                    else
                    {
                        ImGui.TreePop();
                    }
                }

                ImGui.PopID();
            }

            mHasFilledLayers = true;
        }

        private void PlacementNode(BymlHashTable node)
        {
            var pos = (BymlArrayNode)node["Translate"];
            var rot = (BymlArrayNode)node["Rotate"];
            var scale = (BymlArrayNode)node["Scale"];

            ImGui.InputFloat("Pos X", ref ((BymlNode<float>)pos[0]).Data);
            ImGui.InputFloat("Pos Y", ref ((BymlNode<float>)pos[1]).Data);
            ImGui.InputFloat("Pos Z", ref ((BymlNode<float>)pos[2]).Data);

            ImGui.InputFloat("Rot X", ref ((BymlNode<float>)rot[0]).Data);
            ImGui.InputFloat("Rot Y", ref ((BymlNode<float>)rot[1]).Data);
            ImGui.InputFloat("Rot Z", ref ((BymlNode<float>)rot[2]).Data);

            ImGui.InputFloat("Scale X", ref ((BymlNode<float>)scale[0]).Data);
            ImGui.InputFloat("Scale Y", ref ((BymlNode<float>)scale[1]).Data);
            ImGui.InputFloat("Scale Z", ref ((BymlNode<float>)scale[2]).Data);

            ImGui.TreePop();
        }

        private void DynamicNode(BymlHashTable node, string actorName)
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

                if (ImGui.TreeNode(param))
                {
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
                                /*case "String":
                                    byte[] buf = Encoding.ASCII.GetBytes(((BymlNode<string>)paramNode).Data);
                                    ImGui.InputText(pair.Key, buf, (uint)buf.Length);
                                    break;*/
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

                    ImGui.TreePop();
                }
            }
            ImGui.TreePop();
        }

        private void UpdateCanvasSizes()
        {
            canvasSize = Vector2.Max(ImGui.GetContentRegionAvail(), new Vector2(50, 50));
            canvasMin = ImGui.GetCursorScreenPos();
            canvasMax = canvasMin + canvasSize;
            canvasMidpoint = canvasMin + (canvasSize * new Vector2(0.5f));
        }

        private void HandleIO()
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

        private void GridLines()
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
                                addPoint(new Vector2(x, y), 0xFFFFFFFF);
                                pointsList.Add(new Vector2(x, y));
                            }
                            for (int i = 0; i < pointsList.Count - 1; i++)
                            {
                                drawLine(pointsList[i], pointsList[i + 1], 0xFFFFFFFF);
                            }
                            bool isClosed = ((BymlNode<bool>)externalRail["IsClosed"]).Data;
                            if (isClosed)
                            {
                                drawLine(pointsList[pointsList.Count - 1], pointsList[0], 0xFFFFFFFF);
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
                    if (mLayers[layer])
                    {
                        float x = ((BymlNode<float>)translationArr[0]).Data;
                        float y = ((BymlNode<float>)translationArr[1]).Data;
                        Vector2 topLeft = new Vector2(x - 0.5f, y + 0.5f);
                        Vector2 bottomLeft = new Vector2(x - 0.5f, y - 0.5f);

                        addPoint(topLeft, (uint)Color.SpringGreen.ToArgb());
                        addPoint(bottomLeft, (uint)Color.SpringGreen.ToArgb());
                        drawLine(topLeft, bottomLeft, (uint)Color.SpringGreen.ToArgb());

                        Vector2 topRight = new Vector2(x + 0.5f, y + 0.5f);
                        Vector2 bottomRight = new Vector2(x + 0.5f, y - 0.5f);

                        addPoint(topRight, (uint)Color.SpringGreen.ToArgb());
                        addPoint(bottomRight, (uint)Color.SpringGreen.ToArgb());
                        drawLine(topRight, bottomRight, (uint)Color.SpringGreen.ToArgb());

                        drawLine(topLeft, topRight, (uint)Color.SpringGreen.ToArgb());
                        drawLine(bottomLeft, bottomRight, (uint)Color.SpringGreen.ToArgb());
                    }
                }
            }
        }

        private void addPoint(Vector2 gridPos, uint colour)
        {
            Vector2 modPos = panOrigin + (gridPos * new Vector2(gridPixelsPerUnit, -gridPixelsPerUnit));
            drawList.AddCircleFilled(modPos, 2, colour);
            //drawList.AddText(modPos, 0xFFFFFFFF, gridPos.ToString());
        }

        private void drawLine(Vector2 gridPos1, Vector2 gridPos2, uint colour)
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
