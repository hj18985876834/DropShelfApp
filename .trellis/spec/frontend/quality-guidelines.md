# Quality Guidelines

> Code quality standards for frontend development.

---

## Overview

DropShelf is a WPF desktop utility. Quality checks must cover both C# correctness and Windows desktop behavior.

---

## Forbidden Patterns

* Do not use broad `git add .` for task commits.
* Do not proactively fix issues outside the current user-approved development
  scope. If an unrelated build failure, dirty diff, historical bug, or broken
  test blocks verification, report the blocker and wait for user direction.
* Do not commit unrelated dirty files from another feature/session.
* Do not run `git commit` proactively. Recommend a commit only after the user
  has validated the executable and the intended commit scope is clear; commit
  only after the user explicitly approves that commit.
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
* Keep the root `README.md` and `README.zh-CN.md` synchronized with
  user-visible installation, settings, update, uninstall, and data-location
  behavior. Feature specs under `docs/features/` are development contracts;
  README files are the user-facing entry points.

---

## Documentation Sync Rules

Documentation updates are part of the feature or bug-fix scope when behavior is
visible to users, operators, or future development sessions.

Update both `README.md` and `README.zh-CN.md` when changing:

* installation or uninstall behavior;
* startup-with-Windows behavior or registry keys;
* settings fields, labels, defaults, or persistence locations;
* update-check, download, installer launch, release manifest, or checksum flow;
* supported input/output types, drag/drop behavior, shortcuts, tray actions, or
  item actions;
* local data paths, app-owned image storage, logs, or cleanup expectations;
* development validation commands, fixed publish paths, or packaging commands.

Update `docs/packaging.md` when changing installer, publish, GitHub Release,
update manifest, release asset, versioning, SHA256, or smoke-test behavior.

Update the relevant `docs/features/*.md` contract when changing acceptance
criteria, user workflows, validation matrices, or ownership boundaries for a
feature branch.

Before completing a task, check documentation freshness:

1. Did this change affect anything a user can see or follow?
2. Did it change an install/update/uninstall command, path, registry key, or
   release artifact?
3. Did it change local data shape, storage location, retention, or cleanup?
4. Did it change development validation or packaging steps?
5. If any answer is yes, update the relevant docs in the same change and keep
   English and Chinese README content equivalent.

Do not mark a user-visible change complete if the documentation still describes
the previous behavior.

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

### Formal Release Verification

Formal releases must close the source, manifest, GitHub Release, and user
update loop before the release is treated as complete.

* Keep app assembly version, Inno Setup `MyAppVersion`, release tag, and
  `updates/latest.json` `version` aligned.
* Keep the GitHub repository default branch on `main`; release manifests are
  fetched from `main`, and a different default branch makes GitHub UI and
  release operations easy to target incorrectly.
* Build the installer from clean generated output, then record the exact
  `EdgeTuckSetup.exe` byte size and SHA256 before editing the manifest.
* Publish the release with GitHub CLI when available:
  `gh release create v<version> installer/Output/EdgeTuckSetup.exe --repo hj18985876834/DropShelfApp --target <commit> --title "EdgeTuck <version>" --notes "<notes>"`.
* Verify `origin/main`, `v<version>`, and the GitHub Release target all point to
  the intended release commit. Annotated tags should be resolved with
  `git ls-remote --tags origin "v<version>" "v<version>^{}"`.
* Verify GitHub reports `main` as the default branch:
  `gh repo view hj18985876834/DropShelfApp --json defaultBranchRef --jq '.defaultBranchRef.name'`.
  After changing the remote default branch, run `git remote set-head origin -a`
  and confirm `git remote show origin` reports `HEAD branch: main`.
* Verify the GitHub Release asset is named `EdgeTuckSetup.exe`, its asset size
  equals `updates/latest.json` `sizeBytes`, and the manifest installer URL uses
  `/releases/download/v<version>/EdgeTuckSetup.exe`.
* Verify the raw manifest URL on `main` returns the committed manifest before
  asking users to update.
* Prefer downloading the published asset and checking SHA256. If GitHub download
  speed is too slow to finish, record that limitation and fall back to GitHub
  Release asset metadata plus the local uploaded file SHA256; then require a
  real user-side update/install success before calling the release complete.
* After the user confirms they successfully obtained and installed the latest
  version through the update flow, capture that result in the session summary or
  release notes for future troubleshooting.

### Git And Commit Scope

* Use feature branches for parallel work. Expect multiple sessions to modify
  different feature areas at the same time.
* Never use broad `git add .`.
* Stage only the files directly related to the current task or bug fix.
* Treat unrelated tracked or untracked files as another session's work unless
  the user says otherwise.
* If verification exposes an unrelated failure, do not patch it as part of the
  current task. State the exact command and failure, explain that it is outside
  the current scope, and pause for user approval before touching those files.
* Do not commit implementation changes until the user has manually validated the
  executable and explicitly approves the commit.
* Before committing, provide the proposed commit scope and message. Run
  `git commit` only after the user explicitly agrees to that proposal.
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

### WPF Localized Option Controls

For localized `ComboBox` options, do not bind directly to enum values and rely
on `ItemTemplate` converters for display text. WPF can update the drop-down
items while leaving the closed selection box cached, or it can briefly lose the
current `SelectedValue` when `ItemsSource` is replaced during a language switch.

Use stable option objects instead:

```csharp
public sealed class LocalizedOption<T> : ObservableObject
{
    public T Value { get; }
    public string DisplayName { get; set; }
}
```

