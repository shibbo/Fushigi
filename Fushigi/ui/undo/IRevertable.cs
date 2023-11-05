using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui
{
    public interface IRevertable
    {
        string Name { get; }

        IRevertable Revert();
    }
}
