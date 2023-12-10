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
        private (Vector3 pos, int index)? addPointPos;

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

        private (Vector3 pos, int index)? EvaluateAddPointPos(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            if (!ImGui.GetIO().KeyAlt || !ctx.IsSelected(rail))
                return null;

            Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
            Vector3 pos = new(
                 MathF.Round(posVec.X, MidpointRounding.AwayFromZero),
                 MathF.Round(posVec.Y, MidpointRounding.AwayFromZero),
                 2);

            if (rail.Points.Count == 0)
                return (pos, 0);

            if (rail.Points.Count == 1)
                return (pos, 1);


            //find best index to insert at (minimizing distance)

            var min = (distance: float.PositiveInfinity, index: 0);

            int segmentCount = rail.Points.Count;
            if (!rail.IsClosed)
                segmentCount--;

            Vector3 pointA, pointB;

            for (int i = 0; i < segmentCount; i++)
            {
                pointA = rail.Points[i].Position;
                pointB = rail.Points.GetWrapped(i + 1).Position;

                var length = (pointB - pointA).Length();
                var dir = (pointB - pointA) / length;
                var t = Vector3.Dot(pos - pointA, dir) / length;
                if (t < 0 || t > 1)
                    continue;

                var normal = Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitZ));
                float distance = MathF.Abs(Vector3.Dot(pos - pointA, normal));

                var delta = distance;
                if (delta <= min.distance)
                    min = (delta, i + 1);
            }

            if (rail.IsClosed)
            {
                if (min.distance == float.PositiveInfinity)
                    return null;

                return (pos, min.index);
            }
            
            //!rail is not closed here

            //prefer appending/prepending
            //only allow inserting in the middle if the point is close enough to or on the edge
            if (min.distance < 1)
                return (pos, min.index);


            pointA = rail.Points[0].Position;
            pointB = rail.Points[^1].Position;
            if (Vector3.Distance(pointA, pos) < Vector3.Distance(pointB, pos))
                return (pos, 0);
            else
                return (pos, rail.Points.Count);
        }

        public void OnMouseDown(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            bool isSelected = IsSelected(ctx);

            if (!isSelected)
                return;

            mouseDownPos = viewport.ScreenToWorld(ImGui.GetMousePos());

            if(addPointPos.TryGetValue(out var addPos))
            {
                DeselectAll(ctx);
                InsertPoint(ctx, new BGUnitRail.RailPoint(rail, addPos.pos), addPos.index);
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

            addPointPos = EvaluateAddPointPos(ctx, viewport);

            if ((addPointPos.HasValue && ctx.IsSelected(rail)) || HitTest(viewport))
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
                    nextPos2D = viewport.WorldToScreen(
                        rail.Points[i + 1].Position);
                }
                else if (rail.IsClosed) //last point to first if closed
                {
                    nextPos2D = viewport.WorldToScreen(
                       rail.Points[0].Position);
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

            //draw visual hint for added point
            if (addPointPos.TryGetValue(out var addPos))
            {
                var pos2D = viewport.WorldToScreen(addPos.pos);

                if(rail.Points.Count > 0)
                {
                    int index = addPos.index;
                    var pointA = viewport.WorldToScreen(rail.Points.GetWrapped(index - 1).Position);
                    var pointB = viewport.WorldToScreen(rail.Points.GetWrapped(index).Position);
                    var pointC = pos2D;

                    dl.AddTriangleFilled(pointA, pointB, pointC, 0x99FFFFFF);
                    if(rail.IsClosed || index > 0)
                        dl.AddLine(pointA, pointC, 0xFFFFFFFF, 2.5f);
                    if(rail.IsClosed || index < rail.Points.Count)
                        dl.AddLine(pointB, pointC, 0xFFFFFFFF, 2.5f);

                    if (!rail.IsClosed)
                    {
                        if (index == 0)
                            ImGui.SetTooltip("Prepend point");
                        else if (index == rail.Points.Count)
                            ImGui.SetTooltip("Append point");
                        else
                            ImGui.SetTooltip("Insert point");
                    }

                }
                
                dl.AddCircleFilled(pos2D, 3.5f, 0xFFFFFFFF);
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
