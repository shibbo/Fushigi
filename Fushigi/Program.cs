
using System.Drawing;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;

using var window = Window.Create(WindowOptions.Default);
ImGuiController controller = null;
GL gl = null;
IInputContext inputContext = null;

window.Load += () =>
{
    controller = new ImGuiController(
        gl = window.CreateOpenGL(), // load OpenGL
        window, // pass in our window
        inputContext = window.CreateInput() // create an input context
    );
};

window.FramebufferResize += s =>
{
    // Adjust the viewport to the new window size
    gl.Viewport(s);
};

window.Render += delta =>
{
    // Make sure ImGui is up-to-date
    controller.Update((float)delta);

    // This is where you'll do any rendering beneath the ImGui context
    // Here, we just have a blank screen.
    gl.ClearColor(Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
    gl.Clear((uint)ClearBufferMask.ColorBufferBit);

    // This is where you'll do all of your ImGUi rendering
    // Here, we're just showing the ImGui built-in demo window.
    ImGuiNET.ImGui.ShowDemoWindow();

    // Make sure ImGui renders too!
    controller.Render();
};

window.Closing += () =>
{
    // Dispose our controller first
    controller?.Dispose();

    // Dispose the input context
    inputContext?.Dispose();

    // Unload OpenGL
    gl?.Dispose();
};

window.Run();
window.Dispose();