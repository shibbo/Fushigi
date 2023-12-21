using Fushigi.course;
using ImGuiNET;
using System.Numerics;

namespace Fushigi.ui.widgets
{
    class CourseMiniView
    {
        float ratio;
        Vector2 center;

        Vector4 levelBounds;
        Vector2 levelRect;
        Vector2 miniLevelRect;

        Vector2 miniCamPos;
        Vector2 miniCamSize;

        Vector3 camSave;
        Vector2 miniCamSave;

        public void Draw(CourseArea area, LevelViewport viewport, CourseAreaEditContext editContext)
        {
            var topLeft = ImGui.GetCursorScreenPos();

            ImGui.SetNextItemAllowOverlap();
            ImGui.SetCursorScreenPos(topLeft);
            
            var size = ImGui.GetContentRegionAvail();

            var cam = viewport.Camera;
            var camSize = viewport.GetCameraSizeIn2DWorldSpace();

            levelBounds = Vector4.Zero;
            foreach(var actor in area.GetActors().Where(x => x.mPackName != "GlobalAreaInfoActor"))
            {
                if(levelBounds == Vector4.Zero){
                    levelBounds = new Vector4(actor.mTranslation.X, actor.mTranslation.X, actor.mTranslation.Y, actor.mTranslation.Y);
                }
                else{
                    levelBounds = new(Math.Min(levelBounds.X, actor.mTranslation.X),
                    Math.Min(levelBounds.Y, actor.mTranslation.Y),
                    Math.Max(levelBounds.Z, actor.mTranslation.X),
                    Math.Max(levelBounds.W, actor.mTranslation.Y));
                }
            }
            levelRect = new Vector2(levelBounds.Z - levelBounds.X, levelBounds.W - levelBounds.Y);

            float tanFOV = MathF.Tan(cam.Fov / 2);

            ratio = size.X/levelBounds.X < size.Y/levelBounds.Y ? 
                size.X/levelBounds.X : size.Y/levelBounds.Y;

            miniLevelRect = levelRect*ratio;

            miniCamPos = new Vector2(cam.Target.X - levelBounds.X, -cam.Target.Y + levelBounds.Y)*ratio;
            miniCamSize = camSize*ratio;
            miniCamSave = new Vector2(camSave.X - levelBounds.X, -camSave.Y + levelBounds.Y)*ratio;
            center = new Vector2((size.X - levelRect.X)/2, (size.Y - levelRect.Y)/2);

            var lvlTopLeft = topLeft + center;

            var col = ImGuiCol.ButtonActive;

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsMouseDown(ImGuiMouseButton.Left) 
            && ImGui.IsWindowHovered())
            {
                camSave = cam.Target;
            }

            if ((ImGui.IsMouseDown(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Right))
            && ImGui.IsWindowFocused() &&
            ((!ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            || ImGui.IsWindowHovered()))
            {
                if (camSave != default)
                {
                    col = ImGuiCol.TextDisabled;
                    ImGui.GetWindowDrawList().AddRect(lvlTopLeft + miniCamSave - miniCamSize/2 + new Vector2(0, miniLevelRect.Y), 
                    lvlTopLeft + miniCamSave + miniCamSize/2 + new Vector2(0, miniLevelRect.Y), 
                    ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Button]),6,0,3);
                }

                var pos = ImGui.GetMousePos();
                cam.Target = new((pos.X - lvlTopLeft.X)/ratio + levelBounds.X,
                (-pos.Y + lvlTopLeft.Y + miniLevelRect.Y)/ratio + levelBounds.Y, cam.Target.Z);
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && !ImGui.IsMouseDown(ImGuiMouseButton.Left)
            && camSave != default)
            {
                cam.Target = camSave;
                camSave = default;
            }

            ImGui.GetWindowDrawList().AddRect(lvlTopLeft, 
            lvlTopLeft + miniLevelRect, 
            ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]),6,0,3);

            ImGui.GetWindowDrawList().AddRect(lvlTopLeft + miniCamPos - miniCamSize/2 + new Vector2(0, miniLevelRect.Y), 
            lvlTopLeft + miniCamPos + miniCamSize/2 + new Vector2(0, miniLevelRect.Y), 
            ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)col]),6,0,3);
        }
    }
}