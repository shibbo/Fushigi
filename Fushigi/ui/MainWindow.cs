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
                    if (mSelectedCourseScene != null)
                    {
                        if (ImGui.MenuItem("Save"))
                        {
                            mSelectedCourseScene.Save();
                        }
                        if (ImGui.MenuItem("Save As"))
                        {
                            FolderDialog dlg = new FolderDialog();
                            if (dlg.ShowDialog())
                                mSelectedCourseScene.Save(dlg.SelectedPath);
                        }
                    }

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
                                mSelectedCourseScene = new(new(courseLocation), mWindow);        
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
                ImGui.Text("Welcome to Fushigi! Visit 'File->Set RomFS Path' to get started.");

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

            if(ImGui.GetFrameCount() == 2) //only works after the first frame
                ImGui.LoadIniSettingsFromDisk("imgui.ini");

            DrawMainMenu();

            /* if our RomFS is selected, fill the course list */
            if (RomFS.GetRoot() != null)
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
