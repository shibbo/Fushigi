using Fushigi.util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.modal
{
    public abstract class OkDialog<TDialog> : IPopupModal<OkDialog<TDialog>.Void>
        where TDialog : OkDialog<TDialog>, new()
    {
        private struct Void { }

        protected abstract string Title { get; }

        public static async Task ShowDialog(IPopupModalHost modalHost)
        {
            var dialog = new TDialog();
            await modalHost.ShowPopUp(dialog, dialog.Title,
                ImGuiWindowFlags.AlwaysAutoResize);
        }

        protected abstract void DrawBody();

        void IPopupModal<Void>.DrawModalContent(Promise<Void> promise)
        {
            DrawBody();

            if (ImGui.Button("OK"))
            {
                promise.SetResult(new Void());
            }
        }
    }
}
