using Fushigi.course;
using ImGuiNET;
using System.Numerics;

namespace Fushigi.ui.widgets
{
    internal class CourseMiniView
    {
        float ratio;
        Vector2 center;

        Vector4 levelBounds;
        Vector2 levelRect;
        Vector2 miniLevelRect;

        Vector2 miniCamPos;
        Vector2 miniCamSize;

        Vector3 camSave;

        public void Draw(CourseArea area, CourseAreaEditContext editContext, LevelViewport viewport)
        {
            var topLeft = ImGui.GetCursorScreenPos();
            ImGui.SetCursorScreenPos(topLeft);
            
            var size = ImGui.GetContentRegionAvail();

            var cam = viewport.Camera;
            var camSize = viewport.GetCameraSizeIn2DWorldSpace();

            foreach(var actor in area.GetActors())
            {
                levelBounds = new(Math.Min(levelBounds.X, actor.mTranslation.X),
                Math.Min(levelBounds.Y, actor.mTranslation.Y),
                Math.Max(levelBounds.Z, actor.mTranslation.X),
                Math.Max(levelBounds.W, actor.mTranslation.Y));
            }
            levelRect = new Vector2(levelBounds.Z-levelBounds.X, levelBounds.W - levelBounds.Y);

            ratio = size.X/levelBounds.X < size.Y/levelBounds.Y ? 
                size.X/levelBounds.X : size.Y/levelBounds.Y;

            miniLevelRect = levelRect*ratio;

            miniCamPos = new Vector2(cam.Target.X - levelBounds.X, -cam.Target.Y + levelBounds.Y)*ratio-miniCamSize/2;
            miniCamSize = camSize*ratio;
            center = new Vector2((size.X - levelRect.X)/2, (size.Y - levelRect.Y)/2);

            var lvlTopLeft = topLeft + center;

            ImGui.GetWindowDrawList().AddRect(lvlTopLeft, 
            lvlTopLeft + miniLevelRect, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 1)));

            ImGui.GetWindowDrawList().AddRect(lvlTopLeft + miniCamPos + new Vector2(0, miniLevelRect.Y), 
            lvlTopLeft + miniCamPos + miniCamSize + new Vector2(0, miniLevelRect.Y), ImGui.ColorConvertFloat4ToU32(new(0, 0, 1, 1)));

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsMouseDown(ImGuiMouseButton.Left) 
            && ImGui.IsWindowHovered())
            {
                camSave = cam.Target;
            }

            if ((ImGui.IsMouseDown(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Right))
            && ImGui.IsWindowFocused() &&
            ((!ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            || ImGui.IsWindowHovered()))
            {
                var pos = ImGui.GetMousePos();
                pos.Y *= -1;
                cam.Target = new((pos - lvlTopLeft)/ratio + new Vector2(levelBounds.X, levelBounds.Y), 
                cam.Target.Z);
            }

            
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                cam.Target = camSave;
            }
        }
    }
}