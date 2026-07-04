# State Management

> How state is managed in this project.

---

## Overview

DropShelf uses MVVM-lite for WPF state. V1 does not use a third-party MVVM framework.

The app provides small in-house primitives:

* `ObservableObject`
* `RelayCommand`

Revisit `CommunityToolkit.Mvvm` only if ViewModel boilerplate becomes a real maintenance problem.

---

## State Categories

* View state: selected shelf item, drag-over state, hover-driven actions, and settings window state. Store in ViewModels.
* Persistent app state: `AppSettings` and shelf records. Store through Services, not directly from Views.
* File/image state: file paths remain references; app-owned image files live in local app data and are tracked by shelf records.
* Server state: none. DropShelf V1 is local-only and must not depend on accounts, cloud sync, or external APIs.

---

## When to Use Global State

Avoid global mutable state in V1. Prefer explicit service instances passed into ViewModels.

Shared state belongs in a service only when multiple surfaces need the same source of truth, such as:

* shelf records
* app settings
* theme mode
* dock edge

---

## Server State

There is no server state in V1. Do not introduce network calls for shelf records, settings, images, telemetry, templates, or updates.

---

## Scenario: Local Persistence Stores

### 1. Scope / Trigger

Use this contract whenever code reads or writes persistent DropShelf state under
`%LOCALAPPDATA%/DropShelf/`.

### 2. Signatures

* `new AppDataPathService(string? localAppDataRoot = null).GetAppDataRoot()`
  returns the app data root path.
* `SettingsStore.LoadAsync(CancellationToken)` returns `AppSettings`.
* `SettingsStore.SaveAsync(AppSettings, CancellationToken)` writes
  `settings.json`.
* `ShelfStore.LoadAsync(CancellationToken)` returns
  `IReadOnlyList<ShelfItem>`.
* `ShelfStore.SaveAsync(IEnumerable<ShelfItem>, CancellationToken)` writes
  `shelf.json`.

### 3. Contracts

* Root directory: `%LOCALAPPDATA%/DropShelf/`.
* Settings file: `settings.json`.
* Shelf records file: `shelf.json`.
* JSON fields are camelCase.
* Enums are serialized as camelCase strings, not integers.
* `AppSettings.CreateDefault()` is the only source for settings defaults.
* File and folder shelf items store `sourcePath` references only.
* Text and URL shelf items store local `content`.
* Image shelf items store app-owned `imagePath` and `thumbnailPath`.

### 4. Validation & Error Matrix

* Missing `settings.json` -> return `AppSettings.CreateDefault()`.
* Missing `shelf.json` -> return an empty list.
* Empty or malformed JSON -> return defaults/empty list.
* Unknown enum value -> return defaults/empty list.
* Missing optional shelf fields -> allow the model default/null value.
* Corrupt files are not deleted automatically in V1.

### 5. Good/Base/Bad Cases

* Good: save settings or shelf records, reload them, and preserve enum values,
  item IDs, timestamps, and item order.
* Base: no local files exist on first launch; the app starts with default
  settings and an empty shelf.
* Bad: partially written or hand-edited invalid JSON must not crash app startup.

### 6. Tests Required

Use temporary directories, never real `%LOCALAPPDATA%`.

* Assert `AppDataPathService` composes a `DropShelf` path.
* Assert both stores create parent directories on save.
* Assert settings round-trip with non-default values.
* Assert shelf records round-trip in order with stable IDs.
* Assert missing and malformed files fall back conservatively.
* Assert unknown enum values fall back conservatively.

### 7. Wrong vs Correct

#### Wrong

```csharp
await JsonSerializer.SerializeAsync(stream, settings);
```

This writes PascalCase fields and numeric enum values by default.

#### Correct

```csharp
await JsonSerializer.SerializeAsync(stream, settings, PersistenceJsonOptions.Default, cancellationToken);
```

Use the shared persistence JSON options so all stored files keep the same
contract.

---

## Scenario: File And Folder Shelf Items

### 1. Scope / Trigger

Use this contract whenever Explorer file/folder drops become shelf records, or
when file/folder shelf records are displayed, copied, opened, revealed, removed,
cleared, or persisted.

### 2. Signatures

* `DragDropService.CreateFileSystemItems(IEnumerable<string>)` returns one
  `ShelfItem` per existing file or directory path.
* `ShelfViewModel.AddItems(IEnumerable<ShelfItem>)` wraps records in
  `ShelfItemViewModel` instances for display and commands.
* `ShelfViewModel.GetShelfItems()` returns model records for persistence.
* `ShelfItemViewModel.CopyPathCommand` copies the original `SourcePath`.
* `ShelfItemViewModel.OpenCommand` opens the path with default Windows behavior.
* `ShelfItemViewModel.RevealCommand` opens Explorer at the item.
* `ShelfItemViewModel.RemoveCommand` removes the record only.
* `ShelfViewModel.ClearAllCommand` clears shelf records only.

### 3. Contracts

* Existing files create `ShelfItemType.File`.
* Existing directories create `ShelfItemType.Folder`.
* `SourcePath` is the absolute original path.
* `DisplayName` defaults to the file or folder name.
* File/folder items never copy bytes into app data.
* File/folder remove and clear flows never delete, move, rename, or copy the
  original source.
