# How comparable Windows shell-customization projects build and ship their installers (state as of 2026-11-16)

Method: latest-release asset lists pulled from the GitHub REST API on 2026-11-16 (`/repos/{r}/releases/latest`), installer sources read directly from each repo's default branch (raw.githubusercontent.com), issue bodies and comments read via the GitHub issues API, plus docs/wikis. Where only secondary sources exist (forums, scan sites), this is flagged.

## 1. Which installer technology, per-user or per-machine, and why

### Takeaway
The projects closest to Caelestia (GlazeWM, Zebar, YASB, Komorebi) all ship **WiX/MSI**, mostly **per-machine** in Program Files. GlazeWM wraps its MSI and Zebar's MSI in a **WiX v4/v5 Burn bundle** (`.exe`) so one download installs both. Tauri apps (Seelen UI, Windhawk's UI) use **NSIS**. Microsoft's PowerToys moved to **per-user by default**, alongside a machine-wide option. The Store/MSIX route (TranslucentTB, Seelen, Lively) trades custom install and uninstall logic for auto-updates and trust.

### Cited Findings

**GlazeWM (glzr-io)**, v3.10.1, released 2026-03-21
- Release assets: `glazewm-v3.10.1.exe` (33 MB bundle), `standalone-glazewm-v3.10.1-x64.msi` (5.1 MB), `standalone-glazewm-v3.10.1-arm64.msi`, plus a `.dmg` — [GitHub API releases/latest](https://api.github.com/repos/glzr-io/glazewm/releases/latest)
- The `.exe` is a **WiX Burn bundle** (`resources/wix/bundle.wxs`, WiX v4 schema) using `WixStandardBootstrapperApplication` with a custom theme. It chains the GlazeWM MSI and the **Zebar MSI** (x64 or arm64, picked with `NativeMachine` conditions). Zebar is optional through the variable `Custom_InstallZebar`, and Zebar gets the MSI property `ADD_GLAZEWM_STARTER=1`. The packages are marked `Permanent="yes"`, and the bundle sets `DisableModify`/`DisableRemove`, so each MSI shows up in Apps & Features on its own — [bundle.wxs](https://github.com/glzr-io/glazewm/blob/main/resources/wix/bundle.wxs)
- The README says Zebar can optionally be installed through a checkbox during installation — [GlazeWM README](https://github.com/glzr-io/glazewm/blob/main/README.md)
- The standalone MSI is `Scope="perMachine"`, installs to `ProgramFiles6432Folder\glzr.io\GlazeWM`, and sets `REINSTALLMODE=amus` ("reinstall all files; rewrite all registry entries"). It uses `MajorUpgrade Schedule="afterInstallInitialize" AllowDowngrades="yes"`, adds the `cli` subfolder to the **system PATH**, and records `InstallDir` under `HKLM\SOFTWARE\glzr.io\GlazeWM` — [standalone.wxs](https://github.com/glzr-io/glazewm/blob/main/resources/wix/standalone.wxs)
- CI installs **WiX 5.0.0** as a dotnet tool (`wix extension add WixToolset.UI.wixext/5 … BootstrapperApplications.wixext/5`) and signs with **AzureSignTool** (secret `AZ_CERT_NAME`, which points to an Azure Key Vault certificate) — [package.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/package.yaml)

**Zebar (glzr-io)**, v3.3.1, released 2026-03-16
- Release assets: `zebar-v3.3.1-opt1-x64.msi`, `zebar-v3.3.1-opt2-arm64.msi`, `.dmg`, `.deb`, `.AppImage` (Tauri multi-platform). There is no NSIS exe — [GitHub API](https://api.github.com/repos/glzr-io/zebar/releases/latest)
- The Tauri **custom WiX template** `packages/desktop/installer.wxs` (WiX v3 schema, `InstallScope="perMachine"`) installs to `ProgramFiles64Folder\glzr.io\Zebar`. It sets `ARPNOREPAIR`/`ARPNOMODIFY`, reuses the previous `InstallDir` from `HKCU\Software\glzr.io\Zebar`, and copies the same `REINSTALLMODE=amus` + `AllowDowngrades` pattern as GlazeWM — [installer.wxs](https://github.com/glzr-io/zebar/blob/main/packages/desktop/installer.wxs)
- Zebar contains a `marketplace_installer.rs`, an in-app installer for widget packs from the Zebar marketplace. This is a separate channel from the MSI — [repo tree](https://github.com/glzr-io/zebar/tree/main/packages/desktop/src)

**YASB (amnweb)**, v2.0.8, released 2026-11-07
- Release assets: `yasb-2.0.8-x64.msi` (34.7 MB), `yasb-2.0.8-aarch64.msi`, `checksums.txt` — [GitHub API](https://api.github.com/repos/amnweb/yasb/releases/latest)
- The MSI is built with **cx_Freeze `bdist_msi`** (`src/build.py`). It sets `"all_users": True`, `initial_target_dir = [ProgramFiles64Folder]\YASB`, and a fixed `upgrade_code`, and freezes four executables (`yasb`, `yasb_themes`, `yasbc`, `yasb_cloud`) — [build.py](https://github.com/amnweb/yasb/blob/main/src/build.py)
- **Conflict:** the README says to pick "the installer that matches your architecture and install scope. For most devices, that's the x64 per-user installer". It also documents `winget install AmN.yasb` (user scope, the default) and `winget install --scope machine AmN.yasb`. But the latest release only has one MSI per architecture, and build.py sets `all_users: True` — [YASB README](https://github.com/amnweb/yasb/blob/main/README.md). It is unclear whether the MSI is dual-purpose (ALLUSERS=2) or the README is out of date.

**Komorebi (LGUG2Z)**, v0.1.41, released 2026-05-03
- Release assets: `komorebi-0.1.41-x86_64.msi`, `komorebi-0.1.41-aarch64.msi`, matching `.zip` files, and `checksums.txt`. The repo has `wix/main.wxs` and `wix/License.rtf` (the layout `cargo-wix` produces) — [GitHub API](https://api.github.com/repos/LGUG2Z/komorebi/releases/latest), [repo tree](https://github.com/LGUG2Z/komorebi/tree/master/wix)

**Seelen UI (eythaann)**, v2.8.11, released 2026-11-16
- Release assets: `Seelen.UI_2.8.11_x64-setup.exe` (48.8 MB), `…x64-setup-fixed.exe` (258 MB), the arm64 equivalents, and `latest.json` (the Tauri updater manifest) — [GitHub API](https://api.github.com/repos/eythaann/Seelen-UI/releases/latest)
- `tauri.conf.json` sets bundle target **`nsis`** with `"installMode": "perMachine"`, `displayLanguageSelector: true`, and `createUpdaterArtifacts: true` — [tauri.conf.json](https://github.com/eythaann/Seelen-UI/blob/master/src/tauri.conf.json)
- The `-fixed` installers bundle a **Fixed-Version WebView2 runtime**: CI has a "Set Fixed Runtime" step (`scripts/SetFixedRuntime.ps1`), and the NSIS pre-install hook deletes `$INSTDIR\runtime` — [release.yml](https://github.com/eythaann/Seelen-UI/blob/master/.github/workflows/release.yml), [installer-hooks.nsh](https://github.com/eythaann/Seelen-UI/blob/master/src/templates/installer-hooks.nsh)
- The README says Seelen requires WebView2; on Windows 10 the runtime "is included with the setup.exe installer". It also says "All distribution channels ship **signed** packages" and offers a **Microsoft Store** listing (9P67C2D4T9FB) — [Seelen README](https://github.com/eythaann/Seelen-UI/blob/master/README.md)
- There is a separate MSIX pipeline (`.github/workflows/msix.yml`, `scripts/bundle.msix.ts`, `src/templates/AppxManifest.xml`) — [repo tree](https://github.com/eythaann/Seelen-UI)

**Windhawk (ramensoftware)**, v1.7.3, released 2025-12-08
- Release assets: `windhawk_setup.exe` (11.6 MB) and `windhawk_setup_offline.exe` (149 MB) — [GitHub API](https://api.github.com/repos/ramensoftware/windhawk/releases/latest)
- The README says the official installer can extract a **portable version** — [Windhawk README](https://github.com/ramensoftware/windhawk/blob/main/README.md)
- The new Tauri UI (`src/windhawk-core/ui/tauri.conf.json`) has `"bundle": {"active": false, "targets": ["nsis"]}` and `webviewInstallMode: downloadBootstrapper`. Tauri bundling is disabled, which suggests the setup is a custom installer — [tauri.conf.json](https://github.com/ramensoftware/windhawk/blob/main/src/windhawk-core/ui/tauri.conf.json)

**ExplorerPatcher (valinet)**, 26100.9711.71.1, released 2026-10-29
- Release assets: a custom `ep_setup.exe` and `ep_setup_arm64.exe` (about 12 MB each). There is no MSI — [GitHub API](https://api.github.com/repos/valinet/ExplorerPatcher/releases/latest)

**Rainmeter**, v4.5.28, released 2026-10-12
- One asset, `Rainmeter-4.5.28.exe` (2.6 MB) — [GitHub API](https://api.github.com/repos/rainmeter/rainmeter/releases/latest)
- The installer is **NSIS** (`Build/Installer/Installer.nsi`). It runs with `RequestExecutionLevel user` and elevates through the **NSIS UAC plugin** (`UAC_RunElevated`). It calls back into the user context (`UAC_AsUser_Call … UpdateUserStartup`, `CopyIniToAppData`, `RemoveStartMenuShortcuts`) so per-user state lands in the real user's profile even though files go to Program Files. It also offers a **Portable install** (`/PORTABLE=1`) radio button — [Installer.nsi](https://github.com/rainmeter/rainmeter/blob/master/Build/Installer/Installer.nsi)

**Flow Launcher**, v2.1.4, released 2026-09-20
- Release assets: `Flow-Launcher-Setup.exe`, `Flow-Launcher-Portable.zip`, `FlowLauncher-2.1.4-full.nupkg`, and `RELEASES`, which is the **Squirrel** layout. It still runs on Squirrel — [GitHub API](https://api.github.com/repos/Flow-Launcher/Flow.Launcher/releases/latest)
- Squirrel installs per-user under `%localappdata%\FlowLauncher\app-<version>\`. In portable mode the UserData lives at `%localappdata%\FlowLauncher\app-<Version>\UserData` — [Flow README](https://github.com/Flow-Launcher/Flow.Launcher/blob/dev/README.md)
- Velopack migration: PR #2616 "Migrate to Velopack & Github Action" was opened on 2024-03-24 and is still **open** — [PR #2616](https://github.com/Flow-Launcher/Flow.Launcher/pull/2616)
- In PR #4000 (2025-09), maintainers said they prefer Velopack because it provides build versioning and a pre-release channel, and that Squirrel "was abandoned long ago". That PR was closed unmerged on 2025-12-26 — [PR #4000](https://github.com/Flow-Launcher/Flow.Launcher/pull/4000)
- PR #4236 (2026-01-27, closed) added framework-dependent packages without the bundled .NET runtime, in response to issue #4134 asking for a smaller install — [PR #4236](https://github.com/Flow-Launcher/Flow.Launcher/pull/4236)

**PowerToys (Microsoft)**, v0.102.3101, released 2026-11-06
- Release assets: `PowerToysSetup-…-x64.exe` (machine), `PowerToysUserSetup-…-x64.exe` (user), the arm64 equivalents, `GroupPolicyObjectFiles-….zip`, and symbols — [GitHub API](https://api.github.com/repos/microsoft/PowerToys/releases/latest)
- The dev docs say:
  - The installer is **WiX v5**, migrated from WiX v3 in release 0.94.
  - An EXE bootstrapper wraps the MSI, "checks dependencies and closes PowerToys before installing".
  - Per-user installs go to `%LOCALAPPDATA%\PowerToys` (HKCU); per-machine to `Program Files\PowerToys` (HKLM). **Per-user is now the default.**
  - Guards prevent installing both scopes at once.
  - C++ custom actions handle user impersonation, GPO checks and process termination, and the custom-actions DLL is signed separately before the MSI build.
  - **Sparse MSIX packages** are used only for Windows 11 context-menu handlers; "full MSIX installers were abandoned".

  [PowerToys installer docs](https://microsoft.github.io/PowerToys/core/installer/)
- The GitHub README says updating via winget keeps the current installation scope, and that user scope is the winget default — [PowerToys repo](https://github.com/microsoft/PowerToys) (via search snippet)

**TranslucentTB**, release 2026.2, published 2026-08-31
- Release assets: `bundle.msixbundle`, `TranslucentTB.appinstaller`, and `TranslucentTB-portable-{x64,arm64}.zip`. The main channel is the Microsoft Store (9PF4KZ2VN4W9) "with background auto-updates"; the portable zip "only works on Windows 11" — [README](https://github.com/TranslucentTB/TranslucentTB/blob/develop/README.md), [GitHub API](https://api.github.com/repos/TranslucentTB/TranslucentTB/releases/latest)

**Lively Wallpaper**
- There are two channels. The GitHub installer "Require[s] installation of .NET runtime and other dependencies on host system (admin account is required during installation.)" The Store version needs no dependencies and has an incremental updater, but has limitations: no application or game wallpapers, and no extra wallpaper plugins — [Lively wiki: Differences Between Distributions](https://github.com/rocksdanister/lively/wiki/Differences-Between-Distributions)

### Inferences
- The glzr-io pattern is the closest analogue for a "Zebar + native helper" product: per-machine MSIs with fixed UpgradeCodes and `AllowDowngrades`, chained by a WiX Burn bundle with checkboxes and arch detection, signed via AzureSignTool, and published to winget from the release event. Caelestia could ship a Burn bundle that chains **Zebar's own upstream MSI** (as GlazeWM does) plus its own helper MSI.
- `REINSTALLMODE=amus` (glzr) is a heavy-handed way to make sure files are overwritten on same-version reinstalls. It is not usually recommended because it can downgrade shared files, but the glzr apps only install private files, so it is harmless for them.
- Per-user (PowerToys default, Squirrel, YASB claims) avoids UAC. But GlazeWM and Zebar are per-machine, and Caelestia depends on Zebar, so a per-user Caelestia would still pull in a per-machine Zebar unless Zebar is installed some other way.

### Gaps
- StartAllBack's installer technology is not documented publicly. One 2023 forum thread reports the "only for me" option was greyed out in 3.6.2 — [ElevenForum](https://www.elevenforum.com/t/cannot-install-startallback-3-6-2-only-for-me-at-all.13348/latest). Third-party guides mention both `C:\Program Files\StartAllBack` and `%LOCALAPPDATA%\StartAllBack` — [macmyths](https://macmyths.com/how-to-uninstall-startallback-windows-11/). These are low-quality secondary sources, and no vendor statement was found.
- I could not confirm Windhawk's installer engine (NSIS vs custom). The repo does not contain the installer script.
- Nilesoft Shell (latest GitHub release v1.9.15 dated 2024-02-14, no assets on GitHub) and the Files app (v4.3, 2026-10-11, no GitHub assets; Store/sideload) were not investigated further.
- I could not confirm whether the Komorebi WiX template is per-user or per-machine.

## 2. Dependencies, updates and winget/Store

### Takeaway
WebView2 is the main runtime dependency for Tauri apps. Seelen solves it by shipping a second "fixed runtime" installer. Updates are handled in one of three ways: winget-releaser on release, the Tauri updater with a signed `latest.json`, or Store/MSIX auto-update. Velopack is the modern replacement for Squirrel.

### Cited Findings
- GlazeWM publishes to winget automatically. On each `release: published` event, `vedantmgoyal9/winget-releaser` runs with identifier `glzr-io.glazewm` and `installers-regex: 'glazewm-v[0-9.]+\.exe$'` (it submits the Burn bundle, not the MSI) — [winget-release.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/winget-release.yaml)
- YASB also has a `winget.yaml` workflow and an in-app update checker based on winget (`src/core/widgets/services/update_check/winget.py`) — [repo tree](https://github.com/amnweb/yasb/tree/main/.github/workflows)
- Seelen uses the Tauri updater: `latest.json` is a release asset, and CI uses `TAURI_SIGNING_PRIVATE_KEY` to sign update artifacts. `SHA256SUMS` and `SHA256SUMS.sig` are installed into `$INSTDIR` — [release.yml](https://github.com/eythaann/Seelen-UI/blob/master/.github/workflows/release.yml), [installer-hooks.nsh](https://github.com/eythaann/Seelen-UI/blob/master/src/templates/installer-hooks.nsh)
- Windhawk offers an online setup (11.6 MB) and an offline setup (149 MB) — [GitHub API](https://api.github.com/repos/ramensoftware/windhawk/releases/latest). Its Tauri config uses `webviewInstallMode: downloadBootstrapper` — [tauri.conf.json](https://github.com/ramensoftware/windhawk/blob/main/src/windhawk-core/ui/tauri.conf.json)
- Velopack migrates existing Squirrel apps on their first update, repairs shortcuts to point to the new location, and still produces the legacy `RELEASES` file for migrating users — [Velopack docs: migrating from Squirrel](https://docs.velopack.io/migrating/squirrel)
- A Zebar user hit an installer error on v2.5.0. A later comment (2026-09-20) says the install layout changed internally in v3, and that "when the installation changes winget is unable to resolve it and you have to uninstall first" — [zebar#151](https://github.com/glzr-io/zebar/issues/151)
- Lively's Store build avoids the .NET dependency, while its installer requires admin rights to install dependencies — [Lively wiki](https://github.com/rocksdanister/lively/wiki/Differences-Between-Distributions)
- PowerToys' build generates its file list automatically so self-contained .NET/WinAppSDK DLLs are included, and WebView2 DLLs must be published with the .NET apps — [PowerToys installer docs](https://microsoft.github.io/PowerToys/core/installer/)

### Inferences
- If Caelestia depends on Zebar, the cleanest option is the GlazeWM approach: chain Zebar's signed upstream MSI in the bundle instead of repackaging it. Zebar itself relies on WebView2, which is present on Windows 11. For Windows 10, a WebView2 bootstrapper check would be needed.
- Changing UpgradeCode, scope or install layout between versions breaks winget upgrades (zebar#151). Freeze these choices before the first public release.

### Gaps
- I did not verify how YASB's winget manifest maps scopes to installers.
- I did not check the Tauri updater's behaviour with per-machine NSIS (elevation on update) in Seelen.

## 3. Signing, SmartScreen and antivirus

### Takeaway
Every active project in this space now signs. SignPath (free for OSS) is used by YASB and Seelen, Azure Key Vault/AzureSignTool by glzr, and a Certum Open Source certificate by Windhawk. Signing reduces but does not eliminate antivirus hits on hook- or injection-based tools. ExplorerPatcher shows the worst case: a Defender "HackTool"/PUP classification that lasted roughly from 2024 until it was lifted by 2026-10.

### Cited Findings
- YASB signs its DLL, EXEs and MSI through `signpath/github-action-submit-signing-request@v2`, with separate unsigned artifacts uploaded for SignPath to return signed — [windows.yaml](https://github.com/amnweb/yasb/blob/main/.github/workflows/windows.yaml)
- YASB issue #483 (2025-09-30): running `yasbc.exe enable-autostart` caused **`yasb.exe` to be deleted**. The maintainer said "This looks like a problem with your antivirus software… `1.8.3` comes with signed code", and the reporter confirmed 1.8.3 fixed it — [yasb#483](https://github.com/amnweb/yasb/issues/483)
- Seelen signs its binaries with SignPath (`signing-policy-slug: release-signing`) and then runs `npx tauri bundle --no-sign` on the already-signed binaries. A separate `release_no_signed.yml` workflow exists — [release.yml](https://github.com/eythaann/Seelen-UI/blob/master/.github/workflows/release.yml)
- GlazeWM and Zebar sign with AzureSignTool 5.0.0 in CI — [package.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/package.yaml)
- Windhawk 1.7.3 is signed by "Open Source Developer, Michael Maltsev" through the **Certum** Code Signing 2021 CA, and an older 1.6 build was signed through a different chain — [Gridinsoft scan (third-party)](https://gridinsoft.com/online-virus-scanner/id/a37f8d5be2216f55c9c624fed32272b6baab68a67c2b8c2f5f1d5e86b889d244). A scan report shows 2 of 69 engines flagging `windhawk_setup.exe` with generic ML/Rugmi labels — [MalwareTips scan (third-party)](https://tools.malwaretips.com/file-scan/5116cc1703384aac88b8d7a3b57f172aea5da32b6df7355fb2f8247d23813c8f). Treat both as third-party and unofficial.
- Windhawk issues: SentinelOne reports Windhawk's shell commands as suspicious (#222, 2024-07-19, open) — [windhawk#222](https://github.com/ramensoftware/windhawk/issues/222). "CodeIntegrity Event 3033 spam: engine repeatedly tries to inject into processes with signe[d …]" (#1061, 2026-08-24, open) — [windhawk#1061](https://github.com/ramensoftware/windhawk/issues/1061)
- ExplorerPatcher:
  - The wiki explains that flags are heuristic because "injecting code into other executable programs is rarely done by legitimate programs". As mitigation, releases "are built automatically on GitHub's infrastructure… never files compiled on my own computer" — [EP wiki: Antivirus false positives](https://github.com/valinet/ExplorerPatcher/wiki/Antivirus-false-positives)
  - In March 2024, Defender classified EP as a PUP — [Born's Tech and Windows World, 2024-03-02](https://borncity.com/win/2024/03/02/windows-defender-reports-explorerpatcher-as-pup/). Users later reported `HackTool:Win64/ExplorerPatcher!MTB` — [ElevenForum thread](https://www.elevenforum.com/t/ms-has-flagged-explorer-patcher-as-malware.28438/latest)
  - The latest release notes (2026-10-29) say "Windows Defender no longer flags ExplorerPatcher". They still list third-party AV exclusions for `C:\Program Files\ExplorerPatcher`, `%APPDATA%\ExplorerPatcher`, `C:\Windows\dxgi.dll` ("explorer.exe will not start if this file exists but is blocked from being loaded"), and the StartMenuExperienceHost/ShellExperienceHost SystemApps folders. They advise turning off **Smart App Control** before installing and warn that **SentinelOne** can block uninstallation — [EP release 26100.9711.71.1](https://github.com/valinet/ExplorerPatcher/releases/latest)
  - Earlier notes, reported second-hand, warned that "failure to exclude ExplorerPatcher's files may result in inability to install/uninstall EP" — via search snippet of [newreleases.io EP 22621.3880.66.7](https://newreleases.io/project/github/valinet/ExplorerPatcher/release/22621.3880.66.7_78e6248)
- TranslucentTB's README: "Some antiviruses are over eager, so they might flag this program as malicious. IT IS NOT! Over 10M users have downloaded this program safely" — [TranslucentTB README](https://github.com/TranslucentTB/TranslucentTB/blob/develop/README.md)
- PowerToys signs its custom-actions DLL separately, before the MSI is built — [PowerToys installer docs](https://microsoft.github.io/PowerToys/core/installer/)

### Inferences
- Caelestia's helper uses low-level keyboard hooks and hides the taskbar, which is the same heuristic profile that got YASB's unsigned exe deleted and EP flagged. Signing **every** PE (helper exe, any DLL, custom-action DLL, MSI, bundle exe) is the baseline. SignPath Foundation (free for OSS, used by YASB and Seelen) is the most-trodden path for a small open-source project.
- Building releases only in CI (EP's argument) and publishing checksums (YASB `checksums.txt`, Seelen `SHA256SUMS.sig`) are cheap trust signals.
- Smart App Control blocks unsigned or low-reputation apps outright on clean Windows 11 installs, so an unsigned installer is effectively unusable for those users (EP's advice to disable SAC shows the problem).

### Gaps
- None of these projects document their SmartScreen reputation build-up time with an OV certificate versus Azure Trusted Signing. No first-party data was found.
- glzr-io's exact certificate type behind AzureSignTool (Key Vault OV/EV or Trusted Signing) is not stated in the workflow.

## 4. Startup registration, running processes on upgrade, elevation

### Takeaway
There are three startup patterns. The **HKCU Run key**, set by the app itself, is used by GlazeWM, Rainmeter and YASB, with the MSI cleaning it up. A **logon scheduled task with highest privileges** is used by Seelen for its elevated service. The **MSIX `windows.startupTask`** is used by TranslucentTB and the Seelen Store build. To handle upgrades over running processes, installers kill the processes first (Seelen `taskkill`, the PowerToys bootstrapper and custom action, Rainmeter `FindWindow`).

### Cited Findings
- The GlazeWM MSI has a component `Custom_RemoveAutoLaunchRegistry` that, on uninstall, removes the `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` value "GlazeWM" **and** the matching `…\Explorer\StartupApproved\Run` value. The app writes these at runtime, and the MSI cleans them up — [standalone.wxs](https://github.com/glzr-io/glazewm/blob/main/resources/wix/standalone.wxs)
- Seelen:
  - The NSIS pre-install hook force-kills processes with `taskkill /F /T /IM slu-service.exe` and `seelen-ui.exe`, then deletes old `static` and `runtime` folders.
  - The post-install step runs `slu-service.exe install`.
  - Pre-uninstall runs `slu-service.exe stop` and then `uninstall`.

  [installer-hooks.nsh](https://github.com/eythaann/Seelen-UI/blob/master/src/templates/installer-hooks.nsh)
- Seelen's "service" is a **Task Scheduler** task "Seelen UI Service" in folder `\Seelen`. It uses `TASK_TRIGGER_LOGON`, `TASK_RUNLEVEL_HIGHEST` and `TASK_LOGON_INTERACTIVE_TOKEN`, and the code migrates and deletes an older task named "Seelen-UI". At launch, the app checks that the task's action path matches the expected exe ("Service task is not pointing to the correct service executable") — [task_scheduler.rs](https://github.com/eythaann/Seelen-UI/blob/master/src/service/task_scheduler.rs), [startup/windows.rs](https://github.com/eythaann/Seelen-UI/blob/master/src/background/platform/startup/windows.rs)
- TranslucentTB's manifest declares `desktop:Extension Category="windows.startupTask"` with `StartupTask TaskId="TranslucentTB" Enabled="true"` plus the `runFullTrust` capability — [Package.appxmanifest](https://github.com/TranslucentTB/TranslucentTB/blob/develop/AppPackage/Package.appxmanifest)
- When "Open at boot" is greyed out, TranslucentTB's README has users set the policy values `EnableFullTrustStartupTasks=2`, `EnableUwpStartupTasks=2`, `SupportFullTrustStartupTasks=1` and `SupportUwpStartupTasks=1`. Portable users must create a Startup-folder shortcut instead — [TranslucentTB README](https://github.com/TranslucentTB/TranslucentTB/blob/develop/README.md)
- Rainmeter's NSIS installer:
  - It detects existing autostart from either `HKCU\…\Run\Rainmeter` or a `Rainmeter.lnk` in the all-users or current-user Startup folder, and accepts `/AUTOSTARTUP=`.
  - It finds the running instance via `FindWindow "DummyRainWClass" "Rainmeter control window"`, uses `Rainmeter.exe /Restart` for restarts, and runs the startup and shortcut functions **as the original user** via `UAC_AsUser_Call`.
  - A source comment notes "UAC_IsAdmin seems to return incorrect results sometimes, so check again before elevating."

  [Installer.nsi](https://github.com/rainmeter/rainmeter/blob/master/Build/Installer/Installer.nsi)
- YASB enables autostart from the app (tray menu "Enable Autostart" or `yasbc enable-autostart`) rather than from the installer — [YASB wiki: Installation](https://github.com/amnweb/yasb/wiki/Installation), [yasb#483](https://github.com/amnweb/yasb/issues/483). An open request asks for a "high priority startup on boot" — [yasb#443](https://github.com/amnweb/yasb/issues/443)
- PowerToys closes running PowerToys from the bootstrapper and uses a C++ custom action for process termination and user impersonation — [PowerToys installer docs](https://microsoft.github.io/PowerToys/core/installer/)

### Inferences
- For Caelestia, which runs at logon and has a hook helper that does not need admin, the **HKCU Run key written by the app, plus MSI `RemoveRegistryValue` for both `Run` and `StartupApproved\Run` on uninstall** is the simplest proven pattern (GlazeWM). A scheduled task is only justified if something must run elevated (Seelen).
- A per-machine MSI that writes HKCU values only reaches the installing user. Rainmeter's `UAC_AsUser_Call` exists precisely to get around the "elevated installer writes to the wrong profile" problem. Letting the app register its own startup avoids it.
- Kill-before-install is universal. With MSI, the clean equivalent is a custom action or `util:CloseApplication` scheduled before `InstallValidate`, or a Burn bundle step (PowerToys).

### Gaps
- I found no first-party write-up comparing Run key versus scheduled-task reliability (for example, delayed start after Explorer).

## 5. Uninstall leftovers and restoring Windows (critical for a taskbar-hiding theme)

### Takeaway
The most-reported pain point is the **taskbar not being restored after uninstall or kill**. Restore logic that runs only on graceful shutdown fails when the uninstaller force-kills the app or when MSIX gives no uninstall hook. Duplicate installs from different channels (winget, scoop, MSI) make it look as if uninstall failed.

### Cited Findings
- Seelen issue #1703 (2026-07-16): after uninstalling the Store build, the native taskbar was left with **auto-hide enabled** (`StuckRects3` byte[8]=3).
  - The maintainer said the MSIX install method "doesn't support custom actions on uninstalling, so orphaned scheduled task or removing of config folder is impossible on msix uninstallations", and that auto-hide is disabled "on app close".
  - The reporter noted that "the restore only fires on a graceful close", not when the app is uninstalled or killed while active.

  [Seelen#1703](https://github.com/eythaann/Seelen-UI/issues/1703)
- Seelen issue #891 (2025-05-27): uninstall did not revert taskbar animations, fonts or window-animation settings. The maintainer said this is "expected… if you don't gracefully stop the app". Users asked for an uninstall script that reverts every changed setting — [Seelen#891](https://github.com/eythaann/Seelen-UI/issues/891). A similar issue is "[BUG] taskbar hiding after uninstall" (#1197, 2025-09-21) — [Seelen#1197](https://github.com/eythaann/Seelen-UI/issues/1197)
- Seelen's v2.8.11 notes include fixes for "Explorer crashing on Windows 10 with the dock enabled… caused by the native taskbar being hidden in an endless loop" (PR #1969, "stop hiding the native taskbar in a loop") — [Seelen release v2.8.11](https://github.com/eythaann/Seelen-UI/releases/latest)
- Zebar issue #148 (2024-11-10): Zebar bars were still visible after uninstall on 2 of 3 screens. The maintainer suspected "a duplicate scoop or winget install" and suggested `which zebar` — [zebar#148](https://github.com/glzr-io/zebar/issues/148)
- ExplorerPatcher drops `C:\Windows\dxgi.dll`, and blocking it prevents explorer.exe from starting. Security products such as SentinelOne can block EP's uninstallation — [EP release notes](https://github.com/valinet/ExplorerPatcher/releases/latest)
- StartAllBack: guides recommend "Disable program for current user" and signing out before uninstalling — [macmyths (secondary)](https://macmyths.com/how-to-uninstall-startallback-windows-11/), [Windows Report (secondary)](https://windowsreport.com/startallback-uninstall/)
- Seelen's NSIS post-uninstall removes only its own files (`sluhk.dll`, `SHA256SUMS`, …). Config in `%LocalAppData%\com.seelen.seelen-ui` is not shown being removed — [installer-hooks.nsh](https://github.com/eythaann/Seelen-UI/blob/master/src/templates/installer-hooks.nsh), [Seelen#891 log path](https://github.com/eythaann/Seelen-UI/issues/891)

### Inferences
- For Caelestia this is the key design lesson. The uninstaller must restore the taskbar **independently of the app's graceful shutdown**. Options:
  - Ask the helper to stop gracefully first, through a named pipe or command such as `helper.exe --restore-and-exit`.
  - Then run a dedicated restore step, a custom action or `helper.exe --restore`, that re-shows `Shell_TrayWnd`/`Shell_SecondaryTrayWnd`, resets the `StuckRects3` auto-hide bit to its saved original value, and restarts Explorer if needed.
  - Only then delete files.
- That rules out pure MSIX for the primary channel unless the helper is also made resilient (see Seelen#1703). An MSIX app gets no uninstall hook.
- Save the user's original taskbar and appearance state at first run and restore exactly that, rather than forcing defaults (Seelen#891 complaints about fonts and animations).
- Detect and warn about duplicate installs (winget, scoop, MSI) of Zebar or Caelestia, as in zebar#148.

### Gaps
- I found no project that documents a tested "restore Windows" uninstall step. This appears to be an unsolved area that Caelestia could do better.

## 6. Theme/skin package formats (Rainmeter .rmskin) and relevance to a Caelestia widget-pack package

### Takeaway
Rainmeter's `.rmskin` is a **modified zip with integrity metadata and an `RMSKIN.ini` manifest**, opened by a file-associated Skin Installer. It installs exactly one root skin folder plus optional layouts and 32/64-bit plugins, backs up same-named skins, can preserve user variable files on upgrade, and can load a skin or layout after install. Zebar already has its own marketplace installer, and Seelen ships community themes, so a "Caelestia pack" format is viable if it follows the .rmskin feature set.

### Cited Findings
- A `.rmskin` is created with the **Skin Packager** (Manage → "Create .rmskin package…"). It is "a modified zip that includes metadata used to verify the package's integrity", and since Rainmeter 2.3 a renamed plain zip is rejected — [Rainmeter docs: Distributing Skins](https://docs.rainmeter.net/manual/distributing-skins/)
- Packager options:
  - Name and Author (required), Version (optional).
  - **One root config folder per package**, which holds resources and fonts.
  - Any number of layouts.
  - Custom plugins, which must include **both 32-bit and 64-bit** versions.
  - After installation, **Load skin** or **Load layout** (load layout replaces the user's current layout).
  - **Minimum Rainmeter version** (for example `4.0.0.2551`) and **Minimum Windows version**.
  - A **400x60 header .bmp**.
  - **Variables files**, pipe-separated include files whose user values are kept on reinstall or upgrade.
  - **Merge skins**, which keeps existing files instead of replacing the folder and cannot be combined with Variables files.
  - The output is named `Name_Version.rmskin`, and hidden files and folders are excluded.

  [Distributing Skins](https://docs.rainmeter.net/manual/distributing-skins/)
- The manifest keys written by the packager and read by the installer, under section `[rmskin]`, are `Name`, `Author`, `Version`, `LoadType` (Skin/Layout), `Load`, `MinimumRainmeter`, `HeaderImage`, `MergeSkins` and `VariableFiles`. The installer's file dialog accepts `*.rmskin;*.zip` — [Rainmeter source: DialogInstall.cpp / DialogPackage.cpp](https://github.com/rainmeter/rainmeter/tree/master/Library)
- Install behaviour:
  - Skins go to the Skins folder, layouts to Layouts, and plugins to Plugins, matched to the system's architecture.
  - A same-named existing skin "is moved to a Backup folder before installation" unless the user turns that off.
  - Legacy packages could install fonts system-wide, but this is unsupported since 2.4.
  - On a normal install the `.rmskin` extension is associated with the Skin Installer, so double-clicking installs. Portable users run `SkinInstaller.exe` manually.

  [Rainmeter docs: Installing Skins](https://docs.rainmeter.net/manual/installing-skins/)
- The Skin Installer needs `Rainmeter.ini` to exist ("Make sure that Rainmeter has been run at least once") to find the skins path — [SkinInstaller.cpp](https://github.com/rainmeter/rainmeter/blob/master/Library/SkinInstaller.cpp)
- Zebar has an in-app `marketplace_installer.rs` for widget packs — [Zebar source tree](https://github.com/glzr-io/zebar/tree/main/packages/desktop/src). Seelen advertises "Install and share themes from the community" — [Seelen README](https://github.com/eythaann/Seelen-UI/blob/master/README.md)

### Inferences
- A Caelestia package could mirror .rmskin as a zip with a manifest (`caelestia-pack.json` or `.ini` with name, author, version, minimum Caelestia/Zebar version, widgets to enable after install, user-config files to preserve on upgrade, optional header image), plus a hash or signature. A file association (`.caelestia` or `.ctpack`) would open the helper's installer UI.
- Preserving user config on upgrade (VariableFiles) and backing up before overwrite are the two features that make skin upgrades safe, and Caelestia should copy both.
- A dedicated format may be redundant if Zebar's own marketplace pack format fits. Check whether Caelestia's widget pack can be published as a standard Zebar marketplace pack before inventing a new format.
- Rainmeter dropped system-wide font installation from packages. If Caelestia packs need fonts, install them per-user (`%LOCALAPPDATA%\Microsoft\Windows\Fonts`, no admin) rather than system-wide.

### Gaps
- The exact binary layout of the .rmskin integrity footer was not confirmed from source in this pass. The docs say only "modified zip with metadata".
- Zebar's marketplace pack format and its manifest schema were not examined.
