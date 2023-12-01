using Fasterflect;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Fushigi.ui.undo
{
    public abstract class PropertyCapture<T>
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

        protected abstract IRevertable GetRevertable((string name, object oldValue)[] changedProperties);

        public bool TryGetRevertable([NotNullWhen(true)] out IRevertable? revertable,
            [NotNullWhen(true)] out string[]? names)
        {
            (string name, object oldValue)[] changedProperties =
                mCapturedProperties
                .Where(x => !Equals(GetValue(x.name), x.oldValue))
                .Select(x => (x.name, x.oldValue))
                .ToArray();

            if (changedProperties.Length == 0)
            {
                revertable = null;
                names = null;
                return false;
            }

            revertable = GetRevertable(changedProperties);
            names = new string[changedProperties.Length];
            for (int i = 0; i < changedProperties.Length; i++)
                names[i] = changedProperties[i].name;
            return true;
        }
    }

    public class PropertyFieldsCapture : PropertyCapture<object?>
    {
        public static string[] GetPropertyFieldNames(object target)
        {
            List<string> names = [];

            foreach (var info in target.GetType().Fields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldType = info.Type();
                if (!(fieldType.IsValueType || fieldType == typeof(string)))
                    continue;

                names.Add(info.Name);
            }

            return names.ToArray();
        }

        public static readonly PropertyFieldsCapture Empty = new PropertyFieldsCapture();

        private PropertyFieldsCapture()
            : base(null, Array.Empty<string>()) { }

        public PropertyFieldsCapture(object target)
            : base(target, GetPropertyFieldNames(target)) { }

        protected override IRevertable GetRevertable((string name, object oldValue)[] changedProperties)
        {
            Debug.Assert(mTarget != null);
            return new PropertyFieldsSetUndo(mTarget, changedProperties);
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

    public class PropertyFieldsSetUndo(object target, (string name, object oldValue)[] changedProperties) : IRevertable
    {
        public string Name => "Change properties";

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

    public class PropertyDictCapture : PropertyCapture<IDictionary<string, object>?>
    {
        public static readonly PropertyDictCapture Empty = new PropertyDictCapture();

        private PropertyDictCapture()
            : base(null, Array.Empty<string>()) { }

        public PropertyDictCapture(IDictionary<string, object> target): base(target, [.. target.Keys]) { }

        protected override IRevertable GetRevertable((string name, object oldValue)[] changedProperties)
        {
            Debug.Assert(mTarget != null);
            return new PropertyDictSetUndo(mTarget, changedProperties);
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

    public class PropertyDictSetUndo(IDictionary<string, object> target,
        (string name, object oldValue)[] changedProperties) : IRevertable
    {
        public string Name => "Change properties";

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
}
