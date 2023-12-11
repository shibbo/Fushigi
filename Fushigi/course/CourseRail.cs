using Fushigi.Byml;
using Fushigi.param;
using Fushigi.util;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            mRailParam = "Work/Gyml/Rail/RailParam/Default.game__rail__RailParam.gyml";
            mIsClosed = false;
        }

        public CourseRail(BymlHashTable node)
        {
            mAreaHash = BymlUtil.GetNodeData<uint>(node["AreaHash"]);
            mRailParam = BymlUtil.GetNodeData<string>(node["Gyaml"]);
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
                    mParameters.Add(component, c.InitValue);
                }
            }
            else
            {
                var dynamicNode = node["Dynamic"] as BymlHashTable;

                foreach (string component in comp.Keys)
                {
                    if (dynamicNode.ContainsKey(component))
                    {
                        mParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], comp[component]));
                    }
                    else
                    {
                        var c = comp[component];
                        mParameters.Add(component, c.InitValue);
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
            node.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mRailParam), "Gyaml");
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

        public bool TryGetPoint(ulong hash, [NotNullWhen(true)] out CourseRailPoint? point)
        {
            point = mPoints.Find(x => x.mHash == hash);
            return point is not null;
        }

        public CourseRailPoint this[ulong hash]
        {
            get
            {
                bool exists = TryGetPoint(hash, out CourseRailPoint? point);
                Debug.Assert(exists);
                return point!;
            }
        }

        public uint mAreaHash;
        public string mRailParam;
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
                        mParameters.Add(component, c.InitValue);
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
                        mParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], comp[component]));
                    }
                    else
                    {
                        var c = comp[component];
                        mParameters.Add(component, c.InitValue);
                    }
                }
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

        public bool TryGetRail(ulong hash, [NotNullWhen(true)] out CourseRail? rail)
        {
            rail = mRails.Find(x => x.mHash == hash);
            return rail is not null;
        }

        public CourseRail this[ulong hash]
        {
            get
            {
                bool exists = TryGetRail(hash, out CourseRail? rail);
                Debug.Assert(exists);
                return rail!;
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

    public class CourseActorToRailLink
    {
        public CourseActorToRailLink(string linkName)
        {
            mSourceActor = 0;
            mDestRail = 0;
            mDestPoint = 0;
            mLinkName = linkName;
        }

        public CourseActorToRailLink(BymlHashTable table)
        {
            mSourceActor = BymlUtil.GetNodeData<ulong>(table["Src"]);
            mDestRail = BymlUtil.GetNodeData<ulong>(table["Dst"]);
            mLinkName = BymlUtil.GetNodeData<string>(table["Name"]);
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable tbl = new();
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mDestRail), "Dst");
            tbl.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mLinkName), "Name");
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mSourceActor), "Point");
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mSourceActor), "Src");
            return tbl;
        }

        public ulong mSourceActor;
        public ulong mDestRail;
        public ulong mDestPoint;
        public string mLinkName;
    }

    public class CourseActorToRailLinksHolder
    {
        public CourseActorToRailLinksHolder()
        {
        }

        public CourseActorToRailLinksHolder(BymlArrayNode array, CourseActorHolder actorHolder, CourseRailHolder railHolder)
        {
            foreach (BymlHashTable railLink in array.Array)
            {
                mLinks.Add(new CourseActorToRailLink(railLink));
            }
        }

        public bool TryGetLinkWithSrcActor(ulong hash, 
            [NotNullWhen(true)] out CourseActorToRailLink? link)
        {
            link = mLinks.Find(x => x.mSourceActor == hash);

            return link is not null;
        }

        public bool TryGetLinkWithDestRail(ulong hash,
            [NotNullWhen(true)] out CourseActorToRailLink? link)
        {
            link = mLinks.Find(x => x.mDestRail == hash);

            return link is not null;
        }

        public bool TryGetLinkWithDestRailAndPoint(ulong railHash, ulong pointHash,
            [NotNullWhen(true)] out CourseActorToRailLink? link)
        {
            link = mLinks.Find(x => x.mDestRail == railHash && x.mDestPoint == pointHash);

            return link is not null;
        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new((uint)mLinks.Count);

            foreach (var link in mLinks)
            {
                node.AddNodeToArray(link.BuildNode());
            }

            return node;
        }

        public List<CourseActorToRailLink> mLinks = new();
    }
}
