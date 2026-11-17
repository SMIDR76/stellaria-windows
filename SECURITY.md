# Security policy

Stellaria runs a background helper with access to windows, the clipboard and the desktop image.
Security reports are taken seriously and handled before any other work.

## Supported versions

Only the latest release receives security fixes.

## Reporting a vulnerability

Please do not open a public issue for a security problem.

Use GitHub private vulnerability reporting instead: open the repository's **Security** tab and choose
**Report a vulnerability**. Include the version, the steps to reproduce and the impact you observed.

You will receive an answer within seven days. A fix is released as soon as it is ready and the advisory credits the
reporter unless they prefer to stay anonymous.

## Scope

In scope: the helper executable, the Zebar pack, the install and uninstall scripts and the release pipeline.
The design and the threats already considered are described in `docs/security.md`.
