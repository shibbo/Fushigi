﻿using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using Fushigi.param;
using Fushigi.util;
using Silk.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public class CourseActor
    {
        public CourseActor(BymlHashTable actorNode)
        {
            mActorParameters = new Dictionary<string, object>();

            mActorName = BymlUtil.GetNodeData<string>(actorNode["Gyaml"]);
            mLayer = BymlUtil.GetNodeData<string>(actorNode["Layer"]);

            mTranslation = BymlUtil.GetVector3FromArray(actorNode["Translate"] as BymlArrayNode);
            mRotation = BymlUtil.GetVector3FromArray(actorNode["Rotate"] as BymlArrayNode);
            mScale = BymlUtil.GetVector3FromArray(actorNode["Scale"] as BymlArrayNode);
            mAreaHash = BymlUtil.GetNodeData<uint>(actorNode["AreaHash"]);
            mActorHash = BymlUtil.GetNodeData<ulong>(actorNode["Hash"]);
            mName = BymlUtil.GetNodeData<string>(actorNode["Name"]);
            mActorPack = ActorPackCache.Load(mActorName);


            if (actorNode.ContainsKey("Dynamic"))
            {
                if (ParamDB.HasActorComponents(mActorName))
                {
                    List<string> paramList = ParamDB.GetActorComponents(mActorName);

                    foreach (string p in paramList)
                    {
                        var components = ParamDB.GetComponentParams(p);
                        var dynamicNode = actorNode["Dynamic"] as BymlHashTable;

                        foreach (string component in components.Keys)
                        {
                            if (dynamicNode.ContainsKey(component) && !mActorParameters.ContainsKey(component))
                            {
                                mActorParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], component, components[component].Type));
                            }
                            else
                            {
                                if (mActorParameters.ContainsKey(component))
                                {
                                    continue;
                                }

                                var c = components[component];

                                switch (c.Type)
                                {
                                    case "U8":
                                    case "S16":
                                    case "U32":
                                    case "S32":
                                        mActorParameters.Add(component, Convert.ToInt32(components[component].InitValue));
                                        break;
                                    case "F32":
                                        mActorParameters.Add(component, Convert.ToSingle(components[component].InitValue));
                                        break;
                                    case "Bool":
                                        mActorParameters.Add(component, (bool)components[component].InitValue);
                                        break;
                                    case "String":
                                        mActorParameters.Add(component, (string)components[component].InitValue);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                InitializeDefaultDynamicParams();
            }

            if (actorNode.ContainsKey("System"))
            {
                foreach(string node in ((BymlHashTable)actorNode["System"]).Keys)
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

                    mSystemParameters.Add(node, data);
                }
            }
        }

        public CourseActor(string actorName, uint areaHash, string actorLayer)
        {
            mActorName = actorName;
            mAreaHash = areaHash;
            mLayer = actorLayer;
            mName = "";
            mTranslation = new System.Numerics.Vector3(0.0f);
            mRotation = new System.Numerics.Vector3(0.0f);
            mScale = new System.Numerics.Vector3(1.0f);
            mActorHash = RandomUtil.GetRandom();
            mActorParameters = new();
            mSystemParameters = new();
            mActorPack = ActorPackCache.Load(mActorName);

            InitializeDefaultDynamicParams();
        }

        public void InitializeDefaultDynamicParams()
        {
            mActorParameters.Clear();

            if (ParamDB.HasActorComponents(mActorName))
            {
                List<string> paramList = ParamDB.GetActorComponents(mActorName);

                foreach (string p in paramList)
                {
                    var components = ParamDB.GetComponentParams(p);

                    foreach (string component in components.Keys)
                    {
                        if (mActorParameters.ContainsKey(component))
                        {
                            continue;
                        }

                        var c = components[component];

                        switch (c.Type)
                        {
                            case "U8":
                            case "S16":
                            case "U32":
                            case "S32":
                                mActorParameters.Add(component, Convert.ToInt32(components[component].InitValue));
                                break;
                            case "F32":
                                mActorParameters.Add(component, Convert.ToSingle(components[component].InitValue));
                                break;
                            case "Bool":
                                mActorParameters.Add(component, (bool)components[component].InitValue);
                                break;
                            case "String":
                                mActorParameters.Add(component, (string)components[component].InitValue);
                                break;
                        }
                    }
                }
            }
        }

        public ulong GetHash()
        {
            return mActorHash;
        }

        public BymlHashTable BuildNode(CourseLinkHolder linkHolder)
        {
            BymlHashTable table = new();
            table.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(mAreaHash), "AreaHash");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mActorName), "Gyaml");
            table.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mActorHash), "Hash");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mLayer), "Layer");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mName), "Name");

            if (mActorParameters.Count > 0)
            {
                BymlHashTable dynamicNode = new();

                foreach (KeyValuePair<string, object> dynParam in mActorParameters)
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

            if (linkHolder.GetSrcHashesFromDest(mActorHash).Values.Count > 0)
            {
                BymlHashTable inLinksNode = new();

                foreach (var (linkName, links) in linkHolder.GetSrcHashesFromDest(mActorHash))
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

        public string  mActorName;
        public string mName;
        public string mLayer;
        public System.Numerics.Vector3 mStartingTrans;
        public System.Numerics.Vector3 mTranslation;
        public System.Numerics.Vector3 mRotation;
        public System.Numerics.Vector3 mScale;
        public uint mAreaHash;
        public ulong mActorHash;
        public Dictionary<string, object> mActorParameters;
        public Dictionary<string, object> mSystemParameters = new();

        public ActorPack mActorPack;
    }

    public class CourseActorHolder
    {
        public CourseActorHolder(BymlArrayNode actorArray)
        {
            foreach (BymlHashTable actor in actorArray.Array)
            {
                mCourseActors.Add(new CourseActor(actor));
            }
        }

        public CourseActorHolder()
        {
        }

        CourseActor GetActor(ulong hash)
        {
            foreach (var actor in mCourseActors)
            {
                if (actor.GetHash() == hash)
                {
                    return actor;
                }
            }

            return null;
        }

        public bool HasHash(ulong hash)
        {
            return mCourseActors.Any(x => x.GetHash() == hash);
        }

        public CourseActor this[ulong hash]
        {
            get => GetActor(hash);
        }

        public List<CourseActor> GetActors()
        {
            return mCourseActors;
        }

        public BymlArrayNode SerializeToArray(CourseLinkHolder linkHolder)
        {
            BymlArrayNode node = new((uint)mCourseActors.Count);

            foreach (CourseActor actor in mCourseActors)
            {
                node.AddNodeToArray(actor.BuildNode(linkHolder));
            }

            return node;
        }

        List<CourseActor> mCourseActors = new List<CourseActor>();
    }

    public class CourseActorRender //This can be overridden per actor for individual behavior
    {

        public virtual void Render() 
        {
        }
    }
}
