# Component Guidelines

> How components are built in this project.

---

## Overview

<!--
Document your project's component conventions here.

Questions to answer:
- What component patterns do you use?
- How are props defined?
- How do you handle composition?
- What accessibility standards apply?
-->

(To be filled by the team)

---

## Component Structure

WPF windows should keep UI event bridging in code-behind and push reusable state
into ViewModels.

For the app shell:

* `App.xaml.cs` composes the singleton shell objects: `ShelfViewModel`,
  `ShelfWindow`, `WindowDockService`, and `TrayIconService`.
* `ShelfWindow` owns WPF window lifecycle details such as `Loaded`, `Closing`,
  `PreviewKeyDown`, and named XAML surfaces.
* `ShelfViewModel` owns shell state and commands such as show, hide, toggle, and
  open settings.
* `WindowDockService` owns screen-edge sizing and positioning.
* `TrayIconService` owns `System.Windows.Forms.NotifyIcon` and tray menu wiring.

Do not duplicate shell visibility state in the tray service or window
code-behind. Treat `ShelfViewModel.IsShelfVisible` as the source of truth.

---

## Props Conventions

<!-- How props should be defined and typed -->

(To be filled by the team)

---

## Styling Patterns

Keep WPF shell styling local until reusable resources exist. The app shell uses
quiet Windows-native colors, compact controls, and no decorative imagery.

When introducing reusable shell styles, move them to `Resources/Styles/` instead
of copying large style blocks between windows.

---

## Accessibility

Shell entry points must remain discoverable without relying only on tray access:

* The edge handle should have a tooltip.
* `Escape` hides the shelf panel when focused.
* Tray commands must expose Show Shelf, Hide Shelf, Settings, and Quit.

---

## Common Mistakes

* Do not set `StartupUri` for the shell. `App.xaml.cs` must construct the shell
  so tray callbacks, settings callbacks, and single-instance handling share the
  same objects.
* Do not put tray, registry, file-system, or screen-positioning side effects
  directly in ViewModels.
* Do not apply shell-affecting settings from every ComboBox or CheckBox property
  change. Settings windows should keep pending values and expose an explicit
  apply command so choosing dock edge, density, theme, or startup options does
  not re-layout the shelf while the control is still handling input.
* Do not synchronously wait on asynchronous persistence from WPF commands or
  window event handlers. Use an async command pattern for user-triggered saves,
  expose an in-flight state for duplicate-click prevention, and let local store
  awaits use `ConfigureAwait(false)` so any unavoidable startup/shutdown sync
  waits cannot deadlock the dispatcher.
* Do not let card-level drag-out handlers process mouse events that originate
  from action buttons inside the card. Check `e.OriginalSource` up the visual
  tree and skip drag logic for command buttons, and require the normal WPF
  minimum drag distance before calling `DragDrop.DoDragDrop`.
* When `UseWindowsForms` is enabled for `NotifyIcon`, use aliases or
  fully-qualified WPF types for overlapping names. This includes
  `Application`, `Clipboard`, `IDataObject`, `DragDrop`, `DragDropEffects`,
  `DragEventArgs`, `MouseEventArgs`, `Grid`, `Color`, and `ColorConverter`.
  Prefer local aliases such as `WpfClipboard = System.Windows.Clipboard` or
  `WpfGrid = System.Windows.Controls.Grid` in WPF code-behind/services.
