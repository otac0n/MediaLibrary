// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Large portions from <see href="https://www.pinvoke.net/"/>.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// From <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280512(v=vs.85).aspx"/>.
        /// </summary>
        public enum DpiAwareness : int
        {
            None = 0,
            SystemAware = 1,
            PerMonitorAware = 2,
        }

        [Flags]
        private enum FILEOP_Flags : ushort
        {
            FOF_MULTIDESTFILES = 0x0001,
            FOF_CONFIRMMOUSE = 0x0002,
            FOF_SILENT = 0x0004,
            FOF_RENAMEONCOLLISION = 0x0008,
            FOF_NOCONFIRMATION = 0x0010,
            FOF_WANTMAPPINGHANDLE = 0x0020,
            FOF_ALLOWUNDO = 0x0040,
            FOF_FILESONLY = 0x0080,
            FOF_SIMPLEPROGRESS = 0x0100,
            FOF_NOCONFIRMMKDIR = 0x0200,
            FOF_NOERRORUI = 0x0400,
            FOF_NOCOPYSECURITYATTRIBS = 0x0800,
            FOF_NORECURSION = 0x1000,
            FOF_NO_CONNECTED_ELEMENTS = 0x2000,
            FOF_WANTNUKEWARNING = 0x4000,
            FOF_NORECURSEREPARSE = 0x8000,
        }

        private enum FILEOP_Func : uint
        {
            FO_MOVE = 0x0001,
            FO_COPY = 0x0002,
            FO_DELETE = 0x0003,
            FO_RENAME = 0x0004,
        }

        public static void DeleteToRecycleBin(params string[] paths)
        {
            int result;

            var hWnd = Process.GetCurrentProcess().MainWindowHandle;
            var wFunc = FILEOP_Func.FO_DELETE;
            var fFlags = FILEOP_Flags.FOF_SILENT | FILEOP_Flags.FOF_NOCONFIRMATION | FILEOP_Flags.FOF_ALLOWUNDO;
            var pFrom = string.Concat(paths.Select(p => $"{p}\0")) + "\0";
            var lpszProgressTitle = string.Empty + "\0";

            if (IntPtr.Size == 8)
            {
                var operation = new SHFILEOPSTRUCT64
                {
                    hWnd = hWnd,
                    wFunc = wFunc,
                    fFlags = fFlags,
                    pFrom = pFrom,
                    pTo = null,
                    lpszProgressTitle = lpszProgressTitle,
                };

                result = NativeMethods.SHFileOperation64(ref operation);
            }
            else
            {
                var operation = new SHFILEOPSTRUCT32
                {
                    hWnd = hWnd,
                    wFunc = wFunc,
                    fFlags = fFlags,
                    pFrom = pFrom,
                    pTo = null,
                    lpszProgressTitle = lpszProgressTitle,
                };

                result = NativeMethods.SHFileOperation32(ref operation);
            }

            switch (result)
            {
                case 0:
                    break;
            }
        }

        [DllImport("Shcore.dll")]
        public static extern int SetProcessDpiAwareness(DpiAwareness PROCESS_DPI_AWARENESS);

        [DllImport("shell32.dll", EntryPoint = "SHFileOperation", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation32([In, Out] ref SHFILEOPSTRUCT32 lpFileOp);

        [DllImport("shell32.dll", EntryPoint = "SHFileOperation", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation64([In, Out] ref SHFILEOPSTRUCT64 lpFileOp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
        private struct SHFILEOPSTRUCT32
        {
            public IntPtr hWnd;
            public FILEOP_Func wFunc;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;

            public FILEOP_Flags fFlags;

            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;

            public IntPtr hNameMappings;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        private struct SHFILEOPSTRUCT64
        {
            public IntPtr hWnd;
            public FILEOP_Func wFunc;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;

            public FILEOP_Flags fFlags;

            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;

            public IntPtr hNameMappings;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }
    }
}
