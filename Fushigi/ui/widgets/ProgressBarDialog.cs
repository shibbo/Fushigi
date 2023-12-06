using Fushigi.param;
using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;
using NativeFileDialogSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    public class ProgressBarDialog : IPopupModal<ProgressBarDialog.Void>
    {
        private struct Void { }

        private class Progress : IProgress<(string operationName, float? progress)>
        {
            public event Action<(string operationName, float? progress)>? ProgressChanged;
            public void Report((string operationName, float? progress) value)
            {
                lock (this) //just in case
                {
                    ProgressChanged?.Invoke(value);
                }
            }
        }

        public static async Task ShowDialogForAsyncAction(IPopupModalHost modalHost, 
            string text, Func<IProgress<(string operationName, float? progress)>, Task> asyncAction)
        {
            var progress = new Progress();
            var dialog = new ProgressBarDialog(progress, text);
            dialog.mTask = asyncAction(progress);
            await modalHost.ShowPopUp(dialog, "",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar,
                minWindowSize: new Vector2(300, 150));
        }

        public static async Task<TResult> ShowDialogForAsyncFunc<TResult>(IPopupModalHost modalHost,
            string text, Func<IProgress<(string operationName, float? progress)>, Task<TResult>> asyncFunc)
        {
            var progress = new Progress();
            var dialog = new ProgressBarDialog(progress, text);
            var task = asyncFunc(progress);
            dialog.mTask = task;
            await modalHost.ShowPopUp(dialog, "",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
            return task.Result;
        }

        private ProgressBarDialog(Progress progress, string text)
        {
            mTask = null!; //asyncAction/asyncFunc needs to be executed AFTER this constructor
                           //otherwise we might miss a progress report, therefore we don't have a task yet
            mText = text;
            progress.ProgressChanged += p =>
            {
                mProgressValue = p.progress;
                mOperationName = p.operationName;
            };
        }

        void IPopupModal<Void>.DrawModalContent(Promise<Void> promise)
        {
            ImGui.GetFont().Scale = 1.2f;
            ImGui.PushFont(ImGui.GetFont());

            ImGui.Dummy(Vector2.Zero with { X = ImGui.CalcTextSize(mText + sDots[^1]).X });
            ImGui.Text($"{mText}{sDots[(int)ImGui.GetTime() % sDots.Length]}");

            ImGui.GetFont().Scale = 1;
            ImGui.PopFont();

            ImGui.Spacing();

            if(mOperationName is not null)
            {
                ImGui.Text(mOperationName);
                if(mProgressValue.TryGetValue(out float value))
                    ImGui.ProgressBar(value, Vector2.Zero with { X = ImGui.GetContentRegionAvail().X});
            }
            else
            {
                ImGui.NewLine();
            }
            

            if (mTask.IsCompleted)
                promise.SetResult(new Void());
        }

        private static readonly string[] sDots = [
            ".",
            "..",
            "...",
        ];

        private float? mProgressValue = 0;
        private string? mOperationName;
        private Task mTask;
        private readonly string mText;
    }
}
