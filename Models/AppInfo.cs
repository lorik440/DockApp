using System;
using Avalonia.Media.Imaging;

namespace DockApp.Avalonia.Models;

public class AppInfo
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public string? ExecutablePath { get; set; }

    public Bitmap? Icon { get; set; } = null;

    public int LaunchCount { get; set; }

    public DateTime LastUsed { get; set; }

}
