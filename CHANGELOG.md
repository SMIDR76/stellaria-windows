# Changelog

Every notable change to Stellaria. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the
project uses [semantic versioning](https://semver.org/).

## [1.1.0] - unreleased

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
