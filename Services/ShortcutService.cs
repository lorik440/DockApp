using System;
using System.Runtime.InteropServices;

namespace DockApp.Avalonia.Services;

public static class ShortcutService
{
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLink
    {
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)]
            string pszFile,
            int cch,
            IntPtr pfd,
            uint fFlags);

        void GetDescription(
            [Out, MarshalAs(UnmanagedType.LPWStr)]
            string pszName,
            int cch);

        void SetPath(
            [MarshalAs(UnmanagedType.LPWStr)]
            string pszFile);

        // Other COM methods exist here, but we don't need them.
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);

        void IsDirty();

        void Load(
            [MarshalAs(UnmanagedType.LPWStr)]
            string pszFileName,
            uint dwMode);

        void Save(
            [MarshalAs(UnmanagedType.LPWStr)]
            string pszFileName,
            bool fRemember);

        void SaveCompleted(
            [MarshalAs(UnmanagedType.LPWStr)]
            string pszFileName);

        void GetCurFile(
            [MarshalAs(UnmanagedType.LPWStr)]
            out string ppszFileName);
    }

    public static string? Resolve(string shortcutPath)
    {
        try
        {
            var link = new ShellLink();

            var persist =
                (IPersistFile)link;

            persist.Load(
                shortcutPath,
                0);

            var shellLink =
                (IShellLink)link;

            string path =
                new string('\0', 260);

            shellLink.GetPath(
                path,
                path.Length,
                IntPtr.Zero,
                0);

            path = path.TrimEnd('\0');

            if (string.IsNullOrWhiteSpace(path))
                return null;

            return path;
        }
        catch
        {
            return null;
        }
    }
}