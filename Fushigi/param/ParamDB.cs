using Fushigi.util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Fushigi.SARC;
using Silk.NET.OpenGL;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Fushigi.Byml;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using System.Diagnostics.CodeAnalysis;

namespace Fushigi.param
{
    public static class ParamDB
    {
        public struct Component
        {
            public string Parent;
            public Dictionary<string, ComponentParam> Parameters;
        }

        public struct ComponentParam
        {
            public string Type;
            public object InitValue;
        }

        struct ParamList
        {
            public List<string> Components;
        }

        public static void Init()
        {
            if (File.Exists("actors.json") && File.Exists("components.json") && File.Exists("rails.json") && File.Exists("railParams.json"))
            {
                sActors = JsonConvert.DeserializeObject<Dictionary<string, ParamList>>(File.ReadAllText("actors.json"));
                sComponents = JsonConvert.DeserializeObject<Dictionary<string, Component>>(File.ReadAllText("components.json"));
                sRails = JsonConvert.DeserializeObject<Dictionary<string, Component>>(File.ReadAllText("rails.json"));
                sRailParamList = JsonConvert.DeserializeObject<Dictionary<string, ParamList>>(File.ReadAllText("railParams.json"));
                sIsInit = true;
            }
        }

        public static bool HasActorComponents(string actor) => sActors.ContainsKey(actor);

        public static List<string> GetActorComponents(string actor) => sActors[actor].Components;

        public static Dictionary<string, ComponentParam> GetComponentParams(string componentName) => sComponents[componentName].Parameters;
        public static string GetRailComponent(string railName) => sRailParamList[railName].Components[0];
        public static bool TryGetRailPointComponent(string railName, [NotNullWhen(true)] out string? componentName)
        {
            componentName = sRailParamList[railName].Components[1];

            if(componentName=="null")
                componentName = null;

            return componentName is not null;
        }

        public static Dictionary<string, ComponentParam> GetRailComponentParams(string componentName) => sRails[componentName].Parameters;

        public static string[] GetActors() => sActors.Keys.ToArray();

        public static void Load()
        {
            /* if we have already been initialized, we skip this process */
            if (sIsInit)
            {
                return;
            }

            /* the files in /Pack/Actor in the RomFS contain the PACK files that contain our parameters */
            // string[] files = RomFS.GetFiles("/Pack/Actor");
            string[] files = RomFS.GetFiles(Path.Combine("Pack", "Actor"));

            /* iterate through each file */
            foreach (string file in files)
            {
                /* the actor name in question is at the beginning of the file name */
                string actorName = Path.GetFileNameWithoutExtension(file).Split(".pack")[0];
                ParamList param = new ParamList();
                param.Components = new List<string>();

                /* each .pack file should be ZSTD compressed, if not, skip the file */
                byte[] fileBytes;
                try {
                    fileBytes = FileUtil.DecompressFile(file);
                }
                catch (Exception) {
                    continue;
                }

                SARC.SARC sarc = new SARC.SARC(new MemoryStream(fileBytes));

                /* /Component/Blackboard/BlackboardParamTable is where all of the actor-specific parameters live */
                string actorParamDir = "Component/Blackboard/BlackboardParamTable";

                if (!sarc.DirectoryExists(actorParamDir))
                {
                    sActors.Add(actorName, param);
                    continue;
                }

                /* grab every file in this directory, should be all BYMLs */
                string[] filesInDir = sarc.GetFiles(actorParamDir);

                foreach(string paramFile in filesInDir)
                {
                    /* grab our parameter file name, while wiping away the rest of the junk we don't care about */
                    string paramFileName = Path.GetFileNameWithoutExtension(paramFile).Split(".engine")[0];
                    /* actor parameters only need to store the paramter file name */
                    param.Components.Add(paramFileName);

                    Byml.Byml byml = new Byml.Byml(new MemoryStream(sarc.OpenFile(paramFile)));

                    // do we already have this component loaded?
                    // if so, we skip this so we don't load the same parameters a million times
                    if (sComponents.ContainsKey(paramFileName))
                    {
                        continue;
                    }

                    Component component = ReadByml(byml);
                    sComponents.Add(paramFileName, component);
                }

                sActors.Add(actorName, param);
            }

            /*  now let's read our rail parameter files */
            string[] railComponentFiles = RomFS.GetFiles(Path.Combine("Component", "Blackboard", "BlackboardParamTable"));

            foreach (string railComp in railComponentFiles)
            {
                var byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(railComp)));
                string name = Path.GetFileNameWithoutExtension(railComp).Split(".engine")[0];
                Component component = ReadByml(byml);
                sRails.Add(name, component);
            }

            /* read our rail parameter sheets that tell us what to use for rail params and rail point params */
            string[] railParamFiles = RomFS.GetFiles(Path.Combine("Gyml", "Rail", "RailParam"));

