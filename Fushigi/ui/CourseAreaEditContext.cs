using Fushigi.course;
using Fushigi.ui.undo;
using Fushigi.ui.widgets;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui
{
    class CourseAreaEditContext(CourseArea area)
    {
        private readonly HashSet<object> mSelectedObjects = [];

        private readonly UndoHandler mUndoHandler = new();

        public ulong SelectionVersion { get; private set; } = 0;

        private bool mHasDialog = false;

        public void Undo() => mUndoHandler.Undo();
        public void Redo() => mUndoHandler.Redo();

        public void AddToUndo(IRevertable revertable)
        {
            mUndoHandler.AddToUndo(revertable);
        }

        public void AddToUndo(List<IRevertable> revertable)
        {
            mUndoHandler.AddToUndo(revertable);
        }

        public void BeginUndoCollection()
        {
            mUndoHandler.BeginUndoCollection();
        }

        public void EndUndoCollection()
        {
            mUndoHandler.EndUndoCollection();
        }

        public void DeselectAll()
        {
            if (mSelectedObjects.Count > 0)
                SelectionVersion++;

            mSelectedObjects.Clear();
        }

        public void DeselectAllOfType<T>()
            where T : class
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.RemoveWhere(x=>x is T);

            if (mSelectedObjects.Count != countBefore)
                SelectionVersion++;
        }

        public void Select(ICollection<object> objects)
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.UnionWith(objects);

            if (mSelectedObjects.Count != countBefore)
                SelectionVersion++;
        }

        public void Select(object obj)
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.Add(obj);

            if (mSelectedObjects.Count != countBefore)
                SelectionVersion++;
        }

        public void Deselect(object obj)
        {
            int countBefore = mSelectedObjects.Count;
            mSelectedObjects.Remove(obj);

            if (mSelectedObjects.Count != countBefore)
                SelectionVersion++;
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
            if(_obj is not T casted) return false;
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


        public void AddActor(CourseActor actor)
        {
            mUndoHandler.AddToUndo(area.mActorHolder.GetActors()
                .RevertableAdd(actor, $"Adding {actor.mActorName}"));
        }

        public void DeleteActor(CourseActor actor)
        {
            Deselect(actor);
            mUndoHandler.AddToUndo(area.mActorHolder.GetActors()
                .RevertableRemove(actor, $"Removing {actor.mActorName}"));
        }

        public void DeleteSelectedActors()
        {
            var selectedActors = GetSelectedObjects<CourseActor>().ToList();

            mUndoHandler.BeginUndoCollection();

            foreach (var actor in selectedActors)
                DeleteActor(actor);

            mUndoHandler.EndUndoCollection();
        }

        public bool IsActorDestForLink(CourseActor actor)
        {
            return area.mLinkHolder.GetLinkWithDestHash(actor.GetHash()) != null;
        }
    }
}
