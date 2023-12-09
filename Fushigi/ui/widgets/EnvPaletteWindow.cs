using Fushigi.agl;
using Fushigi.Bfres;
using Fushigi.course;
using Fushigi.env;
using Fushigi.gl.Bfres.AreaData;
using Fushigi.util;
using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.ui.widgets
{
    public class EnvPaletteWindow
    {
        private AreaParam AreaParam;
        private EnvPalette EnvPalette;
        private AreaResourceManager AreaResources;

        public EnvPaletteWindow() { }

        private GL _gl;

        public void Load(GL gl, AreaParam areaParam, EnvPalette envPalette, AreaResourceManager areaResources) {
            _gl = gl;
            AreaParam = areaParam;
            EnvPalette = envPalette;
            AreaResources = areaResources;
        }

        public void Reload()
        {
            AreaResources.ReloadPalette(_gl, this.EnvPalette);
        }

        public void Update()
        {
            AreaResources.ReloadPalette(_gl, this.EnvPalette);
        }

        public void Render()
        {
            if (ImGui.Begin("Env Palette"))
            {
                PaletteDropdown();

                ImGui.BeginTabBar("EnvTabs");

                if (ImGui.BeginTabItem("Sky"))
                {
                    RenderSkyboxUI();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Env Colors"))
                {
                    RenderEnvColorUI();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Rim Lighting"))
                {
                    RenderRimUI();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Hemi Lighting"))
                {
                    RenderLightsHemiUI();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Directional Lighting"))
                {
                    RenderLightsUI();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Fog"))
                {
                    RenderFogUI();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.End();
            }
        }

        private void PaletteDropdown()
        {
            void SelectPalette(string name, string palette)
            {
                if (string.IsNullOrEmpty(palette))
                    return;

                palette = palette.Replace("Work/Gyml/Gfx/EnvPaletteParam/", "");
                palette = palette.Replace(".game__gfx__EnvPaletteParam.gyml", "");

                //  Work / Gyml / Gfx / EnvPaletteParam / AW_Hajimari_Sougen_HajimariDokan.game__gfx__EnvPaletteParam.gyml

                bool selected = this.EnvPalette.Name == name;
                if (ImGui.Selectable($"{name} : {palette}", selected))
                {
                    AreaResources.TransitionEnvPalette(this.EnvPalette.Name, palette);

                   // EnvPalette.Load(palette);
                  //  this.Reload();
                }
                if (selected)
                    ImGui.SetItemDefaultFocus();
            }

            if (ImGui.BeginCombo("Area Palette List", $"EnvPalette.Name", ImGuiComboFlags.HeightLarge))
            {
                SelectPalette($"Default Palette", this.AreaParam.EnvPaletteSetting.InitPaletteBaseName);

                if (this.AreaParam.EnvPaletteSetting.WonderPaletteList != null)
                {
                    foreach (var palette in this.AreaParam.EnvPaletteSetting.WonderPaletteList)
                        SelectPalette($"Wonder Palette", palette);
                }
                if (this.AreaParam.EnvPaletteSetting.TransPaletteList != null)
                {
                    foreach (var palette in this.AreaParam.EnvPaletteSetting.TransPaletteList)
                        SelectPalette($"Transition Palette", palette);
                }
                if (this.AreaParam.EnvPaletteSetting.EventPaletteList != null)
                {
                    foreach (var palette in this.AreaParam.EnvPaletteSetting.EventPaletteList)
                        SelectPalette($"Event Palette", palette);
                }
                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo("EnvPalette", $"{EnvPalette.Name}", ImGuiComboFlags.HeightLarge))
            {
                var dir = Path.Combine(UserSettings.GetRomFSPath(), "Gyml", "Gfx", "EnvPaletteParam");
                foreach (var file in Directory.GetFiles(dir))
                {
                    string name = Path.GetFileName(file).Replace(".game__gfx__EnvPaletteParam.bgyml", "");
                    bool select = EnvPalette.Name == name;
                    if (ImGui.Selectable(name, select))
                    {
                        EnvPalette.Load(name);
                        this.Reload();
                    }
                    if (select)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

        }

        public void RenderEnvMapUI()
        {

        }

        public void RenderRimUI()
        {
            if (EnvPalette.Rim == null)
                return;

            ImGui.Columns(2);

            DrawColor("Rim Color", EnvPalette.Rim, "Color");
            DrawFloat("Rim Width", EnvPalette.Rim, "Width");
            DrawFloat("Rim Power", EnvPalette.Rim, "Power");

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Rim Amount:");
            ImGui.NextColumn();
            ImGui.NextColumn();

            ImGui.Indent();

            DrawFloatSlider("Object", EnvPalette.Rim, "IntensityObject", 0, 1f);
            DrawFloatSlider("Player", EnvPalette.Rim, "IntensityPlayer", 0, 1f);
            DrawFloatSlider("Enemy", EnvPalette.Rim, "IntensityEnemy", 0, 1f);

            DrawFloatSlider("Field Band", EnvPalette.Rim, "IntensityFieldBand", 0, 1f);
            DrawFloatSlider("Field Wall", EnvPalette.Rim, "IntensityFieldWall", 0, 1f);
            DrawFloatSlider("Field Deco", EnvPalette.Rim, "IntensityFieldDeco", 0, 1f);

            DrawFloatSlider("Cloud", EnvPalette.Rim, "IntensityCloud", 0, 1f);
            DrawFloatSlider("DV", EnvPalette.Rim, "IntensityDV", 0, 1f);

            ImGui.Columns(1);
        }

        public void RenderEnvColorUI()
        {
            if (EnvPalette.EnvColor == null)
                return;

            ImGui.Columns(2);

            for (int i = 0; i < 8; i++)
                DrawColor($"Color {i}", EnvPalette.EnvColor, $"Color{i}");

            ImGui.Columns(1);
        }

        public void RenderFogUI()
        {
            if (EnvPalette.Fog == null)
                return;

            if (ImGui.CollapsingHeader("Main"))
                RenderFogUI("Main", EnvPalette.Fog.Main);

            if (ImGui.CollapsingHeader("MainWorld"))
                RenderFogUI("MainWorld", EnvPalette.Fog.MainWorld);

            if (ImGui.CollapsingHeader("Cloud"))
                RenderFogUI("Cloud", EnvPalette.Fog.Cloud);

            if (ImGui.CollapsingHeader("CloudWorld"))
                RenderFogUI("CloudWorld", EnvPalette.Fog.CloudWorld);
        }

        public void RenderFogUI(string label, EnvPalette.EnvFog fog)
        {
            if (fog == null)
                return;

            ImGui.Columns(2);

            DrawColor($"{label} Color", fog, "Color");
            DrawFloat($"{label} Start", fog, "Start");
            DrawFloat($"{label} End", fog, "End");
            DrawFloat($"{label} Damp", fog, "Damp");

            ImGui.Columns(1);
        }

        public void RenderLightsHemiUI()
        {
            RenderLightsHemiUI("ObjLight", EnvPalette.ObjLight);
            RenderLightsHemiUI("CharLight", EnvPalette.CharLight);
            RenderLightsHemiUI("FieldLight", EnvPalette.FieldLight);
            RenderLightsHemiUI("DvLight", EnvPalette.DvLight);
            RenderLightsHemiUI("CloudLight", EnvPalette.CloudLight);
        }

        public void RenderLightsUI()
        {
            if (ImGui.CollapsingHeader("ObjLight", ImGuiTreeNodeFlags.DefaultOpen))
                RenderLightsUI("ObjLight", EnvPalette.ObjLight);

            if (ImGui.CollapsingHeader("CharLight", ImGuiTreeNodeFlags.DefaultOpen))
                RenderLightsUI("CharLight", EnvPalette.CharLight);

            if (ImGui.CollapsingHeader("FieldLight", ImGuiTreeNodeFlags.DefaultOpen))
                RenderLightsUI("FieldLight", EnvPalette.FieldLight);

            if (ImGui.CollapsingHeader("DvLight", ImGuiTreeNodeFlags.DefaultOpen))
                RenderLightsUI("DvLight", EnvPalette.DvLight);

            if (ImGui.CollapsingHeader("CloudLight", ImGuiTreeNodeFlags.DefaultOpen))
                RenderLightsUI("CloudLight", EnvPalette.CloudLight);
        }

        public void RenderLightsHemiUI(string label, EnvPalette.EnvLightList list)
        {
            if (list == null)
                return;

            ImGui.Columns(4);

            RenderHemiLights($"{label} Hemi", list.Hemi);

            ImGui.Columns(1);
        }

        public void RenderLightsUI(string label, EnvPalette.EnvLightList list)
        {
            if (list == null)
                return;

            ImGui.Columns(6);

            ImGui.Text("Name"); ImGui.NextColumn();
            ImGui.Text("Color"); ImGui.NextColumn();
            ImGui.Text("Intensity"); ImGui.NextColumn();
            ImGui.Text("Longitude"); ImGui.NextColumn();
            ImGui.Text("Latitude"); ImGui.NextColumn();
            ImGui.Text("Dir"); ImGui.NextColumn();

            RenderDirectionalLights($"{label} Main", list.Main);
            RenderDirectionalLights($"{label} SubDiff0", list.SubDiff0);
            RenderDirectionalLights($"{label} SubDiff1", list.SubDiff1);
            RenderDirectionalLights($"{label} SubDiff2", list.SubDiff2);
            RenderDirectionalLights($"{label} SubSpec0", list.SubSpec0);
            RenderDirectionalLights($"{label} SubSpec1", list.SubSpec1);
            RenderDirectionalLights($"{label} SubSpec2", list.SubSpec2);

            ImGui.Columns(1);
        }

        public void RenderHemiLights(string label, EnvPalette.EnvLightHemisphere hemi)
        {
            if (hemi == null)
                return;

            var sky_color = hemi.Sky.ToVector4();
            var ground_color = hemi.Ground.ToVector4();
            float intensity = hemi.Intensity;

            ImGui.Text(label);
            ImGui.NextColumn();

            if (ImGui.ColorEdit4($"Sky##{label}sky", ref sky_color, ImGuiColorEditFlags.NoInputs))
            {
                hemi.Sky = new EnvPalette.Color(sky_color);
                this.Update();
            }

            ImGui.NextColumn();

            if (ImGui.ColorEdit4($"Ground##{label}Ground", ref ground_color, ImGuiColorEditFlags.NoInputs))
            {
                hemi.Ground = new EnvPalette.Color(ground_color);
                this.Update();
            }

            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);
            if (ImGui.DragFloat($"Intensity##{label}inten", ref intensity))
            {
                hemi.Intensity = intensity;
                this.Update();
            }
            ImGui.PopItemWidth();

            ImGui.NextColumn();

        }

        public void RenderDirectionalLights(string label, EnvPalette.EnvLightDirectional dir)
        {
            if (dir == null)
                return;

            var color = dir.Color.ToVector4();
            float intensity = dir.Intensity;
            float longitude = dir.Longitude;
            float latitude = dir.Latitude;

            ImGui.Text(label);
            ImGui.NextColumn();

            if (ImGui.ColorEdit4($"##Color{label}dir", ref color, ImGuiColorEditFlags.NoInputs))
            {
                dir.Color = new EnvPalette.Color(color);
                this.Update();
            }
            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            if (ImGui.DragFloat($"##Intensity{label}inten", ref intensity))
            {
                dir.Intensity = intensity;
                this.Update();
            }
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            if (ImGui.DragFloat($"##Longitude{label}long", ref longitude))
                dir.Longitude = longitude;

            ImGui.PopItemWidth();

            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            if (ImGui.DragFloat($"##Latitude{label}lat", ref latitude))
                dir.Latitude = latitude;

            ImGui.PopItemWidth();

            ImGui.NextColumn();

            var d = GetDirectionalVector(latitude, longitude);

            ImGui.Text($"{MathF.Round(d.X, 5)} {MathF.Round(d.Y, 5)} {MathF.Round(d.Z, 5)}");

            ImGui.NextColumn();
        }

        static Vector3 GetDirectionalVector(float latitude, float longitude)
        {
            // Convert latitude and longitude from degrees to radians
            float latRad = latitude * MathUtil.Deg2Rad;
            float lonRad = longitude * MathUtil.Deg2Rad;

            float x = MathF.Cos(latRad) * MathF.Sin(lonRad);
            float y = MathF.Sin(latRad);
            float z = MathF.Cos(latRad) * MathF.Cos(lonRad);

            var dir = new Vector3(x, y, z);
            return Vector3.Normalize(-dir);
        }

        public void RenderPostEffectUI()
        {
        }

        public void RenderSkyboxUI()
        {
            if (EnvPalette.Sky == null)
                return;

            if (ImGui.CollapsingHeader("Preview", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (AreaResources.VRSkybox.SkyTexture != null)
                    ImGui.Image((IntPtr)AreaResources.VRSkybox.SkyTexture.ID, new Vector2(400, 128));
            }

            RenderSkyboxLUTUI("Top", EnvPalette.Sky.LutTexTop);
            RenderSkyboxLUTUI("Left", EnvPalette.Sky.LutTexLeft);
            RenderSkyboxLUTUI("Top Left", EnvPalette.Sky.LutTexLeftTop);
            RenderSkyboxLUTUI("Top Right", EnvPalette.Sky.LutTexRightTop);
        }

        private bool open_popup = false;
        private string popup_lut = "";
        private AglCurveEditor AglCurveEditor = new AglCurveEditor();

        public void RenderSkyboxLUTUI(string label, EnvPalette.EnvSkyLut lut)
        {
            if (ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen))
            {
                var screenPos = ImGui.GetCursorScreenPos();
                if (ImGui.Button($"{IconUtil.ICON_EDIT}", new Vector2(30, 30)))
                {
                    ImGui.OpenPopup("gradient_popup");
                    open_popup = true;
                    popup_lut = label;
                    AglCurveEditor.Load(lut);
                }

                ImGui.SameLine();

                if (ImGui.BeginChild($"Grad{label}", new Vector2(ImGui.GetColumnWidth() - 2, 30)))
                {
                    CalculateGradient(lut, screenPos, 400, 30);
                }

                ImGui.EndChild();
            }

            if (popup_lut == label && ImGui.Begin("gradient_popup", ref open_popup, ImGuiWindowFlags.NoNav))
            {
                if (ImGui.CollapsingHeader("Presets", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginChild($"PresetList{label}", new Vector2(ImGui.GetWindowWidth() - 2, 60)))
                    {
                    }
                    ImGui.EndChild();
                }
                if (ImGui.CollapsingHeader($"{label} Params", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);

                    if (label == "Top")
                        DrawFloatSlider("Horizontal Offset", EnvPalette.Sky, "HorizontalOffset", 0, 1.5f);
                    if (label == "Top Left")
                        DrawFloat("Rotation", EnvPalette.Sky, "RotDegLeftTop");
                    if (label == "Top Right")
                        DrawFloat("Rotation", EnvPalette.Sky, "RotDegRightTop");

                    bool useMiddle = lut.UseMiddleColor;

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Use Middle");

                    ImGui.NextColumn();

                    if (ImGui.Checkbox("##Use Middle", ref useMiddle))
                    {
                        lut.UseMiddleColor = useMiddle;
                        this.Update();
                    }
                    ImGui.NextColumn();

                    ImGui.Columns(1);
                }
                if (ImGui.CollapsingHeader($"{label} Gradient", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    AglCurveEditor.Render(400);
                }

                ImGui.EndPopup();
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

        private void DrawColor(string label, object obj, string property)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);
            var color = (EnvPalette.Color)prop.GetValue(obj);
            var vec = color.ToVector4();

            if (ImGui.ColorEdit4($"##{label}", ref vec, ImGuiColorEditFlags.NoInputs))
            {
                prop.SetValue(obj, new EnvPalette.Color(vec)); 
                Update();
            }
            ImGui.NextColumn();
        }

        private void DrawFloat(string label, object obj, string property)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);
            var v = (float)prop.GetValue(obj);
            if (ImGui.DragFloat($"##{label}", ref v))
            {
                prop.SetValue(obj, v);
                Update();
            }
            ImGui.NextColumn();
        }

        private void DrawFloatSlider(string label, object obj, string property, float min, float max)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);
            var v = (float)prop.GetValue(obj);
            if (ImGui.SliderFloat($"##{label}", ref v, min, max))
            {
                prop.SetValue(obj, v);
                Update();
            }
            ImGui.NextColumn();
        }
    }
}
