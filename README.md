# Caelestia for Windows

An unofficial Windows 11 port of [Caelestia](https://github.com/caelestia-dots/shell), the Material You desktop shell
for Hyprland. Windows keep their normal behaviour (snap, maximize, Win+arrows); the shell draws a rounded frame around
them, a vertical bar, and drawers that slide out of the screen edges with Caelestia's expressive motion.

Built on [Zebar](https://github.com/glzr-io/zebar) for the widgets and a small native helper (`caelestia-helper.exe`,
.NET 10) for everything a web page cannot do.

## Features

- **Bar**: virtual desktops with an elastic indicator, open windows, tray, weather, clock, keyboard layout, sound,
  network and a notification bell. App icons can be tinted with the theme colour.
- **Material You colours** taken from the wallpaper, nine variants and fourteen fixed schemes, light and dark modes.
- **Dashboard** (hover the top edge or Win+Alt+A): calendar, weather, media with synced lyrics, live CPU, GPU,
  memory, storage and network gauges.
- **Launcher** (tap Win or Alt+Space): apps, open windows, calculator, emojis, clipboard history, wallpapers, colour
  variants and schemes, and a shortcut cheat sheet (Win+Alt+K).
- **Notification centre** with the Windows notification history grouped by app.
- **Dynamic island** that drops from the top of the frame for a new track (with shuffle and repeat) or a notification.
- **Sound panel** with per-application volume, **utilities** (radios, game mode, keep awake, captures, Pomodoro timer),
  **brightness** over DDC/CI, **Alt+Tab switcher**, **hot corner** for Task View and an **idle screen**.
- **Optional acrylic** blur behind the bar, **reduced motion** support, a **game mode** without animations.
- **Recoverable**: the Windows taskbar comes back if anything stops, and the helper restarts a frozen Zebar.

## Requirements

- Windows 11 (Windows 10 22H2 may work, untested)
- [Zebar](https://github.com/glzr-io/zebar) 3.3 or later: `winget install -e --id glzr-io.zebar`
- To build the helper: the [.NET 10 SDK](https://dotnet.microsoft.com/download) (`winget install -e --id Microsoft.DotNet.SDK.10`).
  Release archives ship a prebuilt helper instead. With the Visual Studio C++ build tools also installed, the helper
  is compiled natively (NativeAOT) and uses about ten times less memory.

How the pieces fit together is described in [docs/architecture.md](docs/architecture.md).

No administrator rights are needed: everything installs in your user profile.

## Install

```powershell
git clone https://github.com/SMIDR76/caelestia-windows
cd caelestia-windows
powershell -ExecutionPolicy Bypass -File scripts\install.ps1
```

The installer copies the pack, builds the helper, registers three logon tasks (helper, switcher, Zebar) and adds a
**Caelestia** folder to the Start menu. On first launch the settings open on a short welcome page.

To remove everything and get the Windows taskbar back:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\uninstall.ps1
```

## Shortcuts

| Keys | Action |
| --- | --- |
| Win (tap) or Alt+Space | Launcher |
| Win+Alt+A | Dashboard |
| Win+Alt+U | Utilities |
| Win+Alt+P | Session menu |
| Win+Alt+V | Clipboard history |
| Win+Alt+. | Emojis |
| Win+Alt+S | Theme settings |
| Win+Alt+K | Shortcut cheat sheet |
| Alt+E | Wallpapers |
| Alt+Tab | Window switcher |

Pointer gestures: top edge for the dashboard, middle of the right edge for sound and brightness, bottom-right corner
for the utilities, top-right corner for Task View.

## If something goes wrong

- Start menu > Caelestia > **Rétablir la barre des tâches** stops the theme and shows the Windows taskbar.
- Start menu > Caelestia > **Relancer** starts it again.
- Logs: `%LOCALAPPDATA%\Caelestia\logs\helper.log`.

## Privacy and security

The helper only listens on `127.0.0.1`, requires a per-session token and exposes a closed list of methods; widgets
cannot run programs. Network use is limited to weather (Open-Meteo, plus ipinfo.io when no location is set) and
lyrics (LRCLIB), both optional. Notification contents are read from your own Windows history, kept in memory and never
logged. See [docs/security.md](docs/security.md), [docs/protocol.md](docs/protocol.md) and
[docs/configuration.md](docs/configuration.md).

## Credits

[Caelestia](https://github.com/caelestia-dots/shell) by soramanew and contributors (design, colours, motion), Zebar
and GlazeWM by glzr-io, Material Color Utilities by Google. Ideas from DankMaterialShell, Noctalia, end-4's dots,
YASB and FluentFlyout. Licensed under GPL-3.0; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
