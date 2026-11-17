# Security model

Caelestia for Windows runs permanently in the background, watches windows, reads the clipboard on request,
captures the desktop to extract wallpaper colours and can simulate a few key presses. That is the same kind of access
spyware wants, so the project treats every feature as a potential risk for the people who install it.
This document lists what can go wrong and what the code does about it. It is the security specification of version
1.0: no release is published until every rule below is enforced by code and covered by a test or a documented manual
check. Any later change that touches one of these areas must keep this document true.

## Principles

1. **No elevation.** Nothing in the project asks for administrator rights. The helper manifest requests `asInvoker`,
   the installer only writes inside the current user profile and `HKEY_CURRENT_USER`.
2. **Widgets cannot run programs.** No widget declares Zebar `shellCommands` privileges except the bar, which may only
   relaunch the installed helper executable with no arguments. Every system action goes through the helper, which only
   exposes the closed list of methods in `docs/protocol.md`. There is no "run a command" or "open any path" method.
3. **Every input is validated.** Request parameters are checked for type, range and allow lists before anything
   touches Windows. Paths coming from widgets are only accepted when they resolve inside a directory the user chose.
4. **Nothing sensitive is written to disk or logs.** Logs never contain tokens, message contents, window titles,
   clipboard text, track titles or file contents. Clipboard history is read on explicit request only and never stored.
