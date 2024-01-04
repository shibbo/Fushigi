using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;
using System.Numerics;

namespace Fushigi.ui.widgets
{
    class OperationWarningDialog : IPopupModal<OperationWarningDialog.DialogResult>
    {
        public enum DialogResult
        {
            OK,
            Cancel
        }

        public static async Task<DialogResult> ShowDialog(IPopupModalHost modalHost,
            string title, string warning,
            params (string category, IReadOnlyList<string> warnings)[] categorizedWarnings)
        {
            var result = await modalHost.ShowPopUp(
                new OperationWarningDialog(warning, categorizedWarnings),
                title, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBringToFrontOnFocus);

            if (result.wasClosed)
                return DialogResult.Cancel;

            return result.result;
        }

        public OperationWarningDialog(string warning,
            (string category, IReadOnlyList<string> warnings)[] categorizedWarnings)
        {
            mWarning = warning;
            mCategorizedWarnings = categorizedWarnings;
        }

        public void DrawModalContent(Promise<DialogResult> promise)
        {
            ImGui.Text(mWarning);
            ImGui.Separator();

            #region scrollarea with sticky headers
            float width = ImGui.GetContentRegionAvail().X;
            ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0),
                new Vector2(width, ImGui.GetWindowViewport().WorkSize.Y / 3f));
            ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos());
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            var cursorPos = ImGui.GetCursorPos();
            if (ImGui.Begin("CategorizedWarnings",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoNavFocus))
            {
                var headerStickPosY = 0f;
                foreach (var (category, warnings) in mCategorizedWarnings)
                {
                    if (warnings.Count == 0)
                        continue;
                    float offset = 0;
                    if(ImGui.GetCursorPosY() - ImGui.GetScrollY() < headerStickPosY)
                    {
                        var newCursorY = ImGui.GetScrollY() + headerStickPosY;
                        offset = newCursorY - ImGui.GetCursorPosY();
                        ImGui.SetCursorPosY(newCursorY);
                    }

                    bool expanded = ImGui.CollapsingHeader($"{category} ({warnings.Count})",
                        ImGuiTreeNodeFlags.DefaultOpen);

                    if (ImGui.IsItemClicked())
                        ImGui.SetScrollY(ImGui.GetScrollY() - offset);

                    if(!expanded)
                        continue;

                    ImGui.PushClipRect(ImGui.GetCursorScreenPos(), 
                        new Vector2(float.PositiveInfinity), true);

                    headerStickPosY = ImGui.GetCursorPosY() - ImGui.GetScrollY();

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - offset);

                    ImGui.Indent();
                    foreach (string w in warnings)
                    {
                        ImGui.Text(w);
                        ImGui.Separator();
                    }
                    ImGui.Unindent();
                    ImGui.PopClipRect();
                    ImGui.SetWindowSize(new Vector2(100, 100));
                }
                cursorPos += ImGui.GetWindowSize() with { X = 0 };
                ImGui.End();
            }
            ImGui.PopStyleVar(2);
            ImGui.SetCursorPos(cursorPos);
            #endregion


            ImGui.Spacing();

            if (ImGui.Button("Continue anyway"))
                promise.SetResult(DialogResult.OK);

            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
                promise.SetResult(DialogResult.Cancel);
        }
        private readonly string mWarning;
        private readonly (string category, IReadOnlyList<string> warnings)[] mCategorizedWarnings;
    }
}
