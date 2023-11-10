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
            => mUndoHandler.AddToUndo(revertable);

        public void AddToUndo(List<IRevertable> revertable)
            => mUndoHandler.AddToUndo(revertable);

        public void BeginUndoCollection() 
            => mUndoHandler.BeginUndoCollection();
        public void EndUndoCollection(string actionName) 
            => mUndoHandler.EndUndoCollection(actionName);

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

        public List<CourseLink> GetLinks()
        {
            return area.mLinkHolder.GetLinks();
        }

        public void AddActor(CourseActor actor)
        {
            mUndoHandler.AddToUndo(area.mActorHolder.GetActors()
                .RevertableAdd(actor, $"Add {actor.mActorName}"));
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
            mUndoHandler.BeginUndoCollection();
            Console.WriteLine($"Deleting actor {actor.mActorName} [{actor.GetHash()}]");
            Deselect(actor);
            DeleteActorFromAllGroups(actor.GetHash());
            DeleteLinksWithSrcHash(actor.GetHash());
            DeleteLinksWithDestHash(actor.GetHash());
            DeleteRail(actor.GetHash());
            mUndoHandler.AddToUndo(area.mActorHolder.GetActors()
                .RevertableRemove(actor));

            mUndoHandler.EndUndoCollection($"Delete {actor.mActorName}");
        }

        public void DeleteSelectedActors()
        {
            var selectedActors = GetSelectedObjects<CourseActor>().ToList();

            mUndoHandler.BeginUndoCollection();

            foreach (var actor in selectedActors)
            {
                DeleteActor(actor);
            }

            mUndoHandler.EndUndoCollection();
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
                AddToUndo(
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
            AddToUndo(
                area.mLinkHolder.GetLinks().RevertableRemoveAt(index, $"Delete {name} Link")
            );
        }

        public bool IsActorDestForLink(CourseActor actor)
        {
            return area.mLinkHolder.GetLinkWithDestHash(actor.GetHash()) != null;
        }

        public void AddLink(CourseLink link)
        {
            Console.WriteLine($"Adding Link: Source: {link.GetSrcHash()} -- Dest: {link.GetDestHash()}");
            AddToUndo(
                area.mLinkHolder.GetLinks().RevertableAdd(link, 
                    $"Add {link.GetLinkName()} Link")
            );
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
