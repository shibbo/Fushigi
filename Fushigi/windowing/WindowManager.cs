using Fushigi.util;
using ImGuiNET;
using Silk.NET.Core.Contexts;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

namespace Fushigi.windowing
{
    internal static class WindowManager
    {
        public static IGLContext? SharedContext { get; private set; } = null;

        private static GL? s_gl = null;

        private record struct WindowResources(ImGuiController ImguiController, IInputContext Input, GL Gl, bool HasRenderDelegate);

        private static bool s_isRunning = false;

        private static readonly List<IWindow> s_pendingInits = [];
        private static readonly List<(IWindow window, WindowResources res)> s_windows = [];

        public static void CreateWindow(out IWindow window, Vector2D<int>? initialWindowSize = null)
        {
            var options = WindowOptions.Default;
            options.API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.Debug | ContextFlags.ForwardCompatible,
                new APIVersion(3, 3)
                );

            if (initialWindowSize.TryGetValue(out var size))
                options.Size = size;

            options.IsVisible = false;

            window = Window.Create(options);

            var _window = window;

            window.Load += () =>
            {
                if(s_gl == null)
                    s_gl = _window.CreateOpenGL();

                //initialization
                if (_window.Native!.Win32.HasValue)
                    WindowsDarkmodeUtil.SetDarkmodeAware(_window.Native.Win32.Value.Hwnd);


                ImGuiFontConfig? imGuiFontConfig = new ImGuiFontConfig("res/Font.ttf", 16);

                var input = _window.CreateInput();
                var imguiController = new ImGuiController(s_gl, _window, input, imGuiFontConfig);

                //update
                _window.Update += ds => imguiController.Update((float)ds);

                s_windows.Add((_window, new WindowResources(imguiController, input, s_gl, false)));
            };

            s_pendingInits.Add(window);
        }

        public static void RegisterRenderDelegate(IWindow window, Action<GL, double, ImGuiController> renderGLDelegate)
        {
            int idx = s_windows.FindIndex(x => x.window == window);

            if (idx == -1)
                throw new Exception($"window was not created using the {nameof(WindowManager)} class");

            var res = s_windows[idx].res;

            if(res.HasRenderDelegate)
                throw new Exception("window has already registered a render delegate");

            var isRequestShow = true;
            window.Render += (deltaSeconds) =>
            {
                res.ImguiController.MakeCurrent();

                renderGLDelegate.Invoke(res.Gl, deltaSeconds, res.ImguiController);

                if (isRequestShow)
                {
                    window.IsVisible = true;
                    isRequestShow = false;
                }
            };

            res.HasRenderDelegate = true;
            s_windows[idx] = (window, res);
        }

        public static void Run()
        {
            if (s_isRunning)
                return;

            s_isRunning = true;

            while (s_windows.Count > 0 || s_pendingInits.Count > 0)
            {
                if (s_pendingInits.Count > 0)
                {
                    foreach (var window in s_pendingInits)
                    {
                        window.Initialize();

                        if (SharedContext == null)
                            SharedContext = window.GLContext;
                    }

                    s_pendingInits.Clear();
                }


                for (int i = 0; i < s_windows.Count; i++)
                {
                    var (window, res) = s_windows[i];

                    window.DoEvents();
                    if (!window.IsClosing)
                    {
                        window.DoUpdate();
                    }

                    if (!window.IsClosing)
                    {
                        window.DoRender();
                    }

                    if (window.IsClosing)
                    {
                        s_windows.RemoveAt(i);

                        if (window.GLContext == SharedContext && s_windows.Count > 0)
                        {
                            SharedContext = s_windows[0].window.GLContext;
                        }

                        res.Input.Dispose();
                        res.ImguiController.Dispose();

                        window.DoEvents();
                        window.Reset();

                        i--;
                    }
                }
            }

            s_gl?.Dispose();
        }
    }
}