using Fushigi.param;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    class CloseConfirmationDialog
    {
        public enum Result
        {
            Yes,
            No
        }

        public static bool Draw(bool doShow, [MaybeNullWhen(false)] out Result? result)
        {
            result = null;

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            if (doShow && !ImGui.IsPopupOpen("CloseConfirmation"))
                ImGui.OpenPopup("CloseConfirmation");

            if (ImGui.BeginPopupModal("CloseConfirmation", ref doShow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
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
                    result = Result.Yes;
             
                ImGui.SameLine();
                if (ImGui.Button("No"))
                    result = Result.No;

                ImGui.EndPopup();
            }

            return result != null;
        }

    }
}
