using Fushigi.Bfres;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class GsysRenderParameters
    {
        //Casts a shadow
        public bool CastDynamicShadow;
        public bool CastDynamicShadowOnly; //Only shows shadow, no rendering

        //Toggle for displaying in an agl cubemap render
        public bool DisplayInCubeMap;
        public bool DisplayInCubeMapOnly; //Only shows in cubemap, no rendering

        //Render priority
        public string PriorityHint = "";
        public int Priority = 0;

        //Lightmap textures to use from a list of agl light maps
        public string LightMapDiffuse = "";
        public string LightMapSpecular = "";

        //Env obj to use for fog and other effects
        public string EnvObjSet = "";

        public void Init(Material material)
        {
            EnvObjSet = material.GetRenderInfoString("gsys_env_obj_set");
            LightMapDiffuse = material.GetRenderInfoString("gsys_light_diffuse");
            LightMapSpecular = material.GetRenderInfoString("gsys_light_specular");
            PriorityHint = material.GetRenderInfoString("gsys_priority_hint");
            Priority = material.GetRenderInfoInt("gsys_priority");

            CastDynamicShadow = material.GetRenderInfoString("gsys_dynamic_depth_shadow") == "1";
            CastDynamicShadowOnly = material.GetRenderInfoString("gsys_dynamic_depth_shadow_only") == "1";

            DisplayInCubeMap = material.GetRenderInfoString("gsys_cube_map") == "1";
            DisplayInCubeMapOnly = material.GetRenderInfoString("gsys_cube_map_only") == "1";
        }
    }
}
