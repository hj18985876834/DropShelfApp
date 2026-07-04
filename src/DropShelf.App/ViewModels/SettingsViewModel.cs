using System.IO;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly Action<AppSettings>? _applySettings;
    private readonly SettingsStore? _settingsStore;
    private readonly StartupService? _startupService;
    private DensityMode _densityMode;
    private DockEdge _dockEdge;
    private bool _hasStatus;
    private bool _isStatusError;
    private bool _startWithWindows;
    private string _statusMessage = string.Empty;
    private ThemeMode _themeMode;

    public SettingsViewModel()
        : this(AppSettings.CreateDefault())
    {
    }

    public SettingsViewModel(AppSettings settings)
        : this(settings, null, null, null)
    {
    }

    public SettingsViewModel(
        AppSettings settings,
        SettingsStore? settingsStore,
        StartupService? startupService,
        Action<AppSettings>? applySettings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settingsStore = settingsStore;
        _startupService = startupService;
        _applySettings = applySettings;
        _dockEdge = settings.DockEdge;
        _themeMode = settings.ThemeMode;
        _densityMode = settings.DensityMode;
        _startWithWindows = settings.StartWithWindows;
    }

    public IReadOnlyList<DockEdge> DockEdgeOptions { get; } = Enum.GetValues<DockEdge>();

    public IReadOnlyList<ThemeMode> ThemeModeOptions { get; } = Enum.GetValues<ThemeMode>();

    public IReadOnlyList<DensityMode> DensityModeOptions { get; } = Enum.GetValues<DensityMode>();

    public DockEdge DockEdge
    {
        get => _dockEdge;
        set
        {
            if (SetProperty(ref _dockEdge, value))
            {
                SaveAndApply("Dock edge saved.");
            }
        }
    }

    public ThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (SetProperty(ref _themeMode, value))
            {
                SaveAndApply("Theme saved.");
            }
        }
    }

    public DensityMode DensityMode
    {
        get => _densityMode;
        set
        {
            if (SetProperty(ref _densityMode, value))
            {
                SaveAndApply("Density saved.");
            }
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (!SetProperty(ref _startWithWindows, value))
            {
                return;
            }

            try
            {
                _startupService?.SetEnabled(value);
                SaveAndApply(value ? "Startup enabled." : "Startup disabled.");
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or InvalidOperationException)
            {
                _startWithWindows = !value;
                OnPropertyChanged();
                SetStatus("Unable to update the Windows startup setting.", isError: true);
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasStatus
    {
        get => _hasStatus;
        private set => SetProperty(ref _hasStatus, value);
    }

    public bool IsStatusError
    {
        get => _isStatusError;
        private set => SetProperty(ref _isStatusError, value);
    }

    public AppSettings Settings
    {
        get => CreateSettings();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _dockEdge = value.DockEdge;
            _themeMode = value.ThemeMode;
            _densityMode = value.DensityMode;
            _startWithWindows = value.StartWithWindows;
            OnPropertyChanged(nameof(DockEdge));
            OnPropertyChanged(nameof(ThemeMode));
            OnPropertyChanged(nameof(DensityMode));
            OnPropertyChanged(nameof(StartWithWindows));
            OnPropertyChanged();
        }
    }

    private void SaveAndApply(string successMessage)
    {
        var settings = CreateSettings();

        try
        {
            _settingsStore?.SaveAsync(settings).GetAwaiter().GetResult();
            _applySettings?.Invoke(settings);
            SetStatus(successMessage, isError: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or InvalidOperationException)
        {
            SetStatus("Unable to save settings.", isError: true);
        }
    }

    private AppSettings CreateSettings()
    {
        return new AppSettings
        {
            DockEdge = DockEdge,
            ThemeMode = ThemeMode,
            DensityMode = DensityMode,
            StartWithWindows = StartWithWindows,
        };
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        HasStatus = true;
        IsStatusError = isError;
    }
}
