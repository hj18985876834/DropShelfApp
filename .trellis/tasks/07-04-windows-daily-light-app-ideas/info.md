# Desktop Drop Shelf Technical Design

## Status

Draft technical route for implementation planning. Requirements live in `prd.md`; research details live under `research/`.

## Product Summary

Desktop Drop Shelf is a local-only Windows utility that gives users a small edge shelf for temporarily holding files, folders, text, URLs, and images while moving between applications.

## Technical Direction

### Stack

* Language: C#
* Runtime: .NET 10 LTS
* UI: WPF
* Architecture: MVVM-lite
* Storage: JSON files and app-owned image files under `%LOCALAPPDATA%/DropShelf/`
* Tests: .NET unit tests for persistence, settings, image storage, and item semantics

### Rationale

WPF is the pragmatic V1 choice because this product depends on native Windows desktop behaviors:

* drag and drop between apps
* tray icon behavior
* edge-positioned utility windows
* keyboard shortcuts
* local file/image persistence
* lightweight Windows-only packaging

WinUI 3 can be revisited later if visual modernization outweighs classic utility integration cost. Electron/Tauri are not preferred for V1 because the app's core value is native Windows behavior and low resource use.

## Repository Layout

Recommended initial layout:

```text
drop-shelf/
  src/
    DropShelf.App/
      App.xaml
      App.xaml.cs
      DropShelf.App.csproj
      Assets/
      Resources/
        Themes/
        Styles/
      Views/
        ShelfWindow.xaml
        SettingsWindow.xaml
      ViewModels/
        ShelfViewModel.cs
        ShelfItemViewModel.cs
        SettingsViewModel.cs
      Models/
        ShelfItem.cs
        ShelfItemType.cs
        AppSettings.cs
        DockEdge.cs
        ThemeMode.cs
        DensityMode.cs
      Services/
        AppDataPathService.cs
        ShelfStore.cs
        ImageStore.cs
        DragDropService.cs
        TrayIconService.cs
        StartupService.cs
        ThemeService.cs
        WindowDockService.cs
      Commands/
        RelayCommand.cs
      Converters/
      Interop/
        NativeMethods.cs
  tests/
    DropShelf.Tests/
  docs/
    architecture.md
    packaging.md
  README.md
```

Keep the first version as one WPF app project plus one test project. Split domain/storage into separate class libraries only when real complexity appears.

## Module Boundaries

### Views

XAML surfaces with minimal code-behind:

* `ShelfWindow`: edge handle, shelf panel, drop zone, item card list.
* `SettingsWindow`: short settings UI.

Code-behind may bridge unavoidable UI events such as drag/drop and window positioning, but business decisions should go through ViewModels/Services.

### ViewModels

UI state and commands:

* `ShelfViewModel`: item collection, selected item, drag-over state, remove/clear commands.
* `ShelfItemViewModel`: display state and per-item actions.
* `SettingsViewModel`: dock edge, theme, density, startup option.

### Models

Serializable data:

* `ShelfItem`: id, type, display name, timestamps, source path/content/image paths.
* `AppSettings`: dock edge, theme mode, density mode, start with Windows.

### Services

Platform and persistence boundaries:

* `ShelfStore`: read/write `shelf.json`.
* `ImageStore`: save app-owned image copies and thumbnails; delete them when records are removed.
* `DragDropService`: interpret dropped/pasted file, text, URL, and image formats.
* `TrayIconService`: show/hide/settings/quit.
* `StartupService`: opt-in start with Windows behavior.
* `ThemeService`: system/light/dark switching.
* `WindowDockService`: position handle and shelf by dock edge.
* `AppDataPathService`: resolve `%LOCALAPPDATA%/DropShelf/` paths.

## Local Data Layout

```text
%LOCALAPPDATA%/DropShelf/
  settings.json
  shelf.json
  images/
    originals/
    thumbs/
  logs/
```

Rules:

* File/folder items store original paths only.
* Text/URL items store local metadata/content.
* Pasted or dragged bitmap images are copied into app-owned local storage.
* Thumbnails are generated for image cards.
* Clearing records never deletes original user files.
* Removing image records deletes app-owned image copies and thumbnails.
* Missing file/folder paths show a missing state.

## UX Implementation Notes

* Use Windows-native / Fluent-inspired resources.
* Keep the shelf panel fixed-size per dock orientation.
* Support compact and comfortable card density.
* Card actions appear on hover.
* Use single-card selection for keyboard operations.
* Empty state is a simple drop zone with concise copy.
* Avoid decorative UI, heavy gradients, and long animations.

## Build / Test / Publish

Expected development commands:

```powershell
dotnet build
dotnet test
dotnet publish -c Release -r win-x64 --self-contained true
```

