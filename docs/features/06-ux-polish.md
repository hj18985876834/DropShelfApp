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
7. User filters cards by type.
8. User reorders cards by dragging the type badge area.
9. Keyboard shortcuts work for the selected card.

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
* The type affordance also acts as the internal reorder handle.
* Dragging the type badge area reorders cards inside the shelf with a lifted-card visual state and dynamic movement while dragging.
* Dragging the card body preserves drag-out copy behavior.
* Actions appear on hover.
* Repeated actions should use icon buttons with tooltips where practical.

### Filtering And Cleanup

* A compact filter control supports all, files, folders, text, links, and images.
* Filtering changes only the visible projection; it does not alter persisted records or order.
* A no-results state appears when a non-empty shelf has no cards for the selected filter.
* Missing-source cards continue to show their existing missing/error state; users can remove them with the normal card context menu or selected-item Delete shortcut.

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
* Filtered no-results
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
* Selected card hidden by filter.
* Drag reorder versus drag-out copy gesture separation.

## Acceptance Criteria

* Empty state is clear and minimal.
* Card hover actions do not shift layout.
* Long text truncates without overlap.
* Selection state is visible.
* Type filtering works without crowding the shelf header.
* Card reorder persists through the existing shelf persistence path.
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
* Filter by each item type.
* Delete or move a source externally and verify the card shows missing/error state.
* Reorder cards by dragging the type badge area.
* Verify the dragged card appears lifted and cards move dynamically during reorder.
* Verify card-body drag-out still works after reorder support is added.
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
