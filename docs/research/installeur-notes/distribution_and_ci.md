# Distribution channels and CI release pipelines for an open-source Windows desktop app (state: November 2026)

Context: Caelestia for Windows (GPL-3.0) will ship a per-user setup exe, a portable zip, a .NET 10 NativeAOT helper exe built on windows-latest, and a Zebar widget pack (static files). It depends on Zebar (`glzr-io.zebar` on winget). Research date: 2026-11-16. Workflow files from the reference projects were read directly through the GitHub API on that date.

## 1. GitHub Releases best practices (tag triggers, notes, naming, checksums, draft vs published, immutable releases, release attestation)

### Takeaway
Since 28 Oct 2025, GitHub's immutable releases are generally available. Once enabled, a published release's assets and tag are locked, and GitHub issues a release attestation automatically. The recommended flow is therefore: create a draft, upload every asset (installers, zip, SHA256SUMS, SBOM), then publish. Reference projects create releases with `gh release create --generate-notes --draft` (GlazeWM) or `softprops/action-gh-release` with a hand-built SHA256 checksum file (YASB).

### Cited Findings
- **Immutable releases timeline.** GitHub declared them generally available on 2025-10-28, after a public preview that started 2025-08-26 — [GitHub changelog GA](https://github.blog/changelog/2025-10-28-immutable-releases-are-now-generally-available/); [preview changelog](https://github.blog/changelog/2025-08-26-releases-now-support-immutability-in-public-preview/)
- **What is locked.** The tag is "locked to a specific commit" and cannot be changed or deleted while the release exists. "All files attached to the release (such as binaries and archives) are protected from modification or deletion." — [GitHub Docs: Immutable releases](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/immutable-releases)
- **Recommended draft flow.** The docs recommend "create the release as a draft, attach all assets, then publish", because this "ensures that all assets are in place before the release becomes immutable." — [GitHub Docs: Immutable releases](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/immutable-releases)
- **Automatic release attestation.** Creating an immutable release "automatically generates a release attestation", a verifiable record of the tag, the commit SHA and the assets. Verification is described in a separate guide, "Verifying the integrity of a release" — [GitHub Docs: Immutable releases](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/immutable-releases)
- **What stays editable, and tag reuse.** On an immutable release, the title, the release notes and the pre-release/latest flags can still be edited. If the release is deleted, its tag name cannot be reused, even after the repository is recreated with the same name — [GitHub Docs: Immutable releases](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/immutable-releases)
- **How it is enabled.** Immutability is a repository-level or organization-level setting, not a per-release flag. Attestations are not supported on GitHub Enterprise Server — [GitHub changelog GA](https://github.blog/changelog/2025-10-28-immutable-releases-are-now-generally-available/)
- **Risk for workflows that move tags.** A community discussion reports that releases created before the feature was enabled can also become immutable, which breaks workflows that move tags — [community discussion #178351](https://github.com/orgs/community/discussions/178351) (secondary source, not confirmed in the docs)
- **GlazeWM release job** (`release.yaml`):
  - triggered by `workflow_dispatch` with a `version_number` input, `permissions: contents: write`, `concurrency: release`;
  - it calls a reusable `package.yaml` with `secrets: inherit`;
  - it downloads `package-*` artifacts with `merge-multiple: true` and renames them to `glazewm-vX.exe`, `standalone-glazewm-vX-x64.msi`, `standalone-glazewm-vX-arm64.msi` and `glazewm-vX.dmg`;
  - it then runs `gh release create --generate-notes --draft` with asset labels — [glzr-io/glazewm release.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/release.yaml)
- **Zebar release job** (`release.yaml`):
  - triggered by `workflow_dispatch` (inputs `draft-release` and `npm-publish`), with permissions `contents/issues/pull-requests: write` and `id-token: write`;
  - the next version is computed by a semantic-release reusable workflow pinned to a commit SHA;
  - the release itself is created by `glzr-io/actions/semantic-release@main`, which is pinned to a branch, not a SHA;
  - assets are renamed `zebar-${gitTag}-opt1-x64.msi`, `-opt2-arm64.msi`, `-opt3.dmg`, `-opt4-x64.deb` and `-opt5-x64.AppImage`;
  - there is no attestation or SBOM step, even though `id-token: write` is granted — [glzr-io/zebar release.yaml](https://github.com/glzr-io/zebar/blob/main/.github/workflows/release.yaml)
- **YASB release job** (`windows.yaml`):
  - triggered by `workflow_dispatch`, with top-level `permissions: contents: read` and `contents: write` granted only on the jobs that need it;
  - the release job creates the tag with `actions/github-script`, builds a changelog from the commit range, and generates checksums with `Get-FileHash dist-msis/*.msi -Algorithm SHA256` (output format `HASH filename`);
  - it publishes with `softprops/action-gh-release@v3` (`generate_release_notes: true`, `prerelease: false`), then bumps the version for the next cycle — [amnweb/yasb windows.yaml](https://github.com/amnweb/yasb/blob/main/.github/workflows/windows.yaml)
- **YASB checksums reused by Scoop.** The Scoop Extras manifest for YASB reads hashes from the release's checksum file (`"hash": {"url": "$baseurl/checksums.txt"}`), so the checksum file has a downstream consumer — [ScoopInstaller/Extras bucket/yasb.json](https://github.com/ScoopInstaller/Extras/blob/master/bucket/yasb.json)

### Inferences
- **Release flow for Caelestia.** Use a tag push (`on: push: tags: ['v*.*.*']`) or a `workflow_dispatch` that creates the tag. Then create a draft release, upload all assets, and publish last. With immutable releases enabled, a broken release cannot be patched in place, so ship a new patch version instead.
- **Asset naming.** Use stable, regex-friendly names with the version and architecture, for example:
  - `Caelestia-Setup-v1.2.3-x64.exe`
  - `Caelestia-Portable-v1.2.3-x64.zip`
  - `caelestia-helper-v1.2.3-x64.exe` (only if shipped standalone)
  - `caelestia-zebar-pack-v1.2.3.zip`
  - `SHA256SUMS.txt`
  - `caelestia-v1.2.3.cdx.json` (SBOM)

  Keep the pattern identical across releases. winget-releaser's `installers-regex` and Scoop's `autoupdate` URL templates both depend on it (see section 6).
- **Checksum file format.** Use a single `SHA256SUMS.txt` in GNU `sha256sum` format (`<hash>  <filename>`). It can be consumed by `sha256sum -c`, by Scoop `autoupdate.hash.url`, and by users.

### Gaps
- I did not open the "Verifying the integrity of a release" guide. The exact CLI commands (reportedly `gh release verify` / `gh release verify-asset`) are therefore unconfirmed.
- I did not open the "Preventing changes to your releases" guide, so the exact repository settings path for enabling immutability is unconfirmed.

## 2. Supply-chain hardening (attestations, SBOM, deterministic .NET builds, SHA pinning, least privilege, OpenSSF Scorecard)

### Takeaway
GitHub's current docs use `actions/attest@v4` for both build provenance and SBOM attestations. The job needs `id-token: write`, `attestations: write` and `contents: read`. Verification is `gh attestation verify <file> -R owner/repo`, with `--predicate-type` for SBOMs. For .NET, setting `ContinuousIntegrationBuild=true` (on top of the default `Deterministic`) plus Source Link gives path-normalised, reproducible binaries. Pinning actions by SHA and declaring least-privilege permissions are what OpenSSF Scorecard's Pinned-Dependencies and Token-Permissions checks reward.

### Cited Findings
- **Attestation action and permissions.** The current docs use `actions/attest@v4` (not the older `actions/attest-build-provenance` / `actions/attest-sbom` names). For binaries and SBOMs, the job needs `id-token: write`, `contents: read` and `attestations: write`. `artifact-metadata: write` is needed only to auto-create storage records when `push-to-registry: true` — [GitHub Docs: Use artifact attestations](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)
- **Provenance step for binaries.** `uses: actions/attest@v4` with `subject-path: 'PATH/TO/ARTIFACT'` — [GitHub Docs: Use artifact attestations](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)
- **SBOM attestation.** Use `subject-path` plus `sbom-path`. The SBOM must be JSON, "SPDX or CycloneDX SBOM predicates" are supported, and the docs suggest creating it with anchore's sbom-action — [GitHub Docs: Use artifact attestations](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)
- **Verification commands.**
  - Provenance: `gh attestation verify PATH/TO/ARTIFACT -R ORG/REPO`.
  - SBOM: add `--predicate-type https://spdx.dev/Document/v2.3`, which is required for non-default predicates.
  - Details: `--format json --jq '.[].verificationResult.statement.predicate'`.
  - Offline verification has a separate guide.

  Source: [GitHub Docs: Use artifact attestations](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)
- **Deterministic .NET builds.**
  - Per the Source Link docs, full determinism is on when both `Deterministic` and `ContinuousIntegrationBuild` are true (SDK 2.1.300+). `Deterministic` is already on by default in SDK-style projects — [dotnet/sourcelink docs](https://github.com/dotnet/sourcelink/blob/main/docs/README.md); [Meziantou: reproducible builds](https://www.meziantou.net/creating-reproducible-build-in-dotnet.htm)
  - `ContinuousIntegrationBuild` should be gated on CI env vars (`GITHUB_ACTIONS`), because setting it locally breaks the debugger's source lookup. `EmbedUntrackedSources` is recommended too, and the `DotNet.ReproducibleBuilds` package applies these settings automatically — [Meziantou](https://www.meziantou.net/creating-reproducible-build-in-dotnet.htm); [DotNet.ReproducibleBuilds on NuGet](https://packages.nuget.org/packages/DotNet.ReproducibleBuilds/1.2.4)
  - Enabling `ContinuousIntegrationBuild` also turns on `DeterministicSourcePaths` — [roslyn issue #37379 mirror](https://github.hscsec.cn/dotnet/roslyn/issues/37379) (mirror; original at github.com/dotnet/roslyn/issues/37379)
- **OpenSSF Scorecard checks.**
  - Pinned-Dependencies passes only if dependencies in GitHub workflows, Dockerfiles, scripts and so on are pinned (by hash for actions). Token-Permissions rewards read-only default tokens, and write scopes lower the score. Signed-Releases looks for signature or provenance files among recent GitHub release assets — [Xygeni: Pinned Dependencies](https://detectors.xygeni.io/xydocs/compliance/checkpoints/openssf_scorecard/pinned_dependencies.html); [Xygeni: Token Permissions](https://detectors.xygeni.io/xydocs/compliance/checkpoints/openssf_scorecard/token_permissions.html); [scorecard.dev](https://scorecard.dev/) (third-party summaries; the authoritative definitions are in `ossf/scorecard` `docs/checks.md`, which I did not open)
  - Scores run from 0 to 10, and a check returns -1 when it does not apply — [socket.dev glossary](https://socket.dev/glossary/openssf-scorecard)
- **Real-world SHA pinning.**
  - PowerToys pins third-party actions by SHA with a version comment, for example `azure/login@a641126d… # v3.1.0`, `actions/setup-dotnet@0f4a354d… # v6.1.0`, `microsoft/setup-msstore-cli@3b2ec913… # v1` — [microsoft/PowerToys msstore-submissions.yml](https://github.com/microsoft/PowerToys/blob/main/.github/workflows/msstore-submissions.yml)
  - GlazeWM pins winget-releaser by SHA (`vedantmgoyal9/winget-releaser@19e706d4…`) but uses `actions/checkout@v4` by tag — [glazewm winget-release.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/winget-release.yaml)

### Inferences
- **Attestation for Caelestia.** In the final release job, run `actions/attest@v4` with `subject-path` covering each of the installer, the zip, the helper exe and the pack zip (multi-line paths or a glob; see Gaps). Then add a second attest step with `sbom-path` for the CycloneDX SBOM.
- **Two layers of provenance.** Artifact attestation (SLSA build provenance tied to the workflow) and the immutable-release attestation (tag ↔ assets) complement each other. Users can check either with `gh`.
- **SBOM tooling.**
  - Two options: run anchore/sbom-action (Syft) on the publish directory, or run CycloneDX for .NET (`dotnet CycloneDX`) on the solution. I did not fetch primary docs for either during this pass.
  - The SBOM is optional and has low value for a self-contained NativeAOT binary with few NuGet dependencies. It is still cheap, and Scorecard and corporate users notice it.
- **NativeAOT determinism caveat.** Bit-for-bit reproducibility is plausible but not guaranteed across MSVC linker and runner image versions. Treat reproducibility as best effort and record the SDK and runner versions in the release notes. This is an inference: I found no source specific to NativeAOT reproducibility.

### Gaps
- I did not confirm whether `actions/attest@v4` `subject-path` accepts globs, newline lists or a `subject-checksums` file. The docs page fetched did not cover it, though older `attest-build-provenance` READMEs did.
- No primary docs were fetched for anchore/sbom-action or CycloneDX for .NET (CycloneDX/cyclonedx-dotnet), so versions and flags are unverified.
- The SLSA level reached by GitHub attestations was not stated on the page fetched.

## 3. Signing step placement and secrets handling

### Takeaway
The pattern across projects is the same: build unsigned binaries, sign the inner PE files (exe/dll), then build the installer that embeds them, then sign the installer. Signing happens through a cloud signing service whose credentials live in GitHub secrets: SignPath for YASB, Azure Key Vault + AzureSignTool for GlazeWM, and Azure OIDC + Key Vault for PowerToys' Store submission. The Microsoft Store's Win32 (exe/msi) path makes signing of the installer and every PE file mandatory.

### Cited Findings
- **YASB signs in three stages through SignPath** (`signpath/github-action-submit-signing-request@v2`, `wait-for-completion: 'true'`), one artifact configuration per stage:
  - first the systray hook DLL (`signing_dll`);
  - then the built EXEs (`signing_executable`);
  - then the MSI (`signing_installer`).

  Each signed output replaces the unsigned file before the next packaging step. The job fails if no signed files come back. Secrets used: `SIGN_TOKEN` and `SIGN_POLICY_SLUG` — [amnweb/yasb windows.yaml](https://github.com/amnweb/yasb/blob/main/.github/workflows/windows.yaml)
- **GlazeWM signs on windows-latest with Azure Key Vault.**
  - It installs WiX 5 (`dotnet tool install --global wix --version 5.0.0`, plus the UI/Util/BootstrapperApplications extensions) and AzureSignTool 5.0.0, then runs `package.ps1`.
  - Secrets: `AZ_VAULT_URL`, `AZ_CERT_NAME`, `AZ_CLIENT_ID`, `AZ_CLIENT_SECRET`, `AZ_TENANT_ID`; variable: `RFC3161_TIMESTAMP_URL`.
  - On macOS it signs the binaries before they go into the app bundle ("Make sure to sign the binaries before they're copied into the app bundle"), then signs the bundle, then the DMG.

  Source: [glzr-io/glazewm package.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/package.yaml)
- **PowerToys Store job credentials.** The job runs under a protected `environment: store`, with `permissions: id-token: write`. It logs in with `azure/login` (OIDC: client/tenant/subscription IDs), downloads its auth cert from Key Vault with `az keyvault secret download`, and deletes `cert.pfx` in an `if: always()` cleanup step — [microsoft/PowerToys msstore-submissions.yml](https://github.com/microsoft/PowerToys/blob/main/.github/workflows/msstore-submissions.yml)
- **Store signing rule for Win32 installers (policy 10.2.9).** "The binary and all of its Portable Executable (PE) files must be digitally signed with a code signing certificate that chains up to a certificate issued by a Certificate Authority (CA) that is part of the Microsoft Trusted Root Program." — [Microsoft Store Policies 7.20, §10.2.9](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies)

### Inferences
- **Signing order for Caelestia:**
  1. Build `caelestia-helper.exe` (NativeAOT) and the app exe(s).
  2. Sign them with an RFC 3161 timestamp.
  3. Build the portable zip and the setup exe from the signed files.
  4. Sign the setup exe (and, for Inno Setup, the uninstaller via `SignTool=` / `SignedUninstaller=yes`; this Inno detail is not sourced here).
  5. Compute SHA256SUMS.
  6. Run attestations.
  7. Upload to the draft release.

  Hashes and attestations must come after the last signing step, because signing changes the bytes.
- **Signing jobs and secrets.**
  - Put the signing job in a GitHub Environment with required reviewers, and give only that job the signing secrets.
  - Prefer OIDC federation (Azure Artifact Signing, formerly Trusted Signing, through `azure/login`) over long-lived client secrets. GlazeWM still uses a client secret, while PowerToys uses OIDC.
  - Never sign on `pull_request` events from forks.
- **SignPath Foundation** offers free signing for OSS projects; YASB appears to use SignPath. Eligibility for a GPL-3.0 project is plausible but was not checked in this pass.

### Gaps
- I did not verify the current Azure Artifact Signing (Trusted Signing) availability for individuals or non-US/EU developers in 2026, nor SignPath Foundation's OSS eligibility criteria. Another research thread may cover these.
- I found no source on whether winget, Scoop or Chocolatey require signing. winget validation does not list signing as a requirement (see section 4), but the SmartScreen and Defender reputation of unsigned binaries remains a practical risk.

## 4. winget: winget-pkgs submission, validation, manifest fields, dependencies, automation, moderation

### Takeaway
winget accepts exe/inno/nullsoft/wix/burn/msi/msix/appx/zip/portable/font installers. They must install silently, come from the publisher's own release URL (GitHub Releases qualifies), use HTTPS with no redirectors, and pass Defender plus multi-AV scans and an install/uninstall test as admin and non-admin. Signing is not a stated requirement. After automated validation, a moderator reviews manually. `PackageDependencies` on another winget package works in practice: GlazeWM depends on `Microsoft.VCRedist.2015+.x64`. Automation is mature: wingetcreate (PowerToys, YASB) or Komac through vedantmgoyal9/winget-releaser (GlazeWM, Flow Launcher). Both require the first version to be submitted manually.

### Cited Findings
- **Installer types.** The minimal schema comment lists "exe, msi, msix, inno, wix, nullsoft, appx, font". The current manifest version is 1.12.0. For Nullsoft or Inno, "the client will automatically set the silent and silent with progress install behaviors" — [Microsoft Learn: Create your package manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest) (updated 2026-09-15)
- **Zip/portable support.** Zip packages are supported as of Windows Package Manager 1.5. A nested installer type field names the installer inside the archive — [winget-types docs.rs](https://docs.rs/winget-types/0.4.3/src/winget_types/installer/mod.rs.html); [winget-cli 1.4.3132-preview notes](https://newreleases.io/project/github/microsoft/winget-cli/release/v1.4.3132-preview) (older note said the community repo did not then accept zip apps; superseded)
- **Silent install is mandatory.** "All tools must support a silent install. If you have an executable that does not support a silent install, then we cannot provide that tool at this time." — [Microsoft Learn: manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- **Silent switches by installer.** MSI `/q`, Inno `/SILENT` or `/VERYSILENT`, Nullsoft `/S`, InstallShield `/s` — [Microsoft Learn: manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- **Product codes.** "Package installers in MSI format use product codes… The product code for a given version of a package should be included in the manifest to help ensure the best upgrade experience." `PackageName` and `Publisher` should match the Add/Remove Programs entry for export and upgrade correlation — [Microsoft Learn: manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- **Manifest layout.** A multi-file manifest needs at least three files: version, defaultLocale and installer. The singleton format applies only to one installer with one locale. Only one PR per package version, and one manifest per submission — [Microsoft Learn: manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest); [Microsoft Learn: Submit to repository](https://learn.microsoft.com/en-us/windows/package-manager/package/repository)
- **Submission expectations:**
  - the installer and app are virus free;
  - they install and uninstall correctly for both administrators and non-administrators;
  - the installer supports non-interactive modes;
  - the installer comes directly from the publisher's website.

  Microsoft "reserves the right to refuse a submission for any reason". After automated tests, "your submission will be manually reviewed by a moderator" — [Microsoft Learn: Submit your manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/repository) (updated 2026-07-14)
- **Validation failure labels:**
  - `Validation-Indirect-URL` (redirectors are not allowed);
  - `Validation-HTTP-Error` (HTTPS is required);
  - `Validation-Unattended-Failed` (the install timed out, typically because it is not silent);
  - `Validation-Uninstall-Error` (the uninstall did not clean up completely);
  - `Validation-Defender-Error`;
  - `Binary-Validation-Error` (the multi-AV "Installers Scan"; flagged PUA is rejected if the AV vendor cannot clear it);
  - `URL-Validation-Error` (also covers URL reputation failures);
  - `Validation-VCRuntime-Dependency` (fix by adding the dependency to the manifest);
  - `Needs-Author-Feedback` (the PR is closed after 10 days without a response).

  Source: [Microsoft Learn: Submit your manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/repository)
- **Policies.** "The InstallerUrl must be the ISV's release location for the Product. Products from download websites will not be allowed" (1.1.4). "The Product may depend on non-integrated software (such as another Product…)" (1.2.3). There must be no download-and-execute of remote scripts beyond the described functionality (1.2.1). A `PrivacyUrl` is expected if personal information is collected (1.5.1). There is no clause on code signing or SmartScreen — [Microsoft Learn: winget repository policies](https://learn.microsoft.com/en-us/windows/package-manager/package/windows-package-manager-policies) (policy v1.0, page updated 2026-08-30)
- **A live example of the fields Caelestia needs** (`glzr-io.glazewm` 3.10.1 installer manifest). It was "Created with WinGet Releaser using komac v2.15.0" and uses schema 1.12.0. Its contents:
  - `InstallerType: burn` and `Scope: machine`;
  - `InstallModes: [interactive, silent, silentWithProgress]` and `UpgradeBehavior: install`;
  - `Dependencies.PackageDependencies: - PackageIdentifier: Microsoft.VCRedist.2015+.x64`;
  - a top-level `ProductCode`, plus `AppsAndFeaturesEntries` with `ProductCode`, `UpgradeCode` and `InstallerType: burn`;
  - an `InstallerUrl` pointing at `github.com/glzr-io/glazewm/releases/download/v3.10.1/glazewm-v3.10.1.exe`.

  Source: [winget-pkgs manifests/g/glzr-io/glazewm](https://github.com/microsoft/winget-pkgs/tree/master/manifests/g/glzr-io/glazewm)
- **Zebar's winget presence.** `glzr-io.zebar` exists in winget-pkgs; the latest folders seen were 3.2.0, 3.3.0 and 3.3.1 — [winget-pkgs manifests/g/glzr-io/zebar](https://github.com/microsoft/winget-pkgs/tree/master/manifests/g/glzr-io/zebar)
- **Scope and dependencies risk.** A winget-pkgs issue reports that a scope requirement can make validation fail for packages with dependencies, when the dependency has no installer of the matching scope. It proposes treating scope as a preference during validation — [winget-pkgs issue #152555](https://github.com/microsoft/winget-pkgs/issues/152555) (current status not confirmed)
- **winget-releaser (vedantmgoyal9):**
  - It uses Komac to create manifests and open PRs. Inputs: `identifier` (required), `version` (defaults to the tag without `v`), `installers-regex` (default `.(exe|msi|msix|appx)(bundle){0,1}$`), `token`, `fork-user`, `max-versions-to-keep`, `release-tag`, `release-notes-url`.
  - Requirements: at least one version must already exist in winget-pkgs; there must be a fork of winget-pkgs; the token must be a classic PAT with `public_repo` (plus `workflow` recommended), because fine-grained PATs are not supported; the release must be published, not a draft.
  - It now runs on Windows, Linux and macOS. There is no deprecation notice, and the examples use `@main`.

  Source: [vedantmgoyal9/winget-releaser](https://github.com/vedantmgoyal9/winget-releaser)
- **How each project publishes to winget:**
  - **GlazeWM** runs on `release: published`, on ubuntu-latest, using `vedantmgoyal9/winget-releaser@<SHA>` with `identifier: glzr-io.glazewm`, `installers-regex: 'glazewm-v[0-9.]+\.exe$'` and `token: secrets.WINGET_TOKEN` — [glazewm winget-release.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/winget-release.yaml)
  - **Flow Launcher** runs on manual dispatch, using `vedantmgoyal2009/winget-releaser@v2` (the old owner name) with `identifier: Flow-Launcher.Flow-Launcher` — [Flow.Launcher winget.yml](https://github.com/Flow-Launcher/Flow.Launcher/blob/dev/.github/workflows/winget.yml)
  - **PowerToys** runs on `release: published`, skips pre-releases, and runs on windows-latest ("winget-create is only supported on Windows"). It downloads `https://aka.ms/wingetcreate/latest` and runs `wingetcreate update Microsoft.PowerToys --version … --urls "<x64 user url>|user" "<x64 machine url>|machine" "<arm64…>|user" … --submit`, with the token in `WINGET_CREATE_GITHUB_TOKEN` — [PowerToys package-submissions.yml](https://github.com/microsoft/PowerToys/blob/main/.github/workflows/package-submissions.yml)
  - **YASB** runs on manual dispatch, on windows-latest, using `wingetcreate update AmN.yasb --urls "$x64Url|x64" "$aarch64Url|arm64" --prtitle … --release-notes-url … --release-date … --submit --token` — [yasb winget.yaml](https://github.com/amnweb/yasb/blob/main/.github/workflows/winget.yaml)
- **Local pre-checks.** Run `winget validate <path>`, and test in Windows Sandbox with `Tools\SandboxTest.ps1` from the winget-pkgs repo — [Microsoft Learn: Submit your manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/repository)
- **AI-assisted triage in winget-pkgs (2026).** The repo now runs agentic workflows such as `missing-dependency-assist`, `manifest-validation-diagnosis`, `elevation-requirement-review`, `domain-validation-assist` and `wingetbot-pr-triage` (`.md` + `.lock.yml` pairs) — [microsoft/winget-pkgs .github/workflows](https://github.com/microsoft/winget-pkgs/tree/master/.github/workflows) (file listing only; I did not read their behaviour)

### Inferences
- **Proposed setup-exe manifest for Caelestia** (assuming Inno Setup with `PrivilegesRequired=lowest`):
  - `InstallerType: inno`, `Scope: user`, `UpgradeBehavior: install`;
  - `Dependencies: PackageDependencies: [{PackageIdentifier: glzr-io.zebar}]`, optionally with a `MinimumVersion`;
  - `ProductCode` set to the Inno AppId with the `_is1` suffix, plus `AppsAndFeaturesEntries` (DisplayName/Publisher matching ARP).

  Inno being silent by default means `InstallerSwitches` are needed only for custom flags. Inno ARP naming is not sourced here.
- **Dependency on Zebar.** Zebar ships as an MSI that is likely machine scope (it is in `PFiles`). Per issue #152555, a `Scope: user` package that depends on it might hit validation friction. Test in Sandbox, and be ready to comment on the PR.
- **Portable zip on winget.** The zip could be added as a second installer (`InstallerType: zip`, `NestedInstallerType: portable`, `NestedInstallerFiles` with `PortableCommandAlias`). Most GUI apps with a real installer list only the installer. GlazeWM and YASB do this.
- **Automation path.**
  1. Submit v1 by hand (`wingetcreate new` or `komac new`).
  2. Add a `release: published` workflow with winget-releaser pinned by SHA, an `installers-regex` that matches only the setup exe, and a classic PAT stored as `WINGET_TOKEN` (minimum `public_repo`), under a bot or maintainer account that owns the winget-pkgs fork.
  3. Skip pre-releases (PowerToys pattern).
- **Moderation time.** No official SLA was found. The process includes automated validation followed by manual moderator review, so expect hours to several days. Defender or multi-AV false positives on an unsigned NativeAOT exe could block the PR (`Binary-Validation-Error`). Signing and Defender submission reduce that risk.

### Gaps
- I found no official moderation-time SLA or statistics for winget-pkgs.
- I did not fetch the 1.12 installer schema doc for exact `PackageDependencies` / `MinimumVersion` syntax, or for `NestedInstallerFiles` / `PortableCommandAlias` details.
- I found no explicit winget policy on unsigned installers or SmartScreen. The only related checks are URL reputation (`URL-Validation-Error`) and the Defender and AV scans.
- Komac's standalone usage (`komac update … --submit`) was not documented from a primary source in this pass.

## 5. Scoop, Chocolatey, Microsoft Store

### Takeaway
- **Scoop:** Extras is the bucket for GUI apps. Manifests are JSON with `checkver: github` plus `autoupdate` URL templates, and the Extras maintainers' bot updates them. GlazeWM, Zebar and YASB are all in Extras. Main is reserved for well-known non-GUI developer tools with 500+ stars.
- **Chocolatey:** Every version is moderated by an automated validator, a verifier (install and uninstall test) and a virus scan, plus human review unless the package is trusted. Embedded binaries require distribution rights plus `LICENSE.txt` and `VERIFICATION.txt`.
- **Microsoft Store:** Individual developer registration is now free (announced Sep 2025). Win32 exe/msi apps can be listed through a versioned HTTPS URL, but only if the installer and every PE file are signed with a trusted-root CA certificate, the install is silent, and the installer is not a web stub. Changing user Windows settings requires supported APIs and user consent (10.2.8).

### Cited Findings
- **Scoop main bucket criteria:** "a reasonably well-known and widely used developer tool"; GitHub projects "should have at least 500 stars and 150 forks"; "a fairly standard install (e.g. uses a version-specific download URL, no elaborate pre/post install scripts)"; "a non-GUI tool" — [Scoop wiki: Criteria for including apps in the main bucket](https://github.com/ScoopInstaller/Scoop/wiki/Criteria-for-including-apps-in-the-main-bucket)
- **Scoop Extras manifests of comparable apps:**
  - **Zebar** (`extras/zebar`): it extracts the MSI (`extract_dir: "PFiles\\glzr.io\\Zebar"`, `bin: zebar.exe`, a shortcut), with `"checkver": "github"` and `autoupdate.architecture.64bit.url` set to `…/download/v$version/zebar-v$version-opt1-x64.msi`; license `GPL-3.0`. Version 3.3.1 was in the bucket at research time — [ScoopInstaller/Extras bucket/zebar.json](https://github.com/ScoopInstaller/Extras/blob/master/bucket/zebar.json)
  - **GlazeWM**: same pattern, using `standalone-glazewm-v$version-x64.msi`, `extract_dir: PFiles64\\glzr.io\\GlazeWM` and `bin: cli\\GlazeWM.exe` — [Extras glazewm.json](https://github.com/ScoopInstaller/Extras/blob/master/bucket/glazewm.json)
  - **YASB**: same pattern, with `autoupdate.hash.url: "$baseurl/checksums.txt"` — [Extras yasb.json](https://github.com/ScoopInstaller/Extras/blob/master/bucket/yasb.json)
- **Chocolatey moderation:**
  - It runs three automated checks: validator (quality against requirements and guidelines), verifier ("checks the correctness (that the package actually works)", install and uninstall), and cleaner (a reminder after 20 days waiting, rejection 15 days later).
  - A "Package Scanner" produces virus scan results.
  - Human moderators review; "A moderator cannot hold up a package based on guidelines/suggestions alone".
  - Automated review starts about 30 minutes after submission.
  - Trusted packages are approved on automated review alone.
  - Included binaries require distribution rights and checksum verification. Rules CPMR0005 and CPMR0006 flag a missing `LICENSE.txt` or `VERIFICATION.txt`.

  Source: [Chocolatey docs: Moderation](https://docs.chocolatey.org/en-us/community-repository/moderation/)
- **Flow Launcher's Chocolatey publishing.** It publishes by sending a `repository_dispatch` (`event_type: publish`) to a separate `Flow-Launcher/chocolatey-package` repo on `release: published` — [Flow.Launcher release_deploy.yml](https://github.com/Flow-Launcher/Flow.Launcher/blob/dev/.github/workflows/release_deploy.yml)
- **Free Store registration for individuals.**
  - Free individual developer registration on the Microsoft Store was announced on 2025-09-10. The previous ~$19 individual fee is waived in the new flow.
  - Sign-up is at storedeveloper.microsoft.com, with identity verification by government ID and selfie.

  Source: [Windows Developer Blog, 2025-09-10](https://blogs.windows.com/windowsdeveloper/2025/09/10/free-developer-registration-for-individual-developers-on-microsoft-store/); [Microsoft Learn: open developer account](https://learn.microsoft.com/windows/apps/publish/faq/open-developer-account) (details from search snippets, not a full read)
- **Store policy 10.2.9 (Win32 installer by URL).**
  - "Non-gaming products may submit an HTTPS-enabled download URL… The installer binary may only be an .msi or .exe."
  - The binary and all of its PE files must be signed by a Microsoft Trusted Root Program CA.
  - The URL must be versioned, and the binary "must not change after submission".
  - "silent install is required, however a User Account Control (UAC) dialog is allowed".
  - The installer must not be "a downloader stub/web installer".

  Source: [Microsoft Store Policies v7.20](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies) (page dated 2026-09-14)
- **Store policies on settings, uninstall and dependencies.**
  - 10.2.8: "You are required to use supported methods and must obtain user consent to change any user's Windows settings, preferences, settings UI, or modify the user's Windows experience in any way. Unsupported methods include… accessibility APIs or undocumented or unsupported APIs in unsupported ways."
  - 10.2.7 requires a clean uninstall.
  - 10.2.4 allows depending on other software if "you disclose the dependency at the beginning of the description".
  - Win32 products must always have a privacy policy.
  - Products submitted under 10.2.9 must not have "an unreasonably low installation success rate".

  Source: [Microsoft Store Policies v7.20](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies)
- **Store automation example (PowerToys).**
  - The workflow runs on `release: published` and skips pre-releases.
  - It uses `microsoft/setup-msstore-cli` and `msstore reconfigure` (cert-based Azure AD app credentials plus `SELLER_ID`).
  - It runs `msstore submission update <PRODUCT_ID>` with a packages JSON (`packageUrl` pointing at the GitHub release asset, `installerParameters: "/quiet /norestart"`, `isSilentInstall: true`, per-architecture entries), then `msstore submission publish`.

  Source: [PowerToys msstore-submissions.yml](https://github.com/microsoft/PowerToys/blob/main/.github/workflows/msstore-submissions.yml)
- **MSIX vs exe/msi on the Store.** Microsoft's docs recommend MSIX for the Store (free Store signing, built-in updates). The exe/msi path gets no Store-managed updates; the publisher hosts the binary — [Microsoft Learn: choose distribution path](https://learn.microsoft.com/en-us/WINDOWS/APPS/package-and-deploy/choose-distribution-path); [Microsoft Learn: MSI/EXE app submission](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msi/create-app-submission) (from search snippets)

### Inferences
- **Scoop for Caelestia.**
  - The natural fit is the portable zip, matching Scoop's portable philosophy: `url` pointing at `Caelestia-Portable-v$version-x64.zip`, `hash.url: $baseurl/SHA256SUMS.txt`, `checkver: github`, `persist` for the user config folder if it lives under the app dir, and `depends: extras/zebar`. Note that Scoop's `depends` within the same bucket is a Scoop feature I did not verify here.
  - Extras acceptance for a young project is not guaranteed, since there are no published Extras criteria. Run a project-owned bucket (`caelestia-windows/scoop-bucket`) first.
- **Chocolatey.** Manual moderation and the extra files (VERIFICATION.txt and the like) make it the lowest-priority channel. Flow Launcher keeps it in a separate repo triggered by dispatch.
- **Microsoft Store.**
  - An exe listing is feasible only with a real code-signing certificate that covers the setup exe, the helper and every PE file.
  - Caelestia modifies the shell experience (docks, panels, Zebar autostart, possibly taskbar hiding). Under 10.2.8 that must use supported APIs with explicit user consent, so any undocumented Explorer or taskbar hacks would risk certification failure.
  - The Zebar dependency must be disclosed at the start of the description (10.2.4). The installer must not download Zebar at install time if that would make it a "downloader stub" — an inference; the policy targets the main installer.
  - The Store is a later-phase channel.

### Gaps
- I found no published acceptance criteria for the Scoop Extras bucket (the CONTRIBUTING path returned 404), and no Extras review-time data.
- Chocolatey's automatic packaging (AU / chocolatey-au) process was not documented in this pass, and neither was the exact current VirusTotal threshold.
- No Store policy text explicitly addresses apps that register to run at startup. Only 10.2.8 (settings and experience changes) and the archived/MSIX startup-task guidance apply indirectly. The 2023 "principled approach to app pinning and app defaults" blog linked from 10.2.8 was not read.
- Store certification turnaround time for Win32 apps was not found.

## 6. Concrete release-workflow structures of GlazeWM, Zebar, YASB, Flow Launcher, PowerToys

### Takeaway
None of the reference projects uses tag-push triggers for releases. All use `workflow_dispatch` and then fan out on `release: published` to package managers (winget, Store, Chocolatey dispatch). Signing is cloud-based: SignPath for YASB, Azure Key Vault + AzureSignTool for GlazeWM. Checksums are hand-rolled in PowerShell, as YASB does. None of the five public workflow sets fetched includes `actions/attest`, an SBOM or Scorecard. Adding them would put Caelestia ahead of its peers.

### Cited Findings
- **GlazeWM** has `build.yaml`, `package.yaml`, `release.yaml`, `winget-release.yaml`, `lint-check.yaml` and `pr-title-check.yaml`:
  - `release.yaml` (dispatch with version) calls `package.yaml`, which calls `build.yaml`.
  - The Windows packaging job uses WiX 5 with a Burn bundle and MSIs, signed with AzureSignTool against Azure Key Vault.
  - The macOS job runs codesign and notarization.
  - The release is created with `gh release create --draft --generate-notes`.
  - A separate winget workflow runs on `release: published`.

  Sources: [workflows dir](https://github.com/glzr-io/glazewm/tree/main/.github/workflows); [package.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/package.yaml); [winget-release.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/winget-release.yaml)
- **Zebar** has `build.yaml`, `release.yaml`, `publish-crate.yaml`, `lint-check.yaml` and `pr-title-check.yaml`. It uses semantic-release (driven by conventional-commit PR titles) to compute versions and create the release with Tauri bundles. The `release.yaml` read here has no winget step, so its winget updates presumably come from elsewhere — [zebar workflows](https://github.com/glzr-io/zebar/tree/main/.github/workflows); [release.yaml](https://github.com/glzr-io/zebar/blob/main/.github/workflows/release.yaml)
- **YASB** has `windows.yaml` (stable), `windows-preview.yaml`, `winget.yaml`, `build-trayhook.yml`, `tests.yaml`, `pr-check.yaml`, `update-schema.yaml`, `publish-wiki.yaml` and others:
  - It builds on a matrix of x64 (windows-latest) and arm64 runners, with Python 3.14.
  - Signing goes through SignPath in three stages (dll, exe, msi).
  - The release job creates the tag, builds the changelog (including an LLM "release agent" summarization step), writes SHA256 checksums, publishes with softprops/action-gh-release@v3, regenerates `schema.json`, and bumps the version.

  Source: [yasb workflows](https://github.com/amnweb/yasb/tree/main/.github/workflows)
- **Flow Launcher** has `dotnet.yml`, `release_deploy.yml`, `release_pr.yml`, `winget.yml`, `default_plugins.yml` and others:
  - `dotnet.yml` builds on push on windows-latest. It sets the version with `vers-one/dotnet-project-version-updater`, uses .NET 9, installs `vpk` (Velopack) for the installer and updates, runs `dotnet build`/`dotnet test`, runs `post_build.ps1`, and uploads artifacts.
  - `release_deploy.yml` runs on `release: published` and triggers docs, website and Chocolatey repos through `repository_dispatch`.

  Source: [Flow.Launcher workflows](https://github.com/Flow-Launcher/Flow.Launcher/tree/dev/.github/workflows)
- **PowerToys** keeps public GitHub workflows only for the post-release fan-out:
  - `package-submissions.yml` submits to winget with wingetcreate (user and machine installers);
  - `msstore-submissions.yml` submits to the Store with msstore-cli and OIDC;
  - `dependency-review.yml` and spelling checks.

  The build itself is not in these GitHub workflows; it presumably runs on Azure Pipelines, which I did not verify — [PowerToys workflows](https://github.com/microsoft/PowerToys/tree/main/.github/workflows)

### Inferences
- **What to copy.**
  - From GlazeWM: the separation of build, package/sign and release workflows (reusable `workflow_call`).
  - From GlazeWM and PowerToys: winget and Store jobs that run on `release: published`.
  - From PowerToys: skipping pre-releases.
  - From YASB: per-stage signing that fails when no signed file comes back.
  - From Flow Launcher: dispatching to separate repos (a Scoop bucket and a Chocolatey package).

### Gaps
- I did not read Zebar's `build.yaml` in full, so it is unknown whether Zebar signs its MSIs and how its winget updates happen.
- PowerToys' actual build pipeline (Azure DevOps, ESRP signing) is not public in `.github/workflows`, so it was not analysed.
- Flow Launcher's signing status was not determined.

## 7. Reference GitHub Actions release pipeline design for Caelestia (synthesis)

### Takeaway
Recommended shape:
1. A `release.yml` triggered by a `v*` tag push (or a dispatch that creates the tag).
2. Least-privilege jobs: build (windows-latest, NativeAOT with MSVC already present), sign (protected environment), then package (installer from signed binaries, then sign the installer).
3. Checksums, SBOM and attestations.
4. A draft release that is published last (immutable).
5. Separate `release: published` workflows for winget (winget-releaser), Scoop bucket and Store.

### Cited Findings
- **Draft, then publish, under immutable releases** — [GitHub Docs: Immutable releases](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/immutable-releases)
- **Attestation permissions and action** (`id-token: write`, `attestations: write`, `contents: read`; `actions/attest@v4`) — [GitHub Docs: artifact attestations](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)
- **winget-releaser needs a published (non-draft) release**, so it fits a `release: published` trigger — [winget-releaser](https://github.com/vedantmgoyal9/winget-releaser); [glazewm winget-release.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/winget-release.yaml)
- **Store submission from a published release with msstore-cli** — [PowerToys msstore-submissions.yml](https://github.com/microsoft/PowerToys/blob/main/.github/workflows/msstore-submissions.yml)
- **Inner-then-outer signing in CI** — [yasb windows.yaml](https://github.com/amnweb/yasb/blob/main/.github/workflows/windows.yaml); [glazewm package.yaml](https://github.com/glzr-io/glazewm/blob/main/.github/workflows/package.yaml)

### Inferences
Proposed `release.yml` (design; actions should be pinned to full SHAs with a `# vX` comment, shown here by tag for readability):

```yaml
name: release
on:
  push:
    tags: ['v[0-9]+.[0-9]+.[0-9]+*']
permissions: {}
concurrency: { group: release, cancel-in-progress: false }

jobs:
  build:
    runs-on: windows-latest
    permissions: { contents: read }
    steps:
      - uses: actions/checkout@<sha>            # persist-credentials: false
      - uses: actions/setup-dotnet@<sha>        # dotnet-version: 10.0.x
      - run: dotnet test -c Release
      - run: dotnet publish src/Helper -c Release -r win-x64 -p:PublishAot=true -p:ContinuousIntegrationBuild=true -p:Version=${{ github.ref_name }}
      - run: ./build/pack-widgets.ps1          # Zebar pack -> caelestia-zebar-pack-vX.zip
      - uses: actions/upload-artifact@<sha>     # name: unsigned

  sign-binaries:
    needs: build
    runs-on: windows-latest
    environment: signing                        # required reviewers, holds signing secrets / OIDC
    permissions: { contents: read, id-token: write }
    steps:
      - download unsigned
      - sign helper + app exe/dll (Azure Artifact Signing via OIDC, or SignPath), RFC3161 timestamp
      - build portable zip from signed files
      - build setup exe (Inno Setup / ISCC) from signed files
      - sign setup exe (+ uninstaller)
      - upload artifact: dist

  publish:
    needs: sign-binaries
    runs-on: ubuntu-latest
    permissions: { contents: write, id-token: write, attestations: write }
    steps:
      - download dist
      - run: cd dist && sha256sum * > SHA256SUMS.txt
      - SBOM: anchore/sbom-action or `dotnet CycloneDX` -> caelestia-vX.cdx.json
      - uses: actions/attest@v4   # subject-path: dist/*  (build provenance)
      - uses: actions/attest@v4   # subject-path: dist/Caelestia-Setup-*.exe, sbom-path: caelestia-vX.cdx.json
      - run: gh release create "$TAG" --draft --generate-notes --verify-tag dist/*
      - run: gh release edit "$TAG" --draft=false      # publish last -> immutable + release attestation
```

Separate workflows triggered on `release: published` (skip if `github.event.release.prerelease`):
- `winget.yml`:
  - runs `vedantmgoyal9/winget-releaser@<sha>` with `identifier: <Publisher>.Caelestia` and `installers-regex: 'Caelestia-Setup-v[0-9.]+-x64\.exe$'`;
  - uses `token: secrets.WINGET_TOKEN` (a classic PAT, `public_repo`).
  - The first version is submitted manually with `wingetcreate new` or `komac new`, declaring `Scope: user`, `InstallerType: inno` and `PackageDependencies: glzr-io.zebar`.
- `scoop.yml`: dispatches to (or commits in) a project-owned bucket; Scoop's own `checkver`/`autoupdate` can also do this without CI.
- Store (later): use msstore-cli as PowerToys does, once a trusted code-signing certificate exists.

Hardening checklist:
- Use `permissions: {}` at the top level and per-job grants.
- Pin every action by SHA, and let Dependabot (`package-ecosystem: github-actions`) update the SHAs.
- Set `persist-credentials: false` on checkout.
- Do not run release workflows on `pull_request_target`.
- Turn on immutable releases in repository settings.
- Add the `ossf/scorecard-action` workflow plus a README badge.
- Set `ContinuousIntegrationBuild`, enable Source Link, and add `EmbedUntrackedSources` in `Directory.Build.props`, gated on `GITHUB_ACTIONS`.
- Document verification in the README: `gh attestation verify Caelestia-Setup-vX-x64.exe -R <owner>/caelestia-windows` and `sha256sum -c SHA256SUMS.txt`.

### Gaps
- **Runner details.** I did not verify that windows-latest in Nov 2026 (Windows Server 2025 image) ships the MSVC build tools and Windows SDK needed for NativeAOT. Another thread on build tooling should confirm this.
- **Signing-time interaction with immutable releases.** If signing happens out of band (a manual SignPath approval), assets must be finalised before publish. With an immutable release, a re-signed asset cannot replace an existing one, and there is no confirmation beyond the docs' "assets protected from modification".
- **`gh release create --verify-tag` and `gh release edit --draft=false`** are standard GitHub CLI flags. I did not re-verify them in this pass.
