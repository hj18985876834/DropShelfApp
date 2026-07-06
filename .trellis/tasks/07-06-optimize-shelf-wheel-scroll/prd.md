# Optimize Shelf Mouse Wheel Scrolling

## Goal

Improve the shelf item list scrolling experience so a light mouse wheel movement does not jump an excessive distance through card items.

## What I Already Know

- The shelf list is a WPF `ListBox` in `src/DropShelf.App/Views/ShelfWindow.xaml`.
- The current list sets scroll bar visibility but does not specify pixel-based scroll units or virtualization mode.
- Shelf items are card-style templates with variable visual height, especially when long text is expanded.
- WPF mature usage for variable-height item lists is to prefer pixel-based virtualized scrolling before writing custom wheel handling.

## Requirements

- Reduce mouse wheel jump distance in the shelf item list.
- Preserve normal scrollbar behavior and keyboard/list selection behavior.
- Keep the change scoped to the shelf list unless verification shows another scroll surface has the same issue.
- Preserve virtualization-friendly WPF list behavior.

## Acceptance Criteria

- [ ] The shelf item `ListBox` scrolls by pixels instead of whole card items.
- [ ] Virtualization remains explicitly enabled with container recycling.
- [ ] The application builds successfully.
- [ ] No unrelated user work is reverted or reformatted.

## Out of Scope

- Adding a user-facing setting for wheel sensitivity.
- Redesigning shelf cards or long-text expansion behavior.
- Replacing `ListBox` with a custom scrolling control.

## Research References

- [`research/wpf-listbox-wheel-scrolling.md`](research/wpf-listbox-wheel-scrolling.md) - WPF official API guidance and recommended approach.
