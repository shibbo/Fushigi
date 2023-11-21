using Fushigi.Bfres;
using Fushigi.util;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class VRSkybox
    {
        private BfresRender BfresRender;
        private Matrix4x4 Transform = Matrix4x4.Identity;

        public VRSkybox(GL gl)
        {
            Init(gl);
        }

        private void Init(GL gl)
        {
            var file_path = FileUtil.FindContentPath(Path.Combine("Model", $"VRModel.bfres.zs"));
            if (!File.Exists(file_path))
                return;

            BfresRender = new BfresRender(gl, FileUtil.DecompressAsStream(file_path));
            this.Transform = Matrix4x4.CreateScale(10000);
        }

        public void Render(GL gl, Camera camera)
        {
            BfresRender.Render(gl, Transform, camera);
        }
    }
}
