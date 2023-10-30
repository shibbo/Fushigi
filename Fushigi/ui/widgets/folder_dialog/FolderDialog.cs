using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TinyDialogsNet;

namespace Fushigi.ui.widgets
{
    /// <summary>
    /// A dialog for selecting a folder path native to the OS platform.
    /// </summary>
    public class FolderDialog
    {
        /// <summary>
        /// The selected path when the dialog has been successful. 
        /// </summary>
        public string SelectedPath { get; set; } = "";

        public bool ShowDialog(string title = "Folder Select")
        {
            string ofd = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FolderBrowserEx.FolderBrowserDialog dialog = new FolderBrowserEx.FolderBrowserDialog() { Title = title, InitialFolder = SelectedPath };
                dialog.ShowDialog();
                ofd = dialog.SelectedFolder;
            }
            else
            {
                ofd = Dialogs.SelectFolderDialog(title, SelectedPath);
            }

            if (!string.IsNullOrEmpty(ofd))
            {
                this.SelectedPath = ofd;
                return true;
            }

            return false;
        }
    }
}