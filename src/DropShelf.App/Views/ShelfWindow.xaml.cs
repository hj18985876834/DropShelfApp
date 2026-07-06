using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;
using WpfDragDrop = System.Windows.DragDrop;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfClipboard = System.Windows.Clipboard;
using WpfMouseButtonState = System.Windows.Input.MouseButtonState;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace DropShelf.App.Views;

public partial class ShelfWindow : Window
{
    private static readonly Duration ShellAnimationDuration = new(TimeSpan.FromMilliseconds(140));
    private static readonly TimeSpan DropFeedbackResetDelay = TimeSpan.FromSeconds(1.6);
    private const double MouseWheelDeltaForOneNotch = 120;
    private const double ShelfWheelScrollStep = 32;

    private readonly DragDropService _dragDropService;
    private readonly WindowDockService _dockService;
    private readonly ImageStore _imageStore;
    private readonly ShelfViewModel _viewModel;
    private readonly Action? _pointerEntered;
    private readonly Action? _pointerLeft;
    private readonly Action? _internalDragStarted;
    private readonly Action? _internalDragEnded;
    private readonly DispatcherTimer _dropFeedbackResetTimer;
    private Window? _handleWindow;
    private bool _allowClose;
    private DockEdge _dockEdge;
    private WpfPoint? _dragStartPoint;
    private bool _isCardContextMenuOpen;
    private bool _isPanelVisible;
    private ScrollViewer? _shelfItemsScrollViewer;
    private bool _wasExpandedByDrag;

    public ShelfWindow(
        ShelfViewModel viewModel,
        WindowDockService dockService,
        DragDropService dragDropService,
        ImageStore imageStore,
        AppSettings settings,
        Action? pointerEntered = null,
        Action? pointerLeft = null,
        Action? internalDragStarted = null,
        Action? internalDragEnded = null)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _dockService = dockService ?? throw new ArgumentNullException(nameof(dockService));
        _dragDropService = dragDropService ?? throw new ArgumentNullException(nameof(dragDropService));
        _imageStore = imageStore ?? throw new ArgumentNullException(nameof(imageStore));
        _pointerEntered = pointerEntered;
        _pointerLeft = pointerLeft;
        _internalDragStarted = internalDragStarted;
        _internalDragEnded = internalDragEnded;
        ArgumentNullException.ThrowIfNull(settings);
        _dockEdge = settings.DockEdge;

        InitializeComponent();
        DataContext = _viewModel;

