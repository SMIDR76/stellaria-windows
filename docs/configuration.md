# Configuration

All settings live in one file: `%APPDATA%\Caelestia\config.json`. The settings app (Win+Alt+S) edits it through the
helper, which validates every value. You can also edit the file by hand while the helper is stopped.

Rules applied when the file is read:

- A missing setting takes its default value.
- An invalid value is ignored, logged in `%LOCALAPPDATA%\Caelestia\logs\helper.log` and replaced by the default.
  A broken file never stops the helper.
- Unknown keys are dropped the next time the helper saves the file.

The file is written atomically (temporary file then rename), so a crash while saving cannot corrupt it.

## Settings

| Setting | Type | Default | Meaning |
| --- | --- | --- | --- |
| `language` | `auto`, `en`, `fr`, `es`, `tr`, `ar`, `kmr`, `kiu` | `auto` | Interface language. `auto` follows the Windows display language and falls back to English. |
| `theme.mode` | `dark`, `light`, `auto` | `dark` | Colour mode. `auto` follows sunrise and sunset at the weather location. |
| `theme.variant` | `tonalspot`, `vibrant`, `expressive`, `fidelity`, `content`, `fruitsalad`, `rainbow`, `neutral`, `monochrome` | `tonalspot` | Material You variant used for the wallpaper based palette. |
| `theme.scheme` | `null` or `{ "name", "flavour" }` | `null` | Fixed colour scheme instead of the wallpaper palette, for example `{ "name": "catppuccin", "flavour": "mocha" }`. |
| `theme.smartScheme` | boolean | `true` | Uses a monochrome palette when the wallpaper has almost no colour. |
| `theme.transparency` | boolean | `false` | Translucent surfaces. |
| `performance.gameMode` | boolean | `false` | Turns off every animation. |
| `taskbar.hide` | boolean | `true` | Hides the Windows taskbar; the Caelestia bar lists open windows instead. See `docs/security.md` for recovery. |
| `clock.seconds` | boolean | `false` | Shows seconds on the bar and dashboard clocks. |
| `desktopClock.enabled` | boolean | `true` | Large clock on the wallpaper. |
| `idle.enabled` | boolean | `true` | Shows the idle screen after inactivity. It never replaces the Windows lock screen. |
| `idle.minutes` | integer 1 to 60 | `10` | Minutes of inactivity before the idle screen. |
| `panels.hoverDashboard` | boolean | `true` | Opens the dashboard when the pointer reaches the middle of the top edge. |
| `panels.hoverOsd` | boolean | `true` | Opens the volume panel from the middle of the right edge. |
| `panels.hoverUtilities` | boolean | `true` | Opens the utilities panel from the bottom right corner. |
| `panels.hoverPopouts` | boolean | `true` | Opens the small bar panels when hovering bar icons. |
| `osd.onVolume` | boolean | `true` | Shows the volume indicator whenever the volume changes. |
| `notifications.theme` | boolean | `true` | Small confirmations from the theme itself. |
| `notifications.system` | boolean | `true` | Notifications for audio device, keyboard layout, Caps Lock and Num Lock changes and the track now playing. |
| `media.lyrics` | boolean | `true` | Synchronised lyrics from LRCLIB in the media tab. Sends the track title and artist to lrclib.net. |
| `weather.enabled` | boolean | `true` | Weather in the bar and dashboard, from Open-Meteo. |
| `weather.location` | `null` or `{ "name", "latitude", "longitude" }` | `null` | Fixed location. When `null`, the approximate location is looked up from the public IP address through ipinfo.io. |
| `launcher.winKey` | boolean | `true` | Pressing a Windows key on its own opens the launcher instead of the Start menu. Win+S and every Win+key shortcut keep working. |
| `launcher.favourites` | list of application identifiers | `[]` | Applications pinned at the top of the launcher. |
| `launcher.hidden` | list of application identifiers | `[]` | Applications hidden from the launcher. |
| `wallpapers.directory` | `null` or an existing absolute folder | `null` | Folder listed by the wallpaper picker. `null` means `Pictures\Wallpapers`, or `Pictures` when that folder does not exist. |
| `screenshots.directory` | `null` or an existing absolute folder | `null` | Folder opened by the screenshots button. `null` means `Pictures\Screenshots`. |

Application identifiers are the ones Windows shows in the Start menu (for example
`Microsoft.WindowsCalculator_8wekyb3d8bbwe!App`). They are only launched when Windows still lists them.

## Files

| Path | Content |
| --- | --- |
| `%APPDATA%\Caelestia\config.json` | Settings described above. |
| `%LOCALAPPDATA%\Caelestia\state.json` | Original taskbar auto-hide value, kept until it is restored. |
| `%LOCALAPPDATA%\Caelestia\logs\helper.log` | Helper log, rotated at 2 MB, three archives kept. |
| `%LOCALAPPDATA%\Caelestia\cache\` | Wallpaper thumbnails. Safe to delete. |
| `%USERPROFILE%\.glzr\zebar\caelestia\runtime.json` | Port and session token of the running helper. Recreated at every start. |