Initial MVP can be shared as a self-contained publish folder or zip. A polished installer can follow after MVP behavior is stable.

## Development Environment Strategy

### Current Environment Finding

The active shell is WSL2 Ubuntu:

```text
Linux ... microsoft-standard-WSL2 ... x86_64 GNU/Linux
```

Current tool availability:

* WSL Ubuntu: `dotnet` is not installed.
* Windows host: .NET 10 SDK is installed (`10.0.301`).
* Windows host: Visual Studio 2022 Community with `.NET desktop development` workload is installed at `D:\Program Files\Microsoft Visual Studio\2022\Community`.
* Windows host: Inno Setup 6 command-line compiler is installed at `D:\Program Files (x86)\Inno Setup 6\ISCC.exe`.

### Impact

This matters because WPF is a Windows desktop framework. WSL/Linux can be used for planning, file edits, and generic scripting, but WPF build/run/debug, tray behavior, drag/drop, startup registration, and installer validation must happen on the Windows side.

### Recommended Workflow

Use a two-environment workflow:

* WSL side:
  * Trellis task management.
  * Documentation and planning.
  * Source editing if the repo is located in the WSL filesystem.
  * Non-Windows-only text/file operations.
* Windows side:
  * Install .NET SDK.
  * Build WPF app.
  * Run WPF app.
  * Run `dotnet test` for the WPF solution.
  * Test drag/drop, tray icon, startup setting, installer, and UI behavior.
  * Run Inno Setup compiler.

### Required Windows Tooling

Before implementation/build:

* .NET 10 SDK on Windows is available.
* Visual Studio 2022 Community with ".NET desktop development" workload is available.
* Inno Setup is available for installer creation.
* Ensure these are callable from Windows PowerShell:

```powershell
dotnet --info
dotnet build
dotnet test
& "D:\Program Files (x86)\Inno Setup 6\ISCC.exe" /?
```

### Command Boundary

From WSL, Windows tools can be invoked through `powershell.exe`, but WPF-specific work should still be considered Windows-side execution:

```bash
powershell.exe -NoProfile -Command "dotnet build"
powershell.exe -NoProfile -Command "dotnet test"
```

This is useful for automation, but manual UI testing still requires interacting with the Windows desktop.

### Repository Location Guidance

Preferred for this project:

* Keep the active WPF repository in the Windows filesystem if Visual Studio / Windows build tools are the primary daily tools.
* If source lives in WSL, be careful with path translation and file watching behavior when opening from Windows tools.

Pragmatic recommendation:

* Put the actual WPF project under a Windows-accessible path used comfortably by Visual Studio.
* Keep Trellis planning artifacts in this project until implementation location is chosen.

### Environment Setup Checklist

* [x] Windows has .NET 10 SDK installed.
* [x] `dotnet --info` on Windows shows .NET SDK `10.0.301`.
* [x] Visual Studio 2022 Community with `.NET desktop development` workload is installed.
* [x] WPF project can build from Windows PowerShell.
* [x] Test project can run from Windows PowerShell.
* [x] App can launch on Windows desktop.
* [x] Inno Setup `ISCC.exe` is installed and callable via `D:\Program Files (x86)\Inno Setup 6\ISCC.exe`.
* [ ] Installer can be built on Windows.
* [ ] Manual UI checklist is run on Windows, not WSL.

### References

* WPF documentation: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
* WPF overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
* .NET install on Windows: https://learn.microsoft.com/en-us/dotnet/core/install/windows
* .NET releases and support: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

## Implementation Milestones

Detailed branch-level feature specs live in `docs/features/`. Each feature branch should use the relevant feature spec as its implementation contract.

1. App shell
   * WPF scaffold
   * tray icon
   * edge handle
   * settings defaults

2. File/folder shelf
   * item model
   * JSON persistence
   * file/folder drag-in
   * card UI
   * open/reveal/remove/clear

3. Drag-out and keyboard
   * drag-out copy behavior
   * selected card state
   * `Delete`, `Ctrl+C`, `Enter`, `Escape`

4. Text/URL/image support
   * paste text/URL
   * paste/drag image
   * app-owned image copy
   * thumbnails

5. UX and release polish
   * theme modes
   * density modes
   * dock edge switching
   * startup toggle
   * missing-file state
   * publish artifact

## Initial Quality Bar

* Persistence and settings behavior covered by unit tests.
* Image cleanup behavior covered by unit tests where practical.
* Manual test checklist covers drag-in, drag-out, restart persistence, theme, density, dock edge, tray, and startup setting.
* No feature should delete or move original user files without explicit future product decision.

## Git / Parallel Work Rules

This project may have multiple feature points developed in parallel. Each session/task must keep Git operations scoped to the files it owns.

