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

## Common Mistakes

* Do not let Views perform persistence directly.
* Do not let ViewModels write registry values, files, or image data directly; use Services.
* Do not duplicate settings defaults across UI and storage. `AppSettings.CreateDefault()` is the starting source of truth.
* Do not make file/folder shelf records copy or move original files. V1 stores path references only.