        _dropFeedbackResetTimer = new DispatcherTimer { Interval = DropFeedbackResetDelay };
        _dropFeedbackResetTimer.Tick += (_, _) => ClearDropFeedback();

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShelfViewModel.IsShelfVisible))
            {
                ApplyShellState();
            }
        };

        Loaded += (_, _) => ApplyShellState();
    }

    public void AttachHandleWindow(Window handleWindow)
    {
        _handleWindow = handleWindow ?? throw new ArgumentNullException(nameof(handleWindow));
        ApplyShellState();
    }

    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _dockEdge = settings.DockEdge;
        ApplyShellState();
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            _viewModel.IsShelfVisible = false;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            _wasExpandedByDrag = false;
            _viewModel.IsShelfVisible = false;
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Delete && _viewModel.RemoveSelectedCommand.CanExecute(null))
        {
            _viewModel.RemoveSelectedCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Enter && _viewModel.OpenSelectedCommand.CanExecute(null))
        {
            _viewModel.OpenSelectedCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.C &&
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
            _viewModel.CopySelectedCommand.CanExecute(null))
        {
            _viewModel.CopySelectedCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.V &&
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _ = PasteClipboardContentAsync();
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }

    private void ApplyShellState()
    {
        if (_viewModel.IsShelfVisible)
        {
            ShowPanel();
        }
        else
        {
            HidePanel();
        }
    }

    private void ShowPanel()
    {
        if (_isPanelVisible)
        {
            PositionPanel();
            return;
        }

        _isPanelVisible = true;
        PositionPanel();
        Show();
        PanelHost.Visibility = Visibility.Visible;
        PanelSlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, null);
        PanelSlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);

        var offset = GetCollapsedPanelOffset();
        PanelSlideTransform.X = offset.X;
        PanelSlideTransform.Y = offset.Y;
        PanelHost.Opacity = 0;

        PanelSlideTransform.BeginAnimation(
            System.Windows.Media.TranslateTransform.XProperty,
            CreateAnimation(offset.X, 0));
        PanelSlideTransform.BeginAnimation(
            System.Windows.Media.TranslateTransform.YProperty,
            CreateAnimation(offset.Y, 0));
        PanelHost.BeginAnimation(UIElement.OpacityProperty, CreateAnimation(0, 1));
    }

    private void HidePanel()
    {
        if (!_isPanelVisible)
        {
            PanelHost.Visibility = Visibility.Collapsed;
            PanelHost.Opacity = 0;
            Hide();
            return;
        }

        _isPanelVisible = false;
        var offset = GetCollapsedPanelOffset();
        var opacityAnimation = CreateAnimation(1, 0);
        opacityAnimation.Completed += (_, _) =>
        {
            if (!_viewModel.IsShelfVisible)
            {
                PanelHost.Visibility = Visibility.Collapsed;
                Hide();
            }
        };

        PanelSlideTransform.BeginAnimation(
            System.Windows.Media.TranslateTransform.XProperty,
            CreateAnimation(0, offset.X));
        PanelSlideTransform.BeginAnimation(
            System.Windows.Media.TranslateTransform.YProperty,
            CreateAnimation(0, offset.Y));
        PanelHost.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
    }

    private Vector GetCollapsedPanelOffset()
    {
        const double offset = 18;

        return _dockEdge switch
        {
            DockEdge.Left => new Vector(-offset, 0),
            DockEdge.Right => new Vector(offset, 0),
            DockEdge.Top => new Vector(0, -offset),
            DockEdge.Bottom => new Vector(0, offset),
            _ => new Vector(0, 0),
        };
    }

    private static DoubleAnimation CreateAnimation(double from, double to)
    {
        return new DoubleAnimation(from, to, ShellAnimationDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
    }

    private void DropZone_OnDragOver(object sender, WpfDragEventArgs e)
    {
        HandleDragOver(e, expandOnAccepted: false);
    }

    private void DropZone_OnDragLeave(object sender, WpfDragEventArgs e)
    {
        ClearDragState(e, collapseAutoExpanded: false);
    }

    private async void DropZone_OnDrop(object sender, WpfDragEventArgs e)
    {
        await HandleDropAsync(e);
    }

    private void ShelfHost_OnDragOver(object sender, WpfDragEventArgs e)
    {
        HandleDragOver(e, expandOnAccepted: true);
    }

    private void ShelfHost_OnDragLeave(object sender, WpfDragEventArgs e)
    {
        ClearDragState(e, collapseAutoExpanded: true);
    }

    private async void ShelfHost_OnDrop(object sender, WpfDragEventArgs e)
    {
        await HandleDropAsync(e);
    }

    private void ShelfHost_OnMouseEnter(object sender, WpfMouseEventArgs e)
    {
        _pointerEntered?.Invoke();
    }

    private void ShelfHost_OnMouseLeave(object sender, WpfMouseEventArgs e)
    {
        if (_isCardContextMenuOpen)
        {
            return;
        }

        _pointerLeft?.Invoke();
    }

    private void ShelfItemsList_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = _shelfItemsScrollViewer ??= FindDescendant<ScrollViewer>(ShelfItemsList);
        if (scrollViewer is null)
        {
            return;
        }

        var wheelNotches = e.Delta / MouseWheelDeltaForOneNotch;
        var targetOffset = scrollViewer.VerticalOffset - wheelNotches * ShelfWheelScrollStep;
        scrollViewer.ScrollToVerticalOffset(Math.Clamp(targetOffset, 0, scrollViewer.ScrollableHeight));
        e.Handled = true;
    }

    private void HandleDragOver(WpfDragEventArgs e, bool expandOnAccepted)
    {
        var accepted = _dragDropService.CanCreateItems(e.Data);
        _viewModel.IsDragOverAccepted = accepted;
        _viewModel.IsDragOverUnsupported = !accepted;
        e.Effects = accepted ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;

        if (accepted && expandOnAccepted && !_viewModel.IsShelfVisible)
        {
            _wasExpandedByDrag = true;
            _viewModel.IsShelfVisible = true;
        }

        e.Handled = true;
    }

    private async Task HandleDropAsync(WpfDragEventArgs e)
    {
        try
        {
            var items = await _dragDropService.CreateItemsAsync(e.Data, _imageStore);
            if (items.Count > 0)
            {
                _viewModel.AddItems(items);
                if (_wasExpandedByDrag)
                {
                    _viewModel.IsShelfVisible = false;
                }
                else
                {
                    _viewModel.IsShelfVisible = true;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
        {
            _viewModel.IsDragOverUnsupported = true;
        }

        _wasExpandedByDrag = false;
        _viewModel.IsDragOverAccepted = false;
        if (_viewModel.IsDragOverUnsupported)
        {
            ScheduleDropFeedbackReset();
        }
        else
        {
            _viewModel.IsDragOverUnsupported = false;
        }
        e.Handled = true;
    }

    private void ClearDragState(WpfDragEventArgs e, bool collapseAutoExpanded)
    {
        if (collapseAutoExpanded && _wasExpandedByDrag)
        {
            _viewModel.IsShelfVisible = false;
            _wasExpandedByDrag = false;
        }

        _viewModel.IsDragOverAccepted = false;
        _viewModel.IsDragOverUnsupported = false;
        e.Handled = true;
    }

    private void ScheduleDropFeedbackReset()
    {
        _dropFeedbackResetTimer.Stop();
        _dropFeedbackResetTimer.Start();
    }

    private void ClearDropFeedback()
    {
        _dropFeedbackResetTimer.Stop();
        _viewModel.IsDragOverAccepted = false;
        _viewModel.IsDragOverUnsupported = false;
    }

    private void PositionPanel()
    {
        if (_handleWindow is not null)
        {
            _dockService.ApplyPanel(this, _handleWindow, _dockEdge);
        }
    }

    private async Task PasteClipboardContentAsync()
    {
        var dataObject = WpfClipboard.GetDataObject();
        if (dataObject is null)
        {
            _viewModel.IsDragOverUnsupported = true;
            ScheduleDropFeedbackReset();
            return;
        }

        try
        {
            var items = await _dragDropService.CreateItemsAsync(dataObject, _imageStore);
            if (items.Count == 0)
            {
                _viewModel.IsDragOverUnsupported = true;
                ScheduleDropFeedbackReset();
                return;
            }

            _viewModel.AddItems(items);
            _viewModel.IsDragOverUnsupported = false;
            _viewModel.IsShelfVisible = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
        {
            _viewModel.IsDragOverUnsupported = true;
            ScheduleDropFeedbackReset();
        }
    }

    private void ShelfItem_OnPreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != WpfMouseButtonState.Pressed ||
            sender is not FrameworkElement { DataContext: ShelfItemViewModel itemViewModel })
        {
            _dragStartPoint = null;
            return;
        }

        var currentPosition = e.GetPosition(this);
        _dragStartPoint ??= currentPosition;
        if (Math.Abs(currentPosition.X - _dragStartPoint.Value.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _dragStartPoint.Value.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var payloadResult = _dragDropService.TryCreateDragOutPayload(itemViewModel.Item);
        if (!payloadResult.CanStartDrag || payloadResult.Payload is null)
        {
            if (!string.IsNullOrWhiteSpace(payloadResult.Message))
            {
                itemViewModel.SetStatusMessage(payloadResult.Message);
            }

            itemViewModel.RefreshPathState();
            _dragStartPoint = null;
            ReleaseMouseCapture(sender);
            e.Handled = true;
            return;
        }

        var payload = payloadResult.Payload;
        ReleaseMouseCapture(sender);
        _internalDragStarted?.Invoke();
        try
        {
            WpfDragDrop.DoDragDrop((DependencyObject)sender, payload.CreateDataObject(), payload.AllowedEffects);
        }
        finally
        {
            _internalDragEnded?.Invoke();
            itemViewModel.RefreshPathState();
            _dragStartPoint = null;
            ReleaseMouseCapture(sender);
        }

        e.Handled = true;
    }

    private void ShelfItem_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 &&
            sender is FrameworkElement { DataContext: ShelfItemViewModel itemViewModel } &&
            itemViewModel.OpenCommand.CanExecute(null))
        {
            _viewModel.SelectedItem = itemViewModel;
            itemViewModel.OpenCommand.Execute(null);
            _dragStartPoint = null;
            ReleaseMouseCapture(sender);
            e.Handled = true;
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        if (sender is UIElement element)
        {
            element.CaptureMouse();
        }
    }

    private void ShelfItem_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = null;
        ReleaseMouseCapture(sender);
    }

    private void CardContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        _isCardContextMenuOpen = true;
        _pointerEntered?.Invoke();
    }

    private void CardContextMenu_OnClosed(object sender, RoutedEventArgs e)
    {
        _isCardContextMenuOpen = false;
        _pointerLeft?.Invoke();
    }

    private static void ReleaseMouseCapture(object sender)
    {
        if (sender is UIElement element && element.IsMouseCaptured)
        {
            element.ReleaseMouseCapture();
        }
    }

    private static T? FindDescendant<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var nestedMatch = FindDescendant<T>(child);
            if (nestedMatch is not null)
            {
                return nestedMatch;
            }
        }

        return null;
    }
}
