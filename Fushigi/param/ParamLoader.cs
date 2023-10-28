using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Fushigi.param
{
    public class ParamLoader
    {
        public static void Load()
        {
            mParams = new Dictionary<string, ParamHolder>();
            var nodes = JsonNode.Parse(File.ReadAllText("res/AreaParam.json")).AsObject();

            ParamHolder areaParms = new ParamHolder();
            /*areaParms.Add("BgmType", (string)nodes["BgmType"]);
            areaParms.Add("EnvSetName", (string)nodes["EnvSetName"]);
            areaParms.Add("EnvironmentSound", (string)nodes["EnvironmentSound"]);
            areaParms.Add("EnvironmentSoundEfx", (string)nodes["EnvironmentSoundEfx"]);
            areaParms.Add("WonderBgmType", (string)nodes["WonderBgmType"]);
            areaParms.Add("BackGroundAreaType", (string)nodes["BackGroundAreaType"]);
            areaParms.Add("PlayerRhythmJumpTiming", (string)nodes["PlayerRhythmJumpTiming"]);
            areaParms.Add("WonderEnvironmentSound", (string)nodes["WonderEnvironmentSound"]);
            areaParms.Add("ExternalSoundAsset", (string)nodes["ExternalSoundAsset"]);
            areaParms.Add("PlayerRhythmJumpBadgeTiming", (string)nodes["PlayerRhythmJumpBadgeTiming"]);
            areaParms.Add("WonderEnvironmentSoundEfx", (string)nodes["WonderEnvironmentSoundEfx"]);
            areaParms.Add("DynamicResolutionQuality", (string)nodes["DynamicResolutionQuality"]);
            areaParms.Add("WonderSEKeyForTag", (string)nodes["WonderSEKeyForTag"]);
            areaParms.Add("WonderBgmEfx", (string)nodes["WonderBgmEfx"]);
            areaParms.Add("BgmString", (string)nodes["BgmString"]);
            areaParms.Add("BadgeMedleyEquipBadgeId", (string)nodes["BadgeMedleyEquipBadgeId"]);
            areaParms.Add("IsNotCallWaterEnvSE", (string)nodes["IsNotCallWaterEnvSE"]);
            areaParms.Add("BgmInterlock", (string)nodes["BgmInterlock"]);
            areaParms.Add("BgmInterlockOfWonder", (string)nodes["BgmInterlockOfWonder"]);
            areaParms.Add("IsInvisibleDeadLine", (string)nodes["IsInvisibleDeadLine"]);
            areaParms.Add("IsWaterArea", (string)nodes["IsWaterArea"]);
            areaParms.Add("IsNeedCallWaterInSE", (string)nodes["IsNeedCallWaterInSE"]);
            areaParms.Add("IsVisibleOnlySameWonderPlayer", (string)nodes["IsVisibleOnlySameWonderPlayer"]);
            areaParms.Add("IsResetMarkerFlag", (string)nodes["IsResetMarkerFlag"]);
            areaParms.Add("IsNotCallWaterEnvSE", (string)nodes["IsNotCallWaterEnvSE"]);
            areaParms.Add("IsNotCallWaterEnvSE", (string)nodes["IsNotCallWaterEnvSE"]);
            areaParms.Add("IsNotCallWaterEnvSE", (string)nodes["IsNotCallWaterEnvSE"]);*/
            mParams.Add("AreaParam", areaParms);
        }

        public static ParamHolder GetHolder(string name)
        {
            return mParams[name];
        }

        static Dictionary<string, ParamHolder> mParams;
    }

    public class ParamHolder : Dictionary<string, string>
    {

    }

    
}
