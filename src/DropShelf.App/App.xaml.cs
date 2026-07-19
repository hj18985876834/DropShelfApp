using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using DropShelf.App.Interop;
using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;
using DropShelf.App.Views;
using WpfClipboard = System.Windows.Clipboard;

namespace DropShelf.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "DropShelf.AppShell";
    private const string ShowExistingMessageName = "DropShelf.AppShell.ShowExisting";
    private static readonly TimeSpan HoverCollapseDelay = TimeSpan.FromMilliseconds(220);

    private readonly int _showExistingMessage = NativeMethods.RegisterWindowMessage(ShowExistingMessageName);
    private readonly object _shelfSaveGate = new();
    private readonly SemaphoreSlim _shelfSaveSemaphore = new(1, 1);
    private Mutex? _singleInstanceMutex;
    private ShelfWindow? _shelfWindow;
    private HandleWindow? _handleWindow;
    private SettingsWindow? _settingsWindow;
    private ShelfViewModel? _shelfViewModel;
    private AppSettings _settings = AppSettings.CreateDefault();
    private SettingsStore? _settingsStore;
    private ShelfStore? _shelfStore;
    private StartupLogService? _startupLogService;
    private StartupService? _startupService;
    private ThemeService? _themeService;
    private LocalizationService? _localizationService;
    private UpdateService? _updateService;
    private AutomaticUpdateCheckService? _automaticUpdateCheckService;
    private TrayIconService? _trayIconService;
    private GlobalHotkeyService? _globalHotkeyService;
    private DragDropService? _dragDropService;
    private ImageStore? _imageStore;
    private UpdateManifest? _knownAvailableUpdate;
    private bool _isShuttingDown;
    private bool _isHandleDragging;
    private bool _isHoverExpanded;
    private bool _isInternalShelfDragActive;
    private DispatcherTimer? _hoverCollapseTimer;
    private ShelfItem[]? _pendingShelfSaveSnapshot;
    private Task? _shelfSaveWorker;
    private bool _isQuickPasteRunning;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var ownsMutex);
        if (!ownsMutex)
        {
            NativeMethods.PostMessage(NativeMethods.HwndBroadcast, _showExistingMessage, 0, 0);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _hoverCollapseTimer = new DispatcherTimer { Interval = HoverCollapseDelay };
        _hoverCollapseTimer.Tick += (_, _) => CollapseHoverShelfIfPointerOutside();

        var appDataRoot = new AppDataPathService().GetAppDataRoot();
        _startupLogService = new StartupLogService(appDataRoot);
        _startupLogService.Write("Startup begin.");
        DispatcherUnhandledException += (_, args) =>
        {
            _startupLogService?.WriteException(args.Exception, "Dispatcher unhandled exception");
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                _startupLogService?.WriteException(exception, "Unhandled exception");
            }
        };
        _settingsStore = new SettingsStore(appDataRoot);
        _shelfStore = new ShelfStore(appDataRoot);
        _startupService = new StartupService();
        _themeService = new ThemeService();
        _updateService = new UpdateService(appDataRoot);
        _automaticUpdateCheckService = new AutomaticUpdateCheckService(_updateService);

        _settings = _settingsStore.LoadAsync().GetAwaiter().GetResult();
        _settings = new AppSettings
        {
            DockEdge = _settings.DockEdge,
            DockOffsetRatio = _settings.DockOffsetRatio,
            ThemeMode = _settings.ThemeMode,
            DensityMode = _settings.DensityMode,
            LanguageMode = _settings.LanguageMode,
            StartWithWindows = _startupService.IsEnabled(),
            IsShelfPinned = _settings.IsShelfPinned,
            AutoUpdateCheckMode = _settings.AutoUpdateCheckMode,
            LastAutomaticUpdateCheckUtc = _settings.LastAutomaticUpdateCheckUtc,
            PendingUpdateVersion = _settings.PendingUpdateVersion,
            LastUpdateCompletedVersion = _settings.LastUpdateCompletedVersion,
        };
        _themeService.Apply(this, _settings);
        _localizationService = new LocalizationService(_settings.LanguageMode);

        var shelfItems = _shelfStore.LoadAsync().GetAwaiter().GetResult();
        var dockService = new WindowDockService();
        _dragDropService = new DragDropService(_localizationService);
        _imageStore = new ImageStore(appDataRoot);
        var fileActionService = new FileActionService();
        var clipboardService = new ClipboardService();

        _shelfViewModel = new ShelfViewModel(
            OpenSettingsWindow,
            shelfItems,
            fileActionService,
            clipboardService,
            _imageStore,
            ConfirmClearAll,
            ConfirmRemoveSelected,
            SelectRelinkPath,
            _localizationService,
            _settings.DensityMode,
            _settings.ThemeMode,
            _settings.IsShelfPinned,
            SaveShelfPinState);
        _shelfViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShelfViewModel.IsShelfVisible))
            {
                _trayIconService?.SetShelfVisible(_shelfViewModel.IsShelfVisible);
            }
        };
        _shelfViewModel.Items.CollectionChanged += (_, _) => QueueShelfSave();

        _shelfWindow = new ShelfWindow(
            _shelfViewModel,
            dockService,
            _dragDropService,
            _imageStore,
            _settings,
            OnShellPointerEntered,
            OnShellPointerLeft,
            OnInternalShelfDragStarted,
            OnInternalShelfDragEnded);

        _handleWindow = new HandleWindow(
            dockService,
            _settings,
            ToggleShelfFromHandle,
            ShowShelfExplicitly,
            UpdateDockPlacement,
            OnHandleDragStarted,
            data => _dragDropService.CanCreateItems(data),
            async data =>
            {
                var items = await _dragDropService.CreateItemsAsync(data, _imageStore);
                if (items.Count > 0)
                {
                    _shelfViewModel.AddItems(items);
                    ShowShelfExplicitly();
                }
            },
            OnHandlePointerEntered,
            OnShellPointerLeft);
        _handleWindow.DataContext = _shelfViewModel;
        _shelfWindow.AttachHandleWindow(_handleWindow);
        _shelfWindow.ApplySettings(_settings);
        try
        {
            _handleWindow.Show();
            var source = HwndSource.FromHwnd(new WindowInteropHelper(_handleWindow).Handle);
            source?.AddHook(WndProc);
            _startupLogService.Write($"Handle shown at Left={_handleWindow.Left}, Top={_handleWindow.Top}, Width={_handleWindow.Width}, Height={_handleWindow.Height}.");
        }
        catch (Exception ex)
        {
            _startupLogService.WriteException(ex, "Handle show failed");
            throw;
        }

        _trayIconService = new TrayIconService(
            ShowShelfExplicitly,
            HideShelfExplicitly,
            OpenSettingsWindow,
            ShutdownApplication,
            _localizationService);
        RegisterGlobalHotkeys();
        _startupLogService.Write("Startup complete.");
        _ = RunPostStartupUpdateTasksAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isShuttingDown = true;
        SaveShelfSynchronously();

        if (_settingsStore is not null)
        {
            _settingsStore.SaveAsync(_settings).GetAwaiter().GetResult();
        }

        _startupLogService?.Write("Exit.");
        DisposeTrayIcon();
        DisposeGlobalHotkeys();
        _singleInstanceMutex?.Dispose();
        _shelfSaveSemaphore.Dispose();
        base.OnExit(e);
    }

    private void QueueShelfSave()
    {
        if (_isShuttingDown || _shelfStore is null || _shelfViewModel is null)
        {
            return;
        }

        var snapshot = _shelfViewModel.GetShelfItems().ToArray();
        lock (_shelfSaveGate)
        {
            _pendingShelfSaveSnapshot = snapshot;
            _shelfSaveWorker ??= SaveShelfSnapshotsAsync();
        }
    }

    private async Task SaveShelfSnapshotsAsync()
    {
        while (true)
        {
            ShelfItem[] snapshot;
            lock (_shelfSaveGate)
            {
                if (_isShuttingDown)
                {
                    _pendingShelfSaveSnapshot = null;
                    _shelfSaveWorker = null;
                    return;
                }

                if (_pendingShelfSaveSnapshot is null)
                {
                    _shelfSaveWorker = null;
                    return;
                }

                snapshot = _pendingShelfSaveSnapshot;
                _pendingShelfSaveSnapshot = null;
            }

            try
            {
                await _shelfSaveSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_shelfStore is not null)
                    {
                        await _shelfStore.SaveAsync(snapshot).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _shelfSaveSemaphore.Release();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ObjectDisposedException)
            {
            }
        }
    }

    private void SaveShelfSynchronously()
    {
        if (_shelfStore is null || _shelfViewModel is null)
        {
            return;
        }

        var snapshot = _shelfViewModel.GetShelfItems().ToArray();
        lock (_shelfSaveGate)
        {
            _pendingShelfSaveSnapshot = null;
        }

        _shelfSaveSemaphore.Wait();
        try
        {
            _shelfStore.SaveAsync(snapshot).GetAwaiter().GetResult();
        }
        finally
        {
            _shelfSaveSemaphore.Release();
        }
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
                _updateService,
                ShutdownApplication,
                ApplySettings,
                ConfirmUpdateInstall,
                _knownAvailableUpdate),
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
        DisposeGlobalHotkeys();
        DisposeTrayIcon();
        _settingsWindow?.Close();
        _shelfWindow?.ForceClose();
        _handleWindow?.ForceClose();
        Shutdown();
        Dispatcher.InvokeShutdown();
    }

    private bool ConfirmClearAll(int itemCount)
    {
        var localizationService = _localizationService ?? new LocalizationService(_settings.LanguageMode);
        var texts = localizationService.Text;
        var message = localizationService.ClearAllMessage(itemCount);
        var result = System.Windows.MessageBox.Show(
            message,
            texts.ClearAllTitle,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No);

        return result == System.Windows.MessageBoxResult.Yes;
    }

    private bool ConfirmRemoveSelected(int itemCount)
    {
        var localizationService = _localizationService ?? new LocalizationService(_settings.LanguageMode);
        var texts = localizationService.Text;
        var message = localizationService.RemoveSelectedMessage(itemCount);
        var result = System.Windows.MessageBox.Show(
            message,
            texts.ClearAllTitle,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No);

        return result == System.Windows.MessageBoxResult.Yes;
    }

    private bool ConfirmUpdateInstall(string title, string message)
    {
        var result = System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Information,
            System.Windows.MessageBoxResult.No);

        return result == System.Windows.MessageBoxResult.Yes;
    }

    private async Task RunPostStartupUpdateTasksAsync()
    {
        await ShowUpdateCompletedNoticeIfNeededAsync();
        await RunAutomaticUpdateCheckAsync();
    }

    private async Task RunAutomaticUpdateCheckAsync()
    {
        if (_automaticUpdateCheckService is null)
        {
            return;
        }

        try
        {
            var result = await _automaticUpdateCheckService.CheckAsync(_settings, SettingsViewModel.GetApplicationVersion());
            if (!result.DidCheck)
            {
                return;
            }

            _settings = result.Settings;
            await SaveCurrentSettingsAsync("Automatic update check settings save failed");
            if (!result.IsUpdateAvailable || result.Manifest is null)
            {
                return;
            }

            _knownAvailableUpdate = result.Manifest;
            var localizationService = _localizationService ?? new LocalizationService(_settings.LanguageMode);
            var texts = localizationService.Text;
            _trayIconService?.ShowInfo(
                texts.AutomaticUpdateAvailableTitle,
                string.Format(System.Globalization.CultureInfo.CurrentCulture, texts.AutomaticUpdateAvailableMessageFormat, result.Manifest.Version));

            if (_settingsWindow?.DataContext is SettingsViewModel settingsViewModel)
            {
                settingsViewModel.ApplyAvailableUpdate(result.Manifest, updateStatus: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            _startupLogService?.WriteException(ex, "Automatic update check failed");
        }
    }

    private async Task ShowUpdateCompletedNoticeIfNeededAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.PendingUpdateVersion))
        {
            return;
        }

        var currentVersion = SettingsViewModel.GetApplicationVersion();
        try
        {
            if (AppVersion.Parse(currentVersion).CompareTo(AppVersion.Parse(_settings.PendingUpdateVersion)) < 0 ||
                string.Equals(_settings.LastUpdateCompletedVersion, _settings.PendingUpdateVersion, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
        catch (FormatException)
        {
            return;
        }

        var completedVersion = _settings.PendingUpdateVersion;
        _settings = new AppSettings
        {
            DockEdge = _settings.DockEdge,
            DockOffsetRatio = _settings.DockOffsetRatio,
            ThemeMode = _settings.ThemeMode,
            DensityMode = _settings.DensityMode,
            LanguageMode = _settings.LanguageMode,
            StartWithWindows = _settings.StartWithWindows,
            IsShelfPinned = _settings.IsShelfPinned,
            AutoUpdateCheckMode = _settings.AutoUpdateCheckMode,
            LastAutomaticUpdateCheckUtc = _settings.LastAutomaticUpdateCheckUtc,
            PendingUpdateVersion = null,
            LastUpdateCompletedVersion = completedVersion,
        };

        var localizationService = _localizationService ?? new LocalizationService(_settings.LanguageMode);
        var texts = localizationService.Text;
        _trayIconService?.ShowInfo(
            texts.UpdateCompletedTitle,
            string.Format(System.Globalization.CultureInfo.CurrentCulture, texts.UpdateCompletedMessageFormat, completedVersion));
        await SaveCurrentSettingsAsync("Update completed settings save failed");
    }

    private string? SelectRelinkPath(ShelfItem item)
    {
        return item.Type switch
        {
            ShelfItemType.File => SelectRelinkFilePath(item),
            ShelfItemType.Folder => SelectRelinkFolderPath(item),
            _ => null,
        };
    }

    private static string? SelectRelinkFilePath(ShelfItem item)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = false,
            FileName = item.DisplayName,
        };

        if (!string.IsNullOrWhiteSpace(item.SourcePath))
        {
            var directory = Path.GetDirectoryName(item.SourcePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                dialog.InitialDirectory = directory;
            }
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string? SelectRelinkFolderPath(ShelfItem item)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            UseDescriptionForTitle = true,
            Description = item.DisplayName,
            ShowNewFolderButton = false,
        };

        if (!string.IsNullOrWhiteSpace(item.SourcePath) && Directory.Exists(item.SourcePath))
        {
            dialog.SelectedPath = item.SourcePath;
        }

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }

    private void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var shouldRestoreCurrentPinState = settings.IsShelfPinned != _settings.IsShelfPinned;
        var mergedLastAutomaticUpdateCheckUtc = LatestUtc(_settings.LastAutomaticUpdateCheckUtc, settings.LastAutomaticUpdateCheckUtc);
        _settings = new AppSettings
        {
            DockEdge = settings.DockEdge,
            DockOffsetRatio = settings.DockOffsetRatio,
            ThemeMode = settings.ThemeMode,
            DensityMode = settings.DensityMode,
            LanguageMode = settings.LanguageMode,
            StartWithWindows = settings.StartWithWindows,
            IsShelfPinned = _settings.IsShelfPinned,
            AutoUpdateCheckMode = settings.AutoUpdateCheckMode,
            LastAutomaticUpdateCheckUtc = mergedLastAutomaticUpdateCheckUtc,
            PendingUpdateVersion = FirstNonEmpty(_settings.PendingUpdateVersion, settings.PendingUpdateVersion),
            LastUpdateCompletedVersion = FirstNonEmpty(_settings.LastUpdateCompletedVersion, settings.LastUpdateCompletedVersion),
        };
        _localizationService?.SetLanguage(_settings.LanguageMode);
        _themeService?.Apply(this, _settings);
        _shelfWindow?.ApplySettings(_settings);
        _handleWindow?.ApplySettings(_settings);
        if (shouldRestoreCurrentPinState)
        {
            _ = SaveCurrentSettingsAsync("Pin state restore after settings apply failed");
        }
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
            LanguageMode = _settings.LanguageMode,
            StartWithWindows = _settings.StartWithWindows,
            IsShelfPinned = _settings.IsShelfPinned,
            AutoUpdateCheckMode = _settings.AutoUpdateCheckMode,
            LastAutomaticUpdateCheckUtc = _settings.LastAutomaticUpdateCheckUtc,
            PendingUpdateVersion = _settings.PendingUpdateVersion,
            LastUpdateCompletedVersion = _settings.LastUpdateCompletedVersion,
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

    private void ShowShelfFromSecondLaunch()
    {
        _startupLogService?.Write("Second launch requested show.");
        ShowShelfExplicitly();
        BringWindowForward(_shelfWindow);
        BringWindowForward(_handleWindow);
    }

    private static void BringWindowForward(Window? window)
    {
        if (window is null)
        {
            return;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == 0)
        {
            return;
        }

        NativeMethods.ShowWindow(handle, NativeMethods.SwShow);
        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HwndTopMost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpShowWindow);
        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HwndNoTopMost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpShowWindow);
        NativeMethods.SetForegroundWindow(handle);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == _showExistingMessage)
        {
            ShowShelfFromSecondLaunch();
            handled = true;
        }
        else if (msg == NativeMethods.WmHotkey)
        {
            HandleGlobalHotkey((int)wParam);
            handled = true;
        }

        return 0;
    }

    private void RegisterGlobalHotkeys()
    {
        if (_handleWindow is null)
        {
            return;
        }

        var handle = new WindowInteropHelper(_handleWindow).Handle;
        if (handle == 0)
        {
            return;
        }

        _globalHotkeyService = new GlobalHotkeyService(handle);
        if (_globalHotkeyService.RegisterDefaultHotkeys())
        {
            return;
        }

        var texts = (_localizationService ?? new LocalizationService(_settings.LanguageMode)).Text;
        _trayIconService?.ShowInfo(texts.HotkeyRegistrationFailedTitle, texts.HotkeyRegistrationFailedMessage);
        _startupLogService?.Write("One or more global hotkeys failed to register.");
    }

    private void HandleGlobalHotkey(int hotkeyId)
    {
        switch (hotkeyId)
        {
            case GlobalHotkeyService.ToggleShelfHotkeyId:
                ToggleShelfFromHandle();
                if (_shelfViewModel?.IsShelfVisible == true)
                {
                    BringWindowForward(_shelfWindow);
                    BringWindowForward(_handleWindow);
                }

                break;
            case GlobalHotkeyService.QuickPasteHotkeyId:
                _ = QuickPasteClipboardContentAsync();
                break;
        }
    }

    private async Task QuickPasteClipboardContentAsync()
    {
        if (_isQuickPasteRunning || _dragDropService is null || _imageStore is null || _shelfViewModel is null)
        {
            return;
        }

        _isQuickPasteRunning = true;
        try
        {
            var texts = (_localizationService ?? new LocalizationService(_settings.LanguageMode)).Text;
            var dataObject = WpfClipboard.GetDataObject();
            if (dataObject is null)
            {
                _trayIconService?.ShowInfo(texts.QuickPasteTitle, texts.QuickPasteUnsupported);
                return;
            }

            var items = await _dragDropService.CreateItemsAsync(dataObject, _imageStore);
            if (items.Count == 0)
            {
                _trayIconService?.ShowInfo(texts.QuickPasteTitle, texts.QuickPasteUnsupported);
                return;
            }

            var result = _shelfViewModel.AddItems(items);
            _trayIconService?.ShowInfo(texts.QuickPasteTitle, _shelfViewModel.ShelfStatusMessage ?? _localizationService!.AddItemsMessage(result.AddedCount, result.DuplicateCount));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
        {
            var texts = (_localizationService ?? new LocalizationService(_settings.LanguageMode)).Text;
            _trayIconService?.ShowInfo(texts.QuickPasteTitle, texts.QuickPasteFailed);
            _startupLogService?.WriteException(ex, "Quick paste failed");
        }
        finally
        {
            _isQuickPasteRunning = false;
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
        if (!_isHoverExpanded || _isHandleDragging || _isInternalShelfDragActive)
        {
            return;
        }

        if (_shelfViewModel?.IsShelfPinned == true || _shelfWindow?.IsKeyboardFocusWithin == true)
        {
            StopHoverCollapseTimer();
            return;
        }

        StopHoverCollapseTimer();
        _hoverCollapseTimer?.Start();
    }

    private void CollapseHoverShelfIfPointerOutside()
    {
        StopHoverCollapseTimer();
        if (!_isHoverExpanded || _isInternalShelfDragActive)
        {
            return;
        }

        if (_shelfViewModel?.IsShelfPinned == true || _shelfWindow?.IsKeyboardFocusWithin == true)
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

    private void OnInternalShelfDragStarted()
    {
        _isInternalShelfDragActive = true;
        StopHoverCollapseTimer();
    }

    private void OnInternalShelfDragEnded()
    {
        _isInternalShelfDragActive = false;
        if (!_isHoverExpanded)
        {
            return;
        }

        if (_handleWindow?.IsMouseOver == true || _shelfWindow?.IsMouseOver == true)
        {
            return;
        }

        StopHoverCollapseTimer();
        _hoverCollapseTimer?.Start();
    }

    private void StopHoverCollapseTimer()
    {
        _hoverCollapseTimer?.Stop();
    }

    private async void SaveShelfPinState(bool isPinned)
    {
        _settings = new AppSettings
        {
            DockEdge = _settings.DockEdge,
            DockOffsetRatio = _settings.DockOffsetRatio,
            ThemeMode = _settings.ThemeMode,
            DensityMode = _settings.DensityMode,
            LanguageMode = _settings.LanguageMode,
            StartWithWindows = _settings.StartWithWindows,
            IsShelfPinned = isPinned,
            AutoUpdateCheckMode = _settings.AutoUpdateCheckMode,
            LastAutomaticUpdateCheckUtc = _settings.LastAutomaticUpdateCheckUtc,
            PendingUpdateVersion = _settings.PendingUpdateVersion,
            LastUpdateCompletedVersion = _settings.LastUpdateCompletedVersion,
        };

        if (isPinned)
        {
            StopHoverCollapseTimer();
        }

        try
        {
            await SaveCurrentSettingsAsync("Pin state save failed");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _startupLogService?.WriteException(ex, "Pin state save failed");
        }
    }

    private async Task SaveCurrentSettingsAsync(string failureContext)
    {
        try
        {
            if (_settingsStore is not null)
            {
                await _settingsStore.SaveAsync(_settings);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _startupLogService?.WriteException(ex, failureContext);
        }
    }

    private static DateTimeOffset? LatestUtc(DateTimeOffset? first, DateTimeOffset? second)
    {
        if (first is null)
        {
            return second?.ToUniversalTime();
        }

        if (second is null)
        {
            return first.Value.ToUniversalTime();
        }

        var firstUtc = first.Value.ToUniversalTime();
        var secondUtc = second.Value.ToUniversalTime();
        return firstUtc >= secondUtc ? firstUtc : secondUtc;
    }

    private static string? FirstNonEmpty(string? first, string? second)
    {
        return !string.IsNullOrWhiteSpace(first) ? first : second;
    }

    private void DisposeTrayIcon()
    {
        _trayIconService?.Dispose();
        _trayIconService = null;
    }

    private void DisposeGlobalHotkeys()
    {
        _globalHotkeyService?.Dispose();
        _globalHotkeyService = null;
    }
}

