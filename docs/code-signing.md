# Code signing

Windows SmartScreen warns about every unsigned installer ("Windows protected your PC", "Unknown publisher"). The
releases are signed through the SignPath Foundation programme for open source projects, which is free. The Release
workflow is ready: signing turns itself on as soon as the variables below exist, and tagged releases are then signed in
two passes (the binaries before they go into the archive and the setup, then the setup itself).

Even signed, a new certificate or a new file can still show a SmartScreen warning for a while: Microsoft builds the
publisher's reputation from clean downloads. The warning fades with use, it cannot be removed instantly.

## Conditions

SignPath Foundation accepts a project at its discretion. Its [code of conduct](https://signpath.org/terms) asks for:

- a public repository under an OSI licence (GPL-3.0 here) with no proprietary component;
- an actively maintained project that has already published releases in the form to be signed, with its features
  documented on the download page, and some existing reputation;
- a "Code signing policy" on the home page with the SignPath attribution, the team roles and a privacy statement
  (see the README);
- multi-factor authentication on GitHub and SignPath for every team member;
- a manual approval of every release, binaries built from source on GitHub-hosted runners, and product name and
  version set in every signed file;
- signing only the project's own files.

## Steps

1. Make the repository public and publish a first release unsigned (tag `v1.0.0`).
2. Turn on two-factor authentication on GitHub.
3. Apply at <https://signpath.org/apply> with the repository link.
4. Once accepted, in SignPath:
   - add the predefined **GitHub.com** trusted build system to the organization and link it to the project, and
     install the SignPath GitHub App on the repository;
   - create two artifact configurations with the slugs `binaries` and `installer`, from
     [signing/binaries.xml](signing/binaries.xml) and [signing/installer.xml](signing/installer.xml);
   - use a signing policy that requires approval (the release signing policy), and note its slug;
   - create an API token for a user allowed to submit signing requests.
5. In the GitHub repository (Settings > Secrets and variables > Actions):
   - secret `SIGNPATH_API_TOKEN`;
   - variables `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG` and `SIGNPATH_SIGNING_POLICY_SLUG`.
6. Tag the next release. The workflow waits (up to two hours) for each signing request to be approved in SignPath,
   then checks that every signed file has a valid Authenticode signature before publishing.

## What is signed

| File | Product name | Version |
| --- | --- | --- |
| `caelestia-helper.exe` | Caelestia for Windows | release version |
| `caelestia-launcher.exe` | Caelestia | release version (`launcher/Cargo.toml` and `launcher/tauri.conf.json` must match) |
| `CaelestiaSwitcher.exe` | Caelestia | release version |
| `CaelestiaSetup-<version>.exe` | Caelestia for Windows | release version (no restriction in its artifact configuration: Inno Setup pads both strings with spaces) |

`scripts/build-release.ps1` runs in three phases for this: `-Phase Binaries` builds the helper, the app and the switcher
into `artifacts/unsigned`, `-Phase Package -SignedBinaries <folder>` builds the archive and the setup with the signed
files, and `-Phase Finish -SignedSetup <folder>` takes the signed setup, writes the update manifest and the checksums.
Without parameters it runs the three phases unsigned, as before.
