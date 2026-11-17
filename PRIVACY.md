# Privacy policy

Stellaria has no account, no telemetry, no analytics and no advertising. It does not collect, store or sell personal
data, and its maintainers receive nothing from your PC. Everything it reads (windows, notifications, clipboard history,
media, the desktop image) stays on your PC: the helper only listens on `127.0.0.1` and never logs notification
contents.

Stellaria contacts the following services, each directly from your PC. The setup lists them before installing.

| Service | When | What it receives | How to turn it off |
| --- | --- | --- | --- |
| [Open-Meteo](https://open-meteo.com/en/terms) | Weather in the bar and the dashboard, every 15 minutes | Your IP address and your approximate latitude and longitude | Theme settings > Dashboard > Weather |
| [ipinfo.io](https://ipinfo.io/privacy-policy) | To find an approximate location for the weather, at most every six hours (through Zebar) | Your IP address | Theme settings > Dashboard > Weather |
| [LRCLIB](https://lrclib.net) | Lyrics of the song that is playing | Your IP address, the title, artist, album and length of the song | Theme settings > Dashboard > Synchronised lyrics |
| [GitHub](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement) | The Stellaria app checks for an update when it opens and every six hours, reads the release notes after an update, and downloads an update you accept | Your IP address | Do not open the app; the theme itself never contacts GitHub |
| [winget](https://learn.microsoft.com/windows/package-manager/) (Microsoft) | Only when you install or repair Stellaria and Zebar is missing | What winget sends to download Zebar | Install Zebar yourself first |

Zebar, which displays the theme, is a separate program by glzr-io with its own behaviour.

Settings are stored on your PC only, in `%APPDATA%\Stellaria`, `%LOCALAPPDATA%\Stellaria` and Zebar's web storage.
Uninstalling Stellaria removes them unless you ask to keep them.

Questions about privacy can be asked in the repository's Discussions or through GitHub private vulnerability
reporting (see [SECURITY.md](SECURITY.md)).
