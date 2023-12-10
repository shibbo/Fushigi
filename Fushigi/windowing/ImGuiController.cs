// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;

using Silk.NET.OpenGL;

using Silk.NET.Windowing;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Fushigi.util;
using Silk.NET.Input.Extensions;
using Silk.NET.Windowing.Glfw;
using Sdl = Silk.NET.SDL.Sdl;
using Silk.NET.Windowing.Sdl;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;
using PixelType = Silk.NET.OpenGL.PixelType;


namespace Fushigi.windowing;

public class ImGuiController : IDisposable
{
    private GL _gl;
    private IWindow _window;
    private IInputContext _input;
    private bool _frameBegun;
    private readonly List<char> _pressedChars = new List<char>();
    private IKeyboard _keyboard;

    private int _attribLocationTex;
    private int _attribLocationProjMtx;
    private int _attribLocationVtxPos;
    private int _attribLocationVtxUV;
    private int _attribLocationVtxColor;
    private uint _vboHandle;
    private uint _elementsHandle;
    private uint _vertexArrayObject;

    private Texture _fontTexture;
    private Shader _shader;

    private int _windowWidth;
    private int _windowHeight;

    public IntPtr Context;

    public Func<Key, Key>? KeyRemapper;

    /// <summary>
    /// Constructs a new ImGuiController.
    /// </summary>
    public ImGuiController(GL gl, IWindow window, IInputContext input) : this(gl, window, input, null)
    {
    }

    /// <summary>
    /// Constructs a new ImGuiController with an onConfigureIO Action.
    /// </summary>
    public ImGuiController(GL gl, IWindow window, IInputContext input, Action? onConfigureIO = null)
    {
        Init(gl, window, input);

        var io = ImGuiNET.ImGui.GetIO();

        onConfigureIO?.Invoke();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        unsafe
        {
            var glfw = GlfwWindowing.GetExistingApi(window);
            var sdl = SdlWindowing.GetExistingApi(window);

            if (glfw is not null)
            {
                
                LocalizeKeyFunc = (key) =>
                {
                    if(Key.A <= key && key <= Key.Z)
                    {
                        int glfwKey = (int)Silk.NET.GLFW.Keys.A + (key - Key.A);
                        int scanCode = glfw.GetKeyScancode(glfwKey);
                        char keyNameChar = glfw.GetKeyName(glfwKey, scanCode)[0];

                        key = Key.A + (char.ToLower(keyNameChar) - 'a');
                    }

                    return key;
                };
            }
            else if (sdl is not null)
            {

                LocalizeKeyFunc = (key) =>
                {
                    if (Key.A <= key && key <= Key.Z)
                    {
                        int sdlKey = (int)Silk.NET.SDL.KeyCode.KA + (key - Key.A);
                        var scanCode = sdl.GetScancodeFromKey(sdlKey);
                        char keyNameChar = sdl.GetScancodeNameS(scanCode)[0];

                        key = Key.A + (char.ToLower(keyNameChar) - 'a');
                    }

                    return key;
                };
            }
        }

        CreateDeviceResources();

        foreach (var key in input.Keyboards[0].SupportedKeys)
        {
            if (TryMapKey(key, out ImGuiKey imguikey))
            {
                io.AddKeyEvent(imguikey, input.Keyboards[0].IsKeyPressed(key));
            }
        }

        SetPerFrameImGuiData(1f / 60f);

        BeginFrame();
    }

    public void MakeCurrent()
    {
        ImGuiNET.ImGui.SetCurrentContext(Context);
    }

    private void Init(GL gl, IWindow window, IInputContext input)
    {
        _gl = gl;
        _window = window;
        _input = input;
        _windowWidth = window.Size.X;
        _windowHeight = window.Size.Y;

        Context = ImGuiNET.ImGui.CreateContext();
        ImGuiNET.ImGui.SetCurrentContext(Context);
        ImGuiNET.ImGui.StyleColorsDark();
    }

