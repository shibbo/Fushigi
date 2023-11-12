using System;
using System.Collections.Generic;
using System.Text;

namespace Fushigi.ui
{
    public class UndoHandler
    {
        public struct RedoEntry
        {
            public IRevertable undoable;
            public IRevertable redoable;
        }

        protected Stack<IRevertable> undoStack = new Stack<IRevertable>();
        protected Stack<RedoEntry> redoStack = new Stack<RedoEntry>();

        //For collection reverting
        List<IRevertable> undoCollection = null;

        public object? GetLastAction()
        {
            if (!undoStack.TryPeek(out IRevertable? top))
                top = null;

            return top;
        }

        /// <summary>
        /// Starts adding called AddToUndo() into a collection.
        /// Must be ended using EndUndoCollection().
        /// </summary>
        public void BeginUndoCollection() {
            //Set a collection to add incomming undo operations
            undoCollection = new List<IRevertable>();
        }

        /// <summary>
        /// Ends the undo collection.
        /// </summary>
        public void EndUndoCollection(string name = "Multi Operation") {
            //Undo ended already, skip
            if (undoCollection == null)
                return;

            //Create a revertable like a normal undo operation but with batch revertables
            if (undoCollection.Count > 0) {
                undoStack.Push(new MultiRevertable(name, undoCollection.ToArray()));
                redoStack.Clear();
            }
            //Reset the undo collection to not be used again
            undoCollection = null;
        }

        public void AddToUndo(List<IRevertable> undoCollection, string name = "Multi Operation")
        {
            if (undoCollection == null)
                return;

            //Create a revertable like a normal undo operation but with batch revertables
            if (undoCollection.Count > 0) {
                undoStack.Push(new MultiRevertable(name, undoCollection.ToArray()));
                redoStack.Clear();
            }
        }

        /// <summary>
        /// Adds a revertable action to the undo stack.
        /// </summary>
        /// <param name="revertable"></param>
        public void AddToUndo(IRevertable revertable)
        {
            //Batch undo collection operation
            if (undoCollection != null)
                undoCollection.Add(revertable);
            else
            {
                //Normal undo operation
                undoStack.Push(revertable);
                redoStack.Clear();
            }
        }

        /// <summary>
        /// Undo the current operation in the undo stack.
        /// </summary>
        public bool Undo()
        {
            bool canUndo = undoStack.Count > 0;
            if (canUndo)
            {
                var undoable = undoStack.Pop();
                var redoable = undoable.Revert();
                redoStack.Push(new RedoEntry { undoable = undoable, redoable = redoable });
            }
            return canUndo;
        }

        /// <summary>
        /// Redo the current operation in the redo stack.
        /// </summary>
        public bool Redo()
        {
            bool canRedo = redoStack.Count > 0;
            if (canRedo)
            {
                var entry = redoStack.Pop();
                entry.redoable.Revert();
                undoStack.Push(entry.undoable);
            }
            return canRedo;
        }

        public class MultiRevertable : IRevertable
        {
            public string Name { get; private set; }

            IRevertable[] revertables;

            public MultiRevertable(string name, IRevertable[] revertables)
            {
                this.Name = name;
                this.revertables = revertables;
            }

            public IRevertable Revert()
            {
                IRevertable[] newRevertables = new IRevertable[revertables.Length];

                int _i = 0;
                for (int i = revertables.Length - 1; i >= 0; i--) //Revertables are meant to be reverted in the reverse order (First In Last Out)
                    newRevertables[_i++] = revertables[i].Revert();

                return new MultiRevertable(Name, newRevertables);
            }
        }
    }
}
