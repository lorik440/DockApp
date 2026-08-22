using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;

namespace DockApp.Avalonia.Services;

public static class IconService
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;

    [DllImport(
        "shell32.dll",
        CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static IntPtr GetIconHandle(string executablePath)
    {
        var result = SHGetFileInfo(
            executablePath,
            0,
            out SHFILEINFO fileInfo,
            (uint)Marshal.SizeOf<SHFILEINFO>(),
            SHGFI_ICON | SHGFI_LARGEICON);

        if (result == IntPtr.Zero)
            return IntPtr.Zero;

        return fileInfo.hIcon;
    }

    public static void DestroyIconHandle(IntPtr iconHandle)
    {
        if (iconHandle != IntPtr.Zero)
            DestroyIcon(iconHandle);
    }

    public static Bitmap? GetIcon(string executablePath)
    {
        IntPtr hIcon = GetIconHandle(executablePath);

        if (hIcon == IntPtr.Zero)
            return null;

        try
        {
            byte[] png = HIconToPng(hIcon);

            using var stream = new MemoryStream(png);

            return new Bitmap(stream);
        }
        finally
        {
            DestroyIconHandle(hIcon);
        }
    }

    private static byte[] HIconToPng(IntPtr hIcon)
    {
        // We'll implement the native HICON -> PNG conversion here.
        throw new NotImplementedException();
    }
}