    private void BeginFrame()
    {
        ImGuiNET.ImGui.NewFrame();
        _frameBegun = true;
        _keyboard = _input.Keyboards[0];
        _window.Resize += WindowResized;
        _keyboard.KeyChar += OnKeyChar;
        _keyboard.KeyDown += OnKeyDown;
        _keyboard.KeyUp += OnKeyUp;
    }

    private void OnKeyChar(IKeyboard arg1, char arg2)
    {
        _pressedChars.Add(arg2);
    }

    private Func<Key, Key>? LocalizeKeyFunc;

    private void OnKeyDown(IKeyboard keyboard, Key key, int scanCode)
    {
        if (key == Key.Unknown)
            return;

        if (TryMapKey(key, out ImGuiKey imguikey))
        {
            ImGui.GetIO().AddKeyEvent(imguikey, true);
        }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scanCode)
    {
        if (key == Key.Unknown)
            return;

        if (TryMapKey(key, out ImGuiKey imguikey))
        {
            ImGui.GetIO().AddKeyEvent(imguikey, false);
        }
    }

    private void WindowResized(Vector2D<int> size)
    {
        _windowWidth = size.X;
        _windowHeight = size.Y;
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
    /// or index data has increased beyond the capacity of the existing buffers.
    /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
    /// </summary>
    public void Render()
    {
        if (_frameBegun)
        {
            var oldCtx = ImGuiNET.ImGui.GetCurrentContext();

            if (oldCtx != Context)
            {
                ImGuiNET.ImGui.SetCurrentContext(Context);
            }

            _frameBegun = false;
            ImGuiNET.ImGui.Render();
            RenderImDrawData(ImGuiNET.ImGui.GetDrawData());

            if (oldCtx != Context)
            {
                ImGuiNET.ImGui.SetCurrentContext(oldCtx);
            }
        }
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds)
    {
        var oldCtx = ImGuiNET.ImGui.GetCurrentContext();

        if (oldCtx != Context)
        {
            ImGuiNET.ImGui.SetCurrentContext(Context);
        }

        if (_frameBegun)
        {
            ImGuiNET.ImGui.Render();
        }

        SetPerFrameImGuiData(deltaSeconds);

        UpdateImGuiInput();

        _frameBegun = true;
        ImGuiNET.ImGui.NewFrame();

        if (oldCtx != Context)
        {
            ImGuiNET.ImGui.SetCurrentContext(oldCtx);
        }
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth, _windowHeight);

        if (_windowWidth > 0 && _windowHeight > 0)
        {
            io.DisplayFramebufferScale = new Vector2(_window.FramebufferSize.X / _windowWidth,
                _window.FramebufferSize.Y / _windowHeight);
        }

        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }
    private bool TryMapKey(Key key, out ImGuiKey result)
        {
            ImGuiKey KeyToImGuiKeyShortcut(Key keyToConvert, Key startKey1, ImGuiKey startKey2)
            {
                int changeFromStart1 = (int)keyToConvert - (int)startKey1;
                return startKey2 + changeFromStart1;
            }

            if(LocalizeKeyFunc is not null)
                key = LocalizeKeyFunc(key);

            result = key switch
            {
                >= Key.F1 and <= Key.F24 => KeyToImGuiKeyShortcut(key, Key.F1, ImGuiKey.F1),
                >= Key.Keypad0 and <= Key.Keypad9 => KeyToImGuiKeyShortcut(key, Key.Keypad0, ImGuiKey.Keypad0),
                >= Key.A and <= Key.Z => KeyToImGuiKeyShortcut(key, Key.A, ImGuiKey.A),
                >= Key.Number0 and <= Key.Number9 => KeyToImGuiKeyShortcut(key, Key.Number0, ImGuiKey._0),
                Key.ShiftLeft => ImGuiKey.LeftShift,
                Key.ShiftRight => ImGuiKey.RightShift,
                Key.ControlLeft => ImGuiKey.LeftCtrl,
                Key.ControlRight => ImGuiKey.RightCtrl,
                Key.AltLeft => ImGuiKey.LeftAlt,
                Key.AltRight => ImGuiKey.RightAlt,
                Key.SuperLeft => ImGuiKey.LeftSuper,
                Key.SuperRight => ImGuiKey.RightSuper,
                Key.Menu => ImGuiKey.Menu,
                Key.Up => ImGuiKey.UpArrow,
                Key.Down => ImGuiKey.DownArrow,
                Key.Left => ImGuiKey.LeftArrow,
                Key.Right => ImGuiKey.RightArrow,
                Key.Enter => ImGuiKey.Enter,
                Key.Escape => ImGuiKey.Escape,
                Key.Space => ImGuiKey.Space,
                Key.Tab => ImGuiKey.Tab,
                Key.Backspace => ImGuiKey.Backspace,
                Key.Insert => ImGuiKey.Insert,
                Key.Delete => ImGuiKey.Delete,
                Key.PageUp => ImGuiKey.PageUp,
                Key.PageDown => ImGuiKey.PageDown,
                Key.Home => ImGuiKey.Home,
                Key.End => ImGuiKey.End,
                Key.CapsLock => ImGuiKey.CapsLock,
                Key.ScrollLock => ImGuiKey.ScrollLock,
                Key.PrintScreen => ImGuiKey.PrintScreen,
                Key.Pause => ImGuiKey.Pause,
                Key.NumLock => ImGuiKey.NumLock,
                Key.KeypadDivide => ImGuiKey.KeypadDivide,
                Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
                Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
                Key.KeypadAdd => ImGuiKey.KeypadAdd,
                Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
                Key.KeypadEnter => ImGuiKey.KeypadEnter,
                Key.GraveAccent => ImGuiKey.GraveAccent,
                Key.Minus => ImGuiKey.Minus,
                Key.Equal => ImGuiKey.Equal,
                Key.LeftBracket => ImGuiKey.LeftBracket,
                Key.RightBracket => ImGuiKey.RightBracket,
                Key.Semicolon => ImGuiKey.Semicolon,
                Key.Apostrophe => ImGuiKey.Apostrophe,
                Key.Comma => ImGuiKey.Comma,
                Key.Period => ImGuiKey.Period,
                Key.Slash => ImGuiKey.Slash,
                Key.BackSlash => ImGuiKey.Backslash,
                _ => ImGuiKey.None
            };

            return result != ImGuiKey.None;
        }

