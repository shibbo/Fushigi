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
        float worldNameSize = 12f;
        GL gl;
        Action<string> selectCourseCallback;
        bool isOpen = true;

        public CourseSelect(GL gl, Action<string> selectCourseCallback, string? selectedCourseName = null)
        {
            this.gl = gl;
            this.selectedCourseName = selectedCourseName;
            this.selectCourseCallback = selectCourseCallback;
        }

        public void Draw()
        {
            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, thumbnailSize * 1.25f);

            if (isOpen && !ImGui.IsPopupOpen("Select Course"))
            {
                ImGui.OpenPopup("Select Course");
            }

            if (!ImGui.BeginPopupModal("Select Course", ref isOpen))
            {
                return;
            }

            ImGui.PopStyleVar();

            DrawTabs();

            DrawCourses();

            ImGui.EndPopup();
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
            var fontSize = ImGui.GetFontSize();
            var font = ImGui.GetFont();
            font.FontSize = worldNameSize;
            ImGui.Text(RomFS.GetCourseEntries()[selectedWorld!].name);
            font.FontSize = fontSize;


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
                    selectCourseCallback(course.Key);
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