* Missing source paths remain visible and report `IsMissing = true`.
* Open and reveal are disabled or return non-blocking status when the source is
  missing or unavailable.

### 4. Validation & Error Matrix

* Unsupported drag payload -> no records; UI marks drag as unsupported.
* Empty or whitespace path -> ignored.
* Path does not exist at drop time -> ignored.
* Duplicate existing path -> allowed in V1.
* Path exists at display time -> open/reveal commands can execute.
* Path no longer exists at display time -> missing state; remove remains
  available.

### 5. Good/Base/Bad Cases

* Good: dropping a mix of files and folders creates ordered records, persists
  them, and restores cards after restart.
* Base: removing a record or clearing the shelf leaves original files/folders
  untouched.
* Bad: treating a file/folder shelf item like an app-owned image and deleting
  `SourcePath` would destroy user data.

### 6. Tests Required

Use temporary directories/files.

* Assert file path -> `ShelfItemType.File`, original `SourcePath`, file name
  `DisplayName`.
* Assert directory path -> `ShelfItemType.Folder`, original `SourcePath`, folder
  name `DisplayName`.
* Assert mixed file/folder order is preserved.
* Assert missing/unsupported paths are ignored on input.
* Assert remove does not delete source files.
* Assert clear all does not delete source files.
* Assert missing source path yields missing state and disables open/reveal.
* Assert copy path sends the original `SourcePath` to the clipboard boundary.

### 7. Wrong vs Correct

#### Wrong

```csharp
File.Delete(item.SourcePath);
Items.Remove(item);
```

File/folder shelf records are references. Removing a record must never delete
the user's original file or folder.

#### Correct

```csharp
Items.Remove(itemViewModel);
await shelfStore.SaveAsync(viewModel.GetShelfItems(), cancellationToken);
```

Only the app's shelf record changes; the file system source remains untouched.

---

## Common Mistakes

* Do not let Views perform persistence directly.
* Do not let ViewModels write registry values, files, or image data directly; use Services.
* Do not duplicate settings defaults across UI and storage. `AppSettings.CreateDefault()` is the starting source of truth.
* Do not make file/folder shelf records copy or move original files. V1 stores path references only.

## Scenario: App-Owned Image Shelf Items

### 1. Scope / Trigger

Use this contract whenever code accepts pathless bitmap data from clipboard or
drag/drop and turns it into a persisted shelf item.

### 2. Signatures

* `new ImageStore(appDataRoot)` owns image cache paths under the DropShelf app
  data root.
* `ImageStore.SaveImage(BitmapSource)` returns a `ShelfItem` with `Type = Image`.
* `ImageStore.DeleteImageFiles(ShelfItem)` removes app-owned image files for an
  image shelf item.
* `ImagePathConverter` loads thumbnail paths with `BitmapCacheOption.OnLoad`
  before binding them in WPF.

### 3. Contracts

* Pathless images are stored under `%LOCALAPPDATA%/DropShelf/images/originals/`.
* Thumbnails are stored under `%LOCALAPPDATA%/DropShelf/images/thumbs/`.
* Image shelf records store `imagePath` and `thumbnailPath`; file/folder shelf
  records continue to store only `sourcePath`.
* Explorer image file drops remain file references. Do not duplicate an image
  file just because its extension is an image type.

### 4. Validation & Error Matrix

* Clipboard/drop data has file-drop format -> handle as file/folder first.
* Clipboard/drop data has bitmap format without file-drop format -> save an
  app-owned original and thumbnail.
* Missing image or thumbnail on reload -> keep the card and show a missing
  state; do not crash startup.
* Delete image item -> delete app-owned original and thumbnail if present.
* Delete file/folder item -> delete shelf record only; never delete source.

### 5. Good/Base/Bad Cases

* Good: paste a screenshot, restart, see the thumbnail, then remove the item and
  confirm both cached files are gone.
* Base: image cache file was manually deleted; app still loads the record and
  shows a missing/corrupt state.
* Bad: binding an image path directly in WPF can keep the file locked and make
  remove/clear fail to delete the app-owned thumbnail.

### 6. Tests Required

* Assert `ImageStore.SaveImage` creates original and thumbnail paths under app
  data.
* Assert `ImageStore.DeleteImageFiles` removes existing app-owned files and
  ignores missing files.
* Assert remove/clear in `ShelfViewModel` deletes app-owned image files but
  leaves original file/folder source paths untouched.
* Assert `DragDropService` treats Explorer image file drops as file records.

### 7. Wrong vs Correct

#### Wrong

```xml
<Image Source="{Binding ThumbnailPath}" />
```

This can leave the thumbnail file locked by the WPF image pipeline.

#### Correct

```xml
<Image Source="{Binding ThumbnailPath, Converter={StaticResource ImagePathConverter}}" />
```

The converter loads with `BitmapCacheOption.OnLoad`, freezes the bitmap, and
lets image cleanup delete app-owned files later.
* Do not route file/folder remove or clear through `ImageStore.DeleteImageFiles`; that cleanup is only for app-owned image files.
