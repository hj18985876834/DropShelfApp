# Desktop Drop Shelf Deep Dive

## Topic

Explore the "Desktop Drop Shelf" direction in more depth: comparable products, Windows opportunity, MVP shape, technical approach, UX risks, and differentiation.

## Comparable products

### Dropshelf for Windows

Dropshelf is a Windows shelf app available through Microsoft Store. Its core flow is: shake the mouse to open a shelf, drop items while dragging, navigate to the destination, then move items out.

Key observations:

* Validates that this workflow exists on Windows.
* Uses Windows App SDK and WinUI 3 according to its public GitHub issue repo.
* Main activation model is shake-to-open, which may be clever but could be less discoverable for mainstream users.

Sources:

* https://apps.microsoft.com/detail/9mzpc6p14l7n
* https://github.com/williamckha/dropshelf-repo

### DropPoint

DropPoint is an open-source cross-platform app that provides a temporary holding area for drag-and-drop operations. Its pitch is to avoid placing windows side-by-side just to drag files.

Key observations:

* Shows the minimal version of this idea can be simple and useful.
* Cross-platform Electron implementation increases package size; this is an opportunity for a lighter Windows-native version.
* The main workflow is "hold files while navigating elsewhere."

Sources:

* https://droppoint.netlify.app/
* https://www.ghacks.net/2022/05/24/droppoint-makes-drag-and-drop-operations-easier/

### Yoink for Apple platforms

Yoink is a mature shelf utility for Apple platforms. It provides an edge shelf for files and app content, follows users across windows/spaces/fullscreen apps, stacks multiple files, supports share/quick actions, clipboard history, and customization.

Key observations:

* Strong product pattern: temporary shelf at screen edge.
* Mature versions expand into share actions, clipboard history, handoff, and shortcuts.
* For our MVP, most of these advanced features should be deferred.

Source:

* https://apps.apple.com/tr/app/yoink-better-drag-and-drop/id457622435

### Dropover for macOS

Dropover is a drag-and-drop utility built around temporary floating shelves. It emphasizes collecting, organizing, sharing, and processing items, including quick actions like resizing images or extracting text.

Key observations:

* Reinforces that the best version is not just "hold files"; it can become an action surface.
* But adding processing actions too early would make V1 too broad.

Sources:

* https://dropoverapp.com/
* https://apps.apple.com/us/app/dropover-easier-drag-drop/id1355679052

## Product opportunity

This is not a blank market, but it remains a good project because:

* The workflow is understandable in seconds.
* Windows does not provide this as a built-in OS-level feature.
* Existing Windows options leave room for a simpler, lighter, more polished local-first implementation.
* The idea can start small and grow through clear extensions.

Recommended positioning:

> A tiny Windows staging shelf for files, images, text, and links, built for moving things between apps without rearranging windows.

## MVP recommendation

### V1 must have

* Shelf window:
  * compact always-on-top floating panel
  * can be shown/hidden from tray or hotkey
  * can be pinned to screen edge
* Input:
  * drag files/folders into shelf
  * drag text/URL/images into shelf if supported by source app
  * paste from clipboard into shelf
* Item cards:
  * file/folder: icon, name, type, path
  * text/link: first line preview
  * image: thumbnail preview
* Output actions:
  * drag item back out to Explorer/app
  * copy file path or text content
  * open item
  * reveal file in Explorer
  * remove item
* Safety/cleanup:
  * clear all
  * auto-expire temporary non-file content after configurable time

### V1 should include if cheap

* Stack multiple dragged files as one grouped card.
* Rename shelf items locally for easier recognition.
* Opacity and compact/comfortable density.

### V1 should not include

* Cloud upload/share links.
* File conversion or compression.
* OCR/image processing.
* Clipboard history replacement.
* Sync across devices.
* Complex multi-shelf workspace system.
* Permanent file library or file manager behavior.

## UX design principles

* The shelf should appear only when useful, then get out of the way.
* Drag-and-drop behavior must feel native and predictable.
* It should never move or delete user files by itself.
* File references should be treated as references, not duplicated copies, unless the user explicitly exports/copies.
* Avoid requiring the user to learn "shake" gestures in V1; use tray + hotkey + optional edge reveal.
* Keep default UI small enough to live near a screen edge without covering work.

## Differentiation options

### Option A: Minimal Native Shelf

Focus on speed, low memory use, clear UX, no cloud, no advanced actions.

Pros:

* Best MVP.
* Easy to explain.
* Least support burden.

Cons:

* Competes mostly on polish.

### Option B: Shelf + Quick Actions

Add small actions such as zip selected files, copy Markdown links, resize images, or share-ready package.

Pros:

* Stronger differentiation.
* Can evolve into a useful productivity surface.

Cons:

* Scope can expand quickly.

### Option C: Shelf + Session Memory

Remember named shelves or recent groups for repeated workflows.

Pros:

* Good for power users.
* More sticky than pure temporary staging.

Cons:

* Risks becoming a file manager or clipboard manager.

## Technical notes

### Windows drag and drop

Windows app frameworks support drag-and-drop between applications using standard drag/drop APIs. WinUI 3 and WPF both have documented drag-and-drop support.

Sources:

* https://learn.microsoft.com/en-us/windows/apps/develop/data/drag-and-drop
* https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/drag-and-drop-overview

### Window behavior

The shelf needs always-on-top or compact overlay behavior and predictable placement near screen edges. Windows App SDK includes window management APIs that support compact overlay / always-on-top style presentation.

Source:

* https://learn.microsoft.com/en-us/windows/apps/develop/ui/manage-app-windows

### Recommended stack

Recommended for V1:

* WPF or Windows App SDK / WinUI 3 in C#.

Reasoning:

* Drag/drop integration is central.
* Native window behavior and tray integration matter.
* A light Windows-only app is a better fit than Electron for this project.

Pragmatic default:

* WPF may be simpler and more proven for classic desktop drag/drop and tray utilities.
* WinUI 3 may look more modern but may involve more friction around shell/tray behavior.

## Key risks

* Dragging items out of the shelf into arbitrary apps may vary by app and data type.
* Edge reveal can conflict with Windows taskbar, multi-monitor layouts, and full-screen apps.
* Text/image/link data formats differ between source apps.
* Users need to understand whether files are copied, moved, or referenced.
* If the app stores references only, missing/deleted source files need clear handling.

## Proposed MVP decision

Start with "Minimal Native Shelf":

* Files/folders first.
* Text/URL/image paste second.
* Drag-out to Explorer/apps where reliable.
* Keep quick actions to copy/open/reveal/remove.
* Defer cloud sharing, conversion, OCR, and clipboard history.
