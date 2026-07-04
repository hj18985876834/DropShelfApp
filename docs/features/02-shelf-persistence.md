# Feature Spec: Shelf Persistence

## Goal

Persist shelf records and app settings locally so DropShelf restores previous shelf state after restart.

## Branch

`feature/shelf-persistence`

## Dependencies

* Project skeleton.
* App shell is useful but not strictly required for service-level work.

## User Flow

1. User adds shelf records through later features.
2. User exits DropShelf.
3. User starts DropShelf again.
4. Previous shelf records and settings are restored.
5. User can remove records or clear all records.

## Detailed Behavior

### App Data Root

Use:

```text
%LOCALAPPDATA%/DropShelf/
```

Expected files:

```text
settings.json
shelf.json
images/
  originals/
  thumbs/
logs/
```

### Shelf Records

Stored in `shelf.json`.

Minimum fields:

* `id`
* `type`
* `displayName`
* `sourcePath`
* `content`
* `imagePath`
* `thumbnailPath`
* `createdAt`

Rules:

* File/folder records store original paths only.
* Text/URL records store local content.
* Image records store app-owned file paths.
* Item order must round-trip.
* Record IDs must remain stable across save/load.

### Settings

Stored in `settings.json`.

Minimum fields:

* `dockEdge`
* `themeMode`
* `densityMode`
* `startWithWindows`

Defaults:

* `dockEdge`: `Right`
* `themeMode`: `System`
* `densityMode`: `Compact`
* `startWithWindows`: `false`

### Failure Handling

* Missing files should return defaults or empty records.
* Malformed JSON should not crash app startup.
* If malformed data is detected, keep behavior conservative:
  * settings fall back to defaults
  * shelf falls back to empty list
* Do not delete corrupted files automatically in V1 unless explicitly added later.

## UI States

Persistence itself has no dedicated UI, but consumers should be able to show:

* empty shelf after no records
* restored populated shelf
* missing file state for file records whose paths no longer exist

## Data Contract

Serialization should remain stable enough for later migrations. Avoid renaming persisted fields casually after this feature lands.

## Edge Cases

* App data directory does not exist.
* `settings.json` missing.
* `shelf.json` missing.
* Empty JSON file.
* Malformed JSON.
* Unknown enum values.
* Shelf item with missing optional fields.
* Large text item is deferred; V1 can store normal text directly.

## Acceptance Criteria

* `AppSettings` saves and loads.
* Shelf item list saves and loads.
* Missing data files do not crash.
* Malformed data files do not crash.
* Item order is preserved.
* Default settings match PRD.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.

## Tests

### Unit / File-System Tests

Use temporary directories, not real `%LOCALAPPDATA%`.

* `AppDataPathService` composes a `DropShelf` local data path.
* `ShelfStore.SaveAsync` creates directories and writes JSON.
* `ShelfStore.LoadAsync` returns empty list when missing.
* Shelf records round-trip.
* Settings defaults test already exists and should remain passing.
* Add settings store tests when settings persistence is implemented.
* Malformed JSON fallback behavior is tested.

### Manual Windows Tests

* Add temporary seeded records through a debug path or later UI.
* Restart app.
* Confirm records restore.

## Files Likely Touched

* `src/DropShelf.App/Models/AppSettings.cs`
* `src/DropShelf.App/Models/ShelfItem.cs`
* `src/DropShelf.App/Services/AppDataPathService.cs`
* `src/DropShelf.App/Services/ShelfStore.cs`
* new `src/DropShelf.App/Services/SettingsStore.cs`
* `tests/DropShelf.Tests/*`

## Out Of Scope

* Database.
* Cloud sync.
* Encryption.
* Auto-delete or auto-expire records.
* Migration framework beyond conservative fallback.
