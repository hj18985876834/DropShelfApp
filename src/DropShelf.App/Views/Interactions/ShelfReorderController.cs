using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DropShelf.App.Models;
using DropShelf.App.ViewModels;
using WpfBorder = System.Windows.Controls.Border;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfPoint = System.Windows.Point;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;

namespace DropShelf.App.Views.Interactions;

public sealed class ShelfReorderController : IDisposable
{
    private static readonly Duration ReorderLiftAnimationDuration = new(TimeSpan.FromMilliseconds(110));
    private static readonly Duration ReorderDropAnimationDuration = new(TimeSpan.FromMilliseconds(130));
    private static readonly Duration ReorderCardResetAnimationDuration = new(TimeSpan.FromMilliseconds(120));
    private const double ReorderAutoScrollFrameMilliseconds = 32;
    private const double ReorderAutoScrollEdgeSize = 52;
    private const double ReorderAutoScrollMinStep = 1;
    private const double ReorderAutoScrollMaxStep = 6;
    private const double ReorderDropIndicatorHeight = 3;
    private const double ReorderDropIndicatorInset = 6;

    private readonly ShelfViewModel _viewModel;
    private readonly FrameworkElement _coordinateRoot;
    private readonly WpfListBox _itemsList;
    private readonly WpfBorder _previewCard;
    private readonly WpfBorder _dropIndicator;
    private readonly Func<WpfScrollViewer?> _getScrollViewer;
    private readonly Func<ShelfItemViewModel, WpfBorder?> _findCardForItem;
    private readonly Action? _internalDragStarted;
    private readonly Action? _internalDragEnded;
    private readonly List<RealizedReorderCard> _realizedCards = [];
    private UIElement? _capturedElement;
    private ShelfItemViewModel? _sourceItem;
    private WpfPoint? _startWindowPosition;
    private WpfPoint? _previewPointerOffset;
    private WpfPoint _latestWindowPosition;
    private WpfPoint _latestListPosition;
    private TimeSpan? _lastRenderingTime;
    private int _sourceVisibleIndex = -1;
    private int? _pendingTargetVisibleIndex;
    private double _sourceShiftHeight;
    private bool _isDragActive;
    private bool _isRendering;
    private bool _isDisposed;

