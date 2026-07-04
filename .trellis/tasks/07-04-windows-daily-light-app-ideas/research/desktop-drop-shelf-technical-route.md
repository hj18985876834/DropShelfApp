# Desktop Drop Shelf Technical Route

## Topic

Define a practical technical route for the local-only Windows Desktop Drop Shelf MVP: framework choice, architecture, module boundaries, persistence, packaging, and implementation phases.

## Official reference checks

### .NET version

As of 2026-07-04, .NET 10 is the current LTS line. Microsoft lists .NET 10 as active LTS with support until November 14, 2028.

Sources:

* https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
* https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

### WPF

WPF is a Windows-only UI framework for .NET desktop applications. It supports XAML UI, controls, data binding, layout, graphics, animation, styles, and templates.

Sources:

* https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
* https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/

### Drag and drop

WPF has built-in drag-and-drop infrastructure for data transfer within WPF apps and between WPF and other Windows applications. This maps directly to the shelf's core workflow.

Sources:

* https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/drag-and-drop
* https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/drag-and-drop-overview
* https://learn.microsoft.com/en-us/dotnet/api/system.windows.dragdrop

### Data binding / MVVM

WPF data binding supports connecting UI to .NET object data sources. MVVM is the natural architecture for keeping shelf item state, commands, and views separated.

Sources:

* https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/
* https://learn.microsoft.com/en-us/windows/uwp/data-binding/data-binding-and-mvvm

### Tray icon

For a WPF app, the pragmatic tray option is to use the Windows Forms `NotifyIcon` component from a WPF host process.

Sources:

* https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon
* https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/notifyicon-component-windows-forms

### Local app data

Use `Environment.GetFolderPath` with `Environment.SpecialFolder.LocalApplicationData` to resolve a per-user local data folder instead of hardcoding paths.

Sources:

* https://learn.microsoft.com/en-us/dotnet/api/system.environment.getfolderpath
* https://learn.microsoft.com/en-us/dotnet/api/system.environment.specialfolder

### Deployment

.NET supports publishing modes including framework-dependent and self-contained deployments. Single-file deployment is possible, but self-contained single-file apps are larger because they include runtime and framework libraries.

Sources:

* https://learn.microsoft.com/en-us/dotnet/core/deploying/
* https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview
* https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/deploying-a-wpf-application-wpf

## Recommended stack

* Language: C#
* Runtime: .NET 10 LTS
* UI framework: WPF
* Architecture: MVVM-lite
* Persistence: JSON metadata files in `%LOCALAPPDATA%/<AppName>/`
* Image storage: app-owned local data folders
* Packaging: start with self-contained Windows x64 publish for development/testing; later choose installer/MSIX after MVP usability is proven

## Why WPF over WinUI 3 / Electron / Tauri

### WPF

Pros:

* Mature Windows desktop framework.
* Strong drag/drop support.
* Good XAML/data binding fit for MVVM.
* Practical tray integration through WinForms `NotifyIcon`.
* Low conceptual overhead for a Windows-only utility.

Cons:

* Less modern out of the box than WinUI 3.
* Fluent-style polish must be implemented with resources/styles.

### WinUI 3

Pros:

* More modern Windows visual direction.
* Strong Windows App SDK alignment.

Cons:

* More friction for classic tray/utility-window patterns.
* Potentially slower to iterate for this MVP.

### Electron / Tauri

Pros:

* Easier if the team is web-first.

Cons:

* Core value is native drag/drop, tray, edge windows, and low resource use.
* Native Windows stack is a cleaner fit for V1.

## Project organization proposal

Suggested repository layout:

```text
drop-shelf/
  src/
    DropShelf.App/
      App.xaml
      App.xaml.cs
      DropShelf.App.csproj
      Assets/
      Resources/
        Themes/
          Theme.System.xaml
          Theme.Light.xaml
          Theme.Dark.xaml
        Styles/
      Views/
        ShelfWindow.xaml
        SettingsWindow.xaml
      ViewModels/
        ShelfViewModel.cs
        ShelfItemViewModel.cs
        SettingsViewModel.cs
      Models/
        ShelfItem.cs
        ShelfItemType.cs
        AppSettings.cs
        DockEdge.cs
        ThemeMode.cs
        DensityMode.cs
      Services/
        AppDataPathService.cs
        ShelfStore.cs
        ImageStore.cs
        DragDropService.cs
        TrayIconService.cs
        StartupService.cs
        ThemeService.cs
        WindowDockService.cs
      Commands/
        RelayCommand.cs
      Converters/
      Interop/
        NativeMethods.cs
  tests/
    DropShelf.Tests/
      ShelfStoreTests.cs
      ImageStoreTests.cs
      AppSettingsTests.cs
  docs/
    architecture.md
    packaging.md
  README.md
```

