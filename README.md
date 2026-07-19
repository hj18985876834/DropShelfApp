# EdgeTuck

[English](README.md) | [简体中文](README.zh-CN.md)

EdgeTuck is a lightweight Windows edge shelf for temporarily holding files,
folders, text, links, and images while you work across apps and folders. It runs
locally, keeps file/folder records as path references, stores pasted images in
local app data, and does not require an account or cloud service.

## Features

- Edge shelf: a small screen-edge handle shows or hides the shelf.
- Pin shelf: keep the hover-opened shelf expanded until you hide it manually.
- Drag in: drop files, folders, text, URLs, and images onto the handle or shelf.
- Batch import: paste structured lines or use quick paste to add multiple files,
  folders, images, or links from the clipboard.
- Drag out: drag shelf cards back to Explorer, Desktop, editors, chat windows,
  or other apps that accept the payload.
- Card management: search cards, filter by item type, multi-select visible
  results, and reorder cards by dragging the type badge area.
- Quick actions: copy, open, reveal in Explorer, remove one item, or clear all
  records.
- Fast access: global shortcuts show/hide the shelf or add current clipboard
  content in the background.
- Local persistence: shelf records and settings are restored after restart.
- Settings: shelf position, theme, density, language, Start with Windows, and
  automatic update-check policy.
- Updates: weekly metadata checks by default, release notes before install,
  installer download, checksum validation, installer launch, and a one-time
  updated-version notice.
- Tray icon: show shelf, hide shelf, open settings, or quit.

## System Requirements

- Windows 10 or later, x64.
- No administrator permission is required for normal use or per-user install.
- The installer installs under the current user profile.

## Installation

Download `EdgeTuckSetup.exe` from the project release page and run it.

The installer:

- installs EdgeTuck to `%LOCALAPPDATA%\Programs\EdgeTuck`;
- creates a Start Menu shortcut;
- optionally creates a desktop shortcut;
- launches EdgeTuck after installation when selected;
- registers a normal Windows uninstall entry;
- removes legacy `DropShelf` shortcuts during renamed upgrades.

For development validation without installing, publish the app and run the full
published folder, not a single copied executable. The current validation output
is:

```text
artifacts/publish/win-x64/DropShelf.App.exe
```

When running a validation build from WSL, copy the full `win-x64` publish folder
to a Windows-local folder before launching.

## Basic Usage

1. Start EdgeTuck.
2. Use the edge handle to open the shelf.
   Use the pin button in the shelf header if you want the shelf to stay open
   after moving the pointer away.
3. Drag files or folders onto the handle or shelf to create file/folder records.
4. Copy text, URLs, or screenshots in another app, then paste into EdgeTuck.
   Structured clipboard text with multiple existing paths or links is imported
   as separate cards.
5. Use each card's actions:
   - `Copy`: copy the path, text, URL, or image.
   - `Open`: open the file, folder, URL, or image with Windows defaults.
   - `Reveal`: show file/folder/image location in Explorer.
   - `Remove`: remove the shelf record.
6. Use the filter control and search box to narrow visible cards.
7. Use `Ctrl+Click`, `Shift+Click`, or `Ctrl+A` to select multiple visible
   cards. Mixed-type multi-copy is not supported; EdgeTuck will ask you to
   select one type before copying. Files, folders, images, text, and links are
   copied with type-specific clipboard formats.
8. Drag the type badge area on a card to reorder cards inside the shelf; the
   dragged card lifts visually and cards move dynamically while you drag.
9. Drag the card body out to another app when you want to reuse it.

Removing a file or folder record never deletes the original file or folder.
Clearing the shelf removes records only. App-owned pasted image files may be
deleted when their image records are removed.

## Keyboard And Tray

- `Esc`: hide the shelf when it has focus.
- `Delete`: remove the selected item.
- `Enter`: open the selected item.
- `Ctrl+C`: copy the selected item.
- `Ctrl+Click`: add or remove a visible card from the current selection.
- `Shift+Click`: select a visible range.
- `Ctrl+A`: select all current visible cards.
- `Ctrl+Alt+Space`: show or hide EdgeTuck globally.
- `Ctrl+Alt+V`: add current clipboard content to EdgeTuck in the background.
- Tray icon double-click: show the shelf.
- Tray menu: Show Shelf, Hide Shelf, Settings, Quit.

## Settings

Open Settings from the shelf or tray menu.

Available settings:

