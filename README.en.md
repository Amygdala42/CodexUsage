# CodexUsage

[简体中文](README.md) | [English](README.en.md)

A lightweight Windows widget for Codex quota and reset time, next to the notification area.

**[Download CodexUsage.exe](https://github.com/Amygdala42/CodexUsage/releases/download/v1.0.3/CodexUsage.exe)** · [Releases and updates](https://github.com/Amygdala42/CodexUsage/releases)

For Windows x64.

![CodexUsage taskbar widget](assets/widget-preview.png)

## Features

- The upper disk and percentage show remaining quota; the lower disk and countdown show time left in the same window.
- Shows returned quota windows with the same rules for Plus and Pro; Spark quotas are excluded.
- Refreshes automatically every five minutes, with manual refresh available.
- Shows the latest public reset announcement and its type, with a separate Source link. Announcements do not confirm an individual account reset.
- Saves your Chinese / English preference; fixed 100% app sizing follows Windows system DPI.
- Switches instantly between dark and light modes and remembers the choice; existing settings default to dark.

## Examples

Illustrations of the v1.0.3 layout with example data; available quota windows depend on the account's actual response.

### Plus

![Plus interface example](assets/plus-preview-en.png)

### Pro

![Pro interface example](assets/pro-preview-en.png)

## Download and run

Requires Windows x64, .NET Framework 4.8 or a newer 4.x version, and Codex installed and signed in with a subscription account.

1. Download `CodexUsage.exe` from Releases, or download and extract `CodexUsage-Windows-x64.zip`.
2. Run `CodexUsage.exe`. Click the widget for details; move the pointer away to dismiss them.

Hover for a quick summary; move away or click to dismiss it. Right-click for the menu. The detail header shows the version and a link to the GitHub project.

Change appearance with the Light mode / Dark mode button in the details view, or right-click the widget or tray icon and choose Appearance → Dark mode / Light mode. Language, theme and quota selection share one row. Light mode uses a soft neutral-gray background, with light blue #7BBDFF and blue #1C8DFF quota and time disks. Dark mode retains its light green and lighter green palette. The widget, details, dropdown, tooltip and tray icon update without a restart; the next launch restores your choice.

To update, exit the old version from the tray, then replace the EXE. Settings live in `%LOCALAPPDATA%\CodexUsage`; existing settings beside the EXE are imported on first run.

See [TESTING](docs/TESTING.md) for verification scope.

## Development

The current source is **1.0.4 with dark/light mode switching (unreleased)**. The download link above still points to the published 1.0.3 release, and the illustrations retain that layout. Fixes address protocol input encoding, announcement caching and retries, keeping the usage selector open during refresh, and responding to taskbar occlusion through desktop reorder events. See [TESTING](docs/TESTING.md) for verification scope.

```powershell
./scripts/build.ps1
./scripts/test.ps1 -Suite All
```

[BUILD](docs/BUILD.md) · [TESTING](docs/TESTING.md) · [PRIVACY](docs/PRIVACY.md)

## License

[MIT](LICENSE) · Amygdala42
