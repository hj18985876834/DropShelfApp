# Feature Spec: UX Polish

## Goal

Make the MVP feel like a quiet Windows-native utility: clear, compact, predictable, and low-distraction.

## Branch

`feature/ux-polish`

## Dependencies

* App Shell.
* File / Folder Flow.
* Text / URL / Image Input for final card types.

## User Flow

1. User sees a compact edge handle.
2. User opens shelf.
3. Empty state clearly explains the drop target.
4. Items appear as stable cards.
5. Hover reveals actions.
6. Clicking a card selects it.
7. Keyboard shortcuts work for the selected card.

## Detailed Behavior

### Visual Style

* Windows-native / Fluent-inspired.
* Neutral surfaces.
* System-like typography.
* Subtle borders and shadows.
* Restrained accent color.
* No heavy gradients, illustrations, playful animations, or marketing layout.

### Empty State

* Simple drop zone.
* Short localizable copy: "Drop files, text, or images here".
* No tutorial overlay in V1.

### Cards

* Stable card dimensions.
* Long filenames truncate cleanly.
* Tooltip or accessible full text where practical.
* Type affordance is visible even when actions are hidden.
* Actions appear on hover.
* Repeated actions should use icon buttons with tooltips where practical.

### Selection And Keyboard

* Single selected card at a time.
* `Escape`: collapse shelf.
* `Delete`: remove selected shelf record.
* `Ctrl+C`: copy selected item content/path.
* `Enter`: open selected item where applicable.

### Motion

Motion should be short and functional:

* expand/collapse: about 120-180 ms if implemented
* item add/remove: subtle fade/slide if cheap
* drag-over accepted: clear border/accent state

No long or attention-grabbing animation.

## UI States

* Handle idle
* Handle hover
* Shelf empty
* Drag-over accepted
* Drag-over unsupported
* Card idle
* Card hover
* Card selected
* Card missing/error
* Compact density
* Comfortable density
* Light theme
* Dark theme

## Data Contract

No data schema changes required, except if UI preferences are added through Settings.

## Edge Cases

* Very long filename.
* Very long text preview.
* High DPI scaling.
* Narrow panel.
* Many items causing scroll.
* Missing image thumbnail.
* Keyboard focus after item removal.

## Acceptance Criteria

* Empty state is clear and minimal.
* Card hover actions do not shift layout.
* Long text truncates without overlap.
* Selection state is visible.
* Keyboard commands work.
* UI remains usable in light and dark themes.
* Compact/comfortable density works if Settings is available.
* No decorative UI elements are added outside MVP style.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.

## Tests

### Unit Tests

* ViewModel selection behavior.
* Command enabled/disabled state.

### Manual Windows Tests

* Verify empty shelf.
* Add multiple item types.
* Hover each card type.
* Select each card type.
* Use `Escape`, `Delete`, `Ctrl+C`, `Enter`.
* Try long filenames and long text.
* Switch light/dark theme.
* Verify at common DPI scaling values if available.

## Files Likely Touched

* `src/DropShelf.App/Views/ShelfWindow.xaml`
* `src/DropShelf.App/ViewModels/ShelfViewModel.cs`
* `src/DropShelf.App/ViewModels/ShelfItemViewModel.cs`
* `src/DropShelf.App/Resources/Styles/*`
* `src/DropShelf.App/Resources/Themes/*`
* `src/DropShelf.App/Converters/*`
* `tests/DropShelf.Tests/*`

## Out Of Scope

* Brand-heavy visual identity.
* Custom theme builder.
* Complex onboarding.
* UI automation tests.
