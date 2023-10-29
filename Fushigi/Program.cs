
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using ImGuiNET;
using Fushigi.util;
using Fushigi.windowing;
using Silk.NET.OpenGL.Extensions.ImGui;
using Fushigi.Byml;
using Fushigi.Byml.Writer;
using Fushigi.Byml.Writer.Primitives;
using Fushigi;
using Fushigi.course;
using System.Text;
using System.Numerics;
using Fushigi.param;
using Fushigi.SARC;
using System.Diagnostics;
using System.Runtime.CompilerServices;

WindowManager.CreateWindow(out IWindow window);

byte[] folderBytes = new byte[256];
bool _stageList = false;
bool _courseSelected = false;
bool _loadActors = false;
string selectedStage = "";
string selectedArea = "";
Vector2 areaScenePan = new();
float areaSceneZoom = 1;

Course currentCourse = null;

ParamDB.Init();
ParamLoader.Load();

window.Load += () => WindowManager.RegisterRenderDelegate(window, DoRendering);

void DoFill()
{
    /* common paths to check */
    if (!RomFS.DirectoryExists("BancMapUnit") || !RomFS.DirectoryExists("Model") || !RomFS.DirectoryExists("Stage"))
    {
        throw new Exception("DoRendering() -- Required folders not found.");
    }

    foreach (KeyValuePair<string, string[]> worldCourses in RomFS.GetCourseEntries())
    {
        if (ImGui.TreeNode(worldCourses.Key))
        {
            for (int i = 0; i < worldCourses.Value.Length; i++)
            {
                string courseLocation = worldCourses.Value[i];
                if (ImGui.TreeNodeEx(courseLocation))
                {
                    if (currentCourse == null || currentCourse.GetName() != courseLocation)
                    {
                        currentCourse = new Course(courseLocation);
                        _courseSelected = true;
                    }

                    ImGui.TreePop();
                }
            }
            ImGui.TreePop();
        }
    }
}

