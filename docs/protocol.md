# Helper protocol

The widgets of the Zebar pack never call Windows APIs themselves. Everything that needs native access goes
through `caelestia-helper.exe`, a background process that exposes a local WebSocket endpoint. This document is
the contract between the two sides. Any change to it must update both `helper/` and `pack/src/core/helper-client.js`
and bump `protocolVersion` when it is not backwards compatible.

## Transport

- The helper listens on `127.0.0.1` only, on the first free port of the range `47610-47619`.
- On startup it writes `pack/runtime.json` inside the installed pack directory:

  ```json
  { "protocolVersion": 1, "port": 47610, "token": "<64 hex characters>", "pid": 12345 }
  ```

  The token is regenerated at every start. Widgets read the file with `fetch('./runtime.json', { cache: 'no-store' })`.
- A widget connects to `ws://127.0.0.1:<port>/`. The token is never part of the URL.
- The helper rejects the upgrade with HTTP 403 when the `Origin` header is not an `http://127.0.0.1:<any port>` or
  `http://localhost:<any port>` origin (Zebar serves widgets from such an origin) or when the `Host` header is not
  `127.0.0.1:<helper port>` or `localhost:<helper port>`. It answers HTTP 503 when 64 connections are already open.
- The first message of the client must be `auth` (see below). The helper sends nothing before a valid `auth`, closes the
  socket with status 1008 (policy violation) on a wrong token, and closes it after 5 seconds without `auth`.
- Every frame is a UTF-8 text frame holding one JSON object of at most 64 KB.
- A connection may send at most 200 requests per 10 seconds. Requests above the limit get a `rate_limited` failure.

## Messages

### Client to helper

| Kind | Shape | Purpose |
| --- | --- | --- |
| `auth` | `{ "kind": "auth", "token": "<token from runtime.json>", "widget": "bar", "monitor": "\\\\.\\DISPLAY1" }` | Must be the first message. `widget` is the Zebar widget name, `monitor` the Windows device name of the monitor the widget runs on. |
| `request` | `{ "kind": "request", "id": "<unique string>", "method": "<name>", "params": { } }` | Ask the helper to do something or to return data. |
| `heartbeat` | `{ "kind": "heartbeat", "visible": true }` | Sent by every widget once per second from `requestAnimationFrame`, with `visible` from `document.visibilityState`. The helper logs a visible widget that sends no heartbeat for 10 seconds and logs again when it recovers, which documents rendering stalls. |

### Helper to client

| Kind | Shape | Purpose |
| --- | --- | --- |
| `hello` | `{ "kind": "hello", "protocolVersion": 1, "helperVersion": "1.0.0" }` | First message after the connection opens. |
| `state` | `{ "kind": "state", "topic": "<topic>", "data": { } }` | Current value of a topic. Sent for every topic right after `hello`, then on every change. |
| `event` | `{ "kind": "event", "name": "<name>", "data": { } }` | Something happened once (a hotkey press, a repaired app bar). |
| `reply` | `{ "kind": "reply", "id": "<request id>", "ok": true, "data": { } }` | Successful answer to a request. |
| `reply` | `{ "kind": "reply", "id": "<request id>", "ok": false, "error": { "code": "<code>", "message": "<english text>" } }` | Failed request. `code` is stable and is what the interface translates. |
| `ping` | `{ "kind": "ping", "time": 1792280465414 }` | Sent every 5 seconds. A client that receives nothing for 15 seconds closes the socket and reconnects. |

A client that connects or reconnects receives the full state again, so it can never stay stale.

## State topics

| Topic | Data |
| --- | --- |
| `focus` | `{ "monitors": { "<device>": { "title": "", "process": "", "exe": "<image path>" } }, "active": "<device>" }`. Empty strings when the desktop has the focus. |
| `monitors` | `{ "<device>": { "fullscreen": false, "covered": false, "bounds": [l, t, r, b], "work": [l, t, r, b], "primary": true } }` |
| `tasks` | `{ "items": [ { "id": "<hwnd>", "title": "", "process": "", "exe": "", "monitor": "", "minimized": false, "active": false } ] }` in a stable order: a window keeps its place, new windows are appended. |
| `icons` | `{ "<exe path>": "<48 px png base64>" }` for the executables of the current tasks. Published only when that set changes, because task titles change far more often than the list of applications. |
| `wallpaper` | `{ "path": "<Windows wallpaper file or null>", "sample": "<png base64, at most 160 px wide>", "source": "capture" \| "file" }`. `capture` is the rendered desktop of the primary monitor, so it follows Wallpaper Engine; `file` is the fallback when the desktop cannot be captured. Published after every wallpaper change and when a 30 second check sees a visible difference. |
| `keyboard` | `{ "capsLock": false, "numLock": true, "layout": "<hkl hex of the last user window>", "layouts": [ { "id": "<hkl hex>", "languageTag": "fr-FR", "name": "<layout name localized by Windows>" } ] }`. Widgets name the language with `Intl.DisplayNames` in the interface language. |
| `desktops` | `{ "items": [ { "id": "<guid>", "name": "<name or null>" } ], "current": 0 }`, the Windows virtual desktops in Task View order. A single unnamed desktop until the user creates a second one. |
| `notifications` | `{ "items": [ { "id", "appId", "time": <unix ms>, "title", "body" } ] }`, the 50 latest Windows toasts from the Action Center history, newest first. Empty while `notifications.history` is off. |
| `idle` | `{ "seconds": 0 }`, published when it crosses 30 s and then every 5 s, and when input resumes |
| `power` | `{ "keepAwake": false }` |
| `shell` | `{ "taskbarHidden": true }`, driven by the `taskbar.hide` setting |
| `config` | the full configuration object described in `docs/configuration.md`, flat keys plus `"version"` |

