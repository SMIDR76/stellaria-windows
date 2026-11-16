# Professional installer UX on Windows (2026) for Caelestia for Windows

Scope: what makes a modern Windows installer feel professional and trustworthy, applied to an enthusiast-facing, GPL-3.0, per-user theme that installs Zebar, copies files, registers startup tasks, optionally syncs Windows colours, and must uninstall cleanly (restoring the taskbar). Research done 2026-11-16, 23 tool calls. Many Microsoft UX guidelines cited below are the Windows 7-era "UX guide" pages, still live on Microsoft Learn and explicitly flagged by Microsoft as not updated for newer Windows; I found no newer Microsoft document that replaces them for installers.

## 1. Screen flow patterns (welcome, licence, requirements, options, location, progress, finish, errors, rollback)

### Takeaway
Microsoft's own guidance says to drop Welcome, Summary and Congratulations pages, make the first page functional, ask everything up front ("decision phase"), label the final button "Install", show one determinate progress bar with a short step label above it, and on success launch the program instead of a finish page. For GPL software a click-through licence is neither required nor forbidden, so a link or an informational licence view (no "I accept" gate) is the cleaner choice.

### Cited Findings

Overall philosophy
- "Users don't enjoy installing software, so modern setup experiences need to be simple, efficient, and problem-free"; setup is "a necessary evil, to be performed as quickly as possible" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Microsoft flags the guide as written for Windows 7 and "has not been updated for newer versions of Windows", though "much of the guidance still applies in principle" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- "Review your setup design for nonessential questions, options, pages, and paths, and be ruthless about eliminating them"; welcome, summary and congratulations pages "often just add clicks" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Settings that "customize a core experience, or one that is crucial to a user's personal identification with the program" belong at first use of the program, not in setup, because users choose better in context — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Four phases: decision, downloading, installation, completion. All questions in the decision phase; problems during install are queued for the completion phase. If install takes more than a minute, assume users are doing something else — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Design for all scenarios including installing for someone else, unattended, and "even want a graphical user interface for the task" (i.e. scripted) — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)

Welcome / first page
- "Eliminate Welcome pages"; use a "Getting Started" page only to inform about prerequisites, with main instruction "Before you begin:"; if no welcome page, put the program name and description on the first page, and welcoming language is fine if the page has a purpose — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup); [Microsoft Learn, Wizards UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/win-wizards)
- "Whenever practical, provide the ability to perform the prerequisites directly from the setup program" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- "Don't ask users questions about the system state. Detect this information automatically instead" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)

Licence (GPL specifics)
- GNU GPL FAQ #ClickThrough: asking users to click to agree to the GPL "is neither required nor forbidden"; merely receiving/using the software creates no obligations, which arise only when you modify or distribute it — [GNU GPL FAQ](https://www.gnu.org/licenses/gpl-faq.html#ClickThrough)
- GNU GPL FAQ #GPLCompatInstaller: the installer need not be GPL-compatible; "The installer and the files it installs are separate works" — [GNU GPL FAQ](https://www.gnu.org/licenses/gpl-faq.html#GPLCompatInstaller)
- GNU GPL FAQ #WhyMustIInclude: a copy of the licence must travel with every copy of the program so recipients learn their rights; a URL alone may stop working — [GNU GPL FAQ](https://www.gnu.org/licenses/gpl-faq.html#WhyMustIInclude)
- Microsoft: instead of a separate licence page, link to the terms with an "Agree and install" button; link to a local file (not a web page) so it cannot break; offer Print. If a licence page is used: plain language, no all-caps, never pre-select acceptance, do not force scrolling to the bottom — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)

Options / components / scope
- "Prefer to eliminate" the typical/custom scope page; if kept, custom starts from typical defaults. "Strive to have a single page of questions and options" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Defaults: "select the most secure and private response to be the default"; strongly recommended options can be pre-selected or labelled "(recommended)" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- "Don't force users to opt out of installing optional features. Allow them to opt in instead" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Wording example: say "Install only for me", not "Restrict installation on a per-user basis" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Wizard anti-pattern: "The ultimate wizard experience isn't Next, Next, Next, Next, Finish"; a page that requires no thought "probably doesn't need to be in the wizard at all"; but don't cram pages ("burrito wizard") — [Microsoft Learn, Wizards UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/win-wizards)
- Don't disable a positive commit button without an obvious reason; preserve selections across Back/Next — [Microsoft Learn, Wizards UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/win-wizards)

