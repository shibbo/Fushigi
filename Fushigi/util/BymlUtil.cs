using Fushigi.Byml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public static class BymlUtil
    {
        public static T GetNodeData<T>(IBymlNode node)
        {
            if (node.Id == BymlNodeId.Int64 || node.Id == BymlNodeId.UInt64)
            {
                return ((BymlBigDataNode<T>)node).Data;
            }

            return ((BymlNode<T>)node).Data;
        }

        public static T GetNodeFromArray<T>(BymlArrayNode? array, int idx)
        {
            return ((BymlNode<T>)array[idx]).Data;
        }

        public static BymlNode<T> CreateNode<T>(string name, T data)
        {
            switch (data.GetType().ToString())
            {
                case "System.Int32":
                    return new BymlNode<T>(BymlNodeId.Int, data);
                case "System.UInt32":
                    return new BymlNode<T>(BymlNodeId.UInt, data);
                case "System.Single":
                    return new BymlNode<T>(BymlNodeId.Float, data);
                case "System.UInt64":
                    return new BymlNode<T>(BymlNodeId.UInt64, data);
                case "System.Boolean":
                    return new BymlNode<T>(BymlNodeId.Bool, data);
                case "System.String":
                    return new BymlNode<T>(BymlNodeId.String, data);
            }

            return null;
        }

        public static System.Numerics.Vector3 GetVector3FromArray(BymlArrayNode? array)
        {
            System.Numerics.Vector3 vec = new System.Numerics.Vector3();
            vec.X = GetNodeFromArray<float>(array, 0);
            vec.Y = GetNodeFromArray<float>(array, 1);
            vec.Z = GetNodeFromArray<float>(array, 2);
            return vec;
        }
        
        public static object GetValueFromDynamicNode(IBymlNode node, string dynNode, string type)
        {
            switch (type)
            {
                case "U8":
                case "U16":
                case "U32":
                    return BymlUtil.GetNodeData<uint>(node);
                case "S16":
                case "S32":
                    return BymlUtil.GetNodeData<int>(node);
                case "Bool":
                    return BymlUtil.GetNodeData<bool>(node);
                case "String":
                    return BymlUtil.GetNodeData<string>(node);
                case "F32":
                    return BymlUtil.GetNodeData<float>(node);
            }

            return null;
        }
    }
}
