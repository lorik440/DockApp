using System;
using Avalonia;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using DockApp.Avalonia.Services;

namespace DockApp.Avalonia;

public partial class MainWindow : Window
{
    private readonly NativeWindowHook _windowHook;

    public MainWindow()
    {
        InitializeComponent();

        _windowHook = new NativeWindowHook();

        Opened += OnWindowOpened;

        Width = 250;
        Height = 125;

        CanResize = false;

        WindowDecorations = WindowDecorations.None;

        var discovery = new AppDiscoveryService();

        var apps = discovery.DiscoverApplications();

        foreach (var app in apps)
        {
            IntPtr icon =
                IconService.GetIconHandle(
                    app.ExecutablePath);

            Console.WriteLine(
                $"{app.Name} -> {app.ExecutablePath}");

            Console.WriteLine(
                $"Icon handle: {icon}");

            IconService.DestroyIconHandle(icon);
        }

    }

    private void OnWindowOpened(
        object? sender,
        EventArgs e)
    {
        var platformHandle = TryGetPlatformHandle();

        if (platformHandle == null)
            return;

        _windowHook.WindowMoveFinished += SnapToGrid;

        _windowHook.Attach(platformHandle.Handle);
    }

    private void OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this)
            .Properties
            .IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private async void SnapToGrid()
    {
        var start = Position;

        int targetX = DesktopGrid.SnapX(start.X);
        int targetY = DesktopGrid.SnapY(start.Y);

        var target = new PixelPoint(targetX, targetY);

        // Already at the correct position
        if (start == target)
            return;

        const int duration = 120;
        const int frames = 8;

        for (int i = 1; i <= frames; i++)
        {
            double t = (double)i / frames;

            // Ease-out curve
            double eased = 1 - Math.Pow(1 - t, 3);

            int x = (int)Math.Round(
                start.X + (target.X - start.X) * eased);

            int y = (int)Math.Round(
                start.Y + (target.Y - start.Y) * eased);

            Position = new PixelPoint(x, y);

            await Task.Delay(duration / frames);
        }

        Position = target;
    }
}