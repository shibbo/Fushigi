using System.Diagnostics.CodeAnalysis;

namespace Fushigi.ui
{
    interface ICommittable
    {
        void Commit(string name);
    }

    internal class EditContextBase
    {
        private class BatchAction(EditContextBase context) : ICommittable
        {
            public string Name { get; private set; }
            public void Commit(string name)
            {
                Name = name;
                context.EndBatchAction(this);
            }
        }

        private readonly Stack<BatchAction> mNestedBatchActions = [];
        private readonly HashSet<object> mSelectedObjects = [];

        private readonly UndoHandler mUndoHandler = new();

        private List<IRevertable>? mCurrentActionBatch;
        private bool mIsRequireSelectionCheck = false;
        private bool mIsRequireUpdate = false;

        private bool mIsSuspendUpdate = false;

        public ulong SelectionVersion { get; private set; } = 0;

        public event Action? Update;

        public ICommittable BeginBatchAction()
        {
            mCurrentActionBatch ??= [];
            var batchAction = new BatchAction(this);
            mNestedBatchActions.Push(batchAction);
            return batchAction;
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

        public void Deselect(object obj)
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.Remove(obj);

            if (mSelectedObjects.Count != countBefore)
                SelectionChanged();
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


        public object? GetLastAction() => mUndoHandler.GetLastAction();
        public IEnumerable<IRevertable> GetRedoUndoStack() => mUndoHandler.GetRedoUndoStack();

        public IEnumerable<T> GetSelectedObjects<T>()
            where T : class
            => mSelectedObjects.OfType<T>();

        //For Undo Window
        public IEnumerable<IRevertable> GetUndoStack() => mUndoHandler.GetUndoStack();

        public bool IsAnySelected<T>()
            where T : class
        {
            return mSelectedObjects.Any(x => x is T);
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

        public void Redo()
        {
            mUndoHandler.Redo();
            Update?.Invoke();
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

        public void Undo()
        {
            mUndoHandler.Undo();
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

        private void DoUpdate()
        {
            if (mIsSuspendUpdate)
            {
                mIsRequireUpdate = true;
                return;
            }
            Update?.Invoke();
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
    }
}