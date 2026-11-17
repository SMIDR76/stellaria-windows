# Releasing

## What a release contains

| File | Contents |
| --- | --- |
| `StellariaSetup-<version>.exe` | Per-user Inno Setup installer (English and French). |
| `Stellaria-<version>-portable.zip` | The pack, the helper, the switcher, the Stellaria app, `scripts\install.ps1` and `scripts\uninstall.ps1`. |
| `SHA256SUMS.txt` | SHA-256 of the two files above. |
| `update.json`, `update.json.minisig` | The update manifest read by the Stellaria app (version, setup name, size and SHA-256), signed with the catalogue key. Only for `x.y.z` versions built with `STELLARIA_SIGNING_KEY`. |

Every file also gets a build provenance attestation, verifiable with
`gh attestation verify <file> -R SMIDR76/stellaria-windows`.

## Steps

1. Set the version in `helper/Directory.Build.props`, `pack/zpack.json`, `package.json`, `launcher/Cargo.toml` and
   `launcher/tauri.conf.json`, write the highlights of the version in `.github/release-notes/v1.2.3.md` (a short
   "What's new" list in English), and commit.
2. Tag the commit and push the tag: `git tag v1.2.3` then `git push origin v1.2.3`.
3. The **Release** workflow builds on `windows-latest` (NativeAOT helper, switcher, Stellaria app, setup, archive,
   checksums), checks that the helper and the app need no Visual C++ runtime, then on `ubuntu-latest` verifies the checksums, attests the files,
   creates a draft release, uploads the files and publishes it. The release page is built by
   `scripts/release-notes.py` from `.github/release-notes/template.md`: a download button, the highlights, an install
   table with the file sizes, a signing notice, how to check the files, and the pull requests grouped by
   `.github/release.yml` (features, fixes, security, dependencies, other) in a collapsed list.
4. Upload the setup to [VirusTotal](https://www.virustotal.com) and, if Microsoft Defender flags it, submit it as a
   software developer at <https://www.microsoft.com/wdsi/filesubmission>.

Running the workflow by hand (**Actions > Release > Run workflow**) builds a `-dev` version and keeps the files as a
workflow artifact without publishing anything.

## Repository settings

- **Immutable releases** (Settings > General > Releases): once published, a release's tag and files can no longer be
  changed or replaced.
- Workflows use the least permissions they need and pin every action to a commit.

## Building locally

`scripts\build-release.ps1 [-Version 1.2.3] [-SkipInstaller]` produces the same files in `artifacts\`. It needs the
.NET 10 SDK, the Visual Studio C++ build tools (for NativeAOT and the app), Rust (`rustup`, stable MSVC toolchain) and, unless `-SkipInstaller` is given, Inno Setup 6.7 or
later (`winget install -e --id JRSoftware.InnoSetup --scope user`). Inno Setup is free for non-commercial use, which
covers this project.

## Testing an installer

The Claude desktop app and other packaged (MSIX) apps redirect some registry and AppData writes of the programs they
start, so an installer launched from them is not a real test. Run it from Explorer, a normal terminal or a scheduled
task, and check: the entry in Installed apps, the three `Stellaria - *` logon tasks, the Stellaria entries in
`%USERPROFILE%\.glzr\zebar\settings.json`, then an uninstall after `taskkill /F /IM stellaria-helper.exe` (the taskbar
and Windows colours must come back).

## Later

Code signing through SignPath Foundation is wired into the Release workflow and turns on once the SignPath variables
exist; see [code-signing.md](code-signing.md). winget and Scoop manifests are planned; the reasoning is in
`docs/research/`. The app's updates need the repository to be public: GitHub answers 404 to
`releases/latest/download/update.json` on a private repository.
