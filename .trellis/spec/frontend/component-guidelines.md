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

### Shell Docking Position

The shelf handle is movable. Store its position as:

* `AppSettings.DockEdge`: the snapped screen edge.
* `AppSettings.DockOffsetRatio`: a `0.0` to `1.0` ratio along that edge.

For left/right edges, `DockOffsetRatio` represents vertical position from top to
bottom. For top/bottom edges, it represents horizontal position from left to
right. Do not persist absolute pixels for the primary docking position because
screen resolution, scaling, and taskbar placement can change between runs.

`WindowDockService` owns all placement math:

* Position windows from `DockEdge + DockOffsetRatio`.
* Clamp the shelf to `SystemParameters.WorkArea`.
* While dragging, move the collapsed handle with the mouse as one stable
  collapsed handle window and keep it inside the work area.
* On mouse release, snap to the nearest edge and return a `DockPlacement`.

The shell uses two top-level WPF windows:

* `HandleWindow`: owns only the small edge handle, mouse capture, free dragging,
  drop-to-open affordance, and snap callbacks.
* `ShelfWindow`: owns only the expanded shelf panel and positions itself
  relative to `HandleWindow`.

`HandleWindow` should update current app settings through a callback. It must
not write settings files directly.

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
* Do not make handle click and handle drag share the same final action. Use the
  WPF minimum drag distance to distinguish a click from a move, suppress the
  synthetic click after a completed drag, and disable hover expansion while the
  handle is being dragged.
* Do not expand the shelf immediately on handle hover. Near screen corners,
  hover expansion changes the window size and moves the handle away before the
  user can press or drag it. Keep expansion explicit through click or accepted
  external drag-over, and keep handle dragging independent.
* Do not handle accepted external drag-over by toggling shell visibility.
  `DragOver` fires repeatedly while the pointer stays over the handle, so it
  must use an idempotent show action; otherwise the panel flickers open/closed.
* Do not resize, dock, expand, collapse, or re-run snapped placement while a
  handle drag is active. Once the drag threshold is crossed, first convert to a
  collapsed handle window at the handle's current screen position, reset the
  drag baseline, then move the window by mouse delta until release. Snapping
  happens only on mouse-up.
* Do not use corner safe margins as a substitute for stable dragging. Handles
  may snap to ratios `0` or `1`; the reason corners stay usable is that the
  window remains the same collapsed handle size during the entire drag.
* Do not use WPF `Window.DragMove()` for the shelf handle. It gives control to a
  system drag loop after the app has already changed shelf layout, which can
  move the handle out from under the cursor near corners. Use captured mouse
  movement in WPF device-independent coordinates instead.
* Do not mix `PointToScreen` physical-pixel coordinates with WPF `Window.Left`
  / `Window.Top` device-independent pixels for shell dragging. Convert through
  the window's `CompositionTarget` before assigning WPF window coordinates.
* Do not collapse a hover-expanded shelf on the first `MouseLeave` event. When
  layout changes during top/bottom expansion, WPF can emit transient leave
  events. Use a short delay and confirm `IsMouseOver == false` before
  auto-collapsing hover expansion.
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
* Do not call `DragDrop.DoDragDrop` with an empty or app-private-only data object
  to simulate a blocked external file drag. Windows targets will not treat that
  as a real file drag, so the user sees a broken drag interaction. If a
  file/folder is blocked by the DropShelf size limit, show card feedback at the
  drag threshold and do not start external drag/drop.
* When `UseWindowsForms` is enabled for `NotifyIcon`, use aliases or
  fully-qualified WPF types for overlapping names. This includes
  `Application`, `Clipboard`, `IDataObject`, `DragDrop`, `DragDropEffects`,
  `DragEventArgs`, `MouseEventArgs`, `Grid`, `Color`, and `ColorConverter`.
  Prefer local aliases such as `WpfClipboard = System.Windows.Clipboard` or
  `WpfGrid = System.Windows.Controls.Grid` in WPF code-behind/services.
