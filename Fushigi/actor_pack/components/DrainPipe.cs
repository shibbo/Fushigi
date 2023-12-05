using Fushigi.Byml.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.actor_pack.components
{
    [Serializable]
    public class DrainPipe
    {
        public string ModelKeyMiddle { get; set; }
        public string ModelKeyTop { get; set; }
    }
}
