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
        LevelViewport viewport;
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
            viewport = new LevelViewport(selectedArea);
            mParentWindow = window;
        }

        public void DrawUI()
        {
            bool status = ImGui.Begin("Course");

            CourseTabBar();

            viewport.Draw(ImGui.GetContentRegionAvail(), mLayersVisibility, 
                selectedActors: new HashSet<BymlHashTable>()); //only temporary

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
            void EditFloat3(string label, BymlArrayNode node)
            {
                var vec = new System.Numerics.Vector3(
                           ((BymlNode<float>)node[0]).Data,
                           ((BymlNode<float>)node[1]).Data,
                           ((BymlNode<float>)node[2]).Data);

                ImGui.Text(label);
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 12);

                if (ImGui.DragFloat3($"##{label}", ref vec))
                {
                    ((BymlNode<float>)node[0]).Data = vec.X;
                    ((BymlNode<float>)node[1]).Data = vec.Y;
                    ((BymlNode<float>)node[2]).Data = vec.Z;
                }
                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                EditFloat3("Scale", (BymlArrayNode)node["Scale"]);
                EditFloat3("Rotation", (BymlArrayNode)node["Rotate"]);
                EditFloat3("Position", (BymlArrayNode)node["Translate"]);

                ImGui.Columns(1);
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

                    ImGui.Columns(2);

                    foreach (KeyValuePair<string, ParamDB.ComponentParam> pair in ParamDB.GetComponentParams(param))
                    {
                        string id = $"##{pair.Key}";

                        ImGui.Text(pair.Key);
                        ImGui.NextColumn();

                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - 12);

                        if (dynamicNode.ContainsKey(pair.Key))
                        {
                            var paramNode = dynamicNode[pair.Key];

                            switch (pair.Value.Type)
                            {
                                case "S16":
                                case "S32":
                                    ImGui.InputInt(id, ref ((BymlNode<int>)paramNode).Data);
                                    break;
                                case "Bool":
                                    ImGui.Checkbox(id, ref ((BymlNode<bool>)paramNode).Data);
                                    break;
                                case "F32":
                                    ImGui.InputFloat(id, ref ((BymlNode<float>)paramNode).Data);
                                    break;
                                case "String":
                                    ImGui.InputText(id, ref ((BymlNode<string>)paramNode).Data, 1024);
                                    break;
                                case "F64":
                                    double val = ((BymlBigDataNode<double>)paramNode).Data;
                                    ImGui.InputDouble(id, ref val);
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
                                        ImGui.InputInt(id, ref val);
                                        break;
                                    }

                                case "Bool":
                                    {
                                        bool val = (bool)pair.Value.InitValue;
                                        ImGui.Checkbox(id, ref val);
                                        break;
                                    }
                                case "F32":
                                    {
                                        float val = Convert.ToSingle(pair.Value.InitValue);
                                        ImGui.InputFloat(id, ref val);
                                        break;
                                    }

                                case "F64":
                                    {
                                        double val = Convert.ToDouble(pair.Value.InitValue);
                                        if (ImGui.InputDouble(id, ref val))
                                        {

                                        }
                                        break;
                                    }
                            }
                        }

                        ImGui.PopItemWidth();

                        ImGui.NextColumn();
                    }

                    ImGui.Columns(1);

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
        }

        public Course GetCourse()
        {
            return course;
        }
    }
}
