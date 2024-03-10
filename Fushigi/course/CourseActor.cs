using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using Fushigi.param;
using Fushigi.util;
using Silk.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public enum WonderViewType{
        Normal,
        WonderOff,
        WonderOnly
    }
    public class CourseActor
    {
        public CourseActor(BymlHashTable actorNode)
        {
            mPackName = BymlUtil.GetNodeData<string>(actorNode["Gyaml"]);
            mLayer = BymlUtil.GetNodeData<string>(actorNode["Layer"]);

            mTranslation = BymlUtil.GetVector3FromArray(actorNode["Translate"] as BymlArrayNode);
            mRotation = BymlUtil.GetVector3FromArray(actorNode["Rotate"] as BymlArrayNode);
            mScale = BymlUtil.GetVector3FromArray(actorNode["Scale"] as BymlArrayNode);
            mAreaHash = BymlUtil.GetNodeData<uint>(actorNode["AreaHash"]);
            mHash = BymlUtil.GetNodeData<ulong>(actorNode["Hash"]);
            mName = BymlUtil.GetNodeData<string>(actorNode["Name"]);
            mActorPack = ActorPackCache.Load(mPackName);


            if (actorNode.ContainsKey("Dynamic"))
            {
                if (ParamDB.HasActorComponents(mPackName))
                {
                    var actorParameters = new Dictionary<string, object>();

                    List<string> paramList = ParamDB.GetActorComponents(mPackName);

                    foreach (string p in paramList)
                    {
                        var components = ParamDB.GetComponentParams(p);
                        var dynamicNode = actorNode["Dynamic"] as BymlHashTable;

                        foreach (string component in components.Keys)
                        {
                            if (dynamicNode.ContainsKey(component) && !actorParameters.ContainsKey(component))
                            {
                                actorParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], components[component]));
                            }
                            else
                            {
                                if (actorParameters.ContainsKey(component))
                                {
                                    continue;
                                }

                                var c = components[component];
                                actorParameters.Add(component, c.InitValue);
                            }
                        }
                    }

                    mActorParameters = new PropertyDict(actorParameters);
                }
            }
            else
            {
                InitializeDefaultDynamicParams();
            }

            if (actorNode.ContainsKey("System"))
            {
                var systemParameters = new List<PropertyDict.Entry>();

                foreach (string node in ((BymlHashTable)actorNode["System"]).Keys)
                {
                    BymlHashTable systemNode = ((BymlHashTable)actorNode["System"]);
                    var curNode = systemNode[node];
                    object? data = null;

                    switch (curNode.Id)
                    {
                        case BymlNodeId.Int:
                            data = BymlUtil.GetNodeData<int>(curNode);
                            break;
                        case BymlNodeId.Float:
                            data = BymlUtil.GetNodeData<float>(curNode);
                            break;
                        case BymlNodeId.Bool:
                            data = BymlUtil.GetNodeData<bool>(curNode);
                            break;
                        default:
                            throw new Exception("CourseActor::CourseActor() -- You are the chosen one. You have found a type we don't account for in the system params. WOAH!");
                    }

                    systemParameters.Add(new(node, data));
                }

                mSystemParameters = new PropertyDict(systemParameters);
            }
        }

        public CourseActor(string packName, uint areaHash, string actorLayer)
        {
            mPackName = packName;
            mAreaHash = areaHash;
            mLayer = actorLayer;
            mName = "";
            mTranslation = new System.Numerics.Vector3(0.0f);
            mRotation = new System.Numerics.Vector3(0.0f);
            mScale = new System.Numerics.Vector3(1.0f);
            mHash = RandomUtil.GetRandom();
            mActorPack = ActorPackCache.Load(mPackName);

            InitializeDefaultDynamicParams();
        }

        public void InitializeDefaultDynamicParams()
        {
            var actorParameters = new Dictionary<string, object>();

            if (ParamDB.HasActorComponents(mPackName))
            {
                List<string> paramList = ParamDB.GetActorComponents(mPackName);

                foreach (string p in paramList)
                {
                    var components = ParamDB.GetComponentParams(p);

                    foreach (string component in components.Keys)
                    {
                        if (actorParameters.ContainsKey(component))
                        {
                            continue;
                        }

                        var c = components[component];
                        actorParameters.Add(component, c.InitValue);
                    }
                }
            }

            mActorParameters = new PropertyDict(actorParameters);
        }

        public BymlHashTable BuildNode(CourseLinkHolder linkHolder)
        {
            BymlHashTable table = new();
            table.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(mAreaHash), "AreaHash");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mPackName), "Gyaml");
            table.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mHash), "Hash");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mLayer), "Layer");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mName), "Name");

            if (mActorParameters.Count > 0)
            {
                BymlHashTable dynamicNode = new();

                foreach (PropertyDict.Entry dynParam in mActorParameters)
                {
                    object param = mActorParameters[dynParam.Key];

                    var valueNode = BymlUtil.CreateNode(param);
                    dynamicNode.AddNode(valueNode.Id, valueNode, dynParam.Key);
                }

                table.AddNode(BymlNodeId.Hash, dynamicNode, "Dynamic");
            }

            if (mSystemParameters.Count > 0)
            {
                BymlHashTable sysNode = new();

                foreach (KeyValuePair<string, object> sysParam in mSystemParameters)
                {
                    object param = mSystemParameters[sysParam.Key];

                    var valueNode = BymlUtil.CreateNode(param);
                    sysNode.AddNode(valueNode.Id, valueNode, sysParam.Key);
                }

                table.AddNode(BymlNodeId.Hash, sysNode, "System");
            }

            if (linkHolder.GetSrcHashesFromDest(mHash).Values.Count > 0)
            {
                BymlHashTable inLinksNode = new();

                foreach (var (linkName, links) in linkHolder.GetSrcHashesFromDest(mHash))
                {
                    inLinksNode.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>(links.Count), linkName);
                }

                table.AddNode(BymlNodeId.Hash, inLinksNode, "InLinks");
            }

            BymlArrayNode rotateNode = new(3);
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mRotation.X));
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mRotation.Y));
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mRotation.Z));

            table.AddNode(BymlNodeId.Array, rotateNode, "Rotate");

            BymlArrayNode scaleNode = new(3);
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>(mScale.X));
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>(mScale.Y));
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>(mScale.Z));

            table.AddNode(BymlNodeId.Array, scaleNode, "Scale");

            BymlArrayNode translateNode = new(3);
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.X));
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.Y));
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.Z));

            table.AddNode(BymlNodeId.Array, translateNode, "Translate");

            return table;
        }

        public string mPackName;
        public string mName;
        public string mLayer;
        public WonderViewType mWonderView = WonderViewType.Normal;
        public bool wonderVisible = true;
        public System.Numerics.Vector3 mStartingTrans;
        public System.Numerics.Vector3 mTranslation;
        public System.Numerics.Vector3 mRotation;
        public System.Numerics.Vector3 mScale;
        public uint mAreaHash;
        public ulong mHash;
        public PropertyDict mActorParameters = PropertyDict.Empty;
        public PropertyDict mSystemParameters = PropertyDict.Empty;

        public ActorPack mActorPack;
    }

    public class CourseActorHolder
    {
        public CourseActorHolder()
        {

        }

        public CourseActorHolder(BymlArrayNode actorArray)
        {
            foreach (BymlHashTable actor in actorArray.Array)
            {
                mActors.Add(new CourseActor(actor));
            }
        }

        public bool TryGetActor(ulong hash, [NotNullWhen(true)] out CourseActor? actor)
        {
            actor = mActors.Find(x => x.mHash == hash);
            return actor is not null;
        }

        public CourseActor this[ulong hash]
        {
            get
            {
                bool exists = TryGetActor(hash, out CourseActor? actor);
                //Debug.Assert(exists);
                return actor!;
            }
        }

        public BymlArrayNode SerializeToArray(CourseLinkHolder linkHolder)
        {
            BymlArrayNode node = new((uint)mActors.Count);

            foreach (CourseActor actor in mActors)
            {
                node.AddNodeToArray(actor.BuildNode(linkHolder));
            }

            return node;
        }

        public List<CourseActor> mActors = [];
        public List<CourseActor> mSortedActors = [];
    }

    public class CourseActorRender //This can be overridden per actor for individual behavior
    {

        public virtual void Render() 
        {
        }
    }
}
