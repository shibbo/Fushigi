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

            ImGui.Columns(2);

            ImGui.SetColumnWidth(0, 150);

            bool edited = false;

            ImGui.Text(label);

            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 100);

            if (!isValid)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.5f, 0, 0, 1));
                ImGui.InputText($"##{label}", ref path, 500, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0.5f, 0, 1));
                ImGui.InputText($"##{label}", ref path, 500, ImGuiInputTextFlags.ReadOnly);
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

            ImGui.PopItemWidth();

            ImGui.SameLine();
            bool clicked = ImGui.Button($"Select##{label}");

            ImGui.NextColumn();

            ImGui.Columns(1);

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
