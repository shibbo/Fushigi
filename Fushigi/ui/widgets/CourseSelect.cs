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
            var courses = RomFS.GetCourseEntries()[selectedWorld!];

            foreach (var course in courses)
            {
                ImGui.TableNextColumn();
                ImGui.Image(course.Value.thumbnail, thumbnailSize);
                if (ImGui.RadioButton(course.Key, course.Key == selectedCourseName))
                {
                    if (selectedCourseName != course.Key)
                    {
                        Debug.WriteLine($"Switching to {course.Key}");
                        switchCourseCallback(course.Key);
                    }
                }
            }

            ImGui.EndTable();

            ImGui.EndListBox();
        }
    }
}