Install location
- "Don't ask questions about unimportant details"; the guide shows an install-location page as an "Incorrect" example to eliminate. If an editable path is shown, give it a Browse button — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Velopack (Squirrel successor) one-click installers ask no questions and install to `%LocalAppData%\{packId}`, overridable only via `Setup.exe --installto {dir}` — [Velopack docs, Installer](https://docs.velopack.io/packaging/installer)

Commit and progress
- "For the final input page, label the commit button Install, not Next" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Always show a progress page; disable Back/Next during progress but keep Cancel enabled and responsive; single determinate bar that never restarts and doesn't hit 100% before done; avoid "brinksmanship" (stuck at 99%) — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Step label above the bar, a verb fragment with ellipsis ("Copying files...", "Installing required components..."); no GUIDs or file names; optionally a brief statement of expected total time — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Progress pages should auto-advance on success and only stay if there is a problem — [Microsoft Learn, Wizards UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/win-wizards)

Finish
- When installing a single program interactively, start the program and close setup instead of showing a completion page. Exceptions: command-line, auto-update, Group Policy and IT-pro installs should not start programs — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Use a Follow-Up/Completion page only if there are real follow-up steps; use "Close" (not "Done"/"Finish"/"Cancel") on that page — [Microsoft Learn, Wizards UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/win-wizards)
- Velopack's one-click setup "will run App Hooks, as well as launch the application for the first time once installation has completed" — [Velopack docs, Installer](https://docs.velopack.io/packaging/installer)

Errors, logs, restart, cancel
- On significant failure show an error page (not a dialog) with an error icon, plain explanation and practical steps; minor recoverable problems shown as an extra task with positive language, no error icon — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Keep support details out of the UI; "Create log files to capture information useful to technical support"; consider a Repair option; link errors to help/troubleshooting content — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Don't confirm cancellation; allow restarting where the user left off if setup is long — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Avoid restarting Windows; use Restart Manager to close apps holding files; if restart unavoidable, single restart, user decides when, and treat "restart later" as success — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Inno Setup supports `/CLOSEAPPLICATIONS` and `/RESTARTAPPLICATIONS` (closes and restarts apps holding files to be updated) — [Inno Setup help, command line](https://jrsoftware.org/ishelp/topic_setupcmdline.htm)
- Inno Setup exit code 5 = user cancelled during install or chose Abort; 4 = fatal error during installation; 7 = "Preparing to Install" stage determined Setup cannot proceed — [Inno Setup help, exit codes](https://jrsoftware.org/ishelp/topic_setupexitcodes.htm)

Requirement checks (WebView2)
- WebView2 Evergreen Runtime "will be included as part of the Windows 11 operating system"; "the vast majority" of Windows 10 devices have it but a small number don't, so check anyway — [Microsoft Learn, WebView2 distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution)
- Detection: `pv (REG_SZ)` > 0.0.0.0 under `HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}` (per-machine) or `HKCU\Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}` (per-user), or call `GetAvailableCoreWebView2BrowserVersionString` — [Microsoft Learn, WebView2 distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution)
- Remediation: ~2 MB bootstrapper `MicrosoftEdgeWebview2Setup.exe /silent /install`; non-elevated run gives a per-user install (auto-replaced by per-machine if Edge's per-machine updater exists) — [Microsoft Learn, WebView2 distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution)

### Inferences
Screen-by-screen proposal for Caelestia (built from the cited rules; not itself sourced):
1. **First page = brand + "Before you begin" checks in one.** Wallpaper-derived hero (Material You colours), product name, one-line description ("Un bureau Material You pour Windows"), and a live checklist: Windows 11 build OK, WebView2 found, Zebar found / will be installed (version shown). Failed checks show a fix-it action inline ("Installer Zebar", "Installer WebView2") rather than a dead end. Licence as a link: "Logiciel libre sous licence GPL-3.0 — voir la licence" opening a local copy, with no accept checkbox (GNU FAQ says not required; Microsoft says link + local file is acceptable). The bundled licence file must still be installed with the program (GPL FAQ #WhyMustIInclude).
2. **"What will change on your PC" + options page (single page).** Opt-in/opt-out toggles: Start at sign-in (on, "recommended"), Alt+Tab switcher (on or off depending on stability), Sync Windows accent colour with wallpaper (off by default: it modifies a system setting, and Microsoft says default to the most private/safe). Next to it, a plain-language disclosure list (taskbar will be hidden while Caelestia runs, scheduled task names, registry keys touched, folders written). Commit button labelled "Installer" (not "Suivant"). No install-location page by default; an "Options avancées" disclosure can expose the path for the few who want it.
3. **Progress.** One determinate bar, steps above it ("Installation de Zebar…", "Copie des fichiers…", "Création des tâches de démarrage…", "Application des couleurs…"). Cancel stays enabled and triggers rollback. A "Détails" expander can show the log tail for enthusiasts without cluttering.
4. **Finish = launch.** Default behaviour: start Caelestia and close; if a Finish page is kept (it is arguably justified because the result is a desktop transformation users may want to tweak), offer "Ouvrir les paramètres" and "Fermer", with the "Lancer maintenant" checkbox pre-checked. Never launch when run silently.
5. **Error page.** Plain-French explanation, concrete next steps, the log path, "Copier les diagnostics" (copies OS build, versions of Zebar/WebView2, last N log lines, exit code to the clipboard) and "Ouvrir un ticket GitHub" link. Microsoft recommends logs for support, so a copy-diagnostics button is a natural modern extension.
6. **Rollback.** Because the risky change is hiding the taskbar and registering tasks, rollback order should be the inverse of install: unregister tasks, restore taskbar settings, remove copied files, leave Zebar if it was pre-existing. Inno exit code 5 (user cancel) and 4 (fatal) should both leave the system as it was.

### Gaps
- No NN/g article specifically on desktop installers was found within the call budget; NN/g onboarding guidance not cited.
- No source found on what other GPL desktop projects (e.g. Rainmeter, Flow Launcher) actually show on their licence page; only the FSF position is sourced.
- No sourced pattern for "copy diagnostics" buttons in installers specifically (pattern inferred from Microsoft's "log for support" rule).

## 2. Uninstall UX (confirmation, keep settings, restoring system state, feedback)

### Takeaway
Microsoft's rule is that uninstall removes all traces of the program (files, Start entries, registry, associations) but leaves user-created files; for a theme that hides the taskbar, "restoring the system state" is the most important trust promise and should be explicit, with a "keep my settings" choice that defaults to keeping user data.

### Cited Findings
- "Uninstall should remove all traces of a program": program files including setup, Start menu entries, desktop/Quick Launch icons, registry settings, file associations; should leave behind user-created files and shared system DLLs — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Microsoft: "Always require elevation for uninstall" to stop malware silently removing software — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup). This conflicts with per-user installs (Squirrel/Velopack, PowerToys user scope) which uninstall without elevation; the guide predates the per-user model, so treat as dated.
- Users should be able to add/remove optional features after setup via "Uninstall or change a program" — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Inno Setup 6.6 brought dark mode to Uninstall as well as Setup, and WizardSizePercent now applies to Uninstall (20% larger by default); 6.7 background colour/image directives also work in Uninstall — [Inno Setup 6 what's new](https://jrsoftware.org/files/is6-whatsnew.htm)
- Velopack exposes an uninstall hook (Windows-only `on_before_uninstall_fast_callback`, triggered by `--veloapp-uninstall`) to clean up before removal; shortcuts are created/removed automatically — [Velopack docs, Installer](https://docs.velopack.io/packaging/installer); [Velopack Squirrel migration](https://docs.velopack.io/migrating/squirrel)
- Seelen UI (closest competitor, also replaces the taskbar) fixed in v1.10.4 that the taskbar was always restored as non-auto-hide; it now restores "as initial state", i.e. snapshot-and-restore the user's original taskbar setting — [Seelen UI v1.10.4 release notes](https://seelen.io/apps/seelen-ui/releases/v1.10.4)
- Zed's docs point Windows users to the Settings > Apps page to uninstall (standard Add/Remove entry is the expected path) — [The New Stack on Zed for Windows](https://thenewstack.io/fast-rust-based-zed-code-editor-finally-arrives-on-windows/) (secondary source)

### Inferences
- Snapshot at install time (taskbar auto-hide state, accent colour, `AutoColorization`, startup entries) into a restore manifest in `%LOCALAPPDATA%\Caelestia`, and on uninstall restore exactly those values (the Seelen bug shows that hard-coding "restore to default" is wrong).
- Uninstall dialog: one screen, "Désinstaller Caelestia ?", with a checkbox "Conserver mes paramètres et thèmes" checked by default (Microsoft: leave user-created files), a second "Désinstaller aussi Zebar" checkbox unchecked and only shown if the installer installed Zebar itself, then a result page listing what was restored ("Barre des tâches Windows restaurée", "Couleurs Windows restaurées", "Tâches de démarrage supprimées").
- Feedback: an optional link to a GitHub issue/discussion ("Dites-nous pourquoi"), never a forced survey or web page auto-opened; no telemetry ping on uninstall.
- Silent uninstall must also restore the taskbar; uninstall should first stop Zebar and the helper so the taskbar comes back immediately rather than after sign-out.
- If Windows colour sync was enabled, uninstall must stop the sync process before restoring colours, otherwise it may race and re-apply.

### Gaps
- No sourced data on how Seelen UI, Rainmeter or Flow Launcher behave on uninstall (keep-settings prompts, taskbar restore on uninstall rather than on toggle).
- No sourced user research on "keep my settings" defaults.

## 3. Reference installers, one-click vs wizard, and user complaints

### Takeaway
The market has split into (a) one-click per-user installers (Squirrel/Velopack, Discord-style: splash, no questions, install to `%LocalAppData%`, launch) and (b) short Inno/NSIS/WiX wizards; tooling ecosystems offer both scopes (PowerToys ships separate user and machine installers). User complaints centre on bundled offers, pre-checked extras, deceptive wording, forced telemetry and refusing "no"; per-user AppData installs are disliked by enterprise users but are the norm for enthusiast tools.

### Cited Findings
- Velopack: "a 'one-click' installer" that "asks no questions and installs as quickly as it can, then launches the app"; shows a splash image (PNG/JPG/animated GIF) or a simple progress bar; default install `%LocalAppData%\{packId}`; separate MSI for per-machine/Group Policy — [Velopack docs, Installer](https://docs.velopack.io/packaging/installer); [Velopack docs, Windows](https://docs.velopack.io/packaging/operating-systems/windows)
- Enterprise complaint about Squirrel per-user installs: SourceTree's switch to Squirrel installing into `C:\Users\USERNAME\AppData...` "prevents us from using this in an enterprise environment" where only Program Files apps may run — [Atlassian Jira SRCTREEWIN-7082](https://jira.atlassian.com/browse/SRCTREEWIN-7082)
- PowerToys publishes per-user and machine-wide installers (e.g. `PowerToysUserSetup-<ver>-x64.exe` vs `PowerToysSetup-<ver>-x64.exe`); winget default is user scope, `--scope machine` for all users; mixing scopes is blocked and users are asked to uninstall the other first — [PowerToys GitHub releases](https://github.com/microsoft/PowerToys/releases); [PowerToys installer docs](https://microsoft.github.io/PowerToys/core/installer/); [GitHub issue #34299](https://github.com/microsoft/powertoys/issues/34299)
- Seelen UI offers MSIX (Store/winget, Store-managed updates) and an NSIS `setup.exe` from GitHub releases; the project itself warns the .exe "may trigger antivirus warnings because it is not digitally signed"; built-in updater for the .exe; channels Release/Beta/Nightly — [Seelen UI blog, distribution channels](https://seelen.io/blog/seelen-ui-distribution-channels)
- Zed for Windows ships an Inno Setup EXE (silent flags `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`) — [wingetly, Zed](https://wingetly.io/apps/zed-industries/zed) (third-party aggregator); [winpkg listing](https://winpkg.pages.dev/apps/ZedIndustries.Zed)
- Microsoft: don't use setup to "push your own agenda, cross-sell other program offerings, or conduct market research"; no personal information gathering; registration optional and unchecked; avoid sound — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Complaints: Express paths pre-select toolbars, search changes and usage analytics; "if there is no way to decline the bundled software... cancel the installation entirely" — [Alphonso Labs](https://www.alphonsolabs.com/installing-windows-apps-mistakes/amp/); deceptive checkboxes and companies that "don't accept No" — [gHacks](https://www.ghacks.net/?p=122728); offers hidden behind Advanced options — [Ask Leo](https://askleo.com/4906); "I already said no, dammit" on repeat prompts — [BetaNews](https://betanews.com/2016/06/23/i-already-said-no-dammit/); forced telemetry without opt-out is a recurring Windows setup criticism — [brennan.io, The Windows 10 Installer Dystopia](https://brennan.io/2020/11/08/windows-10-nightmare-edition/)
- Microsoft Store policy updated 10.2.3 to "prohibit bundleware within your product" — [Store policies change history](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies-change-history)

### Inferences
- For Caelestia, a hybrid fits best: a single-window, two-step installer (checks+brand, then disclosure+options with "Installer") rather than either a zero-question one-click (which can't disclose the taskbar change or get consent for colour sync) or a 6-page classic wizard. This matches Microsoft's "single page of options" rule.
- Per-user install under `%LOCALAPPDATA%` without UAC is the right default for this audience (PowerToys and Velopack both default to user scope); the enterprise objection (SourceTree) is irrelevant for an enthusiast desktop theme.
- No bundled offers, no default-on telemetry, no account, no "rate us" pop-up: these are the exact items users list as distrust triggers.

### Gaps
- Could not source specific installer details (within budget) for Discord, Figma, Spotify, Visual Studio Installer, new Teams, Arc for Windows, Warp, Raycast for Windows, Flow Launcher, Rainmeter. Known patterns (Discord/Figma/Spotify/Teams classic used Squirrel-style one-click; VS Installer is a workload-card bootstrapper; Flow Launcher logs show Squirrel) are from memory or unverified log pages and should be verified before citing.
- No specific Reddit r/software or HN thread captured; complaint evidence comes from tech blogs.

## 4. Silent / unattended install conventions, winget and IT expectations

### Takeaway
winget requires every package to support silent install; Inno Setup and NSIS get silent switches automatically when the manifest says `InstallerType: inno`/`nullsoft`. Inno already provides the full expected flag set (`/SILENT`, `/VERYSILENT`, `/SUPPRESSMSGBOXES`, `/LOG`, `/TASKS`, `/MERGETASKS`, `/CURRENTUSER`, `/LANG`, `/DIR`, `/NORESTART`) and documented exit codes 0 to 8.

### Cited Findings
- winget: "All tools must support a silent install. If you have an executable that does not support a silent install, then we cannot provide that tool" — [Microsoft Learn, winget manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- "When Nullsoft or Inno are specified, the client will automatically set the silent and silent with progress install behaviors" — [Microsoft Learn, winget manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- Common silent switches table: MSI `/q`, InstallShield `/s`, Inno `/SILENT or /VERYSILENT`, Nullsoft `/S` — [Microsoft Learn, winget manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- `PackageName` and `Publisher` should match the Add/Remove Programs entry for export/upgrade correlation; manifest schema 1.12.0; locales can be added (example fr-FR locale file) — [Microsoft Learn, winget manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- InstallerSwitches keys: Silent, SilentWithProgress, Interactive, InstallLocation, Log, Upgrade, Custom; `InstallerSuccessCodes` for non-zero success codes; `ExpectedReturnCodes` exists — [winget-types crate](https://docs.rs/winget-types/0.5.0/winget_types/struct.InstallerManifest.html); [winget-pkgs schema 1.12.0 installer](https://github.com/microsoft/winget-pkgs/blob/master/doc/manifest/schema/1.12.0/installer.md)
- Inno Setup flags: `/SILENT` (progress only), `/VERYSILENT` (nothing), `/SUPPRESSMSGBOXES` (default answers, only with silent), `/LOG` (to user's TEMP, unique date-based name), `/LOG="file"`, `/NOCANCEL`, `/NORESTART`, `/CLOSEAPPLICATIONS`, `/RESTARTAPPLICATIONS`, `/LANG=`, `/DIR=`, `/TASKS=`, `/MERGETASKS=`, `/COMPONENTS=`, `/CURRENTUSER`, `/ALLUSERS`, `/SAVEINF=`, `/LOADINF=` — [Inno Setup help, command line](https://jrsoftware.org/ishelp/topic_setupcmdline.htm)
- Inno exit codes: 0 success; 1 init failed; 2 cancelled before install; 3 fatal moving to install phase; 4 fatal during install; 5 cancelled during install; 6 debugger terminated; 7 Preparing-to-install can't proceed; 8 same and restart needed. Future versions may add codes, so callers should handle unknown values — [Inno Setup help, exit codes](https://jrsoftware.org/ishelp/topic_setupexitcodes.htm)
- Microsoft Store 10.2.9.2: Win32 products "must install silently" with silent parameters supplied in Partner Center; installer binaries (.msi/.exe) must be signed with a cert chaining to a Microsoft-recognised CA — [Store policies change history](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies-change-history); [Microsoft Q&A on 10.2.9.2](https://learn.microsoft.com/en-us/answers/questions/2077162/why-is-it-that-the-test-has-been-passed-in-the-pac)
- Tauri's NSIS installer silent flag is uppercase `/S` — [Tauri docs, Microsoft Store](https://v2.tauri.app/distribute/microsoft-store/)
- Velopack per-user location override: `Setup.exe --installto {dir}` — [Velopack docs, Installer](https://docs.velopack.io/packaging/installer)
- WebView2 bootstrapper silent: `/silent /install` — [Microsoft Learn, WebView2 distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution)
- Microsoft: setup run from command line, auto-update or Group Policy should not start programs — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)

### Inferences
- Map Caelestia options to Inno `[Tasks]` names so `/MERGETASKS="!altTab,colorSync"` works for scripted installs, and document them in docs/ (English) with an example `winget install ... --override` line.
- Write logs to `%TEMP%` by default via `SetupLogging=yes` equivalent, and copy the final log into `%LOCALAPPDATA%\Caelestia\logs` so the "copy diagnostics" button and bug reports can find it.
- Uninstaller should also support `/VERYSILENT` (Inno uninstallers do) and a documented "keep settings" switch for scripted use; uninstaller exit codes are not documented on the exit-code page (gap).
- Use `PrivilegesRequired=lowest` (per-user, no UAC) with `/CURRENTUSER` behaviour as default; this aligns with winget `Scope: user`.

### Gaps
- Exact `ExpectedReturnCodes` response enum values not retrieved from the official schema page (only field existence confirmed).
- Inno uninstaller command-line parameters and exit codes not fetched.

## 5. Localization, accessibility, dark mode and Windows 11 look

### Takeaway
Detect language from the Windows UI language (Inno's default `uilanguage`, Microsoft-recommended), not the regional locale. Inno Setup 6.6+ (current 6.7.4, 2026-10-28) now has real dark mode (`WizardStyle=modern dynamic`), a `windows11` built-in style, Segoe UI, larger DPI-aware wizard sizing, and skips custom styles under high contrast; WPF on .NET 9/10 has a built-in Fluent theme with `ThemeMode=System` (still flagged experimental, WPF0001), while Mica in WPF still comes via libraries like WPF-UI.

### Cited Findings
Language
- Inno `LanguageDetectionMethod`: `uilanguage` (default, `GetUserDefaultUILanguage()`, "the method Microsoft recommends"), `locale` (`GetUserDefaultLangID()`, regional format setting, "not intended to affect languages"), or `none` (first `[Languages]` entry) — [Inno Setup help, LanguageDetectionMethod](https://jrsoftware.org/ishelp/topic_setup_languagedetectionmethod.htm)
- `/LANG=language` sets language and skips the language dialog — [Inno Setup help, command line](https://jrsoftware.org/ishelp/topic_setupcmdline.htm)
- winget manifests support additional locale files (example `fr-FR` ShortDescription) — [Microsoft Learn, winget manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)

Inno Setup modern UI (2025-2026)
- 6.6: "Setup and Uninstall now both support dark mode, forced or automatic"; modes light/dark/dynamic; built-in styles polar, slate, windows11, zircon; modifiers `includetitlebar`, `excludelightbuttons`; custom styles skipped under high-contrast themes; default font Segoe UI; system icons on several pages; `WizardSizePercent` default 120,120 incl. Uninstall; aspect ratio preserved when scaling (`WizardKeepAspectRatio`) — [Inno Setup 6 what's new](https://jrsoftware.org/files/is6-whatsnew.htm); [Inno Setup 6.6.0 announcement](https://groups.google.com/g/innosetup-announce/c/svrvZCO0M3Q)
- `WizardStyleFileDynamicDark` overrides `WizardStyleFile` when dynamic mode is dark; forced `dark` ignores the DynamicDark directives — [Inno Setup help, WizardStyleFileDynamicDark](https://jrsoftware.org/ishelp/topic_setup_wizardstylefiledynamicdark.htm)
- 6.6.1 fixed RTF text colours being overridden by custom dark styles (relevant if the licence or info text is RTF) — [Neowin, Inno Setup 6.6.1](https://www.neowin.net/amp/inno-setup-661/)
- 6.7: `WizardBackColor`, `WizardBackImageFile` (+ DynamicDark variants, `WizardBackImageOpacity`), runtime `WizardSetBackImage`, colour names; RedirectionGuard on by default; experimental `UseSetupLdr=x64`. Latest 6.7.4 dated 2026-10-28 — [Inno Setup 6 what's new](https://jrsoftware.org/files/is6-whatsnew.htm)
- 6.5: PNG wizard images with transparency; `extractarchive` and `download` flags in [Files]; keyboard- and screen-reader-accessible TBitmapButton; IDE high-contrast/screen-reader improvements — [Inno Setup 6 what's new](https://jrsoftware.org/files/is6-whatsnew.htm)
- Reported issue (pre-6.6.0 final): custom style + background image could stop controls taking the style; workaround `WizardStyle=modern dark hidebevels` — [Inno Setup forum](https://groups.google.com/g/innosetup/c/DodpAvYMjAQ)
- Not covered in Inno docs I read: per-monitor DPI v2 specifics, rounded corners, Mica — [Inno Setup 6 what's new](https://jrsoftware.org/files/is6-whatsnew.htm)

WPF / WinUI
- .NET 9 WPF adds a Fluent theme with light/dark; `ThemeMode` on `Application` or `Window`: Light, Dark, System (follows Windows), None (default, legacy Aero2) — [Microsoft Learn, What's new in WPF .NET 9](https://learn.microsoft.com/dotnet/desktop/wpf/whats-new/net90); [Window.ThemeMode API](https://learn.microsoft.com/dotnet/api/system.windows.window.thememode)
- ThemeMode remains experimental in .NET 9/10 (requires suppressing WPF0001) — [FluentWpfCore NuGet](https://packages.nuget.org/packages/FluentWpfCore/1.0.1) (third-party statement)
- WPF-UI provides Fluent controls, Light/Dark/High Contrast styles, and Win32 interop for Mica/Acrylic, supporting .NET 10/9/8 and .NET Framework — [WPF-UI docs](https://docsearch.algolia.com/mcp/docs/repo/lepoco/wpfui); Mica is Windows 11 only — [FluentWpfCore NuGet](https://packages.nuget.org/packages/FluentWpfCore/1.0.1)

Microsoft guidance relevant to accessibility/layout
- Wizard window size should show all pages without page scrolling; don't maximise setup windows; consistent page size and structure — [Microsoft Learn, Wizards UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/win-wizards); [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Spell out acronyms, friendly professional tone, no readme files — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)

### Inferences
- If staying on Inno Setup: `WizardStyle=modern dynamic windows11` (or a custom VCL style tinted with Caelestia's default Material palette via `WizardStyleFile` / `WizardStyleFileDynamicDark`), `WizardBackImageFile` for a soft brand background, PNG artwork, `[Languages]` with French + English, `LanguageDetectionMethod=uilanguage`, `ShowLanguageDialog=no` (or `auto`). This yields dark mode and high-contrast fallback for free; true Material 3 Expressive motion and rounded/Mica surfaces are not achievable in Inno.
- If a fully branded experience is wanted (rounded cards, motion, wallpaper-derived colours on the first screen), a small WPF (.NET 10, Fluent ThemeMode=System + WPF-UI for Mica) or WinUI 3 bootstrapper front-end could drive an Inno/plain-script backend in silent mode. Cost: framework runtime size and accessibility work (UI Automation names, keyboard order, focus visuals) that Inno gives natively. Keep `/VERYSILENT` on the backend so winget is unaffected.
- NSIS remains the weakest choice for dark mode/DPI out of the box (not sourced in this pass; see gaps).
- Respect `prefers reduced motion` equivalent (Windows "Animation effects" off) in any custom UI; honour high contrast by falling back to system colours (Inno already does).
- Localise the winget manifest with an `fr-FR` locale file.

### Gaps
- No primary source fetched on NSIS dark mode / per-monitor DPI v2 limitations.
- No source fetched on Inno Setup's DPI awareness mode (system vs per-monitor v2).
- WinUI 3 installer front-ends: no concrete example or doc fetched.
- No official Microsoft doc pairing WPF ThemeMode with Mica backdrops was found.

## 6. Trust signals (signing, telemetry, disclosure, privacy)

### Takeaway
Signing is the biggest trust lever but, for an individual in France in 2026, Azure Artifact Signing is unavailable (US/Canada individuals only) and gives no instant SmartScreen trust anyway; SignPath Foundation (free for qualifying OSS) or an OV certificate are the realistic routes. Beyond signing, trust comes from disclosure: say what changes, opt-in for anything touching system settings, no telemetry, no bundles.

### Cited Findings
- Microsoft: "Digitally sign the setup executable file" (gives a more specific UAC prompt); elevate as late as possible; require least privilege — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup)
- Azure Artifact Signing (formerly Trusted Signing): individual developers limited to USA and Canada — [Microsoft Learn, code signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options); [Artifact Signing quickstart](https://learn.microsoft.com/azure/artifact-signing/quickstart)
- Artifact Signing "does not provide instant SmartScreen trust"; new files warn until reputation builds; OV certs behave the same; traditional OV certs cost roughly $150–300/year — [Microsoft Learn, code signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options); [Microsoft Learn, sign MSIX guide](https://learn.microsoft.com/windows/msix/package/sign-msix-package-guide)
- SignPath Foundation offers free code signing for qualifying open-source projects — [Microsoft Learn, code signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options); requires OSI-approved licence and public repo — [Zenn writeup](https://zenn.dev/shm_7ec/articles/signpath-oss-code-signing?locale=en) (third-party)
- User reports of SmartScreen "Unrecognized app" regressions after Trusted Signing moved to new intermediate CAs from 2026-03-26 (unconfirmed by Microsoft in what I found) — [Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/5855708/trusted-signing-regression-in-smartscreen-reputati)
- Seelen UI openly warns its unsigned .exe "may trigger antivirus warnings" and steers users to the Store/winget MSIX — [Seelen UI blog](https://seelen.io/blog/seelen-ui-distribution-channels)
- Inno 6.5 `issigverify`/ISSigTool verify file integrity but "don't remove Windows' 'Unknown publisher' warning" — [Inno Setup 6 what's new](https://jrsoftware.org/files/is6-whatsnew.htm)
- Microsoft: for any data collection, "explain what data is transmitted, how it is used, and how long it is kept" with a privacy statement link; don't gather personal info; opt-in for optional features; for IT-pro installs list comprehensive changes ("to build trust through transparency" for summaries when the wizard acts on non-obvious input) — [Microsoft Learn, Setup UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/exper-setup); [Microsoft Learn, Wizards UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/win-wizards)
- winget manifests carry `PrivacyUrl`, `LicenseUrl`, `PublisherUrl` fields — [Microsoft Learn, winget manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- Store policy requires signed .msi/.exe installers chaining to a Microsoft-recognised CA — [Store policies change history](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies-change-history)

### Inferences
- Apply to SignPath Foundation (GPL-3.0 is OSI-approved) so the UAC/SmartScreen dialogs show a publisher name; until then, publish SHA-256 hashes and point users to winget (whose manifest pins `InstallerSha256`).
- Put a short "Ce que Caelestia modifie sur votre PC" panel in the installer and the same list in docs/ (English): hides the Windows taskbar while running (restored on exit/uninstall), creates N scheduled tasks at sign-in (names listed), writes to `%LOCALAPPDATA%\Caelestia` and Zebar's config folder, optionally changes Windows accent colour, no services, no admin rights, no network calls except optional update checks.
- Statement "Aucune télémétrie. Aucune donnée n'est envoyée." on the first page, plus a `PrivacyUrl` in winget pointing to a docs page that says the same.
- Running per-user with no UAC prompt at all is itself a trust signal for a theme; if Zebar's installer needs elevation, say so before the prompt appears ("Zebar demandera une autorisation administrateur").

### Gaps
- Current (Nov 2026) SignPath Foundation terms and the status of the March 2026 SmartScreen regression were not verified.
- No sourced study quantifying the trust effect of "no telemetry" statements or change-disclosure panels.
