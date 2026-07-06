# WPF ListBox Mouse Wheel Scrolling

## Question

How should a WPF `ListBox` with card-style variable-height items reduce excessive mouse wheel movement while preserving mature `ItemsControl` behavior?

## Findings

- WPF `ScrollViewer.CanContentScroll` controls whether a `ScrollViewer` delegates scrolling to an `IScrollInfo` implementation. For `ItemsControl`-based lists this commonly means logical item scrolling through the items host.
- `VirtualizingPanel.ScrollUnit` controls whether virtualization-backed scrolling is item-based or pixel-based. `ScrollUnit="Pixel"` is the mature fit for variable-height card lists because a wheel step can land within an item instead of jumping to the next whole item.
- `VirtualizingPanel.VirtualizationMode="Recycling"` is a mature default for larger item lists because item containers can be reused while scrolling.
- Setting `ScrollViewer.CanContentScroll="False"` can force physical scrolling, but for `ListBox` it is less desirable as a first choice because it can bypass virtualization-oriented item scrolling behavior.
- Custom `PreviewMouseWheel` handling gives exact control over per-notch pixel movement, but it is a heavier customization and should be reserved for cases where pixel scroll unit still feels too fast.

## Sources

- Microsoft Learn: `ScrollViewer.CanContentScroll`
  https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.scrollviewer.cancontentscroll
- Microsoft Learn: `VirtualizingPanel.ScrollUnit`
  https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.virtualizingpanel.scrollunit
- Microsoft Learn: `VirtualizingPanel.VirtualizationMode`
  https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.virtualizingpanel.virtualizationmode
- Microsoft Learn: What's new in WPF 4.5
  https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/framework45

## Recommendation

For the shelf list, first enable pixel-based virtualized scrolling:

```xml
ScrollViewer.CanContentScroll="True"
VirtualizingPanel.IsVirtualizing="True"
VirtualizingPanel.VirtualizationMode="Recycling"
VirtualizingPanel.ScrollUnit="Pixel"
```

If manual testing still shows movement that feels too large, add a scoped `PreviewMouseWheel` handler to cap each wheel notch to a small pixel delta.
