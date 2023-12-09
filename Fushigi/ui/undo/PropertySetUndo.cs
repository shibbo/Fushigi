using Fasterflect;
using Fushigi.util;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Fushigi.ui.undo
{
    public interface IPropertyCapture
    {
        bool HasChanges();
        bool HasChangesSinceLastCheckpoint();
        void MakeCheckpoint();
        bool TryGetRevertable([NotNullWhen(true)] out IRevertable? revertable, Func<string[], string> nameFunc);
    }

    public abstract class PropertyCapture<T> : IPropertyCapture
    {
        protected readonly T mTarget;

        private readonly (string name, object oldValue, object lastCheckpointValue)[] mCapturedProperties;

        public PropertyCapture(T target, string[] propertiesToCapture)
        {
            mTarget = target;
            mCapturedProperties = new (string name, object oldValue, object lastCheckpointValue)[propertiesToCapture.Length];

            for (int i = 0; i < mCapturedProperties.Length; i++)
            {
                string name = propertiesToCapture[i];
                object value = GetValue(name);
                mCapturedProperties[i] = (name, value, value);
            }
        }

        protected abstract object GetValue(string name);
        protected abstract void SetValue(string name, object value);

        public bool HasChanges() => mCapturedProperties.Any(x => !Equals(GetValue(x.name), x.oldValue));
        public bool HasChangesSinceLastCheckpoint() => mCapturedProperties.Any(x => !Equals(GetValue(x.name), x.lastCheckpointValue));
        public void MakeCheckpoint()
        {
            for (int i = 0; i < mCapturedProperties.Length; i++)
            {
                mCapturedProperties[i].lastCheckpointValue = GetValue(mCapturedProperties[i].name);
            }
        }

        protected abstract IRevertable GetRevertable((string name, object oldValue)[] changedProperties, string name);

        public bool TryGetRevertable([NotNullWhen(true)] out IRevertable? revertable,
            Func<string[], string> nameFunc)
        {
            (string name, object oldValue)[] changedProperties =
                mCapturedProperties
                .Where(x => !Equals(GetValue(x.name), x.oldValue))
                .Select(x => (x.name, x.oldValue))
                .ToArray();

            if (changedProperties.Length == 0)
            {
                revertable = null;
                return false;
            }
            
            var names = new string[changedProperties.Length];
            for (int i = 0; i < changedProperties.Length; i++)
                names[i] = changedProperties[i].name;

            revertable = GetRevertable(changedProperties, nameFunc(names));
            
            return true;
        }
    }

    public class PropertyFieldsCapture : PropertyCapture<object?>
    {
        internal static BindingFlags sBindingFlags = BindingFlags.Instance | BindingFlags.Public;
        public static string[] GetPropertyFieldNames(object target, params Type[] additionalFieldCaptureTypes)
        {
            List<string> names = [];

            foreach (var info in target.GetType().Fields(sBindingFlags))
            {
                var fieldType = info.Type();
                if (!(fieldType.IsValueType || 
                      fieldType == typeof(string) ||
                      additionalFieldCaptureTypes.Contains(fieldType)))
                    continue;

                names.Add(info.Name);
            }

            return names.ToArray();
        }

        public static readonly PropertyFieldsCapture Empty = new PropertyFieldsCapture();

        private PropertyFieldsCapture()
            : base(null, Array.Empty<string>()) { }

        public PropertyFieldsCapture(object target, params Type[] additionalFieldCaptureTypes)
            : base(target, GetPropertyFieldNames(target, additionalFieldCaptureTypes)) { }

        protected override IRevertable GetRevertable((string name, object oldValue)[] changedProperties, string name)
        {
            Debug.Assert(mTarget != null);
            return new PropertyFieldsSetUndo(mTarget, changedProperties, name);
        }

        protected override object GetValue(string name)
        {
            Debug.Assert(mTarget != null);
            return mTarget.GetFieldValue(name);
        }

        protected override void SetValue(string name, object value)
        {
            Debug.Assert(mTarget != null);
            mTarget.SetFieldValue(name, value);
        }
    }

    public class PropertyFieldsSetUndo(object target, (string name, object oldValue)[] changedProperties,
        string name = "Change properties") : IRevertable
    {
        public string Name => name;

        public IRevertable Revert()
        {
            var newChangedProperties = new (string name, object oldValue)[changedProperties.Length];

            for (int i = 0; i < changedProperties.Length; i++)
            {
                var (name, value) = changedProperties[i];
                newChangedProperties[i] = (name, target.GetFieldValue(name));
                target.SetFieldValue(name, value);
            }

            return new PropertyFieldsSetUndo(target, newChangedProperties);
        }
    }

    public class PropertyDictCapture : PropertyCapture<PropertyDict?>
    {
        public static readonly PropertyDictCapture Empty = new PropertyDictCapture();

        private PropertyDictCapture()
            : base(null, Array.Empty<string>()) { }

        public PropertyDictCapture(PropertyDict target): base(target, [.. target.Keys]) { }

        protected override IRevertable GetRevertable((string name, object oldValue)[] changedProperties, string name)
        {
            Debug.Assert(mTarget != null);
            return new PropertyDictSetUndo(mTarget, changedProperties, name);
        }

        protected override object GetValue(string name)
        {
            Debug.Assert(mTarget != null);
            return mTarget[name];
        }

        protected override void SetValue(string name, object value)
        {
            Debug.Assert(mTarget != null);
            mTarget[name] = value;
        }
    }

    public class PropertyDictSetUndo(PropertyDict target,
        (string name, object oldValue)[] changedProperties, 
        string name = "Change properties") : IRevertable
    {
        public string Name => name;

        public IRevertable Revert()
        {
            var newChangedProperties = new (string name, object oldValue)[changedProperties.Length];

            for (int i = 0; i < changedProperties.Length; i++)
            {
                var (name, value) = changedProperties[i];
                newChangedProperties[i] = (name, target[name]);
                target[name] = value;
            }

            return new PropertyDictSetUndo(target, newChangedProperties);
        }
    }

    public class FullPropertyCapture : IPropertyCapture
    {
        public static readonly FullPropertyCapture Empty = new FullPropertyCapture();
        private readonly PropertyFieldsCapture mFieldsCapture = PropertyFieldsCapture.Empty;
        private readonly PropertyDictCapture[] mDictCaptures = [];

        private FullPropertyCapture() { }

        public FullPropertyCapture(object target)
        { 
            mFieldsCapture = new PropertyFieldsCapture(target, typeof(PropertyDict));

            List<PropertyDictCapture> dictCaptures = [];

            foreach (var info in target.GetType().Fields(PropertyFieldsCapture.sBindingFlags))
            {
                var fieldType = info.Type();
                if (fieldType != typeof(PropertyDict))
                    continue;

                var value = target.GetFieldValue(info.Name);
                dictCaptures.Add(new PropertyDictCapture((PropertyDict)value));
            }

            mDictCaptures = dictCaptures.ToArray();
        }

        public bool HasChanges() => 
            mFieldsCapture.HasChanges() || 
            mDictCaptures.Any(x => x.HasChanges());

        public bool HasChangesSinceLastCheckpoint() =>
            mFieldsCapture.HasChangesSinceLastCheckpoint() || 
            mDictCaptures.Any(x => x.HasChangesSinceLastCheckpoint());

        public void MakeCheckpoint()
        {
            mFieldsCapture.MakeCheckpoint();
            foreach (var capture in mDictCaptures)
                capture.MakeCheckpoint();
        }

        public bool TryGetRevertable([NotNullWhen(true)] out IRevertable? revertable,
            Func<string[], string> nameFunc)
        {
            List<IRevertable> revertables = [];
            List<string> allNames = [];

            if(mFieldsCapture.TryGetRevertable(out IRevertable? fieldRevertable, 
                names => { allNames.AddRange(names); return null!; }))
            {
                revertables.Add(fieldRevertable);
            }

            foreach (var capture in mDictCaptures)
            {
                if (!capture.TryGetRevertable(out IRevertable? dictRevertable, 
                    names => { allNames.AddRange(names); return null!; }))
                    continue;

                revertables.Add(dictRevertable);
            }

            if (revertables.Count == 0)
            {
                revertable = null;
                return false;
            }

            var names = allNames.ToArray();
            revertable = new UndoHandler.MultiRevertable(nameFunc(names), [.. revertables]);
            return true;
        }
    }
}
