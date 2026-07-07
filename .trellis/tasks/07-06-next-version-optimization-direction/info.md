# Next Version Optimization Analysis And Design

## Executive Summary

The next version should focus on card management, not new input types. Based on the current roadmap, code structure, and user preference, the recommended `0.1.3` scope is:

1. Type filtering.
2. Dynamic drag reorder.

Search, missing filters, clean-missing buttons, and multi-select are explicitly out for this release.

Recommended release theme:

**0.1.3: Organize and clean cards faster**

## Current Product Baseline

EdgeTuck `0.1.2` has recently improved the card reading and shelf control experience:

* smoother wheel scrolling;
* inline long-text expansion;
* pinned shelf state.

The product now has a solid capture/reuse loop. The next bottleneck is management: after users store mixed card types, they need to narrow the view, clean stale records, and arrange the cards in a useful order.

## Current Architecture Fit

Existing code already supports much of the chosen direction:

* `ShelfViewModel.Items` contains the full persisted card list and order.
* `ShelfItemViewModel.Type` supports type-based filtering.
* `ShelfItemViewModel.IsMissing` already detects and displays missing file/folder/image source state.
* `ShelfViewModel.RemoveItem`, `RemoveSelected`, and `ClearAll` already define safe record-removal semantics.
* `GetShelfItems()` already defines the persistence output path.
* Tests already cover missing state, selection after removal, command enablement, localization, and image-file cleanup behavior.

The main missing pieces are:

* visible list state separate from persisted item state;
* type filter state and localized labels;
* no-results UI state;
* reorder logic that updates the backing `Items` order;
* pointer gesture separation between drag reorder, drag-out copy, and text-card click expansion.
* a compact control strategy that does not crowd the shelf header.

## Direction Comparison

### Type filters

Type filtering is the best lightweight alternative to search for this product direction.

Benefits:

* helps users scan a mixed shelf quickly;
* easy to understand;
* low implementation and data-model cost;
* maps directly to existing `ShelfItemType`;
* avoids adding a text-search interface the user does not want.

Risks:

* filter controls can crowd the narrow shelf if not compact;
* no-results states need clear copy.

Verdict: include in MVP.

### Missing items

Missing items should keep the existing card-level missing/error state. Users can
remove those records through the existing context menu, selected-item `Delete`,
or card remove action. A dedicated missing filter or clean-missing button would
add visible management controls the user does not want in this release.

Verdict: keep existing missing display and remove affordances; do not include a
separate cleanup feature in MVP.

### Drag reorder

Drag reorder gives users direct control over visual order and priority.

Benefits:

* users can move important or active cards to the top;
* improves organization without tags/folders;
* uses the existing collection order as the persisted order.

Risks:

* conflicts with existing drag-out gestures unless carefully separated;
* can interfere with text-card click expand/collapse if pointer state is not tracked;
* filtered views complicate mapping visible indexes back to full collection indexes.

Verdict: include in MVP, but design the gesture deliberately.

### Search

Search is useful in general, but the user does not want it for the next version.

Benefits:

* broad discoverability for large shelves;
* good for remembering text snippets or filenames.

Risks:

* adds text input UI and query semantics;
* creates keyboard/focus behavior questions;
* not aligned with current product preference.

Verdict: out of scope for `0.1.3`.

### Multi-select and batch actions

Multi-select is powerful but changes the selection model.

Benefits:

* batch deletion and future batch copy actions become possible;
* aligns with heavier management workflows.

Risks:

* current UX spec says single selected card at a time;
* keyboard, context menu, card highlight, and commands all need redesign;
* higher chance of accidental deletion unless confirmation/UI is strong.

Verdict: defer.

## Recommended UX Design

### Layout

Add a compact management row under the existing header:

* one type filter dropdown/menu: All, Files, Folders, Text, Links, Images.

The existing card list remains below the management row.

