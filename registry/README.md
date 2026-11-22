# Stellaria catalogue

Every file in `items/` is one catalogue entry, named after its `id`. `news.json` feeds the launcher's news page.
`stellaria-registry` (in `helper/tools/Stellaria.Registry`) validates the entries and builds the signed index the
launcher reads.

```powershell
dotnet run --project helper/tools/Stellaria.Registry -- validate registry
$env:STELLARIA_SIGNING_KEY = Get-Content $HOME\.stellaria-signing\stellaria-registry.key
dotnet run --project helper/tools/Stellaria.Registry -- build registry artifacts/registry
```

`build` writes `index.json` and `index.json.minisig`. The signature's trusted comment carries the index version (the
build time in seconds) and an expiry date (14 days by default); the helper refuses an index that is expired, older than
the last one it accepted, or whose content does not match the signed version. The public key is pinned in
`helper/src/Stellaria.Helper/Catalog/TrustedKeys.cs`; the secret key lives outside the repository and, for CI, in the
`STELLARIA_SIGNING_KEY` secret.

## Entry format

| Field | Required | Notes |
| --- | --- | --- |
| `id` | yes | 2 to 48 lowercase letters, digits or dashes, same as the file name. |
| `type` | yes | `core` (Stellaria itself), `preset` (colours and visual options) or `pack` (a Zebar widget pack, not accepted yet). |
| `version` | yes | Semantic version. A published version never changes; publish a new one instead. |
| `license` | yes | An SPDX identifier from the allowed list; packs derived from Caelestia's code must be `GPL-3.0-only`. |
| `name`, `summary` | yes | Objects with `fr` and `en`. |
| `author` | yes | `{ "name", "github" }`. |
| `official` | no | Only for entries maintained in this repository. |
| `tags`, `homepage` | no | |
| `preset` | for presets | `scheme` (`{ "name", "flavour" }` from `pack/schemes.json`, omitted for the dynamic wallpaper colours), `mode` (`dark`, `light`, `auto`), `variant` (a Material You variant), `options` (on/off values for `transparency`, `tintIcons`, `smartScheme`, `clockSeconds`, `desktopClock`, `island`). |
| `artifact` | for packs | `{ "url": "https://github.com/…", "sha256": "…" }`. |
| `scopes` | for packs | Helper scopes the pack needs (see `docs/protocol.md`). |

Presets are declarative: they can only set the values above, never run code. Community submissions will open to
presets first (phase C of `docs/research/Plateforme de thèmes communautaire.md`).

## News format

`news.json` is an array; the app sorts it by date, shows the newest entry as a large card and the others on a timeline,
with filters by kind and a dot on the News tab while an entry from the last 45 days is unread.

| Field | Required | Notes |
| --- | --- | --- |
| `id` | yes | Unique, never reused. |
| `date` | yes | `YYYY-MM-DD`, shown in the reader's language. |
| `kind` | no | `release`, `announcement` (default), `community` or `tip`. |
| `version` | no | Shown as a badge, for releases. |
| `title`, `body` | yes | Objects keyed by language code (`fr`, `en`, `es`, `de`, `pt-BR`, `it`, `pl`, `ru`, `tr`, `zh-CN`, `ja`, `ko`); a missing language falls back to English. |
| `highlights` | no | Up to six `{ "icon", "text" }`, where `icon` is a Material Symbols name and `text` is keyed by language. |
| `url` | no | Opened by "Read more". |

## Roadmap format

`roadmap.json` is an ordered array of phases for the app's Roadmap page.

| Field | Required | Notes |
| --- | --- | --- |
| `id` | yes | Unique. |
| `status` | yes | `done`, `now`, `next` or `later`. |
| `version` | no | The release that shipped the phase. |
| `icon` | no | A Material Symbols name. |
| `title` | yes | Keyed by language code, like the news. |
| `items` | yes | `{ "text" }` entries, `text` keyed by language code. |
