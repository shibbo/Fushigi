using Fushigi.Byml;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
                mActors.Add(node.Data);
            }
        }

        public bool ContainsActor(ulong hash)
        {
            return mActors.Any(a => a == hash);
        }

        public bool TryGetIndexOfActor(ulong hash, out int index)
        {
            index = mActors.FindIndex(a => a == hash);
            return index != -1;
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable tableNode = new();
            tableNode.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mHash), "Hash");

            BymlArrayNode actorsArray = new((uint)mActors.Count);

            foreach (ulong actor in mActors)
            {
                actorsArray.AddNodeToArray(BymlUtil.CreateNode<ulong>(actor));
            }

            tableNode.AddNode(BymlNodeId.Array, actorsArray, "Actors");
            return tableNode;
        }

        public ulong mHash;
        public List<ulong> mActors = new();
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

        public bool TryGetGroup(ulong hash, [NotNullWhen(true)] out CourseGroup? rail)
        {
            rail = mGroups.Find(x => x.mHash == hash);
            return rail is not null;
        }

        public CourseGroup this[ulong hash]
        {
            get
            {
                bool exists = TryGetGroup(hash, out CourseGroup? group);
                Debug.Assert(exists);
                return group!;
            }
        }

        public IEnumerable<CourseGroup> GetGroupsContaining(ulong hash)
        {
            foreach (CourseGroup group in mGroups)
            {
                if (group.ContainsActor(hash))
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

        public List<CourseGroup> mGroups = new();
    }
}
