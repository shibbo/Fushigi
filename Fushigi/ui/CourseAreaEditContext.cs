using Fushigi.course;
using Fushigi.ui.undo;
using Fushigi.util;
using System.Diagnostics.CodeAnalysis;

namespace Fushigi.ui
{
    interface ICommittable
    {
        void Commit(string name);
    }

    class CourseAreaEditContext(CourseArea area)
    {
        private readonly HashSet<object> mSelectedObjects = [];

        private readonly UndoHandler mUndoHandler = new();

        private bool mIsSuspendUpdate = false;
        private bool mIsRequireUpdate = false;
        private bool mIsRequireSelectionCheck = false;

        public event Action? Update;

        public ulong SelectionVersion { get; private set; } = 0;

        private void SelectionChanged()
        {
            if (mIsSuspendUpdate)
            {
                mIsRequireSelectionCheck = true;
                return;
            }
            SelectionVersion++;
            Update?.Invoke();
        }

        private void DoUpdate()
        {
            if (mIsSuspendUpdate)
            {
                mIsRequireUpdate = true;
                return;
            }
            Update?.Invoke();
        }

        public void WithSuspendUpdateDo(Action action)
        {
            if (mIsSuspendUpdate)
            {
                action.Invoke();
                return;
            }

            List<object> prevSelection = mSelectedObjects.ToList();

            mIsSuspendUpdate = true;
            action.Invoke();
            mIsSuspendUpdate = false;

            if (mIsRequireSelectionCheck)
            {
                if (prevSelection.Count != mSelectedObjects.Count ||
                    !mSelectedObjects.SetEquals(prevSelection))
                {
                    SelectionChanged();
                    mIsRequireUpdate = true;
                }

                mIsRequireSelectionCheck = false;
            }

            if (mIsRequireUpdate)
            {
                Update?.Invoke();
                mIsRequireUpdate = false;
            }
        }

        private bool mHasDialog = false;

        //For Undo Window
        public IEnumerable<IRevertable> GetUndoStack() => mUndoHandler.GetUndoStack();
        public IEnumerable<IRevertable> GetRedoUndoStack() => mUndoHandler.GetRedoUndoStack();


        public object? GetLastAction() => mUndoHandler.GetLastAction();

        public void Undo()
        {
            mUndoHandler.Undo();
            Update?.Invoke();
        }

        public void Redo()
        {
            mUndoHandler.Redo();
            Update?.Invoke();
        }

        private class BatchAction(CourseAreaEditContext context) : ICommittable
        {
            public string Name { get; private set; }
            public void Commit(string name)
            {
                Name = name;
                context.EndBatchAction(this);
            }
        }

        private List<IRevertable>? mCurrentActionBatch;
        private readonly Stack<BatchAction> mNestedBatchActions = [];

        public ICommittable BeginBatchAction()
        {
            mCurrentActionBatch = [];
            var batchAction = new BatchAction(this);
            mNestedBatchActions.Push(batchAction);
            return batchAction;
        }
        private void EndBatchAction(BatchAction action)
        {
            if (action != mNestedBatchActions.Pop())
                throw new InvalidOperationException($"Nested batch action {action.Name} committed in incorrect order");

            if (mNestedBatchActions.Count > 0)
                //we're still nested
                return;

            if (mCurrentActionBatch is null || mCurrentActionBatch.Count == 0)
                return;

            mUndoHandler.AddToUndo(mCurrentActionBatch, action.Name);
            mCurrentActionBatch = null;
            Update?.Invoke();
        }

        public void CommitAction(IRevertable action)
        {
            if (mCurrentActionBatch is not null)
            {
                mCurrentActionBatch.Add(action);
                return;
            }

            mUndoHandler.AddToUndo(action);
            Update?.Invoke();
        }

        public void DeselectAll()
        {
            if (mSelectedObjects.Count > 0)
                SelectionChanged();

            mSelectedObjects.Clear();
        }

        public void DeselectAllOfType<T>()
            where T : class
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.RemoveWhere(x => x is T);

            if (mSelectedObjects.Count != countBefore)
                SelectionChanged();
        }

        public void Select(ICollection<object> objects)
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.UnionWith(objects);

