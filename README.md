# Stellaria

Stellaria is an unofficial Windows 11 port of [Caelestia](https://github.com/caelestia-dots/shell), the Material You desktop shell
for Hyprland. Windows keep their normal behaviour (snap, maximize, Win+arrows); the shell draws a rounded frame around
them, a vertical bar, and drawers that slide out of the screen edges with Caelestia's expressive motion.

Built on [Zebar](https://github.com/glzr-io/zebar) for the widgets and a small native helper (`stellaria-helper.exe`,
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
- **Reduced motion** support, a **game mode** without animations.
- **Recoverable**: the Windows taskbar comes back if anything stops, and the helper restarts a frozen Zebar.

## The Stellaria app

The setup adds a **Stellaria** entry to the Start menu: an app in the theme's colours to start, stop or repair the
theme, see each part of the installation on its Installation page (Zebar, helper, widgets, start-up tasks) and
install or repair them step by step, open its settings, apply colour presets from the signed community catalogue (and undo them), and read the
project's news. Links such as `stellaria://apply/preset-nord` open it on the matching preset and ask before applying.

## Requirements

- Windows 11 (Windows 10 22H2 may work, untested)
- [Zebar](https://github.com/glzr-io/zebar) 3.3 or later: `winget install -e --id glzr-io.zebar`
- To build the helper: the [.NET 10 SDK](https://dotnet.microsoft.com/download) (`winget install -e --id Microsoft.DotNet.SDK.10`).
  Release archives ship a prebuilt helper instead. With the Visual Studio C++ build tools also installed, the helper
  is compiled natively (NativeAOT) and uses about ten times less memory.
- To build the Stellaria app: [Rust](https://rustup.rs) with the MSVC toolchain. `scripts\install.ps1` skips the app
  when Rust is missing.

How the pieces fit together is described in [docs/architecture.md](docs/architecture.md).

No administrator rights are needed: everything installs in your user profile.

## Install

Download `StellariaSetup-<version>.exe` from the [latest release](https://github.com/SMIDR76/stellaria/releases/latest)
and run it. It installs for your account only, without administrator rights, in English or French. It shows what it
changes on your PC before installing, installs Zebar and WebView2 when they are missing (Zebar installs for the whole
machine, so Windows asks for permission once), and starts the theme.

The setup is not code-signed yet, so Windows may warn about it:

- **SmartScreen** ("Windows protected your PC"): choose **More info**, then **Run anyway**.
- **Smart App Control** (Windows 11) blocks unsigned programs and has no exception: turn it off in Windows Security >
  App & browser control > Smart App Control, or wait for a signed release.

To check a download, compare its SHA-256 with `SHA256SUMS.txt` from the same release, or verify its build provenance
with the GitHub CLI: `gh attestation verify StellariaSetup-<version>.exe -R SMIDR76/stellaria`.

Silent install: `StellariaSetup-<version>.exe /VERYSILENT /SUPPRESSMSGBOXES`. Add `/TASKS="startnow"` to leave the
Alt+Tab switcher out, and `/LANG=fr` or `/LANG=en` to force the language.

Uninstall from **Settings > Apps > Installed apps**, or with the Start menu entry. It stops the theme, gives the
Windows taskbar and colours back (even if the helper was closed by force), removes the logon tasks and Zebar's
start-up entries, and asks whether to keep your settings (silent uninstalls keep them).

### Portable archive and building from source

`Stellaria-<version>-portable.zip` holds the same files and the `scripts\install.ps1` / `scripts\uninstall.ps1`
pair. From a clone, the same scripts build the helper first:

```powershell
git clone https://github.com/SMIDR76/stellaria
cd stellaria
powershell -ExecutionPolicy Bypass -File scripts\install.ps1
```

`scripts\build-release.ps1` builds the helper, the switcher, the portable archive, the setup (with Inno Setup 6.7 or
later) and the checksums into `artifacts\`. Releases are described in [docs/releasing.md](docs/releasing.md).

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

- Start menu > Stellaria > **Rétablir la barre des tâches** stops the theme and shows the Windows taskbar.
- Start menu > Stellaria > **Relancer** starts it again.
- Logs: `%LOCALAPPDATA%\Stellaria\logs\helper.log`.

## Privacy and security

The helper only listens on `127.0.0.1`, requires a per-session token and exposes a closed list of methods; widgets
cannot run programs. Network use is limited to weather (Open-Meteo, plus ipinfo.io when no location is set) and
lyrics (LRCLIB), both optional, and to the Stellaria app checking GitHub for a signed update and reading its release
notes. Nothing else is sent anywhere: this program will not transfer any information to other networked systems
unless specifically requested by the user or the person installing or operating it. Notification contents are read from your own Windows history, kept in memory and never
logged. See [docs/security.md](docs/security.md), [docs/protocol.md](docs/protocol.md) and
[docs/configuration.md](docs/configuration.md).

## Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by
[SignPath Foundation](https://signpath.org).

- Committers and approvers: [the repository owner](https://github.com/SMIDR76).
- Reviewers: every change from outside the team goes through a pull request reviewed by a committer.
- Every release is built from this repository by the [Release workflow](.github/workflows/release.yml) on GitHub-hosted
  runners and signed only after a manual approval. Only files built from this repository are signed: the helper, the
  Stellaria app, the Alt+Tab switcher and the setup. Third-party files (Zebar, which the setup installs separately,
  and `e_sqlite3.dll`) keep their own publishers' signatures.
- Privacy: see [Privacy and security](#privacy-and-security). Third-party services used on request: Open-Meteo,
  ipinfo.io, LRCLIB and GitHub, under their own privacy policies.

The setup details are in [docs/code-signing.md](docs/code-signing.md).

## Credits

[Caelestia](https://github.com/caelestia-dots/shell) by soramanew and contributors (design, colours, motion), Zebar
and GlazeWM by glzr-io, Material Color Utilities by Google. Ideas from DankMaterialShell, Noctalia, end-4's dots,
YASB and FluentFlyout. Licensed under GPL-3.0; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
