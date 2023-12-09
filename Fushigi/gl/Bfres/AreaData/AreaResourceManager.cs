using Fushigi.env;
using Fushigi.gl.Textures;
using Fushigi.util;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres.AreaData
{
    public class AreaResourceManager
    {
        //Skybox
        public VRSkybox VRSkybox;

        //Environment sets to display env data between models.
        public Dictionary<string, EnvironmentBlockExtended> EnvironmentSets = new Dictionary<string, EnvironmentBlockExtended>();

        public Dictionary<string, AglLightmap> Lightmaps = new Dictionary<string, AglLightmap>();

        public static AreaResourceManager ActiveArea = null;

        private EnvPalette EnvPalette;

        public AreaResourceManager(GL gl, EnvPalette envPalette)
        {
            VRSkybox = new VRSkybox(gl);

            ActiveArea = this;

            //env set list
            EnvironmentSets.Add("PlayerEnemy_base", new EnvironmentBlockExtended());
            EnvironmentSets.Add("Black_base", new EnvironmentBlockExtended());
            EnvironmentSets.Add("Object_Base", new EnvironmentBlockExtended());
            EnvironmentSets.Add("Dv_base", new EnvironmentBlockExtended());

            EnvironmentSets.Add("Cloud_base", new EnvironmentBlockExtended());
            EnvironmentSets.Add("FarPlane_base", new EnvironmentBlockExtended());
            EnvironmentSets.Add("FarPlane_FogCloud", new EnvironmentBlockExtended());
            EnvironmentSets.Add("Field_base", new EnvironmentBlockExtended());

            Lightmaps.Add("PlayerDifLightMap", new AglLightmap(gl));
            Lightmaps.Add("PlayerSpcLightMap", new AglLightmap(gl));

            Lightmaps.Add("EnemyDifLightMap", new AglLightmap(gl));
            Lightmaps.Add("EnemySpcLightMap", new AglLightmap(gl));

            Lightmaps.Add("ObjectDifLightMap", new AglLightmap(gl));
            Lightmaps.Add("ObjectSpcLightMap", new AglLightmap(gl));

            Lightmaps.Add("DvDifLightMap", new AglLightmap(gl));
            Lightmaps.Add("DvSpcLightMap", new AglLightmap(gl));

            Lightmaps.Add("FieldDifLightMap", new AglLightmap(gl));
            Lightmaps.Add("FieldSpcLightMap", new AglLightmap(gl));

            Lightmaps.Add("CloudDifLightMap", new AglLightmap(gl));
            Lightmaps.Add("CloudSpcLightMap", new AglLightmap(gl));

            Lightmaps.Add("FarPlaneLightMap", new AglLightmap(gl));

            Lightmaps.Add("AshibaDifLightMap", new AglLightmap(gl));
            Lightmaps.Add("AshibaSpcLightMap", new AglLightmap(gl));

            ReloadPalette(gl, envPalette);
        }

        private float ratio = 0;
        private bool isEnvPaletteTransition = false;
        private EnvPalette prevPalette;
        private EnvPalette nextPalette;

        public void TransitionEnvPalette(string current, string next)
        {
            prevPalette = new EnvPalette(current);
            nextPalette = new EnvPalette(next);

            ratio = 0;
            isEnvPaletteTransition = true;
        }

        public void UpdatePalette(GL gl)
        {
            if (isEnvPaletteTransition)
            {
                if (ratio >= 1.0f)
                {
                    isEnvPaletteTransition = false;
                    EnvPalette.Name = nextPalette.Name;

                    EnvPalette.Load(nextPalette.Name);
                    ReloadPalette(gl, EnvPalette);
                }
                else
                {
                    EnvPalette.Lerp(prevPalette, nextPalette, ratio);
                    ReloadPalette(gl, EnvPalette);

                    ratio += 0.1f;
                }
            }
        }

        public void ReloadPalette(GL gl, EnvPalette envPalette)
        {
            EnvPalette = envPalette;

            if (isEnvPaletteTransition)
                VRSkybox.SetPaletteLerp(prevPalette, nextPalette, ratio);
            else
                VRSkybox.SetPalette(envPalette);

            EnvironmentSets["PlayerEnemy_base"].Setup(envPalette, EnvironmentBlockExtended.Kind.Char);
            EnvironmentSets["Black_base"].Setup(envPalette, EnvironmentBlockExtended.Kind.Black);
            EnvironmentSets["Object_Base"].Setup(envPalette, EnvironmentBlockExtended.Kind.Obj);
            EnvironmentSets["Dv_base"].Setup(envPalette, EnvironmentBlockExtended.Kind.Dv);

            EnvironmentSets["Cloud_base"].Setup(envPalette, EnvironmentBlockExtended.Kind.Cloud);
            EnvironmentSets["FarPlane_base"].Setup(envPalette, EnvironmentBlockExtended.Kind.FarPlane);
            EnvironmentSets["FarPlane_FogCloud"].Setup(envPalette, EnvironmentBlockExtended.Kind.FarPlaneFogCloud);
            EnvironmentSets["Field_base"].Setup(envPalette, EnvironmentBlockExtended.Kind.Field);

            LoadLightmap(gl, "PlayerDifLightMap", envPalette.CharLight, false);
            LoadLightmap(gl, "PlayerSpcLightMap", envPalette.CharLight, true);

            LoadLightmap(gl, "EnemyDifLightMap", envPalette.CharLight, false);
            LoadLightmap(gl, "EnemySpcLightMap", envPalette.CharLight, true);

            LoadLightmap(gl, "ObjectDifLightMap", envPalette.ObjLight, false);
            LoadLightmap(gl, "ObjectSpcLightMap", envPalette.ObjLight, true);

            LoadLightmap(gl, "DvDifLightMap", envPalette.DvLight, false);
            LoadLightmap(gl, "DvSpcLightMap", envPalette.DvLight, true);

            LoadLightmap(gl, "FieldDifLightMap", envPalette.FieldLight, false);
            LoadLightmap(gl, "FieldSpcLightMap", envPalette.FieldLight, true);

            LoadLightmap(gl, "CloudDifLightMap", envPalette.CloudLight, false);
            LoadLightmap(gl, "CloudSpcLightMap", envPalette.CloudLight, true);

            LoadLightmap(gl, "FarPlaneLightMap", envPalette.FieldLight, false);

            LoadLightmap(gl, "AshibaDifLightMap", envPalette.FieldLight, true);
            LoadLightmap(gl, "AshibaSpcLightMap", envPalette.FieldLight, true);

            VRSkybox.RenderToTexture(gl);
        }

        public EnvironmentBlockExtended GetEnvironmentSet(GsysRenderParameters parameters)
        {
            if (this.EnvironmentSets.ContainsKey(parameters.EnvObjSet))
                return this.EnvironmentSets[parameters.EnvObjSet];

            return EnvironmentSets.Values.FirstOrDefault();
        }

        public AglLightmap GetDiffuseLightmap(GsysRenderParameters parameters)
        {
            if (this.Lightmaps.ContainsKey(parameters.LightMapDiffuse))
                return this.Lightmaps[parameters.LightMapDiffuse];

            return Lightmaps.Values.FirstOrDefault();
        }

        public AglLightmap GetSpecularLightmap(GsysRenderParameters parameters)
        {
            if (this.Lightmaps.ContainsKey(parameters.LightMapSpecular))
                return this.Lightmaps[parameters.LightMapSpecular];

            return Lightmaps.Values.FirstOrDefault();
        }

        public void UpdateBackground(GL gl, Camera camera)
        {
            UpdatePalette(gl);
            GsysShaderRender.GsysResources.UserTexture1 = VRSkybox.SkyTexture;
        }

        public void RenderSky(GL gl, Camera camera)
        {
            VRSkybox.Render(gl, camera);
        }

        public void Update(GL gl)
        {
            VRSkybox.SetPalette(this.EnvPalette);

            VRSkybox.RenderToTexture(gl);
            foreach (var lmap in Lightmaps)
                lmap.Value.Render(gl);
        }

        private void LoadLightmap(GL gl, string name, EnvPalette.EnvLightList lights, bool isSpecular)
        {
            AglLightmap lmap = Lightmaps[name];

            //Set data
            if (isSpecular)
                FromEnvPaletteSpecular(lmap, lights);
            else
                FromEnvPaletteDiffuse(lmap, lights);

            //Render it
            lmap.Render(gl);
        }

        public void FromEnvPaletteDiffuse(AglLightmap lmap, EnvPalette.EnvLightList lightList)
        {
            lmap.Lights.Clear();

            if (lightList == null)
                return;

            if (lightList.SubDiff0 != null) lmap.Lights.Add(LoadDirectionalLighting(lightList.SubDiff0));
          //  if (lightList.SubDiff1 != null) lmap.Lights.Add(LoadDirectionalLighting(lightList.SubDiff1));
          //  if (lightList.SubDiff2 != null) lmap.Lights.Add(LoadDirectionalLighting(lightList.SubDiff2));
        }

        public void FromEnvPaletteSpecular(AglLightmap lmap, EnvPalette.EnvLightList lightList)
        {
            lmap.Lights.Clear();

            if (lightList == null)
                return;

            if (lightList.SubSpec0 != null) lmap.Lights.Add(LoadDirectionalLighting(lightList.SubSpec0));
            if (lightList.SubSpec1 != null) lmap.Lights.Add(LoadDirectionalLighting(lightList.SubSpec1));
            if (lightList.SubSpec2 != null) lmap.Lights.Add(LoadDirectionalLighting(lightList.SubSpec2));
        }

        static AglLightmap.LightSource LoadDirectionalLighting(EnvPalette.EnvLightDirectional dirLight)
        {
            float amount = (dirLight.Intensity);

            AglLightmap.LightSource lightSource = new AglLightmap.LightSource();
            lightSource.Direction = GetDirectionalVector(dirLight.Latitude, dirLight.Longitude);
            lightSource.LowerColor = new Vector4(0, 0, 0, 1f) * amount;
            lightSource.UpperColor = dirLight.Color.ToVector4() * amount;
            return lightSource;
        }

        static Vector3 GetDirectionalVector(float latitude, float longitude)
        {
            // Convert latitude and longitude from degrees to radians
            float latRad = latitude * MathUtil.Deg2Rad;
            float lonRad = longitude * MathUtil.Deg2Rad;

            float x = MathF.Cos(latRad) * MathF.Sin(lonRad);
            float y = MathF.Sin(latRad);
            float z = MathF.Cos(latRad) * MathF.Cos(lonRad);

            var dir = new Vector3(-x, -y, -z);
            return Vector3.Normalize(dir);
        }
    }
}
