# Community, moderation and legal side of an open theme catalog (solo developer, France/EU, 2026)

Scope: "Caelestia for Windows" (GPL-3.0 port of the Caelestia Linux shell to Zebar) plus a launcher hosting a community catalog (themes, widget packs, colour schemes, possibly wallpapers), news and community links. Research date: 2026-11-16. Nothing here is legal advice; items marked **[LEGAL ADVICE]** should be checked with a French lawyer (avocat en droit du numérique) or, at minimum, against the official text on EUR-Lex / Légifrance.

Method note: about 19 tool calls. Several primary texts (EUR-Lex DSA text, Légifrance consolidated LCEN, gnu.org GPL FAQ, KDE Store / Flathub policies, Ko-fi pricing) were not fetched directly in this session; claims depending on them are flagged in Gaps or Inferences rather than presented as cited facts.

## Licensing of community packs (GPL implications, OSI requirement, assets, how other catalogs do it)

### Takeaway
Packs that copy or adapt Caelestia's GPL-3.0 CSS/JS are almost certainly derivative works and must be distributed under GPL-3.0 (or GPL-3.0-compatible terms with attribution); pure colour schemes, data files and original images are arguably separate works. The catalogs studied split into "must be GPL-compatible" (GNOME), "must ship a clear LICENSE" (Obsidian), "default licence if unspecified" (Windhawk: MIT) and "no licence gate" (Spicetify). The simplest safe rule for this project: every catalog entry must declare an SPDX licence; code that derives from Caelestia must be GPL-3.0-or-later; assets must carry their own licence and provenance.

