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
    class ParamDBDialog
    {
        static Task? mLoadParamDB;

        public static void Draw(ref bool shouldDraw)
        {
            mLoadParamDB ??= ParamDB.sIsInit ? Task.Run(ParamDB.Reload) : Task.Run(ParamDB.Load);

            if (mLoadParamDB.IsCompleted)
            {
                shouldDraw = false;
                mLoadParamDB = null;
                return;
            }

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            ImGui.OpenPopup("ParamDB");

            if (ImGui.BeginPopupModal("ParamDB", ref shouldDraw, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
            {
                ImGui.Text("Generating ParamDB...");
                // TODO: replace this progress bar with an animated loading bar
                ImGui.ProgressBar(0.33f, new Vector2(0, 0), "Loading...");

                ImGui.EndPopup();
            }
        }
    }
}
