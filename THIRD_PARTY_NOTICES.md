# Third-party notices

Stellaria is distributed under the GNU General Public License v3.0 (see `LICENSE`).
It reuses the work listed below. Every file named here is produced by `npm run vendor` from the exact
package versions pinned in `package.json` and `package-lock.json`.

## Design

| Project | Use | License |
| --- | --- | --- |
| [Caelestia shell](https://github.com/caelestia-dots/shell) by the caelestia-dots contributors | Visual design, layout, motion and behaviour that this project ports to Windows | GPL-3.0 |

This is an unofficial port. It is not affiliated with or endorsed by the caelestia-dots project.

## Bundled code

| Package | Version | Shipped as | License | License text |
| --- | --- | --- | --- | --- |
| [zebar](https://github.com/glzr-io/zebar) | 3.3.1 | `pack/vendor/zebar.mjs` | GPL-3.0-only | `LICENSE` |
| [@tauri-apps/api](https://github.com/tauri-apps/tauri) | 2.0.2 | inside `pack/vendor/zebar.mjs` | Apache-2.0 OR MIT | `licenses/MIT-tauri-apps-api.txt` |
| [zod](https://github.com/colinhacks/zod) | 3.24.2 | inside `pack/vendor/zebar.mjs` | MIT | `licenses/MIT-zod.txt` |
| [luxon](https://github.com/moment/luxon) | 3.4.4 | inside `pack/vendor/zebar.mjs` | MIT | `licenses/MIT-luxon.txt` |
| [glazewm](https://github.com/glzr-io/glazewm) | 1.7.0 | inside `pack/vendor/zebar.mjs` | MIT | `licenses/MIT-glazewm.txt` |
| [material-color-utilities](https://github.com/material-foundation/material-color-utilities) | 0.3.0 | `pack/vendor/material-color-utilities.mjs` | Apache-2.0 | `licenses/Apache-2.0.txt` |

## Fonts

| Font | Version | Shipped as | License | License text |
| --- | --- | --- | --- | --- |
| [Google Sans Flex](https://github.com/googlefonts/googlesans-flex) | @fontsource-variable 5.3.2 | `pack/assets/fonts/google-sans-flex-*.woff2` | OFL-1.1 | `licenses/OFL-1.1-GoogleSansFlex.txt` |
| [Rubik](https://github.com/googlefonts/rubik) | @fontsource-variable 5.3.0 | `pack/assets/fonts/rubik-*.woff2` | OFL-1.1 | `licenses/OFL-1.1-Rubik.txt` |
| [Material Symbols Rounded](https://github.com/google/material-design-icons) | material-symbols 0.48.0 | `pack/assets/fonts/material-symbols-rounded.woff2` | Apache-2.0 | `licenses/Apache-2.0.txt` |

## Data

| Data | Version | Shipped as | License | License text |
| --- | --- | --- | --- | --- |
| [Unicode CLDR emoji annotations](https://github.com/unicode-org/cldr-json) | 48.2.0 | names and keywords in `pack/assets/emoji/*.json` | Unicode-3.0 | `licenses/Unicode-3.0.txt` |
| [unicode-emoji-json](https://github.com/muan/unicode-emoji-json) | 0.9.0 | emoji order and English fallback names in `pack/assets/emoji/*.json` | MIT | `licenses/MIT-unicode-emoji-json.txt` |
