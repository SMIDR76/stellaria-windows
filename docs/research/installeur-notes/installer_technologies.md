# Windows installer technologies (state at 2026-11-16) and fit for Caelestia for Windows

Scope reminder: per-user, no admin. Payload = Zebar widget pack to `%USERPROFILE%\.glzr\zebar\caelestia`, NativeAOT helper (+ `e_sqlite3.dll`) to `%USERPROFILE%\.caelestia\bin`, optional .NET Framework WinForms switcher, 3 logon scheduled tasks, merge into Zebar `settings.json` (UTF-8 without BOM), Start menu shortcuts. Uninstall must stop processes, run `caelestia-helper.exe --restore-shell`, then remove everything. Depends on Zebar (Tauri app, MSI + winget).

Method note: version numbers and dates for Inno Setup, WiX, Velopack, Squirrel.Windows and Tauri come from the GitHub API (`gh api repos/<owner>/<repo>/releases`), queried 2026-11-16. Claims marked "(prior knowledge, not re-fetched)" come from well-known docs I did not re-open in this session. The report writer should treat them as lower confidence.

## 1. Technology-by-technology: current version, capabilities, effort and pitfalls for this case

### Takeaway
Inno Setup 7.1.1 (or 6.7.4) is the best fit. It is a mature, free, script-based tool that does per-user installs without admin (`PrivilegesRequired=lowest` puts the uninstall entry in HKCU), has a native dark or modern wizard since 6.6, closes running apps through the Restart Manager, and can run arbitrary programs at install and uninstall. Velopack fits poorly because its hooks are short, UI-less and tied to the app exe. MSIX is ruled out by virtualisation and by having no scheduled tasks. WiX/MSI works but means fighting ICE rules for per-user installs, and WiX v7 adds a maintenance-fee and EULA step. A custom installer is the most effort.

### Cited Findings

