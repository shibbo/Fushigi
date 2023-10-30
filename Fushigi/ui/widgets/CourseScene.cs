using Fushigi.Byml;
using Fushigi.course;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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

        public CourseScene(Course course)
        {
            this.course = course;
            selectedArea = course.GetArea(0);
        }

        public void DisplayCourse()
        {
            bool status = ImGui.Begin("Course");

            CreateTabs();

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

            //BgUnits are in an array.
            BymlArrayNode bgUnitsArray = (BymlArrayNode)((BymlHashTable)root)["BgUnits"];
            foreach (BymlHashTable bgUnit in bgUnitsArray.Array)
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
    }
}
