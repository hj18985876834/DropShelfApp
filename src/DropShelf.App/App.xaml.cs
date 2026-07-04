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
    private AppSettings _settings = AppSettings.CreateDefault();
    private SettingsStore? _settingsStore;
    private ShelfStore? _shelfStore;
    private StartupService? _startupService;
    private ThemeService? _themeService;
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

        var appDataRoot = new AppDataPathService().GetAppDataRoot();
        _settingsStore = new SettingsStore(appDataRoot);
        _shelfStore = new ShelfStore(appDataRoot);
        _startupService = new StartupService();
        _themeService = new ThemeService();

        _settings = _settingsStore.LoadAsync().GetAwaiter().GetResult();
        _settings = new AppSettings
        {
            DockEdge = _settings.DockEdge,
            ThemeMode = _settings.ThemeMode,
            DensityMode = _settings.DensityMode,
            StartWithWindows = _startupService.IsEnabled(),
        };
        _themeService.Apply(this, _settings);

        var shelfItems = _shelfStore.LoadAsync().GetAwaiter().GetResult();
        var dockService = new WindowDockService();
        var dragDropService = new DragDropService();
        var imageStore = new ImageStore(appDataRoot);
        var fileActionService = new FileActionService();
        var clipboardService = new ClipboardService();

        _shelfViewModel = new ShelfViewModel(
            OpenSettingsWindow,
            shelfItems,
            fileActionService,
            clipboardService,
            imageStore,
            _settings.DensityMode,
            _settings.ThemeMode);
        _shelfViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShelfViewModel.IsShelfVisible))
            {
                _trayIconService?.SetShelfVisible(_shelfViewModel.IsShelfVisible);
            }
        };

        _shelfWindow = new ShelfWindow(_shelfViewModel, dockService, dragDropService, imageStore, _settings.DockEdge);
        _shelfWindow.ApplySettings(_settings);
        _shelfWindow.Show();

        _trayIconService = new TrayIconService(
            () => _shelfViewModel.IsShelfVisible = true,
            () => _shelfViewModel.IsShelfVisible = false,
            OpenSettingsWindow,
            ShutdownApplication);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_shelfStore is not null && _shelfViewModel is not null)
        {
            _shelfStore.SaveAsync(_shelfViewModel.GetShelfItems()).GetAwaiter().GetResult();
        }

        if (_settingsStore is not null)
        {
            _settingsStore.SaveAsync(_settings).GetAwaiter().GetResult();
        }

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
            DataContext = new SettingsViewModel(
                _settings,
                _settingsStore,
                _startupService,
                ApplySettings),
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

    private void ApplySettings(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _themeService?.Apply(this, _settings);
        _shelfWindow?.ApplySettings(_settings);
    }
}

