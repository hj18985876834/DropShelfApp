# Analyze Next Version Optimization Direction

## Goal

Decide the highest-return optimization direction for the next EdgeTuck release and turn it into a concrete product/technical design. The next release should improve day-to-day card management without adding new input types.

## What I Already Know

* Current released baseline is EdgeTuck `0.1.2`.
* `0.1.2` focused on interaction quality: smoother shelf wheel scrolling, inline long-text expand/collapse, and a persistent shelf pin button.
* `docs/feature-tasks.md` already recommends the next release theme as "Card Management Efficiency".
* Candidate `0.1.3` scope listed in the roadmap:
  * drag reorder cards;
  * search or filter by text, URL, file name, or folder name;
  * multi-select cards and remove selected items in one action;
  * detect and clean up missing file/folder path records.
* User preference for the next release:
  * search is not needed;
  * filtering by item type is needed;
  * missing items should keep their existing card-level error display and normal remove affordances;
  * drag reorder is needed.
* Current architecture is a local-only WPF app using MVVM-lite:
  * `ShelfViewModel.Items` is the central persisted card collection.
  * `ShelfItemViewModel` already exposes type, display text, preview text, metadata, missing-path state, and card commands.
  * `ShelfWindow.xaml` renders one compact `ListBox` of cards.
  * Missing file/folder/image records are already detectable through `IsMissing`, but only single-card removal is available.
  * Current drag-out behavior starts after crossing WPF drag distance, which matters because drag reorder will share similar pointer gestures.

## Recommendation

The best next-version optimization direction is:

**Card Management Efficiency, with MVP scope centered on type filtering and dynamic drag reorder.**

This direction keeps the release focused on visible, practical management improvements:

* type filters make a mixed shelf easier to scan without introducing a full search box;
* missing records keep their existing error display and can be removed through the existing context menu or selected-item shortcut;
* drag reorder gives users manual control over priority and working order.

## Why This Direction Wins

### User value

Type filtering helps users narrow a mixed shelf by intent: files, folders, text, links, and images. It is less open-ended than search, but simpler and faster when the user remembers what kind of thing they stored.

Missing records should not need a dedicated top-level cleanup feature in this release. The app already shows missing state calmly, and users can remove those records through the existing right-click remove action or selected-item `Delete`.

Drag reorder gives users the most direct organization mechanism: important cards can be moved to the top, and temporary work sequences can be arranged manually.

### UX quality

The interaction can stay quiet and Windows-native:

* one compact type filter dropdown/menu near the top of the shelf rather than many top-level buttons;
* card-local drag reorder affordance inside each card instead of a global reorder mode button;
* existing card layout remains stable.

This avoids a heavier search interface while still improving card management.

### Implementation fit

This direction fits the current code structure, with one important risk area:

* `ShelfItemViewModel.Type` already supports type filtering.
* `ShelfItemViewModel.IsMissing` already supports missing/error display.
* `ShelfViewModel.Items` order can represent the persisted manual order.
* Existing tests cover selection, removal, missing state, localization, and commands.
* Drag reorder must be carefully separated from existing drag-out copy gestures.

## Candidate Scoring

Scores use 1-5 where 5 is best. "Cost Fit" is inverted: 5 means lower implementation/risk cost.

| Direction | User Value | UX Lift | Cost Fit | Risk Fit | Total | Verdict |
|---|---:|---:|---:|---:|---:|---|
| Type filters | 4 | 4 | 5 | 5 | 18 | Must do |
| Drag reorder cards | 4 | 4 | 3 | 3 | 14 | Must do, but isolate gesture risk |
| Missing-item cleanup | 2 | 2 | 5 | 5 | 14 | Out for this release |
| Search cards | 3 | 4 | 4 | 4 | 15 | Explicitly out for this release |
| Multi-select + batch remove | 4 | 3 | 2 | 3 | 12 | Defer |
| Global hotkeys | 4 | 4 | 2 | 2 | 12 | Future release |
| URL title/domain summaries | 3 | 4 | 2 | 3 | 12 | Future card readability release |
| Image workflow preview improvements | 3 | 3 | 3 | 3 | 12 | Future image workflow release |
| Multi-monitor/DPI shell behavior | 4 | 4 | 1 | 2 | 11 | Targeted reliability task |

