using System.Threading;
using System.Windows;
using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;
using DropShelf.App.Views;

namespace DropShelf.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "DropShelf.AppShell";

    private Mutex? _singleInstanceMutex;
    private ShelfWindow? _shelfWindow;
    private SettingsWindow? _settingsWindow;
    private ShelfViewModel? _shelfViewModel;
    private TrayIconService? _trayIconService;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var ownsMutex);
        if (!ownsMutex)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var settings = AppSettings.CreateDefault();
        var dockService = new WindowDockService();

        _shelfViewModel = new ShelfViewModel(OpenSettingsWindow);
        _shelfViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShelfViewModel.IsShelfVisible))
            {
                _trayIconService?.SetShelfVisible(_shelfViewModel.IsShelfVisible);
            }
        };

        _shelfWindow = new ShelfWindow(_shelfViewModel, dockService, settings.DockEdge);
        _shelfWindow.Show();

        _trayIconService = new TrayIconService(
            () => _shelfViewModel.IsShelfVisible = true,
            () => _shelfViewModel.IsShelfVisible = false,
            OpenSettingsWindow,
            ShutdownApplication);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void OpenSettingsWindow()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow
        {
            Owner = _shelfWindow,
            DataContext = new SettingsViewModel(),
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ShutdownApplication()
    {
        _settingsWindow?.Close();
        _trayIconService?.Dispose();
        _shelfWindow?.ForceClose();
        Shutdown();
    }
}

