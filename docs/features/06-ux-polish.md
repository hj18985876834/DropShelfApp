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
* Search stacks with the type filter: filter by type first, then match the search query against visible card text such as display name, path, content, URL, or image path.
* Filtering and search change only the visible projection; they do not alter persisted records or order.
* A no-results state appears when a non-empty shelf has no cards for the selected filter/search criteria.
* Missing-source cards continue to show their existing missing/error state; users can remove them with the normal card context menu or selected-item Delete shortcut.

### Batch Import

* External file-drop or clipboard file-drop data can add multiple file, folder, or image cards in one operation.
* Structured clipboard text with multiple non-empty lines is split into separate cards only when every line is an existing file path, existing folder path, or HTTP/HTTPS URL.
* Plain multi-line text remains one text card so copied paragraphs are not split unexpectedly.
* Add feedback summarizes imported card types, such as files, folders, images, text, and links.

### Selection And Keyboard

* Single-card selection remains the primary focus state.
* `Ctrl+Click` toggles a visible card in the batch selection.
* `Shift+Click` selects a visible range from the primary selected card.
* `Ctrl+A` selects all current visible cards.
* `Escape`: collapse shelf.
* `Delete`: remove selected shelf record(s); two or more records require confirmation.
* `Ctrl+C`: copy selected item content/path or copy same-type selected cards.
* `Enter`: open selected item where applicable.
* Mixed-type multi-copy is rejected with a clear status message.
* Same-type multi-copy is dispatched by exact item type: files, folders, and images use file-drop lists; text and URLs use newline-separated text.
* `Ctrl+Alt+Space`: global show/hide shelf shortcut.
* `Ctrl+Alt+V`: global quick paste into the shelf without opening it.

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
* Batch selected cards
* Mixed-type copy warning
* Global hotkey registration failure

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
* Selected card hidden by search.
* Multi-select across filtered/search results.
* Mixed-type multi-copy.
* Global hotkey already registered by another app.
* Drag reorder versus drag-out copy gesture separation.

## Acceptance Criteria

* Empty state is clear and minimal.
* Card hover actions do not shift layout.
* Long text truncates without overlap.
* Selection state is visible.
* Type filtering works without crowding the shelf header.
* Search works with type filtering and does not mutate persisted records.
* Structured multi-line clipboard content imports multiple typed cards when every line is a path or URL.
* Plain multi-line text remains one text card.
* Multi-select supports `Ctrl+Click`, `Shift+Click`, and `Ctrl+A` over the current visible cards.
* Multi-delete requires confirmation for two or more selected records and never deletes original files/folders.
* Multi-copy rejects mixed types and supports type-specific clipboard output for files, folders, images, text, and URLs.
* Global shortcuts show/hide EdgeTuck and add current clipboard content in the background.
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
* Search/filter projection behavior.
* Multi-select delete/copy behavior.

### Manual Windows Tests

* Verify empty shelf.
* Add multiple item types.
* Hover each card type.
* Select each card type.
* Filter by each item type.
* Search by card display name, path, text, URL, and image path.
* Paste multiple copied paths/links and verify separate card creation.
* Paste a normal multi-line paragraph and verify it remains one text card.
* Multi-select with `Ctrl+Click`, `Shift+Click`, and `Ctrl+A`.
* Copy same-type selected cards and verify mixed-type copy warning.
* Press `Ctrl+Alt+Space` to show/hide EdgeTuck from another app.
* Press `Ctrl+Alt+V` with text, URL, file, and image clipboard content.
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
