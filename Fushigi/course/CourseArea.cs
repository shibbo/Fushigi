using Fushigi.Byml;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public class CourseArea
    {
        public CourseArea(string areaName, RomFS romFS) {
            mAreaName = areaName;
            mRomFS = romFS;
            Load();
        }

        public void Load()
        {
            mAreaParams = new AreaParam(
                new Byml.Byml(
                    new MemoryStream(
                        mRomFS.GetFileBytes($"Stage/AreaParam/{mAreaName}.game__stage__AreaParam.bgyml")
                    )
                )
            );
        }

        public string GetName()
        {
            return mAreaName;
        }

        string mAreaName;
        private RomFS mRomFS;
        public AreaParam mAreaParams;

        public class AreaParam
        {
            public AreaParam(Byml.Byml byml)
            {
                mByml = byml;
                var root = (BymlHashTable)byml.Root;
                
                if (root.ContainsKey("BgmType")) {
                    mBGMType = ((BymlNode<string>)root["BgmType"]).Data;
                }
                else
                {
                    mBGMType = "";
                }

                if (root.ContainsKey("EnvSetName"))
                {
                    mEnvSetName = ((BymlNode<string>)root["EnvSetName"]).Data;
                }
                else
                {
                    mEnvSetName = "";
                }

                if (root.ContainsKey("EnvironmentSound"))
                {
                    mEnviornmentSound = ((BymlNode<string>)root["EnvironmentSound"]).Data;
                }
                else
                {
                    mEnviornmentSound = "";
                }

                if (root.ContainsKey("EnvironmentSoundEfx"))
                {
                    mEnviornmentSoundEfx = ((BymlNode<string>)root["EnvironmentSoundEfx"]).Data;
                }
                else
                {
                    mEnviornmentSoundEfx = "";
                }

                if (root.ContainsKey("IsNotCallWaterEnvSE"))
                {
                    mIsNotCallWaterEnvSE = ((BymlNode<bool>)root["IsNotCallWaterEnvSE"]).Data;
                }
                else
                {
                    mIsNotCallWaterEnvSE = false;
                }

                if (root.ContainsKey("WonderBgmStartOffset"))
                {
                    mWonderBGMStartOffset = ((BymlNode<float>)root["WonderBgmStartOffset"]).Data;
                }
                else
                {
                    mWonderBGMStartOffset = 0.0f;
                }

                if (root.ContainsKey("WonderBgmType"))
                {
                    mWonderBGMType = ((BymlNode<string>)root["WonderBgmType"]).Data;
                }
                else
                {
                    mWonderBGMType = "";
                }

                mSkinParams = null;

                if (root.ContainsKey("SkinParam"))
                {
                    var skinParamNode = (BymlHashTable)root["SkinParam"];
                    mSkinParams = new SkinParam();

                    if (skinParamNode.ContainsKey("DisableBgUnitDecoA"))
                    {
                        mSkinParams.mDisableBgUnitDecoA = ((BymlNode<bool>)skinParamNode["DisableBgUnitDecoA"]).Data;
                    }
                    else
                    {
                        mSkinParams.mDisableBgUnitDecoA = false;
                    }

                    if (skinParamNode.ContainsKey("FieldA"))
                    {
                        mSkinParams.mFieldA = ((BymlNode<string>)skinParamNode["FieldA"]).Data;
                    }

                    if (skinParamNode.ContainsKey("FieldB"))
                    {
                        mSkinParams.mFieldB = ((BymlNode<string>)skinParamNode["FieldB"]).Data;
                    }

                    if (skinParamNode.ContainsKey("Object"))
                    {
                        mSkinParams.mObject = ((BymlNode<string>)skinParamNode["Object"]).Data;
                    }
                }
            }

            public bool ContainsParam(string param)
            {
                return ((BymlHashTable)mByml.Root).ContainsKey(param);
            }

            public bool ContainsSkinParam(string param)
            {
                return ((BymlHashTable)((BymlHashTable)mByml.Root)["SkinParam"]).ContainsKey(param);
            }

            public class SkinParam
            {
                public bool mDisableBgUnitDecoA;
                public string mFieldA;
                public string mFieldB;
                public string mObject;
            }

            Byml.Byml mByml;
            public string mBGMType;
            public string mEnvSetName;
            public string mEnviornmentSound;
            public string mEnviornmentSoundEfx;
            public bool mIsNotCallWaterEnvSE;
            public float mWonderBGMStartOffset;
            public string mWonderBGMType;
            public SkinParam mSkinParams;
        }
    }
}
