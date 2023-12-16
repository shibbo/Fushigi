using Fushigi.Byml;
using Fushigi.param;
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
            if (node is BymlBigDataNode<T> bigDataNode)
            {
                return bigDataNode.Data;
            }

            return ((BymlNode<T>)node).Data;
        }

        public static object GetNodeValue(IBymlNode node)
        {
            return ((IBymlValueNode)node).GetValue();
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
        
        public static object GetValueFromDynamicNode(IBymlNode node, ParamDB.ComponentParam param)
        {
            if (param.IsUnsignedInt())
                return Convert.ToUInt32(BymlUtil.GetNodeValue(node));
            if (param.IsSignedInt())
                return Convert.ToInt32(BymlUtil.GetNodeValue(node));
            if (param.IsBool())
                return BymlUtil.GetNodeData<bool>(node);
            if (param.IsString())
                return BymlUtil.GetNodeData<string>(node);
            if (param.IsFloat())
            {
                if (node is BymlBigDataNode<double> doubleNode)
                    return (float)doubleNode.Data;

                return BymlUtil.GetNodeData<float>(node);
            }
            if (param.IsDouble())
                return (float)BymlUtil.GetNodeData<double>(node);

            return null!;
        }
    }
}
