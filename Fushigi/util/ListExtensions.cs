using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public static class ListExtensions
    {
        public static T GetWrapped<T>(this IReadOnlyList<T> list, int index)
        {
            int x = index;
            int y = list.Count;
            if (x > 0)
                return list[x % y];
            if (x < 0)
                return list[(x + 1) % y + y - 1];
            return list[0];
        }
    }
}
