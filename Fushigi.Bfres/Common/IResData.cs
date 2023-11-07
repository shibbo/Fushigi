using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    public interface IResData
    {
        void Read(BinaryReader reader);
    }
}