## Events

| Name | Data |
| --- | --- |
| `hotkey` | `{ "name": "launcher", "monitor": "<device under the cursor>" }` |
| `hotkey.unavailable` | `{ "name": "dashboard", "shortcut": "Win+Alt+A" }` when another program already owns the shortcut |
| `appbars.repaired` | `{ "count": 2 }` |
| `appbars.paused` | `{ "minutes": 5 }` when repairs loop and the circuit breaker stops them |
| `notification` | one item of the `notifications` topic, when a toast arrived in the last 30 seconds |
| `widgets.reload` | `{}`. Every widget reloads its page. Raised by the `widgets.reload` request, used by the settings app to recover a widget that stopped rendering. |

## Requests

| Method | Params | Reply data |
| --- | --- | --- |
| `apps.list` | none | `[ { "name", "id", "icon": "<48 px png base64 or null>" } ]` sorted by name, uninstallers and documents removed |
| `apps.launch` | `{ "id" }`, an identifier returned by `apps.list` | none |
| `audio.sessions` | none | `[ { "id", "name", "system": false, "volume": 0-100, "muted": false, "icon": "<48 px png base64 or null>" } ]`, the live sessions of the default playback device. `name` is the session or process name; `system` marks the Windows system sounds session. |
| `audio.setSessionVolume` | `{ "id", "volume": 0-100 }` from `audio.sessions` | none, `not_found` when the session has ended |
| `audio.setSessionMute` | `{ "id", "muted" }` from `audio.sessions` | none, `not_found` when the session has ended |
| `audio.setDefault` | `{ "id" }` | none |
| `clipboard.history` | `{ "limit": 1-100, default 40 }` | `{ "enabled", "items": [ { "id", "kind": "text", "time": <unix ms>, "text": "<at most 4096 characters>" } ] }`, text entries only |
| `clipboard.paste` | `{ "id" }` from `clipboard.history` | none, the entry becomes the clipboard content and Ctrl+V is sent to the last user window |
| `config.set` | `{ "path": "launcher.favourites", "value": [] }`, see `docs/configuration.md` for every setting | the updated configuration |
| `folders.open` | `{ "name": "wallpapers" \| "screenshots" }` | none |
| `desktops.switch` | `{ "index" }` | none, switches with Ctrl+Win+Left/Right; `not_found` for an index past the last desktop |
| `display.brightness` | none | `{ "<device>": <0-100 or -1 when unsupported> }` |
| `display.setBrightness` | `{ "monitor": "<device>", "value": 0-100 }` | none, `unsupported` when the monitor does not answer DDC/CI |
| `input.paste` | none | none, sends Ctrl+V to the last user window |
| `keyboard.setLayout` | `{ "id" }`, one of the installed layouts | none |
| `media.artwork` | none | `{ "app", "title", "artist", "album", "image", "playing", "shuffle", "repeat": "none" \| "list" \| "track", "canShuffle", "canRepeat" }` or `null` |
| `media.control` | `{ "action": "playPause" \| "next" \| "previous" \| "shuffle" \| "repeat" }` | none, `unsupported` when the player refuses; repeat cycles off, list, track |
| `power.keepAwake` | `{ "enabled" }` | none |
| `power.run` | `{ "action": "lock" \| "sleep" \| "logout" \| "restart" \| "shutdown" }` | none |
| `radios.list` | none | `[ { "kind", "on", "name" } ]` |
| `radios.set` | `{ "kind", "on" }` | the updated list |
| `capture.record` | none | none, sends the Game Bar Win+Alt+R shortcut to the last user window |
| `capture.region` | none | none, opens the Windows snipping overlay (`ms-screenclip:`) |
| `shell.openSettings` | `{ "page": "home" \| "sound" \| "network-status" \| "regionlanguage" \| "display" \| "bluetooth" \| "personalization" }` | none |
| `system.info` | none | `{ "name", "user", "picture": "<192 px png base64 or null>", "cpu", "gpu", "os" }` |
| `system.gpu` | none | `{ "name", "usage": <0-100 or null>, "temperature": <degrees or null> }`. Usage comes from Windows performance counters on every vendor; temperature only when nvidia-smi is installed. |
| `tasks.activate` | `{ "id" }` | none |
| `tasks.close` | `{ "id" }` | none |
| `theme.publish` | `{ "roles": { "<role>": "#rrggbb" }, "transparent": false }`, 1 to 64 roles of 1 to 32 letters | none. Written to `%LOCALAPPDATA%\Caelestia\switcher-theme.json` for the optional window switcher. |
| `wallpapers.apply` | `{ "path" }` inside the configured folder | none, `denied` for any other path |
| `wallpapers.list` | none | `{ "directory", "current", "items": [ { "path", "name", "thumb": "<384 x 216 jpeg base64>" } ] }`, at most 500 images of the configured folder |
| `window.act` | `{ "action": "close" \| "minimize" \| "maximize" \| "focus" }` on the last focused window | none |
| `window.preview` | none | `{ "image": "<jpeg base64 or null>" }` |
| `widgets.reload` | none | none, raises the `widgets.reload` event |

## Error codes

`invalid_request`, `unknown_method`, `invalid_params`, `not_found`, `unsupported`, `denied`, `rate_limited`, `failed`.
