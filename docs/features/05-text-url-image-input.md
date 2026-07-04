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
* Copy action copies full text.

### URL

* URL paste creates `ShelfItemType.Url`.
* Store URL string locally.
* Card shows URL host/title-like display if cheap; otherwise URL preview is acceptable.
* Open action launches default browser.
* Copy action copies URL.

### Images

For pathless clipboard/dragged bitmap data:

* Save an app-owned original image file under `%LOCALAPPDATA%/DropShelf/images/originals/`.
* Generate thumbnail under `%LOCALAPPDATA%/DropShelf/images/thumbs/`.
* Create `ShelfItemType.Image`.
* Store `ImagePath` and `ThumbnailPath`.
* Card shows thumbnail.
* Removing image item deletes app-owned original and thumbnail.

For image files dragged from Explorer:

* Treat as file item unless the user is pasting/copying pathless bitmap data.
* Do not duplicate image file just because it has an image extension.

## UI States

* Text card
* URL card
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
* Copy URL from browser and paste into shelf.
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
