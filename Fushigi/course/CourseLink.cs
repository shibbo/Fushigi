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
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Dst", mDest), "Dst");
            tbl.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>("Name", mLinkName), "Name");
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>("Src", mSource), "Src");
            return tbl;
        }

        public ulong GetSrcHash()
        {
            return mSource;
        }

        public ulong GetDestHash()
        {
            return mDest;
        }

        public string GetLinkName()
        {
            return mLinkName;
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

        public IEnumerable<int> GetIndicesOfLinksWithDest_ForDelete(ulong hash)
        {
            for (int i = mLinks.Count - 1; i >= 0; i--)
            {
                if (mLinks[i].GetDestHash() == hash)
                    yield return i;
            }
        }
        public IEnumerable<int> GetIndicesOfLinksWithSrc_ForDelete(ulong hash)
        {
            for (int i = mLinks.Count - 1; i >= 0; i--)
            {
                if (mLinks[i].GetSrcHash() == hash)
                    yield return i;
            }
        }

        public bool TryGetIndexOfLink(string name, ulong src, ulong dest, out int index)
        {
            index = mLinks.FindIndex(x => x.GetLinkName() == name &&
                x.GetSrcHash() == src && x.GetDestHash() == dest);

            return index != -1;
        }

        public Dictionary<string, List<ulong>> GetDestHashesFromSrc(ulong hash)
        {
            Dictionary<string, List<ulong>> hashes = [];

            foreach (CourseLink link in mLinks)
            {
                if (link.GetSrcHash() == hash)
                    hashes.GetOrCreate(link.GetLinkName()).Add(link.GetDestHash());
            }

            return hashes;
        }

        public Dictionary<string, List<ulong>> GetSrcHashesFromDest(ulong hash)
        {
            Dictionary<string, List<ulong>> hashes = [];

            foreach (CourseLink link in mLinks)
            {
                if (link.GetDestHash() == hash)
                    hashes.GetOrCreate(link.GetLinkName()).Add(link.GetSrcHash());
            }

            return hashes;
        }

        public CourseLink GetLinkWithDestHash(ulong hash) {
            foreach (CourseLink link in mLinks)
            {
                if (link.GetDestHash() == hash)
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
                if (link.GetSrcHash() == hash)
                {
                    return link;
                }
            }

            return null;
        }

        public List<CourseLink> GetLinks()
        {
            return mLinks;
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

        List<CourseLink> mLinks = new();
    }
}
