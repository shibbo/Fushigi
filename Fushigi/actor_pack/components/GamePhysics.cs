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
        [BymlProperty("ControllerSetPath", DefaultValue = "")]
        public string mPath { get; set; }
    }

    [Serializable]
    public class ControllerSetParam
    {
        [BymlProperty("$parent")]
        public string parent { get; set; }
        
        public List<PathAry> ShapeNamePathAry { get; set; }

        [BymlProperty("MatterRigidBodyNamePathAry", DefaultValue = "")]
        public List<PathAry> mRigids { get; set; }

        [BymlProperty("RigidBodyEntityNamePathAry", DefaultValue = "")]
        public List<PathAry> mEntity { get; set; }

        [BymlProperty("RigidBodySensorNamePathAry", DefaultValue = "")]
        public List<PathAry> mSensor { get; set; }
    }

    [Serializable]
    public class PathAry
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    public class ShapeParamList
    {
        [BymlProperty("AutoCalc")]
        public AutoCalc mCalc { get; set; }

        [BymlProperty("Box", DefaultValue = "")]
        public List<ShapeParam> mBox { get; set; }

        [BymlProperty("Sphere", DefaultValue = "")]
        public List<ShapeParam> mSphere { get; set; } 

        [BymlProperty("Capsule", DefaultValue = "")]
        public List<ShapeParam> mCapsule { get; set; }

        [BymlProperty("Polytope", DefaultValue = "")]
        public List<ShapeParam> mPoly { get; set; }
    }

    [Serializable]
    public class RigidParam
    {
        public string ShapeName { get; set; }

        public List<object> ShapeNames { get; set; }
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
    public class ShapeParam
    {
        [BymlProperty("AutoCalc", DefaultValue = "")]
        public AutoCalc mCalc { get; set; }

        public float Radius { get; set; }

        [BymlProperty("Center")]
        public Vector3 mCenter { get; set; }

        [BymlProperty("HalfExtents")]
        public Vector3 mExtents { get; set; }

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