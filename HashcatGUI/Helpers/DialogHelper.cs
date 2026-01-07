using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace HashcatGUI.Helpers;

public static class DialogHelper
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr ptr);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BROWSEINFO
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public IntPtr pszDisplayName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszTitle;
        public uint ulFlags;
        public IntPtr lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    private const uint BIF_RETURNONLYFSDIRS = 0x0001;
    private const uint BIF_NEWDIALOGSTYLE = 0x0040;
    private const uint BIF_EDITBOX = 0x0010;

    public static string? BrowseForFolder(string title)
    {
        var hwnd = IntPtr.Zero;
        var mainWindow = Application.Current?.MainWindow;
        if (mainWindow != null)
        {
            hwnd = new WindowInteropHelper(mainWindow).Handle;
        }

        var pszPath = Marshal.AllocHGlobal(260 * 2);
        var pszDisplayName = Marshal.AllocHGlobal(260 * 2);

        try
        {
            var bi = new BROWSEINFO
            {
                hwndOwner = hwnd,
                pidlRoot = IntPtr.Zero,
                pszDisplayName = pszDisplayName,
                lpszTitle = title,
                ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE | BIF_EDITBOX,
                lpfn = IntPtr.Zero,
                lParam = IntPtr.Zero,
                iImage = 0
            };

            var pidl = SHBrowseForFolder(ref bi);
            if (pidl != IntPtr.Zero)
            {
                try
                {
                    if (SHGetPathFromIDList(pidl, pszPath))
                    {
                        return Marshal.PtrToStringUni(pszPath);
                    }
                }
                finally
                {
                    CoTaskMemFree(pidl);
                }
            }

            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(pszPath);
            Marshal.FreeHGlobal(pszDisplayName);
        }
    }
}
