# Feature Spec: App Shell

## Goal

Create the always-available desktop shell for DropShelf: tray icon, screen-edge handle, shelf window show/hide behavior, and a settings entry point.

## Branch

`feature/app-shell`

## Dependencies

* Project skeleton exists.
* WPF app launches successfully.

## User Flow

1. User starts DropShelf.
2. A small edge handle appears on the default right screen edge.
3. User clicks the handle.
4. The shelf panel appears.
5. User clicks the handle or close/collapse action again.
6. The shelf panel hides while the handle remains available.
7. User can show, hide, open settings, or quit from the tray icon.

## Detailed Behavior

### Startup

* App starts without requiring administrator privileges.
* App creates and owns exactly one shelf shell instance for the primary session.
* First-run handle position uses `DockEdge.Right`.
* The app should not create shelf records or app data files unless needed.

### Edge Handle

* The handle is a compact window or surface attached to the selected screen edge.
* V1 default is right edge.
* It should be always visible enough to discover but low-distraction.
* It should stay above normal windows where practical, without stealing focus aggressively.
* Clicking the handle toggles the shelf panel.
* Hover may show a subtle visual state and tooltip.

### Shelf Panel

* Panel appears near the handle and uses the current dock edge.
* Panel can be hidden without exiting the app.
* `Escape` hides/collapses the panel when focused.
* The initial panel content may be the empty drop zone until shelf item work is implemented.

### Tray Icon

Tray menu items:

* Show Shelf
* Hide Shelf
* Settings
* Quit

Rules:

* Show/Hide should reflect current visible state when practical.
* Quit should shut down the app cleanly.
* Settings opens `SettingsWindow`; settings fields can remain placeholder until the Settings feature.

## UI States

* Handle idle
* Handle hover
* Shelf hidden
* Shelf visible
* Settings window opened from tray or shelf button
* App exiting from tray

## Data Contract

No persisted shelf data is required for this feature. If settings infrastructure is touched, use `AppSettings.CreateDefault()` and do not invent duplicate defaults.

## Edge Cases

* Launch app twice: V1 should avoid obviously confusing duplicate windows where practical. A full single-instance guard can be deferred if needed.
* Taskbar is on right edge: handle should still be visible; exact taskbar avoidance can be improved in Settings/Docking polish.
* Multi-monitor layouts: V1 may attach to primary display unless `WindowDockService` already supports monitor selection.
* Tray icon creation failure: app should still show the window if possible, but log or surface the limitation later.

## Acceptance Criteria

* App starts and shows an edge handle.
* Clicking the handle shows/hides the shelf panel.
* Tray icon appears.
* Tray menu can show shelf, hide shelf, open settings, and quit.
* App exits cleanly from tray Quit.
* No admin prompt is required.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.

## Tests

### Unit Tests

* ViewModel or shell state tests if state is separated from WPF windows.
* Tray command routing can be unit-tested if implemented behind an interface.

### Manual Windows Tests

* Launch app from Windows PowerShell.
* Confirm handle appears.
* Click handle to show/hide shelf.
* Open settings from tray.
* Quit from tray.
* Relaunch app after quit.

## Files Likely Touched

* `src/DropShelf.App/App.xaml.cs`
* `src/DropShelf.App/Views/ShelfWindow.xaml`
* `src/DropShelf.App/Views/ShelfWindow.xaml.cs`
* `src/DropShelf.App/Views/SettingsWindow.xaml`
* `src/DropShelf.App/Services/TrayIconService.cs`
* `src/DropShelf.App/Services/WindowDockService.cs`
* `src/DropShelf.App/ViewModels/ShelfViewModel.cs`

## Out Of Scope

* Full settings persistence.
* Drag/drop item creation.
* Startup registration.
* Installer behavior.
