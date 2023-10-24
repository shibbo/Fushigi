using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public static class NullableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T>(this T? self, out T result)
            where T : struct
        {
            result = self.GetValueOrDefault();
            return self.HasValue;
        }
    }
}
