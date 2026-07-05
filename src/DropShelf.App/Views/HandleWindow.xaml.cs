using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DropShelf.App.Models;
using DropShelf.App.Services;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfDataObject = System.Windows.IDataObject;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace DropShelf.App.Views;

public partial class HandleWindow : Window
{
    private readonly Action _toggleShelf;
    private readonly Action _showShelf;
    private readonly Action<DockPlacement> _dockPlacementChanged;
    private readonly WindowDockService _dockService;
    private readonly Action? _dragStarted;
    private readonly Func<WpfDataObject, bool>? _canAcceptDrop;
    private readonly Func<WpfDataObject, Task>? _acceptDropAsync;
    private readonly Action? _pointerEntered;
    private readonly Action? _pointerLeft;
    private bool _allowClose;
    private bool _isDragging;
    private bool _suppressNextClick;
    private DockEdge _dockEdge;
    private double _dockOffsetRatio;
    private WpfPoint? _dragPointerOffset;
    private WpfPoint? _dragStartScreenPoint;

    public HandleWindow(
        WindowDockService dockService,
        AppSettings settings,
        Action toggleShelf,
        Action showShelf,
        Action<DockPlacement> dockPlacementChanged,
        Action? dragStarted = null,
        Func<WpfDataObject, bool>? canAcceptDrop = null,
        Func<WpfDataObject, Task>? acceptDropAsync = null,
        Action? pointerEntered = null,
        Action? pointerLeft = null)
    {
        _dockService = dockService ?? throw new ArgumentNullException(nameof(dockService));
        ArgumentNullException.ThrowIfNull(settings);
        _dockEdge = settings.DockEdge;
        _dockOffsetRatio = settings.DockOffsetRatio;
        _toggleShelf = toggleShelf ?? throw new ArgumentNullException(nameof(toggleShelf));
        _showShelf = showShelf ?? throw new ArgumentNullException(nameof(showShelf));
        _dockPlacementChanged = dockPlacementChanged ?? throw new ArgumentNullException(nameof(dockPlacementChanged));
        _dragStarted = dragStarted;
        _canAcceptDrop = canAcceptDrop;
        _acceptDropAsync = acceptDropAsync;
        _pointerEntered = pointerEntered;
        _pointerLeft = pointerLeft;

        InitializeComponent();
        ApplySettings(settings);
        Loaded += (_, _) =>
        {
            ApplyLayout();
            _dockService.ApplyHandle(this, _dockEdge, _dockOffsetRatio);
        };
    }

    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _dockEdge = settings.DockEdge;
        _dockOffsetRatio = settings.DockOffsetRatio;
        if (_isDragging)
        {
            return;
        }

        ApplyLayout();
        _dockService.ApplyHandle(this, _dockEdge, _dockOffsetRatio);
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
            return;
        }

        base.OnClosing(e);
    }

    private void ApplyLayout()
    {
        var isHorizontal = _dockEdge is DockEdge.Top or DockEdge.Bottom;
        Width = WindowDockService.GetWindowWidth(_dockEdge, isShelfVisible: false);
        Height = WindowDockService.GetWindowHeight(_dockEdge, isShelfVisible: false);
        HandleContent.Orientation = isHorizontal ? WpfOrientation.Horizontal : WpfOrientation.Vertical;
    }

    private void HandleButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_suppressNextClick)
        {
            _suppressNextClick = false;
            e.Handled = true;
            return;
        }

        _toggleShelf();
        e.Handled = true;
    }

    private void HandleButton_OnDragOver(object sender, WpfDragEventArgs e)
    {
        var accepted = _canAcceptDrop?.Invoke(e.Data) == true;
        e.Effects = accepted ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;
        if (accepted)
        {
            _showShelf();
        }

        e.Handled = true;
    }

    private async void HandleButton_OnDrop(object sender, WpfDragEventArgs e)
    {
        if (_canAcceptDrop?.Invoke(e.Data) == true)
        {
            await AcceptDropAsync(e.Data);
        }

        e.Handled = true;
    }

    private async Task AcceptDropAsync(WpfDataObject dataObject)
    {
        if (_acceptDropAsync is null)
        {
            return;
        }

        try
        {
            await _acceptDropAsync(dataObject);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
        {
        }
    }

    private void HandleButton_OnMouseEnter(object sender, WpfMouseEventArgs e)
    {
        if (!_isDragging)
        {
            _pointerEntered?.Invoke();
        }
    }

    private void HandleButton_OnMouseLeave(object sender, WpfMouseEventArgs e)
    {
        if (!_isDragging)
        {
            _pointerLeft?.Invoke();
        }
    }

    private void HandleButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragPointerOffset = e.GetPosition(this);
        _dragStartScreenPoint = GetMouseScreenPoint(e);
        _isDragging = false;
        if (sender is UIElement element)
        {
            element.CaptureMouse();
        }
    }

    private void HandleButton_OnPreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            _dragPointerOffset is null ||
            _dragStartScreenPoint is null)
        {
            return;
        }

        var currentScreenPoint = GetMouseScreenPoint(e);
        var deltaX = currentScreenPoint.X - _dragStartScreenPoint.Value.X;
        var deltaY = currentScreenPoint.Y - _dragStartScreenPoint.Value.Y;

        if (!_isDragging &&
            Math.Abs(deltaX) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(deltaY) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (!_isDragging)
        {
            _isDragging = true;
            _dragStarted?.Invoke();
        }

        _dockService.PlaceAt(
            this,
            currentScreenPoint.X - _dragPointerOffset.Value.X,
            currentScreenPoint.Y - _dragPointerOffset.Value.Y);
        e.Handled = true;
    }

    private void HandleButton_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement element)
        {
            element.ReleaseMouseCapture();
        }

        _dragPointerOffset = null;
        _dragStartScreenPoint = null;

        if (!_isDragging)
        {
            return;
        }

        var placement = _dockService.SnapToNearestEdge(new WpfPoint(Left + (Width / 2), Top + (Height / 2)));
        _dockEdge = placement.DockEdge;
        _dockOffsetRatio = placement.DockOffsetRatio;
        ApplyLayout();
        _dockService.ApplyHandle(this, _dockEdge, _dockOffsetRatio);
        _dockPlacementChanged(placement);
        _isDragging = false;
        _suppressNextClick = true;
        Dispatcher.BeginInvoke(
            () => _suppressNextClick = false,
            DispatcherPriority.ApplicationIdle);
        e.Handled = true;
    }

    private WpfPoint GetMouseScreenPoint(WpfMouseEventArgs e)
    {
        return TransformScreenPixelsToDeviceIndependent(PointToScreen(e.GetPosition(this)));
    }

    private WpfPoint TransformScreenPixelsToDeviceIndependent(WpfPoint screenPoint)
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget is null
            ? screenPoint
            : source.CompositionTarget.TransformFromDevice.Transform(screenPoint);
    }
}