        internal void PressChar(char keyChar)
        {
            _pressedChars.Add(keyChar);
        }

        private void UpdateImGuiInput()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            var mouseState = _input.Mice[0].CaptureState();
            var keyboardState = _input.Keyboards[0];

            io.AddMousePosEvent(mouseState.Position.X, mouseState.Position.Y);
            io.AddMouseButtonEvent(0, mouseState.IsButtonPressed(MouseButton.Left));
            io.AddMouseButtonEvent(1, mouseState.IsButtonPressed(MouseButton.Right));
            io.AddMouseButtonEvent(2, mouseState.IsButtonPressed(MouseButton.Middle));
            io.AddMouseButtonEvent(3, mouseState.IsButtonPressed(MouseButton.Button4));
            io.AddMouseButtonEvent(4, mouseState.IsButtonPressed(MouseButton.Button5));

            io.AddMouseWheelEvent(mouseState.GetScrollWheels()[0].X, mouseState.GetScrollWheels()[0].Y);
            foreach (var c in _pressedChars)
            {
                io.AddInputCharacter(c);
            }

            _pressedChars.Clear();

            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl, ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl));
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModAlt,  ImGui.IsKeyDown(ImGuiKey.LeftAlt) || ImGui.IsKeyDown(ImGuiKey.RightAlt));
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModShift,  ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift));
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModSuper,  ImGui.IsKeyDown(ImGuiKey.LeftSuper) || ImGui.IsKeyDown(ImGuiKey.RightSuper));
        }

    private unsafe void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight)
    {
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, polygon fill
        _gl.Enable(GLEnum.Blend);
        _gl.BlendEquation(GLEnum.FuncAdd);
        _gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
        _gl.Disable(GLEnum.CullFace);
        _gl.Disable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.StencilTest);
        _gl.Enable(GLEnum.ScissorTest);