**Inno Setup**
- Current releases: 7.1.1 and 6.7.4, both published 2026-10-28. 7.1.0 came out 2026-08-12, 7.0.2 on 2026-07-13 and 7.0.0-beta on 2026-05-18. The 6.x line is still maintained in parallel. — [GitHub API jrsoftware/issrc releases](https://github.com/jrsoftware/issrc/releases); [Inno Setup 7 What's New](https://jrsoftware.org/files/is7-whatsnew.htm)
- Inno Setup 7 adds 64-bit installers and a 64-bit IDE/ISCC, but "Building 32-bit installers is the default in both editions". 7.0.0 adds extended-length path support throughout Setup and Uninstall. — [is7-whatsnew](https://jrsoftware.org/files/is7-whatsnew.htm)
- Breaking changes in 7.x:
  - `EnableFsRedirection` was removed in 7.0.0.
  - `AppVerName` now defaults to "AppName AppVersion".
  - `TimeStampsInUTC` now defaults to yes.
  - 7.0.2 fails compilation on bytes invalid for the script's code page.
  - 7.1.0 sends ISCC warnings to stderr and adds a JSON-lines output option `--messages-jsonl`, which is useful for CI.
  - 7.1.1 notes that old scripts double-scaled `CreateCustomForm`.

  — [is7-whatsnew](https://jrsoftware.org/files/is7-whatsnew.htm)
- Dark mode in the installer UI: "Setup and Uninstall now both support dark mode, forced or automatic" (6.6.0, 2025-11-11). `WizardStyle` gained the appearance modes light, dark and dynamic (`WizardStyle=modern dynamic`) plus built-in styles such as polar and zircon. Segoe UI became the default font, and `WizardResizable` was dropped. 6.5 had dark mode in the Compiler IDE only. — [is6-whatsnew](https://jrsoftware.org/files/is6-whatsnew.htm); [6.6.0 announcement](https://groups.google.com/g/innosetup/c/XLS5iqCc8Dw)
- 7.0.0 adds the built-in light style "stellar". 7.1.1 adds dark menus on Windows 11 26H2. — [is7-whatsnew](https://jrsoftware.org/files/is7-whatsnew.htm)
- 6.7 (6.7.0 on 2026-01-06 through 6.7.4 on 2026-10-28) added:
  - custom wizard background colours and images (`WizardBackColor`, `WizardBackImageFile` and their DynamicDark variants);
  - RedirectionGuard enabled by default on Windows 11 and Windows 10 22H2;
  - an experimental x64 loader.

  6.7.4 fixed RedirectionGuard being inherited by child processes, which matters for `[Run]` entries, and fixed a COM double-release that could crash MSXML users. — [is6-whatsnew](https://jrsoftware.org/files/is6-whatsnew.htm)
- 6.5 (2025-08-12) added:
  - a `[Files]` `download` flag, the `ArchiveExtraction` directive and the `ExtractArchive` function;
  - `ISSigTool` with `[ISSigKeys]` for ECDSA file-signature verification, which is unrelated to Authenticode;
  - the first commercial licences.

  6.5.2 removed the 2 GB Setup.exe limit and TLS 1.0/1.1. — [is6-whatsnew](https://jrsoftware.org/files/is6-whatsnew.htm)
- 6.3.0 (2024-06-09) dropped Vista and Server 2008, making Windows 7 / Server 2008 R2 the minimum, and added official Arm64 (`x64compatible`). — [is6-whatsnew](https://jrsoftware.org/files/is6-whatsnew.htm)
- `PrivilegesRequired=lowest`: Setup doesn't elevate, the uninstall info root key is always HKCU, and "common" shell-folder constants map to their per-user forms. — [Inno Setup help: PrivilegesRequired](https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm)
- 6.7.0 notes that with `PrivilegesRequired=lowest` Setup's processes are not elevated, which can still produce a restart error on Windows Server. — [is6-whatsnew](https://jrsoftware.org/files/is6-whatsnew.htm)
- Community report: if a `lowest` installer was run elevated, the uninstaller may ask for elevation. Users suggest `PrivilegesRequired=none` or care in mixed cases. — [SO mirror: inno setup uninstall without administrator privilege](https://kiwix.lema.org/stackoverflow.com_en_all_2023-11/questions/33155666/inno-setup-uninstall-without-administrator-privilege)
- Restart Manager: `CloseApplications` uses the Windows Restart Manager. `force` passes RmForceShutdown to RmShutdown and `yes` does not. A developer recommends stopping things yourself in `PrepareToInstall`, because the Restart Manager checks run after that step. — [Inno Setup group thread](https://groups.google.com/g/innosetup/c/bB5_F-K2pNE)
- `CloseApplications` defaults to yes, `RestartApplications` exists, and `CloseApplicationsFilter` defaults to `*.exe,*.dll,*.chm`. Extra resources can be registered with `RegisterExtraCloseApplicationsResource`, whose signature changed in 7.0.0. — (prior knowledge, not re-fetched) [Inno Setup help: CloseApplications](https://jrsoftware.org/ishelp/topic_setup_closeapplications.htm); 7.0.0 change: [is7-whatsnew](https://jrsoftware.org/files/is7-whatsnew.htm)
- `[Run]`, `[UninstallRun]` (with a `RunOnceId`) and Pascal Script events (`CurStepChanged`, `CurUninstallStepChanged`, `InitializeUninstall`, `PrepareToInstall`) let you run `schtasks.exe`, PowerShell or the helper exe itself during install and uninstall. Pascal Script can `CreateOleObject('Schedule.Service')` to call the Task Scheduler COM API. Inno Setup has no built-in JSON parser: you write JSON via `LoadStringFromFile`/`SaveStringToFile` (ANSI or UTF-8 handling has to be checked), or, more simply, delegate JSON merging to the C# helper. — (prior knowledge, not re-fetched) [Inno Setup help contents](https://jrsoftware.org/ishelp/contents.htm). The 7.x What's New I read does not mention any new JSON or Task Scheduler helpers. — [is7-whatsnew](https://jrsoftware.org/files/is7-whatsnew.htm)
- Signing: Inno Setup recommends the `SignTool` directive. Self-signed uninstallers and temporary copies can be blocked by security software, so `SignedUninstaller=yes` is suggested when SignTool can't be used. — [is7-whatsnew](https://jrsoftware.org/files/is7-whatsnew.htm)
- Licence: the repo licence is reported as NOASSERTION (a custom Inno Setup licence). Commercial licences exist since 6.5 and include two years of updates. — [GitHub API jrsoftware/issrc](https://github.com/jrsoftware/issrc); [is7-whatsnew](https://jrsoftware.org/files/is7-whatsnew.htm)

**NSIS**
- NSIS 3.11 was released 2025-03-08. Its changelog covers command-line handling, reproducible builds and translations, and nothing about execution levels. — [Neowin NSIS 3.11](https://www.neowin.net/amp/nsis-nullsoft-scriptable-install-system-311); [NSIS download](https://nsis.sourceforge.io/Download)
- A single unverified listing mentions NSIS 3.12 on 2026-04-19. — [Adaptiva patch library](https://adaptiva.com/patch-library/1000000891_patch_for_nsis) (unverified)
- `RequestExecutionLevel none|user|highest|admin` only changes the exe manifest. A per-user installer should write only to HKCU and to folders inside the user profile. — [NSIS RequestExecutionLevel](https://nsis.sourceforge.io/Reference/RequestExecutionLevel); [NSIS install-per-user example](https://nsis.sourceforge.io/Examples/install-per-user.nsi)
- MultiUser.nsh: with the Standard execution level the install is always single-user. Admin/Power and Highest allow per-machine or per-user, chosen with `/AllUsers` and `/CurrentUser`. — [NSIS MultiUser Readme](https://nsis.sourceforge.io/Docs/MultiUser/Readme.html)
- Electron-builder calls its Squirrel.Windows target "maintained, but deprecated" and recommends NSIS. This shows NSIS is still a mainstream choice in the JS desktop world. — [search summary: electron-forge/electron-builder discussion](https://lightrun.com/answers/electron-forge-does-forge-plan-to-replace-squirrel-windows-with-nsis-or-other-alternatives-like-electron-builder-)
- NSIS has no built-in Restart Manager support. Files in use are handled by plugins (e.g. nsProcess, LockedList) or by scripted checks. — (prior knowledge, not re-fetched; no source found this session)

**Tauri bundler (NSIS / WiX)**
- Tauri is at 2.12.6 (2026-11-16). — [GitHub API tauri-apps/tauri releases](https://github.com/tauri-apps/tauri/releases)
- The NSIS target has `bundle > windows > nsis > installMode` with the values `perUser` (no admin, `%LOCALAPPDATA%`), `perMachine` or `both`, and `installerHooks` (a `.nsh` file). This comes from third-party skill pages, not the official docs. — [Tauri windows distribution skill page](https://playbooks.com/skills/dchuk/claude-code-tauri-skills/tauri-windows-distribution)
- Tauri changelog fixes include reusing the previous install path in perMachine/currentUser modes, and an NSIS perMachine installer launching the app as admin. — [tauri-bundler v1.4.7 release notes](https://v2.tauri.app/release/tauri-bundler/v1.4.7); [tauri-bundler 2.0.1-beta.17](https://v2.tauri.app/es/release/tauri-bundler/v2.0.1-beta.17)
- Fit: the Tauri bundler packages a Tauri app. Caelestia has no Tauri binary of its own (it is a pack for Zebar, which is the Tauri app), so the bundler doesn't fit unless you reuse its NSIS templates by hand. — (inference)

**WiX Toolset / MSI**
- WiX v7.0.0 was released 2026-04-06 (rc.1 2026-02-06, rc.2 2026-03-05) and v7.0.1 on 2026-10-26. — [GitHub API wixtoolset/wix releases](https://github.com/wixtoolset/wix/releases)
- Open Source Maintenance Fee: "first introduced in WiX v6". "Organizations that generate more than $10,000 in annual revenue" must sponsor the wixtoolset GitHub org. Starting in v7 the EULA must be accepted, with `AcceptEula=wix7` (MSBuild), `wix build -acceptEula wix7`, or `wix eula accept wix7` once per user and machine. The page does not say what applies below the threshold. — [FireGiant OSMF](https://docs.firegiant.com/wix/osmf/)
- One third-party packaging guide pins v5.0.2 because v6/v7 need EULA acceptance, which blocks unattended builds. — [pyappdist WiX MSI](https://pyappdist.readthedocs.io/en/latest/formats/msi.html)
- Dual-scope packages: `Package/@Scope='perUserOrMachine'` is supported and defaults to per-user, and `perMachineOrUser` was planned for v7. — [WiX Online Meeting 300 highlights (2025-10-15)](https://www.firegiant.com/blog/2025/10/15/wix-online-meeting-300-highlights/)
- The WiX docs list "Configurable scope bundles" and "Out-of-process bootstrapper applications" as new features. — [FireGiant What's new index](https://docs.firegiant.com/wix/whatsnew/) (I only saw the navigation; I did not read the page bodies)
- ICE rules that fire on per-user packages:
  - ICE38 needs components under the user profile to use an HKCU registry value as KeyPath; the usual fix is a dummy HKCU value.
  - ICE64 needs RemoveFolder for every profile directory created.
  - ICE91 can be suppressed when the package is per-user only.
  - ICE57 flags mixed per-user and per-machine data.

  These are long-standing WiX 3-era threads; I found no v6/v7-specific guidance. — [Aaron Stebner: per-user MSI with WiX 3.0](https://learn.microsoft.com/en-us/archive/blogs/astebner/using-wix-3-0-to-create-a-per-user-msi-that-does-not-prompt-for-elevation-on-windows-vista); [Microsoft ICE38](https://learn.microsoft.com/en-ie/Windows/Win32/msi/ice38); [wix-users ICE38 thread](https://go.mail-archive.com/wix-users@lists.sourceforge.net/msg35152.html)
- Microsoft's Uninstall-key page maps ARP values to MSI properties: DisplayName = ProductName, Publisher = Manufacturer, URLInfoAbout = ARPURLINFOABOUT, HelpLink = ARPHELPLINK, InstallLocation = ARPINSTALLLOCATION. UninstallString, ModifyPath and EstimatedSize are "Determined and set by Windows Installer". MSI therefore fills ARP automatically. — [Microsoft Learn: Uninstall Registry Key](https://learn.microsoft.com/en-us/windows/win32/msi/uninstall-registry-key)
- Windows Installer integrates natively with the Restart Manager: the FilesInUse/MsiRMFilesInUse dialog and `MSIRESTARTMANAGERCONTROL`. Per-user MSIs use `ALLUSERS=""`, or `ALLUSERS=2` + `MSIINSTALLPERUSER=1` for dual-purpose packages on Windows 7+. — (prior knowledge, not re-fetched) [Microsoft Learn: Using Windows Installer with Restart Manager](https://learn.microsoft.com/en-us/windows/win32/msi/using-windows-installer-with-restart-manager); [MSIINSTALLPERUSER](https://learn.microsoft.com/en-us/windows/win32/msi/msiinstallperuser); [Single package authoring](https://learn.microsoft.com/en-us/windows/win32/msi/single-package-authoring)
- Custom actions (scheduled tasks, JSON merge, `--restore-shell`): an MSI needs deferred custom actions with rollback counterparts. A per-user MSI cannot use the elevated system context, and in-script custom actions (DTF/C#) must be AOT-safe or rely on the DTF CLR host. — (prior knowledge / inference; no 2026 source found)

**MSIX**
- On Windows 10 1903 and later, new files created under AppData (Local, Roaming, and their Microsoft subfolders) are redirected to a per-user, per-package private location and merged back at runtime for the packaged app. On 1809 and earlier, all AppData writes are copy-on-write. This matches the AppData virtualisation the project already observed. — [Microsoft Learn: Flexible virtualization](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization)
- Opting out:
  - `desktop6:FileSystemWriteVirtualization` set to disabled, or
  - the restricted capability `unvirtualizedResources`.

  Microsoft calls this coarse-grained: it disables AppData virtualisation entirely. — [Flexible virtualization](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization)
- "Apart from AppData", a packaged app can write anywhere the user has write access, including other parts of `%USERPROFILE%`, so `%USERPROFILE%\.glzr` and `%USERPROFILE%\.caelestia` are not redirected at runtime. However, the MSIX package itself cannot deploy files there. Advanced Installer warns that if an app expects files installed under the user profile, the OS won't virtualise them and the app fails to find its resources. — [Flexible virtualization](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization); [Advanced Installer: user settings data in MSIX](https://www.advancedinstaller.com/application-packaging-training/msix-packaging/ebook/user-settings-data-associated.html)
- Autostart: packaged apps should use the `windows.startupTask` extension, which runs after sign-in and is user-controllable. Scheduled tasks pointing at packaged apps are awkward: Task Scheduler needs a plain exe, though an execution alias is a reported workaround. Registry Run keys written by packaged apps "fail silently". MSIX has no install-time custom actions, so it cannot register scheduled tasks or edit Zebar's `settings.json` at install time. — [Advanced Installer: startup programs in MSIX](https://www.advancedinstaller.com/startup-programs-msix.html); [SO mirror: MSIX packaging and Task Scheduler](https://kiwix.lema.org/stackoverflow.com_en_all_2023-11/questions/62087746/msix-packaging-and-task-scheduler); [DEV: WPF to winget/Store autostart bug](https://dev.to/phantasmat2018/shipping-a-wpf-side-project-to-winget-and-the-microsoft-store-and-the-autostart-bug-that-only-2oap); [MS Tech Community: execution alias as task target](https://techcommunity.microsoft.com/discussions/msix-discussions/msix-app-execution-alias-from-cannot-be-selected-from-task-scheduler/3805369/replies/3807279)
- `windows.startupTask` is reported as not enabled by default and as unreliable on some machines. — [MS Tech Community: StartupTask not enabled by default](https://techcommunity.microsoft.com:443/t5/msix/startuptask-not-enabled-by-default-why/td-p/3875695); [MS Tech Community: MSIX will not launch on login](https://techcommunity.microsoft.com/discussions/msix-discussions/msix-will-not-launch-run-on-user-login-using-windows-startuptask-in-manifest-on-/1473940)

**Velopack / Squirrel.Windows**
- Velopack: latest stable 1.2.161 (2026-09-29) and prerelease 1.3.39 (2026-10-17). MIT licence, actively developed (push on 2026-11-16). — [GitHub API velopack/velopack releases](https://github.com/velopack/velopack/releases); [NuGet Velopack](https://www.nuget.org/packages/velopack)
- Default install location is `%LocalAppData%\{packId}`, overridable with `Setup.exe --installto {dir}`. The app lives in a `current` folder behind a stub exe. Setup.exe is per-user and needs no elevation. An MSI option (`--msi --instLocation PerMachine`) does per-machine installs to Program Files with elevation. Setup.exe is a "one-click installer" with a splash image or progress bar, not a multi-page wizard. — [Velopack: Windows overview](https://docs.velopack.io/packaging/operating-systems/windows)
- Updates replace the whole `current` folder. The updater closes processes running from `current`, then looks for other lockers and asks the user to kill them or cancel. If it can't identify a locker (for example one running as admin), it shows an error and relaunches the old version. — [Velopack: Windows overview](https://docs.velopack.io/packaging/operating-systems/windows)
- Hooks launch the main exe with `--veloapp-install` (30 s), `--veloapp-obsolete` (15 s), `--veloapp-updated` (15 s) and `--veloapp-uninstall` (30 s). "If your application … does not exit within the alloted time, it will be killed". There is no way to cancel and no UI may be shown. `VELOPACK_FIRSTRUN` and `VELOPACK_RESTART` are set as environment variables. — [Velopack: hooks](https://docs.velopack.io/integrating/hooks)
- The FAQ says creating shortcuts or registry entries in hooks is no longer required. I found no official example of registering scheduled tasks in a hook. — [Velopack FAQ](https://docs.velopack.io/troubleshooting/faq)
- Squirrel.Windows: last release 2.0.1 on 2020-09-27, last push 2024-07-24, not formally archived. — [GitHub API Squirrel/Squirrel.Windows](https://github.com/Squirrel/Squirrel.Windows)
- Clowd.Squirrel now redirects to Velopack and describes Squirrel.Windows as having "fallen into a state of disrepair". Velopack provides a migration guide. — [Clowd.Squirrel repo](https://www.GitHub.com/clowd/Clowd.Squirrel); [Velopack: migrating from Squirrel](https://docs.velopack.io/migrating/squirrel)

**Advanced Installer**
- Projects that use only freeware ("Simple") features need no licence, and the resulting packages are freely redistributable, commercial use included. — [Advanced Installer: Licensing](https://www.advancedinstaller.com/licenses.html)
- Open-source developers can request a free licence by emailing their name and project details; the company says it answers in 1–2 business days. — [Advanced Installer: Free license](https://www.advancedinstaller.com/free-license.html)
- Advanced Installer is a GUI-first, Windows-only tool that outputs MSI/EXE/MSIX. I found no source this session that says whether Simple projects support per-user scope or custom actions. — (gap)

**Fully custom installer (WPF / WinUI 3 / Avalonia / NativeAOT with an embedded payload)**
- I found no 2026 post-mortem for this session. You would rebuild ARP registration, upgrade detection, file-in-use handling (Restart Manager P/Invoke `RmStartSession`/`RmRegisterResources`/`RmGetList`), rollback, a self-deleting uninstaller and code signing yourself. — (gap / inference)

### Inferences
- **Recommended: Inno Setup 7.1.x**, or 6.7.4 if you want the conservative line. It has:
  - `PrivilegesRequired=lowest`, so no UAC and ARP under HKCU;
  - `WizardStyle=modern dynamic`, which follows the Windows light/dark setting and suits a theme project;
  - Restart Manager `CloseApplications`;
  - `[UninstallRun]` to call `caelestia-helper.exe --restore-shell` before files are deleted;
  - Pascal Script for the edge cases.

  Keep the logic in the C# helper (for example `caelestia-helper.exe --install-tasks`, `--merge-zebar-settings`, `--uninstall`). The installer then only copies files and calls the helper. That avoids Inno Setup's weak JSON and UTF-8 handling, and the same helper code serves both the PowerShell install path and the GUI installer.
- **NSIS** is a viable second choice: free and very flexible. But it has more boilerplate (the MUI2 look is dated, and there's no native dark mode or Restart Manager), so it costs more effort for the same result.
- **WiX/MSI** offers proper transactions and native Restart Manager support. But:
  - a per-user MSI under `%USERPROFILE%\.glzr` triggers ICE38/ICE64 work;
  - scheduled tasks, the JSON merge and `--restore-shell` all become custom actions;
  - WiX v7 requires EULA acceptance in CI, plus the OSMF if the publisher's organisation earns over $10k a year.

  Too heavy for this project.
- **MSIX is unsuitable**: it can't write the install targets, can't create scheduled tasks or edit another app's `settings.json` at install time, and virtualises AppData writes, as already observed.
- **Velopack** fits only if auto-updates become the top priority. Its 30 s, UI-less hooks would run the helper exe, which happens to be the right shape: the helper could register tasks in `--veloapp-install` and restore the shell in `--veloapp-uninstall`. But you lose a wizard (no options page, licence page or Zebar prerequisite check). Its install root is `%LocalAppData%\caelestia`, so the Zebar pack copy into `%USERPROFILE%\.glzr` would happen in a hook. It also takes over the helper's `Main`, which needs care with NativeAOT.
- **Tauri bundler: not applicable.** **Squirrel.Windows: do not use**; it has had no release since 2020-09-27.

### Gaps
- I read neither the official Inno Setup `[Run]`/`[UninstallRun]`/`CloseApplications` help pages nor the Restart Manager portal in this session. Those details are prior knowledge.
- WiX v7 per-user ICE guidance and the out-of-process BA (WPF) docs: the FireGiant pages returned only navigation to my fetch.
- Whether Advanced Installer's free tier supports per-user scope and custom actions.
- Whether NSIS 3.12 exists (only one unverified listing).
- Velopack: no official example of scheduled-task registration in hooks, and no info on NativeAOT compatibility of the `VelopackApp.Build().Run()` integration.

## 2. Per-user vs per-machine, no-admin installs, and the per-user Add/Remove Programs entry

### Takeaway
Inno Setup (`lowest`), NSIS (`RequestExecutionLevel user`), Velopack (Setup.exe) and Tauri's NSIS (`perUser`) all do per-user, no-admin installs cleanly. MSI can do it but fights ICE rules. The ARP entry under HKCU uses the same value names as HKLM. Inno, Velopack and MSI write it automatically; with NSIS or a custom installer you write it yourself.

### Cited Findings
- Inno Setup with `lowest` writes uninstall info to HKCU. — [Inno Setup help: PrivilegesRequired](https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm)
- An NSIS per-user installer should write only to HKCU and the user profile. — [NSIS install-per-user example](https://nsis.sourceforge.io/Examples/install-per-user.nsi)
- Velopack Setup.exe is per-user without elevation; its MSI per-machine mode writes HKLM and elevates. — [Velopack: Windows overview](https://docs.velopack.io/packaging/operating-systems/windows)
- Documented Uninstall-key values: DisplayName, DisplayVersion, Publisher, VersionMajor/Minor, Version, HelpLink, HelpTelephone, InstallDate, InstallLocation, InstallSource, URLInfoAbout, URLUpdateInfo, Comments, Contact, EstimatedSize, Language, ModifyPath, Readme, UninstallString and SettingsIdentifier. The page documents HKLM only, keyed by the MSI ProductCode. — [Microsoft Learn: Uninstall Registry Key](https://learn.microsoft.com/en-us/windows/win32/msi/uninstall-registry-key)
- Other conventional values: `QuietUninstallString`, `DisplayIcon`, `NoModify`/`NoRepair` (DWORD 1), and `EstimatedSize` (DWORD in KB). These are widely used and written by Inno Setup, but they are not on that Microsoft page. — (prior knowledge, not re-fetched)
- Inno Setup writes `Inno Setup: App Path`, `QuietUninstallString` (`/SILENT`), `EstimatedSize`, `NoModify`/`NoRepair` and `DisplayIcon` (from `UninstallDisplayIcon`) under the key `{AppId}_is1`. — (prior knowledge, not re-fetched)

### Inferences
- For Caelestia, set `AppId` to a fixed GUID, never change it, and use `UninstallDisplayIcon` pointing to the helper or switcher exe. Ship `AppPublisherURL`/`AppSupportURL` (GitHub repo) for URLInfoAbout and HelpLink. The ARP entry then lives at `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\{GUID}_is1`.
- Even though the payload lands in two dotfolders, the Inno `{app}` directory could be `%USERPROFILE%\.caelestia` (DisableDirPage=yes). The widget pack then goes to `{%USERPROFILE}\.glzr\zebar\caelestia` as an extra `[Files]` destination, with `[UninstallDelete]` for leftovers.

### Gaps
- I found no official Microsoft page this session that lists `QuietUninstallString`, `NoModify`, `NoRepair` and `DisplayIcon` for app developers.
- I didn't verify how Windows 11 Settings > Apps treats per-user ARP entries (for example whether a Modify button shows) in 2026.

## 3. Repair, modify, upgrade and uninstall flows; detecting previous versions; keeping settings

### Takeaway
Inno Setup detects a previous install by `AppId` and upgrades in place over the same directory. It has no real repair or modify, but you can simply re-run the same setup. MSI has the most formal model (MajorUpgrade, repair), Velopack has the best automatic updates, and MSIX handles updates natively but is ruled out.

### Cited Findings
- The Velopack updater replaces the `current` folder on update. User data must live outside it ("Preserving Files" guide). Hooks `--veloapp-obsolete` and `--veloapp-updated` run around updates. — [Velopack: Windows overview](https://docs.velopack.io/packaging/operating-systems/windows); [Velopack: hooks](https://docs.velopack.io/integrating/hooks)
- Velopack migrates Squirrel installs on the first update and repairs shortcuts. — [Velopack: migrating from Squirrel](https://docs.velopack.io/migrating/squirrel)
- Tauri's NSIS installer reuses the previous install path as its default in perMachine/currentUser modes, after a fix. — [tauri-bundler v1.4.7](https://v2.tauri.app/release/tauri-bundler/v1.4.7)
- In MSI, ARP `ModifyPath` and `UninstallString` are set by Windows Installer. — [Microsoft Learn: Uninstall Registry Key](https://learn.microsoft.com/en-us/windows/win32/msi/uninstall-registry-key)
- In Inno Setup, an upgrade is just a re-run with the same `AppId`: `UsePreviousAppDir`, `UsePreviousTasks` and similar default to yes, and the uninstall log is appended. `[UninstallDelete]` removes leftovers, and user data survives unless listed there. — (prior knowledge, not re-fetched) [Inno Setup help contents](https://jrsoftware.org/ishelp/contents.htm)

### Inferences
- Keep user state (Caelestia settings, the saved accent colours used by `--restore-shell`, the SQLite DB) in a location the installer never overwrites, for example `%USERPROFILE%\.caelestia\state`, not under `bin`. Have the helper's `--restore-shell` read it before uninstall deletes `bin`.
- For the Zebar `settings.json` merge, make it idempotent: the helper adds or updates only Caelestia entries, writing UTF-8 without BOM. Uninstall then removes only those entries, which survives upgrades.
- Updates: Inno has no auto-update. A lightweight approach is for the helper to check GitHub Releases and download or run the new setup with `/SILENT`, or to use winget for updates.

### Gaps
- I found no source for Inno Setup's behaviour when downgrading, or for best practices for "Modify" with per-user Inno installs in Windows 11 Settings.

## 4. Files in use (running helper, switcher, Zebar) and Restart Manager support

### Takeaway
MSI and Inno Setup both use the Windows Restart Manager. Velopack has its own process-kill logic, and NSIS needs plugins or a script. For Caelestia, the reliable approach in every tool is to stop the processes explicitly before files are touched, and to end the scheduled tasks first so they don't relaunch. Don't rely on the Restart Manager alone, because Zebar is a separate app and Caelestia's processes are relaunched by scheduled tasks.

### Cited Findings
- Inno Setup's `CloseApplications` uses the Restart Manager; `force` adds RmForceShutdown. One recommendation is to stop services or processes in `PrepareToInstall`, because the Restart Manager checks run afterwards. — [Inno Setup group thread](https://groups.google.com/g/innosetup/c/bB5_F-K2pNE)
- The Velopack updater tries to close processes running from `current`, then asks the user about other lockers, and falls back to an error and relaunch. — [Velopack: Windows overview](https://docs.velopack.io/packaging/operating-systems/windows)
- Windows Installer supports the Restart Manager through the MsiRMFilesInUse dialog. — (prior knowledge, not re-fetched) [Microsoft Learn: Using Windows Installer with Restart Manager](https://learn.microsoft.com/en-us/windows/win32/msi/using-windows-installer-with-restart-manager)
- 6.7.4 fixed RedirectionGuard being inherited by child processes. This is relevant when Setup spawns the helper or `schtasks`. — [is6-whatsnew](https://jrsoftware.org/files/is6-whatsnew.htm)

### Inferences
- Concrete Inno sequence:
  - **Install or upgrade, in `PrepareToInstall`:** `schtasks /End` the three tasks, then call `caelestia-helper.exe --shutdown` if the old exe exists (or `taskkill /IM caelestia-helper.exe`). Leave `CloseApplications=yes` as a safety net.
  - **After files are copied, in `[Run]`:** register the tasks via the helper (Task Scheduler COM from C#, or `schtasks /Create /XML`), then start them.
  - **Uninstall:** in `[UninstallRun]` (which runs before files are deleted), first run `caelestia-helper.exe --restore-shell`, then end and delete the tasks, then kill processes.
- Restarting Zebar after changing `settings.json` is needed anyway. Zebar is outside the Caelestia folders, so the Restart Manager would only see it if its exe were a registered resource. An explicit kill and relaunch through its task is simpler.

### Gaps
- I didn't confirm whether Inno's `[UninstallRun]` runs before or after its own Restart Manager close step during uninstall.
- I didn't confirm that NativeAOT single-file exes behave normally with RmGetList and Restart Manager registration (no source found).

## 5. Custom actions: PowerShell, schtasks, Task Scheduler COM, JSON editing, and security pitfalls

### Takeaway
Inno (`[Run]`/Pascal + COM), NSIS (`ExecWait`/plugins) and a custom installer make custom steps easy. MSI makes them hard (deferred, rollback, impersonation). Velopack limits them to 30 s UI-less hooks in your own exe, and MSIX doesn't allow them. The safest design for every tool is one signed helper exe that does the work, called with fixed arguments.

### Cited Findings
- Velopack hooks: fixed time limits (15–30 s), no UI, no way to cancel, and processes are killed on timeout. — [Velopack: hooks](https://docs.velopack.io/integrating/hooks)
- MSIX: a packaged app can't create scheduled tasks or Run keys; use the `windows.startupTask` extension instead. — [Advanced Installer: startup programs in MSIX](https://www.advancedinstaller.com/startup-programs-msix.html); [DEV community post](https://dev.to/phantasmat2018/shipping-a-wpf-side-project-to-winget-and-the-microsoft-store-and-the-autostart-bug-that-only-2oap)
- Tauri NSIS supports `installerHooks` (`.nsh`) for custom steps. — [Tauri windows distribution skill page](https://playbooks.com/skills/dchuk/claude-code-tauri-skills/tauri-windows-distribution)
- Inno Setup 6.7+ enables RedirectionGuard (a mitigation against junction/symlink redirection attacks) by default on supported Windows. 6.7.4 stopped child processes from inheriting it, so tools launched from `[Run]` behave normally. — [is6-whatsnew](https://jrsoftware.org/files/is6-whatsnew.htm)
- Inno Setup's docs note that a self-signed uninstaller or temporary copy may be blocked by security software, and recommend `SignTool` or `SignedUninstaller`. Velopack calls signing "very recommended (but not required)", warning unsigned apps may be flagged as viruses. — [is7-whatsnew](https://jrsoftware.org/files/is7-whatsnew.htm); [Velopack: Windows overview](https://docs.velopack.io/packaging/operating-systems/windows)

### Inferences
Security pitfalls for Caelestia's custom actions:
1. **Calling `powershell.exe -File` from an installer:** ExecutionPolicy, AMSI and AV heuristics often flag installers that launch PowerShell, and a script in a user-writable temp dir can be swapped. Prefer the compiled helper.
2. **Scheduled tasks:** create them with full absolute paths to exes in user-owned folders, `/RL LIMITED` (no highest privileges), and logon trigger for the current user only. Use Task Scheduler XML or COM rather than string-concatenated `schtasks` arguments, to avoid quoting and injection issues with usernames that contain spaces.
3. **No elevation anywhere**, so a writable `%USERPROFILE%` payload is not a privilege-escalation vector. Never add `RL HIGHEST` tasks pointing at user-writable exes.
4. **`settings.json`:** parse and serialise with System.Text.Json in the helper, write atomically (temp file plus replace), back up the original, and use UTF-8 without BOM. Inno's `SaveStringToFile` cannot guarantee that easily.
5. **Signing:** sign the setup exe, the uninstaller, the helper and the switcher. Unsigned NativeAOT exes plus an unsigned installer are prime SmartScreen and AV false-positive targets. Free OSS signing options (e.g. SignPath Foundation, Azure Trusted Signing) are worth a separate look.

### Gaps
- I couldn't retrieve current official guidance on custom-action security (e.g. Microsoft's MSI custom action security page, or Inno Setup docs on `[Run]` with `runasoriginaluser`/`runascurrentuser`) in this session.
- I found no 2026 developer post-mortems comparing Inno vs NSIS vs WiX vs Velopack specifically. The comparison above is synthesised from the primary docs.