Avoid a large toolbar. The shelf is narrow (`360px` window width), so controls should be compact and stable. Do not add one top-level button per type unless it fits cleanly in both Chinese and English. The default design should be a single filter control, with any extra management actions consolidated or placed near the relevant content.

### Filter behavior

MVP filter set:

* All
* Files
* Folders
* Text
* Links
* Images
Behavior:

* selecting a filter updates the visible list immediately;
* filter is session-only;
* filtering does not modify persisted order;
* clearing back to All restores the full manual order;
* selected item remains selected if still visible;
* if selected item becomes hidden, select the first visible item or clear selection if no results.

### No-results states

There are three distinct states:

* true empty shelf: current empty drop-zone copy remains;
* no cards for selected filter: shelf has items, but none match the active filter;
These states need short localized copy. Avoid instructional paragraphs.

### Drag reorder behavior

The main UX challenge is distinguishing three gestures:

* click a text card to expand/collapse;
* drag a card within the shelf to reorder;
* drag a card out of the shelf to copy it elsewhere.

Two feasible approaches:

**Approach A: Whole-card reorder with boundary-based drag-out**

How it works:

* dragging a card while the pointer remains over the shelf list reorders;
* dragging outside the shelf/list starts or continues drag-out copy;
* click-to-expand only fires when movement stays below threshold.

Pros:

* most direct and discoverable;
* no extra visual control on cards;
* matches natural list reorder expectations.

Cons:

* highest conflict risk with existing drag-out behavior;
* requires precise pointer state and boundary handling.

**Approach B: Dedicated reorder handle on each card**

How it works:

* a small grip/handle area starts internal reorder;
* dragging the rest of the card keeps existing drag-out behavior;
* clicking text content keeps expand/collapse behavior.

Pros:

* clearest separation of gestures;
* lower regression risk for drag-out and text expansion;
* easier to test manually.

Cons:

* adds visual density to already compact cards;
* users must learn that only the handle reorders.

**Approach C: Top-level reorder mode**

How it works:

* a header/menu button toggles a temporary reorder mode;
* while active, dragging cards reorders;
* while inactive, dragging cards keeps drag-out behavior.

Pros:

* very explicit mode separation;
* lowers gesture ambiguity once the mode is active.

Cons:

* adds more header/top controls, which the product should avoid;
* mode state can be forgotten and feel heavy for a lightweight utility;
* adds an extra step before the user can reorder.

Chosen: **Approach B, with the reorder affordance integrated into the existing type badge area**. The badge remains the type affordance, while hover/selected state can make it feel draggable through cursor, tooltip, border/accent, or a subtle grip treatment. Approach C should remain a fallback only if card-local interaction cannot be made reliable.

Reorder should be visually explicit: once the drag crosses the threshold, the
dragged card appears lifted and cards move as the pointer crosses over target
cards. Mouse-up ends the gesture rather than being the first moment where order
changes.

## Technical Design

### ViewModel state

Add filter/reorder state to `ShelfViewModel`:

* `ShelfFilterMode ActiveFilter`
* `VisibleItems` or `ItemsView`
* `VisibleItemCount`
* `HasVisibleItems`
* `IsNoResults`
* `MoveItem(ShelfItemViewModel item, int targetVisibleIndex)` or equivalent

Recommended model:

```text
Items           = full persisted collection and manual order
VisibleItems    = UI projection filtered from Items
GetShelfItems() = Items.Select(item => item.Item)
```

This keeps persistence independent from UI filtering and avoids accidentally saving only filtered items.

`ICollectionView` is acceptable for filtering, but an explicit `ObservableCollection<ShelfItemViewModel> VisibleItems` may be easier to reason about because reorder must map visible positions back to the full backing collection.

### Filter matching

Use an enum:

```text
ShelfFilterMode
All
File
Folder
Text
Url
Image
```

Filtering rules:

* `All`: every item.
* `File`: `Type == ShelfItemType.File`.
* `Folder`: `Type == ShelfItemType.Folder`.
* `Text`: `Type == ShelfItemType.Text`.
* `Url`: `Type == ShelfItemType.Url`.
* `Image`: `Type == ShelfItemType.Image`.
Do not persist this enum for MVP.