            if (mSelectedObjects.Count != countBefore)
                SelectionChanged();
        }

        public void Select(object obj)
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.Add(obj);

            if (mSelectedObjects.Count != countBefore)
                SelectionChanged();
        }

        public void Deselect(object obj)
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.Remove(obj);

            if (mSelectedObjects.Count != countBefore)
                SelectionChanged();
        }

        public bool IsSelected(object obj) =>
            mSelectedObjects.Contains(obj);

        public bool IsSingleObjectSelected(object obj) =>
            mSelectedObjects.Count == 1 && mSelectedObjects.Contains(obj);

        public bool IsSingleObjectSelected<T>([NotNullWhen(true)] out T? obj)
            where T : class
        {
            obj = null;
            if (mSelectedObjects.Count != 1)
                return false;

            var _obj = mSelectedObjects.First();
            if (_obj is not T casted) return false;
            obj = casted;
            return true;
        }

        public IEnumerable<T> GetSelectedObjects<T>()
            where T : class
            => mSelectedObjects.OfType<T>();

        public bool IsAnySelected<T>()
            where T : class
        {
            return mSelectedObjects.Any(x => x is T);
        }

        public List<CourseLink> GetLinks()
        {
            return area.mLinkHolder.GetLinks();
        }

        public void AddActor(CourseActor actor)
        {
            CommitAction(area.mActorHolder.GetActors()
                .RevertableAdd(actor, $"{IconUtil.ICON_PLUS_CIRCLE} Add {actor.mActorName}"));
        }

        public void SetActorName(CourseActor actor, string newName)
        {
            actor.mActorName = newName;
        }

        public void SetObjectName(CourseActor actor, string newName)
        {
            actor.mName = newName;
        }
        public void DeleteActor(CourseActor actor)
        {
            var batchAction = BeginBatchAction();

            Console.WriteLine($"Deleting actor {actor.mActorName} [{actor.GetHash()}]");
            Deselect(actor);
            DeleteActorFromAllGroups(actor.GetHash());
            DeleteLinksWithSrcHash(actor.GetHash());
            DeleteLinksWithDestHash(actor.GetHash());
            DeleteRail(actor.GetHash());
            CommitAction(area.mActorHolder.GetActors()
                .RevertableRemove(actor));

            batchAction.Commit($"{IconUtil.ICON_TRASH} Delete {actor.mActorName}");
        }

        public void DeleteSelectedActors()
        {
            var selectedActors = GetSelectedObjects<CourseActor>().ToList();

            var batchAction = BeginBatchAction();

            foreach (var actor in selectedActors)
            {
                DeleteActor(actor);
            }

            batchAction.Commit($"{IconUtil.ICON_TRASH} Delete selected");
        }

        private void DeleteActorFromAllGroups(ulong hash)
        {
            Console.WriteLine($"Deleting actor with {hash} from groups.");
            foreach (var group in area.mGroups.GetGroupsContaining(hash))
                DeleteActorFromGroup(group, hash);
        }

        public void DeleteActorFromGroup(CourseGroup group, ulong hash)
        {
            if (group.TryGetIndexOfActor(hash, out int index))
            {
                CommitAction(
                        group.GetActors().RevertableRemoveAt(index,
                        $"Remove actor {hash} from group")
                    );
            }
        }

        private void DeleteLinksWithDestHash(ulong hash)
        {
            foreach (var index in area.mLinkHolder.GetIndicesOfLinksWithDest_ForDelete(hash))
                DeleteLinkByIndex(index);
        }

        private void DeleteLinksWithSrcHash(ulong hash)
        {
            foreach (var index in area.mLinkHolder.GetIndicesOfLinksWithSrc_ForDelete(hash))
                DeleteLinkByIndex(index);
        }

        public void DeleteLink(string name, ulong src, ulong dest)
        {
            if (area.mLinkHolder.TryGetIndexOfLink(name, src, dest, out int index))
                DeleteLinkByIndex(index);
        }

        private void DeleteLinkByIndex(int index)
        {
            var name = area.mLinkHolder.GetLinks()[index].GetLinkName();
            CommitAction(
                area.mLinkHolder.GetLinks().RevertableRemoveAt(index, $"{IconUtil.ICON_TRASH} Delete {name} Link")
            );
        }

        public bool IsActorDestForLink(CourseActor actor)
        {
            return area.mLinkHolder.GetLinkWithDestHash(actor.GetHash()) != null;
        }

        public void AddLink(CourseLink link)
        {
            Console.WriteLine($"Adding Link: Source: {link.GetSrcHash()} -- Dest: {link.GetDestHash()}");
            CommitAction(
                area.mLinkHolder.GetLinks().RevertableAdd(link,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Add {link.GetLinkName()} Link")
            );
        }

        public void AddBgUnit(CourseUnit unit)
        {
            Console.WriteLine("Adding Course Unit");
            CommitAction(area.mUnitHolder.mUnits.RevertableAdd(unit,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Add Tile Unit"));
        }

        public void DeleteBgUnit(CourseUnit unit)
        {
            Console.WriteLine("Deleting Course Unit");
            CommitAction(area.mUnitHolder.mUnits.RevertableRemove(unit,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Delete Tile Unit"));
        }

        public void AddWall(CourseUnit unit, Wall wall)
        {
            Console.WriteLine("Adding Wall");
            CommitAction(unit.Walls.RevertableAdd(wall,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Add Wall"));
        }

        public void DeleteWall(CourseUnit unit, Wall wall)
        {
            Console.WriteLine("Deleting Wall");
            CommitAction(unit.Walls.RevertableRemove(wall,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Delete Wall"));
        }

        public CourseActorHolder GetActorHolder()
        {
            return area.mActorHolder;
        }

        public void DeleteRail(ulong hash)
        {
            Console.WriteLine($"Removing Rail attached to {hash}");
            area.mRailLinks.RemoveLinkFromSrc(hash);
        }
    }
}