- Shelf position: choose the screen edge for the handle/shelf.
- Reset to right edge: restore the default shelf position.
- Theme: System, Light, or Dark.
- Density: Compact or Comfortable.
- Language: Chinese or English.
- Start with Windows: start EdgeTuck when the current user logs in.
- Automatically check for updates: Never, Daily, or Weekly. New installs default
  to Weekly. Automatic checks only fetch update metadata and never download or
  install updates without your click.

Click Apply to save changes. Settings are stored under:

```text
%LOCALAPPDATA%\DropShelf\settings.json
```

## Start With Windows

EdgeTuck uses the current-user Windows Run key:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
EdgeTuck = "<path to DropShelf.App.exe>"
```

This does not write to system-wide `HKLM` and does not require administrator
permission. To verify it:

```powershell
reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v EdgeTuck
```

To remove it manually:

```powershell
reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v EdgeTuck /f
```

If you enable Start with Windows from a temporary validation directory, the Run
key will point to that temporary executable path. Disable the setting or remove
the registry value after validation if you do not want to keep that path.

## Updates

Open Settings and use Check for updates, or leave automatic checks enabled.
New installs check for update metadata at most once per week.

The app reads this update manifest:

```text
https://raw.githubusercontent.com/hj18985876834/DropShelfApp/main/updates/latest.json
```

When a newer version is available, EdgeTuck shows the target version, release
date, installer size, SHA-256 summary, mandatory flag, and release notes before
installation. You must confirm before it downloads `EdgeTuckSetup.exe`. The
installer is launched only after SHA-256 and byte-size validation pass, and the
running app then quits so Setup can replace files.

After the updated app starts successfully, EdgeTuck shows a one-time tray notice
with the installed version. A successful update check also refreshes the
settings-page software name and introduction from the manifest branding
metadata. The app does not require an account, telemetry, or cloud service.

## Uninstall

Use Windows Settings:

```text
Settings > Apps > Installed apps > EdgeTuck > Uninstall
```

The installer uninstall removes the installed program files and shortcuts. It
also removes the `EdgeTuck` Start with Windows Run value. For upgrades from
older builds, it also removes the legacy `DropShelf` Run value and stale
`DropShelf` Start Menu or desktop shortcuts if they exist.

User data may remain under:

```text
%LOCALAPPDATA%\DropShelf
```

Delete that folder manually if you want to remove shelf records, settings,
downloaded update installers, logs, and app-owned pasted images.

## Local Data

EdgeTuck stores user data under the legacy local data directory:

```text
%LOCALAPPDATA%\DropShelf
```

Common files and folders:

- `settings.json`: app settings.
- `shelf.json`: shelf records.
- `images\originals\`: app-owned pasted image originals.
- `images\thumbs\`: image thumbnails.
- `updates\`: downloaded update installers.
- `logs\startup.log`: startup and unhandled-exception diagnostics.

## Development

This repository is a Windows WPF app developed with .NET.

Use Windows-side `dotnet` commands when working from WSL:

```powershell
dotnet build DropShelf.sln
dotnet test DropShelf.sln --no-build
dotnet format DropShelf.sln --verify-no-changes --verbosity minimal
dotnet publish .\src\DropShelf.App\DropShelf.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64
```

Build the installer only when packaging validation is needed:

```powershell
& "D:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer\DropShelf.iss
```

Formal releases should use the signed packaging script:

```powershell
$env:EDGE_TUCK_SIGN_CERT_PATH = "C:\path\to\certificate.pfx"
$env:EDGE_TUCK_SIGN_CERT_PASSWORD = "<certificate password>"
.\scripts\package-release.ps1
```

Development notes:

- `.trellis/`, `.agents/`, `.codex/`, and `AGENTS.md` are project development
  infrastructure and should be kept.
- `artifacts/`, `installer/Output/`, `bin/`, `obj/`, `.trellis/.runtime/`, and
  `.trellis/workspace/` are generated or local runtime output.
- Do not use `git add .`; stage only files related to the current task.

## Project Documentation

- Development architecture: [docs/architecture.md](docs/architecture.md)
- Feature roadmap: [docs/feature-tasks.md](docs/feature-tasks.md)
- Feature contracts: [docs/features/README.md](docs/features/README.md)
- Packaging and release process: [docs/packaging.md](docs/packaging.md)
- Trellis project workflow: [.trellis/workflow.md](.trellis/workflow.md)
