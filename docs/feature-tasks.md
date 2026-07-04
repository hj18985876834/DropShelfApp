# DropShelf Feature Task List

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

### 3. Shelf Persistence

* `ShelfStore` JSON persistence.
* `AppSettings` persistence.
* Local app data path handling.
* Missing/corrupt data fallback.

### 4. File / Folder Flow

* Drag files/folders into shelf.
* Store path references only.
* Render file/folder cards.
* Open, reveal, copy path, remove, clear.

### 5. Drag-Out Copy

* Drag shelf items out to Explorer/Desktop.
* Default copy behavior.
* Verify original files are not moved.

### 6. Text / URL / Image Input

* Paste text and URLs.
* Copy/open text and URL items.
* Paste or drag pathless images.
* Store image copies and thumbnails in app-owned folders.

### 7. UX Polish

* Windows-native / Fluent-inspired styling.
* Empty drop zone.
* Hover card actions.
* Single-card selection.
* Keyboard actions: `Escape`, `Delete`, `Ctrl+C`, `Enter`.

### 8. Settings

* Dock edge: left, right, top, bottom.
* Theme mode: system, light, dark.
* Density: compact, comfortable.
* Start with Windows toggle.

### 9. Installer

* Inno Setup script.
* Per-user install.
* Start Menu shortcut.
* Uninstall entry.
* Installer smoke test.

## Deferred

* Cloud sync.
* Sharing links.
* OCR.
* Image editing.
* File conversion.
* Clipboard history replacement.
* UI automation tests.
