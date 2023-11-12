using Fushigi.util;
using System;
using System.Collections.Generic;
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

        public Transform Transform;

        public Vector3 PreviousPosition;
        public Vector3 PreviousRotation;
        public Vector3 PreviousScale;

        public TransformUndo(Transform transform, string name = $"{IconUtil.ICON_ARROWS_ALT} Transform")
        {
            //Undo display name
            Name = name;
            //The transform to update
            Transform = transform;
            //The state to revert to
            PreviousPosition = transform.Position;
            PreviousRotation = transform.RotationEuler;
            PreviousScale = transform.Scale;
        }

        public IRevertable Revert()
        {
            //Revert transform instance
            var redo = new TransformUndo(Transform);

            Transform.Position = this.PreviousPosition;
            Transform.RotationEuler = this.PreviousRotation;
            Transform.Scale = this.PreviousScale;
            Transform.OnUpdate();

            //Create revert stack
            return redo;
        }
    }
}
