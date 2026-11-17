# Catalog backend, package format and publishing pipeline for a zero-budget community theme/widget platform (2026)

Research note: the web session hit a usage limit after 20 tool calls, so two planned fetches (Scoop autoupdate wiki, jsDelivr data API) could not be done. Those topics are listed under Gaps. Anything not backed by a URL is under Inferences or Gaps.

## Git-based registries: index repo, PR submission, CI validation, bots, maintainer workload

### Takeaway
Every proven low-budget registry uses the same core design: a Git repo that holds small metadata files only, PRs for submission, CI or a bot for mechanical checks, and a human for the final merge. They differ in where the artifacts live (author repos vs central) and in how much is automated. For one maintainer, the Windhawk/Obsidian model fits best: one small file per package, artifacts in the author's GitHub Releases, strict "only the original author can update" rules, and CI doing everything except the merge decision.

### Cited Findings
- **winget-pkgs (Microsoft)**: each PR must be "limited to one package version per PR" and must "not include non-manifest file changes". A GitHub App runs a Validation Pipeline "on your manifest and installers" and reports on the Checks tab. Failures are documented in `doc/ValidationFailureGuide.md`. A bot asks for missing info and closes stale PRs after unanswered reminders. Merge happens once the "requisite number of team members" approve, and triage runs "several times a week" — [winget-pkgs CONTRIBUTING](https://github.com/microsoft/winget-pkgs/blob/master/CONTRIBUTING.md)
- **Windhawk mods**: a new-mod PR "must consist of a single file, `mods/<mod-id>.wh.cpp`". The `github` metadata must match the PR author's GitHub profile. An update PR changes only that file and must raise `version`. "you can only submit an update for a mod that you originally submitted". The commit message becomes the changelog entry. Mods without a license default to MIT — [windhawk-mods README](https://github.com/ramensoftware/windhawk-mods)
- **Obsidian (legacy model)**: the app reads `community-plugins.json` from the obsidian-releases repo. `name`, `author` and `description` are used for search. The detail page pulls `manifest.json` and `README.md` from the author's repo. The manifest is used only to find the latest version, and "Actual files are fetched from your GitHub releases" using a tag that matches the version. Install downloads `manifest.json`, `main.js` and `styles.css`. A `versions.json` in the author's repo maps plugin versions to the minimum app version, so older apps get the latest compatible plugin. The repo also holds `community-plugin-stats.json`, `community-plugins-removed.json` and `community-plugin-deprecation.json` — [obsidian-releases](https://github.com/obsidianmd/obsidian-releases)
- **Obsidian (current docs, 2026)**: submission has moved to a web directory at community.obsidian.md. The author signs in and links GitHub so ownership can be verified, and the directory reads `manifest.json` at the HEAD of the default branch. Submissions are "reviewed automatically" with fix guidance, and issues are fixed by publishing a new release with a higher version. The `id` "must be unique across all published plugins". The release tag must match the manifest version — [Obsidian docs: Submit your plugin](https://docs.obsidian.md/Plugins/Releasing/Submit+your+plugin)
- **Spicetify Marketplace**: there is no central PR. A public repo becomes listed by adding a GitHub topic (`spicetify-extensions`, `spicetify-themes`, `spicetify-apps`) and putting a root `manifest.json` in it. Required keys for themes are name, description, preview, usercss, schemes and readme. Optional keys are branch, authors and tags. A manifest can be an array, so one repo can hold several items. Snippets are the exception: they go through a PR to `snippets.ts` — [Spicetify: Publishing to Marketplace](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
- **Zebar marketplace (glzr)**: this is a hosted, centralized service, not Git-based. Authors run `zebar publish --token <api-token>`, with the token from glzr.io/api-token (or `ZEBAR_PUBLISH_TOKEN`). Options are `--pack-config` (default `./zpack.json`), `--version-override` (must be valid semver), `--commit-sha`, `--release-notes` and `--release-url`. The pack is published under the account that owns the token — [Zebar MARKETPLACE.md](https://raw.githubusercontent.com/glzr-io/zebar/main/MARKETPLACE.md)
- **Zebar packs, local side**: a pack's id comes from its folder name (`~/.glzr/zebar/example-widget/zpack.json` gives `example-widget`), and Zebar scans only one level deep. Marketplace downloads go to an AppData downloads folder, and users are told to copy a pack out of it before editing, because edits there can be overwritten. The README warns "DO NOT re-publish other peoples widgets without permission." The schema is at `resources/zpack-schema.json` — [Zebar README](https://github.com/glzr-io/zebar)
- **Raycast**: one monorepo, `raycast/extensions`. `npm run build` validates for distribution, and `npm run publish` authenticates with GitHub, squashes commits and opens a PR automatically. The Raycast team reviews the PR, and once merged the extension "is published automatically to the Raycast Store" — [Raycast: Publish an extension](https://developers.raycast.com/basics/publish-an-extension)
- **Flathub**: new apps are submitted as a PR against the `new-pr` branch of flathub/flathub, and authors run `flatpak-builder-lint` first. Reviewers are volunteers. A comment `bot, build` starts a test build. Permissions must be justified in the PR. Accepted apps get their own repo under the Flathub org, and the developer gets write access (2FA required). The official build is usually published "within 1-2 hour" — [Flathub submission docs](https://docs.flathub.org/docs/for-app-authors/submission)

### Inferences
- Two families exist. (a) **Central human-reviewed index** (winget, Windhawk, Obsidian-legacy, Raycast, Flathub): high trust, but maintainer time grows with submissions. (b) **Self-listing by topic or token** (Spicetify, Zebar): zero review load, but the catalog is unvetted and depends on GitHub search or a hosted service. For a GPL project that runs local HTML/JS from strangers, (a) is safer. Keep the per-PR review cost low with strict file-scope rules (Windhawk), an automated validator (winget, Obsidian directory) and a fast path for updates.
- **Recommended submission rules for Caelestia**, combining the cited models:
  1. Repo `caelestia-windows-registry` with `packages/<id>.json`, one small file per package. Version history lives inside it as an array of `{version, url, sha256, size, minApp, minZebar, published, yanked?}`.
  2. A new-package PR adds exactly one file. An update PR edits exactly one file and appends a version. The PR author must equal `author.github`, as in Windhawk. That makes "update by original author" mechanically checkable.
  3. A GitHub Actions validator runs on `pull_request_target` with no secrets and no checkout of PR code execution. It checks the JSON Schema, that the id is unique and matches the filename, and that the version is semver and higher than the last one. It downloads the zip, checks SHA-256 and size limits, unzips with path-traversal and size-bomb guards, validates `zpack.json` against Zebar's schema, and enforces an extension allowlist (html/css/js/json/png/webp/svg/woff2). It greps for remote script loads (`<script src=http`), `eval` and `fetch` to non-allowlisted hosts. It renders screenshots with headless Chromium/Playwright and posts them as a PR comment.
  4. Auto-merge: label `update` + CI green + author == owner → auto-merge, because the author already passed review once. New packages always need a human. That keeps the single maintainer's load to new packages only, which matches how update PRs on Windhawk differ from new mods.
  5. Bot needs: GitHub Actions only (labeler, stale, comment). No separate server.

### Gaps
- winget's exact validation steps (installer scanning, hash checks, SmartScreen) are in `doc/Validation.md`, which was not read.
- I found no source on Windhawk's CI checks or reviewer identity. The README does not describe them.
- I found no data on Homebrew/Scoop bot workloads (BrewTestBot, Excavator/checkver). The Scoop autoupdate wiki fetch failed on the usage limit.
- No public numbers on review backlog or maintainer hours were found for any registry.
- How Spicetify Marketplace discovers repos (GitHub search API, unauthenticated limits, blacklist) is not stated on the publishing page.
- The Zebar marketplace backend, review process and zpack.json field list were not documented in the pages fetched. Read `resources/zpack-schema.json` directly.

## Hosting artifacts and the static index: GitHub Releases vs mirror, Pages/Cloudflare/R2, rate limits, costs

### Takeaway
Serve the index as a static file from a CDN (GitHub Pages or Cloudflare Pages, both free). Point downloads at the authors' GitHub Release assets (no size or bandwidth cap, per GitHub), pinned by SHA-256. Mirror into Cloudflare R2 (zero egress cost) only if authors' releases disappear or more control is needed. Never make the client call api.github.com or raw.githubusercontent.com per package, because unauthenticated limits are 60 requests/hour per IP and raw downloads have been throttled since May 2025.

### Cited Findings
- GitHub REST unauthenticated limit: "60 requests per hour", per originating IP. Authenticated: 5,000/h. `GITHUB_TOKEN` in Actions: 1,000/h per repository. Secondary limits include 100 concurrent requests and 900 points/minute for REST — [GitHub Docs: REST rate limits](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api)
- In May 2025 GitHub rolled out stricter limits for unauthenticated requests, covering anonymous REST use, HTTPS cloning and "downloading files from raw.githubusercontent.com" — [GitHub changelog 2025-05-08](https://github.blog/changelog/2025-05-08-updated-rate-limits-for-unauthenticated-requests/). Users report 429s on raw.githubusercontent.com even with light use — [community discussion #159123](https://github.com/orgs/community/discussions/159123), [#157887](https://github.com/orgs/community/discussions/157887)
- GitHub Pages: source repo recommended under 1 GB, published site up to 1 GB, soft bandwidth limit of 100 GB/month, soft limit of 10 builds/hour (not for custom Actions workflows), and a 10-minute deploy timeout. Rate-limited requests get HTTP 429. SaaS and commercial use is prohibited. Over quota, GitHub may suggest "using a third-party CDN, using releases" — [GitHub Pages limits](https://docs.github.com/en/pages/getting-started-with-github-pages/github-pages-limits)
- GitHub Releases: up to 1,000 assets per release, each under 2 GiB. "There is no limit on the total size of a release, nor bandwidth usage." The Releases API exposes per-asset download counts — [GitHub Docs: About releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
- Immutable releases became generally available on 2025-10-28. Once published, assets "can't be added, modified, or deleted", and tags can't be moved or deleted. Releases get signed Sigstore-format attestations that can be verified with the GitHub CLI or Sigstore tooling. Immutability is enabled per repository or organization — [GitHub changelog 2025-10-28](https://github.blog/changelog/2025-10-28-immutable-releases-are-now-generally-available/)
- Cloudflare Pages Free plan: 500 builds/month, 1 concurrent build, 20,000 files per site, 25 MiB max per file, 100 custom domains per project, 100 `_headers` rules. No bandwidth cap is stated for static assets — [Cloudflare Pages limits](https://developers.cloudflare.com/pages/platform/limits/)
- Cloudflare R2 free tier per month: 10 GB-month storage, 1M Class A operations, 10M Class B operations, and free egress. Beyond that: $0.015/GB-month, $4.50 per million Class A, $0.36 per million Class B, egress still free — [Cloudflare R2 pricing](https://developers.cloudflare.com/r2/pricing/)

### Inferences
- **Cost estimate**: an index of a few hundred packages is well under 1 MB (gzip much less). Even 10k launches/day × 200 KB is about 60 GB/month, which fits Pages' 100 GB soft limit and is effectively free on Cloudflare Pages. Artifacts on authors' GitHub Releases cost $0. An R2 mirror of 500 packs × 2 MB is about 1 GB, inside the free 10 GB, and each download is a Class B read (10M/month free). Realistic total: $0/month, plus a custom domain (about €10/year) if wanted.
- **Mirror or not**: linking to author releases (Obsidian model) means zero storage but a link can rot or be replaced. SHA-256 pinning turns replacement into a detectable failure, not a silent compromise, and author repos with immutable releases enabled remove the risk altogether. Recommended: CI copies each accepted zip into a single `artifacts` GitHub Release of the registry repo (release assets have no bandwidth cap), or into R2. The index lists both `url` (mirror) and `upstream`. The mirror is then under project control and survives author deletion. Requiring authors to enable immutable releases is a cheap extra.
- The launcher should fetch only `index.json` (+ signature) and the screenshots from the CDN, and never call the GitHub API at runtime. Popularity data should be precomputed by a scheduled Action (authenticated, 1,000 req/h per repo) and baked into the index.
- Serve screenshots from the same Pages/R2 origin, converted by CI to WebP and resized. Don't hotlink author repos via raw.githubusercontent.com, because of the 2025 throttling.
- Build the index with an Actions workflow on push to main that deploys to Pages. Custom-workflow Pages builds are exempt from the 10 builds/hour soft limit.

### Gaps
- I found no official number for raw.githubusercontent.com limits after May 2025. Only community reports were found.
- Cloudflare Pages' free-plan bandwidth or request policy for static assets is not stated on the limits page. The usual claim of unlimited bandwidth was not verified here.
- jsDelivr (`cdn.jsdelivr.net/gh/...`) as a free mirror plus stats source was not verified (fetch failed).

## Package format: zip + manifest, immutable versions, updates, yanking, deprecation

### Takeaway
Use a plain zip of the Zebar pack folder containing `zpack.json`, plus a Caelestia sidecar manifest that is copied into the registry entry. Versions are semver and immutable. Each version pins sha256 + size + compatibility ranges, and yanking or deprecation are flags in the index, never deletions.

### Cited Findings
- Obsidian ties the release tag to the manifest version and fetches files from that release. `versions.json` gives compatibility fallback, so a client on an older app version gets the newest compatible plugin version — [obsidian-releases](https://github.com/obsidianmd/obsidian-releases), [Obsidian docs](https://docs.obsidian.md/Plugins/Releasing/Submit+your+plugin)
- Obsidian keeps separate removal and deprecation lists (`community-plugins-removed.json`, `community-plugin-deprecation.json`) next to the main index, rather than deleting history — [obsidian-releases](https://github.com/obsidianmd/obsidian-releases)
- Windhawk requires `version` to increase on every update, and the commit message becomes the changelog — [windhawk-mods](https://github.com/ramensoftware/windhawk-mods)
- Zebar's publish CLI requires a valid semver string (`1.0.0`) and takes release notes, commit SHA and release URL as metadata per version — [Zebar MARKETPLACE.md](https://raw.githubusercontent.com/glzr-io/zebar/main/MARKETPLACE.md)
- Zebar derives the pack id from the folder name and only scans one directory level deep — [Zebar README](https://github.com/glzr-io/zebar)
- winget enforces one package version per PR — [winget-pkgs CONTRIBUTING](https://github.com/microsoft/winget-pkgs/blob/master/CONTRIBUTING.md)
- Spicetify manifests carry `preview`, `authors` and `tags`, and can be an array for multi-item repos — [Spicetify wiki](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)

### Inferences
- **Proposed registry entry** (`packages/<id>.json`):
  ```json
  {
    "id": "aurora-bar",
    "type": "widget-pack",
    "name": "Aurora Bar",
    "summary": "...",
    "author": { "name": "...", "github": "login" },
    "license": "MIT",
    "homepage": "https://github.com/login/aurora-bar",
    "tags": ["bar", "minimal"],
    "category": "bars",
    "permissions": ["network:api.open-meteo.com", "providers:media,weather"],
    "dependencies": [{ "id": "caelestia-fonts", "range": "^1.0.0" }],
    "screenshots": [{ "url": "...", "sha256": "...", "w": 1920, "h": 1080 }],
    "versions": [
      {
        "version": "1.2.0",
        "url": "https://github.com/.../releases/download/v1.2.0/aurora-bar-1.2.0.zip",
        "mirror": "https://cdn.example/aurora-bar/1.2.0.zip",
        "sha256": "...",
        "size": 182344,
        "compat": { "caelestia": ">=0.4 <1", "zebar": ">=3.0 <4" },
        "published": "2026-11-01T12:00:00Z",
        "notes": "...",
        "yanked": false
      }
    ],
    "deprecated": null
  }
  ```
- **Immutability**: CI rejects a PR that changes `url`, `sha256` or `size` of an existing version. Only `yanked` / `yankReason` may change. Fixes require a new version, as in Cargo or npm.
- **Yank**: hidden from install and update resolution. Installed copies are left in place and the user gets a notice. **Deprecate**: package-level `{ reason, replacement }`, still installable but marked. **Remove** (malware or DMCA): delete the entry and add it to `removed.json` so clients uninstall or warn. This mirrors Obsidian's separate removed list.
- **Update resolution**: the client picks the highest non-yanked version whose `compat` ranges satisfy the local Caelestia and Zebar versions, as Obsidian does with `versions.json`.
- **Install layout**: unzip into `~/.glzr/zebar/<id>/`. The single-level scan means the zip must contain the pack at its root or one known folder, and CI should enforce that. Keep a `.caelestia-meta.json` with the installed version and sha so local edits can be detected and not overwritten. Zebar's own README warns against editing downloaded packs for the same reason.
- `permissions` is declarative, since HTML/JS in Zebar is not sandboxed by Caelestia. Its value is for review and display, and the CI grep should check that the network hosts used match what is declared.

### Gaps
- I found no source documenting the full `zpack.json` field list (only the schema URL). Read `resources/zpack-schema.json` before finalizing.
- No registry was found that enforces declared permissions for HTML/JS widget packs at runtime.

## Integrity: signed index, SHA-256 pinning, TUF-style freshness

### Takeaway
Sign `index.json` with minisign (Ed25519). Put `{version, generated, expires}` in the signed trusted comment or the signed body, and pin a SHA-256 per artifact. The client rejects an index with a lower `version` than last seen (rollback) or one past `expires` (freeze). This gives the core TUF protections without running TUF's four-role machinery.

### Cited Findings
- TUF lists the threats as rollback ("cannot trick clients into installing software that is older"), indefinite freeze ("cannot respond to client requests with the same, outdated metadata"), arbitrary installation, mix-and-match, endless data and fast-forward — [TUF specification](https://theupdateframework.github.io/specification/latest/)
- TUF roles: root (delegates trust, keys kept offline), targets (which files are trusted), snapshot (consistent view, prevents mix-and-match) and timestamp (re-signed frequently against replay). "Clients MUST NOT trust an expired file." "Clients MUST NOT replace a metadata file with a version number less than the one currently trusted." — [TUF specification](https://theupdateframework.github.io/specification/latest/)
- Minisign uses Ed25519. New implementations "must use the hashed signature format" (prehashed with BLAKE2b-512). The trusted comment is covered by the global signature and "cannot be modified without the secret key". It can hold "timestamps, resource identifiers, or version numbers" to help prevent downgrade attacks. Commands: `minisign -G`, `minisign -Sm file -t '<trusted comment>'`, `minisign -Vm file -P <pubkey>` — [minisign](https://jedisct1.github.io/minisign/)
- GitHub immutable releases add Sigstore-format attestations per release — [GitHub changelog](https://github.blog/changelog/2025-10-28-immutable-releases-are-now-generally-available/)

### Inferences
- **Recommended "TUF-lite"**:
  1. `index.json` contains `"meta": {"version": <monotonic int>, "generated": ..., "expires": now+14d}`, and `index.json.minisig` is signed in CI. The trusted comment repeats `version` and `expires`.
  2. The launcher embeds the public key (and one backup key for rotation). It verifies the signature, refuses `version < lastSeen` (rollback), refuses `now > expires` (freeze), and caps download size (endless data). A scheduled Action re-signs weekly even with no changes, which works as the timestamp role.
  3. Each artifact's `sha256` and `size` sit in the signed index, which covers the targets role. Because there is a single index file, mix-and-match is not possible and snapshot is not needed.
  4. Key custody: the signing key goes in a GitHub Actions secret on a protected environment that requires manual approval, or the maintainer signs locally. The offline backup key acts as a mini-root for rotation. A CI secret is a single point of compromise, which is the trade-off against full TUF.
- If the project later wants full TUF, python-tuf/go-tuf exist, but they add key-ceremony overhead that one maintainer shouldn't need at launch.

### Gaps
- Minisign verification libraries for C#/.NET, Rust and JS were not confirmed from sources, since the minisign page lists none. Check NuGet/crates for maintained implementations, or implement Ed25519 + BLAKE2b directly via libsodium/NSec.

## Search and browsing over a static index

### Takeaway
A single static JSON index searched client-side is enough well into the thousands of packages. Split into a slim `index.json` for lists and search and per-package detail files loaded on demand, and add categories and tags as plain fields.

### Cited Findings
- Obsidian searches only `name`, `author` and `description` from the central list and fetches README and manifest per package when the detail page opens. That is a two-tier static design — [obsidian-releases](https://github.com/obsidianmd/obsidian-releases)
- Spicetify uses `tags` in manifests for browsing — [Spicetify wiki](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
- Cloudflare Pages allows up to 20,000 files of up to 25 MiB each, enough room for per-package detail files and screenshots — [Cloudflare Pages limits](https://developers.cloudflare.com/pages/platform/limits/)

### Inferences
- Slim entry: about 300 bytes × 2,000 packages is about 600 KB raw and about 100 KB gzip, a trivial one-shot download. Use ETag / If-None-Match caching on the CDN so most launches cost a 304.
- Search: plain substring and token matching, or a small fuzzy library (MiniSearch, Fuse.js) in the launcher. Pagination is a UI concern only. The data fits in memory.
- Taxonomy: a fixed `category` enum in the schema (bars, docks, launchers, themes, wallpapers, utilities) that the maintainer controls, plus free `tags` capped at 5 and lowercased by CI.

### Gaps
- No published benchmarks were found for client-side search on catalogs of this size. The figures above are back-of-envelope estimates.

## Popularity without telemetry; ratings/reviews without a server

### Takeaway
Gather popularity server-side from public data with a scheduled GitHub Action and bake it into the index. Use GitHub Release download counts (from the mirror, so every install counts the same way) and author-repo stars. Use GitHub Discussions (via giscus or deep links) for comments and reactions, so no server or database is needed.

### Cited Findings
- The Releases API exposes how many times each release asset was downloaded — [GitHub Docs: About releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
- Obsidian publishes `community-plugin-stats.json` in its index repo, so the stats file is shipped as static data next to the index — [obsidian-releases](https://github.com/obsidianmd/obsidian-releases)
- giscus stores comments in GitHub Discussions with "No database needed". The bot creates a discussion on the first comment or reaction. Discussions can be mapped by a "specific term" (for example the package id). Reactions on the main post are supported. Commenting through the widget requires authorizing the giscus GitHub app via OAuth, but users can also comment directly on GitHub. It advertises "No tracking, no ads, always free", can be self-hosted, and supports lazy loading — [giscus](https://giscus.app/)
- The `GITHUB_TOKEN` limit is 1,000 requests/hour per repo, enough for an hourly or daily stats job over hundreds of repos — [GitHub REST rate limits](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api)

### Inferences
- **Downloads**: if all installs go through mirror assets in the registry's own Releases, one API call per release gives every package's count. That's comparable across packages and needs no client telemetry, since a counter on GitHub's side counting HTTP downloads isn't client tracking. Disclose it in the privacy note anyway. Updates inflate counts, so show "downloads (all versions)" or approximate unique installs as downloads of the latest version.
- **Stars**: one REST call per author repo in the nightly job (`stargazers_count`). Easy to game, so use it as a secondary signal.
- **Ratings**: avoid a star-rating system, since it needs auth and anti-abuse. Use one GitHub Discussion per package, in a "Packages" category of the registry repo created by CI on merge. Its 👍 reaction count is the "likes" metric, collected by the nightly job via GraphQL. The launcher shows the count and an "Open discussion" link. That avoids embedding giscus' OAuth flow in a desktop app, though giscus can be used on a web catalog page.
- "Trending" = delta of download count over 7 days, computed by the nightly job and stored in a small `stats.json` (Obsidian-style separate file, so the signed index doesn't have to change daily).

### Gaps
- Privacy-respecting third-party counters (jsDelivr stats, Plausible-style counters) were not verified because of the tool limit.
- Whether GitHub counts asset downloads served via redirects or CDNs uniquely or deduplicates them is not documented in the pages fetched.

## Accounts and auth later (GitHub OAuth device flow); why no server initially

### Takeaway
At launch no accounts are needed: browsing and installing are anonymous static reads, and submission happens through GitHub PRs. If in-app actions are wanted later (star, react, open a PR), the GitHub OAuth device flow works from a desktop app with no client secret and no backend.

### Cited Findings
- Device flow: POST `https://github.com/login/device/code` with `client_id`, then the user enters the code at `https://github.com/login/device`, then the app polls `https://github.com/login/oauth/access_token` with `grant_type=urn:ietf:params:oauth:grant-type:device_code`. "The `client_secret` is not needed for the device flow." Codes expire after 900 seconds. Polling too fast returns `slow_down`, which adds 5 seconds to the interval. Users are limited to 50 code submissions per hour per app. Device flow must be enabled in the app settings — [GitHub Docs: Authorizing OAuth apps](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
- Authenticated users get 5,000 REST requests/hour versus 60 unauthenticated — [GitHub REST rate limits](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api)
- Zebar's marketplace needs its own account and API token system (glzr.io/api-token), which shows the cost of the hosted-service route — [Zebar MARKETPLACE.md](https://raw.githubusercontent.com/glzr-io/zebar/main/MARKETPLACE.md)
- Obsidian's directory links GitHub accounts specifically to verify repository ownership — [Obsidian docs](https://docs.obsidian.md/Plugins/Releasing/Submit+your+plugin)

### Inferences
- Running a server means uptime, patching, database backups, GDPR exposure (accounts are personal data), abuse handling and money. None of that is needed when GitHub provides identity (PR author), storage (repo + Releases), CI (Actions), discussions (reactions) and CDN (Pages). The static design also matches the "no telemetry" goal.
- Optional later feature, "Submit from launcher": device-flow token with `public_repo` scope, after which the launcher forks the registry, writes `packages/<id>.json` and opens a PR. That's the Raycast `npm run publish` UX with no backend. Store the token in Windows Credential Manager.

### Gaps
- No source was found comparing GitHub Apps and OAuth Apps for this use case (fine-grained permissions vs `public_repo` scope). Check GitHub's "Differences between GitHub Apps and OAuth apps" doc before implementing.

---

**Concrete recommended design, summary of the inferences above**

1. `caelestia-registry` repo: `schema/package.schema.json`, `packages/<id>.json` (one per package, versions array), `removed.json`.
2. PR CI (pull_request_target, read-only, no PR code execution): schema check, id/filename/author checks, semver monotonic, download + sha256 + size cap, safe unzip, zpack.json schema, extension allowlist, static JS/HTML heuristics, Playwright screenshots posted as a comment. Updates by the original author with green CI are auto-merged. New packages need maintainer review.
3. On merge: copy the zip into a registry Release (or R2), convert screenshots to WebP, build `index.json` (slim) + `packages/<id>.json` (detail), bump `meta.version`, set `expires` = +14d, minisign-sign, deploy to GitHub Pages or Cloudflare Pages.
4. Nightly cron: re-sign (freshness), collect download counts, stars and Discussion reactions into `stats.json`.
5. Launcher: fetch the index with ETag caching, verify the minisign signature with the embedded key, check version ≥ last seen and not expired, search in memory, resolve versions by compat ranges, skip yanked versions, download from mirror then upstream, verify sha256, unzip to `~/.glzr/zebar/<id>/`.
6. Cost: $0/month on free tiers. Optional custom domain.
