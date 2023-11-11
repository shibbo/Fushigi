using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class Camera
    {
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Target = Vector3.Zero;
        public float Distance = 10;

        public float Fov = MathF.PI / 2;

        public float Width;
        public float Height;

        public float AspectRatio => Width / Height;

        public Matrix4x4 ProjectionMatrix { get; private set; }
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ViewProjectionMatrix { get; private set; }
        public Matrix4x4 ViewProjectionMatrixInverse { get; private set; }

        public CameraFrustum CameraFrustum = new CameraFrustum();

        public bool InFrustum(BoundingBox box) {
            return CameraFrustum.CheckIntersection(this, box, 1f);
        }

        public bool UpdateMatrices()
        {
            float tanFOV = MathF.Tan(Fov / 2);

            ProjectionMatrix = Matrix4x4.CreateOrthographic(AspectRatio * tanFOV * Distance, tanFOV * Distance,
                -1000, 1000);
            ViewMatrix = Matrix4x4.CreateTranslation(-Target);
            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;

            if (!Matrix4x4.Invert(ViewProjectionMatrix, out var inv))
                return false;

            //Temp. since matrices are updated per frame, check for any changes
            //So we don't have to keep updating frustum info each frame
            if (inv != ViewProjectionMatrixInverse)
                CameraFrustum.UpdateCamera(this);

            ViewProjectionMatrixInverse = inv;


            return true;
        }
    }
}