    public ShelfReorderController(
        ShelfViewModel viewModel,
        FrameworkElement coordinateRoot,
        WpfListBox itemsList,
        WpfBorder previewCard,
        WpfBorder dropIndicator,
        Func<WpfScrollViewer?> getScrollViewer,
        Func<ShelfItemViewModel, WpfBorder?> findCardForItem,
        Action? internalDragStarted,
        Action? internalDragEnded)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _coordinateRoot = coordinateRoot ?? throw new ArgumentNullException(nameof(coordinateRoot));
        _itemsList = itemsList ?? throw new ArgumentNullException(nameof(itemsList));
        _previewCard = previewCard ?? throw new ArgumentNullException(nameof(previewCard));
        _dropIndicator = dropIndicator ?? throw new ArgumentNullException(nameof(dropIndicator));
        _getScrollViewer = getScrollViewer ?? throw new ArgumentNullException(nameof(getScrollViewer));
        _findCardForItem = findCardForItem ?? throw new ArgumentNullException(nameof(findCardForItem));
        _internalDragStarted = internalDragStarted;
        _internalDragEnded = internalDragEnded;
    }

    public bool HasSource => _sourceItem is not null;

    public bool IsDragActive => _isDragActive;

    public void Begin(UIElement handleElement, ShelfItemViewModel item, WpfPoint windowPosition)
    {
        ArgumentNullException.ThrowIfNull(handleElement);
        ArgumentNullException.ThrowIfNull(item);

        if (_isDisposed)
        {
            return;
        }

        Cancel();
        _sourceItem = item;
        _sourceVisibleIndex = _viewModel.VisibleItems.IndexOf(item);
        _startWindowPosition = windowPosition;
        _latestWindowPosition = windowPosition;
        _latestListPosition = MousePositionInList(windowPosition);
        _capturedElement = handleElement;
        handleElement.CaptureMouse();
    }

    public bool Move(WpfPoint windowPosition, WpfPoint listPosition)
    {
        if (_isDisposed)
        {
            return false;
        }

        if (_sourceItem is null || _startWindowPosition is null)
        {
            return false;
        }

        if (_viewModel.ActiveFilter is not ShelfFilterMode.All)
        {
            Cancel();
            return true;
        }

        _latestWindowPosition = windowPosition;
        _latestListPosition = listPosition;

        if (!_isDragActive)
        {
            if (Math.Abs(windowPosition.X - _startWindowPosition.Value.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(windowPosition.Y - _startWindowPosition.Value.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return false;
            }

            StartDrag(windowPosition);
        }

        return true;
    }

    public bool End()
    {
        if (_isDisposed)
        {
            return false;
        }

        var wasDragActive = _isDragActive;
        var sourceItem = _sourceItem;
        var pendingTargetIndex = _pendingTargetVisibleIndex;

        StopRendering();
        HideReorderPreview();
        HideReorderDropIndicator();
        ResetCardTransforms(animate: true);

        if (sourceItem is not null)
        {
            sourceItem.IsReordering = false;
        }

        ClearGestureState();

        if (wasDragActive && sourceItem is not null && pendingTargetIndex is int targetIndex)
        {
            _viewModel.MoveItem(sourceItem, targetIndex);
        }

        if (wasDragActive)
        {
            _internalDragEnded?.Invoke();
        }

        return wasDragActive;
    }

    public void Cancel()
    {
        if (_isDisposed)
        {
            return;
        }

        var wasDragActive = _isDragActive;
        var sourceItem = _sourceItem;

        StopRendering();
        HideReorderPreview();
        HideReorderDropIndicator();
        ResetCardTransforms(animate: true);

        if (sourceItem is not null)
        {
            sourceItem.IsReordering = false;
        }

        ClearGestureState();

        if (wasDragActive)
        {
            _internalDragEnded?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        StopRendering();
        HideReorderPreview();
        HideReorderDropIndicator();
        ResetCardTransforms(animate: false);
        ClearGestureState();
    }

    private void StartDrag(WpfPoint windowPosition)
    {
        if (_sourceItem is null)
        {
            return;
        }

        RebuildRealizedCards();
        var sourceCard = _findCardForItem(_sourceItem);
        _sourceShiftHeight = sourceCard is not null
            ? GetCardShiftHeight(sourceCard)
            : Math.Max(1, _previewCard.ActualHeight);
        _previewPointerOffset = GetPreviewPointerOffset(sourceCard, windowPosition);
        _sourceItem.IsReordering = true;
        _isDragActive = true;
        _pendingTargetVisibleIndex = _sourceVisibleIndex;
        ShowReorderPreview(_sourceItem, windowPosition);
        StartRendering();
        _internalDragStarted?.Invoke();
    }

    private void StartRendering()
    {
        if (_isRendering)
        {
            return;
        }

        _isRendering = true;
        _lastRenderingTime = null;
        CompositionTarget.Rendering += CompositionTarget_OnRendering;
    }

    private void StopRendering()
    {
        if (!_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering -= CompositionTarget_OnRendering;
        _isRendering = false;
        _lastRenderingTime = null;
    }

    private void CompositionTarget_OnRendering(object? sender, EventArgs e)
    {
        if (!_isDragActive || _sourceItem is null)
        {
            Cancel();
            return;
        }

        var renderingTime = e is RenderingEventArgs renderingEventArgs
            ? renderingEventArgs.RenderingTime
            : TimeSpan.Zero;
        var elapsed = _lastRenderingTime is { } lastRenderingTime && renderingTime > lastRenderingTime
            ? renderingTime - lastRenderingTime
            : TimeSpan.FromMilliseconds(ReorderAutoScrollFrameMilliseconds);
        _lastRenderingTime = renderingTime;

        var scrolled = ApplyAutoScroll(elapsed);
        if (scrolled)
        {
            RebuildRealizedCards();
            _latestListPosition = MousePositionInList(_latestWindowPosition);
        }

        MoveReorderPreview(_latestWindowPosition);
        UpdateReorderCalculation();
        UpdateDropIndicator();
    }

    private bool ApplyAutoScroll(TimeSpan elapsed)
    {
        var scrollViewer = _getScrollViewer();
        if (scrollViewer is null || scrollViewer.ScrollableHeight <= 0)
        {
            return false;
        }

        var step = GetReorderAutoScrollStep(_latestListPosition, _itemsList.ActualHeight);
        if (Math.Abs(step) < double.Epsilon)
        {
            return false;
        }

        var frameScale = Math.Clamp(elapsed.TotalMilliseconds / ReorderAutoScrollFrameMilliseconds, 0.25, 2.5);
        var previousOffset = scrollViewer.VerticalOffset;
        var nextOffset = Math.Clamp(previousOffset + (step * frameScale), 0, scrollViewer.ScrollableHeight);
        if (Math.Abs(nextOffset - previousOffset) < double.Epsilon)
        {
            return false;
        }

        scrollViewer.ScrollToVerticalOffset(nextOffset);
        return true;
    }

    private static double GetReorderAutoScrollStep(WpfPoint listPosition, double listHeight)
    {
        if (listHeight <= 0)
        {
            return 0;
        }

        var edgeSize = Math.Min(ReorderAutoScrollEdgeSize, listHeight / 2);
        if (edgeSize <= 0)
        {
            return 0;
        }

        if (listPosition.Y < edgeSize)
        {
            var intensity = 1 - Math.Clamp(listPosition.Y / edgeSize, 0, 1);
            return -GetIntensityStep(intensity);
        }

        if (listPosition.Y > listHeight - edgeSize)
        {
            var intensity = 1 - Math.Clamp((listHeight - listPosition.Y) / edgeSize, 0, 1);
            return GetIntensityStep(intensity);
        }

        return 0;
    }

    private static double GetIntensityStep(double intensity)
    {
        var normalizedIntensity = Math.Clamp(intensity, 0, 1);
        return ReorderAutoScrollMinStep +
            ((ReorderAutoScrollMaxStep - ReorderAutoScrollMinStep) * normalizedIntensity * normalizedIntensity);
    }

    private void UpdateReorderCalculation()
    {
        if (_sourceItem is null)
        {
            return;
        }

        if (_realizedCards.Count == 0)
        {
            RebuildRealizedCards();
        }

        var layouts = _realizedCards
            .Select(card => new ShelfReorderItemLayout(card.Index, card.Top, card.Height, card.ShiftHeight))
            .ToArray();
        var calculation = ShelfReorderCalculator.Calculate(
            layouts,
            _sourceVisibleIndex,
            GetCurrentDragCenterY(),
            _sourceShiftHeight);

        _pendingTargetVisibleIndex = calculation.TargetIndex >= 0
            ? calculation.TargetIndex
            : _sourceVisibleIndex;

        foreach (var card in _realizedCards)
        {
            var offset = calculation.Offsets.GetValueOrDefault(card.Index);
            SetCardTranslate(card.Card, offset);
        }
    }

    private void UpdateDropIndicator()
    {
        if (_sourceItem is null ||
            _pendingTargetVisibleIndex is not int pendingIndex ||
            pendingIndex < 0 ||
            pendingIndex == _sourceVisibleIndex)
        {
            HideReorderDropIndicator();
            return;
        }

        var targetCard = _realizedCards.FirstOrDefault(card => card.Index == pendingIndex);
        if (targetCard is null)
        {
            HideReorderDropIndicator();
            return;
        }

        var listOrigin = _itemsList.TranslatePoint(new WpfPoint(0, 0), _coordinateRoot);
        var listLeft = Math.Max(0, listOrigin.X);
        var listTop = Math.Max(0, listOrigin.Y);
        var listRight = Math.Min(_coordinateRoot.ActualWidth, listOrigin.X + _itemsList.ActualWidth);
        var listBottom = Math.Min(_coordinateRoot.ActualHeight, listOrigin.Y + _itemsList.ActualHeight);
        var indicatorWidth = Math.Max(0, (listRight - listLeft) - (ReorderDropIndicatorInset * 2));
        if (indicatorWidth <= 0)
        {
            HideReorderDropIndicator();
            return;
        }

        var indicatorTop = listOrigin.Y + targetCard.Top - (ReorderDropIndicatorHeight / 2);
        if (pendingIndex > _sourceVisibleIndex)
        {
            indicatorTop = listOrigin.Y + targetCard.Top + targetCard.Height - (ReorderDropIndicatorHeight / 2);
        }

        indicatorTop = Math.Clamp(
            indicatorTop,
            listTop,
            Math.Max(listTop, listBottom - ReorderDropIndicatorHeight));

        _dropIndicator.Width = indicatorWidth;
        SetOverlayTranslate(_dropIndicator, listLeft + ReorderDropIndicatorInset, indicatorTop);
        _dropIndicator.Visibility = Visibility.Visible;
    }

    private void ShowReorderPreview(ShelfItemViewModel itemViewModel, WpfPoint pointerPosition)
    {
        _previewCard.BeginAnimation(UIElement.OpacityProperty, null);
        _previewCard.DataContext = itemViewModel;
        _previewCard.Width = GetReorderPreviewWidth();
        _previewCard.Visibility = Visibility.Visible;
        _previewCard.Opacity = 0;
        _previewCard.BeginAnimation(UIElement.OpacityProperty, CreateAnimation(0, 1, ReorderLiftAnimationDuration));
        MoveReorderPreview(pointerPosition);
    }

    private void MoveReorderPreview(WpfPoint pointerPosition)
    {
        if (_previewCard.Visibility != Visibility.Visible)
        {
            return;
        }

        var listOrigin = _itemsList.TranslatePoint(new WpfPoint(0, 0), _coordinateRoot);
        var listLeft = Math.Max(0, listOrigin.X);
        var listTop = Math.Max(0, listOrigin.Y);
        var listRight = Math.Min(_coordinateRoot.ActualWidth, listOrigin.X + _itemsList.ActualWidth);
        var listBottom = Math.Min(_coordinateRoot.ActualHeight, listOrigin.Y + _itemsList.ActualHeight);
        var previewWidth = _previewCard.ActualWidth > 0
            ? _previewCard.ActualWidth
            : _previewCard.Width;
        var previewHeight = _previewCard.ActualHeight > 0
            ? _previewCard.ActualHeight
            : _previewCard.DesiredSize.Height;
        var pointerOffset = _previewPointerOffset ?? new WpfPoint(previewWidth / 2, 18);
        var left = Math.Clamp(
            pointerPosition.X - pointerOffset.X,
            listLeft,
            Math.Max(listLeft, listRight - previewWidth));
        var top = Math.Clamp(
            pointerPosition.Y - pointerOffset.Y,
            listTop,
            Math.Max(listTop, listBottom - previewHeight));

        SetOverlayTranslate(_previewCard, left, top);
    }

    private void HideReorderPreview()
    {
        if (_previewCard.Visibility != Visibility.Visible)
        {
            return;
        }

        var animation = CreateAnimation(_previewCard.Opacity, 0, ReorderDropAnimationDuration);
        animation.Completed += (_, _) =>
        {
            if (!_isDragActive)
            {
                _previewCard.Visibility = Visibility.Collapsed;
                _previewCard.DataContext = null;
            }
        };
        _previewCard.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private void HideReorderDropIndicator()
    {
        _dropIndicator.Visibility = Visibility.Collapsed;
    }

    private void RebuildRealizedCards()
    {
        _realizedCards.Clear();
        for (var index = 0; index < _viewModel.VisibleItems.Count; index++)
        {
            var item = _viewModel.VisibleItems[index];
            var card = _findCardForItem(item);
            if (card is null || !card.IsVisible)
            {
                continue;
            }

            var translateY = GetCardTranslate(card)?.Y ?? 0;
            var origin = card.TranslatePoint(new WpfPoint(0, 0), _itemsList);
            _realizedCards.Add(new RealizedReorderCard(
                index,
                item,
                card,
                origin.Y - translateY,
                card.ActualHeight,
                GetCardShiftHeight(card)));
        }
    }

    private void ResetCardTransforms(bool animate)
    {
        foreach (var card in _realizedCards)
        {
            ResetCardTranslate(card.Card, animate);
        }

        if (_sourceItem is not null)
        {
            var sourceCard = _findCardForItem(_sourceItem);
            if (sourceCard is not null)
            {
                ResetCardTranslate(sourceCard, animate);
            }
        }
    }

    private static void SetCardTranslate(WpfBorder card, double to)
    {
        var translate = GetMutableCardTranslate(card);
        if (translate is null)
        {
            return;
        }

        if (Math.Abs(translate.Y - to) < 0.5)
        {
            return;
        }

        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.Y = to;
    }

    private static void ResetCardTranslate(WpfBorder card, bool animate)
    {
        var translate = GetMutableCardTranslate(card);
        if (translate is null)
        {
            return;
        }

        if (!animate || Math.Abs(translate.Y) < 0.5)
        {
            translate.BeginAnimation(TranslateTransform.YProperty, null);
            translate.Y = 0;
            return;
        }

        translate.BeginAnimation(
            TranslateTransform.YProperty,
            CreateAnimation(
                translate.Y,
                0,
                ReorderCardResetAnimationDuration,
                new CubicEase { EasingMode = EasingMode.EaseOut }));
    }

    private static TranslateTransform? GetCardTranslate(WpfBorder card)
    {
        if (card.RenderTransform is TransformGroup group && group.Children.Count >= 2)
        {
            return group.Children[1] as TranslateTransform;
        }

        return null;
    }

    private static TranslateTransform? GetMutableCardTranslate(WpfBorder card)
    {
        if (card.RenderTransform is not TransformGroup group || group.Children.Count < 2)
        {
            return null;
        }

        if (group.IsFrozen)
        {
            group = group.CloneCurrentValue();
            card.RenderTransform = group;
        }

        if (group.Children[1] is not TranslateTransform translate)
        {
            return null;
        }

        if (!translate.IsFrozen)
        {
            return translate;
        }

        var mutableTranslate = translate.CloneCurrentValue();
        group.Children[1] = mutableTranslate;
        return mutableTranslate;
    }

    private static void SetOverlayTranslate(FrameworkElement element, double x, double y)
    {
        var translate = GetOverlayTranslate(element);
        if (translate is null)
        {
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);
            return;
        }

        translate.X = x;
        translate.Y = y;
    }

    private static TranslateTransform? GetOverlayTranslate(FrameworkElement element)
    {
        if (element.RenderTransform is TranslateTransform translate)
        {
            if (!translate.IsFrozen)
            {
                return translate;
            }

            var mutableTranslate = translate.CloneCurrentValue();
            element.RenderTransform = mutableTranslate;
            return mutableTranslate;
        }

        if (element.RenderTransform is TransformGroup group)
        {
            if (group.IsFrozen)
            {
                group = group.CloneCurrentValue();
                element.RenderTransform = group;
            }

            for (var index = 0; index < group.Children.Count; index++)
            {
                if (group.Children[index] is not TranslateTransform groupTranslate)
                {
                    continue;
                }

                if (!groupTranslate.IsFrozen)
                {
                    return groupTranslate;
                }

                var mutableTranslate = groupTranslate.CloneCurrentValue();
                group.Children[index] = mutableTranslate;
                return mutableTranslate;
            }
        }

        return null;
    }

    private WpfPoint GetPreviewPointerOffset(WpfBorder? sourceCard, WpfPoint pointerPosition)
    {
        if (sourceCard is null)
        {
            return new WpfPoint(36, 18);
        }

        var cardOrigin = sourceCard.TranslatePoint(new WpfPoint(0, 0), _coordinateRoot);
        var pointerOffsetX = pointerPosition.X - cardOrigin.X;
        var pointerOffsetY = pointerPosition.Y - cardOrigin.Y;

        pointerOffsetX = Math.Clamp(pointerOffsetX, 20, Math.Max(20, sourceCard.ActualWidth - 20));
        pointerOffsetY = Math.Clamp(pointerOffsetY, 14, Math.Max(14, sourceCard.ActualHeight - 14));

        if (double.IsNaN(pointerOffsetX) || double.IsInfinity(pointerOffsetX))
        {
            pointerOffsetX = 36;
        }

        if (double.IsNaN(pointerOffsetY) || double.IsInfinity(pointerOffsetY))
        {
            pointerOffsetY = 18;
        }

        return new WpfPoint(pointerOffsetX, pointerOffsetY);
    }

    private double GetCurrentDragCenterY()
    {
        var previewHeight = _previewCard.ActualHeight > 0
            ? _previewCard.ActualHeight
            : _previewCard.DesiredSize.Height;
        var pointerOffsetY = _previewPointerOffset?.Y ?? 18;
        return _latestListPosition.Y + (previewHeight / 2) - pointerOffsetY;
    }

    private double GetReorderPreviewWidth()
    {
        var scrollViewer = _getScrollViewer();
        var viewportWidth = scrollViewer?.ViewportWidth > 0
            ? scrollViewer.ViewportWidth
            : _itemsList.ActualWidth;
        return Math.Max(160, viewportWidth - 12);
    }

    private WpfPoint MousePositionInList(WpfPoint windowPosition)
    {
        var listOrigin = _itemsList.TranslatePoint(new WpfPoint(0, 0), _coordinateRoot);
        return new WpfPoint(windowPosition.X - listOrigin.X, windowPosition.Y - listOrigin.Y);
    }

    private void ClearGestureState()
    {
        _sourceItem = null;
        _startWindowPosition = null;
        _previewPointerOffset = null;
        _pendingTargetVisibleIndex = null;
        _sourceVisibleIndex = -1;
        _sourceShiftHeight = 0;
        _isDragActive = false;
        _realizedCards.Clear();

        if (_capturedElement is not null && _capturedElement.IsMouseCaptured)
        {
            _capturedElement.ReleaseMouseCapture();
        }

        _capturedElement = null;
    }

    private static double GetCardShiftHeight(WpfBorder card)
    {
        return card.ActualHeight + card.Margin.Top + card.Margin.Bottom;
    }

    private static DoubleAnimation CreateAnimation(double from, double to, Duration duration, IEasingFunction? easingFunction = null)
    {
        return new DoubleAnimation(from, to, duration)
        {
            EasingFunction = easingFunction,
        };
    }

    private sealed record RealizedReorderCard(
        int Index,
        ShelfItemViewModel Item,
        WpfBorder Card,
        double Top,
        double Height,
        double ShiftHeight);
}
