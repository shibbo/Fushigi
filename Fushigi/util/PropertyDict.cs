using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Fushigi.util.PropertyDict;

namespace Fushigi.util
{
    /// <summary>
    /// A sorted list of entries similar to a dictionary, with 0 memory overhead, that prevents it's key set from ever changing
    /// </summary>
    public class PropertyDict : IEnumerable<Entry>
    {
        public struct Entry(string key, object value)
        {
            public string Key = key; 
            public object Value = value;

            public static implicit operator Entry(KeyValuePair<string, object> pair)
                => new(pair.Key, pair.Value);
            public static implicit operator KeyValuePair<string, object>(Entry entry)
                => new(entry.Key, entry.Value);
        }

        public static readonly PropertyDict Empty = new PropertyDict([]);

        public PropertyDict(IEnumerable<Entry> pairs)
        {
            mEntries = pairs.DistinctBy(x => x.Key).ToArray();
            Array.Sort(mEntries, CompareKeys);
        }

        public PropertyDict(IReadOnlyDictionary<string, object> dict)
        {
            mEntries = dict.Select(x=>(Entry)x).ToArray();
            Array.Sort(mEntries, CompareKeys);
        }

        private PropertyDict(Entry[] entries)
        {
            mEntries = entries;
        }

        public int Count => mEntries.Length;

        public IEnumerable<string> Keys => mEntries.Select(x=>x.Key);

        public object this[string key]
        {
            get => GetValueRef(key);
            set => GetValueRef(key) = value;
        }

        public ref object GetValueRef(string key)
        {
            if (!TryGetIndex(key, out var index))
                throw new KeyNotFoundException(key);

            return ref mEntries[index].Value;
        }

        public object? GetValueOrDefault(string key, object? defaultValue = null)
            => TryGetValue(key, out var value) ? value : defaultValue;

        public bool TryGetValue(string key, out object? value)
        {
            if(TryGetIndex(key, out var index))
            {
                value = mEntries[index].Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool ContainsKey(string key) => TryGetIndex(key, out _);

        public Dictionary<string, object> ToDictionary() => 
            new(mEntries.Select(x=>(KeyValuePair<string, object>)x));

        private bool TryGetIndex(string key, out int index) => TryGetIndex(mEntries, key, out index);

        private static bool TryGetIndex(IReadOnlyList<Entry> entries, string key, out int index)
        {
            var start = 0;
            var end = entries.Count - 1;

            while (start <= end)
            {
                var mid = (start + end) / 2;
                var entry = entries[mid];
                var cmp = entry.Key.CompareTo(key);

                if (cmp == 0)
                {
                    index = mid;
                    return true;
                }
                if (cmp > 0)
                    end = mid - 1;
                else /* if (cmp < 0) */
                    start = mid + 1;
            }

            index = start;
            return true;
        }

        private static int CompareKeys(Entry l, Entry r)
            => l.Key.CompareTo(r.Key);

        public IEnumerator<Entry> GetEnumerator()
        {
            return ((IEnumerable<Entry>)mEntries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mEntries.GetEnumerator();
        }

        readonly Entry[] mEntries;
    }
}
