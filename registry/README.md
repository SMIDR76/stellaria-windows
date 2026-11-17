# Caelestia catalogue

Every file in `items/` is one catalogue entry, named after its `id`. `news.json` feeds the launcher's news page.
`caelestia-registry` (in `helper/tools/Caelestia.Registry`) validates the entries and builds the signed index the
launcher reads.

```powershell
dotnet run --project helper/tools/Caelestia.Registry -- validate registry
$env:CAELESTIA_SIGNING_KEY = Get-Content $HOME\.caelestia-signing\caelestia-registry.key
dotnet run --project helper/tools/Caelestia.Registry -- build registry artifacts/registry
```

`build` writes `index.json` and `index.json.minisig`. The signature's trusted comment carries the index version (the
build time in seconds) and an expiry date (14 days by default); the helper refuses an index that is expired, older than
the last one it accepted, or whose content does not match the signed version. The public key is pinned in
`helper/src/Caelestia.Helper/Catalog/TrustedKeys.cs`; the secret key lives outside the repository and, for CI, in the
`CAELESTIA_SIGNING_KEY` secret.

## Entry format

| Field | Required | Notes |
| --- | --- | --- |
| `id` | yes | 2 to 48 lowercase letters, digits or dashes, same as the file name. |
| `type` | yes | `core` (Caelestia itself), `preset` (colours and visual options) or `pack` (a Zebar widget pack, not accepted yet). |
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
