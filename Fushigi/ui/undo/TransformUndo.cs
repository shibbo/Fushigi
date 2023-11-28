using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.ui
{
    public class TransformUndo : IRevertable
    {
        public string Name { get; set; }

        Transform Transform;

        public TransformUndo(Transform transform, string name = $"{IconUtil.ICON_ARROWS_ALT} Transform")
        {
            //Undo display name
            Name = name;
            Transform = transform;
        }

        public IRevertable Revert()
        {
            //Revert transform instance
            var redo = new TransformUndo(Transform);

            Transform.OnUpdate();

            //Create revert stack
            return redo;
        }
    }
}
