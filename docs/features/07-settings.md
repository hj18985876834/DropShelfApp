# Feature Spec: Settings

## Goal

Provide a short settings window for user-controlled MVP preferences: dock edge, theme, density, and start with Windows.

## Branch

`feature/settings`

## Dependencies

* App Shell.
* Shelf Persistence.
* UX Polish is useful but not required.

## User Flow

1. User opens Settings from tray or shelf.
2. User changes dock edge.
3. Shelf moves to selected edge.
4. User changes theme mode.
5. App applies theme.
6. User changes density.
7. Card density changes.
8. User toggles start with Windows.
9. Setting persists after restart.

## Detailed Behavior

### Settings Fields

Dock edge:

* Left
* Right
* Top
* Bottom

Theme mode:

* System
* Light
* Dark

Density:

* Compact
* Comfortable

Start with Windows:

* Boolean
* Default off

### Apply Behavior

* Settings save locally.
* Dock edge should apply without requiring restart if practical.
* Theme should apply without requiring restart if practical.
* Density should apply without requiring restart.
* Startup toggle writes/removes registry entry through `StartupService`.

### Startup Registration

Use:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Rules:

* Enabling writes DropShelf entry for current user.
* Disabling removes that entry.
* Installer does not force startup.
* No admin privilege required.

## UI States

* Settings window opened
* Changed setting saved
* Startup toggle success
* Startup toggle failure
* Invalid/unavailable registry write feedback, if failure occurs

## Data Contract

`AppSettings` fields:

```text
DockEdge DockEdge
ThemeMode ThemeMode
DensityMode DensityMode
bool StartWithWindows
```

Defaults must match `AppSettings.CreateDefault()`.

## Edge Cases

* Settings file missing.
* Settings file malformed.
* Registry write denied.
* App executable path changes after reinstall.
* User manually deletes registry entry while setting says enabled.
* Theme system setting changes while app is running.

## Acceptance Criteria

* Settings window opens from tray or shelf.
* Dock edge changes persist.
* Theme mode changes persist.
* Density changes persist.
* Start with Windows writes/removes HKCU Run key.
* Defaults match PRD.
* No admin prompt is required.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.

## Tests

### Unit Tests

* Settings defaults.
* Settings save/load round-trip.
* Invalid settings fallback.
* StartupService writes/removes using an abstracted registry adapter.
* Theme/density mapping tests if logic is not purely XAML.

### Manual Windows Tests

* Change dock edge and restart.
* Change theme and restart.
* Change density and restart.
* Toggle start with Windows on/off and verify registry key.
* Confirm no admin prompt.

## Files Likely Touched

* `src/DropShelf.App/Views/SettingsWindow.xaml`
* `src/DropShelf.App/ViewModels/SettingsViewModel.cs`
* `src/DropShelf.App/Models/AppSettings.cs`
* `src/DropShelf.App/Services/StartupService.cs`
* `src/DropShelf.App/Services/ThemeService.cs`
* `src/DropShelf.App/Services/WindowDockService.cs`
* new `src/DropShelf.App/Services/SettingsStore.cs`
* `tests/DropShelf.Tests/*`

## Out Of Scope

* Per-monitor custom settings.
* Custom colors.
* Freeform panel resizing.
* Startup enabled by default.
* Installer-managed startup.
