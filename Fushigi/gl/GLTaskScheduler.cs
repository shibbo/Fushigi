using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.gl
{
    public class GLTaskScheduler
    {
        private List<(TaskCompletionSource promise, Action<GL> task)> mPending = [];

        public async Task<TResult> Schedule<TResult>(Func<GL, TResult> task)
        {
            TResult result = default!;

            await Schedule(gl => {
                result = task(gl);
            });
            return result;
        }

        public Task Schedule(Action<GL> task)
        {
            var promise = new TaskCompletionSource();

            lock (mPending)
            {
                mPending.Add((promise, task));
            }

            return promise.Task;
        }

        public void ExecutePending(GL gl)
        {
            int count;
            lock (mPending)
                count = mPending.Count;

            int i = 0;
            while (i < count)
            {
                (TaskCompletionSource promise, Action<GL> task) = mPending[i++];
                task.Invoke(gl);
                promise.SetResult();

                lock (mPending)
                    count = mPending.Count;
            }

            lock (mPending)
            {
                mPending.RemoveRange(0, i);
            }
        }
    }
}
