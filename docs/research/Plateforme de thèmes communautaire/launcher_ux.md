# UX patterns for launcher / platform apps (game launchers, mod managers, theme stores), applied to a "Caelestia for Windows" app (2026)

Scope note: research done 2026-11-16 with about 18 search and fetch calls. Primary sources were favoured: official docs, changelogs and developer statements. Forum and Reddit material is used only for user sentiment and is labelled as such. Several apps in the brief (Microsoft Store, Wallpaper Engine, Seelen UI's in-app UI, Thunderstore web) were not checked in depth. See the Gaps sections.

## 1. Information architecture of reference apps (navigation, home/discover, search, detail page, install/progress, update badges, library, settings)

### Takeaway
Mature launchers separate "acquire" (Store/Discover) from "own" (Library) and give the owned content a home dashboard: recent items, status and quick actions. 2024–2026 redesigns (Modrinth App 0.9, Steam Library, Epic's rebuild) all moved toward a context-aware sidebar, filterable libraries and project pages that match the website. Detail pages that earn trust show source, author identity, changelog/diff and compatibility before the install button.

### Cited Findings
- **Steam Library (still the reference model):** the home is split into customizable "shelves" that users add, reorder by drag and drop, and delete. "Collections" replaced Categories. "Dynamic collections" are saved tag filters that pick up new items automatically. Game pages put big artwork at the top with translucent acrylic panels. — [PC Gamer](https://www.pcgamer.com/steam-library-redesign-date-preview); [Steam library update page](https://store.steampowered.com/libraryupdate); [Windows Central](https://www.windowscentral.com/heres-quick-look-new-steam-library-page-design)
- **Steam caveat:** dynamic collections depend on tags that are not always accurate, so users have to prune them by hand. — [PC Gamer](https://www.pcgamer.com/steam-library-redesign-date-preview)
- **Modrinth App 0.9.0 (24 Dec 2024)**, a full redesign "in-line with the website":
  - the sidebar changes to show the content most relevant to each page;
  - Home shows instances with total play time and last-played date, plus "Quick Instances" for recent ones;
  - Discover was overhauled, and project pages are full-featured and match the website;
  - the Library gives responsive feedback while instances install;
  - instance content has type filters and filters for checking updates;
  - settings were reorganized into clearer interfaces;
  - the release also fixed "Database is locked" errors on slow disks and invalid states caused by API downtime.
  — [Modrinth changelog 0.9.0](https://modrinth.com/news/changelog/app/0.9.0)
- **Modrinth later releases:** 0.10.13 "revamped the app sidebar and friends UI to be more straightforward". 0.10.20 (Nov 2025) focused on contrast and consistency, especially in light mode. — [newreleases v0.10.13](https://newreleases.io/project/github/modrinth/code/release/v0.10.13); [Modrinth changelog 0.10.20](https://modrinth.com/news/changelog/app/0.10.20)
- **Playnite:** add-ons (extensions and themes) are installed from a built-in add-on browser (Main menu > Add-ons > Browse, or F9), with separate "Themes Desktop" and "Themes Fullscreen" categories. — [Playnite docs: installing themes](https://api.playnite.link/docs/manual/features/themesSupport/installingThemes.html); [The Windows Club](https://www.thewindowsclub.com/how-to-use-playnite-fullscreen-themes)
- **Playnite add-on publishing:** the add-on browser installs and updates extensions, and developers must publish to the add-on repository to appear there. — [Playnite changelog](https://api.playnite.link/docs/changelog.html)
- **Playnite modes:** two shells. Desktop mode is built for keyboard and mouse, Fullscreen mode for gamepads (F11 switches). — [Playnite theme intro](https://api.playnite.link/docs/tutorials/themes/introduction.html)
- **Playnite theme settings:** Playnite has no native theme configuration. A community "ThemeModifier" extension adds options to Desktop themes. This gap is a lesson for Caelestia, which needs first-class settings for the active theme. — [Playnite docs](https://api.playnite.link/docs/manual/features/themesSupport/installingThemes.html)
- **Seelen UI (closest Windows peer):** a marketplace on seelen.io with resource types themes, widgets, plugins, icon packs and wallpapers. Listings show a minimum Seelen UI version, the platform and download counts (for example ">6K downloads"). Themes expose optional user-configurable CSS variables. Resources loaded through the CLI last only for the session, and permanent install goes through the settings panel. — [Seelen resource guidelines](https://seelen.io/docs/resource-guidelines); [Seelen theme guidelines](https://seelen.io/docs/theme-guidelines); [example listing](https://seelen.io/resources/bqiiSUFPSTiAixhlJujKOQ/macos-style-alt-taber)
- **Windhawk:** mods are browsed in-app and on windhawk.net, where the catalogue holds "hundreds" of mods. Each mod has a details view with a Changelog tab (added in 1.0 beta). Authors declare their name, GitHub, X and homepage in the mod header metadata. — [MalwareTips thread on Windhawk releases](https://malwaretips.com/threads/windhawk-new-version-of-the-modular-windows-customizer.119684/); [Windhawk wiki: creating a mod](https://github.com/ramensoftware/windhawk/wiki/Creating-a-new-mod); [HN discussion](https://hn.svelte.dev/item/45938723)
- **VS Code extensions view, details before install:** ratings and reviews, Q&A activity, whether a repository, issue tracker and license are provided, and verified-publisher status. — [VS Code extension runtime security](https://code.visualstudio.com/docs/configure/extensions/extension-runtime-security)
- **r2modman / Thunderstore:** the IA is built around "profiles", which are separate mod sets per game. Users can export a profile as a file or code to share mods and configs. Since 3.1.12, an existing profile can be updated from a code or file without delete and re-import. A 3.0.23 change to the code-export host broke imports, and 3.0.24 fixed it. — [r2modmanPlus README](https://github.com/ebkr/r2modmanPlus); [r2modman 3.1.12 on Thunderstore](https://thunderstore.io/c/re-legend/p/ebkr/r2modman/v/3.1.12/); [r2modman 3.0.24](https://new.thunderstore.io/c/zort/p/ebkr/r2modman/v/3.0.24/)
- **Material 3 Expressive navigation:** the navigation rail now comes in collapsed and expanded forms. The expanded rail replaces the navigation drawer. — [SAP Fiori for Android, M3 navigation rail](https://www.sap.com/design-system/fiori-design-android/v26-1/components/m3-standard-components/navigation-rail/usage); [9to5Google](https://9to5google.com/2025/05/14/material-3-expressive-navigation/)
- **Rail sizing (secondary sources, not the official Material page):** about 3–7 destinations, for windows at least 600 dp wide. An expanded rail should show at least 3 items. — [Liri Fluid NavigationRail docs](https://docs.liri.io/sdk/fluid/develop/classNavigationRail.html); [Android WideNavigationRail reference](https://developer.android.com/reference/kotlin/androidx/compose/material3/WideNavigationRail.composable)

### Inferences
- The Steam/Modrinth pattern maps directly onto Caelestia: **Accueil** (dashboard of the active setup), **Découvrir** (catalogue), **Bibliothèque** (installed items) and **Réglages**, plus the special "Caelestia" core item (install/repair/update), which is closer to a game in a launcher than to a catalogue item.
- Modrinth's "sidebar that adapts per page" and "project pages identical to the website" are worth copying. A single item renderer for web and app means one source of truth for screenshots, changelog and compatibility.
- Playnite's missing native theme options and Seelen's "user-configurable CSS variables" show that a theme platform needs a **settings schema** declared by each theme, which the app renders as a form.

### Gaps
- No primary source was found for the 2026 layouts of Epic, GOG Galaxy, Microsoft Store, Wallpaper Engine or CurseForge. These are not described here.
- The official m3.material.io navigation rail page was not fetched. Rail counts and breakpoints come from secondary implementation docs.

## 2. Patterns that build trust, and anti-patterns

### Takeaway
Trust comes from verifiable provenance (public source, linked author identity, verified-publisher criteria), integrity (signing, scanning, a blocklist with auto-removal), visible change history (changelogs and diffs between versions) and user control over updates. Launchers lose trust through forced extra apps or accounts, ads inside paid flows, heavy background processes and slowness.

### Cited Findings
- **VS Code / Marketplace trust stack:**
  - the verified-publisher blue check requires proven domain ownership and at least 6 months in good standing;
  - every package and update is scanned by several AV engines, plus dynamic analysis in a sandbox VM;
  - since VS Code 1.97, the first install from a new third-party publisher triggers a "trust this publisher" prompt;
  - malicious extensions go on a blocklist and are uninstalled automatically;
  - packages are signed at publication and the signature is checked at install.
  — [VS Code docs](https://code.visualstudio.com/docs/configure/extensions/extension-runtime-security)
- **Raycast Store:**
  - every extension's source is public, and community members and Raycast staff review each one against the guidelines before it is merged;
  - automated static security analysis in CI was described as planned, not yet in place;
  - extensions run out of process, so a rogue extension cannot seriously affect the main app;
  - extensions update automatically, which Raycast frames as a security measure.
  — [Raycast developer docs: security](https://developers.raycast.com/information/security); [Raycast blog: how the API works](https://raycast.com/blog/how-raycast-api-extensions-work); [Raycast extension guidelines](https://manual.raycast.com/extensions-guidelines)
- **Windhawk:**
  - the developer says authors' GitHub and X profiles are verified as theirs, so users can judge them;
  - mods are single, usually short files, so they are easier to review;
  - a commenter noted that Windhawk shows diffs when a mod version changes;
  - there is no auto-updater, only update notifications;
  - the background process uses "a couple of MB".
  — [HN thread with developer m417z](https://hn.svelte.dev/item/45938723)
- **Windhawk sceptics:** injection techniques resemble malware and can trigger AV/EDR alerts. "Open source" does not by itself guarantee safety. This shows that permission disclosure matters for system-level customisation. — [HN thread](https://hn.svelte.dev/item/45938723)
- **Seelen marketplace listings:** compatibility (minimum app version, platform) and download counts are shown as social proof. — [Seelen listing example](https://seelen.io/resources/bqiiSUFPSTiAixhlJujKOQ/macos-style-alt-taber)
- **Anti-pattern, CurseForge/Overwolf:**
  - user complaints centre on the Windows app requiring Overwolf, which has admin rights and persistent background services, and on more ads and slower downloads;
  - Overwolf said ads would have an opt-out and that 70–80% of ad revenue goes to creators;
  - a June 2023 malware incident on the Minecraft mod ecosystem damaged trust.
  User-opinion and aggregator sources, treat as sentiment. — [GDLauncher guide](https://gdlauncher.com/guides/curseforge-vs-modrinth); [r/feedthebeast via redlib](https://redlib.ssps.io/r/feedthebeast/comments/1byw5xx/is_the_curseforge_app_really_that_bad); [Trustpilot CurseForge](https://ie.trustpilot.com/review/www.curseforge.com?page=5)
- **Modrinth's positioning:** open source, an open API by default (Prism and ATLauncher integrate with it), no Overwolf requirement, and creators get 75% of ad revenue. Trade-off: a smaller catalogue. — [GDLauncher guide](https://gdlauncher.com/guides/curseforge-vs-modrinth); [Kinetic Hosting](https://www.kinetichosting.com/blog/minecraft/curseforge-vs-modrinth-which-is-better-to-use)
- **Anti-pattern, EA app:** a forum user reports an EA Play subscription ad at every app launch. Users resent being forced into "yet another app to install more apps" even for games owned on Steam. — [EA forums](https://forums.ea.com/discussions/ea-app-feedback-en/why-do-i-need-yet-another-app-to-install-more-apps-no-longer-buying-your-games-/13511695); [EA forums: basic functionality](https://forums.ea.com/discussions/ea-forums-general-discussion-en/basic-functionality-still-not-working-on-ea-app-after-years-of-development/13569817)
- **Anti-pattern, Ubisoft Connect:** the double-launch flow is a common complaint (the Steam launch opens Ubisoft Connect, which then launches the game). A 2026 report says Ubisoft is starting to drop the separate client for some Steam titles but keeps account linking. That report is single-source. — [tech-insider](https://tech-insider.org/ubisoft-connect-launcher-removed-steam-2026/); [PC Gamer on launcher bloat](https://pcgamer.com/2022-saw-launcher-bloat-turn-from-a-minor-annoyance-into-a-genuine-problem)

### Inferences
- A practical trust stack for Caelestia, from cheap to costly:
  1. Every catalogue item links to a public Git repo and author profiles (Windhawk/Raycast model).
  2. Review by pull request on a public "index" repo (Raycast model).
  3. Hash and signature checks at download (VS Code model).
  4. A "permissions" section on each item listing what it touches: files written, scripts executed, Zebar widgets, registry, startup entries, network access.
  5. A **diff view on updates** for script-bearing items (Windhawk model).
  6. A remote blocklist that disables flagged items (VS Code model).
  7. A "Signaler" (report) button.
- Show a "verified" badge only with explicit, published criteria (VS Code's domain plus 6-month rule is a good template). Unexplained badges add no trust.
- Hard rules: no ads, no mandatory account to browse or install (an account only for publishing, rating or sync), no forced background service beyond what Caelestia already runs, and update notifications without silent auto-install for items that run code.

### Gaps
- No source was found on how Thunderstore or Seelen review submissions, or whether they scan for malware.
- No primary data was found on the conversion or retention effect of verified badges or permission screens.

## 3. Onboarding/first run, empty states, offline mode, error handling and "repair", update notifications without nagging

### Takeaway
Keep onboarding minimal and functional: NN/g finds that tutorials do not improve success and that skipping them is cheap. The first run should do only what needs input (language, detect or install Caelestia, wallpaper colour source) and then land on a useful home. Resilience to backend downtime, slow disks and partial installs belongs in the core design, and the 2025–26 launcher stories (Modrinth, Epic) show what happens when it is missing.

### Cited Findings
- **NN/g tutorial study:** 70 users, 4 apps with deck-of-cards tutorials. Participants who read the tutorials found tasks harder and had no gain in success or time. NN/g recommends avoiding onboarding where possible and improving the UI instead. Onboarding is justified when user information is needed to start. — [NN/g: mobile tutorials](https://www.nngroup.com/articles/mobile-tutorials/); [NN/g: mobile app onboarding](https://www.nngroup.com/articles/mobile-app-onboarding/); [NN/g video: skip it when possible](https://www.nngroup.com/videos/onboarding-skip-it-when-possible/)
- **Modrinth App 0.9.0 resilience fixes:** "Database is locked" errors on slow disks, and edge cases where API downtime caused an invalid app state. These are typical failure modes a launcher must handle. — [Modrinth 0.9.0](https://modrinth.com/news/changelog/app/0.9.0)
- **Epic Games Store slowness:** Epic's store head admitted in February 2026 that the client is slow and clunky because it pings backend services on every click. A rebuild targets 5× faster startup, with first improvements around May–June 2026. Early preview users reported crashes, janky library scrolling, slow detail pages and high memory use. — [Tom's Hardware](https://www.tomshardware.com/video-games/pc-gaming/epic-knows-its-launcher-sucks-and-is-fully-rebuilding-it-a-faster-more-stable-client-is-on-the-way-with-player-profiles-and-extended-social-features); [WebProNews](https://www.webpronews.com/epics-launcher-overhaul-5x-faster-boots-and-long-missing-features-aim-to-close-gap-with-steam/); [Gurugamer](https://gurugamer.com/pc-console/epics-ai-built-launcher-claims-speed-edge-over-steam-but-arrives-full-of-bugs-27441/amp)
- **Update policy, Windhawk:** notification-only updates, no auto-installer. Users keep control. — [HN thread](https://hn.svelte.dev/item/45938723)
- **Update policy, Raycast:** automatic updates for extensions, presented as a security measure. This is the opposite choice, and it works because every update is reviewed. — [Raycast docs](https://developers.raycast.com/information/security)
- **Update controls, GOG Galaxy:** per-game "Auto Updates" toggle under Manage Installation > Configure. Users turn it off to pin a version. — [GOG forum, via search summary](https://www.gog.com/forum/general/a_question_about_game_versions_and_updates)
- **Vortex as a cautionary tale:** users report load orders reset after deployment or after adding or removing a mod, and a collection's specified order being ignored. In Vortex, "revert" is itself the undo, with no separate undo. Fixes are still landing in 1.13.x. — [Nexus forums: BG3 load order](https://forums.nexusmods.com/topic/9328373-baldurs-gate-3-sorting-load-order); [Vortex v1.13.1 notes](https://newreleases.io/project/github/Nexus-Mods/Vortex/release/v1.13.1); [Nexus forums: collection order ignored](https://forums.nexusmods.com/topic/13495450-vortex-incapable-of-recognizing-installed-mods-as-part-of-the-collection)

### Inferences
- **First run (3 steps maximum, each skippable with "Plus tard"):**
  1. language (detected from Windows, FR by default);
  2. detect an existing Caelestia install (repo checkout or installed version) or offer "Installer", with a size and duration estimate;
  3. pick the colour source (wallpaper or preset) with a live preview.
  Then land on Accueil. No account step.
- **Empty states:** an empty Bibliothèque shows 3–6 curated starter packs from Découvrir. An empty search shows "Aucun résultat" with a clear-filters action and a link to request or submit a theme. With no network, Découvrir shows a cached catalogue with an "Hors ligne — données du JJ/MM" banner.
- **Offline mode:** Bibliothèque, Réglages and Repair must work fully offline. Only Découvrir and Actualités degrade. Cache catalogue JSON and thumbnails, and never block startup on the network. Epic's admitted per-click backend pings are the anti-pattern.
- **Repair** is a first-class action on the Caelestia core card and on each item:
  1. verify files against a manifest (hashes);
  2. re-download what is missing or corrupt;
  3. re-register startup entries and Zebar or helper processes;
  4. reset the configuration to the last known good state.
  Show progress in steps, give a plain-language result, and offer to copy logs for an issue report.
- **Update notifications:** one badge on Bibliothèque with a count, a per-item "Mise à jour" chip and a "Tout mettre à jour" button. The tray shows at most one toast per update batch. Auto-update is opt-in per item and off by default for items that run scripts (Windhawk logic).

### Gaps
- No primary 2026 data on onboarding for desktop power-user tools specifically. The NN/g study is about mobile and is several years old.
- No source was found for Steam's or Playnite's exact offline-mode UX.

## 4. Previews, live preview before applying, installing vs applying, one-click rollback

### Takeaway
Theme and wallpaper tools separate **installing** (on disk) from **applying** (active), and offer a preview step in between. Version rollback (GOG Galaxy since 2015) and "revert" (Vortex) are expected for anything that can break a setup, but both are limited and error-prone when versions are not retained or state is not snapshotted.

### Cited Findings
- **Lively Wallpaper:** each library tile has a menu with "Preview", which shows the animated wallpaper before you set it, and "Customize", which adjusts properties and greys out those that cannot be customised. An early release added a live-preview window with full input support. 1.4.0.0 added preview GIF and thumbnail generation. — [Lifewire via mirror](https://file-hippo.netlify.app/host-https-www.lifewire.com/set-live-wallpaper-windows-11-8606934); [Lively v1.0.7.0 notes](https://newreleases.io/project/github/rocksdanister/lively/release/v1.0.7.0); [Lively v1.4.0.0 notes](https://newreleases.io/project/github/rocksdanister/lively/release/v1.4.0.0)
- **GOG Galaxy rollback:** introduced in Galaxy 1.1 (2015) to "restore a game to a previous version" after broken patches or mod incompatibility. Not available for every game, and users report a limited number of past builds (one user says 4). — [Engadget](https://www.engadget.com/2015-08-25-gog-galaxy-update-game-rollbacks.html); [Shacknews](https://shacknews.com/article/90969/gog-galaxy-11-update-adds-rollback-patch-restoration); [The Windows Club](https://www.thewindowsclub.com/how-to-rollback-gog-galaxy-game-to-previous-version); [GOG forum, Grim Dawn](https://gog.com/forum/grim_dawn/is_121_patch_rollback_to_120_possible_in_gog_galaxy)
- **Vortex:** a "revert" option exists when collection rules were not applied correctly or the user wants to undo their changes, but forum users note there is no separate undo for revert itself. — [Vortex v1.13.1](https://newreleases.io/project/github/Nexus-Mods/Vortex/release/v1.13.1); [Nexus forums](https://forums.nexusmods.com/topic/9328373-baldurs-gate-3-sorting-load-order)
- **Windhawk:** mod effects are memory patches, so disabling the service and rebooting removes them. This shows "safe reversal" as a design property. — [HN thread](https://hn.svelte.dev/item/45938723)
- **Playnite:** different theme sets per mode (Desktop vs Fullscreen). A theme is installed in one place and selected in settings. — [Playnite docs](https://api.playnite.link/docs/manual/features/themesSupport/installingThemes.html)

### Inferences
- **Three explicit states per item:** *Disponible* → *Installé* → *Actif*. Primary button labels follow the state: "Installer" → "Appliquer" → "Actif ✓" with a secondary "Revenir au précédent".
- **Live preview:** because Caelestia derives colours from the wallpaper, the detail page can render the item's preview cards recoloured with the user's *current* palette, so users see "how it will look on my setup" rather than the author's screenshots only. A "Essayer 30 s" (temporary apply with auto-revert unless confirmed) mirrors the Windows display-resolution confirm pattern. That pattern is common knowledge here, not taken from a source.
- **Rollback:** keep at least the previous version of every item and a snapshot of the active configuration before each apply or update. One-click "Annuler" in the post-apply toast, plus a history list in Réglages ("Historique des changements"). Unlike GOG, retention is local and guaranteed.
- **Media:** a screenshot carousel plus optional short video or GIF (Lively's generated GIF previews), with an autoplay toggle that respects reduced-motion.

### Gaps
- Not verified: Wallpaper Engine's 2026 preview and apply flow, and whether Seelen UI offers a live preview before install.

## 5. Accessibility (keyboard, screen reader, contrast with dynamic colour), FR/EN localization, performance (startup, memory), tray behaviour

### Takeaway
Material's dynamic colour is designed to produce accessible tone pairings *when the algorithm generates the colours*. Custom or brand colours and wallpaper-derived accents applied outside the tonal system lose that guarantee. Performance is now a headline differentiator: Epic publicly admitted its launcher's slowness and is rebuilding for 5× faster startup, and Windhawk's tiny background footprint is cited as a selling point.

### Cited Findings
- **Material 3 dynamic colour:** algorithmic combinations "are designed to meet accessibility standards". Luminance, not hue, keeps elements legible. Each tonal palette has 13 tones. For colours the algorithm did not generate, accessibility cannot be guaranteed. — [Google codelab: designing with accessible colors](https://codelabs.developers.google.com/color-contrast-accessibility)
- **Material Theme Builder:** generates accessible light and dark schemes from a user's wallpaper. — [material-theme-builder GitHub](https://www.Github.com/material-foundation/material-theme-builder)
- **Contrast levels (unverified):** a third-party source claims M3 Expressive defines standard, medium and high contrast levels, with text at 4.5:1 (3:1 for large text). Not confirmed against the official docs. — [skills.sh M3 colour skill](https://skills.sh/shelbeely/shelbeely-agent-skills/material-design-3-color)
- **Contrast in practice:** Modrinth App 0.10.20 (Nov 2025) shipped a release focused on contrast and consistency in light mode, a sign that contrast debt gets noticed. — [Modrinth 0.10.20](https://modrinth.com/news/changelog/app/0.10.20)
- **Input modes:** Playnite's split between a keyboard and mouse Desktop mode and a gamepad Fullscreen mode shows that input modality can be a first-class design axis. — [Playnite theme intro](https://api.playnite.link/docs/tutorials/themes/introduction.html)
- **Performance, Epic:** startup and per-click backend latency are Epic's admitted problem. The target is 5× faster startup. Forum reports describe memory leaks, one user citing about 10 GB in the tray. These are anecdotal and labelled a bug. — [Tom's Hardware](https://www.tomshardware.com/video-games/pc-gaming/epic-knows-its-launcher-sucks-and-is-fully-rebuilding-it-a-faster-more-stable-client-is-on-the-way-with-player-profiles-and-extended-social-features); [Unreal forums](https://forums.unrealengine.com/t/massive-bug-in-epic-games-launcher-vram-usage/2231906/40)
- **Performance, Windhawk and Raycast:** Windhawk's background process uses "a couple of MB". Raycast isolates extensions in a separate process so the main UI stays responsive. — [HN thread](https://hn.svelte.dev/item/45938723); [Raycast blog](https://raycast.com/blog/how-raycast-api-extensions-work)
- **Performance, CurseForge:** criticism focuses on Overwolf's persistent background services. — [GDLauncher guide](https://gdlauncher.com/guides/curseforge-vs-modrinth)

### Inferences
- **Contrast:** derive *all* app UI colours through the M3 tonal system (material-color-utilities) from the wallpaper seed. Never place raw wallpaper pixels or author-supplied hex colours behind text. Offer a "Contraste élevé" switch that maps to a higher contrast level and is enabled automatically when Windows high-contrast or contrast themes are on. Run a WCAG check on text over gradient cards, using the darkest gradient stop.
- **Motion:** the expressive motion and animated shapes must honour Windows "Effets d'animation" (prefers-reduced-motion) and offer an in-app toggle.
- **Keyboard:**
  - every card is focusable, with a visible M3 focus ring;
  - Ctrl+K or "/" focuses search (VS Code and Raycast convention, a design inference);
  - arrow keys move through card grids;
  - Esc closes the detail page;
  - screen-reader labels state the item state ("Installé, mise à jour disponible").
- **Localization:** FR is the source language, with EN complete at launch. Catalogue items carry localized title and description fields, falling back to EN. Dates and sizes are formatted with the system locale (« 12,4 Mo »).
- **Performance budget** (target, not sourced):
  - window interactive in under 1 s from tray, under 2 s cold;
  - no network on the critical path;
  - the closed-to-tray app should use near-zero CPU and a small memory footprint;
  - heavy work (downloads, hashing, repair) runs in the existing helper process, not the UI process (Raycast isolation logic).
- **Tray:**
  - closing the window minimises to tray only if the user opts in during first run or in settings;
  - the tray menu offers Ouvrir, Mises à jour (n), Réparer, Quitter;
  - no tray presence if the user disables it.
  The "forced background service" complaint (Overwolf, Epic) is the main anti-pattern.

### Gaps
- No authoritative source found on screen-reader behaviour of Tauri or WebView2-based launchers, or on contrast tooling for wallpaper-derived palettes on Windows.
- No independent benchmarks of launcher memory or startup were found. Epic figures are company targets, and forum numbers are anecdotal.
- The official M3 contrast-level spec was not fetched.

## 6. Community features: news feed, GitHub/Discord links, sharing configurations, creator pages

### Takeaway
The most valued community features in mod tools are **shareable configurations** (r2modman profile codes, Vortex collections) and **visible creators** (author metadata, profiles, download counts). Both need robust import, update and merge semantics, because these are where reported bugs cluster.

### Cited Findings
- **r2modman:** "Export profiles to easily share both your mods and configs with friends", as a file or a code. Since 3.1.12, an existing profile can be updated from a code or file. A 3.0.23 host change broke import, and 3.0.24 fixed it. — [r2modmanPlus](https://github.com/ebkr/r2modmanPlus); [3.1.12](https://thunderstore.io/c/re-legend/p/ebkr/r2modman/v/3.1.12/); [3.0.24](https://new.thunderstore.io/c/zort/p/ebkr/r2modman/v/3.0.24/)
- **Vortex collections:** reported problems include the collection's load order being ignored on install. The "apply collection rules manually" menu action arrived in 1.13.3. — [Nexus forums](https://forums.nexusmods.com/topic/13495450-vortex-incapable-of-recognizing-installed-mods-as-part-of-the-collection); [Vortex release notes](https://newreleases.io/project/github/Nexus-Mods/Vortex/release/v1.13.1)
- **Creator identity, Windhawk:** mod headers carry author name, GitHub, X and homepage. — [Windhawk wiki](https://github.com/ramensoftware/windhawk/wiki/Creating-a-new-mod)
- **Creator identity, Seelen:** resource IDs are namespaced `@creator/name` for local and community resources. — [Seelen theme guidelines](https://seelen.io/docs/theme-guidelines)
- **Modrinth social features:** the App added groundwork for a Friends system in 0.9.0 and revamped the friends UI in 0.10.13. Modrinth publishes a public per-app changelog feed at modrinth.com/news/changelog. — [Modrinth 0.9.0](https://modrinth.com/news/changelog/app/0.9.0); [Modrinth changelog index](https://modrinth.com/news/changelog)
- **Epic rebuild:** player profiles and extended social features are part of the announced rebuild. — [Tom's Hardware](https://www.tomshardware.com/video-games/pc-gaming/epic-knows-its-launcher-sucks-and-is-fully-rebuilding-it-a-faster-more-stable-client-is-on-the-way-with-player-profiles-and-extended-social-features)
- **Public PR-based contribution (Raycast):** submissions arrive as pull requests on a public repo. One PR notice says initial review is expected within 5 business days, which differs slightly from the guidelines' "first contact within a week". — [Raycast guidelines](https://manual.raycast.com/extensions-guidelines); [Raycast extensions PR](https://github.com/raycast/extensions/pull/21456)

### Inferences
- **"Partager ma config":** export the active Caelestia setup (theme, widget packs with versions, settings, colour source) as a small JSON file or a short code. On import, show a diff ("3 éléments à installer, 2 réglages modifiés") before applying, and snapshot first so the import can be undone. This avoids the r2modman and Vortex failure modes.
- **Actualités tab:** release notes for Caelestia (rendered from GitHub Releases), featured community items, and fixed links (GitHub, Discord, docs, "Signaler un bug"). News must be read-only and dismissible, never modal.
- **Creator page:** avatar, verified links, list of items, total installs, and the last update date (a staleness signal).

### Gaps
- No sources were checked on moderation of comments or ratings in theme stores, or on whether in-app ratings help trust compared with download counts for niche communities.

## 7. Synthesis: proposed information architecture and key screens for the Caelestia app

### Takeaway
The proposal is an M3 Expressive navigation rail (collapsed by default, expandable) with five destinations: **Accueil, Découvrir, Bibliothèque, Actualités, Réglages**. The Caelestia core install is a persistent status card at the top of Accueil, not a sixth tab. Every item moves through *Disponible → Installé → Actif* with preview, permissions, changelog and rollback.

### Cited Findings
- **Rail size and drawer replacement:** 3–7 destinations, and the expanded rail replaces the drawer. — [SAP Fiori M3 rail](https://www.sap.com/design-system/fiori-design-android/v26-1/components/m3-standard-components/navigation-rail/usage); [Liri Fluid docs](https://docs.liri.io/sdk/fluid/develop/classNavigationRail.html)
- **Home and library patterns:** a home dashboard with recent and active items and quick actions, a context-aware sidebar, and libraries with filters including "has update". — [Modrinth 0.9.0](https://modrinth.com/news/changelog/app/0.9.0); [Steam library](https://store.steampowered.com/libraryupdate)
- **Detail-page trust fields:** repo, license, issue tracker, verified publisher, reviews, changelog, compatibility, downloads. — [VS Code](https://code.visualstudio.com/docs/configure/extensions/extension-runtime-security); [Seelen listing](https://seelen.io/resources/bqiiSUFPSTiAixhlJujKOQ/macos-style-alt-taber); [Windhawk HN](https://hn.svelte.dev/item/45938723)

### Inferences
Proposed IA (labels in French):

1. **Accueil**
   - Hero card "Caelestia": installed version, status (À jour / Mise à jour dispo / Problème détecté), and buttons Mettre à jour · Réparer · Ouvrir le dossier. If Caelestia is absent, the card is "Installer Caelestia" with size, duration and what will be modified.
   - "Votre configuration": current wallpaper and palette swatch, active theme and widget packs as chips, "Changer de fond d'écran".
   - Shelves: "Mises à jour disponibles", "Récemment installés", "À la une" (curated, cached offline), and "Nouveautés de Caelestia" (latest release note excerpt).
2. **Découvrir**
   - Persistent search (Ctrl+K) with filter chips: Type (Thème, Pack de widgets, Fond d'écran, Preset), Compatibilité (version de Caelestia), Tri (Populaires, Récents, Mis à jour), "Vérifiés uniquement".
   - Grid of rounded cards: cover recoloured with the user's palette where possible, author with verified badge, install count, and a state chip.
   - **Item detail page:**
     - header with media carousel (screenshots, short video/GIF), title, author link, version, primary button by state, and a "⋯" menu (Signaler, Copier le lien, Voir sur GitHub);
     - tabs Aperçu · Journal des modifications · Permissions · Versions;
     - Permissions lists what the item modifies or runs: files, scripts, Zebar widgets, startup, network;
     - sidebar facts: license, repo, issues, last update, required Caelestia version, size.
   - **Install flow:**
     - button → inline progress in the button and in a global "Téléchargements" indicator;
     - on first install from a new author: "Faire confiance à cet auteur ?" with profile links (VS Code 1.97 pattern);
     - when done: a toast "Installé — Appliquer maintenant ? / Plus tard".
3. **Bibliothèque**
   - Segments: Tous · Thèmes · Widgets · Fonds d'écran · Configurations partagées.
   - Filters: "Mises à jour dispo", "Actif", "Désactivé".
   - Row or card actions: Appliquer, Configurer (renders the item's settings schema), Mettre à jour, Revenir à la version précédente, Désactiver, Désinstaller (confirmation listing what will be removed, plus a "Garder mes réglages" checkbox).
   - "Tout mettre à jour" and "Exporter ma configuration" / "Importer" (shows a diff before applying).
4. **Actualités**
   - Release notes for Caelestia (from GitHub Releases), featured community items, and creator spotlights.
   - Fixed links card: GitHub, Discord, Documentation, Signaler un bug (pre-filled with app version and logs on consent).
5. **Réglages** (footer of the rail)
   - **Thème actif:** settings of the active theme (colour source, scheme variant, contrast, motion intensity, corner radius, per-widget toggles), with live preview and Annuler/Appliquer.
   - **Application:** language FR/EN, launch at startup, close to tray (off by default), update policy (notify / auto for verified non-script items / manual), check frequency, offline cache size.
   - **Maintenance:** Réparer Caelestia, Historique des changements (snapshots with one-click restore), Exporter les journaux, Réinitialiser, Désinstaller Caelestia (separate destructive zone with a full summary).
   - **Accessibilité:** contraste élevé, réduire les animations (follows Windows by default), taille du texte.
   - **À propos:** versions, licences, confidentialité (no telemetry by default, or opt-in).

Cross-cutting rules:
- no account required, no ads;
- the network is never on the startup path;
- every destructive or applying action has an undo toast and a snapshot;
- one tray toast per update batch;
- all colours go through the M3 tonal system;
- reduced-motion is honoured;
- FR is the default locale.

### Gaps
- This IA is a synthesis and has not been user-tested. Validate it with a short usability test: 5 customisation enthusiasts, covering the tasks install, apply, revert, share config and repair.
- Not researched: how much catalogue curation ("À la une") a small community can sustain, and the moderation load of reports and ratings.
