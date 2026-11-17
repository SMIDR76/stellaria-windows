# Architecture

The source files carry no comments. This page explains how the pieces fit together; `protocol.md`,
`configuration.md` and `security.md` cover the helper's interface, settings and threat model.

## Processes

| Process | Role |
| --- | --- |
| `zebar.exe` | Hosts one WebView2 window per widget and per monitor (the `pack/` folder). |
| `caelestia-helper.exe` | Native Windows features the pages cannot reach, over a local WebSocket. |
| `CaelestiaSwitcher.exe` | Optional Alt+Tab switcher, themed from `switcher-theme.json`. |
| `caelestia-launcher.exe` | The Caelestia app (Tauri 2): start, stop and repair the theme, colour presets from the community catalogue, news. Opened on demand, never at logon. |

The first three start from logon tasks registered by `scripts/install.ps1`, and every process runs without
administrator rights.

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

## The Caelestia app

`launcher/` holds a thin Rust shell and a plain HTML, CSS and JavaScript page (`launcher/ui/`), built with `cargo`
alone by `launcher/build.ps1`, which first copies the pack's shared styles, fonts and colour schemes into
`launcher/dist/`. The app holds no theme logic: everything goes through the helper, except the Installation page,
which must work while the helper is stopped or missing.

- **Connection**: the page reads `%LOCALAPPDATA%\Caelestia\launcher.json` (port and token, written by the helper)
  through the `runtime` command, then talks to the helper over the same WebSocket as the widgets, from the origin
  `http://tauri.localhost`, with the `launcher` scope. The helper's answers to `launcher.status`, `launcher.theme` and
  `catalog.*` fill the pages.
- **Rust commands**: `runtime`, `helper` (runs `caelestia-helper.exe` with `--start`, `--restore-shell`,
  `--install`, `--uninstall` or `--shutdown`, nothing else), `installation`, `install_zebar`, `open_folder` (the
  Caelestia, widgets and logs folders only), `open_external` (https links and the Windows apps settings page only),
  `initial_link`, `log_tail` (the last lines of the helper log), `wallpaper` (the Windows wallpaper file as raw
  bytes, shown through a `blob:` URL), `tray_labels`, `installed`, `release_notes`, `check_update`,
  `download_update` and `install_update`.
- **Pages**: Home (theme card, "Your setup" with the wallpaper, palette and active widgets, current look,
  suggestions, news), Installation, Discover, a detail page per preset (a mock-up of the bar and dashboard drawn in
  the preset's colours, dark and light when the scheme has both, facts, apply, copy link), Library (with the last
  five presets applied, kept in the page's storage), News, Help (shortcut list with search, and troubleshooting:
  restore the taskbar, repair, copy a report with versions, component state and the log tail, log viewer) and
  Settings (language, sounds and volume, notification area, replay the welcome). Ctrl+K, or the logo, opens a
  search over pages, presets and actions.
- **First run and updates**: a three-step welcome (language, look with live preview, key shortcuts) opens until it
  has been finished or closed once. After an update, the app compares the installed version with the last one it saw
  and shows the release notes, read from the GitHub release.
- **Notification area**: an icon next to the clock opens the app (left click) or a menu (open, start or stop the
  theme, update when one is available, quit). Closing the window plays the close sound and hides it there, unless
  the setting is off; reopening it plays the start sound again. The app checks for updates every six hours.
- **Motion**: the logo morphs into a different rounded shape on each page, pages slide in when they change (not on
  every refresh), hovering a preset card repaints the app in its colours after 220 ms, finished install steps pop in,
  and the window fades in and out. Everything is turned off when Windows asks for reduced motion.
- **Installation page**: `installation` reads the state of each part without the helper: Zebar (the same places as
  the helper's `ZebarLocator`, version from its uninstall entry), the helper's file version, the pack's `zpack.json`,
  the switcher, Caelestia's entries in Zebar's `settings.json`, the three logon tasks (`schtasks /Query`) and whether
  Zebar and the helper are running. Install and Repair run three steps and show each one: Zebar through winget
  (`install_zebar`, skipped when Zebar is found), `--install`, then `--start`. "Remove from start-up" runs
  `--uninstall` after a confirmation; the files stay, and the full uninstall is left to Windows apps. The page cannot
  download Caelestia's own files: when the helper or the pack is missing, it asks to run the setup again.
- **Updates**: a few seconds after opening, `check_update` downloads `update.json` and `update.json.minisig` from
  the latest GitHub release. The manifest must be signed by the catalogue key with the trusted comment
  `caelestia-update version=<version>`, name `CaelestiaSetup-<version>.exe` and give its size and SHA-256. It is
  offered only when its version is newer than the installed one (`DisplayVersion` of the setup's uninstall entry,
  else the app's own version), and the page never passes a URL or hash to Rust: the verified manifest stays in the
  Rust side. "Update" streams the setup into `%LOCALAPPDATA%\Caelestia\updates`, with progress events, checks the
  size and SHA-256, then plays the update sound. "Install and restart" runs the setup with `/VERYSILENT
  /SUPPRESSMSGBOXES /NORESTART /MERGETASKS=startnow,openlauncher` and closes the app. Until the repository is public,
  the check ends with "no version published yet". `CAELESTIA_UPDATE_SOURCE` (an `https://` or
  `http://127.0.0.1:` address) points the app at a test server; the signature is still required.
- **Links**: `caelestia://install` opens the Installation page. `caelestia://apply/<id>` is registered for the user by the setup and `scripts/install.ps1`. A second
  instance hands its link to the running one (single-instance plugin), which asks before applying the preset.
- **Restrictions**: the content security policy only allows the page's own files and the helper's ports; the window
  only has the `core:default`, `show`, `hide`, `is-visible`, `set-focus` and `destroy` permissions.
- **Language**: French by default like the theme, English from the app's settings.
- **Sounds**: the files in `launcher/ui/sounds/` are decoded before the window appears. `app-start.mp3` plays as
  the window shows, `notification.mp3` with every snackbar, `warning.mp3` with error snackbars, `updated.mp3` when an
  update has finished downloading, and `close.mp3` when the window is closed: the page fades out and the window is
  destroyed once the sound has played (1.2 s at most). All go through Web Audio with a 60 ms fade-in. WebView2 gets
  `--autoplay-policy=no-user-gesture-required` in `tauri.conf.json`, and the app's settings turn every sound off.

### Recovery

- A hung Zebar (15 s) or Caelestia docks pushed away from their screen edges after a display change (6 s) restart
  Zebar, at most once every 10 minutes, after giving every screen its full work area back.
- Waking from sleep restarts Zebar 4 seconds later whatever the cooldown: after a resume, WebView2 windows can keep
  running their pages while no longer drawing on screen.
- The session menu and the launcher's power actions close their window before asking Windows to sleep or shut down.
- `--restore-shell` shows the Windows taskbar and puts back the Windows colours saved before the colour sync.
