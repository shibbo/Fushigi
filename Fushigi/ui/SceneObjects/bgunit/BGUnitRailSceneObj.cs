using Fushigi.course;
using Fushigi.ui.undo;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System.Numerics;

namespace Fushigi.ui.SceneObjects.bgunit
{
    internal class BGUnitRailSceneObj(CourseUnit unit, BGUnitRail rail) : ISceneObject, IViewportDrawable, IViewportSelectable
    {
        public IReadOnlyDictionary<BGUnitRail.RailPoint, RailPoint> ChildPoints;
        public List<BGUnitRail.RailPoint> GetSelected(CourseAreaEditContext ctx) => rail.Points.Where(ctx.IsSelected).ToList();

        public bool mouseDown = false;
        public bool transformStart = false;

        public bool Visible = true;

        public uint Color_Default = 0xFFFFFFFF;
        public uint Color_SelectionEdit = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));
        public uint Color_SlopeError = 0xFF0000FF;

        private Vector3 mouseDownPos;

        public CourseUnit CourseUnit = unit;

        public void Update(ISceneUpdateContext ctx, bool isSelected)
        {
            Dictionary<BGUnitRail.RailPoint, RailPoint> railPoints = [];

            if (isSelected)
            {
                foreach (var pt in rail.Points)
                {
                    var railPointObj = ctx.UpdateOrCreateObjFor(pt, () => new RailPoint(pt));

                    railPoints[pt] = (RailPoint)railPointObj;
                }
            }

            ChildPoints = railPoints;
        }

        private bool IsSelected(CourseAreaEditContext ctx) => ctx.IsSelected(rail);

        private void DeselectAll(CourseAreaEditContext ctx)
        {
            ctx.WithSuspendUpdateDo(() =>
            {
                foreach (var point in rail.Points)
                    ctx.Deselect(point);
            });

        }

        public void SelectAll(CourseAreaEditContext ctx)
        {
            ctx.WithSuspendUpdateDo(() =>
            {
                foreach (var point in rail.Points)
                    ctx.Select(point);
            });
        }

        public void InsertPoint(CourseAreaEditContext ctx, BGUnitRail.RailPoint point, int index)
        {
            var revertible = rail.Points.RevertableInsert(point, index,
                $"{IconUtil.ICON_PLUS_CIRCLE} Rail Point Add");

            ctx.CommitAction(revertible);
            ctx.Select(point);
            CourseUnit.GenerateTileSubUnits();
        }

        public void AddPoint(CourseAreaEditContext ctx, BGUnitRail.RailPoint point)
        {
            var revertible = rail.Points.RevertableAdd(point,
                $"{IconUtil.ICON_PLUS_CIRCLE} Rail Point Add");

            ctx.CommitAction(revertible);
            ctx.Select(point);
            CourseUnit.GenerateTileSubUnits();
        }

        public void RemoveSelected(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            var selected = GetSelected(ctx);
            if (selected.Count == 0)
                return;

            var batchAction = ctx.BeginBatchAction();

            foreach (var point in selected)
            {
                var revertible = rail.Points.RevertableRemove(point);
                ctx.CommitAction(revertible);
            }

            batchAction.Commit($"{IconUtil.ICON_TRASH} Delete Rail Points");

            CourseUnit.GenerateTileSubUnits();
        }

        public void OnKeyDown(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            //TODO move the delete logic over to CourseAreaEditContext and remove this
            if (ImGui.IsKeyPressed(ImGuiKey.Delete))
                RemoveSelected(ctx, viewport);
            if (IsSelected(ctx) && ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.A))
                SelectAll(ctx);
        }

        private bool HitTest(LevelViewport viewport)
        {
            return LevelViewport.HitTestLineLoopPoint(GetPoints(viewport), 10f,
                    ImGui.GetMousePos());
        }

        public void OnMouseDown(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            bool isSelected = IsSelected(ctx);

            if (!isSelected)
                return;

            mouseDownPos = viewport.ScreenToWorld(ImGui.GetMousePos());

            var selected = GetSelected(ctx);

            if (ImGui.GetIO().KeyAlt && selected.Count == 1)
            {
                var index = rail.Points.IndexOf(selected[0]);
                //Insert and add
                Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
                Vector3 pos = new(
                     MathF.Round(posVec.X, MidpointRounding.AwayFromZero),
                     MathF.Round(posVec.Y, MidpointRounding.AwayFromZero),
                     selected[0].Position.Z);

                if (rail.Points.Count - 1 == index) //is last point
                    AddPoint(ctx, new BGUnitRail.RailPoint(rail, pos));
                else
                    InsertPoint(ctx, new BGUnitRail.RailPoint(rail, pos), index + 1);
            }
            else if (ImGui.GetIO().KeyAlt && selected.Count == 0) //Add new point from last 
            {
                Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
                Vector3 pos = new(
                     MathF.Round(posVec.X, MidpointRounding.AwayFromZero),
                     MathF.Round(posVec.Y, MidpointRounding.AwayFromZero),
                     2);

                //find best index to insert at (minimizing circumference)

                var min = (delta: float.PositiveInfinity, index: 0);

                int segmentCount = rail.Points.Count;

                if (!rail.IsClosed && rail.Points.Any())
                {
                    segmentCount--;
                    var delta = Vector3.Distance(rail.Points[0].Position, pos);
                    if (delta < min.delta)
                        min = (delta, 1);
                }

                for (int i = 0; i < segmentCount; i++)
                {
                    var pointA = rail.Points[i];
                    var pointB = rail.Points[(i+1)%rail.Points.Count];

                    var distance = Vector3.Distance(pointA.Position, pointB.Position);
                    var newDistanceSum = Vector3.Distance(pointA.Position, pos) +
                        Vector3.Distance(pointB.Position, pos);

                    var delta = newDistanceSum-distance;
                    if (delta < min.delta)
                        min = (delta, i+1);
                }

                DeselectAll(ctx);
                InsertPoint(ctx, new BGUnitRail.RailPoint(rail, pos), min.index);
            }
            else
            {
                if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                    DeselectAll(ctx);
            }

            for (int i = 0; i < rail.Points.Count; i++)
            {
                Vector3 point = rail.Points[i].Position;

                if (ChildPoints.TryGetValue(rail.Points[i], out RailPoint? childPoint))
                    childPoint.PreviousPosition = point;
            }
            mouseDown = true;
        }

        private Vector2[] GetPoints(LevelViewport viewport)
        {
            Vector2[] points = new Vector2[rail.Points.Count];
            for (int i = 0; i < rail.Points.Count; i++)
            {
                Vector3 p = rail.Points[i].Position;
                points[i] = viewport.WorldToScreen(new(p.X, p.Y, p.Z));
            }
            return points;
        }

        public void OnMouseUp(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            mouseDown = false;

            if (transformStart)
            {
                var batchAction = ctx.BeginBatchAction();

                foreach (var item in mTransformUndos)
                    ctx.CommitAction(item);

                batchAction.Commit($"{IconUtil.ICON_ARROWS_ALT} Move Rail Points");

                transformStart = false;
            }
        }

        private List<TransformUndo> mTransformUndos = [];

        public void OnSelecting(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            if (!mouseDown)
                return;

            Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
            Vector3 diff = posVec - mouseDownPos;

            if (diff.X != 0 && diff.Y != 0 && !transformStart)
            {
                transformStart = true;
            }

            bool anyTransformed = false;
            if (transformStart)
            {
                //this will repeatedly add new undos so we have to clear first
                //not exactly efficient but a lot less error prone then doing preparation separately from update
                //atleast until we have a proper "ongoing action"-system
                mTransformUndos.Clear();
                for (int i = 0; i < rail.Points.Count; i++)
                {
                    if (!ctx.IsSelected(rail.Points[i]))
                        continue;

                    if (!ChildPoints.TryGetValue(rail.Points[i], out RailPoint? childPoint))
                        continue;

                    diff.X = MathF.Round(diff.X, MidpointRounding.AwayFromZero);
                    diff.Y = MathF.Round(diff.Y, MidpointRounding.AwayFromZero);
                    posVec.Z = rail.Points[i].Position.Z;

                    var newPos = childPoint.PreviousPosition + diff;

                    if (!ImGui.GetIO().KeyCtrl || IsValidPosition(newPos, i))
                    {
                        mTransformUndos.Add(new TransformUndo(
                            childPoint.Transform, 
                            childPoint.PreviousPosition, 
                            newPos));

                        rail.Points[i].Position = newPos;
                    }

                    anyTransformed = true;
                }
            }

            if (anyTransformed)
                CourseUnit.GenerateTileSubUnits();
        }

        void IViewportDrawable.Draw2D(CourseAreaEditContext ctx, LevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj)
        {
            if (!Visible)
                return;

            if ((ImGui.GetIO().KeyAlt && ctx.IsSelected(rail)) || HitTest(viewport))
                isNewHoveredObj = true;

            bool isSelected = IsSelected(ctx);

            if (ImGui.IsMouseClicked(0) && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                OnMouseDown(ctx, viewport);
            if (ImGui.IsMouseReleased(0))
                OnMouseUp(ctx, viewport);

            //TODO does it still need a condition like this?
            //if (viewport.mEditorState == LevelViewport.EditorState.Selecting)
            OnSelecting(ctx, viewport);

            OnKeyDown(ctx, viewport);

            var lineThickness = viewport.IsHovered(this) ? 3.5f : 2.5f;

            for (int i = 0; i < rail.Points.Count; i++)
            {
                Vector3 point = rail.Points[i].Position;
                var pos2D = viewport.WorldToScreen(new(point.X, point.Y, point.Z));

                //Next pos 2D
                Vector2 nextPos2D = Vector2.Zero;
                if (i < rail.Points.Count - 1) //is not last point
                {
                    nextPos2D = viewport.WorldToScreen(new(
                        rail.Points[i + 1].Position.X,
                        rail.Points[i + 1].Position.Y,
                        rail.Points[i + 1].Position.Z));
                }
                else if (rail.IsClosed) //last point to first if closed
                {
                    nextPos2D = viewport.WorldToScreen(new(
                       rail.Points[0].Position.X,
                       rail.Points[0].Position.Y,
                       rail.Points[0].Position.Z));
                }
                else //last point but not closed, draw no line
                    continue;

                uint line_color = IsValidAngle(pos2D, nextPos2D) ? Color_Default : Color_SlopeError;
                if (isSelected && line_color != Color_SlopeError)
                    line_color = Color_SelectionEdit;

                dl.AddLine(pos2D, nextPos2D, line_color, lineThickness);

                if (isSelected)
                {
                    //Arrow display
                    Vector3 next = i < rail.Points.Count - 1 ? rail.Points[i + 1].Position : rail.Points[0].Position;
                    Vector3 dist = next - rail.Points[i].Position;
                    var angleInRadian = MathF.Atan2(dist.Y, dist.X); //angle in radian
                    var rotation = Matrix4x4.CreateRotationZ(angleInRadian);

                    float width = 1f;

                    var line = Vector3.TransformNormal(new Vector3(0, width, 0), rotation);

                    Vector2[] arrow =
                    [
                        viewport.WorldToScreen(rail.Points[i].Position + dist / 2f),
                        viewport.WorldToScreen(rail.Points[i].Position + dist / 2f + line),
                    ];
                    float alpha = 0.5f;

                    dl.AddLine(arrow[0], arrow[1], ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha)), lineThickness);
                }
            }
        }

        private bool IsValidAngle(Vector2 point1, Vector2 point2)
        {
            var dist = point2 - point1;
            var angleInRadian = MathF.Atan2(dist.Y, dist.X); //angle in radian
            var angle = angleInRadian * (180.0f / (float)Math.PI); //to degrees

            //TODO improve check and simplify

            //The game supports 30 and 45 degree angle variants
            //Then ground (0) and wall (90)
            float[] validAngles = new float[]
            {
                0, -0,
                27, -27,
                45, -45,
                90, -90,
                135,-135,
                153,-153,
                180,-180,
            };

            return validAngles.Contains(MathF.Round(angle));
        }

        /// <summary>
        /// Check if the proposed position is valid within the rail
        /// </summary>
        /// <param name="pos">new proposed position</param>
        /// <param name="index">index of the point to set to the new position</param>
        /// <returns>true if the point is valid there, false otherwise</returns>
        private bool IsValidPosition(Vector3 pos, int index)
        {
            Vector2 newPos = new(pos.X, pos.Y);

            // Check if the point has valid angles with the points coming before and after it
            int[] offsets = [-1, 1];
            foreach (var offset in offsets)
            {
                var neighborIndex = (index + offset) % rail.Points.Count;

                if (neighborIndex < 0)
                    neighborIndex += rail.Points.Count;

                Vector2 neighborPos = new(rail.Points[neighborIndex].Position.X,
                    rail.Points[neighborIndex].Position.Y);

                if (!IsValidAngle(newPos, neighborPos))
                    return false;
            }

            return true;
        }

        void IViewportSelectable.OnSelect(CourseAreaEditContext editContext)
        {
            IViewportSelectable.DefaultSelect(editContext, rail);
        }

        public class RailPoint : ISceneObject, IViewportSelectable, IViewportDrawable
        {
            public RailPoint(BGUnitRail.RailPoint point)
            {
                this.point = point;
                //TODO remove this as soon as we have an ITransformable interface with a SetTransform
                Transform.Update += () =>
                {
                    point.Position = Transform.Position;
                };
            }

            private readonly BGUnitRail.RailPoint point;

            public Transform Transform = new Transform();

            //For transforming
            public Vector3 PreviousPosition { get; set; }

            private bool HitTest(LevelViewport viewport)
            {
                var pos2D = viewport.WorldToScreen(point.Position);
                Vector2 pnt = new(pos2D.X, pos2D.Y);
                return (ImGui.GetMousePos() - pnt).Length() < 6.0f;
            }

            void ISceneObject.Update(ISceneUpdateContext ctx, bool isSelected)
            {

            }

            void IViewportSelectable.OnSelect(CourseAreaEditContext ctx)
            {
                ctx.WithSuspendUpdateDo(() =>
                {
                    IViewportSelectable.DefaultSelect(ctx, point);
                    ctx.Select(point.mRail);
                });
            }

            void IViewportDrawable.Draw2D(CourseAreaEditContext ctx, LevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj)
            {
                var pos2D = viewport.WorldToScreen(point.Position);

                //Display point color
                uint color = 0xFFFFFFFF;
                if (ctx.IsSelected(point))
                    color = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));

                dl.AddCircleFilled(pos2D, viewport.IsHovered(this) ? 6.0f : 4.0f, color);

                if (HitTest(viewport))
                    isNewHoveredObj = true;
            }
        }
    }
}
