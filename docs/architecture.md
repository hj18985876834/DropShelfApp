# DropShelf Architecture

DropShelf is a local-only Windows WPF utility.

## Layers

* Views: XAML windows and minimal UI event bridging.
* ViewModels: UI state, selection state, and commands.
* Models: serializable shelf items and app settings.
* Services: persistence, image storage, drag/drop interpretation, tray, startup, theme, and docking boundaries.

## Data Rules

* File and folder items store original paths only.
* Text and URL items store local records.
* Clipboard or pathless images are stored as app-owned local image copies.
* Clearing shelf records never deletes original user files.
* Removing image records deletes app-owned image files.