### Selection rules

When applying a filter:

1. Recompute visible items.
2. If `SelectedItem` is still visible, keep it.
3. If not visible and there are visible items, select the first visible item.
4. If no visible items, set `SelectedItem = null`.

This keeps keyboard commands aligned with the list the user can see.

### Reorder rules

Reorder must update the full `Items` collection.

For unfiltered view:

* moving item from index A to index B can directly move inside `Items`.

For filtered view:

* visible index must be mapped back to the full collection.
* recommended behavior: allow reorder while filtered, but insert relative to the target visible item in the full collection.
* simpler alternative: disable reorder unless filter is `All`. This reduces complexity but may feel limiting.

Recommended MVP choice: allow reorder in `All` only unless the implementation can make filtered reordering unambiguous quickly.

### Removal rules

Single-card removal continues to remove from full `Items`. Missing records use
the same existing removal paths; there is no dedicated batch cleanup command in
this MVP.

### Persistence

No schema change required.

`ShelfStore.SaveAsync(viewModel.GetShelfItems())` should continue to save all remaining full items in the current manual order.

### Localization

`LocalizationService.AppText` will need new strings:

* filter labels: All, Files, Folders, Text, Links, Images;
* no-results title/body;
* optional reorder handle tooltip.

Language changes should raise property updates for new labels and no-results text.

### XAML

Likely `ShelfWindow.xaml` changes:

* add a compact filter row between header and drop zone/list, preferably with one filter menu/dropdown rather than many buttons;
* bind list `ItemsSource` to visible collection/view instead of full `Items`;
* bind no-results visual state;
* add reorder visual feedback;
* reuse the existing type badge area as the reorder grip/handle, with hover/selected visual feedback.

Keep fixed dimensions and avoid shifting card layout.

## Test Plan

Unit tests:

* `FilterMode_File_ShowsOnlyFileItems`
* `FilterMode_Folder_ShowsOnlyFolderItems`
* `FilterMode_Text_ShowsOnlyTextItems`
* `FilterMode_Url_ShowsOnlyUrlItems`
* `FilterMode_Image_ShowsOnlyImageItems`
* `FilterMode_All_RestoresManualOrder`
* `Filtering_PreservesSelectedVisibleItem`
* `Filtering_SelectsFirstVisibleItemWhenSelectedItemHidden`
* `MoveItem_ReordersBackingItems`
* `MoveItem_PreservesSelectedItem`
* `GetShelfItems_ReturnsFullItemsInManualOrder`
* `LanguageChange_UpdatesFilterAndCleanupText`

Manual Windows validation:

* Add enough mixed cards to require scrolling.
* Filter by each item type.
* Delete or move a source file externally and verify the existing missing/error state appears.
* Drag reorder cards in the full list and restart app to verify order persists.
* Verify the dragged card appears lifted and cards move dynamically while dragging before mouse-up.
* Verify drag reorder does not trigger drag-out accidentally.
* Verify drag-out still works to Explorer/Desktop/other targets.
* Verify long-text click expand/collapse still works and does not fire after reorder drag.
* Verify `Delete`, `Ctrl+C`, `Enter`, drag-in, pin shelf, and clear all.
* Verify light/dark theme and compact/comfortable density.

## Release Boundary

Recommended `0.1.3` release notes should stay user-facing:

* Added type filters for shelf cards.
* Added drag reorder for shelf cards.

Avoid mentioning internal ViewModel or persistence implementation.

## Follow-Up Roadmap

After this MVP, the next best sequence is:

1. Multi-select and batch actions after the selection model is redesigned.
2. Per-card favorite/pin after reorder and filtering are stable.
3. Optional search if users later report type filters are not enough.
4. Global hotkeys as a power-user release.
5. URL/card rename and richer card readability improvements.
