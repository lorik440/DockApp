using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DockApp.Avalonia.Models;

namespace DockApp.Avalonia.Services;

public class AppDiscoveryService
{
    public List<AppInfo> DiscoverApplications()
    {
        var applications = new List<AppInfo>();

        ScanDirectory(
            Environment.GetFolderPath(
                Environment.SpecialFolder.StartMenu),
            applications);

        ScanDirectory(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonStartMenu),
            applications);

        return applications;
    }

    private void ScanDirectory(
        string directory,
        List<AppInfo> applications)
    {
        if (!Directory.Exists(directory))
            return;

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(
                directory,
                "*.lnk",
                SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }

        foreach (var shortcut in files)
        {
            ProcessShortcut(shortcut, applications);
        }

        IEnumerable<string> directories;

        try
        {
            directories = Directory.EnumerateDirectories(
                directory);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }

        foreach (var subDirectory in directories)
        {
            ScanDirectory(
                subDirectory,
                applications);
        }
    }

    private void ProcessShortcut(
        string shortcut,
        List<AppInfo> applications)
    {
        string name =
            Path.GetFileNameWithoutExtension(shortcut);

        if (!IsValidApplication(name))
            return;

        string? executable =
            ShortcutService.Resolve(shortcut);

        if (string.IsNullOrWhiteSpace(executable))
            return;

        if (!File.Exists(executable))
            return;

        if (!executable.EndsWith(
                ".exe",
                StringComparison.OrdinalIgnoreCase))
            return;

        applications.Add(new AppInfo
        {
            Name = name,
            ShortcutPath = shortcut,
            ExecutablePath = executable,
            FirstSeen = DateTime.Now
        });
    }

    private bool IsValidApplication(string name)
    {
        string lower =
            name.ToLowerInvariant();

        string[] excluded =
        {
            "uninstall",
            "uninstaller",
            "readme",
            "help",
            "documentation",
            "website"
        };

        return !excluded.Any(
            x => lower.Contains(x));
    }
}