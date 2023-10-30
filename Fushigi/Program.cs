
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
using Fushigi.ui.widgets;
using TinyDialogsNet;

WindowManager.CreateWindow(out IWindow window);

UserSettings.Load();


string folderName = "";

if (!String.IsNullOrWhiteSpace(UserSettings.GetRomFSPath()))
{
    folderName = UserSettings.GetRomFSPath();
}

bool _stageList = false;
bool _courseSelected = false;
CourseScene courseScene = null;

Course currentCourse = null;

ParamDB.Init();
ParamLoader.Load();

window.Load += () => WindowManager.RegisterRenderDelegate(window, DoRendering);
window.Closing += DoClosing;

void DoClosing()
{
    UserSettings.Save();
}

void DoFill()
{
    foreach (KeyValuePair<string, string[]> worldCourses in RomFS.GetCourseEntries())
    {
        if (ImGui.TreeNode(worldCourses.Key))
        {
            foreach (var courseLocation in worldCourses.Value)
            {
                if (ImGui.RadioButton(
                        courseLocation,
                        currentCourse == null ? false : courseLocation == currentCourse.GetName()
                    )
                )
                {
                    if (currentCourse == null || currentCourse.GetName() != courseLocation)
                    {
                        currentCourse = new(courseLocation);
                        //selectedArea = currentCourse.GetArea(0).GetName();
                        courseScene = new(currentCourse);
                        _courseSelected = true;
                    }

                    ImGui.TreePop();
                }
            }
            ImGui.TreePop();
        }
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
    //ImGui.InputText("RomFS Folder", ref folderName, 512);

    if (ImGui.Button("Select RomFS Folder"))
    {
        FolderDialog dialog = new FolderDialog();
        if (dialog.ShowDialog("Select Your RomFS Folder..."))
        {
            string basePath = dialog.SelectedPath.Replace("\0", "");
            if (string.IsNullOrEmpty(basePath))
                basePath = "D:\\Hacking\\Switch\\Wonder\\romfs";

            if (Path.Exists(basePath))
            {
                RomFS.SetRoot(basePath);
                UserSettings.SetRomFSPath(basePath);

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
    }

    if (_stageList)
    {
        DoFill();
    }

    if (_courseSelected)
    {
        courseScene.DisplayCourse();
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