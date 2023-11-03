using Fushigi.Byml;
using Fushigi.param;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public class CourseRail
    {
        public CourseRail(BymlHashTable node)
        {
            mAreaHash = BymlUtil.GetNodeData<uint>(node["AreaHash"]);
            mGyml = BymlUtil.GetNodeData<string>(node["Gyaml"]);
            mHash = BymlUtil.GetNodeData<ulong>(node["Hash"]);
            mIsClosed = BymlUtil.GetNodeData<bool>(node["IsClosed"]);

            string pointParam = Path.GetFileNameWithoutExtension(BymlUtil.GetNodeData<string>(node["Gyaml"])).Split(".game")[0];
            var railParams = ParamDB.GetRailComponents(pointParam)[0];
            var comp = ParamDB.GetRailComponentParams(railParams);

            if (!node.ContainsKey("Dynamic"))
            {
                foreach (string component in comp.Keys)
                {
                    var c = comp[component];

                    switch (c.Type)
                    {
                        case "S16":
                        case "U32":
                        case "S32":
                            mParameters.Add(component, Convert.ToInt32(comp[component].InitValue));
                            break;
                        case "F32":
                            mParameters.Add(component, Convert.ToSingle(comp[component].InitValue));
                            break;
                        case "Bool":
                            mParameters.Add(component, (bool)comp[component].InitValue);
                            break;
                        case "String":
                            mParameters.Add(component, (string)comp[component].InitValue);
                            break;
                    }
                }
            }
            else
            {
                var dynamicNode = node["Dynamic"] as BymlHashTable;

                foreach (string component in comp.Keys)
                {
                    if (dynamicNode.ContainsKey(component))
                    {
                        mParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], component, comp[component].Type));
                    }
                    else
                    {
                        var c = comp[component];

                        switch (c.Type)
                        {
                            case "S16":
                            case "U32":
                            case "S32":
                                mParameters.Add(component, Convert.ToInt32(comp[component].InitValue));
                                break;
                            case "F32":
                                mParameters.Add(component, Convert.ToSingle(comp[component].InitValue));
                                break;
                            case "Bool":
                                mParameters.Add(component, (bool)comp[component].InitValue);
                                break;
                            case "String":
                                mParameters.Add(component, (string)comp[component].InitValue);
                                break;
                        }
                    }
                }
            }

            var railArray = node["Points"] as BymlArrayNode;

            foreach(BymlHashTable rail in railArray.Array)
            {
                mPoints.Add(new CourseRailPoint(rail, pointParam));
            }
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable node = new();

            node.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>("AreaHash", mAreaHash), "AreaHash");
            node.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("Gyaml", mGyml), "Gyaml");
            node.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Hash", mHash), "Hash");
            node.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>("IsClosed", mIsClosed), "IsClosed");

            BymlHashTable dynamicNode = new();

            foreach (KeyValuePair<string, object> dynParam in mParameters)
            {
                object param = mParameters[dynParam.Key];
                string shit = param.GetType().ToString();

                switch (param.GetType().ToString())
                {
                    case "System.UInt32":
                        dynamicNode.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(dynParam.Key, (uint)param), dynParam.Key);
                        break;
                    case "System.Int32":
                        dynamicNode.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>(dynParam.Key, (int)param), dynParam.Key);
                        break;
                    case "System.Boolean":
                        dynamicNode.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>(dynParam.Key, (bool)param), dynParam.Key);
                        break;
                    case "System.String":
                        dynamicNode.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(dynParam.Key, (string)param), dynParam.Key);
                        break;
                    case "System.Single":
                        dynamicNode.AddNode(BymlNodeId.Float, BymlUtil.CreateNode<float>(dynParam.Key, (float)param), dynParam.Key);
                        break;
                    default:
                        break;
                }
            }

            node.AddNode(BymlNodeId.Hash, dynamicNode, "Dynamic");

            BymlArrayNode pointsArr = new((uint)mPoints.Count);

            foreach (CourseRailPoint pnt in mPoints)
            {
                pointsArr.AddNodeToArray(pnt.BuildNode());
            }

            node.AddNode(BymlNodeId.Array, pointsArr, "Points");

            return node;
        }

        public ulong GetHash()
        {
            return mHash;
        }

        CourseRailPoint GetPoint(ulong hash)
        {
            foreach (CourseRailPoint pnt in mPoints)
            {
                if (pnt.GetHash() == hash)
                {
                    return pnt;
                }
            }

            return null;
        }

        public CourseRailPoint this[ulong hash]
        {
            get
            {
                return GetPoint(hash);
            }
        }

        uint mAreaHash;
        string mGyml;
        ulong mHash;
        bool mIsClosed;
        List<CourseRailPoint> mPoints = new();
        Dictionary<string, object> mParameters = new();

        public class CourseRailPoint
        {
            public CourseRailPoint(BymlHashTable node, string pointParam)
            {
                mHash = BymlUtil.GetNodeData<ulong>(node["Hash"]);
                mTranslate = BymlUtil.GetVector3FromArray(node["Translate"] as BymlArrayNode);
                // we will always use the second entry in the components, since the first one is only used by the rail itself
                var pointParams = ParamDB.GetRailComponents(pointParam)[1];
                var comp = ParamDB.GetRailComponentParams(pointParams);

                if (!node.ContainsKey("Dynamic"))
                {
                    foreach (string component in comp.Keys)
                    {
                        var c = comp[component];

                        switch (c.Type)
                        {
                            case "S16":
                            case "U32":
                            case "S32":
                                mParameters.Add(component, Convert.ToInt32(comp[component].InitValue));
                                break;
                            case "F32":
                                mParameters.Add(component, Convert.ToSingle(comp[component].InitValue));
                                break;
                            case "Bool":
                                mParameters.Add(component, (bool)comp[component].InitValue);
                                break;
                            case "String":
                                mParameters.Add(component, (string)comp[component].InitValue);
                                break;
                        }
                    }

                    /* we're done with this rail, so we exit as we have no more data to read */
                    return;
                }

                var dynamicNode = node["Dynamic"] as BymlHashTable;

                foreach (string component in comp.Keys)
                {
                    if (dynamicNode.ContainsKey(component))
                    {
                        mParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], component, comp[component].Type));
                    }
                    else
                    {
                        var c = comp[component];

                        switch (c.Type)
                        {
                            case "S16":
                            case "U32":
                            case "S32":
                                mParameters.Add(component, Convert.ToInt32(comp[component].InitValue));
                                break;
                            case "F32":
                                mParameters.Add(component, Convert.ToSingle(comp[component].InitValue));
                                break;
                            case "Bool":
                                mParameters.Add(component, (bool)comp[component].InitValue);
                                break;
                            case "String":
                                mParameters.Add(component, (string)comp[component].InitValue);
                                break;
                        }
                    }
                }
            }

            public ulong GetHash()
            {
                return mHash;
            }

            public BymlHashTable BuildNode()
            {
                BymlHashTable tbl = new();
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Hash", mHash), "Hash");

                BymlHashTable dynamicNode = new();

                foreach (KeyValuePair<string, object> dynParam in mParameters)
                {
                    object param = mParameters[dynParam.Key];
                    string shit = param.GetType().ToString();

                    switch (param.GetType().ToString())
                    {
                        case "System.UInt32":
                            dynamicNode.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(dynParam.Key, (uint)param), dynParam.Key);
                            break;
                        case "System.Int32":
                            dynamicNode.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>(dynParam.Key, (int)param), dynParam.Key);
                            break;
                        case "System.Boolean":
                            dynamicNode.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>(dynParam.Key, (bool)param), dynParam.Key);
                            break;
                        case "System.String":
                            dynamicNode.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(dynParam.Key, (string)param), dynParam.Key);
                            break;
                        case "System.Single":
                            dynamicNode.AddNode(BymlNodeId.Float, BymlUtil.CreateNode<float>(dynParam.Key, (float)param), dynParam.Key);
                            break;
                        default:
                            break;
                    }
                }

                tbl.AddNode(BymlNodeId.Hash, dynamicNode, "Dynamic");

                BymlArrayNode translateNode = new(3);
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("X", mTranslate.X));
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Y", mTranslate.Y));
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Z", mTranslate.Z));

                tbl.AddNode(BymlNodeId.Array, translateNode, "Translate");

                return tbl;
            }

            ulong mHash;
            Dictionary<string, object> mParameters = new();
            System.Numerics.Vector3 mTranslate;
        }
    }

    public class CourseRailHolder
    {
        public CourseRailHolder()
        {

        }

        public CourseRailHolder(BymlArrayNode railArray)
        {
            foreach(BymlHashTable rail in railArray.Array)
            {
                mRails.Add(new CourseRail(rail));
            }
        }

        CourseRail GetRail(ulong hash)
        {
            foreach(CourseRail rail in mRails) 
            {
                if (rail.GetHash() == hash)
                {
                    return rail;
                }
            }

            return null;
        }

        public CourseRail this[ulong hash]
        {
            get
            {
                return GetRail(hash);
            }
        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new((uint)mRails.Count);

            foreach (CourseRail rail in mRails)
            {
                node.AddNodeToArray(rail.BuildNode());
            }

            return node;
        }

        List<CourseRail> mRails = new();
    }

    public class CourseActorToRailLinks
    {
        struct Link
        {
            public CourseActor? Source;
            public CourseRail? Dest;
            public CourseRail.CourseRailPoint? Point;
            public string Name;
        }

        public CourseActorToRailLinks(BymlArrayNode array, CourseActorHolder actorHolder, CourseRailHolder railHolder)
        {
            foreach(BymlHashTable railLink in array.Array)
            {
                ulong sourceHash = BymlUtil.GetNodeData<ulong>(railLink["Src"]);
                ulong destHash = BymlUtil.GetNodeData<ulong>(railLink["Dst"]);
                ulong pointHash = BymlUtil.GetNodeData<ulong>(railLink["Point"]);
                string name = BymlUtil.GetNodeData<string>(railLink["Name"]);

                Link link = new();
                link.Source = actorHolder[sourceHash];
                link.Dest = railHolder[destHash];
                link.Point = link.Dest[pointHash];
                link.Name = name;

                mLinks.Add(link);
            }
        }

        public CourseActorToRailLinks()
        {

        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new((uint)mLinks.Count);

            foreach (Link link in mLinks)
            {
                BymlHashTable tbl = new();
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Dst", link.Dest.GetHash()), "Dst");
                tbl.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("Name", link.Name), "Name");
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Point", link.Point.GetHash()), "Point");
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Src", link.Source.GetHash()), "Src");
                node.AddNodeToArray(tbl);
            }

            return node;
        }

        List<Link> mLinks = new();
    }
}
