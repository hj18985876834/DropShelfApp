# Directory Structure

> How frontend code is organized in this project.

---

## Overview

DropShelf is a Windows WPF desktop app. UI code lives under
`src/DropShelf.App/` and follows a small MVVM-lite layout.

---

## Directory Layout

```
src/
└── DropShelf.App/
    ├── App.xaml
    ├── Assets/
    ├── Commands/
    │   └── RelayCommand.cs
    ├── Converters/
    ├── Interop/
    ├── Models/
    ├── Resources/
    │   ├── Styles/
    │   └── Themes/
    ├── Services/
    ├── ViewModels/
    └── Views/
```

---

## Module Organization

Use these folders by responsibility:

* `Views/`: WPF windows and XAML surfaces. Code-behind should only bridge UI events that are awkward to bind directly, such as window lifecycle or drag/drop event entry points.
* `ViewModels/`: UI state and commands. ViewModels should not directly perform file system, registry, tray, or window-position side effects.
* `Models/`: serializable app data and enums, such as `ShelfItem`, `AppSettings`, `DockEdge`, `ThemeMode`, and `DensityMode`.
* `Services/`: side-effect boundaries for persistence, image storage, drag/drop interpretation, tray icon, startup registration, theme, and docking.
* `Commands/`: small command primitives such as `RelayCommand`.
* `Resources/`: theme/style resources. Keep visual constants centralized here once styling work begins.
* `Interop/`: native Windows interop declarations only.

Keep V1 as a single WPF app project plus one test project. Do not split class libraries until there is real complexity that benefits from the boundary.

---

## Naming Conventions

* Windows/views end with `Window`, e.g. `ShelfWindow`, `SettingsWindow`.
* ViewModels end with `ViewModel`.
* Services end with `Service` or `Store`, based on responsibility.
* Enum names are nouns, e.g. `DockEdge`, `ThemeMode`.
* Keep namespaces aligned with folders under `DropShelf.App`.

---

## Examples

Current baseline examples:

* `src/DropShelf.App/Views/ShelfWindow.xaml`
* `src/DropShelf.App/ViewModels/ShelfViewModel.cs`
* `src/DropShelf.App/Services/ShelfStore.cs`
* `src/DropShelf.App/Models/AppSettings.cs`
