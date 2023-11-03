using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public static class RandomUtil
    {
        public static ulong GetRandom()
        {
            byte[] buf = new byte[8];
            sRandom.NextBytes(buf);
            return BitConverter.ToUInt64(buf);
        }
        static Random sRandom = new();
    }
}
