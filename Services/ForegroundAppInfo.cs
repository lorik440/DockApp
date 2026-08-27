namespace DockApp.Avalonia.Services;

public sealed record ForegroundAppInfo(
    string ExecutablePath,
    string ProcessName,
    string? FileDescription,
    string? ProductName);
