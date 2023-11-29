using Fushigi.Byml;
using Fushigi.param;
using Fushigi.util;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public CourseRail(uint areaHash)
        {
            mHash = RandomUtil.GetRandom();
            mAreaHash = areaHash;
            mGyml = "Work/Gyml/Rail/RailParam/Default.game__rail__RailParam.gyml";
            mIsClosed = false;
        }

        public CourseRail(BymlHashTable node)
        {
            mAreaHash = BymlUtil.GetNodeData<uint>(node["AreaHash"]);
            mGyml = BymlUtil.GetNodeData<string>(node["Gyaml"]);
            mHash = BymlUtil.GetNodeData<ulong>(node["Hash"]);
            mIsClosed = BymlUtil.GetNodeData<bool>(node["IsClosed"]);

            string pointParam = Path.GetFileNameWithoutExtension(BymlUtil.GetNodeData<string>(node["Gyaml"])).Split(".game")[0];
            var railParams = ParamDB.GetRailComponent(pointParam);
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

            node.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(mAreaHash), "AreaHash");
            node.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mGyml), "Gyaml");
            node.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mHash), "Hash");
            node.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>(mIsClosed), "IsClosed");

            BymlHashTable dynamicNode = new();

            foreach (KeyValuePair<string, object> dynParam in mParameters)
            {
                object param = mParameters[dynParam.Key];
                var valueNode = BymlUtil.CreateNode(param);
                dynamicNode.AddNode(valueNode.Id, valueNode, dynParam.Key);
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

        public uint mAreaHash;
        string mGyml;
        public ulong mHash;
        public bool mIsClosed;
        public List<CourseRailPoint> mPoints = new();
        public Dictionary<string, object> mParameters = new();

        public class CourseRailPoint
        {
            public CourseRailPoint()
            {
                this.mHash = RandomUtil.GetRandom();
                this.mTranslate = new System.Numerics.Vector3();
            }


            public CourseRailPoint(CourseRailPoint point)
            {
                this.mHash = RandomUtil.GetRandom();
                this.mTranslate = point.mTranslate;
                this.mControl = point.mControl;
                foreach (var param in point.mParameters)
                    this.mParameters.Add(param.Key, param.Value);
            }

            public CourseRailPoint(BymlHashTable node, string pointParam)
            {
                mHash = BymlUtil.GetNodeData<ulong>(node["Hash"]);
                mTranslate = BymlUtil.GetVector3FromArray(node["Translate"] as BymlArrayNode);

                IDictionary<string, ParamDB.ComponentParam> comp;
                if (ParamDB.TryGetRailPointComponent(pointParam, out var componentName))
                    comp = ParamDB.GetRailComponentParams(componentName);
                else
                    comp = ImmutableDictionary.Create<string, ParamDB.ComponentParam>();

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

                if (node.ContainsKey("Control1"))
                {
                    mControl = BymlUtil.GetVector3FromArray(node["Control1"] as BymlArrayNode);
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
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mHash), "Hash");

                BymlHashTable dynamicNode = new();

                foreach (KeyValuePair<string, object> dynParam in mParameters)
                {
                    object param = mParameters[dynParam.Key];
                    var valueNode = BymlUtil.CreateNode(param);
                    dynamicNode.AddNode(valueNode.Id, valueNode, dynParam.Key);
                }

                tbl.AddNode(BymlNodeId.Hash, dynamicNode, "Dynamic");

                if (mControl != null)
                {
                    BymlArrayNode controlNode = new(3);
                    controlNode.AddNodeToArray(BymlUtil.CreateNode<float>("X", mControl.Value.X));
                    controlNode.AddNodeToArray(BymlUtil.CreateNode<float>("Y", mControl.Value.Y));
                    controlNode.AddNodeToArray(BymlUtil.CreateNode<float>("Z", mControl.Value.Z));

                    tbl.AddNode(BymlNodeId.Array, controlNode, "Control1");
                }

                BymlArrayNode translateNode = new(3);
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("X", mTranslate.X));
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Y", mTranslate.Y));
                translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Z", mTranslate.Z));

                tbl.AddNode(BymlNodeId.Array, translateNode, "Translate");

                return tbl;
            }

            public ulong mHash;
            public Dictionary<string, object> mParameters = new();
            public System.Numerics.Vector3 mTranslate;
            public System.Numerics.Vector3? mControl = null;
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

        public List<CourseRail> mRails = new();
    }

    public class CourseActorToRailLinks
    {
        public struct Link
        {
            public ulong Source;
            public ulong Dest;
            public ulong Point;
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
                link.Source = sourceHash;
                link.Dest = destHash;
                link.Point = pointHash;
                link.Name = name;

                mLinks.Add(link);
            }
        }

        public CourseActorToRailLinks()
        {

        }

        public void RemoveLinkFromSrc(ulong hash)
        {
            int idx = -1;
            foreach (Link link in mLinks)
            {
                if (link.Source == hash)
                {
                    idx = mLinks.IndexOf(link);
                    break;
                }
            }

            if (idx != -1)
            {
                mLinks.RemoveAt(idx);
            }
        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new((uint)mLinks.Count);

            foreach (Link link in mLinks)
            {
                BymlHashTable tbl = new();
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Dst", link.Dest), "Dst");
                tbl.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("Name", link.Name), "Name");
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Point", link.Point), "Point");
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Src", link.Source), "Src");
                node.AddNodeToArray(tbl);
            }

            return node;
        }

        public List<Link> mLinks = new();
    }
}
