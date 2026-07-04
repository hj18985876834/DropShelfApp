using System.Windows;
using System.Windows.Input;
using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;
using WpfDragDrop = System.Windows.DragDrop;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfClipboard = System.Windows.Clipboard;
using WpfGrid = System.Windows.Controls.Grid;
using WpfMouseButtonState = System.Windows.Input.MouseButtonState;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DropShelf.App.Views;

public partial class ShelfWindow : Window
{
    private readonly DragDropService _dragDropService;
    private readonly WindowDockService _dockService;
    private readonly ImageStore _imageStore;
    private readonly ShelfViewModel _viewModel;
    private bool _allowClose;
    private DockEdge _dockEdge;

    public ShelfWindow(
        ShelfViewModel viewModel,
        WindowDockService dockService,
        DragDropService dragDropService,
        ImageStore imageStore,
        DockEdge dockEdge)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _dockService = dockService ?? throw new ArgumentNullException(nameof(dockService));
        _dragDropService = dragDropService ?? throw new ArgumentNullException(nameof(dragDropService));
        _imageStore = imageStore ?? throw new ArgumentNullException(nameof(imageStore));
        _dockEdge = dockEdge;

        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShelfViewModel.IsShelfVisible))
            {
                ApplyShellState();
            }
        };

        Loaded += (_, _) => ApplyShellState();
    }

    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _dockEdge = settings.DockEdge;
        ApplyDockLayout();
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
            Show();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
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
            PasteClipboardContent();
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }

    private void ApplyShellState()
    {
        ApplyDockLayout();
        PanelHost.Visibility = _viewModel.IsShelfVisible ? Visibility.Visible : Visibility.Collapsed;
        _dockService.Apply(this, _dockEdge, _viewModel.IsShelfVisible);
    }

    private void ApplyDockLayout()
    {
        var isHorizontal = _dockEdge is DockEdge.Top or DockEdge.Bottom;

        RootLayout.ColumnDefinitions[0].Width = _dockEdge == DockEdge.Left
            ? new GridLength(WindowDockService.HandleThickness)
            : new GridLength(1, GridUnitType.Star);
        RootLayout.ColumnDefinitions[1].Width = _dockEdge == DockEdge.Right
            ? new GridLength(WindowDockService.HandleThickness)
            : isHorizontal
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);
        RootLayout.RowDefinitions[0].Height = _dockEdge == DockEdge.Top
            ? new GridLength(WindowDockService.HandleThickness)
            : new GridLength(1, GridUnitType.Star);
        RootLayout.RowDefinitions[1].Height = _dockEdge == DockEdge.Bottom
            ? new GridLength(WindowDockService.HandleThickness)
            : isHorizontal
                ? new GridLength(1, GridUnitType.Star)
                : new GridLength(0);

        ToggleHandle.Width = isHorizontal ? double.NaN : WindowDockService.HandleThickness;
        ToggleHandle.Height = isHorizontal ? WindowDockService.HandleThickness : 132;
        ToggleHandle.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        ToggleHandle.VerticalAlignment = isHorizontal ? VerticalAlignment.Stretch : VerticalAlignment.Center;
        ToggleContent.Orientation = isHorizontal ? System.Windows.Controls.Orientation.Horizontal : System.Windows.Controls.Orientation.Vertical;

        switch (_dockEdge)
        {
            case DockEdge.Left:
                SetPanelPosition(column: 1, row: 0);
                SetHandlePosition(column: 0, row: 0);
                PanelHost.Margin = new Thickness(0, 8, 0, 8);
                PanelHost.CornerRadius = new CornerRadius(0, 10, 10, 0);
                break;
            case DockEdge.Right:
                SetPanelPosition(column: 0, row: 0);
                SetHandlePosition(column: 1, row: 0);
                PanelHost.Margin = new Thickness(0, 8, 0, 8);
                PanelHost.CornerRadius = new CornerRadius(10, 0, 0, 10);
                break;
            case DockEdge.Top:
                SetPanelPosition(column: 0, row: 1);
                SetHandlePosition(column: 0, row: 0);
                PanelHost.Margin = new Thickness(8, 0, 8, 0);
                PanelHost.CornerRadius = new CornerRadius(0, 0, 10, 10);
                break;
            case DockEdge.Bottom:
                SetPanelPosition(column: 0, row: 0);
                SetHandlePosition(column: 0, row: 1);
                PanelHost.Margin = new Thickness(8, 0, 8, 0);
                PanelHost.CornerRadius = new CornerRadius(10, 10, 0, 0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_dockEdge), _dockEdge, null);
        }
    }

    private void SetPanelPosition(int column, int row)
    {
        WpfGrid.SetColumn(PanelHost, column);
        WpfGrid.SetRow(PanelHost, row);
    }

    private void SetHandlePosition(int column, int row)
    {
        WpfGrid.SetColumn(ToggleHandle, column);
        WpfGrid.SetRow(ToggleHandle, row);
    }

    private void DropZone_OnDragOver(object sender, WpfDragEventArgs e)
    {
        var accepted = _dragDropService.CanCreateItems(e.Data);
        _viewModel.IsDragOverAccepted = accepted;
        _viewModel.IsDragOverUnsupported = !accepted;
        e.Effects = accepted ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_OnDragLeave(object sender, WpfDragEventArgs e)
    {
        _viewModel.IsDragOverAccepted = false;
        _viewModel.IsDragOverUnsupported = false;
        e.Handled = true;
    }

    private void DropZone_OnDrop(object sender, WpfDragEventArgs e)
    {
        var items = _dragDropService.CreateItems(e.Data, _imageStore);
        if (items.Count > 0)
        {
            _viewModel.AddItems(items);
            _viewModel.IsShelfVisible = true;
        }

        _viewModel.IsDragOverAccepted = false;
        _viewModel.IsDragOverUnsupported = false;
        e.Handled = true;
    }

    private void PasteClipboardContent()
    {
        var dataObject = WpfClipboard.GetDataObject();
        if (dataObject is null)
        {
            _viewModel.IsDragOverUnsupported = true;
            return;
        }

        var items = _dragDropService.CreateItems(dataObject, _imageStore);
        if (items.Count == 0)
        {
            _viewModel.IsDragOverUnsupported = true;
            return;
        }

        _viewModel.AddItems(items);
        _viewModel.IsDragOverUnsupported = false;
        _viewModel.IsShelfVisible = true;
    }

    private void ShelfItem_OnPreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != WpfMouseButtonState.Pressed ||
            sender is not FrameworkElement { DataContext: ShelfItemViewModel itemViewModel })
        {
            return;
        }

        var payload = _dragDropService.CreateDragOutPayload(itemViewModel.Item);
        if (payload is null)
        {
            itemViewModel.RefreshPathState();
            e.Handled = true;
            return;
        }

        WpfDragDrop.DoDragDrop((DependencyObject)sender, payload.CreateDataObject(), payload.AllowedEffects);
        itemViewModel.RefreshPathState();
        e.Handled = true;
    }
}
