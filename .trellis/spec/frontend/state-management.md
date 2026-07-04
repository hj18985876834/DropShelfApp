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

## Common Mistakes

* Do not let Views perform persistence directly.
* Do not let ViewModels write registry values, files, or image data directly; use Services.
* Do not duplicate settings defaults across UI and storage. `AppSettings.CreateDefault()` is the starting source of truth.
* Do not make file/folder shelf records copy or move original files. V1 stores path references only.
