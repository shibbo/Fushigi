using Fushigi.param;
using Fushigi.ui.modal;
using Fushigi.ui.widgets;
using Fushigi.util;
using Fushigi.windowing;
using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Fushigi.ui
{
    public class MainWindow : IPopupModalHost
    {

        private PopupModalHost mModalHost = new();

        private ImFontPtr mDefaultFont;
        private ImFontPtr mIconFont;

        public MainWindow()
        {
            WindowManager.CreateWindow(out mWindow,
                onConfigureIO: () =>
                {
                    unsafe
                    {
                        var io = ImGui.GetIO();
                        io.ConfigFlags = ImGuiConfigFlags.NavEnableKeyboard;

                        var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                        var iconConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                        var nativeConfigJP = ImGuiNative.ImFontConfig_ImFontConfig();

                        //Add a higher horizontal/vertical sample rate for global scaling.
                        nativeConfig->OversampleH = 8;
                        nativeConfig->OversampleV = 8;
                        nativeConfig->RasterizerMultiply = 1f;
                        nativeConfig->GlyphOffset = new Vector2(0);

                        nativeConfigJP->MergeMode = 1;
                        nativeConfigJP->PixelSnapH = 1;

                        iconConfig->MergeMode = 1;
                        iconConfig->OversampleH = 2;
                        iconConfig->OversampleV = 2;
                        iconConfig->RasterizerMultiply = 1f;
                        iconConfig->GlyphOffset = new Vector2(0);

                        float size = 16;

                        {
                            mDefaultFont = io.Fonts.AddFontFromFileTTF(
                                Path.Combine("res", "Font.ttf"),
                                size, nativeConfig, io.Fonts.GetGlyphRangesDefault());

                             io.Fonts.AddFontFromFileTTF(
                                Path.Combine("res", "NotoSansCJKjp-Medium.otf"),
                                    size, nativeConfigJP, io.Fonts.GetGlyphRangesJapanese());

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

        public async Task<bool> TryCloseCourse()
        {
            if (mSelectedCourseScene is not null &&
                mSelectedCourseScene.HasUnsavedChanges())
            {
                var result = await CloseConfirmationDialog.ShowDialog(this);

                if (result == CloseConfirmationDialog.DialogResult.Yes)
                {
                    mSelectedCourseScene = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        bool mSkipCloseTest = false;
        public void Close()
        {
            //prevent infinite loop
            if (mSkipCloseTest)
            {
                UserSettings.Save();
                return;
            }

            mWindow.IsClosing = false;

            Task.Run(async () =>
            {
                if(await TryCloseCourse())
                {
                    mSkipCloseTest = true;
                    mWindow.Close();
                }

            }).ConfigureAwait(false); //fire and forget
        }

        void LoadFromSettings(GL gl)
        {
            string romFSPath = UserSettings.GetRomFSPath();
            if (RomFS.IsValidRoot(romFSPath))
            {
                RomFS.SetRoot(romFSPath, gl);
                ChildActorParam.Load();

                if (!ParamDB.sIsInit)
                {
                    Console.WriteLine("Parameter database needs to be initialized...");
                    mIsGeneratingParamDB = true;
                }

                string? latestCourse = UserSettings.GetLatestCourse();
                if (latestCourse != null && ParamDB.sIsInit)
                {
                    mCurrentCourseName = latestCourse;
                    mSelectedCourseScene = new(new(mCurrentCourseName), gl, this);
                    mIsChoosingPreferences = false;
                    mIsWelcome = false;
                }
            }

            ActorIconLoader.Init();

            if (!string.IsNullOrEmpty(RomFS.GetRoot()) &&
                !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
            {
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
                            Task.Run(async () =>
                            {
                                string? selectedCourse = await CourseSelect.ShowDialog(this, mCurrentCourseName);

                                if (selectedCourse is null || mCurrentCourseName == selectedCourse)
                                    return;

                                if (await TryCloseCourse())
                                {
                                    mCurrentCourseName = selectedCourse;
                                    Console.WriteLine($"Selected course {mCurrentCourseName}!");
                                    mSelectedCourseScene = new(new(mCurrentCourseName), gl, this);
                                    UserSettings.AppendRecentCourse(mCurrentCourseName);
                                }
                            }).ConfigureAwait(false); //fire and forget
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
                    if (ImGui.MenuItem("Blank out baked collision [EXPERIMENTAL]") && mSelectedCourseScene != null)
                    {
                        string directory = Path.Combine(UserSettings.GetModRomFSPath(), "Phive", "StaticCompoundBody");

                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        foreach (var area in mSelectedCourseScene.GetCourse().GetAreas())
                        {
                            var filePath = Path.Combine(directory, $"{area.GetName()}.Nin_NX_NVN.bphsc.zs");
                            File.Copy(Path.Combine(AppContext.BaseDirectory, "res", "BlankStaticCompoundBody.bphsc.zs"),
                                filePath, overwrite: true);
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

                    if (ImGui.MenuItem("Regenerate Parameter Database", ParamDB.sIsInit))
                    {
                        mIsGeneratingParamDB = true;
                    }

                    if (ImGui.MenuItem("Undo"))
                    {
                        mSelectedCourseScene?.Undo();
                    }

                    if (ImGui.MenuItem("Redo"))
                    {
                        mSelectedCourseScene?.Redo();
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
                    mSelectedCourseScene?.DrawUI(gl, delta);
                }

                if (mIsChoosingPreferences)
                {
                    Preferences.Draw(ref mIsChoosingPreferences, gl);
                }

                if (mIsWelcome)
                {
                    DrawWelcome();
                }

                if (mIsGeneratingParamDB)
                {
                    ParamDBDialog.Draw(ref mIsGeneratingParamDB);
                }
            }

            mModalHost.DrawHostedModals();

            //Update viewport from any framebuffers being used
            gl.Viewport(mWindow.FramebufferSize);

            /* render our ImGUI controller */
            controller.Render();
        }

        public Task<(bool wasClosed, TResult result)> ShowPopUp<TResult>(IPopupModal<TResult> modal,
            string title,
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None,
            Vector2? minWindowSize = null)
        {
            return mModalHost.ShowPopUp(modal, title, windowFlags, minWindowSize);
        }

        readonly IWindow mWindow;
        string? mCurrentCourseName;
        CourseScene? mSelectedCourseScene;
        bool mIsChoosingPreferences = true;
        bool mIsWelcome = true;
        bool mIsGeneratingParamDB = false;
    }
}
