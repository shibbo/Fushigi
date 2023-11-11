using Fushigi.Byml.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.actor_pack.components
{
    [Serializable]
    public class ModelInfo : IComponent
    {
        [BymlProperty("FmdbName", DefaultValue = "")]
        public string mModelName { get; set; }

        [BymlProperty("ModelProjectName", DefaultValue = "")]
        public string mFilePath { get; set; }

        public string SearchModelKey { get; set; }

        public List<SubModel> SubModels { get; set; }

        [Serializable]
        public class SubModel
        {
            public string FmdbName { get; set; }
        }

        [Serializable]
        public class MaterialAnimations
        {
            [BymlProperty("$type")]
            public string Type { get; set; }
            public string BBKey { get; set; } = "ColorIdx";
            [BymlProperty("FilePath")]
            public string Fmab { get; set; }
        }
    }
}
