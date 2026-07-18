# DropShelf Feature Task List

Detailed implementation contracts live in [docs/features/](features/README.md). Use this file as the high-level roadmap only.

## Branching Plan

Use focused branches for parallel work:

* `feature/project-skeleton`
* `feature/app-shell`
* `feature/shelf-persistence`
* `feature/file-folder-flow`
* `feature/drag-out-copy`
* `feature/text-url-image-input`
* `feature/settings-theme-density`
* `feature/startup-installer`

## MVP Tasks

### 1. Project Skeleton

* Create WPF solution, app project, and MSTest project.
* Add initial MVVM-lite primitives.
* Add baseline models, services, and docs.
* Verify `dotnet build` and `dotnet test`.

### 2. App Shell

* Tray icon service.
* Edge handle window.
* Shelf panel show/hide behavior.
* Basic settings window entry.
* Detailed spec: [01-app-shell.md](features/01-app-shell.md)

### 3. Shelf Persistence

* `ShelfStore` JSON persistence.
* `AppSettings` persistence.
* Local app data path handling.
* Missing/corrupt data fallback.
* Detailed spec: [02-shelf-persistence.md](features/02-shelf-persistence.md)

### 4. File / Folder Flow

* Drag files/folders into shelf.
* Store path references only.
* Render file/folder cards.
* Open, reveal, copy path, remove, clear.
* Detailed spec: [03-file-folder-flow.md](features/03-file-folder-flow.md)

### 5. Drag-Out Copy

* Drag shelf items out to Explorer/Desktop.
* Default copy behavior.
* Verify original files are not moved.
* Detailed spec: [04-drag-out-copy.md](features/04-drag-out-copy.md)

### 6. Text / URL / Image Input

* Paste text and URLs.
* Copy/open text and URL items.
* Paste or drag pathless images.
* Store image copies and thumbnails in app-owned folders.
* Detailed spec: [05-text-url-image-input.md](features/05-text-url-image-input.md)

### 7. UX Polish

* Windows-native / Fluent-inspired styling.
* Empty drop zone.
* Hover card actions.
* Single-card selection.
* Keyboard actions: `Escape`, `Delete`, `Ctrl+C`, `Enter`.
* Detailed spec: [06-ux-polish.md](features/06-ux-polish.md)

### 8. Settings

* Dock edge: left, right, top, bottom.
* Theme mode: system, light, dark.
* Density: compact, comfortable.
* Start with Windows toggle.
* Detailed spec: [07-settings.md](features/07-settings.md)

### 9. Installer

* Inno Setup script.
* Per-user install.
* Start Menu shortcut.
* Uninstall entry.
* Installer smoke test.
* Detailed spec: [08-installer.md](features/08-installer.md)

## Deferred

## Post-0.1.2 Roadmap

These ideas come from the 0.1.2 release review. They are not committed release
scope yet; use them to seed future Trellis tasks and feature specs.

### Recommended Next Release Theme: Card Management Efficiency

Prioritize card management before adding more input types. EdgeTuck already
handles the core shelf workflow, so the next high-value improvements are about
keeping a growing shelf easy to scan and clean up.

Candidate 0.1.3 scope:

* Drag reorder cards.
* Search or filter by text, URL, file name, or folder name.
* Multi-select cards and remove selected items in one action.
* Detect and clean up missing file/folder path records.

### Future Optimization Areas

* Card organization: pin or favorite important cards, batch actions, and clearer
  duplicate-path handling.
* Fast operations: global hotkeys for show/hide, paste-to-shelf, copy selected
  card, and configurable double-click behavior.
* Multi-monitor shell behavior: per-monitor placement, DPI/scale resilience,
  taskbar avoidance, and configurable handle size/opacity/sensitivity.
* Text and URL cards: URL title/domain summaries, card rename, better snippet
  handling, and formatted preview for Markdown/code-like text.
* File reliability: clearer missing-source state, one-click invalid-record
  cleanup, duplicate detection, and manual relink after files move.
* Image workflow: larger preview, clearer copy-as-file versus copy-as-bitmap
  actions, rename/notes, and timestamp-based names for screenshot images.
* Install/update trust follow-ups: certificate procurement/renewal operations,
  release-channel policy, and installer smoke-test automation.

### Implemented Feature: Install And Update Trust

Add code-signing release support, clearer update notes before install, cleanup
for legacy shortcut names, automatic update-check policy, and an "updated to
version" first-run confirmation.

Automatic update options:

* Never: preserve the current fully manual behavior.
* Daily: check once when the last successful or attempted automatic check is at
  least 24 hours old.
* Weekly: check once when the last successful or attempted automatic check is at
  least 7 days old.

Behavior:

* If an update is available, reuse the existing update information display and
  show the existing download/install button.
* If no update is available, keep the result quiet unless the settings page is
  open; the settings status can show the existing latest-version message.
* If the check fails, do not show a blocking dialog or startup interruption.
  Surface the failure only in the settings page status area.
* Run the check asynchronously so startup and settings-window opening stay
  responsive.

Implementation notes:

* `AutoUpdateCheckMode` supports `Never`, `Daily`, and `Weekly`.
* `LastAutomaticUpdateCheckUtc` throttles daily/weekly checks.
* Automatic checks trigger from startup, reuse the existing update status,
  release notes, and `DownloadUpdateCommand` path, and never download or install
  without a user click.

### Still Out Of Scope For V1

* Cloud sync.
* Sharing links.
* OCR.
* Image editing.
* File conversion.
* Clipboard history replacement.
* UI automation tests.
