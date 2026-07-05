# Feature Spec: Drag-Out Copy

## Goal

Allow users to drag shelf items out to Explorer/Desktop while preserving the V1 safety rule: drag-out copies by default and does not move original files.

## Branch

`feature/drag-out-copy`

## Dependencies

* File / Folder Flow.

## User Flow

1. User has a file or folder card in the shelf.
2. User drags the card out to Desktop or Explorer.
3. Target receives the item as a copy operation.
4. Original source remains in place.
5. Shelf record remains unless user removes it explicitly.

## Detailed Behavior

### Drag Source

* Dragging can start from the whole card or a clear drag affordance.
* Drag payload for file/folder records should use the original `SourcePath`.
* Preferred drag effect is copy.
* Shelf should not remove record after drag-out.
* All drag gestures started from DropShelf should include an internal drag marker
  so dropping back onto DropShelf does not create duplicate shelf records.
* When drag-out is blocked by size, show a non-blocking card status message as
  soon as the drag threshold is crossed. Do not call WPF `DoDragDrop` for the
  oversized item.

### Copy Semantics

* Default drag effect should be copy.
* Do not implement move behavior in V1.
* Do not delete original source after drag-out.
* Do not delete shelf record after drag-out.
* V1 drag-out size limit is 512 MB per item. Folder size is the recursive sum of
  contained files.

### Target Compatibility

Primary targets:

* Desktop
* File Explorer folders

Secondary targets:

* Apps that accept file paths through standard Windows drag/drop

## UI States

* Card drag started
* Drag threshold crossed with size-limit feedback
* Drag visual or cursor indicates copy if feasible
* Drag canceled
* Drag completed

## Data Contract

No new shelf data fields required.

## Edge Cases

* Source path missing before drag starts: drag should be disabled or show feedback.
* Source inaccessible: drag should not crash.
* Source size cannot be read: show feedback and do not start external drag/drop.
* Source exceeds size limit: show feedback and do not start external drag/drop.
* User drops into an unsupported target.
* User cancels drag with Escape.
* User drags a DropShelf card back into DropShelf: app should ignore the drop and
  not create a duplicate record.
* Multiple selected items are out of scope unless multi-select is added later.

## Acceptance Criteria

* File card can be dragged to Desktop/Explorer.
* Folder card can be dragged to Desktop/Explorer.
* Operation copies by default where target honors drag effect.
* Original source remains.
* Shelf record remains.
* Missing source cannot be dragged as if valid.
* Oversized source shows a clear status message when the user attempts to drag
  it out, and no external drag/drop operation starts.
* Dragging a shelf card back onto DropShelf does not add a duplicate card.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.

## Tests

### Unit Tests

* Drag payload builder returns file drop data for valid source paths.
* Missing source path returns disabled/invalid result.
* Oversized file/folder returns an invalid result with a user-facing message.
* Internal DropShelf drag payloads are ignored by drop-in creation logic.

### Manual Windows Tests

* Drag file card to Desktop.
* Drag folder card to Explorer.
* Verify original still exists.
* Verify shelf card still exists.
* Cancel drag and verify nothing changes.

## Files Likely Touched

* `src/DropShelf.App/Services/DragDropService.cs`
* `src/DropShelf.App/ViewModels/ShelfItemViewModel.cs`
* `src/DropShelf.App/Views/ShelfWindow.xaml`
* `src/DropShelf.App/Views/ShelfWindow.xaml.cs`
* `tests/DropShelf.Tests/*`

## Out Of Scope

* Move operations.
* Multi-select drag.
* Dragging generated zip packages.
* Dragging cloud/share links.
