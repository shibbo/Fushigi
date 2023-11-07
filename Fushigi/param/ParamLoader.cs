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
            var nodes = JsonNode.Parse(
                File.ReadAllText(
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        Path.Combine("res", "AreaParam.json")
                    )
                )
            ).AsObject();
            ParamHolder areaParms = new ParamHolder();

            foreach (KeyValuePair<string, JsonNode> obj in nodes)
            {
                // todo -- support other things
                if (obj.Value is JsonValue)
                {
                    areaParms.Add(obj.Key, (string)obj.Value);
                }
                
            }

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
