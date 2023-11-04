using Fushigi.util;
using ImGuiNET;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

// Uses https://github.com/mellinoe/synthapp/blob/master/src/synthapp/Widgets/FilePicker.cs as a base
// seems to only be able to pick out files...TODO -- update to pick folders specifically

namespace Fushigi.ui.widgets
{
    public class FilePicker
    {
        private const string FilePickerID = "###FilePicker";
        private static readonly Dictionary<object, FilePicker> s_filePickers = new Dictionary<object, FilePicker>();
        private static readonly Vector2 DefaultFilePickerSize = new Vector2(600, 400);

        public string CurrentFolder { get; set; }
        public string SelectedFile { get; set; }

        public static FilePicker GetFilePicker(object o, string startingPath)
        {
            if (File.Exists(startingPath))
            {
                startingPath = new FileInfo(startingPath).DirectoryName;
            }
            else if (string.IsNullOrEmpty(startingPath) || !Directory.Exists(startingPath))
            {
                startingPath = "D:\\Hacking\\Switch\\Wonder\\romfs\\";
                if (string.IsNullOrEmpty(startingPath))
                {
                    startingPath = AppContext.BaseDirectory;
                }
            }

            if (!s_filePickers.TryGetValue(o, out FilePicker fp))
            {
                fp = new FilePicker();
                fp.CurrentFolder = startingPath;
                s_filePickers.Add(o, fp);
            }

            return fp;
        }

        public bool Draw(ref string selected)
        {
            string label = null;
            if (selected != null)
            {
                if (FileUtil.TryGetFileInfo(selected, out FileInfo realFile))
                {
                    label = realFile.Name;
                }
                else
                {
                    label = "<Select File>";
                }
            }
            if (ImGui.Button(label))
            {
                ImGui.OpenPopup(FilePickerID);
            }

            bool result = false;
            ImGui.SetNextWindowSize(DefaultFilePickerSize);
            if (ImGui.BeginPopupModal(FilePickerID))
            {
                result = DrawFolder(ref selected, true);
                ImGui.EndPopup();
            }

            return result;
        }

        private bool DrawFolder(ref string selected, bool returnOnSelection = false)
        {
            ImGui.Text("Current Folder: " + CurrentFolder);
            bool result = false;

            if (ImGui.BeginChildFrame(1, new Vector2(0, 600)))
            {
                DirectoryInfo di = new DirectoryInfo(CurrentFolder);
                if (di.Exists)
                {
                    if (di.Parent != null)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFF00);
                        if (ImGui.Selectable($"..{Path.DirectorySeparatorChar}", false))
                        {
                            CurrentFolder = di.Parent.FullName;
                        }
                        ImGui.PopStyleColor();
                    }
                    foreach (var fse in Directory.EnumerateFileSystemEntries(di.FullName))
                    {
                        if (Directory.Exists(fse))
                        {
                            string name = Path.GetFileName(fse);
                            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFF00);
                            if (ImGui.Selectable(name + Path.DirectorySeparatorChar, false))
                            {
                                CurrentFolder = fse;
                            }
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            string name = Path.GetFileName(fse);
                            bool isSelected = SelectedFile == fse;
                            if (ImGui.Selectable(name, isSelected))
                            {
                                SelectedFile = fse;
                                if (returnOnSelection)
                                {
                                    result = true;
                                    selected = SelectedFile;
                                }
                            }
                            if (ImGui.IsMouseDoubleClicked(0))
                            {
                                result = true;
                                selected = SelectedFile;
                                ImGui.CloseCurrentPopup();
                            }
                        }
                    }
                }

            }
            ImGui.EndChildFrame();


            if (ImGui.Button("Cancel"))
            {
                result = false;
                ImGui.CloseCurrentPopup();
            }

            if (SelectedFile != null)
            {
                ImGui.SameLine();
                if (ImGui.Button("Open"))
                {
                    result = true;
                    selected = SelectedFile;
                    ImGui.CloseCurrentPopup();
                }
            }

            return result;
        }
    }
}
