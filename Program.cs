using System;
using Avalonia;

namespace DockApp.Avalonia;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration & platform options
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                WinUICompositionBackdropCornerRadius = 25f
            })
            .WithInterFont()
            .LogToTrace();
}