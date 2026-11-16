# Code signing, SmartScreen reputation and antivirus false positives for a solo open-source Windows project (state as of November 2026)

Context: "Caelestia for Windows", GPL-3.0, individual developer in France (EU). Ships a setup exe plus a NativeAOT helper exe (low-level keyboard hook, logon scheduled tasks, hides the taskbar, edits HKCU). Research date: 2026-11-16.

## Microsoft Artifact Signing (formerly Azure Trusted Signing): name, eligibility for an EU individual, price, certificates, CI

### Takeaway
The service is now called **Artifact Signing** (formerly Trusted Signing). It is generally available and costs $9.99/month (Basic). **Individual developers must be located in the US or Canada**, so a solo developer in France cannot use it as an individual in 2026. Organizations in the EU can, but they need a legal business entity, a domain email and business documents. Certificates last 72 hours and are renewed daily, and Microsoft timestamping keeps signatures valid after that. Signing gives no instant SmartScreen pass: reputation still builds over time.

### Cited Findings
- **Name.** Microsoft docs now say "Artifact Signing (formerly Trusted Signing)". The docs moved from /azure/trusted-signing to /azure/artifact-signing (FAQ dated 2026-05-14, updated 2026-10-27) — [Artifact Signing FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq); [SmartScreen reputation for Windows app developers](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
- **GA status.** The rebrand and general availability were announced around January 2026 — [devclass, 2026-01-14](https://www.devclass.com/security/2026/01/14/code-signing-windows-apps-may-be-easier-and-more-secure-with-new-azure-artifact-service/4079554); [ElevenForum thread "Artifact Signing (GA)"](https://www.elevenforum.com/t/microsoft-simplifying-code-signing-for-windows-apps-artifact-signing-ga.43742/latest). These are secondary sources, not the Microsoft announcement itself.
- **Geographic eligibility (official, quickstart dated 2026-05-21, updated 2026-10-08).** Exact wording: "Public Trust certificates are available to organizations in the United States, Canada, the European Union, the United Kingdom, Australia, New Zealand, Japan, South Korea, Singapore, Switzerland, Norway, and Israel. **Individual developers must be located in the United States or Canada.** These geographic restrictions do not apply to Private Trust certificates." — [Quickstart: Set up Artifact Signing](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)
- **French individuals in practice.** A March 2026 Microsoft Q&A post reports that France is missing from the Country/Region list in the individual validation form. The only answer is AI-generated, not from Microsoft staff. It also claims individual onboarding is "paused" and new subscriptions are limited to US/Canadian organizations with 3 years of history. That claim is unverified and contradicts the official quickstart. — [Q&A: can't create a new trusted signing individual identity](https://learn.microsoft.com/en-us/answers/questions/5810735/cant-create-a-new-trusted-signing-individual-ident). A related March 2026 question asks whether an Irish unincorporated sole trader qualifies — [Q&A: Irish sole trader](https://learn.microsoft.com/en-us/answers/questions/5915697/can-an-irish-sole-trader-(non-incorporated)-qualif)
- **Individual identity validation process.**
  - Identity details are pulled from the Azure billing account, which must have Account Type "Individual". The legal name and sold-to address must match a government-issued ID.
  - Verification goes through Microsoft Entra Verified ID with the AU10TIX partner: ID scan, Face Check, and the Microsoft Authenticator app.
  - Your city, state/province and country appear on the certificate. Your email and street address do not.
  - Source: [Quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)
- **Organization identity validation.**
  - Requires a legal business entity name, a website, a primary email on a domain owned by the entity, and a business identifier.
  - Takes "1 to 20 business days (possibly longer…)".
  - Documents must have been issued within the last 12 months. You get three upload attempts.
  - Source: [Quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)
- **Subscription type.** Free, trial and sponsored Azure subscriptions are not supported. You need pay-as-you-go or an enterprise agreement. — [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- **No custom names, no EV.** No custom CN or O: the CN must be the validated legal name. No EV certificates, "no plan to issue EV certificates in the future". — [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- **Price.**
  - Basic: $9.99/month, 5,000 signatures/month, then $0.005 per extra signature, one certificate profile of each type.
  - Premium: $99.99/month, 100,000 signatures/month, 10 profiles of each type.
  - Billing starts when the account is created and is not pro-rated.
  - Sources: [SmartScreen reputation doc ("Starts at $9.99/month")](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation); [Artifact Signing pricing page](https://azure.microsoft.com/en-us/pricing/details/artifact-signing/) (quotas and non-pro-rata confirmed; the dollar figures did not render when fetched and came from search snippets of the same page)
- **Certificate lifetime.** Certificates are renewed daily and valid for **72 hours**. The TSA endpoint is `http://timestamp.acs.microsoft.com`. Countersigned signatures stay valid after the certificate expires unless it is revoked. — [Artifact Signing certificate management (search snippet of learn.microsoft.com concept-certificate-management)](https://learn.microsoft.com/en-us/azure/artifact-signing/concept-certificate-management)
- **Identity validation expiry.** If identity validation is not renewed, certificate renewal stops. Reminders start 60 days before expiry. — [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- **Security level.** The service is FIPS 140-3 Level 3. The private key is never given to the user. — [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- **SmartScreen behaviour.** "SmartScreen reputation builds up automatically. The prompt stops appearing once the file hash has sufficient download history." If prompts persist, submit the signed file to Microsoft Security Intelligence. — [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- **Signing tools.** You can sign with SignTool plus the Artifact Signing dlib, PowerShell, GitHub Actions, the SDK, or the Azure DevOps task `AzureArtifactSigning@`. Troubleshooting tip: `signtool verify /v /debug /pa file`. — [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- **GitHub Action `azure/artifact-signing-action@v2`.**
  - Inputs: `endpoint` (e.g. `https://weu.codesigning.azure.net/` for West Europe), `signing-account-name`, `certificate-profile-name`, `files-folder` / `files` / `files-catalog`, `files-folder-filter: exe,dll`, `file-digest: SHA256`, `timestamp-rfc3161: http://timestamp.acs.microsoft.com`, `timestamp-digest: SHA256`.
  - Authentication: OIDC federated credentials with `azure/login@v3` and `id-token: write` (recommended), or an app-registration client secret.
  - Required role: "Artifact Signing Certificate Profile Signer".
  - Runners: Windows only (windows-2022/2025). Arm runners are not supported.
  - Source: [Azure/artifact-signing-action](https://github.com/Azure/artifact-signing-action)
- **Azure regions.** Available regions include North Europe, West Europe, Poland Central and Switzerland North. — [Quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)

### Inferences
- A French solo developer **cannot** get a Public Trust certificate from Artifact Signing as an individual. The only Artifact Signing route is to sign as an EU **organization**. A French micro-entrepreneur (EI) is legally the individual, not a separate legal entity, so it is unclear whether Microsoft's validators would accept one. Creating a company (e.g. SASU) or an association loi 1901 with a SIREN, website and domain email is the likely way in.
- Since the end-entity certificate rotates every 72 hours, SmartScreen reputation probably attaches to the stable validated identity (CN/O) and the Microsoft intermediate CA, not to one certificate thumbprint. Microsoft does not document this publicly.

### Gaps
- No official Microsoft statement found on whether individual onboarding is "paused" in 2026 beyond the quickstart's US/Canada rule. The "paused" claim comes only from an AI answer.
- No official confirmation that a French EI or an association would pass organization validation.
- The live pricing page did not render prices. The $9.99 figure is confirmed by another Microsoft Learn page; $99.99 and $0.005 come from search snippets only.

## SignPath Foundation free code signing for open-source projects

### Takeaway
SignPath Foundation offers free code signing to OSI-licensed open-source projects through SignPath.io. The certificate is issued to **"SignPath Foundation"**, not to you, so that name appears as the publisher. The binaries must be built on GitHub-hosted CI from the public repository, and each release needs manual approval. A strict policy page and credit line are mandatory. The project must already be released and must have "a certain verifiable reputation", which is a hurdle for a brand-new project. Its system-modifying behaviour (taskbar, hooks, logon tasks) must be clearly disclosed, warned about and uninstallable.

### Cited Findings
- **Cost.** "For OSS projects, our services are free of charge." Example users: Stellarium, Flameshot, GitExtensions, Layer 1 Lite DB. — [signpath.org](https://signpath.org/)
- **Base conditions.** Exact wording where quoted:
  - "must use an OSI-approved Open Source license without commercial dual-licensing for all components" (GPL-3.0 qualifies).
  - No proprietary components, though system libraries are allowed.
  - The project must be actively maintained.
  - "The project must already be released in the form that should be signed."
  - Its functionality must be described on the download page.
  - No malware or PUPs.
  - Source: [SignPath Foundation terms](https://signpath.org/terms)
- **Conditions for using the Foundation certificate.**
  - The signing team must also be the dev/maintenance team and own the repo.
  - You may only sign your own binaries, "built from source in a verifiable way".
  - Every release needs manual approval.
  - Changes from non-committers must be reviewed.
  - The product name must match the project name.
  - Executables require "a certain verifiable reputation".
  - No hacking tools or security bypass.
  - Software "must not change the user's system configuration without proper warnings".
  - Uninstallation must be provided.
  - Any data transfer needs a privacy policy.
  - MFA is required for all members on SignPath and on the source repo.
  - Source: [SignPath Foundation terms](https://signpath.org/terms)
- **Website obligations.**
  - A "Code signing policy" must appear on the home and download pages.
  - Credit line: "Free code signing provided by SignPath.io, certificate by SignPath Foundation."
  - List the team roles: committers/reviewers and approvers.
  - Link a privacy policy, or state that the program transfers no information.
  - Source: [SignPath Foundation terms](https://signpath.org/terms). Real policy pages: [pandoc](https://pandoc.org/code-signing-policy.html), [Ink/Stitch](https://inkstitch.org/code-signing-policy), [OpenRCT2](https://openrct2.io/code-signing-policy), [Solarus](https://www.solarus-games.org/about/code-signing-policy)
- **GitHub Actions integration.**
  - Add the "GitHub.com" Trusted Build System and install the SignPath GitHub App.
  - Upload the unsigned artifact with `actions/upload-artifact@v4+`, then call `signpath/github-action-submit-signing-request@v3`.
  - Inputs: `api-token`, `organization-id`, `project-slug`, `signing-policy-slug`, `github-artifact-id`, and optionally `wait-for-completion` and `output-artifact-directory`.
  - Origin metadata comes from GitHub and "cannot be forged".
  - For OSS, all jobs leading up to the signing request must run on **GitHub-hosted runners**.
  - Signing of files inside the artifact (e.g. inner exe inside a zip/installer) is driven by the artifact configuration XML.
  - Source: [SignPath docs: GitHub trusted build system](https://docs.signpath.io/trusted-build-systems/github)
- **Policy maturity.** The terms page marks its code of conduct as a draft. — [SignPath terms](https://signpath.org/terms)

### Inferences
- **Fit for Caelestia.** GPL-3.0, a public GitHub repo and GitHub Actions builds fit SignPath well. The blockers are:
  - The "already released" and "verifiable reputation" requirements. Ship a few unsigned releases first and build stars and downloads.
  - The system-configuration clause. The installer must clearly warn that it hides the taskbar, registers logon tasks and installs a keyboard hook, and must ship a clean uninstaller (Add/Remove Programs).
- **Consequences of the Foundation certificate.** The publisher shown in UAC/SmartScreen would be "SignPath Foundation". This certificate is shared by many OSS projects, so it probably already has established SmartScreen reputation. That is an advantage over a fresh personal certificate, but this is not documented by SignPath or Microsoft.

### Gaps
- No public quantified threshold for "verifiable reputation" (stars, downloads, age).
- No typical approval delays found.
- No official statement found on SmartScreen behaviour for SignPath Foundation-signed binaries.

## Traditional OV/EV certificates in 2026 (CA/B Forum rules, EV and SmartScreen, prices, cloud HSM)

### Takeaway
Since March 1, 2026, publicly trusted code signing certificates (OV and EV) can be valid for at most **460 days** (CA/B Forum ballot CSC-31). Private keys must live in hardware (token or cloud HSM), so CAs now sell cloud-signing products. Microsoft states explicitly that **EV no longer bypasses SmartScreen**, so paying for EV to avoid warnings is "no longer justified". For an EU individual open-source developer, Certum's "Open Source Code Signing in the Cloud" (€49 listed) is the cheapest identity-validated option, issued in your own name.

### Cited Findings
- **460-day maximum.** CA/B Forum Ballot CSC-31 limits validity to 460 days for certificates issued from March 1, 2026 (adopted November 2025, Code Signing Baseline Requirements v3.10.0). It applies to OV and EV. Existing certificates run until their original expiry. — [sslinsights](https://sslinsights.com/code-signing-certificate-validity-reduced-460-days/); [SSL Dragon](https://www.ssldragon.com/blog/code-signing-trust-window-460-days/); [Encryption Consulting](https://www.encryptionconsulting.com/shorter-code-signing-cert/). All are vendor blogs; one ([TheSSLStore](https://www.thesslstore.com/blog/code-signing-certificates-lifespans-to-drop-to-one-year/)) says "one year", which conflicts with the 460-day figure.
- **Certum's effective date.** Certum says that from **February 27, 2026** a code signing certificate may be valid at most **459 days**, and multi-year options are being phased out. — [Certum shop, Code Signing](https://shop.certum.eu/code-signing.html) (via search snippet)
- **EV and SmartScreen.** Microsoft (page dated 2026-05-04): "EV certificates no longer bypass SmartScreen… they no longer impact SmartScreen behavior. Paying a premium for EV solely to avoid SmartScreen warnings is no longer justified." With an OV/EV certificate, the first downloads still show "unrecognized" but display the verified publisher name. — [SmartScreen reputation for Windows app developers](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
- **Certum Open Source Code Signing in the Cloud.**
  - Listed at €49.00 (net = gross).
  - Uses the SimplySign cloud with no card or reader, through the SimplySign mobile app and SimplySign Desktop (virtual smart card).
  - The certificate contains "natural person data prefixed with 'Open Source Developer' phrase".
  - Cloud products are limited to 5,000 signatures per month.
  - Marketed as "Trusted by Microsoft, also for building the reputation of Microsoft SmartScreen Filter".
  - Source: [Certum shop, Open Source Code Signing on SimplySign](https://shop.certum.eu/open-source-code-signing-on-simplysign.html). Other listings show an open-source set with card/reader from €69 and other variants from €359 — [Certum Open Source Code Signing set](https://shop.certum.eu/open-source-code-signing.html). Required documents: [Certum support](https://support.certum.eu/en/code-signing-required-documents/) (not fetched).
- **Standard Certum SimplySign through a reseller.** About €109/year on a 3-year term (pre-2026 multi-year pricing). — [SSLPOINT](https://sslpoint.com/eu/code-signing)

### Inferences
- **Certum open-source certificate vs SignPath.** The Certum certificate (Polish CA, EU-friendly, identifies an individual) is the most practical own-name option for a French solo developer. Expect roughly €49 plus renewal about every 15 months.
  - SimplySign requires an interactive phone-generated code, so unattended GitHub Actions signing is awkward. Community tools exist to automate SimplySign, but none was verified here. Signing locally on the developer's PC after CI builds is the simple path.
- **Reputation never transfers with a new certificate.** Each yearly renewal produces a new certificate (new key, unless re-keyed with the same subject). Microsoft says changing the signing certificate "affects the publisher trust signal", so expect some reputation loss on each renewal.

### Gaps
- The hardware-key requirement (CA/B Forum, effective June 1, 2023) was not fetched from a primary source. It is widely known, but not cited here.
- The exact date Microsoft removed EV's instant reputation (commonly said to be 2024) was not found in a primary source. The Microsoft page says only "years ago… no longer exists".
- No verified 2026 prices for Sectigo, DigiCert, SSL.com or GlobalSign OV/EV.

## How SmartScreen reputation works: signed vs unsigned, timescales, GitHub downloads, Mark-of-the-Web

### Takeaway
SmartScreen weighs **publisher (certificate) reputation** and **file-hash reputation**. Unsigned files start from zero on every new version. Signed files can carry reputation forward through a consistent publisher identity. Microsoft gives no threshold: it "can take several weeks and hundreds of clean installs". There is no submission mechanism to speed up consumer SmartScreen reputation. On Windows 11, Smart App Control can block unsigned files outright.

### Cited Findings
- **Two signals.** Publisher reputation and file-hash reputation. "When a file is not signed, SmartScreen reputation must build for each new version of your files, starting with zero reputation. Reputation cannot transfer from previous versions unless both were signed using the same publisher identity." — [SmartScreen reputation (Microsoft Learn, 2026-05-04)](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
- **What users see.** Unsigned or self-signed files get "Windows protected your PC" and the user must click "Run anyway". Enterprise policy can block continuation entirely. — [same](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
- **Timescale.** "There is no exact threshold, but it can take several weeks and hundreds of clean installs from a wide audience." "There is no need (or mechanism) to manually submit a file for SmartScreen reputation review for consumer endpoints." — [same](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
- **Microsoft's best practices.** — [same](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
  - Sign every release.
  - Do not modify files after signing.
  - Use a consistent signing identity.
  - Do not sign PUA-like software, or the certificate gains **negative** reputation.
  - Warn early adopters.
  - Publishing on the Microsoft Store avoids SmartScreen entirely.
- **Smart App Control.** "On Windows 11 devices, the Smart App Control feature may supersede SmartScreen… will block execution of unsigned files unless the file has a positive reputation… apply to all executable files, not just those downloaded from the Internet." — [same](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
- **Unknown-software warnings.** These "aren't blocks. Users can choose to download and run the application normally". — [How Microsoft identifies malware and PUA (2026-01-29)](https://learn.microsoft.com/en-us/defender-xdr/criteria)

### Inferences
- **Signing every inner binary matters.** Smart App Control checks every executable, not just downloaded ones. The NativeAOT helper exe and any DLLs extracted by the setup must also be signed. Otherwise Smart App Control can block the helper even though the installer was signed.
- **Mark-of-the-Web.** SmartScreen download checks are triggered by MotW (the Zone.Identifier stream), which browsers add to files downloaded from GitHub Releases. Files extracted by an installer normally do not inherit MotW, so the setup exe is the main SmartScreen exposure point. Smart App Control, however, applies regardless.
- **Microsoft Store.** For a theme that modifies the shell, Store distribution is probably not practical, but it is the only zero-warning path.

### Gaps
- No primary source fetched on how GitHub-hosted downloads (objects.githubusercontent.com) are treated by SmartScreen URL reputation versus file reputation.
- No primary source fetched on exactly which archive tools propagate MotW.

## Microsoft Defender and other antivirus false positives: triggers, submission, other projects' experience

### Takeaway
Dispute Defender detections by submitting the file as a **Software developer** on the WDSI portal (sign-in required). Track submissions in submission history, and use the developer contact form if you disagree with the verdict. Microsoft's own criteria matter here: hidden presence, missing uninstall, and "unauthorized registry changes"/system tampering are classification criteria. Shell-modification tools such as ExplorerPatcher have received persistent detections (e.g. `HackTool:Win64/ExplorerPatcher!MTB`, 2024) that were not resolved by submissions.

### Cited Findings
- **Submitting a file.**
  - Portal: https://www.microsoft.com/wdsi/filesubmission. Sign-in is required, and submissions moved to a new Entra ID app on May 20, 2024.
  - To dispute: "Submit the file in question as a software developer. Wait until your submission has a final determination. If you're not satisfied… use the developer contact form".
  - Track progress at https://www.microsoft.com/wdsi/submissionhistory. Statuses are Submitted, In progress and Closed. Use "rescan" to refresh the verdict.
  - Prevalent files and enterprise (SAID) customers are prioritized.
  - Source: [Submit files for analysis by Microsoft](https://learn.microsoft.com/en-us/defender-xdr/submission-guide)
- **Submission and reputation.** Submissions also help "start establishing reputation" for unknown software. — [How Microsoft identifies malware and PUA](https://learn.microsoft.com/en-us/defender-xdr/criteria)
- **Microsoft criteria relevant to Caelestia's behaviours.** — [criteria page, dated 2026-01-29](https://learn.microsoft.com/en-us/defender-xdr/criteria)
  - Lack of choice: "Fail to clearly indicate when the software is active… attempt to hide or disguise its presence". Also: install or remove software without consent.
  - Poor removal: not using Add/Remove Programs.
  - Tampering software includes "Unauthorized registry changes: Modifications to the Windows Registry or system settings that impact the security posture of the device" and "Manipulating system components".
  - Malware categories include "Hacktool" and "Obfuscator". Password stealers "often work along with a key logger".
  - PUA categories include "Evasion software" and "Poor industry reputation" (being flagged by other vendors).
- **ExplorerPatcher.**
  - Defender detection `HackTool:Win64/ExplorerPatcher!MTB` started in 2024. Users relied on Defender exclusions. — [MiniTool](https://www.minitool.com/news/hacktool-win64-explorerpatcher-mtb.html); [Microsoft Q&A](https://learn.microsoft.com/en-us/answers/a/7756196)
  - The developer called it Microsoft hostility rather than infection. — [Neowin](https://www.neowin.net/amp/new-explorerpatcher-update-fixes-win--x-start-menu-crashes-and-more-on-windows-11/)
  - The project dropped its new taskbar component from setup because of AV false positives. — [ExplorerPatcher release notes](https://newreleases.io/project/github/valinet/ExplorerPatcher/release/22621.3880.66.7_78e6248)
  - Bitdefender also flagged it. — [Bitdefender community](https://community.bitdefender.com/en/discussion/101363/explorerpatcher-detected-as-false-positive)
- **ExplorerPatcher issue #3125 (April 2024).**
  - A maintainer traced detections to a specific commit in the 64.3 prerelease and commented that code out.
  - The maintainer advised *against* submitting to Microsoft, because analysts may confirm malware on "certain patterns of system calls".
  - Users worked around it with folder exclusions in AppData\Roaming.
  - Source: [valinet/ExplorerPatcher#3125](https://github.com/valinet/ExplorerPatcher/issues/3125)
- **Windhawk (January 2025).** One of 69 VirusTotal engines flagged a file. The answer came from a community advisor, not Microsoft. — [Microsoft Q&A](https://learn.microsoft.com/en-us/answers/a/7996713)
- **Signing does not exempt PUA behaviour.** Signing PUA-like software gives the certificate negative reputation. — [SmartScreen reputation](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)

### Inferences
- **Which behaviours are riskiest.** Caelestia's helper combines a global low-level keyboard hook (keylogger-like API pattern), logon persistence through scheduled tasks, hiding the taskbar (a "hide/disguise"-adjacent shell change) and registry writes.
  - Each is legitimate, but together they resemble heuristic malware profiles.
  - Mitigations:
    - Keep the helper unpacked: no UPX, and avoid single-file self-extraction.
    - Keep clear version and product metadata, and sign it.
    - Show a visible tray icon or settings UI.
    - Write a README/installer disclosure.
    - Provide a full uninstaller in Add/Remove Programs that restores the taskbar.
    - Never disable or touch Defender settings.
- **Release checklist.** Upload each release candidate to VirusTotal before publishing. Submit any Defender hit as "Software developer" right away. For other vendors, use their own false-positive forms. ExplorerPatcher's experience shows that changing the triggering code pattern can work better than submission.
- **NativeAOT.** NativeAOT binaries are large native exes without IL metadata, so heuristic engines see them as ordinary unknown native code. No source specific to NativeAOT false positives was found.

### Gaps
- **Not researched in this pass:** PowerToys, YASB and AutoHotkey false-positive histories. AutoHotkey compiled scripts are commonly reported as flagged, but this was not fetched.
- No primary source on typical WDSI turnaround times for individual developers.
- No source quantifying how much signing reduces third-party AV heuristic detections.

## Signing in CI: installer and inner binaries, RFC 3161 timestamping, dual signing, verification

### Takeaway
Sign inner binaries (helper exe, DLLs) **before** packaging, then sign the setup exe (and its uninstaller, if generated separately). Use SHA-256 file digests and an RFC 3161 timestamp so signatures outlive short-lived certificates (72 h for Artifact Signing, at most 460 days for CA certificates). Verify with `signtool verify /pa /v`. Both Artifact Signing (`azure/artifact-signing-action`) and SignPath (`signpath/github-action-submit-signing-request`) have first-class GitHub Actions integrations. Artifact Signing needs Windows runners; SignPath requires GitHub-hosted runners for OSS.

### Cited Findings
- **Timestamping.** The Artifact Signing TSA is `http://timestamp.acs.microsoft.com`. Use RFC 3161 with SHA-256 through action inputs `timestamp-rfc3161` and `timestamp-digest`. — [artifact-signing-action](https://github.com/Azure/artifact-signing-action); [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- **After expiry.** Timestamped signatures stay valid after the 72-hour certificate expires unless it is revoked. — [concept-certificate-management](https://learn.microsoft.com/en-us/azure/artifact-signing/concept-certificate-management)
- **Verification.** "Command succeeded" but no signature visible → run `signtool.exe verify /v /debug /pa fileName`. — [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- **Do not modify after signing.** Avoid modifying files after signing, because that can break the signature. — [SmartScreen reputation](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
- **Artifact Signing action filters.** `files-folder-filter: exe,dll` signs inner binaries in a folder. `files-catalog` lists specific files. — [artifact-signing-action](https://github.com/Azure/artifact-signing-action)
- **SignPath artifact configuration.** The artifact configuration describes nested signing: the root `<zip-file>` from upload-artifact, then inner files. Signed output can be downloaded back into the workflow with `output-artifact-directory`. — [SignPath GitHub docs](https://docs.signpath.io/trusted-build-systems/github)

### Inferences
- **Typical pipeline for Caelestia.**
  1. Build the NativeAOT helper.
  2. Sign the helper exe.
  3. Build the installer (Inno/NSIS/WiX) embedding the signed helper. If Inno Setup is used, its `SignTool` directive also signs the generated uninstaller.
  4. Sign the setup exe.
  5. Verify with `signtool verify /pa /all`.
  6. Publish the SHA-256 hashes in release notes.
- **Dual signing (SHA-1 + SHA-256).** Only needed for Windows 7/Vista-era clients. For a Windows 10/11-only theme, a single SHA-256 signature with an SHA-256 RFC 3161 timestamp is sufficient. Not verified against a 2026 primary source.

### Gaps
- No 2026 primary source fetched for dual-signing deprecation or Windows 7 relevance.
- No primary source fetched on Inno Setup / NSIS signing directives.
