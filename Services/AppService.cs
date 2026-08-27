using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using DockApp.Avalonia.Models;

namespace DockApp.Avalonia.Services;

public class AppService
{
    private static readonly Guid FOLDERID_AppsFolder =
        new("1e87508d-89c2-42f0-8a7e-645a0f50ca58");

    private const uint SHGDN_NORMAL = 0x00000000;
    private const uint SHGDN_DESKTOPABSOLUTEPARSING = 0x80028000;

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_PIDL = 0x000000008;

    public List<AppInfo> GetApps()
    {
        var apps = new List<AppInfo>();

        IntPtr appsFolderPidl = IntPtr.Zero;

        try
        {
            int hr = SHParseDisplayName(
                "shell:AppsFolder",
                IntPtr.Zero,
                out appsFolderPidl,
                0,
                IntPtr.Zero);

            if (hr != 0 || appsFolderPidl == IntPtr.Zero)
                return apps;

            IntPtr shellFolderPtr;

            hr = SHBindToObject(
                IntPtr.Zero,
                appsFolderPidl,
                IntPtr.Zero,
                typeof(IShellFolder).GUID,
                out shellFolderPtr);

            if (hr != 0 || shellFolderPtr == IntPtr.Zero)
                return apps;

            var shellFolder =
                (IShellFolder)Marshal.GetObjectForIUnknown(shellFolderPtr);

            Marshal.Release(shellFolderPtr);

            uint enumFlags =
                (uint)(SHCONTF.SHCONTF_FOLDERS |
                       SHCONTF.SHCONTF_NONFOLDERS);

            hr = shellFolder.EnumObjects(
                IntPtr.Zero,
                enumFlags,
                out IEnumIDList enumIdList);

            if (hr != 0 || enumIdList == null)
                return apps;

            IntPtr itemPidl;

            while (enumIdList.Next(1, out itemPidl, out uint fetched) == 0
                   && fetched == 1)
            {
                try
                {
                    string? name = GetDisplayName(
                        shellFolder,
                        itemPidl,
                        SHGDN_NORMAL);

                    string? parsingName = GetDisplayName(
                        shellFolder,
                        itemPidl,
                        SHGDN_DESKTOPABSOLUTEPARSING);

                    if (string.IsNullOrWhiteSpace(name) ||
                        string.IsNullOrWhiteSpace(parsingName))
                    {
                        continue;
                    }

                    string appId = NormalizeAppId(parsingName);

                    if (string.IsNullOrWhiteSpace(appId))
                        continue;

                    if (IsExcludedApp(name, appId))
                        continue;

                    apps.Add(new AppInfo
                    {
                        Id = appId,
                        Name = name,
                        Path = appId,
                        ExecutablePath = GetExecutablePath(appId)
                    });
                }
                finally
                {
                    Marshal.FreeCoTaskMem(itemPidl);
                }
            }

            Marshal.ReleaseComObject(enumIdList);
            Marshal.ReleaseComObject(shellFolder);

            return apps
                .GroupBy(app => app.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(app => app.Name)
                .ToList();
        }
        finally
        {
            if (appsFolderPidl != IntPtr.Zero)
                Marshal.FreeCoTaskMem(appsFolderPidl);
        }
    }

    private string? GetDisplayName(
        IShellFolder folder,
        IntPtr pidl,
            uint flags)
    {
        try
        {
            int hr = folder.GetDisplayNameOf(
                pidl,
                flags,
                out STRRET strret);

            if (hr != 0)
                return null;

            char[] buffer = new char[512];

            hr = StrRetToBufW(
                ref strret,
                pidl,
                buffer,
                (uint)buffer.Length);

            if (hr == 0)
            {
                return new string(buffer)
                    .TrimEnd('\0');
            }

            // STRRET_WSTR
            if (strret.uType == 0)
            {
                if (strret.pOleStr == IntPtr.Zero)
                    return null;

                string? result =
                    Marshal.PtrToStringUni(strret.pOleStr);

                Marshal.FreeCoTaskMem(strret.pOleStr);

                return result;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private Bitmap? GetIcon(
        IntPtr appsFolderPidl,
        IntPtr pidl)
    {
        IntPtr iconHandle = IntPtr.Zero;

        try
        {
            IntPtr pidlFull = ILCombine(
                appsFolderPidl,
                pidl);

            if (pidlFull == IntPtr.Zero)
                return null;

            try
            {
                SHFILEINFO info = new();

                IntPtr result = SHGetFileInfo(
                    pidlFull,
                    0,
                    ref info,
                    (uint)Marshal.SizeOf<SHFILEINFO>(),
                    SHGFI_PIDL | SHGFI_ICON | SHGFI_LARGEICON);

                if (result == IntPtr.Zero)
                    return null;

                iconHandle = info.hIcon;

                if (iconHandle == IntPtr.Zero)
                    return null;

                using var iconStream =
                    IconToPngStream(iconHandle);

                return new Bitmap(iconStream);
            }
            finally
            {
                ILFree(pidlFull);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            if (iconHandle != IntPtr.Zero)
                DestroyIcon(iconHandle);
        }
    }

    private System.IO.MemoryStream IconToPngStream(IntPtr hIcon)
    {
        using var icon = System.Drawing.Icon.FromHandle(hIcon);

        using var bitmap = icon.ToBitmap();

        var stream = new System.IO.MemoryStream();

        bitmap.Save(
            stream,
            System.Drawing.Imaging.ImageFormat.Png);

        stream.Position = 0;

        return stream;
    }

    public void LaunchApp(AppInfo app)
    {
        if (string.IsNullOrWhiteSpace(app.Path))
            return;

        string launchPath = app.Path.StartsWith(
            "shell:AppsFolder\\",
            StringComparison.OrdinalIgnoreCase)
            ? app.Path
            : $@"shell:AppsFolder\{app.Path}";

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{launchPath}\"",
            UseShellExecute = true
        });
    }

    private static string NormalizeAppId(string parsingName)
    {
        const string appsFolderPrefix = "shell:AppsFolder\\";

        string result = parsingName.Trim();

        if (result.StartsWith(
            appsFolderPrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            result = result[appsFolderPrefix.Length..];
        }

        return result;
    }

    public void LoadIcons(
        IReadOnlyCollection<AppInfo> apps)
    {
        if (apps.Count == 0)
            return;

        var appsById = apps
            .Where(app => app.Icon is null)
            .GroupBy(app => app.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        if (appsById.Count == 0)
            return;

        IntPtr appsFolderPidl = IntPtr.Zero;

        try
        {
            int hr = SHParseDisplayName(
                "shell:AppsFolder",
                IntPtr.Zero,
                out appsFolderPidl,
                0,
                IntPtr.Zero);

            if (hr != 0 || appsFolderPidl == IntPtr.Zero)
                return;

            hr = SHBindToObject(
                IntPtr.Zero,
                appsFolderPidl,
                IntPtr.Zero,
                typeof(IShellFolder).GUID,
                out IntPtr shellFolderPtr);

            if (hr != 0 || shellFolderPtr == IntPtr.Zero)
                return;

            var shellFolder =
                (IShellFolder)Marshal.GetObjectForIUnknown(shellFolderPtr);

            Marshal.Release(shellFolderPtr);

            try
            {
                uint enumFlags =
                    (uint)(SHCONTF.SHCONTF_FOLDERS |
                           SHCONTF.SHCONTF_NONFOLDERS);

                hr = shellFolder.EnumObjects(
                    IntPtr.Zero,
                    enumFlags,
                    out IEnumIDList enumIdList);

                if (hr != 0 || enumIdList == null)
                    return;

                try
                {
                    IntPtr itemPidl;

                    while (appsById.Count > 0 &&
                           enumIdList.Next(
                               1,
                               out itemPidl,
                               out uint fetched) == 0 &&
                           fetched == 1)
                    {
                        try
                        {
                            string? parsingName = GetDisplayName(
                                shellFolder,
                                itemPidl,
                                SHGDN_DESKTOPABSOLUTEPARSING);

                            if (string.IsNullOrWhiteSpace(parsingName))
                                continue;

                            string appId = NormalizeAppId(parsingName);

                            if (!appsById.TryGetValue(
                                    appId,
                                    out AppInfo? app))
                            {
                                continue;
                            }

                            app.Icon = GetIcon(
                                appsFolderPidl,
                                itemPidl);

                            appsById.Remove(appId);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(itemPidl);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(enumIdList);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(shellFolder);
            }
        }
        finally
        {
            if (appsFolderPidl != IntPtr.Zero)
                Marshal.FreeCoTaskMem(appsFolderPidl);
        }
    }

    private static string? GetExecutablePath(string appId)
    {
        if (appId.EndsWith(
                ".exe",
                StringComparison.OrdinalIgnoreCase) &&
            System.IO.File.Exists(appId))
        {
            return appId;
        }

        return null;
    }

    private static bool IsExcludedApp(
        string name,
        string appId)
    {
        string normalizedName = NormalizeForFilter(name);
        string normalizedId = NormalizeForFilter(appId);

        string[] excluded =
        [
            "fileexplorer",
            "windowsexplorer",
            "commandprompt",
            "cmd",
            "powershell",
            "windowsterminal",
            "terminal",
            "run",
            "controlpanel",
            "taskmanager",
            "registryeditor",
            "eventviewer",
            "services",
            "computer management",
            "device manager"
        ];

        return excluded.Any(excludedValue =>
            normalizedName == NormalizeForFilter(excludedValue) ||
            normalizedId.Contains(
                NormalizeForFilter(excludedValue),
                StringComparison.Ordinal));
    }

    private static string NormalizeForFilter(string value)
    {
        return new string(
            value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
    }

    #region Native Methods

    [DllImport(
        "shell32.dll",
        CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(
        string name,
        IntPtr bindingContext,
        out IntPtr pidl,
        uint sfgaoIn,
        IntPtr sfgaoOut);

    [DllImport("shell32.dll")]
    private static extern int SHBindToObject(
        IntPtr parent,
        IntPtr pidl,
        IntPtr bindCtx,
        [In] Guid riid,
        out IntPtr ppv);

    [DllImport("shell32.dll")]
    private static extern IntPtr ILCombine(
        IntPtr pidl1,
        IntPtr pidl2);

    [DllImport("shell32.dll")]
    private static extern void ILFree(
        IntPtr pidl);

    [DllImport("shell32.dll")]
    private static extern IntPtr SHGetFileInfo(
        IntPtr pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(
        IntPtr hIcon);

    [DllImport(
    "shlwapi.dll",
    CharSet = CharSet.Unicode)]
    private static extern int StrRetToBufW(
    ref STRRET pstr,
    IntPtr pidl,
    [Out]
    char[] pszBuf,
    uint cch);
    #endregion

    #region COM Interfaces

    [ComImport]
    [Guid("000214E6-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellFolder
    {
        void ParseDisplayName(
            IntPtr hwnd,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPWStr)]
            string displayName,
            ref uint pchEaten,
            out IntPtr ppidl,
            ref uint attributes);

        int EnumObjects(
            IntPtr hwnd,
            uint grfFlags,
            out IEnumIDList ppenumIDList);

        void BindToObject(
            IntPtr pidl,
            IntPtr pbc,
            ref Guid riid,
            out IntPtr ppv);

        void BindToStorage(
            IntPtr pidl,
            IntPtr pbc,
            ref Guid riid,
            out IntPtr ppv);

        void CompareIDs(
            IntPtr lParam,
            IntPtr pidl1,
            IntPtr pidl2);

        void CreateViewObject(
            IntPtr hwndOwner,
            ref Guid riid,
            out IntPtr ppv);

        void GetAttributesOf(
            uint cidl,
            IntPtr apidl,
            ref uint rgfInOut);

        void GetUIObjectOf(
            IntPtr hwndOwner,
            uint cidl,
            IntPtr apidl,
            ref Guid riid,
            IntPtr rgfReserved,
            out IntPtr ppv);

        int GetDisplayNameOf(
            IntPtr pidl,
            uint uFlags,
            out STRRET lpName);

        void SetNameOf(
            IntPtr hwnd,
            IntPtr pidl,
            [MarshalAs(UnmanagedType.LPWStr)]
            string name,
            uint flags,
            out IntPtr newPidl);
    }

    [ComImport]
    [Guid("000214F2-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IEnumIDList
    {
        [PreserveSig]
        int Next(
            uint celt,
            out IntPtr rgelt,
            out uint pceltFetched);

        void Skip(uint celt);

        void Reset();

        void Clone(out IEnumIDList ppenum);
    }

    #endregion

    #region Native Structures

    [StructLayout(LayoutKind.Explicit)]
    private struct STRRET
    {
        [FieldOffset(0)]
        public uint uType;

        [FieldOffset(8)]
        public IntPtr pOleStr;

        [FieldOffset(8)]
        public IntPtr pStr;
    }

    [StructLayout(LayoutKind.Sequential)]
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

    [Flags]
    private enum SHCONTF : uint
    {
        SHCONTF_FOLDERS = 0x0020,
        SHCONTF_NONFOLDERS = 0x0040
    }

    #endregion
}
