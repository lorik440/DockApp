using System;
using Avalonia;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DockApp.Avalonia.Services;
using System.Collections.ObjectModel;
using DockApp.Avalonia.Models;
using System.Text;

namespace DockApp.Avalonia;

public partial class MainWindow : Window
{
    
    private readonly NativeWindowHook _windowHook;
    private readonly AppService _appService ;
    private readonly UsageService _usageService ;
    private readonly StartupService _startupService;
    private readonly WindowService _windowService;
    private readonly AppActivityService _activityService;
    private readonly DispatcherTimer _rankingRefreshTimer;
    private readonly DispatcherTimer _settingsFlushTimer;
    private readonly object _appsSync = new();
    private readonly List<AppInfo> _allApps = new();
    private string? _lastRecordedAppId;
    private DateTime _lastRecordedAtUtc;
    private IntPtr _hwnd;

    public ObservableCollection<AppInfo> Apps { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;

        _appService = new AppService();
        _usageService = new UsageService();
        _startupService = new StartupService();
        _windowService = new WindowService();
        _activityService = new AppActivityService();
        _activityService.ForegroundAppChanged += OnForegroundAppChanged;
        _rankingRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _rankingRefreshTimer.Tick += OnRankingRefreshTimerTick;
        _settingsFlushTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(45)
        };
        _settingsFlushTimer.Tick += OnSettingsFlushTimerTick;

        RestoreSavedPosition();

        _windowHook = new NativeWindowHook();

        Opened += OnWindowOpened;
        Closed += OnWindowClosed;

        Width = 200;
        Height = 112;

        CanResize = false;

        WindowDecorations = WindowDecorations.None;