#if !GLES && !LEGACY
        _gl.Disable(GLEnum.PrimitiveRestart);
        _gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
#endif

        float L = drawDataPtr.DisplayPos.X;
        float R = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
        float T = drawDataPtr.DisplayPos.Y;
        float B = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;

        Span<float> orthoProjection = stackalloc float[] {
                2.0f / (R - L), 0.0f, 0.0f, 0.0f,
                0.0f, 2.0f / (T - B), 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, 0.0f,
                (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f,
            };

        _shader.UseShader();
        _gl.Uniform1(_attribLocationTex, 0);
        _gl.UniformMatrix4(_attribLocationProjMtx, 1, false, orthoProjection);
        _gl.CheckGlError("Projection");

        _gl.BindSampler(0, 0);

        // Setup desired GL state
        // Recreate the VAO every time (this is to easily allow multiple GL contexts to be rendered to. VAO are not shared among GL contexts)
        // The renderer would actually work without any VAO bound, but then our VertexAttrib calls would overwrite the default one currently bound.
        _vertexArrayObject = _gl.GenVertexArray();
        _gl.BindVertexArray(_vertexArrayObject);
        _gl.CheckGlError("VAO");

        // Bind vertex/index buffers and setup attributes for ImDrawVert
        _gl.BindBuffer(GLEnum.ArrayBuffer, _vboHandle);
        _gl.BindBuffer(GLEnum.ElementArrayBuffer, _elementsHandle);
        _gl.EnableVertexAttribArray((uint)_attribLocationVtxPos);
        _gl.EnableVertexAttribArray((uint)_attribLocationVtxUV);
        _gl.EnableVertexAttribArray((uint)_attribLocationVtxColor);
        _gl.VertexAttribPointer((uint)_attribLocationVtxPos, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert), (void*)0);
        _gl.VertexAttribPointer((uint)_attribLocationVtxUV, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert), (void*)8);
        _gl.VertexAttribPointer((uint)_attribLocationVtxColor, 4, GLEnum.UnsignedByte, true, (uint)sizeof(ImDrawVert), (void*)16);
    }

    private unsafe void RenderImDrawData(ImDrawDataPtr drawDataPtr)
    {
        int framebufferWidth = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
        int framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
        if (framebufferWidth <= 0 || framebufferHeight <= 0)
            return;

        // Backup GL state
        _gl.GetInteger(GLEnum.ActiveTexture, out int lastActiveTexture);
        _gl.ActiveTexture(GLEnum.Texture0);

        _gl.GetInteger(GLEnum.CurrentProgram, out int lastProgram);
        _gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);

        _gl.GetInteger(GLEnum.SamplerBinding, out int lastSampler);

        _gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);
        _gl.GetInteger(GLEnum.VertexArrayBinding, out int lastVertexArrayObject);

#if !GLES
        Span<int> lastPolygonMode = stackalloc int[2];
        _gl.GetInteger(GLEnum.PolygonMode, lastPolygonMode);
