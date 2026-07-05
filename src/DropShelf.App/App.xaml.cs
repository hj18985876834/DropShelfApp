using System.Threading;
using System.Windows;
using System.Windows.Threading;
using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;
using DropShelf.App.Views;

namespace DropShelf.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "DropShelf.AppShell";
    private static readonly TimeSpan HoverCollapseDelay = TimeSpan.FromMilliseconds(220);

    private Mutex? _singleInstanceMutex;
    private ShelfWindow? _shelfWindow;
    private HandleWindow? _handleWindow;
    private SettingsWindow? _settingsWindow;
    private ShelfViewModel? _shelfViewModel;
    private AppSettings _settings = AppSettings.CreateDefault();
    private SettingsStore? _settingsStore;
    private ShelfStore? _shelfStore;
    private StartupService? _startupService;
    private ThemeService? _themeService;
    private TrayIconService? _trayIconService;
    private bool _isShuttingDown;
    private bool _isHandleDragging;
    private bool _isHoverExpanded;
    private DispatcherTimer? _hoverCollapseTimer;

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
        _hoverCollapseTimer = new DispatcherTimer { Interval = HoverCollapseDelay };
        _hoverCollapseTimer.Tick += (_, _) => CollapseHoverShelfIfPointerOutside();

        var appDataRoot = new AppDataPathService().GetAppDataRoot();
        _settingsStore = new SettingsStore(appDataRoot);
        _shelfStore = new ShelfStore(appDataRoot);
        _startupService = new StartupService();
        _themeService = new ThemeService();

        _settings = _settingsStore.LoadAsync().GetAwaiter().GetResult();
        _settings = new AppSettings
        {
            DockEdge = _settings.DockEdge,
            DockOffsetRatio = _settings.DockOffsetRatio,
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

        _shelfWindow = new ShelfWindow(
            _shelfViewModel,
            dockService,
            dragDropService,
            imageStore,
            _settings,
            OnShellPointerEntered,
            OnShellPointerLeft);

        _handleWindow = new HandleWindow(
            dockService,
            _settings,
            ToggleShelfFromHandle,
            ShowShelfExplicitly,
            UpdateDockPlacement,
            OnHandleDragStarted,
            data => dragDropService.CanCreateItems(data),
            data =>
            {
                var items = dragDropService.CreateItems(data, imageStore);
                if (items.Count > 0)
                {
                    _shelfViewModel.AddItems(items);
                    ShowShelfExplicitly();
                }
            },
            OnHandlePointerEntered,
            OnShellPointerLeft);
        _shelfWindow.AttachHandleWindow(_handleWindow);
        _shelfWindow.ApplySettings(_settings);
        _handleWindow.Show();

        _trayIconService = new TrayIconService(
            ShowShelfExplicitly,
            HideShelfExplicitly,
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

        DisposeTrayIcon();
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
            Owner = _handleWindow ?? (Window?)_shelfWindow,
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
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        DisposeTrayIcon();
        _settingsWindow?.Close();
        _shelfWindow?.ForceClose();
        _handleWindow?.ForceClose();
        Shutdown();
        Dispatcher.InvokeShutdown();
    }

    private void ApplySettings(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _themeService?.Apply(this, _settings);
        _shelfWindow?.ApplySettings(_settings);
        _handleWindow?.ApplySettings(_settings);
    }

    private void UpdateDockPlacement(DockPlacement placement)
    {
        _isHandleDragging = false;
        _settings = new AppSettings
        {
            DockEdge = placement.DockEdge,
            DockOffsetRatio = placement.DockOffsetRatio,
            ThemeMode = _settings.ThemeMode,
            DensityMode = _settings.DensityMode,
            StartWithWindows = _settings.StartWithWindows,
        };
        _shelfWindow?.ApplySettings(_settings);
    }

    private void ToggleShelfFromHandle()
    {
        StopHoverCollapseTimer();
        if (_shelfViewModel is null)
        {
            return;
        }

        if (_isHoverExpanded && _shelfViewModel.IsShelfVisible)
        {
            _isHoverExpanded = false;
            return;
        }

        _isHoverExpanded = false;
        _shelfViewModel.IsShelfVisible = !_shelfViewModel.IsShelfVisible;
    }

    private void ShowShelfExplicitly()
    {
        StopHoverCollapseTimer();
        _isHoverExpanded = false;
        if (_shelfViewModel is not null)
        {
            _shelfViewModel.IsShelfVisible = true;
        }
    }

    private void HideShelfExplicitly()
    {
        StopHoverCollapseTimer();
        _isHoverExpanded = false;
        if (_shelfViewModel is not null)
        {
            _shelfViewModel.IsShelfVisible = false;
        }
    }

    private void OnHandleDragStarted()
    {
        _isHandleDragging = true;
        HideShelfExplicitly();
    }

    private void OnHandlePointerEntered()
    {
        if (_isHandleDragging)
        {
            return;
        }

        StopHoverCollapseTimer();
        _isHoverExpanded = true;
        if (_shelfViewModel is not null)
        {
            _shelfViewModel.IsShelfVisible = true;
        }
    }

    private void OnShellPointerEntered()
    {
        if (_isHoverExpanded)
        {
            StopHoverCollapseTimer();
        }
    }

    private void OnShellPointerLeft()
    {
        if (!_isHoverExpanded || _isHandleDragging)
        {
            return;
        }

        StopHoverCollapseTimer();
        _hoverCollapseTimer?.Start();
    }

    private void CollapseHoverShelfIfPointerOutside()
    {
        StopHoverCollapseTimer();
        if (!_isHoverExpanded)
        {
            return;
        }

        if (_handleWindow?.IsMouseOver == true || _shelfWindow?.IsMouseOver == true)
        {
            return;
        }

        _isHoverExpanded = false;
        if (_shelfViewModel is not null)
        {
            _shelfViewModel.IsShelfVisible = false;
        }
    }

    private void StopHoverCollapseTimer()
    {
        _hoverCollapseTimer?.Stop();
    }

    private void DisposeTrayIcon()
    {
        _trayIconService?.Dispose();
        _trayIconService = null;
    }
}

