# Feature Spec: Text, URL, And Image Input

## Goal

Support non-file shelf items: text, URLs, and pathless images from clipboard or drag/drop sources.

## Branch

`feature/text-url-image-input`

## Dependencies

* Shelf Persistence.
* File / Folder Flow for card rendering patterns.

## User Flow

1. User copies text, a URL, or an image from another app.
2. User opens shelf and pastes into it, or drags supported content into it.
3. Shelf creates a card for the content.
4. User can copy/open/remove the item.
5. Image cards show thumbnails and persist after restart.

## Detailed Behavior

### Text

* Plain text paste creates `ShelfItemType.Text` unless it is recognized as URL.
* Store content locally.
* Card shows first line or short preview.
* Long text cards expand inline when clicked and collapse when clicked again.
* Expansion happens in the same card preview text area: the compact one-line
  preview switches to wrapped full text, without adding a second content box.
* Expanded text uses the normal card width, wraps content, and does not use a
  nested card-local scroll area or duplicate tooltip content.
* Text cards do not show full-content hover tooltips in either compact or
  expanded state; clicking is the way to inspect full text.
* Dragging a text card past the WPF drag threshold starts drag-out and must not
  also toggle expansion on mouse-up.
* Copy action copies full text.

### URL

* URL paste creates `ShelfItemType.Url`.
* Store URL string locally.
* Card shows URL host/title-like display if cheap; otherwise URL preview is acceptable.
* URL cards keep their compact visual form and do not use the text-card
  expand/collapse behavior.
* Open action launches default browser.
* Copy action copies URL.

### Images

For pathless clipboard/dragged bitmap data:

* Save an app-owned original image file under `%LOCALAPPDATA%/DropShelf/images/originals/`.
* Generate thumbnail under `%LOCALAPPDATA%/DropShelf/images/thumbs/`.
* Create `ShelfItemType.Image`.
* Store `ImagePath` and `ThumbnailPath` for app-owned pasted images.
* External image files keep their original path reference and are shown as image cards without duplicating or deleting the source file.
* Card shows thumbnail.
* Removing image item deletes app-owned original and thumbnail.

For image files dragged from Explorer:

* Treat as file item unless the user is pasting/copying pathless bitmap data.
* Do not duplicate image file just because it has an image extension.

## UI States

* Text card
* URL card
* Text card preview expanded inline
* Image thumbnail card
* Image cache missing/corrupt state
* Unsupported clipboard content feedback

## Data Contract

Text item:

```text
Type = Text
Content = text
```

URL item:

```text
Type = Url
Content = URL
```

Image item:

```text
Type = Image
ImagePath = app-owned original image path
ThumbnailPath = app-owned thumbnail path
```

## Edge Cases

* Empty clipboard.
* Whitespace-only text.
* Very large text.
* Invalid URL-like text.
* Clipboard has multiple formats; choose predictable priority:
  1. file drop
  2. image bitmap
  3. URL/text
* Image save fails.
* Thumbnail generation fails.
* Image cache file missing after restart.

## Acceptance Criteria

* Pasting plain text creates text item.
* Pasting URL creates URL item.
* Clicking a text card expands/collapses the existing preview text into full
  wrapped text inline without adding a nested box or scrollbar.
* Dragging a text card out does not also expand/collapse it.
* URL card display and open/copy behavior remain unchanged.
* Copy text/URL works.
* Opening URL launches default browser.
* Pasting image creates image item.
* Image thumbnail displays.
* Image item persists after restart.
* Removing image item removes app-owned image files.
* File image dragged from Explorer remains file path reference.
* Unsupported clipboard/drop content does not crash.
* `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.

## Tests

### Unit / File-System Tests

* URL detection for obvious valid/invalid strings.
* ImageStore creates original and thumbnail paths under app data.
* ImageStore cleanup ignores already-missing files.
* Removing image item deletes app-owned files.
* File image path remains file item when input source is Explorer file drop.

### Manual Windows Tests

* Copy text from Notepad and paste into shelf.
* Click a long text card to expand the preview text in-place, verify hovering
  does not show a full-content tooltip, click it again to collapse it,
  then drag it out and verify drag-out does not toggle expansion.
* Copy URL from browser and paste into shelf.
* Verify the URL card keeps its compact display and still opens in the default
  browser.
* Copy screenshot/image and paste into shelf.
* Restart app and verify image thumbnail reloads.
* Remove image item and verify app-owned image files are gone.

## Files Likely Touched

* `src/DropShelf.App/Services/DragDropService.cs`
* `src/DropShelf.App/Services/ImageStore.cs`
* `src/DropShelf.App/Models/ShelfItem.cs`
* `src/DropShelf.App/ViewModels/ShelfViewModel.cs`
* `src/DropShelf.App/Views/ShelfWindow.xaml`
* `tests/DropShelf.Tests/*`

## Out Of Scope

* OCR.
* Image editing.
* Image resizing beyond thumbnail generation.
* Cloud upload.
* Clipboard history replacement.
