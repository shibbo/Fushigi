using Fushigi.agl;
using Fushigi.env;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.ui.widgets
{
    public class AglCurveEditor
    {
        public List<Key> Keys = new List<Key>();

        private float[] CurveData;
        private AglCurve.CurveType Type;
        private EnvPalette.EnvSkyLut Lut;

        private Vector2 mouseDownPos;
        private bool mouseDown = false;

        private List<Key> selectedKeys = new List<Key>();

        private int KeyCount = 1;

        public void Load(EnvPalette.EnvSkyLut lut)
        {
            Lut = lut;
            Type = lut.Curve.GetCurveType();
            CurveData = lut.Curve.Data.ToArray();

            Keys.Clear();

            switch (Type)
            {
                case AglCurve.CurveType.Linear: //value list
                    LoadLinear(CurveData);
                    break;
                case AglCurve.CurveType.Hermit: //value + slope
                    LoadHermite(CurveData);
                    break;
                case AglCurve.CurveType.Hermit2D:
                case AglCurve.CurveType.Hermit2DSmooth://value + in slope + out slope
                    LoadHermite2D(CurveData);
                    break;
            }
            KeyCount = Keys.Count;
        }

        public void Render(int height)
        {
            var width = ImGui.GetWindowWidth() - 200;

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 55);

            ImGui.PushItemWidth(200);
            if (ImGui.BeginCombo("", this.Type.ToString()))
            {
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();

            ImGui.SameLine();

            ImGui.PushItemWidth(200);
            if (ImGui.InputInt("Key Count", ref KeyCount, 1, 1))
            {
                //Keep set to 1 for no empty keys
                if (KeyCount <= 0)
                    KeyCount = 1;

                //Only increase by 1 for simplicity
                if (KeyCount > Keys.Count)
                    KeyCount = Keys.Count + 1;

                if (Keys.Count < KeyCount)
                    Keys.Add(new Key());
                else if (Keys.Count > KeyCount)
                {
                    var last = Keys.Last();
                    if (last.Value == 0)
                        Keys.Remove(last);
                }

                GenerateCurve();
            }
            ImGui.PopItemWidth();

            var pos = ImGui.GetCursorPos();

            for (float i = 0; i <= 1.1f; i += 0.1f)
            {
                ImGui.SetCursorPosY((pos.Y + 80) + (1 - i) * height - 8);

                ImGui.Text($"{MathF.Round(i, 2)}");
            }

            ImGui.SetCursorPosX(pos.X + 55);
            ImGui.SetCursorPosY(pos.Y);

            DrawGradientTop(Lut, "gradient");

            ImGui.SetCursorPosX(pos.X + 55);
            ImGui.SetCursorPosY(pos.Y + 80);

            var screenPos = ImGui.GetCursorScreenPos();
            var scaleFactor = new Vector2(width, height);

            CalculateGradientBG(Lut, screenPos, (int)width, height);

            RenderGrid(width, height);

            RenderInterpolatedCurve(width, height);

            Vector4[] colors = Lut.UseMiddleColor ?
          new Vector4[] { Lut.ColorBegin.ToVector4(), Lut.ColorMiddle.ToVector4(), Lut.ColorEnd.ToVector4() } :
          new Vector4[] { Lut.ColorBegin.ToVector4(), Lut.ColorEnd.ToVector4(), Lut.ColorEnd.ToVector4() };

            Vector4 LerpBetweenColors(Vector4 a, Vector4 b, Vector4 c, float t)
            {
                //bottom to top
                if (!Lut.UseMiddleColor)
                    return Vector4.Lerp(a, b, t);

                if (t < 0.5f) // Bottom to middle
                    return Vector4.Lerp(a, b, t / 0.5f);
                else // Middle to top
                    return Vector4.Lerp(b, c, (t - 0.5f) / 0.5f);
            }

            if (ImGui.IsMouseDown(0) && ImGui.IsMouseClicked(0))
            {
                mouseDownPos = ImGui.GetMousePos();
                mouseDown = true;
                selectedKeys.Clear();
            }

            if (ImGui.IsMouseReleased(0))
            {
                mouseDown = false;
                foreach (var key in selectedKeys)
                {
                    key.PrevValue = key.Value;
                }
            }

            if (mouseDown && selectedKeys.Count > 0)
            {
                var delta = ImGui.GetMousePos() - mouseDownPos;
                var diff =  delta.Y * (1.0f / height);
                foreach (var key in selectedKeys)
                    key.Value = Math.Clamp(key.PrevValue - diff, 0, 1f);

                GenerateCurve();
            }

            for (int i = 0; i < KeyCount; i++)
            {
                float time = i / (KeyCount - 1f);

                //Every key is fixed to a frame based on the number of keys used
                float x = i == 0 ? 0 : width * time;

                var keyPos = screenPos + new Vector2(x, height - Keys[i].Value * height);

                float slope_length = 0.06f;

                Vector2 cpOut = keyPos + ((Vector2.Normalize(new Vector2(1, Keys[i].SlopeOut)) * -slope_length) * scaleFactor);
                Vector2 cpIn = keyPos + ((Vector2.Normalize(new Vector2(1, Keys[i].SlopeIn)) * slope_length) * scaleFactor);

                var color = LerpBetweenColors(colors[0], colors[1], colors[2], Keys[i].Value);

                bool hovered = (ImGui.GetMousePos() - keyPos).Length() < 10.0f;

                if (hovered)
                {
                    color = new Vector4(1, 1, 0, 1);
                }
                if (hovered && ImGui.IsMouseClicked(0))
                {
                    selectedKeys.Clear();
                    selectedKeys.Add(Keys[i]);

                    Keys[i].PrevValue = Keys[i].Value;
                    Keys[i].PrevSlopeIn = Keys[i].SlopeIn;
                    Keys[i].PrevSlopeOut = Keys[i].SlopeOut;

                    mouseDown = true;
                    mouseDownPos = ImGui.GetMousePos();
                }

                if (selectedKeys.Contains(Keys[i]))
                {
                    color = new Vector4(1, 0, 0, 1);
                }

                var key_color = ImGui.ColorConvertFloat4ToU32(color);

                ImGui.GetWindowDrawList().AddLine(keyPos, cpIn, 0xFFFFFFFF);
                ImGui.GetWindowDrawList().AddLine(keyPos, cpOut, 0xFFFFFFFF);

                ImGui.GetWindowDrawList().AddCircleFilled(keyPos, 10, 0xFFFFFFFF);
                ImGui.GetWindowDrawList().AddCircleFilled(keyPos, 8, key_color);

                ImGui.GetWindowDrawList().AddCircleFilled(cpOut, 5, 0xFFFFFFFF);
                ImGui.GetWindowDrawList().AddCircleFilled(cpIn, 5, 0xFFFFFFFF);
            }
        }

        private void GenerateCurve()
        {
            if (this.Type == AglCurve.CurveType.Hermit)
            {
                float[] data = new float[KeyCount * 2];
                for (int i = 0; i < KeyCount; i++)
                {
                    data[i * 2] = this.Keys[i].Value;
                    data[i * 2 + 1] = this.Keys[i].SlopeIn;
                }
                this.Lut.Curve.Data = data.ToList();
            }
            else if (this.Type == AglCurve.CurveType.Hermit2D)
            {
                float[] data = new float[KeyCount * 3];
                for (int i = 0; i < KeyCount; i++)
                {
                    data[i * 3] = this.Keys[i].Value;
                    data[i * 3 + 1] = this.Keys[i].SlopeIn;
                    data[i * 3 + 2] = this.Keys[i].SlopeOut;
                }
                this.Lut.Curve.Data = data.ToList();
            }
            else
            {
                float[] data = new float[KeyCount];
                for (int i = 0; i < KeyCount; i++)
                    data[i] = this.Keys[i].Value;

                this.Lut.Curve.Data = data.ToList();
            }
            this.CurveData = this.Lut.Curve.Data.ToArray();
        }

        public void RenderGrid(float width, float height)
        {
            var screenPos = ImGui.GetCursorScreenPos();

            int num_w = 11;
            int num_h = 11;

            for (int x = 0; x < num_w; x++)
            {
                for (int y = 0; y < num_h; y++)
                {
                    float posX = x * 0.1f * width;
                    float posY = height - y * 0.1f * height;

                    ImGui.GetWindowDrawList().AddLine(
                        screenPos + new Vector2(posX, posY), 
                        screenPos + new Vector2((width * (num_w - 1) * 0.1f), posY), 0xFF555555);

                    ImGui.GetWindowDrawList().AddLine(
                        screenPos + new Vector2(posX, posY),
                        screenPos + new Vector2(posX, (num_h - 1) * 0.1f), 0xFF555555);
                }
            }
        }

        public void RenderInterpolatedCurve(float width, float height)
        {
            var screenPos = ImGui.GetCursorScreenPos();

            Vector2 GetCurvePos(int index)
            {
                float time = index / (height - 1f);

                float x = MathF.Min(AglCurve.Interpolate(CurveData, Type, time), 1f);
                return new Vector2((time * width), height - x * height);
            }

            for (int i = 1; i < height; i++)
            {
                var pos = screenPos + GetCurvePos(i - 1);
                var pos2 = screenPos + GetCurvePos(i);

                ImGui.GetWindowDrawList().AddLine(pos, pos2, 0xFFFFFFFF);
            }
        }

        private void DrawGradientTop(EnvPalette.EnvSkyLut lut, string label)
        {
            var start = lut.ColorBegin.ToVector4();
            var middle = lut.ColorMiddle.ToVector4();
            var end = lut.ColorEnd.ToVector4();

            var screenPos = ImGui.GetCursorScreenPos();

            if (ImGui.ColorEdit4($"Start Color##{label}start", ref start, ImGuiColorEditFlags.NoInputs))
            {
                lut.ColorBegin = new EnvPalette.Color(start);
            }

            ImGui.SameLine();

            if (lut.UseMiddleColor)
            {
                if (ImGui.ColorEdit4($"Middle Color##{label}mid", ref middle, ImGuiColorEditFlags.NoInputs))
                {
                    lut.ColorMiddle = new EnvPalette.Color(middle);
                }
            }

            ImGui.SameLine();

            if (ImGui.ColorEdit4($"End Color##{label}end", ref end, ImGuiColorEditFlags.NoInputs))
            {
                lut.ColorEnd = new EnvPalette.Color(end);
            }

            var screenPosY = ImGui.GetCursorScreenPos().Y;

            CalculateGradient(lut, new Vector2(screenPos.X, screenPosY), 520, 50);
        }

        public void CalculateGradientBG(EnvPalette.EnvSkyLut lut, Vector2 screenPos, int width, float height)
        {
            Vector4[] colors = lut.UseMiddleColor ?
                new Vector4[] { lut.ColorBegin.ToVector4(), lut.ColorMiddle.ToVector4(), lut.ColorEnd.ToVector4() } :
                new Vector4[] { lut.ColorBegin.ToVector4(), lut.ColorEnd.ToVector4(), lut.ColorEnd.ToVector4() };

            Vector4 LerpBetweenColors(Vector4 a, Vector4 b, Vector4 c, float t)
            {
                //bottom to top
                if (!lut.UseMiddleColor)
                    return Vector4.Lerp(a, b, t);

                if (t < 0.5f) // Bottom to middle
                    return Vector4.Lerp(a, b, t / 0.5f);
                else // Middle to top
                    return Vector4.Lerp(b, c, (t - 0.5f) / 0.5f);
            }

            for (int i = 0; i < width; i++)
            {
                float time = i / (width - 1f);

                //Use "X" to lerp between the colors to get
                Vector4 color = LerpBetweenColors(colors[0], colors[1], colors[2], time);

                ImGui.GetWindowDrawList().AddRectFilled(
                    screenPos + new Vector2(0, height - time * height - 1),
                    screenPos + new Vector2(width, height - time * height + 1), ImGui.ColorConvertFloat4ToU32(color));
            }
        }

        public void CalculateGradient(EnvPalette.EnvSkyLut lut, Vector2 screenPos, int width, float height)
        {
            var pos = screenPos;

            var type = lut.Curve.GetCurveType();
            var data = lut.Curve.Data.ToArray();

            Vector4[] colors = lut.UseMiddleColor ?
                new Vector4[] { lut.ColorBegin.ToVector4(), lut.ColorMiddle.ToVector4(), lut.ColorEnd.ToVector4() } :
                new Vector4[] { lut.ColorBegin.ToVector4(), lut.ColorEnd.ToVector4(), lut.ColorEnd.ToVector4() };

            Vector4 LerpBetweenColors(Vector4 a, Vector4 b, Vector4 c, float t)
            {
                //bottom to top
                if (!lut.UseMiddleColor)
                    return Vector4.Lerp(a, b, t);

                if (t < 0.5f) // Bottom to middle
                    return Vector4.Lerp(a, b, t / 0.5f);
                else // Middle to top
                    return Vector4.Lerp(b, c, (t - 0.5f) / 0.5f);
            }

            for (int i = 0; i < width; i++)
            {
                float time = i / (width - 1f);
                float x = MathF.Min(AglCurve.Interpolate(data, type, time), lut.Curve.MaxX);

                //Use "X" to lerp between the colors to get
                Vector4 color = LerpBetweenColors(colors[0], colors[1], colors[2], x);

                ImGui.GetWindowDrawList().AddRectFilled(
                    screenPos + new Vector2(time * width - 1, 0),
                    screenPos + new Vector2(time * width + 1, height), ImGui.ColorConvertFloat4ToU32(color));
            }
        }

        private void LoadLinear(float[] curve)
        {
            float num_keys = curve.Length;
            for (int i = 0; i < num_keys; i++)
            {
                Keys.Add(new Key() { Value = curve[i], });
            }
        }

        private void LoadHermite(float[] curve)
        {
            float num_keys = curve.Length / 2;
            for (int i = 0; i < num_keys; i++)
            {
                Keys.Add(new Key() {
                    Value = curve[i * 2], 
                    SlopeIn = curve[i * 2 + 1],
                    SlopeOut = curve[i * 2 + 1],
                });
            }
        }

        private void LoadHermite2D(float[] curve)
        {
            float num_keys = curve.Length / 3;
            for (int i = 0; i < num_keys; i++)
            {
                Keys.Add(new Key()
                {
                    Value = curve[i * 3],
                    SlopeIn = curve[i * 3 + 1] - curve[i],
                    SlopeOut = curve[i * 3 + 2] - curve[i],
                });
            }
        }

        public class Key
        {
            public float Value;
            public float SlopeIn;
            public float SlopeOut;

            public float PrevValue;
            public float PrevSlopeIn;
            public float PrevSlopeOut;
        }
    }
}
