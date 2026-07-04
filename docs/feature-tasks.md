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

* Cloud sync.
* Sharing links.
* OCR.
* Image editing.
* File conversion.
* Clipboard history replacement.
* UI automation tests.
