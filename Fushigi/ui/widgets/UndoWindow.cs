using Fushigi.util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    internal class UndoWindow
    {
        public void Render(CourseAreaEditContext context)
        {
            if (ImGui.Begin("History"))
            {
                if (ImGui.Button($"{IconUtil.ICON_UNDO}"))
                {
                    context.Undo();
                }
                ImGui.SameLine();
                if (ImGui.Button($"{IconUtil.ICON_REDO}"))
                {
                    context.Redo();
                }
                foreach (var op in context.GetUndoStack().Reverse())
                {
                    bool selected = context.GetLastAction() == op;
                    if (ImGui.Selectable(op.Name, selected))
                    {

                    }
                }
                foreach (var op in context.GetRedoUndoStack())
                {
                    ImGui.BeginDisabled();

                    bool selected = context.GetLastAction() == op;
                    if (ImGui.Selectable(op.Name, selected))
                    {

                    }
                    ImGui.EndDisabled();
                }
                ImGui.End();
            }
        }
    }
}