        _startupService.EnableStartup();

    }

    private void OnWindowOpened(
        object? sender,
        EventArgs e)
    {

        var platformHandle = TryGetPlatformHandle();

        if (platformHandle == null)
            return;

        _hwnd = platformHandle.Handle;

        //movement hook to snap to grid after moving
        _windowHook.WindowMoveFinished += SnapToGrid;
        _windowHook.Attach(_hwnd);

        _windowService.SendToBottom(_hwnd);

        Position = DesktopGrid.ClampToVisibleWorkArea(
            this,
            Position);

        _ = LoadAppsAsync();
        _activityService.Start();
    }

    private async Task LoadAppsAsync()
    {
        var apps = await Task.Run(_appService.GetApps);

        lock (_appsSync)
        {
            _allApps.Clear();

            foreach (var app in apps)
            {
                _usageService.ApplyUsage(app);
                _allApps.Add(app);
            }
        }

        RefreshVisibleApps();
        _ = LoadVisibleIconsAsync();
    }

    private void RefreshVisibleApps()
    {
        List<AppInfo> rankedApps = GetRankedApps();

        lock (_appsSync)
        {
            var visibleIds = rankedApps
                .Select(app => app.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (AppInfo app in _allApps)
            {
                if (!visibleIds.Contains(app.Id))
                {
                    app.Icon?.Dispose();
                    app.Icon = null;
                }
            }
        }

        Apps.Clear();

        foreach (var app in rankedApps)
        {
            Apps.Add(app);
        }
    }

    private async Task LoadVisibleIconsAsync()
    {
        List<AppInfo> visibleApps = GetRankedApps()
            .Where(app => app.Icon is null)
            .ToList();

        if (visibleApps.Count == 0)
            return;

        await Task.Run(() => _appService.LoadIcons(visibleApps));

        RefreshVisibleApps();
    }

    private List<AppInfo> GetRankedApps()
    {
        lock (_appsSync)
        {
            return _allApps
                .OrderByDescending(app => app.LaunchCount)
                .ThenByDescending(app => app.LastUsed)
                .ThenBy(app => app.Name)
                .Take(6)
                .ToList();
        }
    }

    private void RestoreSavedPosition()
    {
        PixelPoint? savedPosition = _usageService.GetSavedPosition();

        Position = savedPosition ?? new PixelPoint(80, 80);
    }
    


    private void OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (e.Source is Button)
            return;

        if (e.GetCurrentPoint(this)
            .Properties
            .IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnAppButtonClick(
        object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AppInfo app })
            return;

        RecordAppUse(app);
        _appService.LaunchApp(app);

        ScheduleRankingRefresh();

        _windowService.SendToBottom(_hwnd);
    }

    private void OnWindowClosed(
        object? sender,
        EventArgs e)
    {
        _activityService.Dispose();
        FlushSettingsNow();
    }

    private void OnForegroundAppChanged(ForegroundAppInfo foregroundApp)
    {
        AppInfo? app = FindMatchingInstalledApp(foregroundApp);

        if (app is null)
            return;

        if (!RecordAppUse(app))
            return;

        global::Avalonia.Threading.Dispatcher.UIThread.Post(
            ScheduleRankingRefresh);
    }

    private AppInfo? FindMatchingInstalledApp(ForegroundAppInfo foregroundApp)
    {
        string executableName = System.IO.Path.GetFileNameWithoutExtension(
            foregroundApp.ExecutablePath);

        string normalizedProcess = NormalizeForMatch(executableName);
        string normalizedDescription = NormalizeForMatch(
            foregroundApp.FileDescription ?? "");
        string normalizedProduct = NormalizeForMatch(
            foregroundApp.ProductName ?? "");

        lock (_appsSync)
        {
            return _allApps
                .Select(app => new
                {
                    App = app,
                    Score = GetMatchScore(
                        app,
                        foregroundApp.ExecutablePath,
                        normalizedProcess,
                        normalizedDescription,
                        normalizedProduct)
                })
                .Where(match => match.Score > 0)
                .OrderByDescending(match => match.Score)
                .Select(match => match.App)
                .FirstOrDefault();
        }
    }

    private bool RecordAppUse(AppInfo app)
    {
        DateTime now = DateTime.UtcNow;

        if (string.Equals(
                _lastRecordedAppId,
                app.Id,
                StringComparison.OrdinalIgnoreCase) &&
            now - _lastRecordedAtUtc < TimeSpan.FromSeconds(2))
        {
            return false;
        }

        _usageService.RecordForegroundUse(app);
        ScheduleSettingsFlush();
        _lastRecordedAppId = app.Id;
        _lastRecordedAtUtc = now;

        return true;
    }

    private void ScheduleRankingRefresh()
    {
        RefreshVisibleApps();
        _ = LoadVisibleIconsAsync();

        _rankingRefreshTimer.Stop();
        _rankingRefreshTimer.Start();
    }

    private void ScheduleSettingsFlush()
    {
        _settingsFlushTimer.Stop();
        _settingsFlushTimer.Start();
    }

    private void OnRankingRefreshTimerTick(
        object? sender,
        EventArgs e)
    {
        _rankingRefreshTimer.Stop();
        RefreshVisibleApps();
        _ = LoadVisibleIconsAsync();
    }

    private void OnSettingsFlushTimerTick(
        object? sender,
        EventArgs e)
    {
        _settingsFlushTimer.Stop();
        _usageService.Flush();
    }

    private void FlushSettingsNow()
    {
        _settingsFlushTimer.Stop();
        _usageService.Flush();
    }

    private static int GetMatchScore(
        AppInfo app,
        string executablePath,
        string normalizedProcess,
        string normalizedDescription,
        string normalizedProduct)
    {
        if (!string.IsNullOrWhiteSpace(app.ExecutablePath) &&
            string.Equals(
                app.ExecutablePath,
                executablePath,
                StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        string normalizedName = NormalizeForMatch(app.Name);
        string normalizedId = NormalizeForMatch(app.Id);

        if (normalizedName == normalizedProduct ||
            normalizedName == normalizedDescription ||
            normalizedName == normalizedProcess)
        {
            return 80;
        }

        if (ContainsUsefulMatch(normalizedId, normalizedProcess))
            return 60;

        if (ContainsUsefulMatch(normalizedName, normalizedProcess) ||
            ContainsUsefulMatch(normalizedProcess, normalizedName))
        {
            return 40;
        }

        return 0;
    }


    private static bool ContainsUsefulMatch(
        string value,
        string candidate)
    {
        return candidate.Length >= 4 &&
               value.Contains(candidate, StringComparison.Ordinal);
    }

    private static string NormalizeForMatch(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private async void SnapToGrid()
    {
        var start = Position;

        var target = DesktopGrid.SnapToVisibleWorkArea(
            this,
            start);

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
        _usageService.SavePosition(Position);
        _windowService.SendToBottom(_hwnd);
    }
}
