# Contributing

Thank you for helping Stellaria. Bug reports, ideas, translations, presets and code are all welcome.

## Before you start

- Search the existing issues and discussions first.
- For a bug, give the Stellaria version, the Windows build, the steps to reproduce, and the report from
  Stellaria > Help > Troubleshooting > Copy a report.
- Security problems go through private vulnerability reporting, never a public issue: see [SECURITY.md](SECURITY.md).

## Changes

1. Fork the repository and create a branch from `main`.
2. Keep each pull request to one change, and describe what it does and how you checked it.
3. Run the checks used by CI:
   - `dotnet test .` inside `helper/` (the helper's tests);
   - `npm ci` then `npm run lint` (the pack's JavaScript);
   - `cargo build --manifest-path launcher/Cargo.toml` (the app).
4. Code has no comments; explanations go in `docs/`. Follow the style of the surrounding code.
5. Presets and themes for the catalogue go in `registry/items/`; [registry/README.md](registry/README.md) gives the
   format and the rules.

## Review and releases

Every pull request is reviewed by a maintainer before it is merged, and `main` only changes through pull requests.
Releases are built by the Release workflow on GitHub-hosted runners and signed only after a maintainer approves the
signing request (see the code signing policy in the [README](README.md#code-signing-policy)).

By contributing, you agree that your work is published under the project's licence, GPL-3.0, and you follow the
[code of conduct](CODE_OF_CONDUCT.md).
