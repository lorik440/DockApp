using System;
using System.Runtime.InteropServices;
using Avalonia;

namespace DockApp.Avalonia;

public static class DesktopGrid
{
    private const int SM_CXICONSPACING = 38;
    private const int SM_CYICONSPACING = 39;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    public static int CellWidth =>
        GetSystemMetrics(SM_CXICONSPACING);

    public static int CellHeight =>
        GetSystemMetrics(SM_CYICONSPACING);

    public static int DockWidth =>
        CellWidth * 3;

    public static int DockHeight =>
        CellHeight * 2;

    public static int SnapX(int x)
    {
        return (int)Math.Round(
            (double)x / CellWidth
        ) * CellWidth;
    }

    public static int SnapY(int y)
    {
        return (int)Math.Round(
            (double)y / CellHeight
        ) * CellHeight;
    }
}