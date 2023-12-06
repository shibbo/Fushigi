using Fushigi.util;
using ImGuiNET;
using Silk.NET.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.modal
{
    public interface IPopupModalHost
    {
        Task<(bool wasClosed, TResult result)> ShowPopUp<TResult>(IPopupModal<TResult> modal,
            string title,
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None,
            Vector2? minWindowSize = null);

        Task WaitTick();
    }

    public class PopupModalHost : IPopupModalHost
    {
        private struct PopupInfo
        {
            public string Id;
            public ImGuiWindowFlags WindowFlags;
            public Vector2? MinWindowSize;
        }

        private struct ModalMethods
        {
            public Action Execute;
            public Action Cancel;
        }

        private readonly Stack<(PopupInfo info, ModalMethods methods, Task resultTask)> mPopupStack = [];
        private readonly List<(PopupInfo info, ModalMethods methods, Task resultTask)> mNewPopups = [];

        private ulong mTicks = 0;
        private List<(ulong targetTick, TaskCompletionSource promise)> mTickWaiters = []; 

        public Task<(bool wasClosed, TResult result)> ShowPopUp<TResult>(IPopupModal<TResult> modal, 
            string title,
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None,
            Vector2? minWindowSize = null)
        {
            var info = new PopupInfo()
            {
                Id = $"{title}##ImPopup{mPopupStack.Count}",
                WindowFlags = windowFlags,
                MinWindowSize = minWindowSize
            };
            var completionSource = new TaskCompletionSource<(bool wasClosed, TResult result)>();
            var promise = new Promise<TResult>();

            var methods = new ModalMethods
            {
                Execute = () =>
                {
                    modal.DrawModalContent(promise);
                    if (promise.TryGetResult(out TResult? result))
                        completionSource.TrySetResult((wasClosed: false, result));
                },
                Cancel = () =>
                {
                    completionSource.TrySetResult((wasClosed: true, default!));
                }
            };

            lock(mNewPopups)
                mNewPopups.Add((info, methods, completionSource.Task));

            return completionSource.Task;
        }

        public Task WaitTick()
        {
            lock (mTickWaiters)
            {
                var promise = new TaskCompletionSource();

                mTickWaiters.Add((mTicks+2, promise));

                return promise.Task;
            }
        }

        private readonly List<Task> mModalsToClose = [];

        public void DrawHostedModals()
        {
            lock (mTickWaiters)
            {
                for (int i = mTickWaiters.Count - 1; i >= 0; i--)
                {
                    if (mTickWaiters[i].targetTick == mTicks)
                    {
                        mTickWaiters[i].promise.SetResult();
                        mTickWaiters.RemoveAt(i);
                    }

                }
            }

            lock (mNewPopups)
            {
                foreach (var item in mNewPopups)
                    mPopupStack.Push(item);

                mNewPopups.Clear();
            }

            mModalsToClose.Clear();
            //close all modals with a result
            foreach (var (_, _, resultTask) in mPopupStack)
            {
                if (resultTask.IsCompleted)
                    mModalsToClose.Add(resultTask);
                else
                    break; //we don't want to close modals that have unfinished modals in front
            }

            int openedPopups = 0;

            foreach (var (info, methods, resultTask) in mPopupStack.Reverse())
            {
                bool shouldClose = mModalsToClose.Contains(resultTask);

                Vector2 center = ImGui.GetMainViewport().GetCenter();
                ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
                if(info.MinWindowSize.TryGetValue(out Vector2 minWindowSize))
                    ImGui.SetNextWindowSizeConstraints(minWindowSize, ImGui.GetWindowViewport().Size);

                if(!shouldClose && !ImGui.IsPopupOpen(info.Id))
                    ImGui.OpenPopup(info.Id);

                bool open = true;
                if (!ImGui.BeginPopupModal(info.Id, ref open, info.WindowFlags))
                {
                    methods.Cancel();
                    continue;
                }

                methods.Execute();
                    

                if (shouldClose)
                    ImGui.CloseCurrentPopup();

                openedPopups++;
            }

            for (var i = 0; i < openedPopups; i++)
                ImGui.EndPopup();


            while (mPopupStack.TryPeek(out var entry) &&
                mModalsToClose.Contains(entry.resultTask))
            {
                mPopupStack.Pop();
            }

            lock (mTickWaiters)
                mTicks++;
        }
    }
}
