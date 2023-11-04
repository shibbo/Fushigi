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

        public TransformUndo(Transform transform, string name = "Transform")
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
            Transform.Position = this.PreviousPosition;
            Transform.RotationEuler = this.PreviousRotation;
            Transform.Scale = this.PreviousScale;
            Transform.Update();

            //Create revert stack
            return new TransformUndo(Transform);
        }
    }
}
