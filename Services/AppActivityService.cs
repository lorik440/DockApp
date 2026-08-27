using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DockApp.Avalonia.Services;

public sealed class AppActivityService : IDisposable
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    private readonly WinEventDelegate _eventDelegate;
    private readonly int _currentProcessId;
    private IntPtr _hook;
    private string? _lastExecutablePath;

    public AppActivityService()
    {
        _eventDelegate = OnForegroundWindowChanged;
        _currentProcessId = Environment.ProcessId;
    }

    public event Action<ForegroundAppInfo>? ForegroundAppChanged;

    public void Start()
    {
        if (_hook != IntPtr.Zero)
            return;

        _hook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND,
            EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _eventDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
    }

    public void Dispose()
    {
        if (_hook == IntPtr.Zero)
            return;

        UnhookWinEvent(_hook);
        _hook = IntPtr.Zero;
    }

    private void OnForegroundWindowChanged(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime)
    {
        if (hwnd == IntPtr.Zero)
            return;

        GetWindowThreadProcessId(
            hwnd,
            out int processId);

        if (processId == 0 || processId == _currentProcessId)
            return;

        try
        {
            using Process process = Process.GetProcessById(processId);

            string? executablePath = process.MainModule?.FileName;

            if (string.IsNullOrWhiteSpace(executablePath) ||
                IsIgnoredProcess(process.ProcessName, executablePath) ||
                string.Equals(
                    executablePath,
                    _lastExecutablePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _lastExecutablePath = executablePath;

            ForegroundAppChanged?.Invoke(new ForegroundAppInfo(
                executablePath,
                process.ProcessName,
                process.MainModule?.FileVersionInfo.FileDescription,
                process.MainModule?.FileVersionInfo.ProductName));
        }
        catch
        {
            // Some elevated or protected processes do not expose module details.
        }
    }

    private static bool IsIgnoredProcess(
        string processName,
        string executablePath)
    {
        string name = processName.ToLowerInvariant();
        string fileName = System.IO.Path.GetFileName(
            executablePath).ToLowerInvariant();

        return name is "explorer" or "cmd" or "powershell" or "pwsh" or
                   "windowsterminal" or "conhost" or "openconsole" ||
               fileName is "explorer.exe" or "cmd.exe" or "powershell.exe" or
                   "pwsh.exe" or "windowsterminal.exe" or "conhost.exe" or
                   "openconsole.exe";
    }

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(
        IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr hWnd,
        out int lpdwProcessId);
}
