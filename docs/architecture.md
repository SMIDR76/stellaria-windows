# Architecture

The source files carry no comments. This page explains how the pieces fit together; `protocol.md`,
`configuration.md` and `security.md` cover the helper's interface, settings and threat model.

## Processes

| Process | Role |
| --- | --- |
| `zebar.exe` | Hosts one WebView2 window per widget and per monitor (the `pack/` folder). |
| `caelestia-helper.exe` | Native Windows features the pages cannot reach, over a local WebSocket. |
| `CaelestiaSwitcher.exe` | Optional Alt+Tab switcher, themed from `switcher-theme.json`. |

All three start from logon tasks registered by `scripts/install.ps1`, and run without administrator rights.

## The pack

Each widget is a page (`<name>.html`) with its own stylesheet (`<name>.css`) and module (`<name>.js`).
`common.css` holds the design tokens, Caelestia's motion curves, the drawer frame and the shared "welcome card"
styles (`.hero`, `.hero-mark`, `.hero-ornament`, `.hero-chip`, `.tonal-icon`, `.cascade`). The dashboard splits its
styles per tab under `styles/`.

`common.js` only re-exports the modules of `src/core/`:

| Module | Contents |
| --- | --- |
| `store.js` | `settings`, the `keys` of shared state, the `store` over `localStorage` and the `option` helpers. |
| `monitor.js` | Tauri calls, the current monitor, cursor position, `hideOnFullscreen` and the frame `layout`. |
| `theme.js` | Material You schemes, `applyTheme`, `initTheme`, the Windows accent palette and wallpaper analysis. |
| `dom.js` | `element`, `icon`, shapes, the hero elements, `smoothValue`, the ripple and `toast`. |
| `helper.js` | The helper connection, `actions` (named calls to the helper) and `onShared` events. |
| `panels.js` | Edge drawers, the dashboard state and resident popups (launcher, session, settings). |
| `weather.js` | Location and Open-Meteo forecast with caching. |
| `helper-client.js` | The WebSocket client: authentication, reconnection, state topics and requests. |

### Shared state

Widgets are separate pages. They share state through `localStorage`: `store.set` writes a key and wakes the
other windows through the `storage` event, and the current window through a `caelestia-store` event. `store.watch`
reacts to both and keeps a slow poll as a safety net. Results that several widgets need (clipboard history,
brightness, radios, keyboard layouts, window preview) are published with `share` and read with `onShared`.

Only the bar of the primary monitor relays helper state that has side effects (hotkeys, theme publication for the
switcher, Windows colours), so an action runs once even though every monitor has a bar.

### Panels

- **Edge drawers** (dashboard, utilities, sound, toasts, island) are always loaded and hidden. A drawer opens when a
  store key asks for it (`requestDrawer`), follows the cursor while open and closes when it leaves.
- **Resident popups** (launcher, session, settings) are hidden windows on the primary monitor. Opening one writes a
  request; the resident acknowledges it, moves to the requested monitor and shows itself. Without an answer within
  400 ms a temporary instance is started instead.
- Continuous animations only run while `body.open` is set, because hidden WebView2 windows keep rendering.

## The helper

`Program.cs` builds the modules listed at its end and starts them with a shared `ModuleContext` (state hub, request
router, Windows message loop, log, paths, configuration). Each module registers its topics and methods by name; the
list and their parameters are in `protocol.md`.

| Folder | Contents |
| --- | --- |
| `Server/` | WebSocket server, token check, sessions and request routing. |
| `Protocol/` | Message shapes and error codes. |
| `Modules/` | One class per feature (audio, media, notifications, wallpapers, Windows colours…). |
| `Desktop/` | Window and process tracking, taskbar control, app bar repair and the Zebar watchdog. |
| `Interop/` | P/Invoke and COM declarations. |
| `Imaging/` | Window and desktop capture (scaled down by GDI), PNG and JPEG encoding, fingerprints. |
| `Hosting/` | Paths, log, single instance and saved state. |
| `Configuration/` | The configuration schema and store. |

The helper is published with NativeAOT when the Visual Studio C++ build tools are installed, which keeps it near
35 MB of memory; otherwise the installer falls back to a self-contained single file.

### Setup commands

Installers only copy files and call the helper, so every way of installing shares the same logic:

| Command | Effect |
| --- | --- |
| `--install [--zebar <path>] [--switcher <path>]` | Adds the pack's widgets (`pack/startup.json`) to Zebar's start-up list (backup, atomic write, UTF-8 without BOM) and registers the logon tasks from XML, for the current user without elevation. Zebar is found in Program Files, the uninstall entries or Scoop. |
| `--start` | Runs the logon tasks now. |
| `--shutdown` | Ends the tasks, asks the running helper to quit (then stops it), stops Zebar and the switcher and gives the screens their full work area back. |
| `--uninstall` | `--shutdown`, then restores the taskbar and Windows colours from the saved state, removes the tasks, the old Run entries and the Caelestia start-up entries of Zebar. Works even if the helper was killed. |
| `--restore-shell` | `--shutdown`, then shows the taskbar and restores the Windows colours. |

`installer/caelestia.iss` (Inno Setup) and `scripts/install.ps1` call these commands; `scripts/build-release.ps1` and
the Release workflow build the release files (see `releasing.md`).

### Recovery

- A hung Zebar (15 s) or Caelestia docks pushed away from their screen edges after a display change (6 s) restart
  Zebar, at most once every 10 minutes, after giving every screen its full work area back.
- Waking from sleep restarts Zebar 4 seconds later whatever the cooldown: after a resume, WebView2 windows can keep
  running their pages while no longer drawing on screen.
- The session menu and the launcher's power actions close their window before asking Windows to sleep or shut down.
- `--restore-shell` shows the Windows taskbar and puts back the Windows colours saved before the colour sync.
