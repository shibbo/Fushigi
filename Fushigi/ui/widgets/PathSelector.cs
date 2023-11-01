using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    /// <summary>
    /// A widget for displaying a given directory path with a button to select one. 
    /// </summary>
    internal class PathSelector
    {
        public static bool Show(string label, ref string path, bool isValid = true)
        {
            //Ensure path isn't null for imgui
            if (path == null)
                path = "";

            //Validiate directory
            if (!System.IO.Directory.Exists(path))
                isValid = false;

            bool clicked = ImGui.Button($"  -  ##{label}");
            bool edited = false;

            ImGui.SameLine();
            if (!isValid)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.5f, 0, 0, 1));
                ImGui.InputText(label, ref path, 500, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0.5f, 0, 1));
                ImGui.InputText(label, ref path, 500, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopStyleColor();
            }

            if (ImGui.BeginPopupContextItem($"{label}_clear", ImGuiPopupFlags.MouseButtonRight))
            {
                if (ImGui.MenuItem("Clear"))
                {
                    path = "";
                    edited = true;
                }
                ImGui.EndPopup();
            }

            if (clicked)
            {
                var dialog = new FolderDialog();
                if (dialog.ShowDialog())
                {
                    path = dialog.SelectedPath;
                    return true;
                }
            }
            return edited;
        }
    }
}
