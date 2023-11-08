using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Fushigi.Bfres.Common
{
    public abstract class ResDict : Dictionary<string, IResData>
    {

    }

    public class ResDict<T> : ResDict, IResData where T : IResData, new()
    {
        public ResDict() { }

        public string GetKey(int index)
        {
            if (index >= 0 && index < Keys.Count)
                return Keys.ElementAt(index);

            return null;
        }

        public void Read(BinaryReader reader)
        {
            reader.ReadUInt32(); //magic
            int numNodes = reader.ReadInt32();

            List<Node> nodes = new List<Node>();
            for (int i = 0; i < numNodes; i++) 
            {
                nodes.Add(new Node()
                {
                    Reference = reader.ReadUInt32(),
                    IdxLeft = reader.ReadUInt16(),
                    IdxRight = reader.ReadUInt16(),
                    Key = reader.ReadStringOffset(reader.ReadUInt64()),
                });
            }

            for (int i = 1; i < nodes.Count; i++)
                this.Add(nodes[i].Key, null);
        }

        protected class Node
        {
            internal uint Reference;
            internal ushort IdxLeft;
            internal ushort IdxRight;
            internal string Key;
            internal IResData Value;
        }
    }
}
