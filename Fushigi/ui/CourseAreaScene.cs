using Fushigi.course;
using Fushigi.ui.widgets;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui
{
    interface ISceneRoot
    {
        void Update(ISceneUpdateContext ctx);
    }

    interface ISceneObject
    {
        void Update(ISceneUpdateContext ctx, bool isSelected);
    }

    interface ISceneUpdateContext
    {
        ISceneObject UpdateOrCreateObjFor(object courseObject, Func<ISceneObject> createFunc);
        void AddOrUpdateSceneObject(ISceneObject sceneObject);
    }

    internal class CourseAreaScene : ISceneUpdateContext
    {
        public CourseAreaEditContext EditContext;

        public CourseAreaScene(CourseArea area, ISceneRoot sceneRoot)
        {
            EditContext = new(area);
            EditContext.Update += Update;
            this.mSceneRoot = sceneRoot;
            Update();
        }

        bool mIsUpdating = false;
        int mUpdateBlockers = 0;

        bool mNeedsUpdate = false;

        public void Update()
        {
            if(mIsUpdating) return;

            if (mUpdateBlockers > 0)
            {
                mNeedsUpdate = true;
                return;
            }

            mIsUpdating = true;
            mOrderedSceneObjects.Clear();
            MarkAllDirty();
            mSceneRoot.Update(this);
            CollectDirty();

            mIsUpdating = false;
            mNeedsUpdate = false;
        }

        public bool TryGetObjFor(object courseObject, [NotNullWhen(true)] out ISceneObject? sceneObject)
        {
            bool success = mCourseSceneObjects.TryGetValue(courseObject, out var entry);
            sceneObject = entry.obj;
            return success;
        }

        void ISceneUpdateContext.AddOrUpdateSceneObject(ISceneObject sceneObject)
        {
            if (!mIsUpdating)
                throw new InvalidOperationException("Cannot call this function outside of Update");

            if (!mCourseSceneObjects.TryGetValue(sceneObject, out var entry))
            {
                entry = (sceneObject, isDirty: true);
            }

            if (!entry.isDirty)
                return;

            mOrderedSceneObjects.Add(entry.obj);

            entry.obj.Update(this, false);

            mCourseSceneObjects[sceneObject] = entry with { isDirty = false };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The created/updated scene object</returns>
        ISceneObject ISceneUpdateContext.UpdateOrCreateObjFor(object courseObject, Func<ISceneObject> createFunc)
        {
            if (!mIsUpdating)
                throw new InvalidOperationException("Cannot call this function outside of Update");

            if (!mCourseSceneObjects.TryGetValue(courseObject, out var entry))
            {
                var sceneObject = createFunc.Invoke();
                entry = (sceneObject, isDirty: true);
            }

            if (!entry.isDirty)
                return entry.obj;

            mOrderedSceneObjects.Add(entry.obj);

            entry.obj.Update(this, EditContext.IsSelected(courseObject));

            mCourseSceneObjects[courseObject] = entry with { isDirty = false };
            return entry.obj;
        }

        private void MarkAllDirty()
        {
            foreach (var key in mCourseSceneObjects.Keys)
            {
                ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(mCourseSceneObjects, key);
                value.isDirty = true;
            }
        }

        private void CollectDirty()
        {
            var dirtyEntries = mCourseSceneObjects.Where(x=>x.Value.isDirty).Select(x=>x.Key).ToList();

            foreach (var key in dirtyEntries)
                mCourseSceneObjects.Remove(key);
        }


        /// <summary>
        /// Provides a safe and fast way to invoke an action on every object of a certain
        /// type/with a certain interface
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void ForEach<T>(Action<T> action) 
            where T : class
        {
            mUpdateBlockers++;

            var span = CollectionsMarshal.AsSpan(mOrderedSceneObjects);

            for (int i = 0; i < span.Length; i++)
            {
                var obj = span[i];
                if(obj is T casted)
                    action.Invoke(casted);
            }

            mUpdateBlockers--;
            if (mNeedsUpdate)
                Update();
        }

        /// <summary>
        /// Provides a convenient way to iterate through all objects of a certain
        /// type/with a certain interface
        /// <para>Calling Update directly or indirectly while iterating WILL cause a 
        /// "Collection was modified" exception</para>
        /// </summary>
        /// <returns></returns>
        private IEnumerable<ISceneObject> GetObjects<T>()
            where T : class
            => mOrderedSceneObjects.Where(x=> x is T);

        /// <summary>
        /// Objects that have a direct mapping to an actual CourseObject
        /// </summary>
        private Dictionary<object, (ISceneObject obj, bool isDirty)> mCourseSceneObjects = [];

        private List<ISceneObject> mOrderedSceneObjects = [];
        private readonly ISceneRoot mSceneRoot;
    }
}
