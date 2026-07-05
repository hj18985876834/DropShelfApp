using System.IO;
using System.Windows.Input;
using DropShelf.App.Commands;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly Action<AppSettings>? _applySettings;
    private readonly SettingsStore? _settingsStore;
    private readonly StartupService? _startupService;
    private AppSettings _lastAppliedSettings;
    private DensityMode _densityMode;
    private DockEdge _dockEdge;
    private double _dockOffsetRatio;
    private bool _hasStatus;
    private bool _isApplying;
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
        _lastAppliedSettings = settings;
        _dockEdge = settings.DockEdge;
        _dockOffsetRatio = settings.DockOffsetRatio;
        _themeMode = settings.ThemeMode;
        _densityMode = settings.DensityMode;
        _startWithWindows = settings.StartWithWindows;

        ApplyCommand = new AsyncRelayCommand(_ => SaveAndApplyAsync("Settings saved."));
        ResetDockPositionCommand = new RelayCommand(_ => ResetDockPosition());
    }

    public IReadOnlyList<DockEdge> DockEdgeOptions { get; } = Enum.GetValues<DockEdge>();

    public IReadOnlyList<ThemeMode> ThemeModeOptions { get; } = Enum.GetValues<ThemeMode>();

    public IReadOnlyList<DensityMode> DensityModeOptions { get; } = Enum.GetValues<DensityMode>();

    public DockEdge DockEdge
    {
        get => _dockEdge;
        set => SetPendingProperty(ref _dockEdge, value);
    }

    public double DockOffsetRatio
    {
        get => _dockOffsetRatio;
        private set => SetPendingProperty(ref _dockOffsetRatio, value);
    }

    public ThemeMode ThemeMode
    {
        get => _themeMode;
        set => SetPendingProperty(ref _themeMode, value);
    }

    public DensityMode DensityMode
    {
        get => _densityMode;
        set => SetPendingProperty(ref _densityMode, value);
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

            ClearStatus();
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

    public bool IsApplying
    {
        get => _isApplying;
        private set => SetProperty(ref _isApplying, value);
    }

    public ICommand ApplyCommand { get; }

    public ICommand ResetDockPositionCommand { get; }

    public AppSettings Settings
    {
        get => CreateSettings();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _dockEdge = value.DockEdge;
            _dockOffsetRatio = value.DockOffsetRatio;
            _themeMode = value.ThemeMode;
            _densityMode = value.DensityMode;
            _startWithWindows = value.StartWithWindows;
            OnPropertyChanged(nameof(DockEdge));
            OnPropertyChanged(nameof(DockOffsetRatio));
            OnPropertyChanged(nameof(ThemeMode));
            OnPropertyChanged(nameof(DensityMode));
            OnPropertyChanged(nameof(StartWithWindows));
            OnPropertyChanged();
        }
    }

    private async Task SaveAndApplyAsync(string successMessage)
    {
        var settings = CreateSettings();

        try
        {
            IsApplying = true;
            _startupService?.SetEnabled(settings.StartWithWindows);
            if (_settingsStore is not null)
            {
                await _settingsStore.SaveAsync(settings);
            }

            _applySettings?.Invoke(settings);
            _lastAppliedSettings = settings;
            SetStatus(successMessage, isError: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or InvalidOperationException)
        {
            ApplySettingsToProperties(_lastAppliedSettings);
            SetStatus("Unable to save settings.", isError: true);
        }
        finally
        {
            IsApplying = false;
        }
    }

    private AppSettings CreateSettings()
    {
        return new AppSettings
        {
            DockEdge = DockEdge,
            DockOffsetRatio = DockOffsetRatio,
            ThemeMode = ThemeMode,
            DensityMode = DensityMode,
            StartWithWindows = StartWithWindows,
        };
    }

    private void ResetDockPosition()
    {
        DockEdge = DockEdge.Right;
        DockOffsetRatio = AppSettings.CreateDefault().DockOffsetRatio;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        HasStatus = true;
        IsStatusError = isError;
    }

    private void SetPendingProperty<T>(ref T field, T value)
    {
        if (SetProperty(ref field, value))
        {
            ClearStatus();
        }
    }

    private void ClearStatus()
    {
        StatusMessage = string.Empty;
        HasStatus = false;
        IsStatusError = false;
    }

    private void ApplySettingsToProperties(AppSettings settings)
    {
        _dockEdge = settings.DockEdge;
        _dockOffsetRatio = settings.DockOffsetRatio;
        _themeMode = settings.ThemeMode;
        _densityMode = settings.DensityMode;
        _startWithWindows = settings.StartWithWindows;
        OnPropertyChanged(nameof(DockEdge));
        OnPropertyChanged(nameof(DockOffsetRatio));
        OnPropertyChanged(nameof(ThemeMode));
        OnPropertyChanged(nameof(DensityMode));
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(Settings));
    }
}
