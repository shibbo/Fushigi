using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Fushigi.course
{
    public class AreaParam : BymlObject
    {   
        public string EnvSetName { get; set; }
        public string BgmType { get; set; }
        public string WonderBgmType { get; set; }
        public float WonderBgmStartOffset { get; set; }
        public bool BgmInterlock { get; set; }
        public string EnvironmentSound { get; set; } 
        public string WonderEnvironmentSound { get; set; }
        public bool IsNotCallWaterEnvSE { get; set; }
        public string BackGroundAreaType { get; set; }
        public bool BgmInterlockOfWonder { get; set; }
        public string PlayerRhythmJumpTiming { get; set; }
        public string ExternalSoundAsset { get; set; }
        public string PlayerRhythmJumpBadgeTiming { get; set; }
        public string WonderEnvironmentSoundEfx { get; set; }
        public string DynamicResolutionQuality { get; set; }
        public bool IsInvisibleDeadLine { get; set; }
        public bool IsWaterArea { get; set; }
        public string WonderSEKeyForTag { get; set; }
        public bool IsNeedCallWaterInSE { get; set; }
        public bool IsVisibleOnlySameWonderPlayer { get; set; }
        public bool IsResetMarkerFlag { get; set; }
        public string WonderBgmEfx { get; set; }
        public string BgmString { get; set; }
        public bool UseMetalicPlayerSoundAsset { get; set; }
        public int RemotePlayerSEPriority { get; set; }
        public bool IsKoopaJr04Area { get; set; }
        public bool IsSetListenerCenter { get; set; }
        public string BadgeMedleyEquipBadgeId { get; set; }
        public string ExternalRhythmPatternSet { get; set; }

        public AreaSkinParam SkinParam { get; set; } = new AreaSkinParam();
        public AreaEnvPaletteSetting EnvPaletteSetting { get; set; } = new AreaEnvPaletteSetting();

        public AreaParam(Byml.Byml byml)
        {
            this.Load((BymlHashTable)byml.Root);
        }

        public bool ContainsParam(string param)
        {
            return ((BymlHashTable)this.HashTable).ContainsKey(param);
        }

        public object GetParam(BymlHashTable node, string paramName, string paramType)
        {
            switch (paramType)
            {
                case "String":
                    return ((BymlNode<string>)node[paramName]).Data;
                case "Bool":
                    return ((BymlNode<bool>)node[paramName]).Data;
                case "Float":
                    return ((BymlNode<float>)node[paramName]).Data;
            }

            return null;
        }

        public BymlHashTable GetRoot()
        {
            return (BymlHashTable)this.HashTable;
        }

        [Serializable]
        public class AreaEnvPaletteSetting
        {
            public string InitPaletteBaseName { get; set; }
            public List<string> WonderPaletteList { get; set; }
            public List<string> TransPaletteList { get; set; }
            public List<string> EventPaletteList { get; set; }
        }

        [Serializable]
        public class AreaSkinParam
        {
            public bool DisableBgUnitDecoA { get; set; }
            public string FieldA { get; set; } = "";
            public string FieldB { get; set; } = "";
            public string Object { get; set; } = "";
        }
    }
}
