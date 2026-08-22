using System;
using Avalonia.Media.Imaging;

namespace DockApp.Avalonia.Models;

public class AppInfo
{
    public string Name { get; set; } = "";

    public string ShortcutPath { get; set; } = "";

    public string ExecutablePath { get; set; }

    public Bitmap? IconPath { get; set; } = null;

    public int LaunchCount { get; set; }

    public DateTime LastUsed { get; set; }

    public DateTime FirstSeen { get; set; }
}