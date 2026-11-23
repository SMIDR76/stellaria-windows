# Changelog

Every notable change to Stellaria. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the
project uses [semantic versioning](https://semver.org/).

## [1.2.0] - unreleased

### Added

- A Customise page in the Stellaria app: frame border thickness and corner roundness with a live preview, and switches
  for the active window on the bar, Do not disturb, seconds, tinted icons, the collapsible tray, the island, theme
  confirmations, system events, the desktop clock, the hot corner, the idle screen and game mode.
- Do not disturb: a switch in the notification panel and the theme settings hides the island and the small
  confirmations, keeps the history, and shows a paused bell on the bar.
- An option to hide the active window on the bar.
- A Roadmap page in the app, shipped in the signed catalogue (`registry/roadmap.json`).
- A short video that announces a new version when the app starts, with its highlights and an Update button. It shows a
  still frame when Windows asks for reduced motion.
- `frame.border` (4 to 24 px): the helper writes the thickness into the frame edges of `zpack.json` and restarts Zebar
  once, so Zebar draws and reserves the new edges itself.
- Requests `theme.options`, `launcher.options`, `launcher.setOption`, `launcher.themeAction` and `stellaria.open`, and
  the events `theme.option` and `theme.action`.
- Every theme setting now lives in the app's Customise page, in four tabs (Appearance, Wallpaper, Desktop, Panels):
  colour mode, the 25 schemes, dynamic variants, Windows colours, the wallpaper folder, the frame, the bar, the idle
  screen, hover panels, notifications, weather, lyrics and hidden launcher apps. Win+Alt+S, the launcher's "Theme
  settings" action and the first-run welcome open the app; the old panel only remains as a fallback.
- The theme follows the app's language.
- Icon animations and click sparkles in the app, which stop when Windows asks for reduced motion.
- Battery support on laptops: a battery icon with its level on the bar (opens Windows battery settings), alerts at
  20, 10 and 5 %, a toast when the charger is plugged in or out, and a battery card with charge, time left and health in
  the dashboard's Performance tab. Desktop PCs without a battery show nothing.

### Fixed

- Steam games and web links in the launcher show their own icon instead of a blank page.
- After an update, the newer catalogue shipped with the setup wins over the cached one, so news and the roadmap appear.
- Updates clear Zebar's web cache (never the theme's settings), so widgets no longer show files from the previous
  version for up to a week.

## [1.1.0] - 2026-11-18

### Added

- The theme, the Stellaria app and the setup in twelve languages: English, French, Spanish, German, Brazilian
  Portuguese, Italian, Polish, Russian, Turkish, Simplified Chinese, Japanese and Korean (the setup has no Simplified
  Chinese, which Inno Setup does not ship). Automatic follows the Windows display language; the theme settings
  (Appearance > Language) and the app's Settings let you choose.
- A collapsible tray in the bar: four icons stay visible, the others sit behind an arrow. The `trayCollapse` option
  (theme settings > Desktop) turns it off.
- `hotkeys.escape` (scope `input`): while a panel opened from the keyboard is open, the helper holds the Escape key for
  at most two minutes and raises a `hotkey` event named `escape`.
- `launcher.wallpaper` (scope `launcher`): the desktop background as the theme samples it, for the app's Home page.
- English launcher commands in every language but French: `>wallpaper`, `>scheme`, `>variant`, `>shortcuts`.
- A redesigned News page in the app: the latest entry as a large card with its highlights, a timeline for the
  others, filters by kind, dates in the reader's language and a dot on the News tab for unread entries. The news is
  written in all twelve languages.

### Changed

- The dashboard and the utilities opened with their shortcut or a click stay open until the shortcut, a click outside
  or Escape; only panels opened by hovering close when the pointer leaves.
- The setup and the release notes describe the eleven setup languages.

### Fixed

- Turning off "Replace the taskbar" left the Windows taskbar on auto-hide; it now gets back the setting it had before
  Stellaria, and Explorer recomputes the screen space it reserves, as when stopping the theme or uninstalling.
- Widgets reload when their fonts fail to load after the PC wakes up, and the welcome screen shows only once.
- The Stellaria app could stay on its background without any page after being reopened.
- The app's Home page showed the Windows wallpaper file instead of the Wallpaper Engine wallpaper on screen.

## [1.0.0] - 2026-11-17

First public release.

[1.1.0]: https://github.com/SMIDR76/stellaria-windows/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/SMIDR76/stellaria-windows/releases/tag/v1.0.0