### Cited Findings
- GNOME Shell extensions: the review guidelines state that "derived works like extensions MUST be distributed under compatible terms", and that submitting means approving GNOME to distribute it under compatible terms; reused code from another extension "MUST include attribution to the original author in the distributed files", otherwise rejection — [gjs.guide review guidelines](https://gjs.guide/extensions/review-guidelines/review-guidelines.html)
- GNOME also forbids copyrighted or trademarked content (brand names, logos, artwork, multimedia) "without proof of express permission from the owner"; forks "MUST have a unique name"; no telemetry; code "MUST be readable and reviewable JavaScript", not minified/obfuscated; AI use allowed but authors must be able to explain the code, and submissions with "comments serving as LLM prompts" are rejected — [gjs.guide review guidelines](https://gjs.guide/extensions/review-guidelines/review-guidelines.html)
- Historical GNOME intent: authors agree to GPL v2 or later on upload to extensions.gnome.org; later stated policy that extensions should be at least GPL-compatible — [gnome-shell-list 2012](https://lists.gnome.org/archives/gnome-shell-list/2012-January/msg00005.html); [gnome-shell-list 2013](https://lists.gnome.org/archives/gnome-shell-list/2013-May/msg00083.html); community debate on whether GPLv3 extensions fit: [GNOME Discourse](https://discourse.gnome.org/t/gplv3-extension-and-extensions-gnome-org/6506)
- GNOME's extension site does not expose licence information to users (open issue) — [extensions-web issue #53](https://gitlab.gnome.org/Infrastructure/extensions-web/-/issues/53)
- Obsidian: developer policies require a LICENSE file stating the licence and compliance with the licences of reused code; no obfuscation to hide purpose, no dynamically loaded ads; submissions now go through the Community directory (Obsidian account linked to GitHub to verify repo ownership), reviewed and published only by Obsidian team; only the initial version is reviewed — [Obsidian Developer policies](https://docs.obsidian.md/Developer+policies); [Submit your theme](https://docs.obsidian.md/Themes/App+themes/Submit+your+theme); [Community directory](https://docs.obsidian.md/community-directory)
- Windhawk (app is GPL-3.0): mods are submitted as pull requests to `ramensoftware/windhawk-mods`; mods that do not specify a licence are submitted under MIT; the author is responsible for licensing third-party code — [windhawk-mods via search result / Windhawk repo](https://github.com/ramensoftware/windhawk); [SourceForge listing (GPLv3)](https://sourceforge.net/projects/windhawk)
- Spicetify Marketplace: discovery is by GitHub topic tag (`spicetify-themes`, `spicetify-extensions`, `spicetify-apps`) on a public repo with a `manifest.json` (name, description, preview, main, readme); the wiki (last edited 2022) says nothing about licence requirements — [Spicetify Marketplace wiki](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
- WordPress precedent: WordPress.org holds that themes/plugins are derivative works inheriting the GPL, while acknowledging grey areas; the 2009 Software Freedom Law Center opinion treated PHP theme code as derivative but CSS and images as separate works — [Themes are GPL, too (WordPress.org, 2009)](https://wordpress.org/news/2009/07/themes-are-gpl-too/); [WordPress LICENSE](https://wordpress.org/LICENSE)
- The GNU GPL FAQ (as quoted secondhand) says copyleft is generally not worth it for minor works like templates and recommends permissive terms for templates because they are combined with user data — [Drupal group quoting the FAQ](https://groups.drupal.org/node/114819?page=1) (secondary; the FAQ itself was not fetched)

### Inferences
- Caelestia's CSS/JS widgets copied into a pack = modification of a GPL-3.0 work, so the pack must be GPL-3.0 (GPL-3.0-or-later if upstream allows "or later"; check upstream Caelestia licence header). A pack that only supplies a colour palette JSON/TOML or new original CSS that targets documented selectors is closer to the "separate work"/data case, but this is not settled law **[LEGAL ADVICE]** if a commercial or proprietary pack ever appears.
- Recommended catalog rule: (1) mandatory `license` field with an SPDX identifier in the pack manifest, plus a LICENSE file; (2) code licences limited to OSI-approved ones, and GPL-3.0-or-later required when the pack contains or adapts Caelestia/Caelestia-Windows code; (3) assets (fonts, icons, images) may use separate licences (OFL-1.1 for fonts, CC BY / CC BY-SA / CC0 for art) and must be listed with source URL and author in a `CREDITS`/`assets` section of the manifest; (4) no "NoDerivatives" or "NonCommercial" code licences (not OSI); allow CC BY-NC only for wallpapers if at all, flagged in the UI; (5) a default licence like Windhawk's "MIT if unspecified" is a bad fit here, because a GPL-derived pack cannot be MIT — reject instead of defaulting.
- Attribution: since Caelestia's design is by soramanew and contributors, each derived pack should carry a notice (GPL-3.0 section 5 requires "prominent notices" of modification). Add a CI check that the manifest has `license`, `authors`, `upstream` fields.
- Fonts: fonts bundled in packs should be OFL or similarly redistributable; Material Symbols (if used by Caelestia) are Apache-2.0 (not verified here). Icons from proprietary brands (Spotify, Discord logos) fall under GNOME-style trademark rules.

### Gaps
- Exact current gnu.org GPL FAQ wording on themes/CSS/plugins was not fetched (https://www.gnu.org/licenses/gpl-faq.html).
- KDE Store (store.kde.org / Pling) licence policy and Flathub licence requirements were not researched in this session.
- Upstream Caelestia licence text ("GPL-3.0-only" vs "or later") not verified here; check the upstream repo LICENSE.
- Whether Spicetify's marketplace has a licence gate or a blacklist file (it is believed to maintain a blacklist in its repo) is unverified.

## EU / French legal obligations for an individual operating a platform hosting user content

### Takeaway
If the catalog accepts third-party packs and makes them available to the public, the operator is a hosting service and very likely an "online platform" under the DSA. As a natural person far below the micro/small-enterprise thresholds, most online-platform duties (Section 3) are excluded, but hosting duties (notice-and-action under Art. 16, statement of reasons under Art. 17) and baseline intermediary duties remain. Art. 17 of the Copyright DSM Directive does not apply (open-source software-sharing platforms are excluded and the service is not a large commercial content-sharing service). In France, the website needs mentions légales; a non-professional individual may stay anonymous by publishing only the host's details, provided the host has their identity, and loses that option once there is any economic element (ads, affiliate links, sales).

### Cited Findings
- DSA Art. 19 disapplies Section 3 (online-platform obligations: Art. 20 internal complaints, Art. 21 out-of-court dispute settlement, Art. 22 trusted flaggers, etc.) for micro and small enterprises under Recommendation 2003/361/EC, except Art. 24(3); not applicable to VLOPs; exemption continues 12 months after outgrowing thresholds — [Prighter DSA Art. 19](https://prighter.com/resources/laws/dsa/articles/article-19); [Springlex DSA Art. 19](https://www.springlex.eu/en/packages/dsa/dsa-regulation/article-19/)
- Thresholds: micro = fewer than 10 staff and ≤ EUR 2 M turnover/balance sheet; small = fewer than 50 staff and ≤ EUR 10 M — [EdLaw DSA platform provisions](https://edlaw.eduverse.com/docs/jurisdiction/int/eu/dsa/PLATFORM-PROVISIONS.md) (secondary)
- Hosting services of any size remain subject to Art. 16 (notice-and-action) and Art. 17 (statement of reasons) and the basic intermediary obligations — [EdLaw intermediary duties](https://edlaw.eduverse.com/docs/jurisdiction/int/eu/dsa/INTERMEDIARY-DUTIES.md) (secondary)
- Art. 17 requires all hosting providers to give a clear and specific statement of reasons to affected users when removing/restricting content; submitting those statements to the Commission's DSA Transparency Database (Art. 24(5)) is an online-platform duty from which micro/small platforms are exempt under Art. 19(1) — [Commission DSA Transparency Database FAQ](https://digital-strategy.ec.europa.eu/en/faqs/dsa-transparency-database-questions-and-answers); [Prighter Art. 17](https://prighter.com/resources/laws/dsa/articles/article-17); [Mason Hayes & Curran](https://www.mhc.ie/latest/insights/dsa-transparency-database-to-apply-to-all-online-platforms)
- Copyright DSM Directive (EU) 2019/790 Art. 2(6): "open source software-developing and-sharing platforms" are expressly excluded from the definition of online content-sharing service provider, hence from Art. 17 — [Commission guidance on Art. 17](https://digital-strategy.ec.europa.eu/ga/library/guidance-article-17-directive-2019790-copyright-digital-single-market); [Directive text, Art. 2](https://www.legislation.gov.uk/eudr/2019/790/article/2/data.html)
- LCEN after the SREN law (loi n° 2024-449 du 21 mai 2024): publisher identification moved from former Art. 6 III to Art. 1-1 I; non-professional anonymity to Art. 1-1 II; penalties to Art. 1-2; Art. 6 now only covers intermediary services (ISPs, hosts, search engines, platforms) — [Simonnet Avocat, 22/04/2026](https://www.simonnetavocat.fr/mentions-legales-dun-site-internet-obligations-sanctions-et-modele/); [Légifrance, loi SREN](https://www.legifrance.gouv.fr/jorf/article_jo/JORFARTI000049563632)
- Art. 1-1 II: a non-professional publisher may keep identity private if the site shows the host's name/address and the host has been given the publisher's identification; host discloses only on judicial request; anonymity is lost with any economic counterpart (advertising, affiliate links, paid newsletter, sales) — [Simonnet Avocat](https://www.simonnetavocat.fr/mentions-legales-dun-site-internet-obligations-sanctions-et-modele/); same principle: [CMS Francis Lefebvre](https://cms.law/fr/fra/legal-updates/le-defaut-de-mentions-legales-peut-conduire-a-la-condamnation-penale-de-l-rsquo-editeur-du-site); [Legiscope](https://www.legiscope.com/blog/modele-mentions-legales-site.html)
- Penalty for missing mentions légales (Art. 1-2 LCEN): up to 1 year imprisonment and EUR 75,000 fine for a natural person — [Simonnet Avocat](https://www.simonnetavocat.fr/mentions-legales-dun-site-internet-obligations-sanctions-et-modele/)
- Conflicting guidance: some guides list name, home address and phone as mandatory even for a personal blog, without distinguishing the non-professional regime — [Weka](https://www.weka.fr/administration-locale/base-documentaire/management-strategique-de-l-information-t2-wk330/le-droit-de-l-information-et-le-droit-du-web-nv1815713788235/utiliser-les-mentions-legales-obligatoires-sur-un-site-internet-nv1815713788256.html); contradicted by the Art. 1-1 II regime above.
- LCEN Art. 19 (online traders' information duties) is scheduled to end 1 September 2026, its obligations moving into the Code de la consommation — [Simonnet Avocat](https://www.simonnetavocat.fr/mentions-legales-dun-site-internet-obligations-sanctions-et-modele/) (single source; relevant only if the project sells anything)
- GitHub DMCA: GitHub restores counter-noticed content within 10-14 days unless the claimant reports a lawsuit; forks are not disabled automatically unless named in the notice; notices and counter notices are published (redacted) in the github/dmca repo — [GitHub DMCA takedown policy](https://docs.github.com/en/site-policy/content-removal-policies/dmca-takedown-policy); [Guide to counter notices](https://docs.github.com/en/site-policy/content-removal-policies/guide-to-submitting-a-dmca-counter-notice)

### Inferences
- Architecture choice drives exposure. If packs live in authors' own GitHub repos and the catalog only indexes a `catalog.json` of links (Spicetify / Obsidian model), GitHub is the host of the content and handles DMCA; the project is closer to a search/index tool and its exposure is mainly to delisting. If the project mirrors pack files (own repo, CDN, release assets), it becomes the hosting service for them. Recommendation: index-only, pinned to commit SHA plus hash, with delisting as the moderation lever.
- Even index-only, publish a DSA-style notice channel: a "Report a pack" link (GitHub issue form plus an email address) accepting the Art. 16 elements (reason, exact location/URL, notifier name and email, good-faith statement), acknowledge receipt, decide "in a timely, diligent, non-arbitrary and objective manner", and send the pack author a short statement of reasons (what was removed, why, which rule or law, how to contest). A template in `docs/` covers Art. 17.
- Points of contact (DSA Art. 11 for authorities, Art. 12 for users) and terms and conditions describing moderation (Art. 14) apply to all intermediaries per the general reading; a single email address plus a "Catalog rules" page covering content rules and moderation steps should meet this for a micro provider **[LEGAL ADVICE]** for confirmation. Art. 12 specifics were not found in this session's searches.
- Art. 18 DSA (notifying authorities of suspected criminal offences involving threat to life/safety) applies to hosts of any size; very unlikely to arise for themes but worth a line in the moderation runbook (Art. 18 not verified this session).
- Being a French resident, the competent Digital Services Coordinator is Arcom (not verified this session).
- GDPR with no accounts and no telemetry: still applicable to (a) server/CDN logs with IP addresses if the project runs its own website or download endpoint, (b) emails received for reports/takedowns, (c) any Discord/Matrix server the project runs. GitHub Pages and GitHub releases put GitHub in the role of processor/controller for its own logs. Minimum: a short privacy notice (who, what data, why, retention, rights, CNIL complaint), log retention limited (for example 30 days or less), no third-party fonts/analytics loaded from the launcher without need. The launcher fetching `catalog.json` and images from GitHub/jsDelivr discloses the user's IP to those hosts: mention it in the privacy notice. **[LEGAL ADVICE]** on whether the launcher's update/catalog fetch needs more than an information notice (it should not need consent since no cookies/tracking, but ePrivacy Art. 5(3) applies only to terminal-device storage/access).
- Mentions légales for a free, non-commercial project site: publishing host details (for example GitHub Inc. or the static host) and keeping identity private is permitted, but if donations become regular income or there is any sale/ads, the safer path is to switch to full identification **[LEGAL ADVICE]**: it is not established whether donations alone count as an "economic counterpart".

### Gaps
- The DSA text itself (EUR-Lex 32022R2065) was not fetched; Arts. 11, 12, 14, 16, 17 and 18 wording should be confirmed there: https://eur-lex.europa.eu/eli/reg/2022/2065/oj
- No source found on how Art. 16 is scaled down in practice for one-person hosts, or on Arcom guidance for small French hosts.
- Whether an index-only catalog (links, no file hosting) is a "hosting service" or merely a search/linking service under the DSA is unresolved **[LEGAL ADVICE]**.
- CNIL guidance on server logs for a personal website was not fetched.

## Wallpapers: copyright risk

### Takeaway
Wallpapers are the highest-risk content type: most attractive wallpapers (anime art, game art, photography) are copyrighted, fan art is legally contested, and takedown processes are both necessary and abused. Recommendation: either ship no wallpapers, or accept only wallpapers whose author is the submitter or that carry an explicit free licence (CC0, CC BY, CC BY-SA, Unsplash-like terms), with source URL mandatory.

### Cited Findings
- GNOME forbids copyrighted artwork without proof of express permission from the owner — [gjs.guide review guidelines](https://gjs.guide/extensions/review-guidelines/review-guidelines.html)
- Steam Workshop (including Wallpaper Engine items) faced waves of bogus DMCA claims in 2025; appealing requires disclosing personal data, which many authors refuse — [Notebookcheck](https://www.notebookcheck.net/DMCA-abuse-on-Steam-is-out-of-control-and-Valve-isn-t-fixing-it.1084373.0.html); [Gamepressure](https://www.gamepressure.com/newsroom/valve-has-to-to-do-something-about-it-major-steam-workshop-crisis/z283be)
- Wallpaper Engine community threads show users treating animated wallpapers made from found art as infringement, and handling via DMCA emails — opinion, not law — [Steam discussion](https://steamcommunity.com/app/431960/discussions/2/3770111882048126651)
- GitHub publishes DMCA notices and counter notices publicly; counter notices require the respondent's identity — [GitHub DMCA policy](https://docs.github.com/en/site-policy/content-removal-policies/dmca-takedown-policy)

### Inferences
- Under French/EU law there is no US-style "fair use"; exceptions are narrow (Art. L122-5 CPI), so redistributing someone's artwork as a wallpaper without licence is infringing in nearly all cases.
- Practical rules: wallpaper entries must include `author`, `source_url`, `license`; reject "found on Reddit/Pinterest/wallhaven" items without a licence; prefer linking out to the artist's page over rehosting; never rehost wallpapers in the main repo (keeps repo history clean and makes removal effective). If a notice arrives: delist immediately, notify submitter with reasons, restore only with proof of licence.
- Preview screenshots of themes that include a third-party wallpaper are also a reproduction; ask submitters to use free wallpapers in screenshots.

### Gaps
- No official Wallpaper Engine or Valve policy page was confirmed; no case law on wallpaper redistribution found.

## Governance and moderation workload

### Takeaway
A solo maintainer should keep the catalog GitOps-based: one `catalog` repo, submissions as pull requests (or issue forms that a bot converts), automated validation in CI, human review limited to the first submission plus diffs of executable code, CODEOWNERS for trusted reviewers, a Contributor Covenant 3.0 code of conduct, a published content policy and a takedown/report process. Abandoned and impersonating packs need explicit rules up front.

### Cited Findings
- Obsidian reviews only the initial submission; review time depends on team workload; only team members publish — [Obsidian Submit your theme](https://docs.obsidian.md/Themes/App+themes/Submit+your+theme); [Community directory](https://docs.obsidian.md/community-directory)
- GNOME reviews "carefully for malicious code, malware and security risks, but not for bugs"; unreadable/obfuscated code is rejected — [gjs.guide review guidelines](https://gjs.guide/extensions/review-guidelines/review-guidelines.html)
- Windhawk accepts mods as pull requests to a central repo — [Windhawk](https://github.com/ramensoftware/windhawk)
- Spicetify uses GitHub topic tags for automatic discovery (low maintainer workload, low control) — [Spicetify Marketplace wiki](https://github.com/spicetify/marketplace/wiki/Publishing-to-Marketplace)
- Contributor Covenant 3.0 was released 28 July 2025 as a major rewrite, with a builder to add one's own reporting/enforcement procedures; enforcement ladder: Correction, Warning, Temporary Ban, Permanent Ban, rungs may be skipped depending on severity; licensed CC BY-SA 4.0; adopted by Django (April 2026) and Hanami (Sept 2025) — [Ethical Source announcement](https://ethicalsource.dev/blog/contributor-covenant-3); [Django adoption](https://www.djangoproject.com/weblog/2026/apr/15/contributor-covenant-adoption/); [Hanami adoption](https://hanamirb.org/blog/2025/09/02/hanami-adopts-contributor-covenant-3-0)
- Naming: forks must have a distinct name; the gnome.org namespace is reserved — [gjs.guide review guidelines](https://gjs.guide/extensions/review-guidelines/review-guidelines.html)

### Inferences
- Recommended model ("curated index"): `caelestia-windows-catalog` repo with one manifest per pack (`packs/<id>/manifest.json`) pointing to the author repo and a pinned commit SHA + SHA-256. Updates are PRs bumping the SHA. CI checks schema, SPDX licence, image sizes, no executables/binaries, no network calls in JS (grep for `fetch`/`XMLHttpRequest`/`WebSocket` to non-allowlisted hosts), no minified code. First submission: full human review. Updates touching only CSS/colour: auto-merge after CI plus 24-48 h. Updates touching JS: human review.
- CODEOWNERS: `/packs/<id>/` owned by the pack author's GitHub handle (they can approve bumps to their own pack), `/schema/` and `/.github/` owned only by the maintainer. Promote "trusted reviewers" after N good reviews, via a GitHub team; document it in `GOVERNANCE.md`.
- Trust levels in the launcher: "Official" (maintained in main repo), "Reviewed" (catalog), "Unlisted/sideload" (user pastes a URL, with a warning). This limits liability and workload.
- Abandoned packs: if a pack breaks against the current version and the author does not respond within 30-60 days to an issue, mark "unmaintained" (hidden by default); allow adoption by a new maintainer only via a fork with a new name or explicit author consent (GPL allows forking; the name/id is the project's call).
- Impersonation/naming: reserved names (`caelestia`, `official`, `default`, the maintainer's handle, trademarks such as Windows/Microsoft/Spotify); pack id = `author.slug`; no use of third-party logos without permission (GNOME-style rule).
- Content rules: no NSFW (desktop themes are shown in screenshots; a launcher catalog is used by minors), no hate symbols/harassment, no political propaganda in previews/names; no malware, crypto miners, telemetry or remote code loading; no ads. Keep it in a `CONTENT_POLICY.md` linked from the launcher; this doubles as the DSA Art. 14 terms description.
- Workload estimate: a review queue tends to concentrate around releases; with CI doing schema/licence checks, a first-submission review of a CSS-only pack is minutes; JS widget packs need real reading. A "maximum open submissions per author" and a monthly "review day" keep it sustainable.

### Gaps
- KDE Store and Flathub governance documents (Flathub's review and verified-publisher model) were not fetched this session.
- No quantitative data found on review workload for comparable catalogs.

## Community channels, news feed and bug reporting without telemetry

### Takeaway
GitHub Discussions keeps everything in one place, indexable, tied to the existing identity, and under GitHub's moderation tools and policies; Discord gives reach but is a separate moderation burden and personal-data space; Matrix fits FOSS values but has a smaller audience. News can be sourced from GitHub Releases (Atom feed) plus a `news.json` in the repo, served statically. Bug reports: issue forms with a diagnostics bundle the user generates and reviews locally.

### Cited Findings
- No primary sources on Discussions vs Discord vs Matrix were fetched this session (see Gaps).
- GNOME forbids telemetry in extensions ("MUST NOT use any telemetry tools to track users and share the user data online"), a useful precedent for the catalog's own rules — [gjs.guide review guidelines](https://gjs.guide/extensions/review-guidelines/review-guidelines.html)

### Inferences
- Recommendation: GitHub Discussions as the primary forum (categories: Announcements [maintainer-only], Q&A, Show and tell, Pack requests, Ideas); optional Discord later only with at least one co-moderator; a Matrix room bridged if wanted. Running a Discord server makes the maintainer responsible for moderating it under Discord's rules and the CoC.
- News feed: GitHub Releases provide an Atom feed at `https://github.com/<owner>/<repo>/releases.atom` (standard GitHub behaviour, not verified here). A `news.json` (id, date, title, body in Markdown, optional link, min/max app version) in the main repo, fetched via raw GitHub or jsDelivr and cached by the launcher, gives control over non-release news and costs nothing; sign it or fetch over HTTPS from a fixed URL only.
- Bug reports: GitHub issue forms (`.github/ISSUE_TEMPLATE/*.yml`) with required fields (version, Windows build, Zebar version, pack list); the helper produces a `diagnostics.zip` (versions, config with secrets/paths redacted, recent logs) that is shown to the user before they attach it manually. No automatic upload means no GDPR processing by the project beyond what the user chooses to post publicly; warn that GitHub issues are public.

### Gaps
- GitHub Discussions documentation, Discord/Matrix comparisons and GitHub releases.atom behaviour were not fetched this session; verify at https://docs.github.com/en/discussions and in GitHub docs.

## Donations and sponsorship

### Takeaway
GitHub Sponsors (no fee on sponsorships from personal accounts, payouts to an individual) is the lowest-friction option and fits the GitHub-centric setup; Open Collective via a fiscal host (Open Collective Europe) gives transparency and keeps money off the personal account but costs a host fee; Ko-fi is simple but fees were not verified. In France, donations received by an individual are taxable income and may affect the mentions légales anonymity regime.

### Cited Findings
- GitHub Sponsors: secondary sources report no fee on sponsorships from personal accounts and up to 6% for organization-account sponsorships; GitHub docs mention a 3% card processing charge organisations can avoid by invoice — [Codenote comparison](https://codenote.net/en/posts/github-sponsors-vs-open-collective-fees-comparison/); [GitHub docs: sponsorship fees and taxes](https://docs.github.com/en/enterprise-cloud@latest/sponsors/sponsoring-open-source-contributors/about-sponsorships-fees-and-taxes)
- GitHub Sponsors supports payouts via a fiscal host for user accounts, including Open Collective Europe — [GitHub docs: fiscal host](https://docs.github.com/en/sponsors/receiving-sponsorships-through-github-sponsors/using-a-fiscal-host-to-receive-github-sponsors-payouts); [GitHub changelog 2022-09-21](https://github.blog/changelog/2022-09-21-github-sponsors-fiscal-host-support-for-user-accounts)
- Open Collective: platform free for collectives; when a host charges a host fee, Open Collective takes 15% of that host-fee revenue; payment processor fees (Stripe/PayPal/Wise) apply — [Codenote comparison quoting Open Collective](https://codenote.net/en/posts/github-sponsors-vs-open-collective-fees-comparison/)
- Typical host fees are 5-15% (secondhand) — [Beancount tax guide, 2026](https://beancount.io/blog/2026/07/08/open-source-maintainer-sponsorship-income-taxes-guide)
- Anonymity under LCEN Art. 1-1 II is lost when there is an economic counterpart (ads, affiliate links, paid newsletter, sales) — [Simonnet Avocat](https://www.simonnetavocat.fr/mentions-legales-dun-site-internet-obligations-sanctions-et-modele/)

### Inferences
- French tax treatment: regular donations to an individual are generally declared as income (BNC non professionnels); they are not tax-deductible for donors (not an association d'intérêt général); occasional small amounts still technically taxable **[LEGAL ADVICE / expert-comptable]**. Using Open Collective Europe as fiscal host moves the money to an entity, but reimbursement of expenses to the maintainer is the usual pattern and paid work remains taxable.
- Do not offer perks that make donations look like sales (paywalled packs, early access) unless prepared to handle consumer law, VAT and full mentions légales.
- Keep it in one place: GitHub Sponsors button (`.github/FUNDING.yml`) plus optionally Ko-fi; state in the README that donations do not buy priority support or catalog placement (keeps moderation neutral).
- Credit upstream: consider a line pointing to soramanew/Caelestia's own funding links, since the design is theirs.

### Gaps
- Ko-fi fee schedule not found in this session (check https://ko-fi.com pricing / Ko-fi Gold).
- Official current GitHub Sponsors fee page and Open Collective Europe host fee not confirmed directly.
- No French official source (impots.gouv.fr, URSSAF) fetched on taxation of donations to individuals or open-source maintainers.
