using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class CameraRay
    {
        public Vector4 Origin { get; set; }
        public Vector4 Far { get; set; }
        public Vector3 Direction { get; set; }

        public bool TransformPoint(Vector3 dir, Vector3 currentPosition, out Vector3 newPos)
        {
            newPos = currentPosition;

            if (this.IntersectsPlane(dir, currentPosition, out float intersectDist)) {
                newPos = this.Origin.Xyz() + (this.Direction * intersectDist);

                return true;
            }
            return false;
        }

        public bool IntersectsPlane(Vector3 normal, Vector3 point, out float intersectDist)
        {
            float denom = Vector3.Dot(Direction, normal);
            if (Math.Abs(denom) == 0)
            {
                intersectDist = 0f;
                return false;
            }

            intersectDist = Vector3.Dot(point - Origin.Xyz(), normal) / denom;
            return intersectDist > 0f;
        }

        public static CameraRay ScreenToWorld(Vector2 pos, Matrix4x4 viewProjectionMatrixInverse, int width, int height)
        {
            Vector3 mousePosA = new Vector3(pos.X, pos.Y, -1f);
            Vector3 mousePosB = new Vector3(pos.X, pos.Y, 1f);

            Vector4 nearUnproj = UnProject(viewProjectionMatrixInverse, mousePosA, width, height);
            Vector4 farUnproj = UnProject(viewProjectionMatrixInverse, mousePosB, width, height);

            Vector3 dir = (farUnproj - nearUnproj).Xyz();
            Vector3.Normalize(dir);

            return new CameraRay() { Origin = nearUnproj, Far = farUnproj, Direction = dir };
        }

        public static Vector4 UnProject(Matrix4x4 viewProjectionMatrixInverse, Vector3 mouse, int width, int height)
        {
            Vector4 vec = new Vector4();

            vec.X = 2.0f * mouse.X / width - 1;
            vec.Y = -(2.0f * mouse.Y / height - 1);
            vec.Z = mouse.Z;
            vec.W = 1.0f;

            vec = Vector4.Transform(vec, viewProjectionMatrixInverse);

            if (vec.W > float.Epsilon || vec.W < float.Epsilon)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }
            return vec;
        }
    }
}
