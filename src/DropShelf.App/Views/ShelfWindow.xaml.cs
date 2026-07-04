using System.Windows;
using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;

namespace DropShelf.App.Views;

public partial class ShelfWindow : Window
{
    private readonly DockEdge _dockEdge;
    private readonly WindowDockService _dockService;
    private readonly ShelfViewModel _viewModel;
    private bool _allowClose;

    public ShelfWindow(ShelfViewModel viewModel, WindowDockService dockService, DockEdge dockEdge)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _dockService = dockService ?? throw new ArgumentNullException(nameof(dockService));
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

        base.OnPreviewKeyDown(e);
    }

    private void ApplyShellState()
    {
        PanelHost.Visibility = _viewModel.IsShelfVisible ? Visibility.Visible : Visibility.Collapsed;
        _dockService.Apply(this, _dockEdge, _viewModel.IsShelfVisible);
    }
}