## Module responsibilities

### Views

Only XAML surfaces and light UI event bridging:

* `ShelfWindow`: edge handle, shelf panel, drop zone, item list.
* `SettingsWindow`: small settings UI.

### ViewModels

UI state and commands:

* `ShelfViewModel`: shelf collection, selected item, clear all, drop acceptance state.
* `ShelfItemViewModel`: card display state and per-item commands.
* `SettingsViewModel`: dock edge, theme mode, density, startup toggle.

### Models

Serializable domain data:

* `ShelfItem`: id, type, created/updated time, display name, source path or content reference.
* `AppSettings`: dock edge, theme mode, density, start with Windows.

### Services

Side effects and platform boundaries:

* `ShelfStore`: read/write shelf metadata JSON.
* `ImageStore`: save pasted/dragged images and thumbnails under app local data.
* `DragDropService`: map Windows data formats into shelf item creation commands.
* `TrayIconService`: show/hide/settings/quit tray menu.
* `StartupService`: opt-in start with Windows behavior.
* `ThemeService`: system/light/dark theme switching.
* `WindowDockService`: position handle/panel on selected screen edge.

## Data storage proposal

Base:

```text
%LOCALAPPDATA%/DropShelf/
  settings.json
  shelf.json
  images/
    originals/
    thumbs/
  logs/
```

Shelf metadata stores:

* Files/folders: original paths only.
* Text/URL: local record content or content file reference if large.
* Images: local app-owned image file path and thumbnail path.

Important semantics:

* Clearing records never deletes original user files.
* Removing image records deletes app-owned image copies and thumbnails.
* Missing file/folder paths show a missing state.

## Testing strategy

Unit-testable:

* Shelf metadata serialization/deserialization.
* Settings persistence and defaults.
* Image store path creation and cleanup.
* Missing-file detection.
* Clear/remove semantics.

Manual/UI verification:

* Drag in files/folders from Explorer.
* Drag out to Explorer/Desktop.
* Paste text/URL/image.
* Theme switching.
* Dock edge switching.
* Tray show/hide/quit.
* Restart persistence.

## Build and packaging direction

Initial development:

* `dotnet build`
* `dotnet test`
* `dotnet publish -c Release -r win-x64 --self-contained true`

Packaging choices to decide later:

1. Portable self-contained folder/zip
   * Fastest for testing.
   * No installer polish.

2. Installer
   * Better for non-technical sharing.
   * Needed once app icon, startup behavior, and uninstall expectations matter.

3. MSIX / Microsoft Store path
   * More formal distribution.
   * Adds packaging constraints; better after MVP stabilizes.

## Implementation milestones

### Milestone 1: Skeleton and app shell

* WPF app scaffold.
* Tray icon.
* Edge handle window.
* Settings model with defaults.

### Milestone 2: Shelf data and file/folder flow

* Shelf item model.
* JSON persistence.
* File/folder drag-in.
* Card list.
* Open/reveal/remove/clear.

### Milestone 3: Drag-out and copy semantics

* Drag shelf file/folder items out.
* Default copy behavior.
* Ctrl+C / Delete / Enter for selected card.

### Milestone 4: Text/URL/image support

* Paste text/URL.
* Paste/drag images.
* Image copy and thumbnail generation.

### Milestone 5: UX polish and packaging

* Theme modes.
* Density modes.
* Dock edge switching.
* Missing-file state.
* Startup toggle.
* Publish artifact.

## Key risks

* Drag-out behavior can vary between target applications.
* Edge docking must handle taskbar, multi-monitor, DPI scaling, and screen changes.
* Image drag/paste formats differ between source applications.
* WPF styling must be disciplined to feel modern without overbuilding a design system.

## Recommendation

Proceed with WPF + .NET 10 LTS + MVVM-lite. Keep the app as a single WPF executable project plus a test project until the codebase grows enough to justify splitting libraries.
