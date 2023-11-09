using Fushigi.Byml;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public class CourseGroup
    {
        public CourseGroup(BymlHashTable table)
        {
            mHash = BymlUtil.GetNodeData<ulong>(table["Hash"]);

            BymlArrayNode groups = table["Actors"] as BymlArrayNode;

            foreach (BymlBigDataNode<ulong> node in groups.Array)
            {
                mActorHashes.Add(node.Data);
            }
        }

        public ulong GetHash()
        {
            return mHash;
        }

        public bool IsActorValid(ulong hash)
        {
            return mActorHashes.Any(a => a == hash);
        }

        public bool TryGetIndexOfActor(ulong hash, out int index)
        {
            index = mActorHashes.FindIndex(a => a == hash);
            return index != -1;
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable tableNode = new();
            tableNode.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Hash", mHash), "Hash");

            BymlArrayNode actorsArray = new((uint)mActorHashes.Count);

            foreach (ulong actor in mActorHashes)
            {
                actorsArray.AddNodeToArray(BymlUtil.CreateNode<ulong>("", actor));
            }

            tableNode.AddNode(BymlNodeId.Array, actorsArray, "Actors");
            return tableNode;
        }

        public List<ulong> GetActors()
        {
            return mActorHashes;
        }

        ulong mHash;
        List<ulong> mActorHashes = new();
    }

    public class CourseGroupHolder
    {
        public CourseGroupHolder()
        {
        
        }

        public CourseGroupHolder(BymlArrayNode array)
        {
            foreach (BymlHashTable tbl in array.Array)
            {
                mGroups.Add(new CourseGroup(tbl));
            }

        }

        CourseGroup GetGroup(ulong hash)
        {
            foreach (CourseGroup grp in mGroups)
            {
                if (grp.GetHash() == hash)
                {
                    return grp;
                }
            }

            return null;
        }

        public CourseGroup this[ulong hash]
        {
            get
            {
                return GetGroup(hash);
            }
        }

        public IEnumerable<CourseGroup> GetGroupsContaining(ulong hash)
        {
            foreach (CourseGroup group in mGroups)
            {
                if (group.IsActorValid(hash))
                    yield return group;
            }
        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode arrayNode = new((uint)mGroups.Count);

            foreach(CourseGroup grp in mGroups)
            {
                arrayNode.AddNodeToArray(grp.BuildNode());
            }

            return arrayNode;
        }

        List<CourseGroup> mGroups = new();
    }
}
