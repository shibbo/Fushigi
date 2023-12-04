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
        Vector3 OldPos;
        Vector3 NewPos;

        public TransformUndo(Transform transform, Vector3 oldPos, Vector3 newPos, string name = $"{IconUtil.ICON_ARROWS_ALT} Transform")
        {
            //Undo display name
            Name = name;
            Transform = transform;
            OldPos = oldPos;
            NewPos = newPos;
        }

        public IRevertable Revert()
        {
            //Revert transform instance
            var redo = new TransformUndo(Transform, NewPos, OldPos);

            Transform.Position = OldPos;
            Transform.OnUpdate();

            //Create revert stack
            return redo;
        }
    }
}
