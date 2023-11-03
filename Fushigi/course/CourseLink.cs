using Fushigi.Byml;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public class CourseLink
    {
        public CourseLink(BymlHashTable table, CourseActorHolder actorHolder)
        {
            mSource = actorHolder[BymlUtil.GetNodeData<ulong>(table["Src"])];
            mDest = actorHolder[BymlUtil.GetNodeData<ulong>(table["Dst"])];
            mLinkName = BymlUtil.GetNodeData<string>(table["Name"]);
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable tbl = new();
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Dst", mDest.GetHash()), "Dst");
            tbl.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("Name", mLinkName), "Name");
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Src", mSource.GetHash()), "Src");
            return tbl;
        }

        CourseActor? mSource;
        CourseActor? mDest;
        string mLinkName;
    }

    public class CourseLinkHolder
    {
        public CourseLinkHolder(BymlArrayNode linkArray, CourseActorHolder actorHolder)
        {
            foreach (BymlHashTable tbl in linkArray.Array)
            {
                mLinks.Add(new CourseLink(tbl, actorHolder));
            }
        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new((uint)mLinks.Count);

            foreach(CourseLink link in mLinks)
            {
                node.AddNodeToArray(link.BuildNode());
            }

            return node;
        }

        List<CourseLink> mLinks = new();
    }
}
