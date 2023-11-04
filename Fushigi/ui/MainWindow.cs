using Fushigi.course;
using Fushigi.util;
using Fushigi.windowing;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fushigi.param;
using Fushigi.ui.widgets;
using ImGuiNET;
using System.Numerics;
using System.Diagnostics;
using Silk.NET.SDL;

namespace Fushigi.ui
{
    public class MainWindow
    {

        public MainWindow()
        {
            WindowManager.CreateWindow(out mWindow);   
            mWindow.Load += () => WindowManager.RegisterRenderDelegate(mWindow, Render);
            mWindow.Closing += Close;
            mWindow.Run();
            mWindow.Dispose();
        }

        public void Close()
        {
            UserSettings.Save();
        }

        void LoadFromSettings()
        {
            string romFSPath = UserSettings.GetRomFSPath();
            if (!string.IsNullOrEmpty(romFSPath))
            {
                RomFS.SetRoot(romFSPath);
            }

            if (!ParamDB.sIsInit && !string.IsNullOrEmpty(RomFS.GetRoot()))
            {
                ParamDB.Load();
            }

            string? latestCourse = UserSettings.GetLatestCourse();
            if (latestCourse != null)
            {
                mCurrentCourseName = latestCourse;
                mSelectedCourseScene = new(new(mCurrentCourseName));
                mIsChoosingCourse = false;
            }
        }

        void DrawMainMenu()
        {
            /* create a new menubar */
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Set RomFS Path"))
                    {
                        /* open a new folder dialog to select the RomFS */
                        var dialog = new FolderDialog();
                        dialog.SelectedPath = UserSettings.GetRomFSPath();
                        if (dialog.ShowDialog("Select Your RomFS Folder..."))
                        {
                            string basePath = dialog.SelectedPath.Replace("\0", "");

                            /* set our root, but also set the root path in user setings */
                            if (!RomFS.SetRoot(basePath))
                            {
                                return;
                            }

                            UserSettings.SetRomFSPath(basePath);

                            /* if our parameter database isn't set, set it */
                            if (!ParamDB.sIsInit)
                            {
                                ParamDB.Load();
                            }
                        }
                    }

                    if (RomFS.GetRoot() != null)
                    {
                        if (ImGui.MenuItem("Open Course"))
                        {
                            mIsChoosingCourse = true;
                        }
                    }

                    /* Saves the currently loaded course */

                    var text_color = mSelectedCourseScene == null ?
                         ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled] :
                         ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(text_color));

                    if (ImGui.MenuItem("Save") && mSelectedCourseScene != null)
                    {
                        //Ensure the romfs path is set for saving
                        if (!string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                            mSelectedCourseScene.Save();
                        else //Else configure the mod path
                        {
                            FolderDialog dlg = new FolderDialog();
                            if (dlg.ShowDialog("Select the romfs directory to save to."))
                            {
                                UserSettings.SetModRomFSPath(dlg.SelectedPath);
                                mSelectedCourseScene.Save();
                            }
                        }
                    }
                    if (ImGui.MenuItem("Save As") && mSelectedCourseScene != null)
                    {
                        FolderDialog dlg = new FolderDialog();
                        if (dlg.ShowDialog("Select the romfs directory to save to."))
                        {
                            UserSettings.SetModRomFSPath(dlg.SelectedPath);
                            mSelectedCourseScene.Save();
                        }
                    }

                    ImGui.PopStyleColor();

                    /* a ImGUI menu item that just closes the application */
                    if (ImGui.MenuItem("Close"))
                    {
                        mWindow.Close();
                    }

                    /* end File menu */
                    ImGui.EndMenu();
                }
                /* end entire menu bar */
                ImGui.EndMenuBar();
            }
        }

        void DrawCourseList()
        {
            bool status = ImGui.Begin("Select Course");

            mCurrentCourseName = mSelectedCourseScene?.GetCourse().GetName();

            foreach (KeyValuePair<string, string[]> worldCourses in RomFS.GetCourseEntries())
            {
                if (ImGui.TreeNode(worldCourses.Key))
                {
                    foreach (var courseLocation in worldCourses.Value)
                    {
                        if (ImGui.RadioButton(
                                courseLocation,
                                mCurrentCourseName == null ? false : courseLocation == mCurrentCourseName
                            )
                        )
                        {
                            // Close course selection whether or not this is a different course
                            mIsChoosingCourse = false;

                            // Only change the course if it is different from current
                            if (mCurrentCourseName == null || mCurrentCourseName != courseLocation)
                            {
                                mSelectedCourseScene = new(new(courseLocation));
                                UserSettings.AppendRecentCourse(courseLocation);
                            }

                        }
                    }
                    ImGui.TreePop();
                }
            }

            if (status)
            {
                ImGui.End();
            }
        }

        void DrawWelcome()
        {
            if (ImGui.Begin("Welcome"))
            {
                ImGui.Text("Welcome to Fushigi! Set the RomFS game path and save directory to get started.");

                var romfs = UserSettings.GetRomFSPath();
                var mod = UserSettings.GetModRomFSPath();

                ImGui.Indent();

                if (PathSelector.Show("RomFS Game Path", ref romfs, Directory.Exists($"{romfs}/BancMapUnit")))
                {
                    UserSettings.SetRomFSPath(romfs);
                    RomFS.SetRoot(romfs);
                    /* if our parameter database isn't set, set it */
                    if (!ParamDB.sIsInit)
                    {
                        ParamDB.Load();
                    }
                }

                Tooltip.Show("The game files which are stored under the romfs folder.");

                if (PathSelector.Show("Save Directory", ref mod, !string.IsNullOrEmpty(mod)))
                    UserSettings.SetModRomFSPath(mod);

                Tooltip.Show("The save output where to save modified romfs files");

                ImGui.End();
            }
        }

        public void Render(GL gl, double delta, ImGuiController controller)
        {

            /* keep OpenGLs viewport size in sync with the window's size */
            gl.Viewport(mWindow.FramebufferSize);

            gl.ClearColor(.45f, .55f, .60f, 1f);
            gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            ImGui.DockSpaceOverViewport();

            //only works after the first frame
            if (ImGui.GetFrameCount() == 2)
            {
                ImGui.LoadIniSettingsFromDisk("imgui.ini");
                LoadFromSettings();
            }

            DrawMainMenu();

            // if our RomFS is selected, fill the course list
            // ImGui settings are available frame 3
            if (!string.IsNullOrEmpty(RomFS.GetRoot()) && 
                !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()) &&
                ImGui.GetFrameCount() > 2)
            {
                if (mIsChoosingCourse)
                {
                    DrawCourseList();
                } 

                mSelectedCourseScene?.DrawUI();
            }
            else
            {
                DrawWelcome();
            }

            /* render our ImGUI controller */
            controller.Render();
        }

        readonly IWindow mWindow;
        string? mCurrentCourseName;
        CourseScene? mSelectedCourseScene;
        bool mIsChoosingCourse = true;
    }
}
