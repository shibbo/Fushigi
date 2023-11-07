using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.util
{
    public class MessageBox
    {
        public enum MessageBoxType
        {
            YesNo = 0,
            Ok = 1
        }

        public enum MessageBoxResult
        {
            Waiting = -1,
            Ok = 0,
            No = 1,
            Yes = 2,
            Closed = 3
        }

        public MessageBox(MessageBoxType type)
        {
            mType = type;
        }

        public MessageBoxResult Show(string header, string message)
        {
            MessageBoxResult res = MessageBoxResult.Waiting;

            bool needsClose = true;
            bool status = ImGui.Begin(header, ref needsClose);
            ImGui.Text(message);

            switch (mType)
            {
                case MessageBoxType.Ok:
                    if (ImGui.Button("OK"))
                    {
                        res = MessageBoxResult.Ok;
                    }
                    break;
                case MessageBoxType.YesNo:
                    if (ImGui.Button("Yes"))
                    {
                        res = MessageBoxResult.Yes;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("No"))
                    {
                        res = MessageBoxResult.No;
                    }
                    break;
            }

            if (!needsClose)
            {
                res = MessageBoxResult.Closed;
            }

            if (status)
            {
                ImGui.End();
            }

            return res;
        }

        MessageBoxType mType;
    }
}
