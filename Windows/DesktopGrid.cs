using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace DockApp.Avalonia;

public static class DesktopGrid
{
    // Windows desktop icon spacing at 100% DPI
    private const int BaseCellWidth = 38;
    private const int BaseCellHeight = 39;

    public static int GetCellWidth(Window window)
    {
        return (int)Math.Round(
            BaseCellWidth * GetScale(window));
    }

    public static int GetCellHeight(Window window)
    {
        return (int)Math.Round(
            BaseCellHeight * GetScale(window));
    }

    private static double GetScale(Window window)
    {
        var screen =
            window.Screens.ScreenFromWindow(window) ??
            window.Screens.Primary;

        if (screen == null)
            return 1.0;

        // Avalonia screen scaling.
        return screen.Scaling;
    }

    public static PixelPoint SnapToVisibleWorkArea(
        Window window,
        PixelPoint position)
    {
        PixelRect workArea = GetWorkArea(
            window,
            position);

        int windowWidth = Math.Max(
            1,
            (int)Math.Ceiling(window.Bounds.Width));

        int windowHeight = Math.Max(
            1,
            (int)Math.Ceiling(window.Bounds.Height));

        int minX = workArea.X;
        int minY = workArea.Y;

        int maxX = Math.Max(
            minX,
            workArea.Right - windowWidth);

        int maxY = Math.Max(
            minY,
            workArea.Bottom - windowHeight);

        int cellWidth = GetCellWidth(window);
        int cellHeight = GetCellHeight(window);

        int snappedX = SnapInsideRange(
            position.X,
            cellWidth,
            minX,
            maxX);

        int snappedY = SnapInsideRange(
            position.Y,
            cellHeight,
            minY,
            maxY);

        return new PixelPoint(
            snappedX,
            snappedY);
    }

    public static PixelPoint ClampToVisibleWorkArea(
        Window window,
        PixelPoint position)
    {
        PixelRect workArea = GetWorkArea(
            window,
            position);

        int windowWidth = Math.Max(
            1,
            (int)Math.Ceiling(window.Bounds.Width));

        int windowHeight = Math.Max(
            1,
            (int)Math.Ceiling(window.Bounds.Height));

        int maxX = Math.Max(
            workArea.X,
            workArea.Right - windowWidth);

        int maxY = Math.Max(
            workArea.Y,
            workArea.Bottom - windowHeight);

        return new PixelPoint(
            Math.Clamp(position.X, workArea.X, maxX),
            Math.Clamp(position.Y, workArea.Y, maxY));
    }

    private static int SnapInsideRange(
        int value,
        int cellSize,
        int min,
        int max)
    {
        int snapped =
            (int)Math.Round(
                (double)(value - min) / cellSize)
            * cellSize
            + min;

        return Math.Clamp(
            snapped,
            min,
            max);
    }

    private static PixelRect GetWorkArea(
        Window window,
        PixelPoint position)
    {
        global::Avalonia.Platform.Screen? screen =
            window.Screens.ScreenFromWindow(window) ??
            window.Screens.ScreenFromPoint(position) ??
            window.Screens.Primary;

        return screen?.WorkingArea ?? new PixelRect(
            0,
            0,
            1920,
            1080);
    }
}