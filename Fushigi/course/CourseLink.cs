using Fushigi.Byml;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public class CourseLink
    {
        public CourseLink(string linkName)
        {
            mSource = 0;
            mDest = 0;
            mLinkName = linkName;
        }

        public CourseLink(BymlHashTable table)
        {
            mSource = BymlUtil.GetNodeData<ulong>(table["Src"]);
            mDest = BymlUtil.GetNodeData<ulong>(table["Dst"]);
            mLinkName = BymlUtil.GetNodeData<string>(table["Name"]);
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable tbl = new();
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mDest), "Dst");
            tbl.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mLinkName), "Name");
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mSource), "Src");
            return tbl;
        }

        public ulong mSource;
        public ulong mDest;
        public string mLinkName;
    }

    public class CourseLinkHolder
    {
        public CourseLinkHolder()
        {

        }

        public CourseLinkHolder(BymlArrayNode linkArray)
        {
            foreach (BymlHashTable tbl in linkArray.Array)
            {
                mLinks.Add(new CourseLink(tbl));
            }
        }

        public bool HasLinksWithDest(ulong dest) => mLinks.Any(x=>x.mDest == dest);

        public IEnumerable<int> GetIndicesOfLinksWithDest_ForDelete(ulong hash)
        {
            for (int i = mLinks.Count - 1; i >= 0; i--)
            {
                if (mLinks[i].mDest == hash)
                    yield return i;
            }
        }
        public IEnumerable<int> GetIndicesOfLinksWithSrc_ForDelete(ulong hash)
        {
            for (int i = mLinks.Count - 1; i >= 0; i--)
            {
                if (mLinks[i].mSource == hash)
                    yield return i;
            }
        }

        public Dictionary<string, List<ulong>> GetDestHashesFromSrc(ulong hash)
        {
            Dictionary<string, List<ulong>> hashes = [];

            foreach (CourseLink link in mLinks)
            {
                if (link.mSource == hash)
                    hashes.GetOrCreate(link.mLinkName).Add(link.mDest);
            }

            return hashes;
        }

        public Dictionary<string, List<ulong>> GetSrcHashesFromDest(ulong hash)
        {
            Dictionary<string, List<ulong>> hashes = [];

            foreach (CourseLink link in mLinks)
            {
                if (link.mDest == hash)
                    hashes.GetOrCreate(link.mLinkName).Add(link.mSource);
            }

            return hashes;
        }

        public CourseLink GetLinkWithDestHash(ulong hash) {
            foreach (CourseLink link in mLinks)
            {
                if (link.mDest == hash)
                {
                    return link;
                }
            }

            return null;
        }

        public CourseLink GetLinkWithSrcHash(ulong hash)
        {
            foreach (CourseLink link in mLinks)
            {
                if (link.mSource == hash)
                {
                    return link;
                }
            }

            return null;
        }

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new();

            foreach(CourseLink link in mLinks)
            {
                node.AddNodeToArray(link.BuildNode());
            }

            return node;
        }

        public List<CourseLink> mLinks = new();
    }
}
