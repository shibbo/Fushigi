using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NativeFileDialogSharp;
using System.IO;

namespace Fushigi.ui.widgets
{
    public class FolderDialog
    {
        public string SelectedPath { get; set; } = "";

        public bool ShowDialog(string title = "Folder Select")
        {
            DialogResult dialogResult = Dialog.FolderPicker();
            SelectedPath = dialogResult.Path;
            return dialogResult.IsOk;
        }
    }
}