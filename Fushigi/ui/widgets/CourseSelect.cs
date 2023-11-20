using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.ui.widgets
{
    internal class CourseSelect
    {
        string? selectedWorld;
        string? selectedCourseName;
        Vector2 thumbnailSize = new(200f, 112.5f);
        GL gl;
        Action<string> switchCourseCallback;

        public CourseSelect(GL gl, Action<string> switchCourseCallback, string? selectedCourseName = null)
        {
            this.gl = gl;
            this.selectedCourseName = selectedCourseName;
            this.switchCourseCallback = switchCourseCallback;
        }

        public void Draw()
        {
            if (!ImGui.Begin("Select Course"))
            {
                return;
            }

            DrawTabs();

            DrawCourses();

            ImGui.End();
        }

        void DrawTabs()
        {
            if (!ImGui.BeginTabBar(""))
            {
                return;
            }

            foreach (var world in RomFS.GetCourseEntries().Keys)
            {
                if (ImGui.BeginTabItem(world))
                {
                    if (selectedWorld != world)
                    {
                        selectedWorld = world;
                    }

                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }

        void DrawCourses()
        {
            ImGui.Text(RomFS.GetCourseEntries()[selectedWorld!].name);

            if (!ImGui.BeginListBox(selectedWorld, ImGui.GetContentRegionAvail()))
            {
                return;
            }

            var numColumns = (int)(ImGui.GetContentRegionAvail().X / thumbnailSize.X);
            if (!ImGui.BeginTable("", numColumns))
            {
                return;
            }
            ImGui.TableNextRow();

            RomFS.CacheCourseThumbnails(gl, selectedWorld!);
            var courses = RomFS.GetCourseEntries()[selectedWorld!].courseEntries;

            float em = ImGui.GetFrameHeight();

            foreach (var course in courses)
            {
                ImGui.PushID(course.Key);
                ImGui.TableNextColumn();
                bool clicked = ImGui.Selectable(string.Empty, course.Key == selectedCourseName, 
                    ImGuiSelectableFlags.None, new Vector2(thumbnailSize.X, thumbnailSize.Y + em * 1.8f));

                if (clicked)
                {
                    if (selectedCourseName != course.Key)
                    {
                        Debug.WriteLine($"Switching to {course.Key}");
                        switchCourseCallback(course.Key);
                    }
                }

                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();

                var dl = ImGui.GetWindowDrawList();

                dl.PushClipRect(min, max, true);

                course.Value.thumbnail.CheckState(false);
                dl.AddImage((IntPtr)course.Value.thumbnail.ID,
                    (min + max - thumbnailSize) / 2 - new Vector2(0, em * 1.25f),
                    (min + max + thumbnailSize) / 2 - new Vector2(0, em * 1.25f));

                ReadOnlySpan<char> text = course.Value.name;
                if (text[^1] == '\0')
                    text = text[..^1];
                float textWidth = ImGui.CalcTextSize(text).X;

                dl.AddText(new Vector2(
                    (min.X + max.X - textWidth) / 2, 
                    min.Y + thumbnailSize.Y + ImGui.GetStyle().FramePadding.Y),
                    ImGui.GetColorU32(ImGuiCol.Text), text);

                if (textWidth > (max-min).X && ImGui.IsMouseHoveringRect(
                    new Vector2(min.X, min.Y + thumbnailSize.Y),
                    new Vector2(max.X, min.Y + thumbnailSize.Y + em)))
                {
                    ImGui.SetTooltip(text);
                }

                text = course.Key;
                textWidth = ImGui.CalcTextSize(text).X;

                dl.AddText(new Vector2(
                    (min.X + max.X - textWidth) / 2, 
                    min.Y + thumbnailSize.Y + em + ImGui.GetStyle().FramePadding.Y),
                    (ImGui.GetColorU32(ImGuiCol.Text) & 0xFF_FF_FF) | 0x99u << 24, text);

                dl.PopClipRect();

                ImGui.PopID();
            }

            ImGui.EndTable();

            ImGui.EndListBox();
        }
    }
}
