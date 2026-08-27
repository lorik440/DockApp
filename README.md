# DockApp

A lightweight, adaptive Windows desktop launcher built with C# and Avalonia.

DockApp provides a small, unobtrusive launcher that learns which applications you use most and dynamically keeps your most-used apps available for quick access.

The project is designed to feel more like a **native Windows desktop feature** than a traditional standalone application. It focuses on fast startup, persistent state, Windows application discovery, and integration with Windows desktop behavior.

## Features

* **Dynamic app ranking**

  * Tracks application usage.
  * Apps are ranked using launch frequency and recent activity.
  * The six highest-ranked applications are displayed.

* **Windows application discovery**

  * Uses the Windows `AppsFolder` / Shell APIs to discover installed applications.
  * Supports launching applications through Windows Shell.

* **Foreground activity tracking**

  * Monitors the application currently being used.
  * Updates usage information when applications become active.

* **Persistent usage data**

  * Application usage statistics are saved locally.
  * Usage data is restored when DockApp starts again.

* **Persistent launcher state**

  * Remembers the applications currently displayed.
  * Preserves the ordering of the launcher across restarts.
  * Restores the saved state when DockApp starts.

* **Persistent window position**

  * Remembers where the DockApp window was placed.
  * Restores the saved position on startup.

* **Fast startup**

  * Designed to display the launcher using previously saved state as early as possible.
  * Expensive operations such as application discovery and data refresh can be performed after the initial UI is displayed.
  * Cached application information and icons can be used to reduce startup time.

* **Desktop-aware positioning**

  * The window can be moved around the desktop.
  * Movement is snapped to a desktop-style grid.
  * The window is clamped to the visible Windows work area so it does not move outside the screen.

* **Windows startup integration**

  * DockApp can register itself to start with Windows.
  * Startup behavior is designed to make DockApp available shortly after the Windows desktop becomes available.

* **Minimal desktop presence**

  * Borderless window.
  * Hidden from the taskbar.
  * Transparent background with an acrylic-style appearance.
  * Designed to remain unobtrusive while sitting directly on the desktop.
  * Does not require a desktop shortcut for normal use.

## Architecture

DockApp is built around a separation between the launcher UI, application logic, and Windows-specific functionality.

```text
DockApp
│
├── Avalonia UI
│   └── Desktop launcher interface
│
├── Core application logic
│   ├── Application discovery
│   ├── Usage tracking
│   ├── App ranking
│   └── Persistent state
│
└── Windows integration
    ├── Windows Shell APIs
    ├── Foreground activity tracking
    ├── Window management
    └── Windows startup integration
```

This structure allows the desktop UI and application logic to remain independent from Windows-specific implementation details.

The long-term goal is to deepen Windows integration while keeping the core DockApp functionality independent of the UI technology.

## Startup Design

DockApp is designed around a **fast-first startup model**.

Instead of waiting for every Windows application to be discovered before displaying the launcher, DockApp can restore its previously saved state first:

```text
Windows login
      │
      ▼
DockApp starts
      │
      ▼
Load saved launcher state
      │
      ▼
Display DockApp
      │
      ├───────────────┐
      ▼               ▼
Load cached data    Background refresh
                    │
                    ├── Discover applications
                    ├── Update usage data
                    ├── Recalculate ranking
                    └── Refresh cached icons
```

This allows the launcher to become visible quickly while background operations continue without blocking the initial UI.

## Technology

* **C#**
* **.NET 10**
* **Avalonia UI 12.1.1**
* **XAML**
* **Windows Shell APIs**
* **Windows native APIs**
* **System.Text.Json**

The project targets Windows:

```text
net10.0-windows
```

## Windows Integration

DockApp is intended to behave as closely as practical to a Windows desktop feature while remaining a desktop application.

Current and planned Windows integration includes:

* Windows application discovery through Shell APIs
* Windows foreground activity tracking
* Desktop-aware window positioning
* Windows startup integration
* Persistent application and window state
* Windows-native packaging and application identity as a future deployment direction

The project may eventually explore deeper Windows integration such as **Windows App SDK** capabilities and a possible **Windows Widgets** companion surface.

Widgets are considered an optional integration point rather than a replacement for the main DockApp desktop launcher.

## Distribution

DockApp is distributed for Windows as a self-contained `win-x64` application.

Each release provides:

```text
DockApp
│
├── DockApp-Setup-vX.X.X.exe
│   └── Standard Windows installation
│
└── DockApp-win-x64.zip
    └── Portable version
```

The installer is intended for normal users, while the ZIP package provides a portable alternative.

## Project Status

DockApp is an actively developed project.

The current focus is on building a reliable and lightweight desktop launcher with:

* Fast startup
* Accurate Windows application discovery
* Dynamic usage-based ranking
* Persistent state
* Reliable desktop positioning
* Minimal visual presence
* Windows integration

Future development will focus on improving Windows integration and startup behavior without requiring a fundamental rewrite of the existing application architecture.
