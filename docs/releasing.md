# Releasing

## What a release contains

| File | Contents |
| --- | --- |
| `CaelestiaSetup-<version>.exe` | Per-user Inno Setup installer (English and French). |
| `Caelestia-<version>-portable.zip` | The pack, the helper, the switcher, `scripts\install.ps1` and `scripts\uninstall.ps1`. |
| `SHA256SUMS.txt` | SHA-256 of the two files above. |

Every file also gets a build provenance attestation, verifiable with
`gh attestation verify <file> -R SMIDR76/caelestia-windows`.

## Steps

1. Set the version in `helper/Directory.Build.props` and `pack/zpack.json`, and commit.
2. Tag the commit and push the tag: `git tag v1.2.3` then `git push origin v1.2.3`.
3. The **Release** workflow builds on `windows-latest` (NativeAOT helper, switcher, setup, archive, checksums), checks
   that the helper needs no Visual C++ runtime, then on `ubuntu-latest` verifies the checksums, attests the files,
   creates a draft release with generated notes, uploads the files and publishes it.
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
.NET 10 SDK, the Visual Studio C++ build tools (for NativeAOT) and, unless `-SkipInstaller` is given, Inno Setup 6.7 or
later (`winget install -e --id JRSoftware.InnoSetup --scope user`). Inno Setup is free for non-commercial use, which
covers this project.

## Testing an installer

The Claude desktop app and other packaged (MSIX) apps redirect some registry and AppData writes of the programs they
start, so an installer launched from them is not a real test. Run it from Explorer, a normal terminal or a scheduled
task, and check: the entry in Installed apps, the three `Caelestia - *` logon tasks, the Caelestia entries in
`%USERPROFILE%\.glzr\zebar\settings.json`, then an uninstall after `taskkill /F /IM caelestia-helper.exe` (the taskbar
and Windows colours must come back).

## Later

Code signing (SignPath Foundation, or a Certum open source certificate), winget and Scoop manifests and signed
automatic updates are planned; the reasoning is in `docs/research/`.