#endif

        Span<int> lastScissorBox = stackalloc int[4];
        _gl.GetInteger(GLEnum.ScissorBox, lastScissorBox);

        _gl.GetInteger(GLEnum.BlendSrcRgb, out int lastBlendSrcRgb);
        _gl.GetInteger(GLEnum.BlendDstRgb, out int lastBlendDstRgb);

        _gl.GetInteger(GLEnum.BlendSrcAlpha, out int lastBlendSrcAlpha);
        _gl.GetInteger(GLEnum.BlendDstAlpha, out int lastBlendDstAlpha);

        _gl.GetInteger(GLEnum.BlendEquationRgb, out int lastBlendEquationRgb);
        _gl.GetInteger(GLEnum.BlendEquationAlpha, out int lastBlendEquationAlpha);

        bool lastEnableBlend = _gl.IsEnabled(GLEnum.Blend);
        bool lastEnableCullFace = _gl.IsEnabled(GLEnum.CullFace);
        bool lastEnableDepthTest = _gl.IsEnabled(GLEnum.DepthTest);
        bool lastEnableStencilTest = _gl.IsEnabled(GLEnum.StencilTest);
        bool lastEnableScissorTest = _gl.IsEnabled(GLEnum.ScissorTest);

#if !GLES && !LEGACY
        bool lastEnablePrimitiveRestart = _gl.IsEnabled(GLEnum.PrimitiveRestart);
#endif

        SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);

        // Will project scissor/clipping rectangles into framebuffer space
        Vector2 clipOff = drawDataPtr.DisplayPos;         // (0,0) unless using multi-viewports
        Vector2 clipScale = drawDataPtr.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

        // Render command lists
        for (int n = 0; n < drawDataPtr.CmdListsCount; n++)
        {
            ImDrawListPtr cmdListPtr = drawDataPtr.CmdLists[n];

            // Upload vertex/index buffers

            _gl.BufferData(GLEnum.ArrayBuffer, (nuint)(cmdListPtr.VtxBuffer.Size * sizeof(ImDrawVert)), (void*)cmdListPtr.VtxBuffer.Data, GLEnum.StreamDraw);
            _gl.CheckGlError($"Data Vert {n}");
            _gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(cmdListPtr.IdxBuffer.Size * sizeof(ushort)), (void*)cmdListPtr.IdxBuffer.Data, GLEnum.StreamDraw);
            _gl.CheckGlError($"Data Idx {n}");

            for (int cmd_i = 0; cmd_i < cmdListPtr.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr cmdPtr = cmdListPtr.CmdBuffer[cmd_i];

                if (cmdPtr.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    Vector4 clipRect;
                    clipRect.X = (cmdPtr.ClipRect.X - clipOff.X) * clipScale.X;
                    clipRect.Y = (cmdPtr.ClipRect.Y - clipOff.Y) * clipScale.Y;
                    clipRect.Z = (cmdPtr.ClipRect.Z - clipOff.X) * clipScale.X;
                    clipRect.W = (cmdPtr.ClipRect.W - clipOff.Y) * clipScale.Y;

                    if (clipRect.X < framebufferWidth && clipRect.Y < framebufferHeight && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
                    {
                        // Apply scissor/clipping rectangle
                        _gl.Scissor((int)clipRect.X, (int)(framebufferHeight - clipRect.W), (uint)(clipRect.Z - clipRect.X), (uint)(clipRect.W - clipRect.Y));
                        _gl.CheckGlError("Scissor");

                        // Bind texture, Draw
                        _gl.BindTexture(GLEnum.Texture2D, (uint)cmdPtr.TextureId);
                        _gl.CheckGlError("Texture");

                        _gl.DrawElementsBaseVertex(GLEnum.Triangles, cmdPtr.ElemCount, GLEnum.UnsignedShort, (void*)(cmdPtr.IdxOffset * sizeof(ushort)), (int)cmdPtr.VtxOffset);
                        _gl.CheckGlError("Draw");
                    }
                }
            }
        }

        // Destroy the temporary VAO
        _gl.DeleteVertexArray(_vertexArrayObject);
        _vertexArrayObject = 0;

        // Restore modified GL state
        _gl.UseProgram((uint)lastProgram);
        _gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);

        _gl.BindSampler(0, (uint)lastSampler);

        _gl.ActiveTexture((GLEnum)lastActiveTexture);

        _gl.BindVertexArray((uint)lastVertexArrayObject);

        _gl.BindBuffer(GLEnum.ArrayBuffer, (uint)lastArrayBuffer);
        _gl.BlendEquationSeparate((GLEnum)lastBlendEquationRgb, (GLEnum)lastBlendEquationAlpha);
        _gl.BlendFuncSeparate((GLEnum)lastBlendSrcRgb, (GLEnum)lastBlendDstRgb, (GLEnum)lastBlendSrcAlpha, (GLEnum)lastBlendDstAlpha);

        if (lastEnableBlend)
        {
            _gl.Enable(GLEnum.Blend);
        }
        else
        {
            _gl.Disable(GLEnum.Blend);
        }

        if (lastEnableCullFace)
        {
            _gl.Enable(GLEnum.CullFace);
        }
        else
        {
            _gl.Disable(GLEnum.CullFace);
        }

        if (lastEnableDepthTest)
        {
            _gl.Enable(GLEnum.DepthTest);
        }
        else
        {
            _gl.Disable(GLEnum.DepthTest);
        }
        if (lastEnableStencilTest)
        {
            _gl.Enable(GLEnum.StencilTest);
        }
        else
        {
            _gl.Disable(GLEnum.StencilTest);
        }

        if (lastEnableScissorTest)
        {
            _gl.Enable(GLEnum.ScissorTest);
        }
        else
        {
            _gl.Disable(GLEnum.ScissorTest);
        }

