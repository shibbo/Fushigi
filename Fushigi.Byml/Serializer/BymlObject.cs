using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Byml.Serializer
{
    internal class BymlObject
    {
        public BymlHashTable HashTable;

        public BymlObject(BymlHashTable bymlHashTable) {
            this.HashTable = bymlHashTable;
        }

        public void Deserialize()
        {
            BymlSerialize.Deserialize(this, HashTable);
        }

        public void Serialize()
        {
            var hashTable = BymlSerialize.Serialize(this);
            //Merge hash tables. Keep original params intact
            foreach (var pair in hashTable.Pairs)
            {
                //Update or add any hash table params
                if (HashTable.ContainsKey(pair.Name))
                    HashTable.SetNode(pair.Name, pair.Value);
                else
                    HashTable.AddNode(pair.Id, pair.Value, pair.Name);
            }
        }
    }
}
