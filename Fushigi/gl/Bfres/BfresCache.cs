using Fushigi.util;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl.Bfres
{
    public class BfresCache
    {
        public static Dictionary<string, Task<BfresRender?>> Cache = [];

        [Obsolete("Only exists for compatibility with the tile rendering branch")]
        public static BfresRender? Load(GL gl, string projectName)
        {
            if (!Cache.ContainsKey(projectName))
            {
                var path = FileUtil.FindContentPath(Path.Combine("Model", projectName + ".bfres.zs"));
                if (File.Exists(path))
                {
                    Cache.Add(projectName, Task.FromResult<BfresRender?>(
                        new BfresRender(gl, FileUtil.DecompressAsStream(path))));
                }
                else //use null renderer to not check the file again (todo this function should only load during course load)
                {
                    Cache.Add(projectName, Task.FromResult<BfresRender?>(null));
                }
            }
            var task = Cache[projectName];
            return task.IsCompletedSuccessfully ? task.Result : null;
        }

        public static Task<BfresRender?> LoadAsync(GLTaskScheduler glScheduler, string projectName)
        {
            if (!Cache.ContainsKey(projectName))
            {
                var path = FileUtil.FindContentPath(Path.Combine("Model", projectName + ".bfres.zs"));
                if (File.Exists(path))
                {
                    Cache.Add(projectName, LoadInternal(glScheduler, path));
                }
                else //use null renderer to not check the file again (todo this function should only load during course load)
                {
                    Cache.Add(projectName, Task.FromResult<BfresRender?>(null));
                }
            }
            var task = Cache[projectName];
            return task;
        }

        private static async Task<BfresRender?> LoadInternal(GLTaskScheduler glScheduler, string path)
        {
            using var stream = await Task.Run<Stream>(() => FileUtil.DecompressAsStream(path));

            BfresRender render = await glScheduler.Schedule(gl => new BfresRender(gl, stream));
            return render;
        }
    }
}