            foreach(string railParamFile in railParamFiles)
            {
                var byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(railParamFile)));
                string name = Path.GetFileNameWithoutExtension(railParamFile).Split(".game")[0];
                BymlHashTable? root = byml.Root as BymlHashTable;

                ParamList list = new();

                list.Components = new List<string>();

                string railParam = Path.GetFileNameWithoutExtension(BymlUtil.GetNodeData<string>(root["RailParam"])).Split(".engine")[0];
                string railPointParam = "null";

                if (root.ContainsKey("RailPointParam"))
                {
                    railPointParam = Path.GetFileNameWithoutExtension(BymlUtil.GetNodeData<string>(root["RailPointParam"])).Split(".engine")[0];
                }

                list.Components.Add(railParam);
                list.Components.Add(railPointParam);

                sRailParamList.Add(name, list);
            }

            /* write our JSON for our actors */
            List<string> jsonOutput = new List<string>();
            jsonOutput.Add(JsonConvert.SerializeObject(sActors, Formatting.Indented));
            File.WriteAllLines("actors.json", jsonOutput.ToArray());
            /* write our JSON for our components */
            List<string> compOutput = new List<string>();
            compOutput.Add(JsonConvert.SerializeObject(sComponents, Formatting.Indented));
            File.WriteAllLines("components.json", compOutput.ToArray());
            /* write our JSON for our rails */
            List<string> railsOutput = new List<string>();
            railsOutput.Add(JsonConvert.SerializeObject(sRails, Formatting.Indented));
            File.WriteAllLines("rails.json", railsOutput.ToArray());
            /* write our JSON for our rail parameters */
            List<string> railParamOutput = new List<string>();
            railParamOutput.Add(JsonConvert.SerializeObject(sRailParamList, Formatting.Indented));
            File.WriteAllLines("railParams.json", railParamOutput.ToArray());
            /* we are all now initialized and ready to go! */
            sIsInit = true;
        }

        public static void Reload()
        {
            sActors.Clear();
            sComponents.Clear();
            sRails.Clear();
            sRailParamList.Clear();
            Load();
        }

        static Component ReadByml(Byml.Byml byml)
        {
            var root = (BymlHashTable)byml.Root;

            string type = "";

            Component component = new Component();
            /* by default, parameters do not have a parent */
            component.Parent = "null";
            component.Parameters = new Dictionary<string, ComponentParam>();

            /* this is where things can become a little complex
                * after the root node there are dictionaries that contain arrays of parameters that are a specific type (or a node that defines a parent)
                * here we loop through each of these nodes to sort out which type we are dealing with here */
            foreach (BymlHashPair pair in root.Pairs)
            {
                switch (pair.Name)
                {
                    case "BlackboardParamU8Array":
                        type = "U8";
                        break;
                    case "BlackboardParamS16Array":
                        type = "S16";
                        break;
                    case "BlackboardParamU32Array":
                        type = "U32";
                        break;
                    case "BlackboardParamS32Array":
                        type = "S32";
                        break;
                    case "BlackboardParamF32Array":
                        type = "F32";
                        break;
                    case "BlackboardParamF64Array":
                        type = "F64";
                        break;
                    case "BlackboardParamBoolArray":
                        type = "Bool";
                        break;
                    case "BlackboardParamStringArray":
                        type = "String";
                        break;
                    default:
                        break;
                }

                /* if our current node is a BymlNode<string> and *not* a BymlArrayNode, that means we have hit a parent parameter */
                if (pair.Value is BymlNode<string>)
                {
                    component.Parent = Path.GetFileName(((BymlNode<string>)pair.Value).Data.Split(".engine")[0]);
                }
                else
                {
                    /* we are currently dealing with an array node of parameters */
                    BymlArrayNode tbl = (BymlArrayNode)pair.Value;

                    /* there are some files that contain array lengths of 0, despite the node still being written to the file */
                    if (tbl.Length <= 0)
                    {
                        continue;
                    }

                    /* iterate through each node, these should be a BymlHashTable node */
                    foreach (IBymlNode node in tbl.Array)
                    {
                        /* this check should not fail */
                        if (node is BymlHashTable)
                        {
                            BymlHashTable ht = node as BymlHashTable;

                            /* if the IsInstanceParam value is False, it means that the parameter is not used in a course context
                                * so, if it is False, we ignore it and move on to the next parameter as we will only read what matters
                                */
                            bool isInstParam = ((BymlNode<bool>)(ht["IsInstanceParam"])).Data;

                            if (!isInstParam)
                            {
                                continue;
                            }

                            ComponentParam comp = new ComponentParam();
                            string key = ((BymlNode<string>)(ht["BBKey"])).Data;
                            comp.Type = type;

                            /* we look through the type that we set earlier to assign the proper initial value */
                            switch (type)
                            {
                                case "U8":
                                case "U32":
                                    comp.InitValue = ((BymlNode<uint>)ht["InitVal"]).Data;
                                    break;
                                /* S16 and S32 are both internally still a Int32 node, but S16 has bounds checks */
                                case "S16":
                                case "S32":
                                    comp.InitValue = ((BymlNode<int>)ht["InitVal"]).Data;
                                    break;
                                case "F32":
                                    comp.InitValue = ((BymlNode<float>)ht["InitVal"]).Data;
                                    break;
                                case "F64":
                                    comp.InitValue = ((BymlNode<double>)ht["InitVal"]).Data;
                                    break;
                                case "Bool":
                                    comp.InitValue = ((BymlNode<bool>)ht["InitVal"]).Data;
                                    break;
                                case "String":
                                    comp.InitValue = ((BymlNode<string>)ht["InitValConverted"]).Data;
                                    break;
                            }

                            /* and now we add our parameter to the component we are dealing with */
                            component.Parameters.Add(key, comp);
                        }
                        else
                        {
                            throw new Exception("There is a BYML node in the parameters that isn't a BymlHashTable...");
                        }
                    }
                }
            }

            return component;
        }

        static Dictionary<string, ParamList>? sActors = new Dictionary<string, ParamList>();
        static Dictionary<string, Component>? sComponents = new Dictionary<string, Component>();
        static Dictionary<string, Component>? sRails = new Dictionary<string, Component>();
        static Dictionary<string, ParamList>? sRailParamList = new Dictionary<string, ParamList>();
        public static bool sIsInit = false;
    }
}
