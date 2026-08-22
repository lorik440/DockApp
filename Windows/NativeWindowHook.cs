using System;
using System.Runtime.InteropServices;

namespace DockApp.Avalonia;

public sealed class NativeWindowHook
{
    private const uint WM_EXITSIZEMOVE = 0x0232;
    private const uint WM_NCDESTROY = 0x0082;

    private const int GWLP_WNDPROC = -4;

    private IntPtr _hwnd;
    private IntPtr _oldWndProc;
    private WndProc? _newWndProc;

    private delegate IntPtr WndProc(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport(
        "user32.dll",
        EntryPoint = "SetWindowLongPtrW",
        SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(
        IntPtr hWnd,
        int nIndex,
        IntPtr newProc);

    [DllImport(
        "user32.dll",
        EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(
        IntPtr lpPrevWndFunc,
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam);

    public event Action? WindowMoveFinished;

    public void Attach(IntPtr hwnd)
    {
        if (_hwnd != IntPtr.Zero)
            return;

        _hwnd = hwnd;

        _newWndProc = WindowProc;

        IntPtr newProc =
            Marshal.GetFunctionPointerForDelegate(_newWndProc);

        _oldWndProc = SetWindowLongPtr(
            _hwnd,
            GWLP_WNDPROC,
            newProc);

        if (_oldWndProc == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to subclass window. Win32 error: " +
                $"{Marshal.GetLastWin32Error()}");
        }
    }

    private IntPtr WindowProc(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (msg == WM_EXITSIZEMOVE)
        {
            WindowMoveFinished?.Invoke();
        }

        IntPtr result = CallWindowProc(
            _oldWndProc,
            hWnd,
            msg,
            wParam,
            lParam);

        return result;
    }
}