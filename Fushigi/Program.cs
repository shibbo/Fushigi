
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using ImGuiNET;
using Fushigi.ui.widgets;
using Fushigi.windowing;
using Silk.NET.OpenGL.Extensions.ImGui;

WindowManager.CreateWindow(out IWindow window);

window.Load += () => WindowManager.RegisterRenderDelegate(window, DoRendering);

void DoRendering(GL gl, double delta, ImGuiController controller)
{
    // This is where you'll do any rendering beneath the ImGui context
    // Here, we just have a blank screen.
    gl.Viewport(window.Size);

    gl.ClearColor(.45f, .55f, .60f, 1f);
    gl.Clear((uint)ClearBufferMask.ColorBufferBit);


    // This is where you'll do all of your ImGUi rendering
    // Here, we're just showing the ImGui built-in demo window.
    ImGui.ShowDemoWindow();

    FilePicker fp = FilePicker.GetFilePicker(controller, "D:\\Hacking\\Switch\\Wonder\\romfs\\");
    string file = "";

    if (fp.Draw(ref file))
    {

    }

    // Make sure ImGui renders too!
    controller.Render();
}

window.Run();
window.Dispose();