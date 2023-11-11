using Fushigi.param;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    class CloseConfirmationDialog
    {

        public static bool needConfirmation = false;

        public static void Draw(ref bool shouldDraw, ref bool shouldClose)
        {
            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            ImGui.OpenPopup("CloseConfirmation");

            if (ImGui.BeginPopupModal("CloseConfirmation", ref shouldDraw, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
            {
                float centerXText = (ImGui.GetWindowWidth() - ImGui.CalcTextSize("Unsaved changes.").X) * 0.5f;
                ImGui.SetCursorPosX(centerXText);

                ImGui.Text("Unsaved changes.");
                ImGui.NewLine();
                ImGui.Text("Do you still want to close?");
                ImGui.NewLine();

                float centerXButtons = (ImGui.GetWindowWidth() - ImGui.CalcTextSize("Yes No").X) * 0.4f;
                ImGui.SetCursorPosX(centerXButtons);
                if (ImGui.Button("Yes"))
                {
                    shouldClose = true;
                    shouldDraw = false;
                    needConfirmation = false;
                }
             
                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    shouldClose = false;
                    shouldDraw = false;
                }

                ImGui.EndPopup();
            }
        }

    }
}
