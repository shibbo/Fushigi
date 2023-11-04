using Fushigi.Byml;
using Fushigi.param;
using Fushigi.util;
using Silk.NET.Core;
using System;
using System.Collections.Generic;
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
                // does our actor have no dynamic node? well, let's set up our default settings then
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

        public CourseActor(string actorName, uint areaHash)
        {
            mActorName = actorName;
            mAreaHash = areaHash;
            mLayer = "PlayArea1";
            mName = "";
            mTranslation = new System.Numerics.Vector3(0.0f);
            mRotation = new System.Numerics.Vector3(0.0f);
            mScale = new System.Numerics.Vector3(1.0f);
            mActorHash = RandomUtil.GetRandom();
            mActorParameters = new();

            List<string> paramList = ParamDB.GetActorComponents(mActorName);

            foreach (string p in paramList)
            {
                var components = ParamDB.GetComponentParams(p);

                foreach (string component in components.Keys)
                {
                    var c = components[component];

                    switch (c.Type)
                    {
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

        public ulong GetHash()
        {
            return mActorHash;
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable table = new();
            table.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>("AreaHash", mAreaHash), "AreaHash");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("Gyaml", mActorName), "Gyaml");
            table.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Hash", mActorHash), "Hash");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("Layer", mLayer), "Layer");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("Name", mName), "Name");

            BymlHashTable dynamicNode = new();

            foreach(KeyValuePair<string, object> dynParam in mActorParameters)
            {
                object param = mActorParameters[dynParam.Key];
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

            table.AddNode(BymlNodeId.Hash, dynamicNode, "Dynamic");

            BymlArrayNode rotateNode = new(3);
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>("X", mRotation.X));
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Y", mRotation.Y));
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Z", mRotation.Z));

            table.AddNode(BymlNodeId.Array, rotateNode, "Rotate");

            BymlArrayNode scaleNode = new(3);
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>("X", mScale.X));
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>("Y", mScale.Y));
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>("Z", mScale.Z));

            table.AddNode(BymlNodeId.Array, scaleNode, "Scale");

            BymlArrayNode translateNode = new(3);
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("X", mTranslation.X));
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Y", mTranslation.Y));
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>("Z", mTranslation.Z));

            table.AddNode(BymlNodeId.Array, translateNode, "Translate");

            return table;
        }

        public string  mActorName;
        public string mName;
        public string mLayer;
        public System.Numerics.Vector3 mTranslation;
        public System.Numerics.Vector3 mRotation;
        public System.Numerics.Vector3 mScale;
        public uint mAreaHash;
        public ulong mActorHash;
        public Dictionary<string, object> mActorParameters;
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

        public void AddActor(CourseActor actor)
        {
            mCourseActors.Add(actor);
        }
        
        public void DeleteActor(CourseActor actor)
        {
            mCourseActors.Remove(actor);
        }

        public CourseActor this[ulong hash]
        {
            get => GetActor(hash);
        }

        public List<CourseActor> GetActors()
        {
            return mCourseActors;
        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new((uint)mCourseActors.Count);

            foreach (CourseActor actor in mCourseActors)
            {
                node.AddNodeToArray(actor.BuildNode());
            }

            return node;
        }

        List<CourseActor> mCourseActors = new List<CourseActor>();
    }
}
