
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

WindowManager.CreateWindow(out IWindow window);

byte[] folderBytes = new byte[256];
bool _stageList = false;
bool _courseSelected = false;
string selectedStage = "";
string selectedArea = "";
Dictionary<string, string[]> courseEntries = [];
Vector2 areaScenePan = new();
float areaSceneZoom = 1;

Course currentCourse = null;

ParamDB.Init();
ParamLoader.Load();

window.Load += () => WindowManager.RegisterRenderDelegate(window, DoRendering);

void CacheCourseFiles()
{
    courseEntries.Clear();
    string[] loadFiles = RomFS.GetFiles("/Stage/WorldMapInfo");
    foreach (string loadFile in loadFiles)
    {
        string worldName = Path.GetFileName(loadFile).Split(".game")[0];
        List<string> courseLocationList = new();
        Byml byml = new Byml(new MemoryStream(File.ReadAllBytes(loadFile)));
        var root = (BymlHashTable)byml.Root;
        var courseList = (BymlArrayNode)root["CourseTable"];

        for (int i = 0; i < courseList.Length; i++)
        {
            var course = (BymlHashTable)courseList[i];
            string derp = ((BymlNode<string>)course["StagePath"]).Data;

            // we need to "fix" our StagePath so it points to our course
            string courseLocation = Path.GetFileName(derp).Split(".game")[0];

            courseLocationList.Add(courseLocation);
        }
        if (!courseEntries.ContainsKey(worldName))
        {
            courseEntries.Add(worldName, courseLocationList.ToArray());
        }
    }
}

void DoFill()
{
    /* common paths to check */
    if (!RomFS.DirectoryExists("BancMapUnit") || !RomFS.DirectoryExists("Model") || !RomFS.DirectoryExists("Stage"))
    {
        throw new Exception("DoRendering() -- Required folders not found.");
    }

    foreach (KeyValuePair<string, string[]> worldCourses in courseEntries)
    {
        if (ImGui.TreeNode(worldCourses.Key))
        {
            for (int i = 0; i < worldCourses.Value.Length; i++)
            {
                string courseLocation = worldCourses.Value[i];
                if (ImGui.TreeNodeEx(courseLocation))
                {
                    currentCourse = new Course(courseLocation);
                    _courseSelected = true;
                    ImGui.TreePop();
                }
            }
            ImGui.TreePop();
        }
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
            selectedArea = area.GetName();
        }
    }

    if (status)
    {
        ImGui.End();
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
    if (mouseHover && io.MouseWheel!=0)
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
        drawList.AddLine(new Vector2(canvasMin.X+x, canvasMin.Y), new Vector2(canvasMin.X + x, canvasMax.Y), MathF.Abs((canvasMin.X + x) - panOrigin.X)<0.01 ? 0xFF008000 : 0xFF505050);
    }
    for (float y = gridStart.Y % gridPixelsPerUnit; y < canvasSize.Y; y += gridPixelsPerUnit)
    {
        drawList.AddLine(new Vector2(canvasMin.X, canvasMin.Y+y), new Vector2(canvasMax.X, canvasMin.Y+y), MathF.Abs((canvasMin.Y + y) - panOrigin.Y) < 0.01 ? 0xFF000080 : 0xFF505050);
    }


    /* debug reference points
    drawList.AddCircleFilled(panOrigin, 2, 0xFFFFFFFF);
    drawList.AddText(panOrigin, 0xFFFFFFFF, "origin");

    Action<Vector2, uint> addDebugPoint = (gridPos, colour) =>
    {
        drawList.AddCircleFilled(panOrigin + (gridPos * new Vector2(unitScreenSize, -unitScreenSize)), 2, colour);
        drawList.AddText(panOrigin + (gridPos * new Vector2(unitScreenSize, -unitScreenSize)), 0xFFFFFFFF, gridPos.ToString());
    };

    addDebugPoint(new Vector2(2, 3), 0xFF0000FF);
    addDebugPoint(new Vector2(-5, 7), 0xFF00FF00);
    addDebugPoint(new Vector2(-1, -5), 0xFFFF0000);
    */

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

        RomFS.SetRoot(basePath);

        if (Path.Exists(basePath))
        {
            CacheCourseFiles();

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

    // Make sure ImGui renders too!
    controller.Render();
}

window.Run();
window.Dispose();