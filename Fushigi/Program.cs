
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
bool _loadActors = false;
string selectedStage = "";
string selectedArea = "";
Vector2 areaScenePan = new();
float areaSceneZoom = 1;
AreaScene areaScene = null;

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
    if (area == null)
    {
        return;
    }

    var root = area.GetRootNode();

    bool actorStatus = ImGui.Begin("Actors");

    // actors are in an array
    BymlArrayNode actorArray = (BymlArrayNode)((BymlHashTable)root)["Actors"];

    foreach (BymlHashTable node in actorArray.Array)
    {
        string actorName = ((BymlNode<string>)node["Gyaml"]).Data;
        ulong hash = ((BymlBigDataNode<ulong>)node["Hash"]).Data;

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

    foreach (var area in currentCourse.GetAreas())
    {
        if (ImGui.Selectable(area.GetName()))
        {
            if (selectedArea != area.GetName())
            {
                selectedArea = area.GetName();
                areaScene = new(currentCourse.GetArea(selectedArea));
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

    //DoAreaParamLoad(area.mAreaParams);

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
        DoAreaSelect();
    }

    if (selectedArea != "")
    {
        DoAreaParams();
        areaScene.DisplayArea();
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