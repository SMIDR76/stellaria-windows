# Case studies: customization platforms and community catalogs (state as of Nov 2026)

Scope note: research was cut short by a tool rate limit after about 30 calls. Platforms with strong primary sources: Windhawk, Zebar, Seelen UI, Obsidian, VS Code Marketplace / Open VSX, GNOME Extensions, KDE Store, Spicetify, Thunderstore, Modrinth, Wallpaper Engine. Thin or missing: BetterDiscord store rules, Lively Wallpaper, Rainmeter catalogs (DeviantArt/VisualSkins), CurseForge app, r2modman internals. These gaps are listed under each question.

## Submission workflow (how authors get content into the catalog)

### Takeaway
There are three models. (1) Git PR into a central registry repo, as in Windhawk, Spicetify snippets/themes and the Obsidian legacy flow. (2) Publish from the CLI or website to the platform's own backend, as in Zebar (`zebar publish --token`), Seelen (upload a bundled `.yaml`), Thunderstore (zip upload), Modrinth and VS Code. (3) Discovery by GitHub topic, as in Spicetify extensions. Windhawk's PR model has the tightest identity binding: the PR author must equal the `github` field in the mod metadata, and only the original author can push updates.

### Cited Findings
- **Windhawk:** a new mod is a PR to `ramensoftware/windhawk-mods` holding exactly one file, `mods/<mod-id>.wh.cpp`. The `github` metadata must match the PR author's GitHub profile. A listed `twitter` handle must be owned and verifiable by the author. — [windhawk-mods README](https://github.com/ramensoftware/windhawk-mods)
- **Windhawk updates:** an update is also a PR touching only that file, with an increased `version`. "You can only submit an update for a mod that you originally submitted." The commit message becomes the mod changelog entry. — [windhawk-mods README](https://github.com/ramensoftware/windhawk-mods)
- **Windhawk licensing:** mods with no stated license are submitted under MIT, and the author is responsible for licensing third-party code. — [windhawk-mods README](https://github.com/ramensoftware/windhawk-mods)
- **Windhawk repo scale signal:** about 2,965 commits, 432 PRs and 1.2k issues at the time of fetch. — [windhawk-mods](https://github.com/ramensoftware/windhawk-mods)
- **Zebar:** packs are published with the CLI, `zebar publish --token <api-token>`. The token comes from glzr.io/api-token or the `ZEBAR_PUBLISH_TOKEN` env var, and the pack is published under the account that owns the token. Optional flags are `--pack-config` (default `./zpack.json`), `--version-override` (semver), `--commit-sha`, `--release-notes` and `--release-url`; the last three are shown on the marketplace page. — [Zebar MARKETPLACE.md](https://github.com/glzr-io/zebar/blob/main/MARKETPLACE.md)
- **Zebar:** the README says "DO NOT re-publish other peoples widgets without permission." — [Zebar README](https://github.com/glzr-io/zebar)
- **Seelen UI:**
  - Authoring flow: create a folder with a `metadata.yml`, test it with `slu resource load <kind> <path>`, bundle it with `slu resource bundle <kind> <path>` into a self-contained `.yaml` (includes resolved, SCSS compiled), then upload that file through the Seelen website "to publish or update your resource in the marketplace". — [Seelen Resource Guidelines](https://seelen.io/docs/resource-guidelines)
  - Resource IDs take the form `@username/resource-name`. The marketplace assigns a permanent internal ID at publication. — [Seelen Resource Guidelines](https://seelen.io/docs/resource-guidelines)
- **Spicetify:**
  - Extensions, themes and custom apps are discovered from public GitHub repos carrying the topic `spicetify-extensions`, `spicetify-themes` or `spicetify-apps`, with a valid root `manifest.json`. Snippets are instead added by PR, editing `snippets.ts` in the Marketplace repo. — [Spicetify Marketplace wiki: Publishing](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
  - The `spicetify/spicetify-extensions` repo is described as "simply holding the list of available themes", and themes are added by PR. This conflicts with the topic-based wiki, so which mechanism is current for themes is unverified. — [spicetify-extensions repo](https://github.com/spicetify/spicetify-extensions)
- **Thunderstore:** the author uploads a zip containing `manifest.json`, `README.md` and `icon.png`. An update means raising `version_number` and uploading again under the same name and team. — [Thunderstore wiki: Creating a package](https://wiki.thunderstore.io/mods/creating-a-package), [Updating a package](https://wiki.thunderstore.io/mods/updating-a-package)
- **Obsidian (May 2026):** Obsidian launched "Obsidian Community", a new directory plus a developer dashboard. Developers can run a preview scan on any branch, tag or commit, or use the ESLint plugin locally. — [Obsidian blog: The future of plugins (12 May 2026)](https://obsidian.md/blog/future-of-plugins/)

### Inferences
- Caelestia already has two compatible paths: Zebar's own `zebar publish` for raw packs, and a Windhawk-style registry repo (one manifest or file per pack, PR author must equal the declared owner, only the owner can update). The second costs nothing to host and gives free audit history.
- A PR-only flow excludes non-developer theme makers. Seelen's "bundle, then upload on the website" flow is friendlier. A hybrid could work: a web form or CLI that opens the PR automatically.

### Gaps
- **BetterDiscord:** I could not find the official submission or approval rules for the betterdiscord.app store. Only a maintainer comment says that listed addons are "vetted". — [BetterDiscord issue #1388](https://github.com/BetterDiscord/BetterDiscord/issues/1388)
- Not researched for lack of time: Lively Wallpaper's library model, Rainmeter's DeviantArt/VisualSkins workflows, and the CurseForge author console.

## Hosting and package/manifest format

### Takeaway
Formats converge on the same pattern: a folder or zip with a JSON or YAML manifest (id, name, description, semver version, preview images, optional dependencies or target app version), plus a README and an icon. Hosting splits three ways: GitHub as CDN (Windhawk builds and hosts compiled mods on GitHub; Spicetify pulls from the author's repo), a platform backend (Zebar, Seelen, Thunderstore, Modrinth) or Steam Workshop.

### Cited Findings
- **Zebar packs:**
  - Each pack is defined by `zpack.json` and can hold several widgets. Zebar scans `~/.glzr/zebar` one level deep, the pack id comes from the folder name, and the schema is at `resources/zpack-schema.json`. — [Zebar README](https://github.com/glzr-io/zebar)
  - Marketplace downloads are stored in `%AppData%/zebar/downloads/`. To edit a marketplace pack, users must copy it into `~/.glzr/zebar/` so updates do not overwrite their changes. — [Zebar README](https://github.com/glzr-io/zebar)
- **Windhawk:**
  - A mod is one C++ source file (`.wh.cpp`) with a metadata block. — [windhawk-mods README](https://github.com/ramensoftware/windhawk-mods)
  - Since v1.7 (5 Dec 2025), mods are "compiled and hosted on GitHub" by a build script in the mods repo, and clients download these pre-compiled binaries by default. Local compilation remains an option. — [Windhawk v1.7 blog](https://ramensoftware.com/windhawk-v1-7-pre-compiled-mods-and-more)
- **Seelen UI:**
  - Resource kinds: Theme, Widget, Plugin, IconPack, Wallpaper. Required metadata: `displayName` and `description`. Optional: `tags`, `appTargetVersion` and images. Text fields can be a language map, which must include `en`. — [Seelen Resource Guidelines](https://seelen.io/docs/resource-guidelines)
  - Images (1:1 portrait, 21:9 banner, 16:9 screenshots) are uploaded to the website, and only seelen.io-hosted images are accepted. — [Seelen Resource Guidelines](https://seelen.io/docs/resource-guidelines)
  - Seelen themes are CSS/SCSS with user-configurable variables, injected into widget webviews and targeting elements by class. Widget authors are told to use global classes rather than CSS modules so themes can target them. — [Seelen Widget Guidelines](https://seelen.io/docs/widget-guidelines), [Theme Guidelines](https://seelen.io/docs/theme-guidelines)
- **Spicetify `manifest.json`:**
  - Required fields: extension = name, description, preview, main, readme; theme = name, description, preview, usercss, schemes, readme; app = name, description, preview, readme. Optional: `branch`, `authors`, `tags` and a theme `include`. — [Spicetify wiki](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
  - Raw GitHub links do not work for `include`, so a GitHub Pages URL is required. A repo can expose several items with an array manifest. — [Spicetify wiki](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
- **Thunderstore:**
  - The package zip needs `manifest.json` (name, version_number as Major.Minor.Patch, website_url, description of 250 chars max, dependencies), `README.md` and a 256x256 `icon.png`. — [Thunderstore wiki](https://wiki.thunderstore.io/mods/creating-a-package), [Thunderstore create docs](https://thunderstore.io/c/aska/create/docs)
  - An uploaded version cannot be edited afterwards. — [Thunderstore wiki: Updating](https://wiki.thunderstore.io/mods/updating-a-package)
- **Rainmeter `.rmskin`:** the package carries an `RMSKIN.ini` whose `[rmskin]` section has `Load=` and `LoadType=`, which set the skin or layout to load after install. The installer has a "Load included skins" checkbox. A GitHub Action (`2bndy5/rmskin-action`) builds rmskin packages in CI. — [Rainmeter forum](https://forum.rainmeter.net/viewtopic.php?p=160499), [rmskin-action](https://github.com/2bndy5/rmskin-action)

### Inferences
- `zpack.json` already plays the role of Thunderstore's `manifest.json`. A Caelestia catalog should add the fields Zebar lacks:
  - `caelestiaTarget` (minimum Caelestia or helper version, like Seelen's `appTargetVersion`)
  - typed dependencies (Thunderstore's `team-name-version` strings)
  - preview images hosted by the catalog (Seelen hosts images only on seelen.io, which prevents hotlink rot and tracking pixels)
  - immutable versions (Thunderstore)
- Seelen's rule that themes target stable global class names is directly applicable. Caelestia should publish a stable CSS class/variable contract so community themes survive pack updates.

### Gaps
- The full `zpack-schema.json` fields (tags, preview images, excludeFiles) and where Zebar's marketplace stores packs (own CDN?) were not confirmed. MARKETPLACE.md does not say.
- No official RMSKIN.ini key reference was found.

## Review and moderation (automated checks, human review, time to review)

### Takeaway
The trend in 2025-2026 is automated scanning on every version, with humans reserved for risk. Obsidian moved in May 2026 from a manual backlog to minute-scale automated review of every version. Windhawk added an author-triggered AI review before the human queue. Microsoft scans before publication and rescans in bulk. Modrinth uses a malware scanner that escalates to a technical moderator. KDE and the Zebar marketplace show no evidence of review, and KDE paid for it in 2024.

### Cited Findings
- **Windhawk:**
  - Every PR has exactly one of three labels: `waiting-for-author`, `waiting-for-ai-review` or `waiting-for-reviewer`.
  - The author comments `/ai-review` (the AI review can take "20-30 minutes or more"), fixes and repeats, then comments `/ready-for-reviewer` to enter the human queue. PRs that skip the AI review "will be sent back".
  - Unaddressed AI findings need a one-sentence justification each. The AI is "set up to look for the mistakes that come up most often in Windhawk mods" and its findings are "suggestions to verify".
  - The wiki does not publish human review criteria or SLAs.
  - — [Windhawk wiki: Mod submission review process](https://github.com/ramensoftware/windhawk/wiki/Mod-submission-review-process)
- **Obsidian (12 May 2026):**
  - Before: a small team reviewed initial submissions manually, the queue grew "as coding agents accelerated plugin creation", and later versions were not reviewed at all.
  - After: every version is scanned for policy compliance, code quality and known vulnerabilities, including malware. Results arrive in minutes. A passing plugin is searchable within 24h, and a failing new version is removed from search within 24h.
  - Over 2,300 queued submissions were processed before launch. Manual review now focuses on popular and featured plugins and community-flagged issues.
  - All existing plugins were re-reviewed. Those failing get a temporary exception with no deadline.
  - Obsidian admits the scorecards have false positives and negatives.
  - — [Obsidian blog](https://obsidian.md/blog/future-of-plugins/)
- **Obsidian runtime model:** Restricted Mode is on by default. Obsidian "cannot reliably restrict plugins to specific permissions", so plugins inherit full app access to files, network and installing programs. — [Obsidian Help: Plugin security](https://obsidian.md/help/plugin-security)
- **VS Code Marketplace (Microsoft, June 2025):**
  - Static scan before publication, and a failed scan blocks publication. Periodic bulk rescans of all packages.
  - Malware is removed immediately and the publisher banned if needed. Removed malicious extensions go on a block list that forces uninstall in VS Code.
  - Newly published extensions are scanned for secrets.
  - — [Microsoft: Security and trust in Visual Studio Marketplace](https://developer.microsoft.com/blog/security-and-trust-in-visual-studio-marketplace/), [VS Code docs: Extension runtime security](https://code.visualstudio.com/docs/configure/extensions/extension-runtime-security)
- **Modrinth:** moderators check every project against the Rules/ToS. An in-house malware scanner can flag code for manual review by a technical moderator, which adds time. Editing a project does not change its queue position. Holiday reviews can take about 2 weeks. — [Modrinth support: Project review times](https://support.modrinth.com/en/articles/8793355-modrinth-project-review-times)
- **Modrinth turnaround:** 24-48h is a third-party figure only. — [CyberPost](https://cyberpost.co/is-modrinth-trustable/)
- **GNOME Extensions (Dec 2025):** the review guidelines now say "Extensions must not be AI-generated". Red flags are unnecessary code, inconsistent style, imaginary APIs and LLM-prompt comments. AI as a completion or learning aid is allowed if the author can explain the code. A reviewer reported spending 6+ hours some days reviewing 15,000+ lines. — [It's FOSS](https://itsfoss.com/news/no-ai-extension-gnome/), [Linuxiac](https://linuxiac.com/gnome-will-reject-shell-extensions-with-ai-generated-code/)
- **KDE Store:** after the March 2024 incident, KDE said it lacks the resources to review the code of each submitted global theme. It warned that third-party "Global themes and widgets ... can and will run arbitrary code" and asked users to report faulty content. — [Linuxiac](https://linuxiac.com/usage-of-plasma-6-global-themes-may-pose-serious-risks/), [David Edmundson, KDE Planet, 20 Mar 2024](https://planet.kde.org/david-edmundson-2024-03-20-trusting-content-on-the-kde-store)
- **Zebar:** MARKETPLACE.md describes no review, approval step or content policy. — [Zebar MARKETPLACE.md](https://github.com/glzr-io/zebar/blob/main/MARKETPLACE.md)
- **Seelen:** the resource guidelines describe no review or moderation. — [Seelen Resource Guidelines](https://seelen.io/docs/resource-guidelines)
- **Spicetify:** the publishing wiki describes no review for topic-tagged repos. — [Spicetify wiki](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
- **BetterDiscord:** a maintainer states that all addons on betterdiscord.app are vetted. The same maintainer argues review mainly helps user education, since plugins have other routes to the Discord token. — [BetterDiscord issue #1388](https://github.com/BetterDiscord/BetterDiscord/issues/1388)

### Inferences
- For a one-person project like Caelestia, Windhawk's sequence is the realistic template: automated CI on the PR, then an optional AI pre-review the author triggers, then a short human approval.
  - Add Obsidian's lesson: review every version, not just the first.
  - Add GNOME's lesson: a written rule that lets the reviewer reject unreviewable code dumps.
- CSS-only themes are low risk. JS widget packs run in Zebar's WebView with Zebar's providers (shell commands are possible), so they deserve a stricter lane than themes. Two tiers fit: "theme (CSS/variables only)" with fast-track review and "widget pack (JS)" with full review.

### Gaps
- Human review times for Windhawk PRs were not published, and I found no measured data.
- Thunderstore's moderation process (pre-upload checks, reporting) was not verified.

## Trust signals (verified authors, downloads, ratings)

### Takeaway
The strongest signals in use: identity binding (Windhawk: GitHub account equals mod owner), domain-verified publishers with a good-standing period (VS Code blue check), and source visibility (Windhawk shows source for every mod). Obsidian is adding capability disclosures and verified-developer labels in 2026. Download counts are common but get gamed: malicious Wallpaper Engine items had tens of thousands of downloads.

### Cited Findings
- **VS Code:** the verified publisher badge means domain ownership was proven and the publisher has been in good standing for at least 6 months. — [Microsoft blog](https://developer.microsoft.com/blog/security-and-trust-in-visual-studio-marketplace/)
- **VS Code name reuse:** a loophole allowed re-registration of names from removed extensions. — [OffSeq radar](https://radar.offseq.com/threat/loophole-allows-threat-actors-to-claim-vs-code-ext-744f0afc)
- **Obsidian (planned):** plugins will declare network, file system, clipboard and other access, shown before install "over the coming months". Labels for verified trusted developers are also planned. — [Obsidian blog](https://obsidian.md/blog/future-of-plugins/)
- **Windhawk:** "Each mod comes in the form of source code, which can be reviewed to make sure that the mod does exactly what it says." — [Ramen Software: Windhawk](https://ramensoftware.com/windhawk)
- **Zebar:** the marketplace page shows the linked commit SHA, release notes and release URL for each version. — [Zebar MARKETPLACE.md](https://github.com/glzr-io/zebar/blob/main/MARKETPLACE.md)
- **Seelen:** listings show a minimum compatible Seelen UI version, such as "2.4.11+". — [Seelen resource listing example](https://seelen.io/resources/I2NI_d3WRRav-yEj75w0Cg/customizable-clean-glass-power-menu)
- **Wallpaper Engine:** infected Workshop items had "thousands or even tens of thousands of downloads", which shows download counts are not a safety signal. — [Windows Central](https://windowscentral.com/gaming/pc-gaming/wallpaper-engine-malware-steam-workshop-kaspersky), [TechRadar](https://techradar.com/pro/security/gamers-beware-experts-flag-steam-workshop-is-being-abused-to-spread-malware-via-wallpaper-engine-app)
- **Seelen domain:** Gridinsoft's own heuristics rate seelen.io "Suspicious", 35/100, while none of 26 external sources flag it. This is a weak signal, but it shows a young storefront domain can attract AV false flags. — [Gridinsoft](https://gridinsoft.com/online-virus-scanner/url/seelen-io)

### Inferences
- Signals cheap enough for Caelestia:
  - an "Official" vs "Community" badge
  - a "Reviewed version" marker per version (Obsidian style)
  - the GitHub identity of the author
  - a link to source at the exact commit SHA (Zebar already supports `--commit-sha`)
  - a capability summary derived from the pack, such as "runs shell commands" or "network access"
- Hold off on ratings until the catalog has volume.

### Gaps
- Rating and review systems (Modrinth, KDE Store, Thunderstore likes) were not researched in detail.
- Download-count manipulation data was not found.

## Install / update / uninstall mechanics and dependency/version compatibility

### Takeaway
Clients install into a managed folder separate from user edits (Zebar: `%AppData%/zebar/downloads/` vs `~/.glzr/zebar/`). They update by semver bump, and good ones allow pinning older versions (Windhawk 1.7 version selection). Thunderstore-style dependency strings with automatic install are the most mature dependency model. A minimum host version per resource (Seelen `appTargetVersion`) is the simplest compatibility gate.

### Cited Findings
- **Windhawk 1.7:**
  - Installs and updates download pre-compiled mods. Users can view and install older mod versions.
  - Settings can be edited visually or as YAML.
  - The Windhawk app itself updates via its setup installer from windhawk.net, with an in-app notice.
  - — [Windhawk v1.7](https://ramensoftware.com/windhawk-v1-7-pre-compiled-mods-and-more)
- **Windhawk code signing:** Windhawk 1.6.1 was blocked by Windows after Microsoft revoked its Trusted Signing certificate. The developer bought a certificate from another vendor for 1.7. — [Windhawk v1.7](https://ramensoftware.com/windhawk-v1-7-pre-compiled-mods-and-more)
- **Zebar:** the marketplace is browsed and installed from inside the Zebar GUI. Downloaded packs live in `%AppData%/zebar/downloads/`, and local copies must be made to customise them without being overwritten. — [Zebar README](https://github.com/glzr-io/zebar)
- **Seelen UI:**
  - Installs use a website download button plus a browser prompt that opens Seelen UI (deep link). — [Seelen listing](https://seelen.io/resources/I2NI_d3WRRav-yEj75w0Cg/customizable-clean-glass-power-menu)
  - `slu resource load` registers a resource only for the current session. — [Seelen Resource Guidelines](https://seelen.io/docs/resource-guidelines)
  - `appTargetVersion` sets the "Oldest Seelen UI version your resource is compatible with". — [Seelen Resource Guidelines](https://seelen.io/docs/resource-guidelines)
- **Thunderstore:** dependencies are strings in the form `{team}-{package}-{version}`, installed automatically by the mod manager (r2modman and others). Versions are immutable once uploaded. — [Thunderstore wiki](https://wiki.thunderstore.io/mods/creating-a-package), [Updating](https://wiki.thunderstore.io/mods/updating-a-package)
- **Spicetify:** the optional `branch` field picks the branch used for publication, otherwise the default branch. Updates therefore track the author's repo head, and no immutable version is described. — [Spicetify wiki](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
- **BetterDiscord:** addons are dropped into the plugins/themes folder, and themes and some plugins need manual updates checked against the website or GitHub. — [Mintlify BetterDiscord docs](https://www.mintlify.com/betterdiscord/betterdiscord/quickstart)
- **VS Code:** the block list forces uninstall of malicious extensions on clients. — [Microsoft blog](https://developer.microsoft.com/blog/security-and-trust-in-visual-studio-marketplace/)
- **Mitigation for auto-updates:** after GlassWorm, researchers advised disabling extension auto-update and using allowlists. — [SecurityWeek](https://www.securityweek.com/open-vsx-downplays-impact-from-glassworm-campaign/)
- **Rainmeter:** the `.rmskin` installer copies files and optionally loads the skin named in `RMSKIN.ini`. The user can untick "Load included skins". — [Rainmeter forum](https://forum.rainmeter.net/viewtopic.php?p=160499)

### Inferences
- Caelestia's launcher should:
  - keep catalog installs in a managed directory and user forks in a separate one (Zebar pattern)
  - store versions immutably, so users can roll back (Windhawk, Thunderstore)
  - carry a remote kill-switch/block list the helper checks at start (VS Code)
  - gate installs on a minimum Caelestia version (Seelen)
  - support `caelestia://` deep links from the website (Seelen)
  - show a "what changed" diff or changelog before auto-applying an update
- Auto-update of third-party JS packs is the main attack vector seen in GlassWorm (hijacked accounts pushing malicious updates). Re-reviewing every version and optionally delaying community updates (manual, or N days) is a cheap mitigation.
- Signing matters for the launcher itself. The Windhawk certificate revocation shows the risk of a single signing provider.

### Gaps
- Uninstall cleanup behaviour (leftover settings) was not documented in the sources found.
- r2modman profile mechanics and the Modrinth/CurseForge app dependency resolution were not researched.

## Monetisation, donations and scale numbers

### Takeaway
Scale data found: Obsidian has over 4,000 plugins and themes and over 120M downloads (May 2026). GlassWorm's first wave reached 35,800 downloads across 7 extensions. Removed VS Code Material Theme extensions had about 9M downloads. Monetisation models were not researched successfully.

### Cited Findings
- **Obsidian:** over 4,000 plugins and themes since 2020, over 120 million plugin downloads, and over 2,300 queued submissions cleared at the May 2026 launch. — [Obsidian blog](https://obsidian.md/blog/future-of-plugins/)
- **VS Code Material Theme:** Microsoft removed two Material Theme extensions with about 9M combined installs and banned the developer. It later restored some after admitting a false positive. — [Techzine](https://www.techzine.eu/news/security/129129/visual-studio-code-extensions-with-9-million-downloads-removed-for-security-risks), [Neowin](https://www.neowin.net/news/senior-microsoft-exec-admits-mistakenly-removing-vscode-extensions-used-by-millions/)
- **GlassWorm:** the first wave was 7 Open VSX extensions with 35,800 downloads (Oct 2025). — [SecurityWeek](https://www.securityweek.com/open-vsx-downplays-impact-from-glassworm-campaign/)

### Inferences
- Donation links in the manifest (`funding` field) are a low-cost addition. Revenue sharing is out of scope for a GPL hobby project.

### Gaps
- Not found in this session:
  - Windhawk mod count or user numbers
  - Zebar marketplace pack count
  - Seelen resource count
  - Spicetify Marketplace numbers
  - Modrinth or CurseForge creator rewards programs
  - Thunderstore scale
  - BetterDiscord addon counts
  - Wallpaper Engine Workshop size
- Monetisation policies (Modrinth creator monetization, CurseForge rewards, Steam paid Workshop) were not verified.

## Documented problems (malware, abandoned packages, breaking updates, moderation burden) and responses

### Takeaway
Every open catalog that runs code has had incidents. The recurring patterns:
- executable content in a "visual" format (Wallpaper Engine application wallpapers in 2026, KDE global themes in 2024)
- account or token hijack pushing malicious updates (Open VSX GlassWorm 2025-26, CurseForge/fractureiser 2023)
- review backlogs once AI-generated submissions surge (Obsidian, GNOME)

The effective responses: remove code-executing content types from public catalogs, scan every version, force remote uninstall, and require MFA or scoped tokens.

### Cited Findings
- **Wallpaper Engine / Steam Workshop (2026):**
  - Kaspersky found dozens of infected "application" wallpapers on the Workshop, many with thousands to tens of thousands of downloads. The goal was stealing Steam accounts, plus backdoors and miners.
  - The code ran by design, since application wallpapers are executables. Activity dates to at least December 2025.
  - Steam removed the items. Wallpaper Engine announced it would permanently remove application wallpapers from the public Workshop within 1-2 weeks.
  - — [Windows Central](https://windowscentral.com/gaming/pc-gaming/wallpaper-engine-malware-steam-workshop-kaspersky), [Digital Citizen](https://www.digitalcitizen.life/wallpaper-engine-workshop-reportedly-spread-malware-through-malicious-steam-items/), [GameGeeker](https://gamegeeker.com/news/wallpaper-engine-removes-steam-workshop-application-wallpapers), [TechSpot](https://techspot.com/news/112812-criminals-have-distributing-malware-steam-since-2025-tens.html)
  - The incident date is reported as mid-June 2026. — [GamingBible](https://gamingbible.com/news/platform/steam/wallpaper-engine-malware-attack-133629-20260617)
- **KDE Store (March 2024):**
  - The "Grey Layout" global theme ran an errant `rm -rf` that wiped a user's files. Analysis points to a shell-parsing bug, not intentional malware.
  - KDE removed the theme, added warnings in the store and said it cannot review all submissions.
  - — [It's FOSS](https://news.itsfoss.com/kde-plasma-global-theme-fiasco), [Linuxiac](https://linuxiac.com/usage-of-plasma-6-global-themes-may-pose-serious-risks/), [KDE Planet](https://planet.kde.org/david-edmundson-2024-03-20-trusting-content-on-the-kde-store)
- **Open VSX GlassWorm (Oct 2025 - Apr 2026):**
  - Oct 2025: 7 extensions with 35.8k downloads, using credential theft to spread to further extensions. It returned in Nov 2025 with 3 more.
  - Feb 2026: a developer account compromise via likely leaked tokens pushed malicious versions of 4 legitimate extensions.
  - Mar 2026: at least 72 extensions abused transitive dependencies and extension packs.
  - Apr 2026: 70+ "sleeper" clones were published.
  - Open VSX removed items but disputed the "worm" label.
  - — [SecurityWeek](https://www.securityweek.com/open-vsx-downplays-impact-from-glassworm-campaign/), [The Hacker News](https://thehackernews.com/2026/03/glassworm-supply-chain-attack-abuses-72.html), [Security Boulevard](https://securityboulevard.com/2026/04/glassworm-malware-attacks-return-via-73-openvsx-sleeper-extensions/), [InfoWorld](https://www.infoworld.com/article/4145589/open-vsx-extensions-hijacked-glassworm-malware-spreads-via-dependency-abuse-2.html)
- **VS Code Marketplace:**
  - Malicious extensions with over 45k downloads stole PII and opened backdoors (Check Point). — [Check Point](https://blog.checkpoint.com/securing-the-cloud/malicious-vscode-extensions-with-more-than-45k-downloads-steal-pii-and-enable-backdoors/)
  - Microsoft revoked tokens leaked inside extensions and added secret scanning in 2025. — [Microsoft blog](https://developer.microsoft.com/blog/security-and-trust-in-visual-studio-marketplace/)
- **CurseForge (June 2023):** targeted malware infected Minecraft mod JARs, and CurseForge says all infected files were removed and published an incident report and detection tool. — [CurseForge blog](https://blog.curseforge.com/safeguarding-our-community-curseforge-fighting-malware-incident-report/), [CurseForge support](https://support.curseforge.com/de/support/solutions/articles/9000228502-june-2023-infected-mods-detection-tool-old)
- **Modrinth (7 May 2024):** disclosed a malicious "Windows Borderless" project that passed review. Details of the response were not retrieved because of the rate limit. — [Modrinth news](https://modrinth.com/news/article/windows-borderless-malware-disclosure/)
- **Other Steam Workshop incidents:**
  - Hijacked Slay the Spire "Downfall" mod. — [gHacks](https://www.ghacks.net/?p=204703)
  - Cities: Skylines rogue mods removed by Valve. — [Malwarebytes](https://www.malwarebytes.com/?p=61302)
  - Project Zomboid banned a dozen-plus Workshop mods with obfuscated code. — [PC Gamer](https://www.pcgamer.com/games/survival-crafting/project-zomboid-identifies-and-bans-over-a-dozen-steam-workshop-mods-containing-heavily-obfuscated-code-that-was-creating-malicious-files)
- **BetterDiscord-adjacent (March 2026):** malicious npm packages patched BetterDiscord's `index.js` to neutralise its built-in webhook protection, enabling token exfiltration. This was not a store-listed plugin. — [Cyber Security News](https://cybersecuritynews.com/malicious-npm-packages-target-discord-browsers-and-crypto-wallets/amp/)
- **Thunderstore governance (2024):** amid harassment, the BetterUI developer removed most mods. Thunderstore deprecated the mod and required removal of a feature before further updates, which shows the platform policing feature content. — [r/riskofrain thread](https://redlib.ssps.io/r/riskofrain/comments/1cv6egg/whats_up_with_betterui/l4tbwni/?context=3)
- **Moderation burden:**
  - GNOME reviewer spending 6h+/day on 15k+ LOC, which led to the AI-code rule (Dec 2025). — [It's FOSS](https://itsfoss.com/news/no-ai-extension-gnome/)
  - Obsidian backlog driven by coding agents. — [Obsidian blog](https://obsidian.md/blog/future-of-plugins/)
- **Obsidian (2026):** reports of attacks exploiting community plugins were called by the CEO a "social engineering attack that requires users to proactively reject multiple security warnings". — [Gigazine](https://www.gigazine.net/gsc_news/en/20260513-obsidian-plugin-future)

### Inferences
- Lessons for Caelestia:
  1. Never let a "theme" type execute code. Keep CSS/token themes strictly declarative, and treat JS widget packs as code. This lesson comes from Wallpaper Engine (2026) and KDE (2024).
  2. Treat the author account as the attack surface. Require GitHub identity binding (Windhawk) and review every version, especially updates to popular packs (GlassWorm, CurseForge).
  3. Ship a remote revocation list the helper enforces (VS Code).
  4. Avoid transitive dependency auto-install for JS packs, or require each dependency to be separately reviewed (GlassWorm transitive abuse).
  5. Write the review rules down before the first submission, including a rule against unexplainable AI-generated dumps (GNOME), so rejections are defensible.
- Zebar's own marketplace (token publish, no documented review) is a convenient channel but not a curated one. A Caelestia catalog layered over it should be its own allowlist of reviewed pack versions (by commit SHA), not a mirror of everything on the Zebar marketplace.

### Gaps
- Zebar marketplace status and health in 2026 (pack count, activity, the web URL glzr.io/marketplace returned 404) could not be confirmed.
- Lively Wallpaper (web wallpapers on a WebView, similar risk to Zebar packs) was not researched.
- No verified Spicetify Marketplace malware incident was found. It was not searched exhaustively.
- Abandoned-package handling (Obsidian "temporary exception" is the only data point; Modrinth/Thunderstore "deprecated" flags) was not researched in depth.
- The Modrinth Windows Borderless post-mortem details are missing because the fetch was blocked by the rate limit.