void DoActorLoad()
{
    CourseArea area = currentCourse.GetArea(selectedArea);
    var root = area.GetRootNode();

    bool actorStatus = ImGui.Begin("Actors");

    // actors are in an array
    BymlArrayNode actorArray = (BymlArrayNode)((BymlHashTable)root)["Actors"];

    foreach (BymlHashTable node in actorArray.Array)
    {
        string actorName = ((BymlNode<string>)node["Gyaml"]).Data;
        ulong hash = ((BymlBigDataNode<ulong>)node["Hash"]).Value;

        ImGui.PushID(hash.ToString());
        if (ImGui.TreeNode(actorName))
        {
            if (ImGui.TreeNode("Placement"))
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

            List<string> actorParams = ParamDB.GetActorComponents(actorName);

            /* actor parameters are loaded from the dynamic node */

            if (node.ContainsKey("Dynamic"))
            {
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
                                        double val = ((BymlBigDataNode<double>)paramNode).Value;
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
                                            ImGui.InputDouble(pair.Key, ref val);
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
        }
        ImGui.PopID();
    }

    if (actorStatus)
    {
        ImGui.End();
    }
}

void DoAreaSelect()
{
    bool status = ImGui.Begin("Area Select");
    int areaCount = currentCourse.GetAreaCount();

    for (int i = 0; i < areaCount; i++)
    {
        CourseArea area = currentCourse.GetArea(i);
        if (ImGui.Selectable(area.GetName()))
        {
            if (selectedArea != area.GetName())
            {
                selectedArea = area.GetName();
                _loadActors = true;
            }
        }
    }

    if (status)
    {
        ImGui.End();
    }

    if (_loadActors)
    {
        DoActorLoad();
    }
}

void DoAreaParamLoad(CourseArea.AreaParam area)
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

void DoAreaParams()
{
    bool status = ImGui.Begin("Course Area Parameters");

    CourseArea area = currentCourse.GetArea(selectedArea);

    // if the area is null, it means we just switched from another course to a new one
    // so, we nullify the selected area until the user selects a new one
    if (area == null)
    {
        selectedArea = "";
        return;
    }

    ImGui.Text(area.GetName());

    DoAreaParamLoad(area.mAreaParams);

    if (status)
    {
        ImGui.End();
    }
}


void DoAreaScene()
{
    const int gridBasePixelsPerUnit = 32;

    bool status = ImGui.Begin("Course Area");

    CourseArea area = currentCourse.GetArea(selectedArea);
    var root = area.GetRootNode();

    //canvas viewport coordinates
    Vector2 canvasMin = ImGui.GetCursorScreenPos();
    Vector2 canvasSize = Vector2.Max(ImGui.GetContentRegionAvail(), new Vector2(50, 50));
    Vector2 canvasMax = canvasMin + canvasSize;
    Vector2 canvasMidpoint = canvasMin + (canvasSize * new Vector2(0.5f));

    ImGuiIOPtr io = ImGui.GetIO();
    ImDrawListPtr drawList = ImGui.GetWindowDrawList();

    //canvas background
    drawList.AddRectFilled(canvasMin, canvasMax, 0xFF323232);

    //mouse hover and click detection
    ImGui.InvisibleButton("canvas", canvasSize, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);
    bool mouseHover = ImGui.IsItemHovered();
    bool mouseActive = ImGui.IsItemActive();

    // panning with middle mouse click
    if (mouseActive && ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
    {
        areaScenePan += io.MouseDelta;
    }
    Vector2 panOrigin = canvasMidpoint + areaScenePan;

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
    float gridPixelsPerUnit = gridBasePixelsPerUnit * areaSceneZoom;

    // grid lines
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

    Action<Vector2, uint> addPoint = (gridPos, colour) =>
    {
        Vector2 modPos = panOrigin + (gridPos * new Vector2(gridPixelsPerUnit, -gridPixelsPerUnit));
        drawList.AddCircleFilled(modPos, 2, colour);
        //drawList.AddText(modPos, 0xFFFFFFFF, gridPos.ToString());
    };

    Action<Vector2, Vector2, uint> drawLine = (gridPos1, gridPos2, colour) =>
    {
        Vector2 modPos1 = panOrigin + (gridPos1 * new Vector2(gridPixelsPerUnit, -gridPixelsPerUnit));
        Vector2 modPos2 = panOrigin + (gridPos2 * new Vector2(gridPixelsPerUnit, -gridPixelsPerUnit));
        drawList.AddLine(modPos1, modPos2, colour);
    };

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

    drawList.AddRect(canvasMin, canvasMax, 0xFFFFFFFF);
    if (status)
    {
        ImGui.End();
    }
}

void DoRendering(GL gl, double delta, ImGuiController controller)
{
    // This is where you'll do any rendering beneath the ImGui context
    // Here, we just have a blank screen.
    gl.Viewport(window.FramebufferSize);

    gl.ClearColor(.45f, .55f, .60f, 1f);
    gl.Clear((uint)ClearBufferMask.ColorBufferBit);


    // This is where you'll do all of your ImGUi rendering
    // Here, we're just showing the ImGui built-in demo window.
    //ImGui.ShowDemoWindow();

    bool status = ImGui.Begin("Input Folder");
    ImGui.InputText("RomFS Folder", folderBytes, 512);

    if (ImGui.Button("Select"))
    {
        string basePath = System.Text.Encoding.ASCII.GetString(folderBytes).Replace("\0", "");
        if (string.IsNullOrEmpty(basePath))
            basePath = "D:\\Hacking\\Switch\\Wonder\\romfs";     

        if (Path.Exists(basePath))
        {
            RomFS.SetRoot(basePath);

            if (!ParamDB.sIsInit)
            {
                ParamDB.Load();
            }

            _stageList = true;
        }
        else
        {
            throw new FileNotFoundException("DoRendering() -- Path does not exist.");
        }
    }

    if (_stageList)
    {
        DoFill();
    }

    if (_courseSelected)
    {
        DoAreaSelect();
    }

    if (selectedArea != "")
    {
        DoAreaParams();
        DoAreaScene();
    }

    if (status)
    {
        ImGui.End();
    }

    //ImGui.ShowMetricsWindow();

    // Make sure ImGui renders too!
    controller.Render();
}

window.Run();
window.Dispose();