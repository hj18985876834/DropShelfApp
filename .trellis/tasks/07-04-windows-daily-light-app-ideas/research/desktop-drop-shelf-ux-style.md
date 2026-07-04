# Desktop Drop Shelf UX and Visual Style

## Topic

Define UX, visual style, interaction feel, and usability quality bar for the local-only Windows Desktop Drop Shelf MVP.

## Design references

### Windows app design

Microsoft's Windows app design guidance emphasizes that Windows apps should look, feel, and behave consistently with the platform, covering layout, navigation, input, typography, motion, materials, and related foundations.

Sources:

* https://learn.microsoft.com/en-us/windows/apps/design/
* https://learn.microsoft.com/en-us/windows/apps/design/guidelines-overview

### Fluent 2

Fluent 2 provides Microsoft's current design system direction. For this product, Fluent should be treated as a restraint system: familiar controls, clear spacing, platform typography, subtle surfaces, and predictable interaction behavior.

Sources:

* https://fluent2.microsoft.design/
* https://developer.microsoft.com/en-us/fluentui

### Motion

Microsoft's motion guidance frames motion as functional: it should identify next steps, inform users about UI changes, and make transitions understandable. For this app, motion should clarify shelf expansion/collapse and item insertion/removal, not decorate the product.

Sources:

* https://fluent2.microsoft.design/motion
* https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/motion
* https://learn.microsoft.com/en-us/windows/apps/design/motion/timing-and-easing

## UX principles

### 1. Quiet by default

The shelf is a helper, not the user's main workspace. It should stay visually present enough to be discoverable, but not compete with app content.

Implications:

* Edge handle should be compact and low-contrast.
* Expanded shelf should be narrow by default.
* Avoid bright colors, heavy shadows, large titles, decorative illustrations, or marketing-style empty states.

### 2. Predictable data semantics

Users must understand what the shelf does to their content.

Implications:

* File/folder items are references.
* Image items copied into the app should feel app-owned and removable.
* Clear/delete wording must avoid implying original file deletion.
* Missing-file state should be explicit and calm.

### 3. Drag-first, mouse-friendly

The core interaction is dragging things in and out. The UI must prioritize drop targets, item affordances, and reliable hit areas.

Implications:

* Handle target must be easy to acquire.
* Expanded shelf must have a clear drop zone.
* Cards need stable dimensions to avoid layout shifts while dragging.
* Hover and selected states should be visible but restrained.

### 4. Minimal settings

V1 should include only settings that affect daily use:

* dock edge
* start with Windows
* theme mode if cheap: system / light / dark

Avoid deep customization in V1.

## Visual direction

Recommended V1 style:

* Windows-native / Fluent-inspired.
* Compact utility, not dashboard.
* Neutral background aligned with system theme.
* 6-8 px corner radius for cards and panels.
* Subtle border plus light shadow/elevation for the expanded shelf.
* Accent color only for active drop state and important focus/selection states.
* Typography close to Windows defaults: Segoe UI or framework default.
* Dense but readable card layout.

## Core surfaces

### Edge handle

* Small tab attached to chosen screen edge.
* Shows a simple shelf/tray icon or stack mark.
* Low-contrast idle state.
* Slight accent or elevation on hover.
* Tooltip: "Open shelf" / localized copy later.

### Expanded shelf

* Width/height constrained so it does not dominate the screen.
* Header row:
  * small title or icon
  * item count
  * clear-all button
  * close/collapse button
* Main area:
  * drop zone when empty
  * scrollable card list when populated
* Footer optional:
  * tiny hint or settings entry only if necessary

### Item cards

Card types:

* File/folder: file icon, name, short path or parent folder, type indicator.
* Text/URL: first line preview, small type label.
* Image: thumbnail, dimensions if cheap, source indicator.

Card actions:

* Primary: drag handle / drag whole card.
* Secondary: copy, open, reveal, remove.
* Use icons with tooltips where possible.

## Motion

Use restrained functional motion:

* Shelf expand/collapse: 120-180 ms.
* Item added: short fade/slide into list.
* Item removed: short fade/height collapse.
* Drop accepted: brief border/accent state.

Avoid:

* bouncing
* playful physics
* long animations
* repeated attention-grabbing effects

## States

V1 should explicitly design these states:

* Empty: simple drop target, no marketing copy.
* Drag-over: clear active drop border/background.
* Populated: compact scrollable list.
* Missing file: item remains visible with disabled/open-failed state and remove action.
* Image missing/corrupt cache: show placeholder and remove action.
* Unsupported drop: short inline feedback or toast.
* Too many items visually: scroll, not auto-delete.

## Accessibility and ergonomics

* Minimum interactive target should be comfortable for mouse use; avoid tiny delete buttons without hover/focus support.
* Keyboard basics: Escape collapses shelf; Delete removes selected item if selection exists; Ctrl+C copies selected item if practical.
* Respect system light/dark mode if implementation cost is reasonable.
* Avoid relying on color alone for missing/error states.
* Text must fit compact cards without overlap; long filenames should truncate with tooltip.

## Localization

The user said the app should be local-only, not necessarily Chinese-only. For UX copy, keep strings centralized so the app can later support Chinese and English.

V1 can choose one language for initial implementation, but should avoid hard-coded strings scattered through UI code.

## Recommendation

Use a quiet Windows-native utility style:

* Looks at home on Windows 11.
* Feels small and dependable.
* Prioritizes drag/drop clarity over visual flourish.
* Uses motion only to explain expansion, drop, add, and remove.
