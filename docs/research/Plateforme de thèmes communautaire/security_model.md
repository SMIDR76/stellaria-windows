# Security model for a community catalog of Caelestia/Zebar widget packs

Research date: 2026-11-16. Scope: threats and proven mitigations for community-distributed widget packs (HTML/CSS/JS in Zebar's Tauri/WebView2 windows) that can reach Zebar's privileged commands and the Caelestia .NET helper (WebSocket on 127.0.0.1, per-session token). It ends with a concrete layered design.

Method note: Zebar's behaviour was checked against its source on `main` (via the GitHub API) on 2026-11-16. Caelestia's helper behaviour was checked against this repository (`docs/protocol.md`, `helper/tests/...`). A tool rate limit hit midway through the research, so the TUF, Sigstore, minisign, VS Code Marketplace and GNOME review pages could not be fetched. Claims about them are marked **[not re-fetched]**: they come from well-established public docs whose canonical URLs are given, and they should be re-checked before the final report quotes them verbatim.

## 1. Threat model: what a malicious or compromised pack can do in this architecture

### Takeaway
A pack runs arbitrary JS in a WebView that shares the origin of every other Zebar pack (`http://127.0.0.1:6124`). The pack can therefore exfiltrate anything it can read with `fetch`. It can reach Zebar's shell commands if its own `zpack.json` grants them. If it gets the Caelestia token, it can drive every helper method. The sensitive helper surfaces are clipboard history, notifications and window titles (the keylogger equivalents), plus app launch and power actions (code execution and DoS). The distribution channel (author accounts, updates, look-alike names) is as important as the runtime.

### Cited Findings
- Zebar widget windows get a Tauri capability that applies to all windows labelled `widget-*` and to remote URLs `http://asset.localhost`, `asset://localhost` and `http://127.0.0.1:6124`. It grants the core app, event and path defaults, `dialog:allow-message`, and many `core:window:*` setters (position, size, always-on-top, ignore-cursor-events, and so on). The capability is identical for every pack. — [zebar/packages/desktop/capabilities/widget.json](https://github.com/glzr-io/zebar/blob/main/packages/desktop/capabilities/widget.json)
- Zebar serves widget files from a local asset server on fixed port 6124. Each pack directory gets a UUID token stored in an `HttpOnly`, `SameSite=Strict` cookie named `ZEBAR_TOKEN`, and the token maps to a base directory plus file patterns. So all packs share the origin `http://127.0.0.1:6124`, and file access is scoped per directory. — [zebar/packages/desktop/src/asset_server.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/asset_server.rs)
- Zebar exposes the Tauri commands `shell_exec` and `shell_spawn`, which run a program for the calling widget (identified by `window.label()`). It also exposes `shell_write(pid, buffer)`, which takes only a PID and has no widget parameter in its signature. — [zebar/packages/desktop/src/commands.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/commands.rs)
- Widgets read and act on the system through "providers", for example `audio` with a `setVolume` function, `glazewm` with `runCommand` ("Invokes a WM command"), `network`, `systray`, `komorebi`, and so on. — [glzr-io/zebar README](https://github.com/glzr-io/zebar)
- The Caelestia helper writes `{ protocolVersion, port, token (64 hex), pid }` to `%USERPROFILE%\.glzr\zebar\caelestia\runtime.json` at every start. Widgets read it with `fetch('./runtime.json')`, then send `auth` with the token and a **self-declared** `widget` name and `monitor`. — `docs/protocol.md` and `docs/configuration.md` in this repo
- The helper's WebSocket handshake accepts only the origin `http://127.0.0.1:6124` and the exact Host `127.0.0.1:<port>`. Foreign origins, `https://127.0.0.1:6124` and DNS-rebinding hosts are rejected. — `helper/tests/Caelestia.Helper.Tests/Server/WebSocketHandshakeTests.cs` in this repo
- WebView2 guidance: "when hosting web content in a native application, the web content can access the native application's resources and APIs", so apps must "Treat all web content as insecure", "Validate web messages and host object parameters", and "Avoid generic proxies". — [Microsoft Learn, Develop secure WebView2 apps (updated 2026-09-14)](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security)

### Inferences
- **The origin check does not separate packs.** Every Zebar pack, official or community, loads from `http://127.0.0.1:6124`, so the Origin header the helper checks is identical for all of them. The `widget` field in `auth` is self-asserted. Today the session token is the only boundary, and it gives all-or-nothing access to every helper method.
- **Token exposure paths.** Zebar's per-directory asset tokens mean a different pack probably cannot `fetch('../caelestia/runtime.json')`. This needs testing with a path-traversal attempt against the asset server. However, any pack with a broad shell privilege (for example `powershell` with a permissive regex), or any other local process, can read the file. A pack installed *inside* the Caelestia pack directory, or a Caelestia "sub-theme" that ships JS loaded from that directory, inherits the token.
- **Threat catalogue specific to Caelestia:**
  - *Exfiltration*: nothing restricts outbound `fetch`/`WebSocket`/`<img src>` to arbitrary hosts (no CSP on widget pages, see Q3). Anything a pack can read can leave the machine.
  - *Keylogging-equivalent*: the helper has no keyboard hook. Clipboard history (passwords, 2FA codes, crypto addresses), notification contents (OTP SMS mirrored to Windows, chat messages) and window titles (document names, URLs) are the high-value streams. A pack subscribed to those events is a spyware agent.
  - *Clipboard hijacking*: if any write or paste method exists or is added, crypto-address swapping is the classic abuse.
  - *Launching programs*: an app-launch method or a Zebar `shellCommands` grant equals arbitrary code execution as the user. From there, persistence (Run keys, scheduled tasks, Startup folder) and full infostealer behaviour follow.
  - *Power actions*: shutdown, restart or sleep loops are a DoS or annoyance vector.
  - *UI spoofing*: `core:window:*` lets a widget make an always-on-top, click-through or fullscreen window. A pack could draw a fake UAC, Windows Hello or browser login prompt (credential phishing). This needs no helper access.
  - *Self-persistence inside Zebar*: a pack can fetch remote JS at runtime (dynamic `import()` of a URL, or `eval` of fetched text). The reviewed code can then differ from the executed code, which defeats review. This is the "malicious update after approval" problem without even shipping an update.
  - *Supply chain*: author account takeover (phishing or consent-phishing, stolen tokens), malicious version bumps, typosquatted pack names, ownership transfer or sale of a popular pack, and compromised npm dependencies bundled into a pack.

### Gaps
- Not verified whether Zebar's asset server rejects `..` traversal or cross-pack paths. `asset_server.rs` was only grepped.
- Not verified whether `shell_write` checks that the PID belongs to the calling widget elsewhere (in `ShellState::write`). If it doesn't, any widget could write to another widget's spawned process stdin.
- Not verified whether Zebar widget windows can navigate themselves to an arbitrary remote URL. Tauri's `remote.urls` would not grant IPC there, but the page would still run.

## 2. Real incidents (2023–2026) and their lessons

### Takeaway
Every major extension ecosystem has shipped malware through three channels: (a) malicious-from-day-one uploads that pass light review (themes included), (b) legitimate publishers whose accounts or tokens were stolen, followed by a malicious auto-update, and (c) worms that reuse stolen publishing tokens to infect more packages. The defences that worked were short auto-update exposure windows, fast central takedown, publish-token hygiene and MFA. Content review alone did not catch invisible or obfuscated code.

### Cited Findings
- **GlassWorm (Open VSX / VS Code, Oct 2025 onward).** Koi Security found it on 2025-10-17. The initial wave hit 7 Open VSX extensions (other counts: 13 Open VSX + 1 Microsoft Marketplace, about 35,800 downloads). It hid code with invisible Unicode characters, stole GitHub, Git and Open VSX credentials, and used stolen Open VSX tokens to publish more malicious extensions. It targeted 49 crypto-wallet extensions. — [SecurityWeek](https://www.securityweek.com/supply-chain-attack-targets-vs-code-extensions-with-glassworm-malware/); [heise](https://heise.de/-10790065); [CSO Online](https://www.csoonline.com/article/4076718/self-propagating-worm-found-in-marketplaces-for-visual-studio-code-extensions-2.html)
  - Open VSX said it removed the extensions and that the incident was contained as of 2025-10-21. It disputed the "self-replicating worm" label. — [SecurityWeek, "Open VSX downplays impact"](https://www.securityweek.com/open-vsx-downplays-impact-from-glassworm-campaign/)
  - It resurfaced on Open VSX in Nov 2025 and appeared in GitHub repositories (Aikido). — [SecurityWeek, "GlassWorm returns"](https://securityweek.com/glassworm-malware-returns-to-open-vsx-emerges-on-github); [Security Affairs](https://securityaffairs.com/?p=184427)
  - A further wave targeted macOS, reported 2026-01-02 as the fourth wave. — [Red Hot Cyber](https://www.redhotcyber.com/?p=149668)
  - Socket reported a cluster of 73 "sleeper" extensions linked to the same family. Source detail is from search snippets only. — [Dark Reading](https://www.darkreading.com/application-security/fresh-glassworm-vs-code-extensions-supply-chain)
- **Cyberhaven and the Chrome Web Store campaign (Dec 2024).** A consent-phishing email impersonating Chrome Web Store support led an employee with publish rights to authorise a malicious OAuth app ("Privacy Policy Extension"). The attacker published malicious version 24.10.4, which auto-updated to Chrome users between 2024-12-25 01:32 UTC and 2024-12-26 02:50 UTC (about 31 h) and targeted Facebook business/ads data. About 400,000 users were affected. At least 16 extensions were compromised in the same campaign, affecting more than 600,000 users. The campaign was traced back to at least Mar 2024 or Dec 2023. — [Security Affairs](https://securityaffairs.com/172491/hacking/chrome-browser-extensions-compromise.html); [Island](https://www.island.io/info/cyberhaven-and-extension-takeover-incident); [RH-ISAC](https://rhisac.org/threat-intelligence/cyberhaven-extension-compromise-part-of-broader-campaign-affecting-multiple-chrome-extensions/); [DomainTools](https://dti.domaintools.com/cyberhaven-breach-likely-part-of-a-long-term-criminal-campaign/)
- **Shai-Hulud (npm, Sep 2025).** First compromises came on 2025-09-14 and the main wave on 15–16 Sep. More than 500 packages were affected, including `@ctrl/tinycolor` (2M+ weekly downloads) and CrowdStrike packages. Install-time code harvested tokens and reused npm tokens to republish the victims' other packages. GitHub announced npm publishing changes on 2025-09-22, and CISA issued an alert on 2025-09-23. — [The Hacker News](https://thehackernews.com/2025/09/40-npm-packages-compromised-in-supply.html); [SOCRadar](https://socradar.io/shai-hulud-npm-supply-chain-attack-what-you-need-to-know/); [Wiz](https://threats.wiz.io/all-incidents/shai-hulud-ongoing-package-supply-chain-compromise-delivering-data-stealing-malware)
- **Cosmetic packages are not harmless.** A VS Code *theme* extension, "Theme Darcula dark", was reported collecting system information and had more than 45,000 downloads. This comes from a search-result summary. The original ReversingLabs report (2023) was not fetched. — [TechRadar](https://techradar.com/news/hackers-are-using-malicious-microsoft-vscode-extensions-to-steal-passwords)
- **BetterDiscord.** JFrog-reported npm packages posing as a "Solara executor" dropped the Cipher stealer (Discord tokens, browser passwords, wallets). On machines with BetterDiscord, the stealer patched its files to disable webhook protections. This shows that client-mod ecosystems become persistence targets. — [Cyber Security News](https://cybersecuritynews.com/malicious-npm-packages-target-discord-browsers-and-crypto-wallets/amp/); [VPNCentral](https://vpncentral.com/?p=246639)
- **Obsidian.** The help page says Obsidian "cannot reliably restrict plugins to specific permissions or access levels" for technical reasons. Restricted Mode, which blocks third-party code, is on by default. The current policy says every plugin version is scanned automatically. An older version said the team cannot manually review every release, and manual review is kept for popular, featured or flagged plugins. The directory plans capability disclosures and verified-author badges. A 2026 "Phantom Pulse" RAT campaign got victims to enable syncing of installed community plugins. — [Obsidian Help, Plugin security](https://obsidian.md/help/plugin-security); [dev.to analysis](https://dev.to/pickuma/phantom-pulse-rat-hits-obsidian-plugins-how-to-audit-dev-tool-supply-chains-488c)
- **Windhawk (Windows customisation, closest analogue).** Mods are C++ source files in the public `ramensoftware/windhawk-mods` repo, compiled into DLLs and injected into target processes. Since v1.7 Windhawk downloads **pre-compiled** mods by default, and local compilation can be re-enabled in settings. Submissions go through an AI review stage and then human review. Commentators note that this is not an independent audit or reproducible verification of the downloaded binary. — [Windhawk site](https://ramensoftware.com/windhawk); [Windhawk README](https://cdn.jsdelivr.net/gh/ramensoftware/windhawk@main/README.md); [Gridinsoft, "Is Windhawk safe?"](https://blog.gridinsoft.com/windhawk-safety/) (secondary source); [WindowsForum](https://windowsforum.com/news/windhawk-on-windows-11-customization-vs-risk.444400/)

### Inferences
- **Lesson for Caelestia (Cyberhaven, Shai-Hulud, GlassWorm):** a reviewed pack is only as safe as its next update. Auto-updates must be pinned to catalog-approved hashes and gated by review. A cooling-off delay before an approved update reaches users would have shrunk Cyberhaven's 31-hour window.
- **Lesson (GlassWorm):** static checks must flag invisible or bidirectional Unicode, high-entropy blobs and runtime code loading. Human reviewers reading a diff in a browser do not see zero-width characters.
- **Lesson (Windhawk):** source review only helps if the reviewed source is exactly what runs. For JS packs that means shipping unminified source, or building in CI from a pinned commit and hashing the output.
- **Lesson (Obsidian):** if the host cannot enforce permissions, disclosure is just labelling. Caelestia controls the helper, so it *can* enforce scopes there, which is better than Obsidian's position.
- **Lesson (theme malware):** "it's just a theme" is the social-engineering hook. Treat theme packs as code.

### Gaps
- No public reports were found of malicious **Spicetify** themes or extensions, or of malicious BetterDiscord *plugins* distributed through its official store. The only BetterDiscord case involves external npm malware.
- GNOME Extensions (extensions.gnome.org) review rules could not be fetched (rate limit). Recalled but **[not re-fetched]**: every upload and update is human-reviewed, and the guidelines forbid obfuscated or minified code and require the submitted source to be what runs — [GNOME review guidelines](https://gjs.guide/extensions/review-guidelines/review-guidelines.html). Re-verify before quoting.
- VS Code Marketplace protections (malware scan, publisher verification, signature verification, name-squatting checks, kill-switch blocklist) **[not re-fetched]** — [VS Code extension runtime security](https://code.visualstudio.com/docs/configure/extensions/extension-runtime-security).

## 3. What Zebar itself enforces today for packs

### Takeaway
Zebar enforces exactly one per-widget privilege: a `shellCommands` allowlist of `{ program, argsRegex }` declared by the pack itself in `zpack.json`. It has no network restriction, no CSP on widget pages, no per-pack Tauri capability, and no visible hash or signature verification of marketplace tarballs. The pack author grants the privileges, so on their own they protect nothing against a malicious pack. They only matter if the installer (Caelestia's launcher) controls or vets `zpack.json`.

### Cited Findings
- The `zpack.json` schema: `widgets[].privileges.shellCommands[]` with `program` (string) and `argsRegex` (string). There is no `network` field and the privilege fields have no descriptions. Other widget fields: `htmlPath`, `includeFiles`, `caching.rules[].urlRegex`, `zOrder` (`top_most|normal|bottom_most`), `presets`, and so on. — [zebar/resources/zpack-schema.json](https://github.com/glzr-io/zebar/blob/main/resources/zpack-schema.json)
- `check_shell_privilege` filters privileges with `privilege.program == program` (exact string). If `argsRegex` is empty, only empty args pass. Otherwise it passes when `regex::Regex::new(&privilege.args_regex)` **`is_match`** the joined argument string. `is_match` is a substring search, so the regex is unanchored unless the author writes `^...$`. Shell events are emitted only to the spawning widget. — [zebar/packages/desktop/src/shell_state.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/shell_state.rs)
- Privilege lookup uses the calling window's label, which maps to that widget's own config (`widget.config.privileges.shell_commands`). — [shell_state.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/shell_state.rs); [commands.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/commands.rs)
- The app-level CSP in `tauri.conf.json` (`default-src 'self' asset:`, `connect-src ipc: http://ipc.localhost https://api.glzr.io https://*.supabase.co`, and so on) covers Zebar's bundled settings UI. A grep of `asset_server.rs` for `Content-Security`/`csp` found no CSP header on widget responses. — [zebar/packages/desktop/tauri.conf.json](https://github.com/glzr-io/zebar/blob/main/packages/desktop/tauri.conf.json); [asset_server.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/asset_server.rs)
- The marketplace installer downloads with `reqwest::get(tarball_url)` and extracts into a per-pack, per-version download dir. A grep for `sha|hash|sign|verify` found no integrity check. — [zebar/packages/desktop/src/marketplace_installer.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/marketplace_installer.rs)
- Publishing: `zebar publish --token <api token from glzr.io/api-token>` publishes under the token owner's account. `--commit-sha`, `--release-notes` and `--release-url` are optional and only displayed. The doc mentions no review step. — [zebar/MARKETPLACE.md](https://github.com/glzr-io/zebar/blob/main/MARKETPLACE.md)
- Packs live one level deep under `~/.glzr/zebar/<pack-id>/zpack.json`. Marketplace downloads go to `%AppData%/zebar/downloads/`. — [glzr-io/zebar README](https://github.com/glzr-io/zebar)

### Inferences
- **Practical consequence:** `{ "program": "powershell", "argsRegex": ".*" }` (or any unanchored regex containing a common token) gives a pack full code execution. The launcher must treat any `shellCommands` entry in a community pack as a top-tier permission, or strip it.
- Since there is no CSP header from the asset server, the only way to constrain a community widget's network today is for the launcher to inject a `<meta http-equiv="Content-Security-Policy">` into the widget HTML at install time, or to contribute per-pack CSP or capabilities upstream to Zebar.
- Since Zebar's marketplace does no integrity checks and has no visible review, Caelestia's catalog should not reuse it as a trust anchor. It should download, verify and place files itself.

### Gaps
- Not checked whether Zebar's settings UI shows the declared `privileges` to the user before installing a marketplace pack (no consent prompt was found in the files inspected).
- Not checked whether Zebar re-reads `zpack.json` at runtime, which would let a pack that can write files escalate its own privileges, for example via a shell grant.

## 4. Runtime isolation mitigations: Tauri capabilities, CSP, WebView2

### Takeaway
Tauri v2 can scope commands per window label and per remote origin. Zebar applies one shared capability to all `widget-*` windows, so Caelestia cannot get per-pack Tauri isolation without changing Zebar. The realistic runtime controls Caelestia owns are: (1) a launcher-injected CSP that locks `connect-src` to the helper plus declared domains and bans `unsafe-eval` and remote scripts, and (2) enforcement inside the helper itself, which is the one boundary Caelestia fully controls.

### Cited Findings
- Tauri capabilities grant permissions to windows or webviews by **label** ("The security boundaries depend on window labels (not titles)"). If a window matches several capabilities, they "effectively merge the security boundaries and permissions of all involved capabilities". — [Tauri v2, Capabilities](https://v2.tauri.app/security/capabilities/)
- By default the API is "only accessible to bundled code shipped with the Tauri App". `remote.urls` extends it to remote origins with the warning "Please consider usage of this feature very carefully". On Linux and Android, Tauri cannot distinguish an embedded `<iframe>` from the window. The boundary does not protect against "Too lax scopes and configuration" or "Malicious or insecure Rust code". Custom commands registered in the invoke handler are callable from **all windows** by default unless restricted through `AppManifest::commands` in `build.rs`. — [Tauri v2, Capabilities](https://v2.tauri.app/security/capabilities/)
- Zebar uses `remote.urls: ["http://127.0.0.1:6124", ...]` and `windows: ["widget-*"]` for all widgets. — [widget.json](https://github.com/glzr-io/zebar/blob/main/packages/desktop/capabilities/widget.json)
- WebView2: check the document origin before sending data in, design specific messages instead of generic proxies, validate every message parameter, disable unused features (`AreHostObjectsAllowed`, `IsWebMessageEnabled`, and so on), filter `NavigationStarting`/`FrameNavigationStarting`, and run the WebView2 host non-elevated, isolating elevated work in a separate process. — [Microsoft Learn, WebView2 security](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security)
- Tauri's own CSP mechanism exists, and Zebar sets it for its settings UI. — [zebar tauri.conf.json](https://github.com/glzr-io/zebar/blob/main/packages/desktop/tauri.conf.json)

### Inferences
- The WebView2 rule "avoid generic proxies" applies directly to the Caelestia helper. Methods such as "run arbitrary PowerShell" or "launch any path" are generic proxies and should not exist for community scopes. "Launch app by catalog AUMID chosen by the user in a picker" is specific.
- A per-pack CSP of the form `default-src 'self'; script-src 'self'; connect-src 'self' ws://127.0.0.1:<helperPort> <declared hosts>; img-src 'self' data: <declared hosts>; object-src 'none'; base-uri 'none'; form-action 'none'` blocks exfiltration via fetch, XHR, WebSocket and images, and blocks runtime code loading. The helper port changes per start, so a wildcard port or a fixed helper port is needed (CSP allows `ws://127.0.0.1:*`). A meta CSP does not cover `frame-ancestors` or reporting, and it only applies if the launcher controls the HTML entry file. Pack JS cannot remove it after parsing, but a pack could open a new window or navigate. That behaviour in Zebar is unverified.
- The helper must stay non-elevated (WebView2 guidance), with power actions and other privileged operations brokered behind explicit scopes.

### Gaps
- Not verified: whether WebView2 in Zebar honours `<meta>` CSP injected into widget HTML (it should, since it is Chromium), and whether Zebar's `zebar` client library needs `unsafe-eval` or inline scripts.
- Not verified: whether Zebar would accept an upstream change for per-pack CSP or capability scoping. No discussion was found.

## 5. Catalog and supply-chain mitigations: signing, TUF, review, revocation

### Takeaway
Use a signed, hash-pinned index (TUF concepts: offline root, targets, snapshot, timestamp with short expiry), minisign/Ed25519 signatures, and optional Sigstore provenance from CI. Add mandatory human review for the first submission and for every permission increase, CI static analysis aimed at what defeated reviewers (invisible Unicode, obfuscation, runtime code loading, undeclared network hosts), name-similarity checks, and a signed revocation list that the launcher acts on.

### Cited Findings
- The Cyberhaven malicious version reached users through automatic updates within about 31 h of a stolen publish authorisation. Store-side review did not stop it. — [Security Affairs](https://securityaffairs.com/172491/hacking/chrome-browser-extensions-compromise.html); [Island](https://www.island.io/info/cyberhaven-and-extension-takeover-incident)
- Shai-Hulud and GlassWorm spread by reusing stolen **publishing tokens**. Recommended responses: rotate tokens, use phishing-resistant MFA, pin safe versions. GitHub changed npm publishing on 2025-09-22. — [SOCRadar](https://socradar.io/shai-hulud-npm-supply-chain-attack-what-you-need-to-know/); [SecurityWeek](https://www.securityweek.com/supply-chain-attack-targets-vs-code-extensions-with-glassworm-malware/)
- Zebar's marketplace has no visible tarball integrity check, and its publish flow relies on a bearer API token. — [marketplace_installer.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/marketplace_installer.rs); [MARKETPLACE.md](https://github.com/glzr-io/zebar/blob/main/MARKETPLACE.md)
- **[not re-fetched]** TUF separates responsibilities into roles. *Root* (offline, threshold-signed) delegates keys. *Targets* lists file hashes and lengths and can delegate to other parties. *Snapshot* pins the versions of all targets metadata, which prevents mix-and-match. *Timestamp* is a frequently re-signed, short-expiry file that prevents freeze attacks. Version numbers prevent rollback. Thresholds and key rotation let the system survive the loss of a single key. — [TUF overview](https://theupdateframework.io/docs/overview/); [TUF specification](https://theupdateframework.github.io/specification/latest/)
- **[not re-fetched]** minisign signs files with Ed25519. A trusted comment is included in the signature, and a public key is a short base64 string embeddable in an app. — [minisign](https://jedisct1.github.io/minisign/)
- **[not re-fetched]** Sigstore "keyless" signing: Fulcio issues a short-lived certificate bound to an OIDC identity (for example a GitHub Actions workflow), and Rekor records the signature in a public transparency log. GitHub artifact attestations build on this. — [Sigstore docs](https://docs.sigstore.dev/); [GitHub artifact attestations](https://docs.github.com/en/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds)
- Windhawk shows a working model for a small project: all mod source in one public repo, AI review followed by human review, and binaries built centrally. — [Gridinsoft](https://blog.gridinsoft.com/windhawk-safety/); [Windhawk](https://ramensoftware.com/windhawk)

### Inferences
- A full TUF deployment is heavy for a hobby catalog. A **TUF-lite** subset still captures most of the value: one offline root key (or 2-of-3) that signs the online index key; `index.json` with per-pack `{id, version, sha256, size, permissions, tier, reviewedCommit}`; a `timestamp.json` with a short expiry (for example 7 days) and a monotonic version so the launcher rejects rollback and stale feeds; all files signed with minisign/Ed25519, with the root public key embedded in the launcher.
- Packages should be built by catalog CI from the author's repo at a **pinned commit SHA**, not uploaded as author-built tarballs. CI emits the tarball, its SHA-256 and a GitHub attestation. The reviewed diff is then exactly what ships (the lesson Windhawk's critics raise).
- Because author tokens are the main takeover vector, the catalog should **not** accept direct publishes. Updates arrive as PRs to the catalog repo that bump the pinned commit. A compromised author GitHub account then only produces a PR that still needs maintainer review.
- Typosquatting: reject new pack IDs or display names within a small edit distance (or confusable-Unicode match) of existing ones, and show a verified-author badge only for authors linked to an existing reputation.
- Static analysis rules worth having, each tied to an incident: invisible or bidi Unicode (GlassWorm); `eval`, `new Function`, string `setTimeout`, dynamic `import()` of non-relative URLs, `<script src=http...>` (remote code); high-entropy or very long single-line literals and minified files without source (obfuscation); URL literals not in the declared network allowlist (exfiltration); `zpack.json` with `shellCommands` or unanchored `argsRegex`; `includeFiles` outside the pack dir; bundled `node_modules` without a lockfile, scanned against OSV/Socket feeds (Shai-Hulud).

### Gaps
- No .NET TUF client library was evaluated (python-tuf, go-tuf, rust-tuf/`tough` exist **[not re-fetched]**). For a .NET launcher, minisign verification (Ed25519 via `NSec` or BouncyCastle) plus a hand-rolled TUF-lite may be simpler. This needs a separate check.
- Exact current Chrome Web Store behaviour (disabling an extension when an update requests new permissions until the user re-approves) is recalled, not fetched **[not re-fetched]** — [Chrome extension permission warnings](https://developer.chrome.com/docs/extensions/develop/concepts/permission-warnings).

## 6. Per-pack authorization for the helper API

### Takeaway
Because origin and `widget` name are shared or self-asserted, per-pack authorization has to rest on a **per-pack secret delivered through a channel only that pack can read**. Zebar's per-directory asset-server scoping provides such a channel: a token file written into each pack's own directory. The helper maps token to pack ID to granted scopes, enforces a method and event allowlist per scope, and treats clipboard, notifications, window titles, launch and power as high-risk scopes that need explicit user consent.

### Cited Findings
- Zebar asset tokens map to a pack's base directory and file patterns, so one pack's page is served only its own directory's files. — [asset_server.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/asset_server.rs)
- The current helper protocol uses one session token in `caelestia/runtime.json`, read via `fetch('./runtime.json')`, and a self-declared `widget` in `auth`. — `docs/protocol.md` in this repo
- The helper's origin allowlist is `http://127.0.0.1:6124` only, with an exact Host check against DNS rebinding. — `helper/tests/Caelestia.Helper.Tests/Server/WebSocketHandshakeTests.cs` in this repo
- WebView2 guidance: specific messages, no generic proxies, validate all parameters. — [Microsoft Learn](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security)
- Tauri's model is an analogue: per-label capabilities, each grant naming specific commands and scopes. — [Tauri v2, Capabilities](https://v2.tauri.app/security/capabilities/)

### Inferences
- **Mechanism.** At startup (and on install or uninstall) the helper writes `runtime.json` containing `{port, packId, token}` into **each installed pack's own directory**. It uses a different 256-bit random token per pack, regenerated per session, and the file is ACL'd to the user. The helper keeps `token -> {packId, tier, grantedScopes}`. `auth` no longer trusts the client's `widget` claim for authorization (keep it only for logging and monitor routing). Official Caelestia packs get their tokens the same way, so there is one code path.
- **Residual risk.** Any process running as the user can read every token file. That is acceptable: same-user native malware has already won. A community pack with a broad `shellCommands` grant can read other packs' tokens, which is why community packs must not get shell grants.
- **Scope catalogue (suggested).** Each scope lists the methods *and* events it unlocks:
  - Low risk, granted silently: `theme.read` (Windows accent colours), `media.read`, `audio.read`, `power.status`, `monitors.read`.
  - Medium risk, shown at install: `media.control`, `audio.control`, `wallpaper.set`, `windows.list` (titles leak activity), `windows.focus`.
  - High risk, explicit consent with a warning, never for unreviewed packs: `clipboard.read` (history), `clipboard.write`, `notifications.read`, `apps.launch` (only by AUMID or Start-menu entry, never arbitrary paths or args), `power.actions` (sleep, restart, shutdown with a helper-side confirmation toast or rate limit).
- **Enforcement details:** check scope on every request *and* filter every pushed event by scope. Reject unknown methods. Validate parameters strictly. Rate-limit the high-risk methods. Keep an audit log per pack ("pack X read clipboard 14 times today") that the launcher can display. Consider redacting clipboard and notification contents for community packs (counts, or app name only) unless the high-risk scope is granted.
- **Consent UX.** The launcher shows scopes before install. An update that adds scopes is not applied until the user re-approves, mirroring Chrome's permission-escalation behaviour **[not re-fetched]**.

### Gaps
- Not designed here: how a pack running on several monitors (several windows, same pack ID) shares one token. It should work naturally, since the file is per pack.
- Not verified: whether Zebar lets a widget `fetch` files from its pack directory that were written after the pack was loaded (the asset token holds file patterns, which may be fixed at load).

## 7. Concrete layered security design for Caelestia community packs

### Takeaway
Defence in depth across six layers: trust tiers, signed hash-pinned catalog, CI-built and reviewed submissions, install-time consent, runtime confinement (CSP plus helper scopes, no shell for community packs), and revocation and monitoring. The single most important property: **community code never holds the master helper token, and never gets Zebar `shellCommands`.**

### Cited Findings
- Based on the sources in sections 1–6, in particular [Zebar shell_state.rs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/src/shell_state.rs), [Zebar widget.json](https://github.com/glzr-io/zebar/blob/main/packages/desktop/capabilities/widget.json), [Microsoft WebView2 security](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security), [Tauri v2 capabilities](https://v2.tauri.app/security/capabilities/), and the incident sources for [Cyberhaven](https://securityaffairs.com/172491/hacking/chrome-browser-extensions-compromise.html), [GlassWorm](https://www.securityweek.com/supply-chain-attack-targets-vs-code-extensions-with-glassworm-malware/) and [Shai-Hulud](https://thehackernews.com/2025/09/40-npm-packages-compromised-in-supply.html).

### Inferences

**Layer 0 – Trust tiers**
- *Official*: built and signed by Caelestia CI, may request any scope, may use `shellCommands` that Caelestia itself controls.
- *Verified community*: listed in the catalog, CI-built from a pinned commit, human-reviewed, limited to low and medium scopes by default. High-risk scopes need a second reviewer and an explicit user prompt. No `shellCommands`.
- *Local / sideloaded*: the developer-mode toggle in the launcher, shown with a persistent "unreviewed" badge. Low scopes only unless the user grants more manually. Never auto-updated.

**Layer 1 – Catalog integrity (TUF-lite)**
- `root.pub` is embedded in the launcher. The root key is offline (hardware key or 2-of-3), and it signs `keys.json` listing the online index key or keys.
- `index.json`: per pack `{id, displayName, author, tier, version, sha256, size, reviewedCommit, scopes[], networkHosts[], revoked?}`, signed with minisign/Ed25519 and carrying a monotonically increasing `version`.
- `timestamp.json` re-signed daily with about 7-day expiry. The launcher refuses expired or rolled-back metadata and pins the downloaded tarball by `sha256` and `size`. Optional: a GitHub attestation (Sigstore) for each tarball as provenance, checked in CI and shown in the UI, not required at runtime.

**Layer 2 – Submission and update pipeline**
- Authors open a PR to the catalog repo pointing to `repo@commitSHA`. There is no token-based publishing, which removes the Shai-Hulud and GlassWorm stolen-token path.
- CI fetches that commit, runs the lint suite (invisible Unicode, eval or remote code, obfuscation and entropy heuristics, undeclared URLs, `zpack.json` privileges, file layout, dependency audit), builds a deterministic tarball (sorted entries, fixed mtimes), and posts the hash and a human-readable **permission diff** and **code diff since the last approved version**.
- A maintainer reviews: everything for the first version, and the diff for updates. Any new scope, network host or ownership change triggers full re-review. Name-similarity check on new IDs.
- After merge, a release delay (for example 48 h) before the index update is signed. The window lets the author or community flag a hijacked release, which is the Cyberhaven lesson.

**Layer 3 – Install and update consent in the launcher**
- Show the tier, author, scopes (in plain language, high-risk ones in red), network hosts and review date. Offer a sandboxed preview: render the pack in the launcher with a **mock helper** that returns fake data, and with network blocked by CSP.
- Updates install silently only if scopes and hosts are unchanged. Otherwise the pack stays on the old version until the user re-approves.

**Layer 4 – Runtime confinement**
- The launcher, not the pack, writes the final `zpack.json` for community packs: it strips `privileges.shellCommands` and rejects `includeFiles` outside the pack.
- The launcher injects a CSP `<meta>` into each widget HTML entry: `default-src 'self'; script-src 'self'; connect-src 'self' ws://127.0.0.1:* <declared hosts>; img-src 'self' data: <declared hosts>; object-src 'none'; base-uri 'none'; form-action 'none'`. CI rejects packs whose code breaks under this CSP.
- The helper gets per-pack tokens written into each pack directory, a scope-to-method/event allowlist, strict parameter validation, rate limits, an audit log, redaction of clipboard and notification content without the high-risk scope, and `apps.launch` only for user-picked Start-menu entries. The helper stays non-elevated.
- Keep the master or legacy single token only for official packs during migration, then remove it.
- Upstream asks to Zebar (optional): a per-pack CSP header from the asset server, anchored `argsRegex` (or full-match semantics), a consent prompt for `privileges` when installing from its marketplace, and hash verification of marketplace tarballs.

**Layer 5 – Revocation and response**
- `index.json` carries `revoked` entries (pack ID plus versions plus reason). The launcher checks at start and daily. On revocation it stops the pack (unloads the Zebar widget), revokes its helper token immediately (the helper reloads its grants), moves the files to quarantine and notifies the user.
- An incident playbook: freeze the index (sign a timestamp with no updates), rotate the online key with the offline root if it is suspected compromised, publish an advisory.

**Layer 6 – Residual risks (accepted or documented)**
- Same-user native malware can read every token. That is out of scope.
- UI spoofing via always-on-top click-through windows is allowed by Zebar's shared capability. Mitigations are review plus a rule that community packs may not use `top_most` fullscreen presets without justification.
- Data a pack legitimately receives (for example media titles) can still leak to its declared hosts. Users see those hosts at install.

### Gaps
- No measurement of the maintainer workload a human-review gate implies. Windhawk's AI-plus-human model is the nearest precedent, with no throughput figures found.
- Pages not re-fetched because of the rate limit: TUF, minisign, Sigstore, GNOME review guidelines, VS Code Marketplace, Chrome permission escalation. They are marked **[not re-fetched]** above and should be verified before the final report quotes them.
