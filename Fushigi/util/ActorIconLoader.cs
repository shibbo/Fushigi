using Fushigi.gl;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    /// <summary>
    /// A cache for loading icons for actors.
    /// </summary>
    public class ActorIconLoader
    {
        static Dictionary<string, int> Icons = new Dictionary<string, int>();

        public static void Init()
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "res", "actor_icons");
            string actor_icons_zip = Path.Combine(AppContext.BaseDirectory, "res", "ActorIcons.zip");
            if (!File.Exists(actor_icons_zip))
                return;

            //Unpack actor icons if folder is not present
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);

                Task.Run(() =>
                {
                    ZipArchive zip = new ZipArchive(File.OpenRead(actor_icons_zip));
                    zip.ExtractToDirectory(folder);
                });
            }
        }

        public static int GetIcon(GL gl, string gyml, string model)
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "res", "actor_icons");
            string icon_path = Path.Combine(folder, $"{gyml}.bfres_{model}.png");

            if (Icons.ContainsKey(icon_path))
                return Icons[icon_path];

            if (File.Exists(icon_path))
            {
                //Load the needed icon
                var tex = GLTexture2D.Load(gl, icon_path);
                Icons.Add(icon_path, (int)tex.ID);
                return (int)tex.ID;
            }

            return -1;
        }
    }
}
