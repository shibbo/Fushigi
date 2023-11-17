using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    internal class CourseSelect
    {
        string? selectedWorld;
        string? selectedCourseName;
        Vector2 thumbnailSize = new(200, 100);
        GL gl;

        public CourseSelect(GL gl, string? selectedCourseName = null)
        {
            this.gl = gl;
            this.selectedCourseName = selectedCourseName;
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

            var courses = RomFS.GetCourseEntries()[selectedWorld!];
            var courseThumbnails = RomFS.GetCourseThumbnails();

            foreach (var course in courses)
            {
                ImGui.TableNextColumn();
                ImGui.Image(courseThumbnails[course], thumbnailSize);
                if (ImGui.RadioButton(course, course == selectedCourseName))
                {
                    if (selectedCourseName != course)
                    {
                        Debug.WriteLine($"Switching to {course}");
                    }
                }
            }

            ImGui.EndTable();

            ImGui.EndListBox();
        }
    }
}
