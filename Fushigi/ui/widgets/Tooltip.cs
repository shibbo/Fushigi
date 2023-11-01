using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    /// <summary>
    /// Displays a tooltip from the previously drawn item.
    /// </summary>
    internal class Tooltip
    {
        public static void Show(string text)
        {
            if (ImGui.IsItemHovered()) //TODO add delay
                ImGui.SetTooltip(text);
        }
    }
}
