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
using Fushigi.Bfres;
using Fushigi.SARC;
using Fushigi.gl.Bfres;

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
                        io.ConfigFlags = ImGuiConfigFlags.NavEnableKeyboard;

                        var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                        var iconConfig = ImGuiNative.ImFontConfig_ImFontConfig();

                        //Add a higher horizontal/vertical sample rate for global scaling.
                        nativeConfig->OversampleH = 8;
                        nativeConfig->OversampleV = 8;
                        nativeConfig->RasterizerMultiply = 1f;
                        nativeConfig->GlyphOffset = new Vector2(0);

                        iconConfig->MergeMode = 1;
                        iconConfig->OversampleH = 2;
                        iconConfig->OversampleV = 2;
                        iconConfig->RasterizerMultiply = 1f;
                        iconConfig->GlyphOffset = new Vector2(0);

                        float size = 16;

                        {
                            mDefaultFont = io.Fonts.AddFontFromFileTTF(
                                Path.Combine("res", "Font.ttf"),
                                size, nativeConfig, io.Fonts.GetGlyphRangesJapanese());

                            //other fonts go here and follow the same schema

                            GCHandle rangeHandle = GCHandle.Alloc(new ushort[] { IconUtil.MIN_GLYPH_RANGE, IconUtil.MAX_GLYPH_RANGE, 0 }, GCHandleType.Pinned);
                            try
                            {
                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-regular-400.ttf"),
                                    size, iconConfig, rangeHandle.AddrOfPinnedObject());

                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-solid-900.ttf"),
                                    size, iconConfig, rangeHandle.AddrOfPinnedObject());

                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-brands-400.ttf"),
                                    size, iconConfig, rangeHandle.AddrOfPinnedObject());

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

        public bool TryCloseCourse(Action onSuccessRetryAction)
        {
            if (mCloseCourseRequest.TryGetValue(out var request))
            {
                if (request.success)
                {
                    mCloseCourseRequest = null;
                    return true;
                }
            }

            if(mSelectedCourseScene is not null &&
                mSelectedCourseScene.HasUnsavedChanges())
            {
                mCloseCourseRequest = (onSuccessRetryAction, success: false);
                return false;
            }

            return true;
        }

        public void Close()
        {
            if (!TryCloseCourse(onSuccessRetryAction: mWindow.Close))
            {
                mWindow.IsClosing = false;
                return;
            }

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

            ActorIconLoader.Init();

            if (!string.IsNullOrEmpty(RomFS.GetRoot()) &&
                !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
            {
                mIsChoosingPreferences = false;
                mIsWelcome = false;
            }

            if (!ParamDB.sIsInit && !string.IsNullOrEmpty(RomFS.GetRoot()))
            {
                Console.WriteLine("Parameter database needs to be initialized...");
                mIsGeneratingParamDB = true;
            }

            string? latestCourse = UserSettings.GetLatestCourse();
            if (latestCourse != null && ParamDB.sIsInit)
            {
                mCurrentCourseName = latestCourse;
                mSelectedCourseScene = new(new(mCurrentCourseName), gl);
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
                            void SwitchCourse(string courseLocation)
                            {
                                if (mCurrentCourseName == courseLocation)
                                {
                                    mCourseSelect = null;
                                    return;
                                }                                 

                                if (!TryCloseCourse(onSuccessRetryAction: () => SwitchCourse(courseLocation)))
                                    return;

                                Console.WriteLine($"Selected course {courseLocation}!");

                                mCurrentCourseName = courseLocation;
                                mSelectedCourseScene = new(new(mCurrentCourseName), gl);
                                mCourseSelect = null;
                                UserSettings.AppendRecentCourse(courseLocation);
                            }

                            mCourseSelect = new(gl, SwitchCourse, mCurrentCourseName);
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
                        mIsGeneratingParamDB = true;
                    }

                    if (ImGui.MenuItem("Undo"))
                    {
                        mSelectedCourseScene?.activeViewport.mEditContext.Undo();
                    }

                    if (ImGui.MenuItem("Redo"))
                    {
                        mSelectedCourseScene?.activeViewport.mEditContext.Redo();
                    }

                    /* end Edit menu */
                    ImGui.EndMenu();
                }

                /* end entire menu bar */
                ImGui.EndMenuBar();
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
                    if (mCourseSelect != null)
                    {
                        mCourseSelect.Draw();
                    }

                    mSelectedCourseScene?.DrawUI(gl, delta);
                }

                if (mIsChoosingPreferences)
                {
                    Preferences.Draw(ref mIsChoosingPreferences);
                }

                if (mIsWelcome)
                {
                    DrawWelcome();
                }

                if (mIsGeneratingParamDB)
                {
                    ParamDBDialog.Draw(ref mIsGeneratingParamDB);
                }

                bool hasRequest = mCloseCourseRequest.TryGetValue(out var request);
                bool hasResult = CloseConfirmationDialog.Draw(hasRequest, out var result);

                if (hasRequest && hasResult) //just to make sure
                {
                    if (result == CloseConfirmationDialog.Result.Yes)
                    {
                        mSelectedCourseScene = null;
                        mCloseCourseRequest = request with { success = true };
                        request.onSuccessRetryAction.Invoke();
                    }
                    else if(result == CloseConfirmationDialog.Result.No)
                    {
                        mCloseCourseRequest = null;
                    }
                }
            }

            //Update viewport from any framebuffers being used
            gl.Viewport(mWindow.FramebufferSize);

            /* render our ImGUI controller */
            controller.Render();
        }

        readonly IWindow mWindow;
        string? mCurrentCourseName;
        CourseScene? mSelectedCourseScene;
        CourseSelect? mCourseSelect = null;
        bool mIsChoosingPreferences = true;
        bool mIsWelcome = true;
        bool mIsGeneratingParamDB = false;
        (Action onSuccessRetryAction, bool success)? mCloseCourseRequest = null;
    }
}
