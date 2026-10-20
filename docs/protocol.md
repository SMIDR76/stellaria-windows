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
| `heartbeat` | `{ "kind": "heartbeat", "visible": true }` | Sent by every widget once per second from `requestAnimationFrame`, so the helper can detect a widget whose rendering stopped. |

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
| `focus` | `{ "monitors": { "<device>": { "title": "", "process": "", "icon": "<png base64 or null>" } }, "active": "<device>" }` |
| `monitors` | `{ "<device>": { "fullscreen": false, "covered": false, "bounds": [l, t, r, b], "work": [l, t, r, b], "primary": true } }` |
| `tasks` | `{ "items": [ { "id": "<hwnd>", "title": "", "process": "", "exe": "", "monitor": "", "minimized": false, "active": false } ], "icons": { "<exe path>": "<png base64>" } }` |
| `wallpaper` | `{ "path": "<file or null>", "sample": "<png base64, 160 px wide>", "source": "capture" }` or `"source": "file"` |
| `keyboard` | `{ "capsLock": false, "numLock": true, "layout": "<hkl hex>", "layouts": [ { "id": "", "language": "", "name": "" } ] }` |
| `idle` | `{ "seconds": 0 }`, published when it crosses 30 s and then every 5 s, and when input resumes |
| `power` | `{ "keepAwake": false }` |
| `shell` | `{ "taskbarHidden": true }` |
| `config` | the full configuration object described in `docs/configuration.md` |

## Events

| Name | Data |
| --- | --- |
| `hotkey` | `{ "name": "launcher", "monitor": "<device under the cursor>" }` |
| `hotkey.unavailable` | `{ "name": "dashboard", "shortcut": "Win+Alt+A" }` when another program already owns the shortcut |
| `appbars.repaired` | `{ "count": 2 }` |
| `appbars.paused` | `{ "minutes": 5 }` when repairs loop and the circuit breaker stops them |

## Requests

| Method | Params | Reply data |
| --- | --- | --- |
| `apps.list` | none | `[ { "name", "id", "icon" } ]` |
| `apps.launch` | `{ "id" }`, an identifier returned by `apps.list` | none |
| `audio.setDefault` | `{ "id" }` | none |
| `clipboard.history` | `{ "limit": 40 }` | `{ "enabled", "items": [ { "id", "kind", "time", "text" } ] }` |
| `clipboard.paste` | `{ "id" }` | none |
| `config.set` | `{ "path": "launcher.favourites", "value": [] }` | the updated configuration |
| `folders.open` | `{ "name": "wallpapers" \| "screenshots" }` | none |
| `display.brightness` | none | `{ "<device>": <0-100 or -1 when unsupported> }` |
| `display.setBrightness` | `{ "monitor", "value" }` | none |
| `input.paste` | none | none |
| `keyboard.setLayout` | `{ "id" }` | none |
| `media.artwork` | none | `{ "app", "title", "artist", "album", "image" }` or `null` |
| `power.keepAwake` | `{ "enabled" }` | none |
| `power.run` | `{ "action": "lock" \| "sleep" \| "logout" \| "restart" \| "shutdown" }` | none |
| `radios.list` | none | `[ { "kind", "on", "name" } ]` |
| `radios.set` | `{ "kind", "on" }` | the updated list |
| `capture.record` | none | none |
| `shell.taskbar` | `{ "hidden" }` | none |
| `shell.openSettings` | `{ "page": "sound" \| "network-status" \| "regionlanguage" \| "display" \| "bluetooth" \| "personalization" }` | none |
| `system.info` | none | `{ "name", "user", "picture", "cpu", "gpu", "os" }` |
| `tasks.activate` | `{ "id" }` | none |
| `tasks.close` | `{ "id" }` | none |
| `theme.publish` | `{ "roles": { "<role>": "#rrggbb" }, "transparent": false }` | none, used by the window switcher |
| `wallpapers.apply` | `{ "path" }` | none |
| `wallpapers.list` | none | `{ "current", "items": [ { "path", "name", "thumb" } ] }` from the configured directory |
| `window.act` | `{ "action": "close" \| "minimize" \| "maximize" \| "focus" }` on the last focused window | none |
| `window.preview` | none | `{ "image": "<jpeg base64 or null>" }` |

## Error codes

`invalid_request`, `unknown_method`, `invalid_params`, `not_found`, `unsupported`, `denied`, `rate_limited`, `failed`.
