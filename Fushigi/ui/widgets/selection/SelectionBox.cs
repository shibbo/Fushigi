using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    /// <summary>
    /// Draws and detects a 2D selection box for selecting objects.
    /// </summary>
    public class SelectionBox
    {
        /// <summary>
        /// The smallest point in the box. 
        /// </summary>
        public Vector2 MinPoint
        {
            get
            {
                return new Vector2(
               MathF.Min(startPoint.X, endPoint.X),
               MathF.Min(startPoint.Y, endPoint.Y));
            }
        }

        /// <summary>
        /// The largest point in the box. 
        /// </summary>
        public Vector2 MaxPoint
        {
            get
            {
                return new Vector2(
               MathF.Max(startPoint.X, endPoint.X),
               MathF.Max(startPoint.Y, endPoint.Y));
            }
        }

        /// <summary>
        /// Determines if the box is currently active to display.
        /// </summary>
        public bool IsActive => Action != SelectAction.None;

        //Start mouse point
        private Vector2 startPoint;
        //End mouse point
        private Vector2 endPoint;
        //Action for selection or deselection
        private SelectAction Action = SelectAction.None;

        /// <summary>
        /// Starts the selection box action.
        /// </summary>
        public void StartSelection()
        {
            startPoint = ImGui.GetIO().MousePos;
            endPoint = ImGui.GetIO().MousePos;
            Action = SelectAction.Select;
        }

        /// <summary>
        /// Ends the selection box. 
        /// </summary>
        public void EndSelection()
        {
            Action = SelectAction.None;
        }

        /// <summary>
        /// Checks if the given point is within the selection box in screen space.
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        public bool IsInside(Vector2 screenPoint)
        {
            return (screenPoint.X < MaxPoint.X && screenPoint.X > MinPoint.X &&
                    screenPoint.Y < MaxPoint.Y && screenPoint.Y > MinPoint.Y);
        }

        /// <summary>
        /// Draws the selection box.
        /// </summary>
        public void Render(ImDrawListPtr drawList)
        {
            if (this.Action == SelectAction.None)
                return;

            endPoint = ImGui.GetIO().MousePos;

            drawList.AddRectFilled(this.MinPoint, this.MaxPoint, ImGui.ColorConvertFloat4ToU32(
                new Vector4(0.5f, 0.5f, 0.5f, 0.15f)));

            drawList.AddRect(this.MinPoint, this.MaxPoint, ImGui.ColorConvertFloat4ToU32(
                new Vector4(1, 1, 1, 0.25f)), 0, ImDrawFlags.Closed, 2.5f);
        }

        enum SelectAction
        {
            None,
            Select,
            Deselect,
        }
    }
}
