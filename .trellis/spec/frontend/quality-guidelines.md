# Quality Guidelines

> Code quality standards for frontend development.

---

## Overview

DropShelf is a WPF desktop utility. Quality checks must cover both C# correctness and Windows desktop behavior.

---

## Forbidden Patterns

* Do not use broad `git add .` for task commits.
* Do not commit unrelated dirty files from another feature/session.
* Do not move or delete original user files from shelf clear/remove flows.
* Do not add cloud sync, accounts, telemetry, or external APIs in V1.
* Do not put persistence, registry, tray, or file-system side effects directly in Views.
* Do not leave Windows desktop behavior untested until the end of a feature.

---

## Required Patterns

* Run Windows-side `dotnet build` and `dotnet test` after each runnable feature slice.
* Run `dotnet format --verify-no-changes`; treat analyzer warnings as quality
  failures because this command returns non-zero for project analyzers.
* Keep file/folder shelf records as original path references.
* Store pathless/pasted images under app-owned local data.
* Use services for side-effect boundaries.
* Keep WPF UI styling quiet and Windows-native / Fluent-inspired.
* Validate actual desktop behavior incrementally on Windows, especially drag/drop, tray, window docking, startup, and installer behavior.

---

## Development Workflow Rules

The app targets Windows, but development may run from WSL. Use Windows-side
tools through PowerShell for build, test, publish, and manual validation:

```powershell
dotnet build DropShelf.sln
dotnet test DropShelf.sln --no-build
dotnet format DropShelf.sln --verify-no-changes --verbosity minimal
dotnet publish .\src\DropShelf.App\DropShelf.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64
```

### Publishing For Validation

* Publish validation executables only to the WSL artifact directory
  `artifacts/publish/win-x64`.
* Do not ask users to run WPF validation executables directly from WSL UNC paths
  such as `\\wsl.localhost\Ubuntu\...`. WPF can fail during window creation with
  `System.DllNotFoundException: Unable to load DLL 'PenIMC_cor3.dll'` from that
  network path even when publish succeeded.
* After publishing from WSL, copy the complete `artifacts/publish/win-x64`
  folder to this fixed Windows-local validation directory before launch:
  `C:\Users\HJ\Desktop\DropShelf-validate`.
* The validation executable path for this workstation is always:
  `C:\Users\HJ\Desktop\DropShelf-validate\DropShelf.App.exe`.
* Do not create alternate validation folders, timestamped folders, feature
  suffixes, or temporary publish directories such as
  `artifacts/publish/win-x64-<feature>` or
  `C:\Users\HJ\Desktop\DropShelf-validate-<suffix>` unless the user explicitly
  asks for a separate copy.
* Before replacing the fixed validation directory, stop only `DropShelf.App`
  processes whose executable `Path` starts with
  `C:\Users\HJ\Desktop\DropShelf-validate\`. If Windows denies termination or
  the path is unavailable, report the blocking process and ask the user to close
  it manually instead of publishing to a different folder.
* Do not build installers during normal feature validation. Only build an
  installer when the user explicitly asks for packaging/install testing.
* After publishing, provide the Windows-local executable path for manual
  validation, not the WSL UNC path.

### Git And Commit Scope

* Use feature branches for parallel work. Expect multiple sessions to modify
  different feature areas at the same time.
* Never use broad `git add .`.
* Stage only the files directly related to the current task or bug fix.
* Treat unrelated tracked or untracked files as another session's work unless
  the user says otherwise.
* Do not commit implementation changes until the user has manually validated the
  executable and explicitly approves the commit.
* If two sessions need the same file, make the smallest possible edit and call
  out the overlap before committing.

### Bug-Fix Validation Loop

For UI stability bugs, do not stop at a code-level explanation:

* Reproduce or inspect the affected event/command path.
* Add or update focused unit tests for the non-visual logic.
* Run Windows-side build, test, and format checks.
* Publish the fixed executable to the fixed validation directory.
* Ask the user to validate the exact workflow that failed.
* Capture any learned convention in `.trellis/spec/` before the final commit.

### UI Stability Debugging Method

For WPF shell bugs involving dragging, docking, hover, focus, or multiple
windows, classify the failure before patching:

* Coordinate-space issue: local element coordinates, screen physical pixels, and
  WPF device-independent pixels are being mixed.
* Window-boundary issue: the draggable target changes size, position, visibility,
  or window ownership during the gesture.
* Event-state issue: `MouseEnter`, `MouseLeave`, `DragOver`, `Click`, and
  synthetic post-drag clicks are sharing the same state transition.
* Persistence issue: the visible state changed but the durable settings contract
  was not updated or normalized.

Apply fixes in this order:

1. Stabilize the interaction boundary first, such as separating a handle window
   from an expanded panel window.
2. Make state transitions idempotent before adding timers or thresholds.
3. Convert coordinates at the boundary where values cross from WPF visual space
   to window placement.
4. Move snap, persistence, and expensive layout recalculation to gesture end
   unless the feature explicitly requires live preview.
5. Add unit tests for pure placement/state logic and publish a Windows-local
   validation build for the real desktop behavior.

Do not accept workaround fixes such as corner margins, arbitrary cursor offsets,
or repeated live snapping if the real issue is an unstable interaction boundary.

---

## Testing Requirements

Use MSTest for V1.

Unit-test:

* settings defaults and persistence
* shelf metadata persistence
* image store path/cleanup behavior
* ViewModel commands and selection behavior

For MSTest 4, use analyzer-preferred assertions so `dotnet format
--verify-no-changes` stays green:

* Use `Assert.IsEmpty(collection)` instead of `Assert.AreEqual(0, collection.Count)`.
* Use `Assert.HasCount(expected, collection)` instead of count equality asserts.
* Use `[TestMethod]` with `[DataRow]` for data-driven tests; do not use obsolete
  `[DataTestMethod]`.

Manual Windows validation is required for:

* app launch
* tray icon
* edge handle
* drag in
* drag out
* theme/density switching
* startup setting
* installer install/uninstall

---

## Code Review Checklist

* Does the change stay scoped to the current feature branch/task?
* Are unrelated dirty files excluded?
* Are service boundaries preserved?
* Are original files protected from accidental delete/move behavior?
* Do tests cover changed non-UI logic?
* Was the relevant Windows manual check run for UI/desktop behavior?
* Does the UI remain compact, quiet, and predictable?
