using Fushigi.Byml.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.actor_pack.components
{
    [Serializable]
    public class GamePhysics
    {
        public string ControllerSetPath { get; set; }
    }

    [Serializable]
    public class ControllerSetParam
    {
        public List<ShapeName> ShapeNamePathAry { get; set; }
    }

    [Serializable]
    public class ShapeName
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    public class ShapeParam
    {
        [BymlProperty("AutoCalc")]
        public AutoCalc mCalc { get; set; }

        [BymlProperty("Box")]
        public List<Box> mBox { get; set; }

        [BymlProperty("Sphere")]
        public List<Sphere> mSphere { get; set; } 

        [BymlProperty("Capsule")]
        public List<Sphere> mCapsule { get; set; }
    }

    [Serializable]
    public class AutoCalc
    {
        [BymlProperty("Axis")]
        public Vector3 mAxis { get; set; }

        [BymlProperty("Center")]
        public Vector3 mCenter { get; set; }

        [BymlProperty("Min")]
        public Vector3 mMin { get; set; }

        [BymlProperty("Max")]
        public Vector3 mMax { get; set; }
    }

    [Serializable]
    public class Box
    {
        [BymlProperty("Center")]
        public Vector3 mCenter { get; set; }

        [BymlProperty("HalfExtents")]
        public Vector3 mExtents { get; set; }

    }

    [Serializable]
    public class Sphere
    {
        public float Radius { get; set; }

        [BymlProperty("Center")]
        public Vector3 mCenter { get; set; }

        
    }

    [Serializable]
    public class Capsule
    {
        public float Radius { get; set; }

        [BymlProperty("CenterA")]
        public Vector3 mCenterA { get; set; }

        [BymlProperty("CenterB")]
        public Vector3 mCenterB { get; set; }   
    }
}