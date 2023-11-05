using Fushigi.param;
using Fushigi.util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    class Preferences
    {
        public static void Draw(ref bool continueDisplay)
        {
            if (ImGui.Begin("Preferences"))
            {
                ImGui.Text("Welcome to Fushigi! Set the RomFS game path and save directory to get started.");

                var romfs = UserSettings.GetRomFSPath();
                var mod = UserSettings.GetModRomFSPath();

                ImGui.Indent();


                if (PathSelector.Show(
                    "RomFS Game Path",
                    ref romfs,
                    RomFS.IsValidRoot(romfs))
                    )
                {
                    RomFS.SetRoot(romfs);
                    UserSettings.SetRomFSPath(romfs);
                    
                    /* if our parameter database isn't set, set it */
                    if (!ParamDB.sIsInit)
                    {
                        ParamDB.Load();
                    }
                }

                Tooltip.Show("The game files which are stored under the romfs folder.");

                if (PathSelector.Show("Save Directory", ref mod, !string.IsNullOrEmpty(mod)))
                    UserSettings.SetModRomFSPath(mod);

                Tooltip.Show("The save output where to save modified romfs files");

                ImGui.Unindent();

                if (ImGui.Button("Close"))
                {
                    continueDisplay = false;
                }

                ImGui.End();
            }
        }
    }
}
