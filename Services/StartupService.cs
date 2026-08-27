namespace DockApp.Avalonia.Services;

using System;
using System.Diagnostics;
using Microsoft.Win32;

public class StartupService
{
    private const string RunKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string AppName = "DockApp.Avalonia";

    public void EnableStartup()
    {
        string? exePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(exePath))
            return;

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            RunKeyPath,
            writable: true);

        key?.SetValue(
            AppName,
            $"\"{exePath}\"",
            RegistryValueKind.String);
    }

    public void DisableStartup()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            RunKeyPath,
            writable: true);

        key?.DeleteValue(
            AppName,
            throwOnMissingValue: false);
    }

    public bool IsStartupEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            RunKeyPath,
            writable: false);

        string? value = key?.GetValue(AppName) as string;

        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(
                   Process.GetCurrentProcess().ProcessName,
                   StringComparison.OrdinalIgnoreCase);
    }
}
