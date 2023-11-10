using Fushigi.Byml;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.param
{
    public static class ChildActorParam
    {
        public static void Load()
        {
            byte[] packbytes = FileUtil.DecompressData(RomFS.GetFileBytes("Pack/Bootup.Nin_NX_NVN.pack.zs"));
            SARC.SARC packSarc = new SARC.SARC(new MemoryStream(packbytes));

            foreach (string file in packSarc.GetFiles("ChildActorParam"))
            {
                string actorName = Path.GetFileNameWithoutExtension(file).Split(".game")[0];
                var byml = new Byml.Byml(new MemoryStream(packSarc.OpenFile(file)));
                var root = byml.Root as BymlHashTable;

                if (root.ContainsKey("SelectTable"))
                {
                    var selTbl = root["SelectTable"] as BymlHashTable;
                    List<string> parameters = new();

                    foreach (var param in selTbl.Pairs)
                    {
                        parameters.Add(param.Name);
                    }

                    sChildParameters.Add(actorName, parameters);
                }
            }
        }

        public static List<string> GetActorParams(string actorName)
        {
            return sChildParameters[actorName];
        }

        public static bool ActorHasChildParam(string actorName)
        {
            return sChildParameters.ContainsKey(actorName);
        }

        static Dictionary<string, List<string>> sChildParameters = new();
    }
}
