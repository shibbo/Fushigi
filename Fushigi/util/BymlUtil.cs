using Fushigi.Byml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        [Obsolete("Use CreateNode(data) instead")]
        public static IBymlNode CreateNode<T>(string name, T data) 
            => CreateNode<T>(data);
        public static IBymlNode CreateNode<T>(T data)
        {
            var node = data switch
            {
                int value => new BymlNode<int>(BymlNodeId.Int, value),
                uint value => new BymlNode<uint>(BymlNodeId.UInt, value),
                float value => new BymlNode<float>(BymlNodeId.Float, value),
                ulong value => new BymlNode<ulong>(BymlNodeId.UInt64, value),
                bool value => new BymlNode<bool>(BymlNodeId.Bool, value),
                string value => new BymlNode<string>(BymlNodeId.String, value),
                null => CreateNullNode(),
                _ => null
            };
            Debug.Assert(node is not null);
            return node;
        }

        public static IBymlNode CreateNullNode() => new BymlNode<uint>(BymlNodeId.Null, 0);

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
            if (type == "U8" && node.Id == BymlNodeId.Int)
            {
                type = "S32";
            }
            else if (type == "U8" && node.Id == BymlNodeId.UInt)
            {
                type = "U32";
            }

            switch (type)
            {
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
