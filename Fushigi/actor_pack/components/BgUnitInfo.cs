using Fushigi.Byml.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fushigi.actor_pack.components
{
    [Serializable]
    public class BgUnitInfo
    {
        public (string bfresName, string modelName) GetModelInfo()
        {
            var m = Regex.Match(Fmdb, "Work/Model/Unit/(.*)/output/(.*).fmdb");

            return (m.Groups[1].Value, m.Groups[2].Value);
        }

        public string Fmdb { get; set; }

        public string DecoInfo { get; set; }

        public List<AttrListEntry> AttrList { get; set; }

        public Dictionary<string, string> GetMaterialNames() => AttrList.ToDictionary(x => x.DrawAttr, x => x.MaterialName);

        [Serializable]
        public class AttrListEntry
        {
            public string DrawAttr { get; set; }
            public string MaterialName { get; set; }
        }

        public List<string> MaterialPresetList { get; set; }

        public bool UseLongParts { get; set; }
    }
}
