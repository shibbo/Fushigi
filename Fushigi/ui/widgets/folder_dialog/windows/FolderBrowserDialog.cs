using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FolderBrowserEx
{
    public enum DialogResult
    {
        OK,
        Cancel
    }

    public class FolderBrowserDialog
    {
        public FolderBrowserDialog()
        {
            SelectedFolders = new List<string>();
        }

        /// <summary>
        /// Gets/sets the title of the dialog
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets/sets folder in which dialog will be open.
        /// </summary>
        public string InitialFolder { get; set; }

        /// <summary>
        /// Gets/sets directory in which dialog will be open
        /// if there is no recent directory available.
        /// </summary>
        public string DefaultFolder { get; set; }

        /// <summary>
        /// Gets selected folder.
        /// </summary>
        public string SelectedFolder
        {
            get => SelectedFolders != null && SelectedFolders.Count >= 1 ? SelectedFolders[0] : string.Empty;
        }

        /// <summary>
        /// Gets selected folders when AllowMultiSelect is true.
        /// </summary>
        public List<string> SelectedFolders { get; private set; }

        public bool AllowMultiSelect { get; set; }

        /// <summary>
        /// Shows the folder browser dialog with a the default owner
        /// </summary>
        /// System.Windows.Forms.DialogResult.OK if the user clicks OK in the dialog box;
        /// otherwise, System.Windows.Forms.DialogResult.Cancel.
        /// </returns>
        public DialogResult ShowDialog()
        {
            return ShowDialog(IntPtr.Zero);
        }

        /// <summary>
        /// Shows the folder browser dialog with a the specified owner
        /// </summary>
        /// <param name="owner">Any object that implements IWin32Window to own the folder browser dialog</param>
        /// <returns>
        /// System.Windows.Forms.DialogResult.OK if the user clicks OK in the dialog box;
        /// otherwise, System.Windows.Forms.DialogResult.Cancel.
        /// </returns>
        public DialogResult ShowDialog(IntPtr handle)
        {
            SelectedFolders.Clear();
            if (Environment.OSVersion.Version.Major >= 6)
            {
                return ShowVistaDialog(handle);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
        private DialogResult ShowVistaDialog(IntPtr handle)
        {
            var frm = (NativeMethods.IFileOpenDialog)(new NativeMethods.FileOpenDialogRCW());
            frm.GetOptions(out uint options);
            options |= NativeMethods.FOS_PICKFOLDERS |
                       NativeMethods.FOS_FORCEFILESYSTEM |
                       NativeMethods.FOS_NOVALIDATE |
                       NativeMethods.FOS_NOTESTFILECREATE |
                       NativeMethods.FOS_DONTADDTORECENT;

            if (AllowMultiSelect)
                options |= NativeMethods.FOS_ALLOWMULTISELECT;

            frm.SetOptions(options);
            if (this.InitialFolder != null)
            {
                var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); //IShellItem
                if (NativeMethods.SHCreateItemFromParsingName
                   (this.InitialFolder, IntPtr.Zero, ref riid,
                    out NativeMethods.IShellItem directoryShellItem) == NativeMethods.S_OK)
                {
                    frm.SetFolder(directoryShellItem);
                }
            }
            if (this.DefaultFolder != null)
            {
                var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); //IShellItem
                if (NativeMethods.SHCreateItemFromParsingName
                   (this.DefaultFolder, IntPtr.Zero, ref riid,
                    out NativeMethods.IShellItem directoryShellItem) == NativeMethods.S_OK)
                {
                    frm.SetDefaultFolder(directoryShellItem);
                }
            }
            if (this.Title != null)
            {
                frm.SetTitle(this.Title);
            }
            if (frm.Show(handle) == NativeMethods.S_OK)
            {
                if (AllowMultiSelect)
                {
                    frm.GetResults(out NativeMethods.IShellItemArray shellItemArray);
                    shellItemArray.GetCount(out uint numFolders);
                    for (uint i = 0; i < numFolders; i++)
                    {
                        shellItemArray.GetItemAt(i, out NativeMethods.IShellItem shellItem);
                        if (shellItem.GetDisplayName(NativeMethods.SIGDN_FILESYSPATH,
                        out IntPtr pszString) == NativeMethods.S_OK)
                        {
                            if (pszString != IntPtr.Zero)
                            {
                                try
                                {
                                    this.SelectedFolders.Add(Marshal.PtrToStringAuto(pszString));
                                }
                                finally
                                {
                                    Marshal.FreeCoTaskMem(pszString);
                                }
                            }
                        }
                    }
                    return DialogResult.OK;
                }
                else if (!AllowMultiSelect && frm.GetResult(out NativeMethods.IShellItem shellItem) == NativeMethods.S_OK)

                {
                    if (shellItem.GetDisplayName(NativeMethods.SIGDN_FILESYSPATH,
                        out IntPtr pszString) == NativeMethods.S_OK)
                    {
                        if (pszString != IntPtr.Zero)
                        {
                            try
                            {
                                this.SelectedFolders.Add(Marshal.PtrToStringAuto(pszString));
                                return DialogResult.OK;
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(pszString);
                            }
                        }
                    }
                }
            }
            return DialogResult.Cancel;
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose() { } //just to have possibility of Using statement.
    }
}