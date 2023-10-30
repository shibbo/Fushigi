using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FolderBrowserEx
{
    internal static class NativeMethods
    {
        #region Constants
        public const uint FOS_PICKFOLDERS = 0x00000020;
        public const uint FOS_FORCEFILESYSTEM = 0x00000040;
        public const uint FOS_NOVALIDATE = 0x00000100;
        public const uint FOS_NOTESTFILECREATE = 0x00010000;
        public const uint FOS_DONTADDTORECENT = 0x02000000;
        public const uint FOS_ALLOWMULTISELECT = 0x00000200;

        public const uint S_OK = 0x0000;

        public const uint SIGDN_FILESYSPATH = 0x80058000;

        #endregion

        #region COM

        [ComImport, ClassInterface(ClassInterfaceType.None),
            TypeLibType(TypeLibTypeFlags.FCanCreate),
                    Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        internal class FileOpenDialogRCW { }

        [ComImport, Guid("d57c7288-d4ad-4768-be02-9d969532d960"),
            InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IFileOpenDialog : IFileDialog
        {
            // Defined on IModalWindow - repeated here due to requirements of COM interop layer
            // --------------------------------------------------------------------------------
            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime), PreserveSig]
            new uint Show([In] IntPtr parent);

            // Defined on IFileDialog - repeated here due to requirements of COM interop layer
            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetFileTypes([In] uint cFileTypes,
                [In, MarshalAs(UnmanagedType.LPArray)] IntPtr rgFilterSpec);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetFileTypeIndex([In] uint iFileType);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void GetFileTypeIndex(out uint piFileType);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde,
                out uint pdwCookie);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void Unadvise([In] uint dwCookie);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetOptions([In] uint fos);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void GetOptions(out uint pfos);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new uint GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            void Close([MarshalAs(UnmanagedType.Error)] int hr);

            [MethodImpl(MethodImplOptions.InternalCall,

                MethodCodeType = MethodCodeType.Runtime)]
            new void SetClientGuid([In] ref Guid guid);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void ClearClientData();

            // Not supported:  IShellItemFilter is not defined, converting to IntPtr
            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            new void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

            // Defined by IFileOpenDialog
            // ---------------------------------------------------------------------------------
            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            uint GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);

            [MethodImpl(MethodImplOptions.InternalCall,
                MethodCodeType = MethodCodeType.Runtime)]
            void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
        }

        [ComImport(), Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"),
                      InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IFileDialog
        {
            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig()]
            uint Show([In, Optional] IntPtr hwndOwner); //IModalWindow

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypes([In] uint cFileTypes,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr rgFilterSpec);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypeIndex([In] uint iFileType);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileTypeIndex(out uint piFileType);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde,
            out uint pdwCookie);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint Unadvise([In] uint dwCookie);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetOptions([In] uint fos);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint GetOptions(out uint fos);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint GetCurrentSelection
                 ([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint AddPlace
              ([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);

            [MethodImpl(MethodImplOptions.InternalCall,
             MethodCodeType = MethodCodeType.Runtime)]
            uint SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)]
                                     string pszDefaultExtension);

            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint Close([MarshalAs(UnmanagedType.Error)] uint hr);

            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint SetClientGuid([In] ref Guid guid);

            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint ClearClientData();

            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
        }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"),
                    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItem
        {
            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint BindToHandler([In] IntPtr pbc, [In] ref Guid rbhid,
            [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IntPtr ppvOut);

            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint GetDisplayName([In] uint sigdnName, out IntPtr ppszName);

            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

            [MethodImpl(MethodImplOptions.InternalCall,
                        MethodCodeType = MethodCodeType.Runtime)]
            uint Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi,
                         [In] uint hint, out int piOrder);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        internal enum SIATTRIBFLAGS
        {
            AND = 1,
            APPCOMPAT = 3,
            OR = 2
        }

        [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"),
                    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItemArray
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid rbhid,
                    [In] ref Guid riid, out IntPtr ppvOut);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetPropertyStore([In] int Flags, [In] ref Guid riid, out IntPtr ppv);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetPropertyDescriptionList([In] ref PROPERTYKEY keyType, [In] ref Guid riid, out IntPtr ppv);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetAttributes([In] SIATTRIBFLAGS dwAttribFlags, [In] uint sfgaoMask, out uint psfgaoAttribs);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCount(out uint pdwNumItems);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetItemAt([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void EnumItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
        }
        #endregion

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int SHCreateItemFromParsingName
         ([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc,
         ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
    }
}