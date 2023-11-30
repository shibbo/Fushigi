using Fushigi.gl;
using Fushigi.param;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course.distance_view
{
    public class DistantViewManager
    {
        private Dictionary<string, Matrix4x4> LayerMatrices = new Dictionary<string, Matrix4x4>();

        private DVLayerParamTable ParamTable = new DVLayerParamTable();

        private CourseActor DVLocator;

        private float ScrollSpeedX = -0.025f;
        private float ScrollSpeedY = 0f;

        public DistantViewManager(CourseArea area)
        {
            PrepareDVLocator(area);
        }

        public void PrepareDVLocator(CourseArea area)
        {
            ParamTable.LoadDefault();

            foreach (var actor in area.GetActors())
            {
                if (actor.mActorName == "DVBasePosLocator")
                {
                    DVLocator = actor;
                    //TODO there should be a way to update these during property edit
                    if (DVLocator.mActorParameters.ContainsKey("TimeScrollRateX"))
                        ScrollSpeedX = (float)DVLocator.mActorParameters["TimeScrollRateX"];
                    if (DVLocator.mActorParameters.ContainsKey("TimeScrollRateY"))
                        ScrollSpeedY = (float)DVLocator.mActorParameters["TimeScrollRateY"];
                    if (DVLocator.mActorParameters.ContainsKey("DVLayerParamName"))
                    {
                        string layer_param = (string)DVLocator.mActorParameters["DVLayerParamName"];
                        if (!string.IsNullOrEmpty(layer_param))
                            ParamTable.Load(layer_param);
                    }
                }
            }

            LayerMatrices.Clear();
            foreach (var layer in this.ParamTable.Layers)
                LayerMatrices.Add(layer.Key, Matrix4x4.Identity);
        }

        public void UpdateMatrix(string layer, ref Matrix4x4 matrix)
        {
            if (LayerMatrices.ContainsKey(layer))
                matrix *= LayerMatrices[layer];
        }

        public void Calc(Vector3 camera_pos)
        {
            foreach (var layer in this.ParamTable.Layers.Keys)
            {
                var scroll_config = ParamTable.Layers[layer];
                var locator_pos = DVLocator != null ? DVLocator.mTranslation : Vector3.Zero;

                //Place via base locator pos + camera

                //Distance between dv locator and camera
                Vector2 distance = new Vector2(camera_pos.X - locator_pos.X, camera_pos.Y - locator_pos.Y);
                Vector2 movement_ratio = new Vector2(1.0f) - scroll_config;
                Vector2 scroll_time_rate = new Vector2(1.0f - ScrollSpeedX, 1.0f - ScrollSpeedY);

                float posX = 0, posY = 0;

                if (scroll_config.X != 1 && ScrollSpeedX != 0)
                    posX = distance.X * movement_ratio.X * scroll_time_rate.X;
                if (scroll_config.X != 1 && ScrollSpeedX != 0)
                    posY = distance.Y * movement_ratio.Y * scroll_time_rate.Y;

                LayerMatrices[layer] = Matrix4x4.CreateTranslation(posX, posY, 0);
            }
        }
    }
}
