# DropShelf Visual And Motion Optimization

## Topic

How to improve DropShelf's visual quality and motion experience while keeping it a quiet, Windows-native drag/drop utility.

## Sources

* Microsoft Windows app design guidelines: https://learn.microsoft.com/en-us/windows/apps/design/guidelines-overview
* Fluent 2 motion: https://fluent2.microsoft.design/motion
* Windows timing and easing: https://learn.microsoft.com/en-us/windows/apps/design/motion/timing-and-easing
* Dropshelf for Windows: https://github.com/williamckha/dropshelf-repo
* DropPoint: https://droppoint.netlify.app/
* Yoink: https://eternalstorms.at/yoink/mac/
* Dropover: https://dropoverapp.com/
* Unclutter: https://unclutterapp.com/

## Takeaways From Design Guidance

* Motion should be functional: explain relationship, state change, and next
  action. It should not be decorative.
* Use short, consistent durations. Windows/WinUI references include fast values
  around 83 ms, 167 ms, and 250 ms; DropShelf should usually stay in the
  120-180 ms range for shelf transitions and 80-140 ms for small feedback.
* Easing matters more than duration alone. Enter/show should decelerate; exit
  should feel like it gets out of the way.
* Use color, elevation, geometry, and iconography to make controls readable and
  predictable.

## Takeaways From Comparable Shelf Apps

* Yoink and DropPoint focus on freeing the mouse: put items down temporarily,
  navigate elsewhere, then drag them out.
* Dropover emphasizes temporary shelves that appear only when needed, supports
  multiple content types, and exposes power features later through shortcuts,
  pinned shelves, and action menus.
* Dropshelf validates the Windows-native version of this product shape: a drag
  shelf for Windows using WinUI/Windows App SDK, shake-to-open, and Fluent-style
  visual language.
* Unclutter's useful pattern is a hidden drawer with a gesture: the product
  remains out of the way until the user expresses intent.

## Current DropShelf Fit

DropShelf already has the right architecture for polished motion:

* A stable `HandleWindow` owns the edge tab, drag, hover, and external drop
  affordance.
* `ShelfWindow` owns the expanded panel and currently animates show/hide with a
  short translate + opacity transition.
* Theme resources and density resources already exist, so visual refinement can
  be centralized rather than scattered.

Current polish gaps:

* The handle uses text letters instead of a recognizable shelf/stack icon.
* Card actions are text initials instead of icon buttons.
* Header lacks a stronger information hierarchy: count, clear all, settings,
  and collapse affordances should be stable.
* Card hover state changes abruptly; action reveal and selection could be more
  deliberate.
* Drag-over feedback is color-only and should add shape/elevation/inline state.
* Add/remove item transitions are not yet animated.

## Recommended Optimization Direction

### Phase 1: Interaction Motion Baseline

* Keep first launch collapsed.
* Keep explicit click expansion separate from hover/drag transient expansion.
* Use consistent durations:
  * handle hover: 80-100 ms
  * shelf expand/collapse: 140-180 ms
  * drag-over feedback: 80-120 ms
  * item add/remove: 120-160 ms
* Animate only opacity, transform, and lightweight brush/elevation changes.
* Avoid layout-size animation for the top-level shell window during dragging.

### Phase 2: Visual System Cleanup

* Replace `D/S` handle text with a simple shelf/stack icon mark.
* Replace `C/O/R/X` buttons with icon buttons and tooltips.
* Introduce stable header:
  * app name or icon
  * item count
  * clear all
  * settings
  * collapse
* Add subtle panel elevation/shadow and keep 6-8 px card radius.
* Refine light/dark palettes so accent is used only for active states.

### Phase 3: Drag And Drop Delight

* When a valid item is dragged over the handle, animate the handle into an
  active drop target before expanding the shelf.
* Show a visible drop affordance in the shelf: border accent + small drop icon +
  accepted/unsupported copy.
* On successful drop, add a brief item insertion animation and then auto-collapse
  only if expansion was drag-triggered.
* For unsupported drops, avoid modal dialogs; use inline feedback for 1-2
  seconds.

### Phase 4: Card Quality

* Cards should have fixed interaction zones so action buttons do not shift text.
* Fade in actions on hover/selection instead of appearing abruptly.
* Make selected card state distinct from hover.
* Add missing-file and image-cache-missing states with calm warning treatment.
* Keep long names truncated with tooltip.

### Phase 5: Optional Later Enhancements

* Keyboard shortcut to summon shelf.
* Pinned/open recent shelf behavior.
* Multi-screen availability.
* Preview/Quick Look style action.
* Power-user action menu.

These should stay out of the first polish pass unless the core interaction feels
solid.

## Recommended MVP Scope For The Next Implementation Pass

Implement Phases 1-3 first. This will produce the biggest perceived quality
increase with the lowest architecture risk because it uses existing WPF windows,
theme resources, and drag/drop state.