#if !GLES && !LEGACY
        if (lastEnablePrimitiveRestart)
        {
            _gl.Enable(GLEnum.PrimitiveRestart);
        }
        else
        {
            _gl.Disable(GLEnum.PrimitiveRestart);
        }

        _gl.PolygonMode(GLEnum.FrontAndBack, (GLEnum)lastPolygonMode[0]);
#endif

        _gl.Scissor(lastScissorBox[0], lastScissorBox[1], (uint)lastScissorBox[2], (uint)lastScissorBox[3]);
    }

    private void CreateDeviceResources()
    {
        // Backup GL state

        _gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);
        _gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);
        _gl.GetInteger(GLEnum.VertexArrayBinding, out int lastVertexArray);

        string vertexSource =
                @"#version 330
        layout (location = 0) in vec2 Position;
        layout (location = 1) in vec2 UV;
        layout (location = 2) in vec4 Color;
        uniform mat4 ProjMtx;
        out vec2 Frag_UV;
        out vec4 Frag_Color;
        void main()
        {
            Frag_UV = UV;
            Frag_Color = Color;
            gl_Position = ProjMtx * vec4(Position.xy,0,1);
        }";


        string fragmentSource =
                @"#version 330
        in vec2 Frag_UV;
        in vec4 Frag_Color;
        uniform sampler2D Texture;
        layout (location = 0) out vec4 Out_Color;
        void main()
        {
            Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
        }";

        _shader = new Shader(_gl, vertexSource, fragmentSource);

        _attribLocationTex = _shader.GetUniformLocation("Texture");
        _attribLocationProjMtx = _shader.GetUniformLocation("ProjMtx");
        _attribLocationVtxPos = _shader.GetAttribLocation("Position");
        _attribLocationVtxUV = _shader.GetAttribLocation("UV");
        _attribLocationVtxColor = _shader.GetAttribLocation("Color");

        _vboHandle = _gl.GenBuffer();
        _elementsHandle = _gl.GenBuffer();

        RecreateFontDeviceTexture();

        // Restore modified GL state
        _gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);
        _gl.BindBuffer(GLEnum.ArrayBuffer, (uint)lastArrayBuffer);

        _gl.BindVertexArray((uint)lastVertexArray);

        _gl.CheckGlError("End of ImGui setup");
    }

    /// <summary>
    /// Creates the texture used to render text.
    /// </summary>
    private unsafe void RecreateFontDeviceTexture()
    {
        // Build texture atlas
        var io = ImGuiNET.ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);   // Load as RGBA 32-bit (75% of the memory is wasted, but default font is so small) because it is more likely to be compatible with user's existing shaders. If your ImTextureId represent a higher-level concept than just a GL texture id, consider calling GetTexDataAsAlpha8() instead to save on GPU memory.

        // Upload texture to graphics system
        _gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);

        _fontTexture = new Texture(_gl, width, height, pixels);
        _fontTexture.Bind();
        _fontTexture.SetMagFilter(TextureMagFilter.Linear);
        _fontTexture.SetMinFilter(TextureMinFilter.Linear);

        // Store our identifier
        io.Fonts.SetTexID((IntPtr)_fontTexture.GlTexture);

        // Restore state
        _gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        _window.Resize -= WindowResized;
        _keyboard.KeyChar -= OnKeyChar;
        _keyboard.KeyDown -= OnKeyDown;
        _keyboard.KeyUp -= OnKeyUp;

        _gl.DeleteBuffer(_vboHandle);
        _gl.DeleteBuffer(_elementsHandle);
        _gl.DeleteVertexArray(_vertexArrayObject);

        _fontTexture.Dispose();
        _shader.Dispose();

        ImGuiNET.ImGui.DestroyContext(Context);
    }



    enum TextureCoordinate
    {
        S = TextureParameterName.TextureWrapS,
        T = TextureParameterName.TextureWrapT,
        R = TextureParameterName.TextureWrapR
    }

    class Texture : IDisposable
    {
        public const SizedInternalFormat Srgb8Alpha8 = (SizedInternalFormat)GLEnum.Srgb8Alpha8;
        public const SizedInternalFormat Rgb32F = (SizedInternalFormat)GLEnum.Rgb32f;

        public const GLEnum MaxTextureMaxAnisotropy = (GLEnum)0x84FF;

        public static float? MaxAniso;
        private readonly GL _gl;
        public readonly string Name;
        public readonly uint GlTexture;
        public readonly uint Width, Height;
        public readonly uint MipmapLevels;
        public readonly SizedInternalFormat InternalFormat;

        public unsafe Texture(GL gl, int width, int height, IntPtr data, bool generateMipmaps = false, bool srgb = false)
        {
            _gl = gl;
            MaxAniso ??= gl.GetFloat(MaxTextureMaxAnisotropy);
            Width = (uint)width;
            Height = (uint)height;
            InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;
            MipmapLevels = (uint)(generateMipmaps == false ? 1 : (int)Math.Floor(Math.Log(Math.Max(Width, Height), 2)));

            GlTexture = _gl.GenTexture();
            Bind();

            PixelFormat pxFormat = PixelFormat.Bgra;

            _gl.TexStorage2D(GLEnum.Texture2D, MipmapLevels, InternalFormat, Width, Height);
            _gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, Width, Height, pxFormat, PixelType.UnsignedByte, (void*)data);

            if (generateMipmaps)
                _gl.GenerateTextureMipmap(GlTexture);

            SetWrap(TextureCoordinate.S, TextureWrapMode.Repeat);
            SetWrap(TextureCoordinate.T, TextureWrapMode.Repeat);

            _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLevel, MipmapLevels - 1);
        }

        public void Bind()
        {
            _gl.BindTexture(GLEnum.Texture2D, GlTexture);
        }

        public void SetMinFilter(TextureMinFilter filter)
        {
            _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
        }

        public void SetMagFilter(TextureMagFilter filter)
        {
            _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
        }

        public void SetAnisotropy(float level)
        {
            const TextureParameterName textureMaxAnisotropy = (TextureParameterName)0x84FE;
            _gl.TexParameter(GLEnum.Texture2D, (GLEnum)textureMaxAnisotropy, MathUtil.Clamp(level, 1, MaxAniso.GetValueOrDefault()));
        }

        public void SetLod(int @base, int min, int max)
        {
            _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureLodBias, @base);
            _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinLod, min);
            _gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLod, max);
        }

        public void SetWrap(TextureCoordinate coord, TextureWrapMode mode)
        {
            _gl.TexParameterI(GLEnum.Texture2D, (TextureParameterName)coord, (int)mode);
        }

        public void Dispose()
        {
            _gl.DeleteTexture(GlTexture);
        }
    }

    struct UniformFieldInfo
    {
        public int Location;
        public string Name;
        public int Size;
        public UniformType Type;
    }

    class Shader
    {
        public uint Program { get; private set; }
        private readonly Dictionary<string, int> _uniformToLocation = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _attribLocation = new Dictionary<string, int>();
        private bool _initialized = false;
        private GL _gl;
        private (ShaderType Type, string Path)[] _files;

        public Shader(GL gl, string vertexShader, string fragmentShader)
        {
            _gl = gl;
            _files = new[]{
                (ShaderType.VertexShader, vertexShader),
                (ShaderType.FragmentShader, fragmentShader),
            };
            Program = CreateProgram(_files);
        }
        public void UseShader()
        {
            _gl.UseProgram(Program);
        }

        public void Dispose()
        {
            if (_initialized)
            {
                _gl.DeleteProgram(Program);
                _initialized = false;
            }
        }

        public UniformFieldInfo[] GetUniforms()
        {
            _gl.GetProgram(Program, GLEnum.ActiveUniforms, out var uniformCount);

            UniformFieldInfo[] uniforms = new UniformFieldInfo[uniformCount];

            for (int i = 0; i < uniformCount; i++)
            {
                string name = _gl.GetActiveUniform(Program, (uint) i, out int size, out UniformType type);

                UniformFieldInfo fieldInfo;
                fieldInfo.Location = GetUniformLocation(name);
                fieldInfo.Name = name;
                fieldInfo.Size = size;
                fieldInfo.Type = type;

                uniforms[i] = fieldInfo;
            }

            return uniforms;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetUniformLocation(string uniform)
        {
            if (_uniformToLocation.TryGetValue(uniform, out int location) == false)
            {
                location = _gl.GetUniformLocation(Program, uniform);
                _uniformToLocation.Add(uniform, location);

                if (location == -1)
                {
                    Debug.Print($"The uniform '{uniform}' does not exist in the shader!");
                }
            }

            return location;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetAttribLocation(string attrib)
        {
            if (_attribLocation.TryGetValue(attrib, out int location) == false)
            {
                location = _gl.GetAttribLocation(Program, attrib);
                _attribLocation.Add(attrib, location);

                if (location == -1)
                {
                    Debug.Print($"The attrib '{attrib}' does not exist in the shader!");
                }
            }

            return location;
        }

        private uint CreateProgram(params (ShaderType Type, string source)[] shaderPaths)
        {
            var program = _gl.CreateProgram();

            Span<uint> shaders = stackalloc uint[shaderPaths.Length];
            for (int i = 0; i < shaderPaths.Length; i++)
            {
                shaders[i] = CompileShader(shaderPaths[i].Type, shaderPaths[i].source);
            }

            foreach (var shader in shaders)
                _gl.AttachShader(program, shader);

            _gl.LinkProgram(program);

            _gl.GetProgram(program, GLEnum.LinkStatus, out var success);
            if (success == 0)
            {
                string info = _gl.GetProgramInfoLog(program);
                Debug.WriteLine($"GL.LinkProgram had info log:\n{info}");
            }

            foreach (var shader in shaders)
            {
                _gl.DetachShader(program, shader);
                _gl.DeleteShader(shader);
            }

            _initialized = true;

            return program;
        }

        private uint CompileShader(ShaderType type, string source)
        {
            var shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var success);
            if (success == 0)
            {
                string info = _gl.GetShaderInfoLog(shader);
                Debug.WriteLine($"GL.CompileShader for shader [{type}] had info log:\n{info}");
            }

            return shader;
        }
    }
}

internal static class GLExtensions
{
    [Conditional("DEBUG")]
    public static void CheckGlError(this GL gl, string title)
    {
        var error = gl.GetError();
        if (error != GLEnum.NoError)
        {
            Debug.Print($"{title}: {error}");
        }
    }
}
