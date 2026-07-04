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
* When enabling Windows Forms for `NotifyIcon`, use aliases or fully-qualified
  WPF types where needed to avoid `Application` name collisions.
