using Fushigi.gl;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    /// <summary>
    /// Manages cached images to be added and loaded once on load. 
    /// </summary>
    public class GLImageCache
    {
        /// <summary>
        /// A list of cached images.
        /// </summary>
        public Dictionary<string, uint> Images = new Dictionary<string, uint>();

        public uint GetImage(GL gl, string key, string filePath)
        {
            if (!Images.ContainsKey(key))
                Images.Add(key, GLTexture2D.Load(gl, filePath).ID);

            return Images[key];
        }

        public void RemoveImage(GL gl, string key)
        {
            if (Images.ContainsKey(key))
            {
                gl.DeleteTexture(Images[key]);
                Images.Remove(key);
            }
        }

        public void Dispose(GL gl)
        {
            foreach (var image in Images)
                gl.DeleteTexture(image.Value);
            Images.Clear();
        }
    }
}