### Core rules

* Check dirty state before editing and before committing.
* Only stage files intentionally changed by the current task/session.
* Do not stage unrelated dirty files.
* Do not revert, checkout, reset, or overwrite files changed by another task/session.
* Do not use broad `git add .` for task commits.
* Prefer explicit paths:

```bash
git add src/DropShelf.App/Services/ShelfStore.cs tests/DropShelf.Tests/ShelfStoreTests.cs
```

### When unrelated files are dirty

If dirty files are unrelated to the current task:

* Leave them alone.
* Mention them separately in the commit plan.
* Do not include them in the task commit.

### When the same file is being modified

If the current task needs to edit a file that already has unrelated changes:

1. Inspect the existing diff first.
2. Determine whether changes are compatible.
3. Edit around existing changes without reverting them.
4. If both tasks need the same lines or behavior, pause and coordinate before proceeding.
5. Commit only the portion that belongs to the current task when possible.

### Commit planning

Before committing:

* Run `git status --short`.
* Review `git diff` for every file that will be staged.
* Prepare a scoped commit plan listing files included and unrelated dirty files excluded.
* Commit one coherent task unit at a time.

### Branching recommendation

For parallel feature work, prefer one branch per feature/task:

```text
feature/app-shell
feature/file-shelf
feature/image-support
feature/installer
```

If multiple sessions must work on the same branch, commits must stay narrowly scoped by file and feature.

## Testing Strategy

### Test Framework

Use MSTest for V1.

Rationale:

* Official Microsoft .NET test framework.
* Works with `dotnet test`, Visual Studio, VS Code, and Rider.
* Fits the Microsoft-native WPF/.NET stack.
* Avoids debating third-party test framework style during MVP.

Reference commands:

