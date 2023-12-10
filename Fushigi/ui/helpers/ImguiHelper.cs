using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.helpers
{
    public class ImguiHelper
    {
        public static bool DrawTextToggle(string text, bool toggle, Vector2 size)
        {
            var color = toggle ? ImGui.GetStyle().Colors[(int)ImGuiCol.Text]
                               : ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];

            ImGui.PushStyleColor(ImGuiCol.Text, color);

            bool pressed = ImGui.Button(text, size);

            ImGui.PopStyleColor();

            return pressed;
        }
    }
}
