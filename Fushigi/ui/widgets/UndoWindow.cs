using Fushigi.util;
using ImGuiNET;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
                if (ImGui.Selectable($"{IconUtil.ICON_FILE_DOWNLOAD}"+"Course Loaded", !context.GetUndoStack().Any()))
                {
                    for(var a = context.GetUndoStack().Count()-1; a >= 0; a--)
                    {
                        context.Undo();
                    }
                }
                for (var i = 0; i < context.GetUndoStack().Count(); i++)
                {
                    var op = context.GetUndoStack().Reverse().ElementAt(i);
                    string name = op.Name == null ? $"Operation{i}" : op.Name;

                    bool selected = context.GetLastAction() == op;
                    if (ImGui.Selectable(name + "##"+i, selected))
                    {
                        for(var a = context.GetUndoStack().Count()-1; a > i; a--)
                        {
                            context.Undo();
                        }
                    }
                }
                for (var i = 0; i < context.GetRedoUndoStack().Count(); i++)
                {
                    var op = context.GetRedoUndoStack().ElementAt(i);
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
                    ImGui.PushID(i);
                    if (ImGui.Selectable(op.Name+"##"+i))
                    {
                        for(var a = 0; a <=  i; a++)
                        {
                            context.Redo();
                        }
                    }
                    ImGui.PopID();
                    ImGui.PopStyleColor();
                }
                ImGui.End();
            }
        }
    }
}
