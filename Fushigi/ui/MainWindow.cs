using Fushigi.util;
using Fushigi.windowing;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Fushigi.param;
using Fushigi.ui.widgets;
using ImGuiNET;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using System.Diagnostics;

namespace Fushigi.ui
{
    public class MainWindow
    {

        private ImFontPtr mDefaultFont;
        private ImFontPtr mIconFont;

        public MainWindow()
        {
            WindowManager.CreateWindow(out mWindow, 
                onConfigureIO: () => { 
                    unsafe
                    {
                        var io = ImGui.GetIO();

                        var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                        //Add a higher horizontal/vertical sample rate for global scaling.
                        nativeConfig->OversampleH = 8;
                        nativeConfig->OversampleV = 8;
                        nativeConfig->RasterizerMultiply = 1f;
                        nativeConfig->GlyphOffset = new Vector2(0);
                        {
                            mDefaultFont = io.Fonts.AddFontFromFileTTF(
                                Path.Combine("res", "Font.ttf"),
                                16, nativeConfig, io.Fonts.GetGlyphRangesJapanese());

                            //other fonts go here and follow the same schema

                            nativeConfig->MergeMode = 1;

                            GCHandle rangeHandle = GCHandle.Alloc(new ushort[] { IconUtil.MIN_GLYPH_RANGE, IconUtil.MAX_GLYPH_RANGE, 0 }, GCHandleType.Pinned);
                            try
                            {
                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-regular-400.ttf"),
                                    16, nativeConfig, rangeHandle.AddrOfPinnedObject());

                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-solid-900.ttf"),
                                    16, nativeConfig, rangeHandle.AddrOfPinnedObject());

                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-brands-400.ttf"),
                                    16, nativeConfig, rangeHandle.AddrOfPinnedObject());

                                io.Fonts.Build();
                            }
                            finally
                            {
                                if (rangeHandle.IsAllocated)
                                    rangeHandle.Free();
                            }
                        }
                    }
                });
            mWindow.Load += () => WindowManager.RegisterRenderDelegate(mWindow, Render);
            mWindow.Closing += Close;
            mWindow.Run();
            mWindow.Dispose();
        }

        public void Close()
        {
            UserSettings.Save();
        }

        void LoadFromSettings(GL gl)
        {
            string romFSPath = UserSettings.GetRomFSPath();
            if (RomFS.IsValidRoot(romFSPath))
            {
                RomFS.SetRoot(romFSPath);
                ChildActorParam.Load();
            }

            if (!string.IsNullOrEmpty(RomFS.GetRoot()) &&
                !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
            {
                mIsChoosingPreferences = false;
                mIsWelcome = false;
            }

            if (!ParamDB.sIsInit && !string.IsNullOrEmpty(RomFS.GetRoot()))
            {
                Console.WriteLine("Parameter database needs to be initialized...");
                ParamDB.Load();
            }

            string? latestCourse = UserSettings.GetLatestCourse();
            if (latestCourse != null)
            {
                mCurrentCourseName = latestCourse;
                mSelectedCourseScene = new(new(mCurrentCourseName), gl);
                mIsChoosingCourse = false;
                mIsChoosingPreferences = false;
                mIsWelcome = false;
            }
        }

        void DrawMainMenu(GL gl)
        {
            /* create a new menubar */
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {                   
                    if (!string.IsNullOrEmpty(RomFS.GetRoot()) && 
                        !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
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
                                Console.WriteLine($"Setting RomFS path to {dlg.SelectedPath}");
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

                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Preferences"))
                    {
                        mIsChoosingPreferences = true;
                    }

                    if (ImGui.MenuItem("Regenerate Parameter Database", ParamDB.sIsInit)) {
                        ParamDB.sIsInit = false;
                        ParamDB.Reload();
                    }

                    /* end Edit menu */
                    ImGui.EndMenu();
                }

                /* end entire menu bar */
                ImGui.EndMenuBar();
            }
        }

        void DrawCourseList(GL gl)
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
                                Console.WriteLine($"Selected course {courseLocation}!");
                                mSelectedCourseScene = new(new(courseLocation), gl);
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
            if (!ImGui.Begin("Welcome"))
            {
                return;
            }

            ImGui.Text("Welcome to Fushigi! Set the RomFS game path and save directory to get started.");

            if (ImGui.Button("Close"))
            {
                mIsWelcome = false;
            }

            ImGui.End();
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
                LoadFromSettings(gl);
            }

            DrawMainMenu(gl);

            // ImGui settings are available frame 3
            if (ImGui.GetFrameCount() > 2)
            {
                if (!string.IsNullOrEmpty(RomFS.GetRoot()) && 
                    !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                {
                    if (mIsChoosingCourse)
                    {
                        DrawCourseList(gl);
                    }

                    mSelectedCourseScene?.DrawUI(gl);
                }

                if (mIsChoosingPreferences)
                {
                    Preferences.Draw(ref mIsChoosingPreferences);
                }

                if (mIsWelcome)
                {
                    DrawWelcome();
                }
            }

            /* render our ImGUI controller */
            controller.Render();
        }

        readonly IWindow mWindow;
        string? mCurrentCourseName;
        CourseScene? mSelectedCourseScene;
        bool mIsChoosingCourse = true;
        bool mIsChoosingPreferences = true;
        bool mIsWelcome = true;
    }
}