## Proposed 0.1.3 Theme

**Organize and clean cards faster.**

User-facing release framing:

* Filter shelf cards by type.
* Reorder cards by dragging them inside the shelf.
* Missing source records continue to show card-level error state and can be removed through existing actions.

## Requirements

### Type Filtering

* Add one compact type filter control near the top of the shelf.
* Filter options:
  * all;
  * files;
  * folders;
  * text;
  * links;
  * images;
* Type filtering is session-only for MVP and should not be persisted.
* Filtering must not modify persisted shelf order or shelf data.
* Empty filtered state should distinguish "no cards yet" from "no cards for this filter".
* Selected item should remain selected if still visible; otherwise move selection to the first visible result or clear it.
* Search input, fuzzy search, and text query matching are out of scope for this release.
* Avoid adding one top-level button per type if it crowds the header/panel. Prefer a dropdown, menu button, or single compact segmented control only if it fits cleanly.

### Missing Items

* Do not add a visible clean-missing button.
* Do not add a missing option to the type filter.
* Missing file/folder/image cards should keep the existing missing/error display.
* Users can remove missing records through the existing card context menu, selected-item `Delete`, or card remove action.

### Drag Reorder

* Users can drag cards within the shelf list to reorder them.
* The new order is the order returned by `GetShelfItems()` and is persisted by the existing shelf save path.
* Reorder works for all item types.
* Reorder must not create duplicate records or lose records.
* Reorder must preserve the selected item when possible.
* Drag reorder must not break drag-out copy to other apps.
* Drag reorder must not trigger text-card expand/collapse accidentally.
* Reorder should provide dynamic visual feedback: the dragged card appears lifted and cards move during drag instead of waiting until mouse-up.
* MVP does not require multi-select reorder.
* Drag reorder should avoid a global top-level "reorder mode" button unless card-local interaction proves unworkable.
* Preferred interaction: integrate the reorder grip/handle into the existing type badge area, so users can reorder without adding more controls to the shelf header or crowding the card action area.
* The type badge area should remain recognizable as the item-type affordance, but on hover/selection it can visually indicate that it is draggable for reorder.
* Dragging ordinary card content should preserve existing behavior: text cards can still click-expand, and dragging the card body out can still copy to other apps.

### UX

* Keep the shelf compact and stable.
* Existing shortcuts remain:
  * `Delete`: remove selected visible card.
  * `Ctrl+C`: copy selected visible card.
  * `Enter`: open selected visible card.
  * `Escape`: collapse shelf.
* Drag/drop into the shelf remains available.
* Drag-out from cards remains available.
* Filter controls should not crowd the existing header buttons.
* Do not add many top-level buttons. If multiple controls are needed, consolidate them into one compact management control or move affordances into the card row.
* No search box should be added in this release.

## Acceptance Criteria

