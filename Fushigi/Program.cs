
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

WindowManager.CreateWindow(out IWindow window);

byte[] folderBytes = new byte[256];
bool _stageList = false;
bool _courseSelected = false;
string selectedStage = "";
string selectedArea = "";
Dictionary<string, string[]> courseEntries = [];

Course currentCourse = null;

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
        if(!courseEntries.ContainsKey(worldName))
            courseEntries.Add(worldName, courseLocationList.ToArray());
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

    if (area.mAreaParams.ContainsParam("BgmType"))
    {
        byte[] buf = Encoding.ASCII.GetBytes(area.mAreaParams.mBGMType);
        ImGui.InputText("Background Music Type", buf, (uint)buf.Length);
    }

    if (area.mAreaParams.ContainsParam("EnvSetName"))
    {
        byte[] buf = Encoding.ASCII.GetBytes(area.mAreaParams.mEnvSetName);
        ImGui.InputText("Env Set Name", buf, (uint)buf.Length);
    }

    if (area.mAreaParams.ContainsParam("EnvironmentSound"))
    {
        byte[] buf = Encoding.ASCII.GetBytes(area.mAreaParams.mEnviornmentSound);
        ImGui.InputText("Environment Sound", buf, (uint)buf.Length);
    }

    if (area.mAreaParams.ContainsParam("EnvironmentSoundEfx"))
    {
        byte[] buf = Encoding.ASCII.GetBytes(area.mAreaParams.mEnviornmentSoundEfx);
        ImGui.InputText("Environment Sound EFX", buf, (uint)buf.Length);
    }

    if (area.mAreaParams.ContainsParam("IsNotCallWaterEnvSE"))
    {
        ImGui.Checkbox("IsNotCallWaterEnvSE", ref area.mAreaParams.mIsNotCallWaterEnvSE);
    }

    if (area.mAreaParams.ContainsParam("WonderBgmStartOffset"))
    {
        ImGui.InputFloat("WonderBgmStartOffset", ref area.mAreaParams.mWonderBGMStartOffset);
    }

    if (area.mAreaParams.ContainsParam("EnvironmentSoundEfx"))
    {
        byte[] buf = Encoding.ASCII.GetBytes(area.mAreaParams.mEnviornmentSoundEfx);
        ImGui.InputText("Environment Sound EFX", buf, (uint)buf.Length);
    }

    if (area.mAreaParams.ContainsParam("WonderBgmType"))
    {
        byte[] buf = Encoding.ASCII.GetBytes(area.mAreaParams.mWonderBGMType);
        ImGui.InputText("Wonder BGM Type", buf, (uint)buf.Length);
    }

    if (area.mAreaParams.mSkinParams != null)
    {
        if (ImGui.TreeNode("Skin Parameters"))
        {
            CourseArea.AreaParam.SkinParam skinParams = area.mAreaParams.mSkinParams;

            if (area.mAreaParams.ContainsSkinParam("FieldA"))
            {
                byte[] fielda_buf = Encoding.ASCII.GetBytes(skinParams.mFieldA);
                ImGui.InputText("FieldA", fielda_buf, (uint)fielda_buf.Length);
            }

            if (area.mAreaParams.ContainsSkinParam("FieldB"))
            {
                byte[] buf = Encoding.ASCII.GetBytes(skinParams.mFieldA);
                ImGui.InputText("FieldB", buf, (uint)buf.Length);
            }

            if (area.mAreaParams.ContainsSkinParam("Object"))
            {
                byte[] buf = Encoding.ASCII.GetBytes(skinParams.mObject);
                ImGui.InputText("Object", buf, (uint)buf.Length);
            }

            if (area.mAreaParams.ContainsSkinParam("DisableBgUnitDecoA"))
            {
                ImGui.Checkbox("DisableBgUnitDecoA", ref skinParams.mDisableBgUnitDecoA);
            }
        }
    }

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

        RomFS.SetRoot(basePath);

        if (Path.Exists(basePath))
        {
            CacheCourseFiles();
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