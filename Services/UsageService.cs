using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia;
using DockApp.Avalonia.Models;

namespace DockApp.Avalonia.Services;

public class UsageService
{
    private readonly object _syncRoot = new();
    private readonly string _settingsPath;
    private DockSettings _settings;
    private bool _hasUnsavedChanges;

    public UsageService()
    {
        string appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        string settingsFolder = Path.Combine(
            appData,
            "DockApp.Avalonia");

        Directory.CreateDirectory(settingsFolder);

        _settingsPath = Path.Combine(
            settingsFolder,
            "settings.json");

        _settings = Load();
    }

    public void ApplyUsage(AppInfo app)
    {
        if (string.IsNullOrWhiteSpace(app.Id))
            return;

        lock (_syncRoot)
        {
            if (_settings.Apps.TryGetValue(app.Id, out AppUsage? usage))
            {
                app.LaunchCount = usage.LaunchCount;
                app.LastUsed = usage.LastUsed;
            }
        }
    }

    public void RecordLaunch(AppInfo app)
    {
        if (string.IsNullOrWhiteSpace(app.Id))
            return;

        lock (_syncRoot)
        {
            if (!_settings.Apps.TryGetValue(app.Id, out AppUsage? usage))
            {
                usage = new AppUsage();
                _settings.Apps[app.Id] = usage;
            }

            usage.Name = app.Name;
            usage.LaunchCount++;
            usage.LastUsed = DateTime.UtcNow;

            app.LaunchCount = usage.LaunchCount;
            app.LastUsed = usage.LastUsed;

            _hasUnsavedChanges = true;
        }
    }

    public void RecordForegroundUse(AppInfo app)
    {
        RecordLaunch(app);
    }

    public PixelPoint? GetSavedPosition()
    {
        lock (_syncRoot)
        {
            if (_settings.Window.X is null || _settings.Window.Y is null)
                return null;

            return new PixelPoint(
                _settings.Window.X.Value,
                _settings.Window.Y.Value);
        }
    }

    public void SavePosition(PixelPoint position)
    {
        lock (_syncRoot)
        {
            if (_settings.Window.X == position.X &&
                _settings.Window.Y == position.Y)
            {
                return;
            }

            _settings.Window.X = position.X;
            _settings.Window.Y = position.Y;

            _hasUnsavedChanges = true;
        }
    }

    public void Flush()
    {
        lock (_syncRoot)
        {
            if (!_hasUnsavedChanges)
                return;

            Save();
            _hasUnsavedChanges = false;
        }
    }

    private DockSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new DockSettings();

            string json = File.ReadAllText(_settingsPath);

            return JsonSerializer.Deserialize<DockSettings>(json) ??
                   new DockSettings();
        }
        catch
        {
            return new DockSettings();
        }
    }

    private void Save()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(
            _settings,
            options);

        File.WriteAllText(
            _settingsPath,
            json);
    }

    private sealed class DockSettings
    {
        public Dictionary<string, AppUsage> Apps { get; set; } = new();

        public WindowSettings Window { get; set; } = new();
    }

    private sealed class AppUsage
    {
        public string Name { get; set; } = "";

        public int LaunchCount { get; set; }

        public DateTime LastUsed { get; set; }
    }

    private sealed class WindowSettings
    {
        public int? X { get; set; }

        public int? Y { get; set; }
    }
}
