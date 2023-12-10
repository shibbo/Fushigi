using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fushigi.param
{
    [Serializable]
    public class DefaultBgUnitSkinConfigTable
    {
        public string GetPackName(string skinName, string modelType)
        {
            var actorPath = CellList[$"{skinName}___{modelType}"].Path;

            var m = Regex.Match(actorPath, "Work/Actor/(.*).engine__actor__ActorParam.gyml");

            return m.Groups[1].Value;
        }

        public Dictionary<string, Cell> CellList { get; set; }

        [Serializable]
        public class Cell
        {
            public string Path { get; set; }
        }
    }
}
