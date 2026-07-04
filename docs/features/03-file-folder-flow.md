# Feature Spec: File And Folder Flow

## Goal

Support the core V1 flow for files and folders: drag into shelf, store path references, render cards, and perform basic actions.

## Branch

`feature/file-folder-flow`

## Dependencies

* App Shell.
* Shelf Persistence.

## User Flow

1. User opens shelf.
2. User drags one or more files/folders from Explorer into the shelf.
3. Shelf creates one card per item.
4. User can open, reveal in Explorer, copy path, remove, or clear.
5. Restarting app restores cards.

## Detailed Behavior

### Input

Accept file system drops from Explorer.

For each dropped path:

* If path is file, create `ShelfItemType.File`.
* If path is directory, create `ShelfItemType.Folder`.
* Store original path in `SourcePath`.
* Do not copy or move the source.
* `DisplayName` defaults to file/folder name.

### Cards

Each card should show:

* file/folder icon or type affordance
* display name
* parent folder or shortened path
* hover actions

Card actions:

* copy path
* open
* reveal in Explorer
* remove

### Remove / Clear

* Remove deletes the shelf record only.
* Clear all deletes shelf records only.
* Original files and folders are never deleted, moved, or modified.

### Missing Files

If a stored `SourcePath` no longer exists:

* card remains visible
* card shows a calm missing state
* open/reveal actions are disabled or show non-blocking feedback
* remove remains available

## UI States

* Empty drop zone
* Drag-over accepted
* One or more file/folder cards
* Card hover actions visible
* Selected card
* Missing file/folder card
* Clear-all confirmation is optional; if no confirmation, wording must be clear that originals are not deleted

## Data Contract

File item:

```text
Type = File
SourcePath = absolute original file path
DisplayName = file name
```

Folder item:

```text
Type = Folder
SourcePath = absolute original folder path
DisplayName = folder name
```

No file bytes are stored for file/folder items.

## Edge Cases

* Multiple files dropped at once.
* Mix of files and folders.
* Duplicate same path dropped more than once. V1 may allow duplicates unless we decide otherwise later.
* Very long filenames.
* Path no longer exists.
* Path exists but access denied.
* Network/removable drive path unavailable.
* Unsupported drag payload should not crash.

## Acceptance Criteria

* Dragging files from Explorer creates file cards.
* Dragging folders from Explorer creates folder cards.
* Cards persist after restart.
* Copy path copies original path.
* Open uses default Windows behavior.
* Reveal opens Explorer at the item when available.
* Remove deletes record only.
* Clear all deletes records only.
* Original files/folders remain unchanged.
* Missing paths show missing state.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.

## Tests

### Unit / File-System Tests

* Create file item from path.
* Create folder item from path.
* Remove item does not delete source file in temp directory.
* Clear all does not delete source files in temp directory.
* Missing path detection returns expected state.

### Manual Windows Tests

* Drag file from Explorer.
* Drag folder from Explorer.
* Drag multiple items.
* Restart app and confirm cards remain.
* Delete source file externally and confirm missing state.
* Copy path.
* Open file/folder.
* Reveal in Explorer.
* Remove and clear all.

## Files Likely Touched

* `src/DropShelf.App/Services/DragDropService.cs`
* `src/DropShelf.App/Services/ShelfStore.cs`
* `src/DropShelf.App/ViewModels/ShelfViewModel.cs`
* `src/DropShelf.App/ViewModels/ShelfItemViewModel.cs`
* `src/DropShelf.App/Views/ShelfWindow.xaml`
* `src/DropShelf.App/Models/ShelfItem.cs`
* `tests/DropShelf.Tests/*`

## Out Of Scope

* Dragging items out.
* File copy/move operations.
* File previews.
* File rename.
* Recursive folder scanning.
