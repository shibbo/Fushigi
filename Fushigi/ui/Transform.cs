using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui
{
    public class Transform
    {
        public Vector3 Position {  get; set; }
        public Vector3 RotationEuler { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;

        public virtual void Update()
        {

        }
    }
}
