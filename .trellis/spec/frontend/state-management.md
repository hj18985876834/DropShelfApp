# State Management

> How state is managed in this project.

---

## Overview

DropShelf uses MVVM-lite for WPF state. V1 does not use a third-party MVVM framework.

The app provides small in-house primitives:

* `ObservableObject`
* `RelayCommand`
* `AsyncRelayCommand` for WPF commands that await persistence or other
  asynchronous service calls

Revisit `CommunityToolkit.Mvvm` only if ViewModel boilerplate becomes a real maintenance problem.

---

## State Categories

* View state: selected shelf item, drag-over state, hover-driven actions, and settings window state. Store in ViewModels.
* Persistent app state: `AppSettings` and shelf records. Store through Services, not directly from Views.
* File/image state: file paths remain references; app-owned image files live in local app data and are tracked by shelf records.
* Server state: none. DropShelf V1 is local-only and must not depend on accounts, cloud sync, or external APIs.

### Scenario: Async ViewModel Commands

#### 1. Scope / Trigger

Use `AsyncRelayCommand` when a WPF command needs to call an async service method,
especially settings or shelf persistence.

#### 2. Signatures

* `new AsyncRelayCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute = null)`
* `AsyncRelayCommand.ExecuteAsync(object? parameter = null)` is the testable
  entry point for unit tests.
* `AsyncRelayCommand.IsExecuting` disables command re-entry through
  `CanExecute`.

#### 3. Contracts

* Command execution must not block the WPF dispatcher with
  `.GetAwaiter().GetResult()` or `.Result`.
* ViewModels may expose a separate `IsApplying`/`IsSaving` property when the UI
  needs status text or loading state.
* Service awaits inside local persistence stores should use
  `ConfigureAwait(false)` because startup and shutdown still have a few
  synchronous shell lifecycle boundaries.

#### 4. Validation & Error Matrix

* Save succeeds -> persist data, apply side effects, show success status.
* Save or registry update fails with expected local exceptions -> roll back
  pending settings and show an error status.
* Command is clicked again while execution is pending -> `CanExecute` returns
  false and no second save starts.

#### 5. Good/Base/Bad Cases

* Good: settings Apply awaits `SettingsStore.SaveAsync`, disables duplicate
  Apply clicks, and keeps the settings window responsive.
* Base: synchronous commands that only update local ViewModel state can continue
  using `RelayCommand`.
* Bad: a WPF command calls `settingsStore.SaveAsync(...).GetAwaiter().GetResult()`
  and deadlocks when the continuation tries to resume on the dispatcher.

#### 6. Tests Required

* Unit tests should call `AsyncRelayCommand.ExecuteAsync()` and await it.
* Test success persistence and applied state.
* Test expected failure rollback and error status.
* Test that a delayed save sets the ViewModel in-flight state and disables
  command re-entry.

#### 7. Wrong vs Correct

##### Wrong

```csharp
ApplyCommand = new RelayCommand(_ =>
{
    settingsStore.SaveAsync(settings).GetAwaiter().GetResult();
    ApplySettings(settings);
});
```

##### Correct

```csharp
ApplyCommand = new AsyncRelayCommand(async _ =>
{
    IsApplying = true;
    await settingsStore.SaveAsync(settings);
    ApplySettings(settings);
    IsApplying = false;
});
```

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
* Shelf record changes must be saved after collection mutations, not only during
  application exit. Text, URL, and pathless image records are created at runtime
  and can be lost if the process is killed, validation builds replace the
  executable, or shutdown skips the normal `OnExit` path.

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
* Good: adding, removing, or clearing shelf records queues a durable shelf save
  from the app composition layer while `OnExit` still performs a final
  synchronous save of the latest ViewModel snapshot.
* Base: no local files exist on first launch; the app starts with default
  settings and an empty shelf.
* Bad: partially written or hand-edited invalid JSON must not crash app startup.
* Bad: only saving `shelf.json` from `OnExit`; this makes runtime-created text,
  URL, and image items disappear after abnormal termination.

### 6. Tests Required

Use temporary directories, never real `%LOCALAPPDATA%`.

* Assert `AppDataPathService` composes a `DropShelf` path.
* Assert both stores create parent directories on save.
* Assert settings round-trip with non-default values.
* Assert shelf records round-trip in order with stable IDs.
* Assert text, URL, and image shelf records round-trip with `content`,
  `imagePath`, and `thumbnailPath`.
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
* `DragDropService.TryCreateDragOutPayload(ShelfItem)` returns a
  `DragOutPayloadResult` with either a WPF file-drop payload or a user-facing
  refusal message.
* `DragDropService.MaxDragOutBytes` is the V1 per-item drag-out limit.
* `DragDropService.InternalDragFormat` marks drags started from DropShelf.

### 3. Contracts

