
# DockApp

A lightweight, adaptive Windows desktop launcher built with C# and Avalonia.

DockApp provides a small, unobtrusive launcher that learns which applications you use most and dynamically keeps your most-used apps available for quick access.

The project is designed to feel more like a native Windows desktop feature than a traditional standalone application.

## Features

- **Dynamic app ranking**
  - Tracks application usage.
  - Apps are ranked by launch count and most recent use.
  - The six highest-ranked applications are displayed.

- **Windows application discovery**
  - Uses the Windows `AppsFolder` / Shell APIs to discover installed applications.
  - Supports launching applications through Windows Shell.

- **Foreground activity tracking**
  - Monitors the application currently being used.
  - Updates usage information when applications become active.

- **Persistent usage data**
  - Application usage statistics are saved locally.
  - Usage data is restored when DockApp starts again.

- **Persistent window position**
  - Remembers where the DockApp window was placed.
  - Restores the saved position on startup.

- **Desktop-aware positioning**
  - The window can be moved around the desktop.
  - Movement is snapped to a desktop-style grid.
  - The window is clamped to the visible Windows work area so it does not move outside the screen.

- **Windows startup integration**
  - DockApp registers itself to start with Windows.

- **Minimal desktop presence**
  - Borderless window.
  - Hidden from the taskbar.
  - Transparent background with an acrylic-style appearance.
  - Designed to remain unobtrusive while sitting directly on the desktop.

## Technology

- **C#**
- **.NET 10**
- **Avalonia UI 12.1.1**
- **XAML**
- **Windows Shell APIs**
- **Windows native APIs**
- **System.Text.Json**

The project targets Windows:

```text
net10.0-windows