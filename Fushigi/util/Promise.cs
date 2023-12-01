using System.Diagnostics.CodeAnalysis;

namespace Fushigi.util
{
    public class Promise<T>
    {
        private T? mValue;
        private bool mHasResult = false;
        public void SetResult(T value)
        {
            mValue = value;
            mHasResult = true;
        }

        public bool TryGetResult([NotNullWhen(true)] out T? result)
        {
            result = mValue;
            return mHasResult;
        }
    }
}
