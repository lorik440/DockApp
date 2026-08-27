using System;
using System.Runtime.InteropServices;

namespace DockApp.Avalonia.Services;

public class WindowService
{
    private static readonly IntPtr HwndBottom = new(1);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    public void SendToBottom(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return;

        SetWindowPos(
            hwnd,
            HwndBottom,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);
}