5. **Network use is minimal, documented and optional.** See [Privacy](#privacy).
6. **The desktop is always recoverable.** If the helper stops for any reason, the Windows taskbar is restored. See
   [Recovery](#recovery).
7. **Reproducible and verifiable delivery.** See [Supply chain](#supply-chain).

## Threats and mitigations

### A web page talks to the helper (cross-site WebSocket hijacking, DNS rebinding)

The helper listens on `127.0.0.1` only. A browser tab can still try to open `ws://127.0.0.1:47610`.

- The handshake is refused unless the `Origin` header is an `http://127.0.0.1` or `http://localhost` origin. Browsers
  set this header themselves and pages cannot forge it.
- The handshake is refused unless the `Host` header is `127.0.0.1:<port>` or `localhost:<port>`, which defeats DNS
  rebinding (a hostile domain resolving to 127.0.0.1 still sends its own name as `Host`).
- After the upgrade, the first message must carry the session token. The helper sends nothing before that, closes the
  connection after 5 seconds without authentication and closes it on a wrong token. Tokens are compared in constant
  time and are never placed in URLs, so they cannot end up in a log.
- The token is a fresh random 256-bit value at every start. It is published in `runtime.json` inside the pack directory.
  Zebar serves pack files only to its own widgets: its asset server requires a per-pack random cookie (`ZEBAR_TOKEN`,
  HttpOnly) and sends no CORS headers, so other web pages cannot read the file. This was checked against Zebar 3.3.1
  (`packages/desktop/src/asset_server.rs`) and by requesting the server without the cookie (HTTP 401).

### Another program of the same user talks to the helper

A process running under the same Windows account can read `runtime.json` and connect. This is not a privilege
escalation: such a process already has every right the helper has. The helper still limits what it can do to the
documented methods and never runs arbitrary commands, so it does not become a convenient tool for malware either.

### Malicious text reaches a widget (cross-site scripting)

Window titles, track titles, lyrics, clipboard entries and file names are attacker controlled. A script injected in a
widget page would gain that widget's abilities.

- Widgets build their DOM with `textContent` and attribute setters only. `innerHTML`, `outerHTML`,
  `insertAdjacentHTML`, `document.write`, `eval` and `new Function` are rejected by ESLint (`no-restricted-properties`,
  `no-restricted-syntax`, `no-eval`, `no-implied-eval`).
- Because widgets hold no `shellCommands` privileges (principle 2), even a successful injection cannot start a program.

### A community pack abuses the helper

Each pack gets its own token and only the scopes listed for it (see `protocol.md`, section Scopes). The official pack and
the launcher have separate tokens; a pack without an entry in `packs.json` gets no runtime file. Topics and events are
filtered per session, so a pack without `notifications` or `input` never sees notification contents or hotkeys, and
requests outside the granted scopes fail with `denied` and are logged. The catalogue index is signed with a minisign
Ed25519 key pinned in the helper (`Catalog/TrustedKeys.cs`); an index older than the last one seen, expired, or whose
content does not match its signed version is refused.

### Abuse of helper methods

| Method | Risk | Mitigation |
| --- | --- | --- |
| `wallpapers.apply` | Setting an arbitrary file | Path must resolve inside the configured wallpaper directory and have an image extension. |
| `wallpapers.list` | Reading arbitrary folders | Only the configured directory is listed. Changing it goes through `config.set`, which requires an existing directory chosen by the user. |
| `apps.launch` | Running arbitrary executables | Only application identifiers present in the current Start menu list are accepted, and they are launched through the shell like a Start menu click. |
| `shell.openSettings` | Opening arbitrary URIs | Only the `ms-settings:` pages in an allow list. |
| `folders.open` | Opening or executing arbitrary paths | Only the named folders `wallpapers` and `screenshots`, resolved by the helper, and only if they are directories. |
| `clipboard.history` | Leaking secrets copied by the user | Read on request only, never logged, never cached, limited to text entries, and only when Windows clipboard history is enabled by the user. |
| `window.preview` | Screenshots of private windows | Image of the last focused window only, kept in memory and sent to local widgets, never written to disk. |
| `input.paste`, `capture.record`, `shell.taskView`, `desktops.switch` | Simulated key presses | Fixed key sequences (Ctrl+V, Win+Alt+R, Win+Tab, Ctrl+Win+arrows) sent only after a user action in the interface (click, wheel over the desktops, hot corner). |
| `notifications` topic | Notification contents are personal | Read only, from the user's own Action Center database (`wpndatabase.db`), 50 latest toasts, kept in memory, never logged or written anywhere. Turned off with `notifications.history`. Dismissing in the theme only hides entries; Windows' history is never modified. |
| `power.run` | Unwanted shutdown | Fixed actions only; the session menu asks for confirmation before restart and shutdown. |

### Resource exhaustion

Incoming messages are limited to 64 KB, the handshake to 8 KB and 5 seconds, connections to 64 and requests to
200 per 10 seconds per connection. Outgoing queues are bounded: a client that does not read fast enough is
disconnected instead of making the helper wait.

## Privacy

The helper itself never contacts the network. The widgets contact these services over HTTPS:

| Service | When | Data sent | How to turn it off |
| --- | --- | --- | --- |
| ipinfo.io (through Zebar's `ip` provider) | Weather enabled and no manual location | The request itself reveals the public IP address | Set a manual location or disable weather in settings |
| Open-Meteo (api.open-meteo.com) | Weather enabled | Approximate latitude and longitude of the city | Disable weather in settings |
| LRCLIB (lrclib.net) | Lyrics enabled and a track is playing | Track title, artist and duration | Disable lyrics in settings |

No telemetry, analytics or crash reporting is collected.

## Recovery

When the bar replaces the taskbar, the helper turns on taskbar auto-hide and hides the taskbar window. Before changing
auto-hide the first time it saves the original value in `%LOCALAPPDATA%\Caelestia\state.json`.

The taskbar window is shown again, still in auto-hide mode so it appears when the pointer reaches the screen edge:

- when the helper exits normally, receives `--quit`, or the Windows session ends;
- when the helper crashes, through its unhandled exception handler;
- when Explorer restarts, because hiding is not persistent.

Auto-hide stays on between sessions on purpose: turning it off at every exit would let Zebar dock its bars against a
work area that still includes the taskbar at the next logon.

The original auto-hide value is restored, and the taskbar shown, by `caelestia-helper.exe --restore-shell` (also
available as a Start menu shortcut created by the installer) and by `uninstall.ps1`. `--restore-shell` first asks a
running helper to quit so it cannot hide the taskbar again.

These paths are verified on the NativeAOT build: the taskbar is visible right after `--quit`, and `--restore-shell`
writes back the saved value and clears it.

The optional Windows colour sync (`windows.setColors`) only writes the current user's personalisation values under
`HKCU` (`Explorer\Accent`, `DWM`, `Themes\Personalize`) and the accent preference Settings itself uses. It saves the
previous values first; turning the option off, `--restore-shell` and `uninstall.ps1` write them back. Values that did
not exist before are removed again, so Windows falls back to its defaults.

## Supply chain

- Every JavaScript dependency is pinned to an exact version in `package.json`, with integrity hashes in
  `package-lock.json`. Vendored files are regenerated by `npm run vendor`, never edited by hand.
- NuGet packages are pinned to exact versions.
- GitHub Actions are pinned to full commit SHAs and run with a read-only `GITHUB_TOKEN`, except the release job, which
  only gets the permissions needed to create artifact attestations.
- Release archives are built by CI only. Each release publishes SHA-256 checksums and a build provenance attestation
  that anyone can check with `gh attestation verify <file> -R <owner>/<repository>`.
- The executable is not code signed yet, so Windows SmartScreen shows a warning on first launch. Free signing for open
  source projects is available from the SignPath Foundation once the repository is public; the steps are tracked in
  `docs/releasing.md`.

## Reporting a vulnerability

See `SECURITY.md`.