```powershell
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

References:

* https://learn.microsoft.com/en-us/dotnet/core/testing/
* https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
* https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage

### Unit Tests

Unit tests should cover logic that can run without WPF windows:

* `ShelfStore`
  * creates missing data directory
  * saves and loads shelf metadata
  * preserves item order
  * handles malformed or missing `shelf.json` safely
* `AppSettings`
  * default values
  * load/save round trip
  * invalid setting fallback
* `ImageStore`
  * saves pasted image data into app-owned folder
  * creates thumbnail path
  * deletes image and thumbnail on record removal
  * ignores missing files during cleanup
* `ShelfItem` semantics
  * file/folder item stores path reference only
  * text/URL item stores local content
  * image item stores app-owned paths
* `StartupService`
  * writes/removes registry value through an abstracted registry adapter
  * does not require admin path assumptions
* `ThemeService` / settings mapping
  * system/light/dark setting maps to expected app state
* ViewModels
  * remove item
  * clear all
  * selected item commands
  * command enabled/disabled states

### Integration / File-System Tests

Use temporary directories to test real file behavior without touching user data:

* shelf metadata writes to a temp app data root
* image files and thumbnails are created under temp app data root
* removing image items deletes app-owned files
* clearing file-reference items does not delete original temp source files
* missing file paths are detected and surfaced as missing state

Do not run tests against real `%LOCALAPPDATA%/DropShelf/`.

### Manual Functional Test Checklist

Run this checklist during each milestone and before sharing an installer:

#### App shell

* App launches without admin privileges.
* Tray icon appears.
* Tray show/hide works.
* Tray quit exits cleanly.
* Edge handle appears on default right edge.
* Shelf expands/collapses from handle.

#### Docking

* Left, right, top, bottom dock positions apply correctly.
* Dock position persists after restart.
* Shelf does not cover the taskbar in common layouts.
* Behavior is acceptable on different DPI scaling values.

#### Drag / paste input

* Drag file from Explorer into shelf.
* Drag folder from Explorer into shelf.
* Paste plain text into shelf.
* Paste URL into shelf.
* Paste image from clipboard into shelf.
* Unsupported drop gives brief feedback and does not crash.

#### Shelf actions

* Drag file item out to Explorer/Desktop; original file remains.
* Copy file path from file item.
* Open file/folder item.
* Reveal file/folder in Explorer.
* Copy text/URL content.
* Open URL item.
* Copy image item or drag image out if implemented for that milestone.
* Remove single item.
* Clear all.

#### Persistence

* Shelf items persist after app restart.
* File/folder references still point to original paths.
* Image records reload thumbnails after restart.
* Missing source file shows missing state.
* Removing missing item works.

#### UX / UI

* Empty drop zone is clear and minimal.
* Card hover actions appear and do not shift layout.
* Long filenames truncate cleanly.
* Theme follows system by default.
* Manual light/dark setting works.
* Compact/comfortable density setting works.
* Keyboard: `Escape`, `Delete`, `Ctrl+C`, `Enter`.

#### Installer

* Installer runs without admin prompt.
* App installs for current user.
* Start Menu shortcut works.
* Uninstaller appears.
* Uninstall removes app files.
* Uninstall preserves local user data unless future option says otherwise.
* Reinstall can read existing local data.

### UI Automation

Automated WPF UI tests are not required for V1.

Reasoning:

* The highest-risk logic can be covered through services and ViewModels.
* Drag-and-drop UI automation is often brittle and expensive relative to this MVP.
* Manual checklist gives better signal early.

Future option:

* Add Windows UI Automation / Appium-style tests after the UI stabilizes and regressions become costly.

References:

* https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-overview
* https://learn.microsoft.com/en-us/archive/msdn-magazine/2009/march/test-run-automating-ui-tests-in-wpf-applications

### Development Testing Workflow

For each milestone:

1. Write or update unit tests for changed services/ViewModels.
2. Run `dotnet test`.
3. Run the relevant manual checklist section.
4. Fix failures before moving to the next milestone.

Before producing an installer:

1. Run `dotnet test`.
2. Run full manual checklist.
3. Publish release build.
4. Build installer.
5. Install on a clean Windows user profile if available.
6. Run installer checklist.

### Incremental Windows Validation

Do not wait until the full app is finished before testing on Windows.

Because this is a WPF desktop utility, each feature slice should be validated on Windows as soon as it is minimally runnable. The expected development loop is:

1. Implement a small feature slice.
2. Run unit tests.
3. Build on Windows.
4. Launch the app on Windows.
5. Manually verify only the affected UI/desktop behavior.
6. Fix immediately before starting the next slice.

Recommended validation cadence:

* After app shell: build/run, tray icon, edge handle.
* After docking: left/right/top/bottom placement, restart persistence.
* After file/folder drag-in: Explorer drag to shelf, card display, persistence.
* After actions: open, reveal, copy, remove, clear.
* After drag-out: copy behavior into Explorer/Desktop.
* After text/URL: paste/copy/open.
* After image support: paste image, thumbnail, restart reload, delete cleanup.
* After theme/density/settings: setting changes and restart persistence.
* After startup toggle: registry entry write/remove.
* After installer: install/uninstall/reinstall checks.

This reduces risk because drag/drop, tray, window positioning, DPI, taskbar, and installer behavior are hard to validate purely from code review or Linux-side commands.

## Distribution Decision

V1 distribution target: traditional Windows installer.

Recommended packaging tool: Inno Setup.

Rationale:

* Produces a familiar `setup.exe` flow for non-technical users.
* Easier to configure than WiX/MSI for a small desktop utility.
* Better fit than MSIX for an early MVP that may still change packaging details.
* Can create Start Menu/Desktop shortcuts and uninstall entries.
* Works well with a self-contained `dotnet publish` output.

Packaging flow:

1. Publish the WPF app as a Windows x64 self-contained release.
2. Feed the publish directory into an Inno Setup script.
3. Generate a signed or unsigned `DropShelfSetup.exe` depending on release stage.
4. For early sharing, clearly label unsigned builds as test builds.

Expected commands:

```powershell
dotnet publish .\src\DropShelf.App\DropShelf.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64
iscc .\installer\DropShelf.iss
```

Installer responsibilities:

* Install app files as a per-user install, preferably under a user-local app install directory.
* Create Start Menu shortcut.
* Optionally create Desktop shortcut.
* Register uninstall entry.
* Do not enable start-with-Windows by default.
* Preserve `%LOCALAPPDATA%/DropShelf/` user data during uninstall unless a future uninstall option explicitly asks to remove it.

Future packaging options:

* WiX/MSI if enterprise-style deployment becomes important.
* MSIX / Microsoft Store after MVP behavior and identity are stable.

## Open Technical Decisions

* Final user confirmation before moving from planning to implementation.

## Install Scope Decision

* V1 installer scope: current user only.
* Installer should not require administrator privileges for normal install.
* Prefer a user-local install directory such as `%LOCALAPPDATA%/Programs/DropShelf/`.
* Per-machine / all-users install is out of scope for V1.

## Startup Decision

* V1 start-with-Windows implementation: current user Registry `Run` key.
* The app setting is default off.
* Enabling the setting writes a `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry for DropShelf.
* Disabling the setting removes that entry.
* Startup registration is managed by the app setting, not forced by the installer.

## MVVM Dependency Decision

* V1 does not introduce a third-party MVVM framework.
* Implement small in-house MVVM primitives:
  * `ObservableObject`
  * `RelayCommand`
  * optional async command only if needed
* Revisit `CommunityToolkit.Mvvm` only if ViewModel boilerplate becomes a real maintenance problem after MVP.