* Existing files create `ShelfItemType.File`.
* Existing directories create `ShelfItemType.Folder`.
* `SourcePath` is the absolute original path.
* `DisplayName` defaults to the file or folder name.
* File/folder items never copy bytes into app data.
* Drag-out uses standard `DataFormats.FileDrop` with `DragDropEffects.Copy`.
* Every drag started from DropShelf carries `InternalDragFormat`.
* Drop-in creation must ignore any data object carrying `InternalDragFormat`,
  even if it also contains `FileDrop`, so dragging a shelf card back into the
  app cannot duplicate records.
* Drag-out is allowed only when the source still exists and its size can be
  read.
* V1 drag-out refuses files or recursively measured folders larger than
  `DragDropService.MaxDragOutBytes` at the point where the drag threshold is
  crossed. Show the result message on the card and do not call WPF
  `DragDrop.DoDragDrop` for the oversized item.
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
* Path exists and size <= `MaxDragOutBytes` -> drag-out payload can be created.
* Path exists but size > `MaxDragOutBytes` -> no external drag/drop starts; card
  status says the item is too large.
* Path size cannot be read -> no external drag/drop starts; card status says
  size cannot be read.
* Internal DropShelf drag is dropped back into DropShelf -> no records are
  created.
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
* Assert drag-out payload reports `Copy` for valid file/folder paths.
* Assert oversized file/folder returns an invalid result with a user-facing
  status message.
* Assert missing or inaccessible source returns an invalid drag-out result.
* Assert internal drag data is ignored by drop-in creation.

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
* `ImageStore.SaveImageAsync(BitmapSource, CancellationToken)` is the UI-facing
  path for clipboard/drag bitmap input.
* `DragDropService.CreateItemsAsync(IDataObject, ImageStore, CancellationToken)`
  creates shelf records and moves pathless image encoding off the dispatcher.
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
* Image file references may still show a card thumbnail by binding preview UI to
  `ShelfItemViewModel.ImagePreviewPath`, not by changing the record type.
* Common previewable image extensions include `.png`, `.jpg`, `.jpeg`, `.gif`,
  `.bmp`, `.webp`, `.tif`, `.tiff`, `.ico`, `.heic`, `.heif`, `.avif`, and
  Windows imaging variants such as `.wdp`.

### 4. Validation & Error Matrix

* Clipboard/drop data has file-drop format -> handle as file/folder first.
* Drag-over and accept-preview checks must only call
  `GetDataPresent(format, autoConvert: false)`; do not call `GetData` or
  inspect file/text payloads in high-frequency preview events.
* Clipboard/drop data has bitmap format without file-drop format -> save an
  app-owned original and thumbnail in the background, then add the finished
  image shelf record.
* Explorer file-drop data points to a supported image file -> keep
  `Type = File`, keep `SourcePath`, and expose `ImagePreviewPath = SourcePath`.
* Missing image or thumbnail on reload -> keep the card and show a missing
  state; do not crash startup.
* Delete image item -> delete app-owned original and thumbnail if present.
* Delete file/folder item -> delete shelf record only; never delete source.

### 5. Good/Base/Bad Cases

* Good: paste a screenshot, restart, see the thumbnail, then remove the item and
  confirm both cached files are gone.
* Good: clipboard/drop handlers read the WPF data object on the dispatcher, then
  await `CreateItemsAsync`; PNG encoding and thumbnail generation do not block
  shell input.
* Good: `CanCreateItems` only checks native formats, so Windows delayed
  clipboard/drag rendering is triggered once at paste/drop time instead of on
  every `DragOver`.
* Base: image cache file was manually deleted; app still loads the record and
  shows a missing/corrupt state.
* Bad: using `GetData` in `DragOver`, `CanCreateItems`, or other preview paths;
  this can repeatedly force the source application to render text or bitmap
  data before the user drops anything.
* Bad: calling `ImageStore.SaveImage` directly from a paste or drop event blocks
  the dispatcher while PNG encoding and disk writes complete.
* Bad: binding an image path directly in WPF can keep the file locked and make
  remove/clear fail to delete the app-owned thumbnail.

### 6. Tests Required

* Assert `ImageStore.SaveImage` creates original and thumbnail paths under app
  data.
* Assert `ImageStore.SaveImageAsync` and `DragDropService.CreateItemsAsync`
  create image records and cached files for bitmap input.
* Assert `DragDropService.CanCreateItems` accepts supported formats without
  reading payload data.
* Assert `ImageStore.DeleteImageFiles` removes existing app-owned files and
  ignores missing files.
* Assert remove/clear in `ShelfViewModel` deletes app-owned image files but
  leaves original file/folder source paths untouched.
* Assert `DragDropService` treats Explorer image file drops as file records.
* Assert `ShelfItemViewModel` exposes image preview paths for common image file
  references and does not expose previews for non-image files.

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
