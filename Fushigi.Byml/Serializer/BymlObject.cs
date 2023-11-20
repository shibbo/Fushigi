using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Byml.Serializer
{
    public class BymlObject
    {
        public BymlHashTable HashTable;

        public void Load(BymlHashTable bymlHashTable) {
            this.HashTable = bymlHashTable;
            this.Deserialize();
        }

        public void Deserialize()
        {
            BymlSerialize.Deserialize(this, HashTable);
        }

        public BymlHashTable Serialize()
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
            return hashTable;
        }
    }
}