* [ ] Users can filter cards by all, file, folder, text, link, and image.
* [ ] Filtering does not change persisted shelf order or shelf data.
* [ ] A no-results state appears when the shelf has items but none match the selected filter.
* [ ] Missing cards continue to show missing/error state and remain removable through existing remove actions.
* [ ] Users can drag a card to a new position in the shelf.
* [ ] The dragged card appears lifted during reorder.
* [ ] Cards dynamically move during drag reorder before mouse-up.
* [ ] Reordered card order persists after app restart.
* [ ] Drag reorder does not break existing drag-out copy behavior.
* [ ] Drag reorder does not accidentally toggle long-text expansion.
* [ ] Existing add, remove, clear all, copy, open, reveal, pin, filtering, and shelf persistence behavior remains coherent.
* [ ] Selection and keyboard commands target visible cards predictably.
* [ ] Chinese and English UI text is localized.
* [ ] Unit tests cover type filtering, selection after filtering, and reorder collection behavior.
* [ ] Manual Windows validation covers drag reorder versus drag-out gesture separation.
* [ ] `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.

## Definition Of Done

* Requirements and UX copy are documented.
* Unit tests cover ViewModel behavior.
* Manual Windows tests cover pointer gesture behavior.
* Feature docs and release notes are updated if implementation proceeds.
* Data-safety behavior is explicit: no original files/folders are deleted.

## Out Of Scope

* Search box, fuzzy search, text query matching, scoring, or match highlighting.
* Missing filter and clean-missing button.
* Multi-select and batch arbitrary deletion.
* Tags, folders, groups, favorites, or pinning individual cards.
* Persisted filter preferences.
* URL metadata fetching or web page title lookup.
* Global hotkeys.
* Cloud sync or sharing.

## Technical Approach

Use the existing full `Items` collection as the source of truth for persistence and manual order. Add a visible projection for filtering, and ensure reordering updates the backing `Items` collection, not just the visible projection.

Recommended ViewModel additions:

* `ShelfFilterMode ActiveFilter`
* `VisibleItems` or `ItemsView`
* `VisibleItemCount`
* `HasVisibleItems`
* `IsNoResults`
* `MoveItemCommand` or `MoveItem(source, target)` method

Recommended filtering model:

```text
Items           = full persisted collection and manual order
VisibleItems    = UI projection filtered from Items
GetShelfItems() = Items.Select(item => item.Item)
```

If `ICollectionView` makes drag reorder harder, prefer an explicit `ObservableCollection<ShelfItemViewModel> VisibleItems` and a well-tested synchronization method.

Drag reorder should be implemented with explicit gesture separation:

* dragging from the card-local reorder handle reorders within the shelf list;
* cards move dynamically while dragging over another card;
* dragging from the card body continues to use the existing drag-out payload behavior;
* movement threshold and text-card click handling must remain distinct.
* a top-level reorder-mode toggle is a fallback only if card-local affordances cannot produce reliable behavior.

## Decision (ADR-lite)

**Context**: The roadmap initially listed search/filter, missing cleanup, drag reorder, and multi-select as candidate card-management improvements. User feedback clarified that search is not needed for the next release, while type filtering, missing cleanup, and drag reorder are needed.

**Decision**: Scope `0.1.3` around type filtering and dynamic drag reorder. Exclude search, missing filter, clean-missing button, and multi-select. Avoid adding many top-level controls; prefer a single compact filter control and a card-local reorder affordance integrated into the existing type badge area.

**Consequences**: The release becomes more tactile and organization-oriented. Implementation risk shifts from query/filter complexity to pointer gesture correctness, especially separating reorder from drag-out and click-to-expand behavior. Reusing the type badge area keeps the shelf header clean and avoids adding card-side clutter, but implementation must keep the badge's type identity clear.

## Technical Notes

Likely impacted files for a future implementation task:

* `src/DropShelf.App/ViewModels/ShelfViewModel.cs`
* `src/DropShelf.App/ViewModels/ShelfItemViewModel.cs`
* `src/DropShelf.App/Views/ShelfWindow.xaml`
* `src/DropShelf.App/Views/ShelfWindow.xaml.cs`
* `src/DropShelf.App/Services/LocalizationService.cs`
* `tests/DropShelf.Tests/ShelfViewModelTests.cs`
* `docs/features/06-ux-polish.md`
* `docs/features/04-drag-out-copy.md`
* `docs/feature-tasks.md`

## Final UX Decision

The reorder affordance lives in the existing type badge area. The badge continues to identify item type, and hover/selected states can add a grip cursor, subtle visual state, or tooltip to communicate that dragging this area reorders the card. The rest of the card keeps existing behavior for selection, text expansion, and drag-out copy.
