using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fushigi.util;

namespace Fushigi.ui.modal
{
    public interface IPopupModal<TResult>
    {
        void DrawModalContent(Promise<TResult> promise);
    }
}
