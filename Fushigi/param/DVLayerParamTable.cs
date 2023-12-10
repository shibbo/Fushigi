using Fushigi.Byml;
using Fushigi.util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.param
{
    public class DVLayerParamTable
    {
        public Dictionary<string, Vector2> Layers = new Dictionary<string, Vector2>();

        public void LoadDefault()
        {
           /* SARC.SARC packSarc = RomFS.GetOrLoadBootUpPack();
            var file = packSarc.OpenFile("Layer/DVLayerParamTable/Default.game__actor__DVLayerParamTable.bgyml");
            Load(new MemoryStream(file));*/
        }

        public void Load(string name)
        {
            var file = FileUtil.FindContentPath(Path.Combine("Layer", "DVLayerParamTable", $"{name}game__actor__DVLayerParamTable.bgyml"));
            if (File.Exists(file))
                Load(new MemoryStream(File.ReadAllBytes(file)));
        }

        public void Load(MemoryStream stream)
        {
            var root = (BymlHashTable)new Byml.Byml(stream).Root;
            var list = (BymlHashTable)root["LayerParams"];

            Layers.Clear();
            foreach (var layer in list.Pairs)
            {
                var v = (BymlHashTable)layer.Value;

                Layers.Add(layer.Name, new Vector2(
                    BymlUtil.GetNodeData<float>(v["X"]),
                    BymlUtil.GetNodeData<float>(v["Y"])));
            }
        }
    }
}
