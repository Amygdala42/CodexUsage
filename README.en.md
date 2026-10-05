# CodexUsage

[简体中文](README.md) | [English](README.en.md)

A lightweight Windows widget for Codex quota and reset time, next to the notification area.

**[Download v1.1.0 · CodexUsage.exe](https://github.com/Amygdala42/CodexUsage/releases/download/v1.1.0/CodexUsage.exe)** · [ZIP package](https://github.com/Amygdala42/CodexUsage/releases/download/v1.1.0/CodexUsage-Windows-x64.zip) · [Release notes](https://github.com/Amygdala42/CodexUsage/releases/tag/v1.1.0)

For Windows x64.

The download links point to the **v1.1.0 release published on 2026-10-02**. Repository source additionally contains settings recovery, protocol bounds, interaction and packaging fixes, plus immediate reset-news queries on manual Refresh. These fixes are not yet included in release assets; see [unreleased changes](CHANGELOG.md).

The v1.1.0 palette (2026-10-02): dark mode uses dark slate surfaces with two cyan-blue highlights matching the desktop icon; its taskbar percentage remains near-white. Light mode uses gentle gray surfaces. The quota disk, popup quota value and progress bar use #4B885F; the time disk, taskbar countdown, popup reset text and links use #25663B. The light-mode taskbar percentage and pre-load placeholder dash are pure black #000000. Ordinary body text remains dark, while stale and error states keep their neutral-gray and amber cues.

![CodexUsage taskbar widget](assets/widget-preview.png)

## Features

- The upper disk and percentage show remaining quota; the lower disk and countdown show time left in the same window.
- Shows returned quota windows with the same rules for Plus and Pro; Spark quotas are excluded.
- Refreshes automatically every five minutes, with manual refresh available.
- Shows the latest public reset announcement and its type, with a separate Source link. Announcements do not confirm an individual account reset.
- In current source, manual Refresh also queries public reset news immediately; automatic news requests retain their cache and retry intervals.
- Saves your Chinese / English preference; fixed 100% app sizing follows Windows system DPI.
- Switches instantly between dark and light modes and remembers the choice; existing settings default to dark.

## Examples

Illustrations of the v1.1.0 layout using example data, not live-account screenshots; available quota windows depend on the account's actual response.

### Plus

![Plus interface example](assets/plus-preview-en.png)

### Pro

![Pro interface example](assets/pro-preview-en.png)

### Dark and light

The production renderer and example data illustrate both themes in normal, stale, error and syncing states.

![Dark and light rendering preview](assets/appearance-preview.png)

## Download and run

Requires Windows x64, .NET Framework 4.8 or a newer 4.x version, and Codex installed and signed in with a subscription account.

1. Download `CodexUsage.exe` from Releases, or download and extract `CodexUsage-Windows-x64.zip`.
2. Run `CodexUsage.exe`. Click the widget for details; move the pointer away to dismiss them.

Hover for a quick summary; move away or click to dismiss it. Right-click for the menu. The detail header shows the version and a link to the GitHub project.

Change appearance with the Light mode / Dark mode button in the details view, or right-click the widget or tray icon and choose Appearance → Dark mode / Light mode. Language, theme and quota selection share one row. Dark mode pairs dark slate surfaces with cyan-blue disks matching the desktop icon (#3ABED7 / #339AC5); the taskbar percentage remains near-white. Light mode pairs soft gray surfaces with two greens: the quota disk, popup quota value and progress bar use #4B885F; the time disk, taskbar countdown, popup reset text and links use #25663B. The taskbar percentage and pre-load placeholder dash are pure black #000000. Ordinary body text stays dark, while stale and error states retain neutral-gray and amber cues. Theme switching needs no restart; the next launch restores your choice.

To update, exit the old version from the tray, then replace the EXE. Settings live in `%LOCALAPPDATA%\CodexUsage`; existing settings beside the EXE are imported on first run.

See [TESTING](docs/TESTING.md) for verification scope.

## Development

Source remains at **v1.1.0**, with executable file and assembly versions **1.1.0.0**. The 2026-10-02 release package passed **278 checks**; after the subsequent source fixes, the full rerun on 2026-10-03 passed **320 checks, zero failures**. These results correspond to different source states. See [TESTING](docs/TESTING.md) for native GUI, live Plus-account and other multi-display/DPI verification limits.

```powershell
./scripts/build.ps1
./scripts/test.ps1 -Suite All
# Build, run all tests and create a separate delivery batch
./scripts/package.ps1
```

Temporary build and test products go to `build/app/` and `build/tests/`. Deliveries go to `output/YYYY-MM-DD/batch-NNN/`, using the Asia/Shanghai date and an increasing daily batch number without replacing existing batches. The executable stays `CodexUsage.exe`, current version 1.1.0.0. `env/` is reserved for actual environments, not deliveries, reports or Git worktrees. This build uses the system environment and needs no repository-local environment installation.

See [project directory rules](PROJECT_RULES.md) for the layout.

[BUILD](docs/BUILD.md) · [TESTING](docs/TESTING.md) · [PRIVACY](docs/PRIVACY.md)

## License

[MIT](LICENSE) · Amygdala42