Bind with `DisplayMemberPath="DisplayName"`, `SelectedValuePath="Value"`, and
`SelectedValue` to the persisted enum property. On language changes, keep the
same option object instances and update their `DisplayName` values so both the
drop-down list and the closed selection box refresh without clearing selection.

### WPF Density Resources

Density-sensitive WPF layout values should be application resources applied by
`ThemeService`, with matching default keys in `App.xaml`. Do not leave shelf
card height, card padding, card gaps, badge size, or drop-zone padding as local
hard-coded XAML values when they are part of the compact/comfortable density
experience.

Use `DynamicResource` for density values in XAML so settings changes can refresh
the open shelf immediately after `ThemeService.Apply(...)` updates
`Application.Resources`.

```xml
<Setter Property="MinHeight" Value="{DynamicResource DropShelfCardMinHeight}" />
<Border Padding="{DynamicResource DropShelfCardPadding}" />
```

### WPF Instant Shell Settings

Shell controls that immediately change persisted behavior, such as a header
pin toggle, should expose state on the relevant ViewModel and pass a save
callback into the App layer. Do not write settings directly from Views.

### WPF Interaction Animations

For runtime interaction animations on item templates, animate concrete WPF
objects rather than deep, unnamed `Storyboard.TargetProperty` paths.

Avoid paths such as:

```xml
Storyboard.TargetProperty="(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"
```

These can compile but fail at runtime with `XamlParseException` when WPF cannot
resolve a dependency object in the path while materializing a virtualized item.
Use one of these stable patterns instead:

* Give the animated `ScaleTransform`, `TranslateTransform`, or `Effect` a
  template-local name and target it with `Storyboard.TargetName`.
* Or find the realized item container in code-behind and call `BeginAnimation`
  on the concrete `ScaleTransform`, `TranslateTransform`, or
  `DropShadowEffect` instance.
* Before animating `Transform` or `Effect` instances that came from a `Style`
  setter or resource, check `IsFrozen`. WPF can freeze shared `Freezable`
  objects. Replace frozen transforms/effects with per-card instances before
  calling `BeginAnimation`.

For drag reorder lift/drop effects, do not animate the realized `ListBoxItem` as
the dragged object. A virtualized/recycling `ListBox` can reuse containers while
auto-scroll changes the visible range, which makes real-item lift/drop effects
fragile. Use a top-level non-hit-testable overlay card for the drag preview and
leave the real list item as a semi-transparent placeholder. The overlay follows
the pointer in window coordinates and is independent from list scrolling,
virtualization, and container reuse. Clamp the overlay to the list viewport and
size it from the scroll viewer viewport width, not arbitrary text content, so
long text cannot make the preview wider than neighboring cards. Target-card
detection should fall back to vertical range matching when direct hit-testing
does not return a card, because horizontal width differences and scroll viewer
chrome should not prevent reorder.

After changing reorder animations, publish and launch the Windows-local
validation executable and validate the drag gesture itself; build and
startup-only checks are not sufficient because invalid storyboard paths, frozen
`Freezable` instances, or list virtualization effects can fail only when the
item template is realized and dragged.

Drag reorder in a scrollable shelf must support edge auto-scroll. Keep the
latest pointer position in list coordinates, run a short dispatcher timer while
the reorder gesture is active, and scroll when the pointer enters the top or
bottom edge zone. Scroll speed should increase as the pointer approaches the
edge. Keep auto-scroll conservative: do not synchronously force a reorder on
every scroll tick, because scrolling changes the item under the pointer and can
create a scroll/reorder/layout feedback loop. Reorder on pointer movement and,
after auto-scroll, queue at most one dispatcher callback after layout settles to
try a reorder at the current pointer position. Only reorder after the pointer
crosses the target card's midpoint band. Stop auto-scroll and clear pending
auto-scroll reorder callbacks on every reorder cleanup path: mouse up, lost
capture, cancelled drag, or left button no longer pressed.

When a shell setting can change while `SettingsWindow` is open, merge that
state in `App.ApplySettings(...)` before saving or applying the settings page
snapshot. This prevents an older settings-page ViewModel from overwriting a
newer shell toggle value.

Required coverage:

* ViewModel tests for toggle state, localized tooltip text, and callback
  notification.
* Settings persistence tests that round-trip the new field and preserve it
  through unrelated settings saves.
* App-shell behavior review for explicit manual hide paths versus automatic
  hover/timer collapse paths.

### WPF Shelf Card Management

Shelf filtering must be a visible projection over the persisted card collection.
Keep the full `ShelfViewModel.Items` collection as the source of truth for
persistence, item count, cleanup, and manual order. Bind the card list to a
separate visible projection such as `VisibleItems`, and keep
`GetShelfItems()` returning the full backing collection.

Do not remove, save, or reorder only the visible projection. Any operation that
changes shelf records or order must update the backing `Items` collection so the
existing `Items.CollectionChanged -> QueueShelfSave() -> GetShelfItems()` path
persists the correct data.

When adding card gestures, keep drag-out copy and internal reorder gestures
separate:

* Dragging the card body starts drag-out copy.
* Dragging the type badge/reorder handle starts internal reorder.
* Internal reorder should show the dragged card as lifted and move cards
  dynamically while dragging, not only on mouse-up, unless a later task
  explicitly chooses a placeholder-only preview.
* Click-to-expand for text cards must only run when movement stays below the
  drag threshold.

Required coverage:

* ViewModel tests that filtering leaves `GetShelfItems()` unchanged.
* ViewModel tests that reorder changes the backing collection order.
* Manual Windows validation for reorder versus drag-out and text expansion.

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
