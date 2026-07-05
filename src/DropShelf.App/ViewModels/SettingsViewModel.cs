using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;
using DropShelf.App.Commands;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly Action<AppSettings>? _applySettings;
    private readonly LocalizationService _localizationService;
    private readonly SettingsStore? _settingsStore;
    private readonly StartupService? _startupService;
    private readonly UpdateService? _updateService;
    private readonly Action? _shutdownApplication;
    private AppBranding _appBranding = AppBranding.Default;
    private AppSettings _lastAppliedSettings;
    private UpdateManifest? _availableUpdate;
    private DensityMode _densityMode;
    private DockEdge _dockEdge;
    private double _dockOffsetRatio;
    private bool _hasStatus;
    private bool _isApplying;
    private bool _isCheckingForUpdate;
    private bool _isStatusError;
    private bool _isUpdateAvailable;
    private bool _isUpdating;
    private LanguageMode _languageMode;
    private bool _startWithWindows;
    private string _statusMessage = string.Empty;
    private ThemeMode _themeMode;
    private readonly IReadOnlyList<LocalizedOption<ThemeMode>> _themeModeOptions;
    private readonly IReadOnlyList<LocalizedOption<DensityMode>> _densityModeOptions;
    private readonly IReadOnlyList<LocalizedOption<LanguageMode>> _languageModeOptions;

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
        : this(settings, settingsStore, startupService, null, null, applySettings)
    {
    }

    public SettingsViewModel(
        AppSettings settings,
        SettingsStore? settingsStore,
        StartupService? startupService,
        UpdateService? updateService,
        Action? shutdownApplication,
        Action<AppSettings>? applySettings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _localizationService = new LocalizationService(settings.LanguageMode);
        _settingsStore = settingsStore;
        _startupService = startupService;
        _updateService = updateService;
        _shutdownApplication = shutdownApplication;
        _applySettings = applySettings;
        _lastAppliedSettings = settings;
        _dockEdge = settings.DockEdge;
        _dockOffsetRatio = settings.DockOffsetRatio;
        _themeMode = settings.ThemeMode;
        _densityMode = settings.DensityMode;
        _languageMode = settings.LanguageMode;
        _startWithWindows = settings.StartWithWindows;
        _themeModeOptions = Enum.GetValues<ThemeMode>()
            .Select(value => new LocalizedOption<ThemeMode>(value, GetThemeModeDisplayName(value)))
            .ToArray();
        _densityModeOptions = Enum.GetValues<DensityMode>()
            .Select(value => new LocalizedOption<DensityMode>(value, GetDensityModeDisplayName(value)))
            .ToArray();
        _languageModeOptions = Enum.GetValues<LanguageMode>()
            .Select(value => new LocalizedOption<LanguageMode>(value, GetLanguageModeDisplayName(value)))
            .ToArray();

        ApplyCommand = new AsyncRelayCommand(_ => SaveAndApplyAsync(SavedMessage));
        CheckForUpdatesCommand = new AsyncRelayCommand(_ => CheckForUpdatesAsync(), _ => _updateService is not null);
        DownloadUpdateCommand = new AsyncRelayCommand(_ => DownloadAndInstallUpdateAsync(), _ => _updateService is not null && IsUpdateAvailable && _availableUpdate is not null);
        ResetDockPositionCommand = new RelayCommand(_ => ResetDockPosition());
    }

    public IReadOnlyList<DockEdge> DockEdgeOptions { get; } = Enum.GetValues<DockEdge>();

    public IReadOnlyList<LocalizedOption<ThemeMode>> ThemeModeOptions => _themeModeOptions;

    public IReadOnlyList<LocalizedOption<DensityMode>> DensityModeOptions => _densityModeOptions;

    public IReadOnlyList<LocalizedOption<LanguageMode>> LanguageModeOptions => _languageModeOptions;

    private AppText Text => _localizationService.Text;

    public string WindowTitle => Text.SettingsWindowTitle;

    public string HeaderTitle => Text.SettingsHeaderTitle;

    public string PreferencesTitle => Text.PreferencesTitle;

    public string ShelfPositionLabel => Text.ShelfPositionLabel;

    public string ResetDockPositionText => Text.ResetDockPositionText;

    public string ThemeLabel => Text.ThemeLabel;

    public string DensityLabel => Text.DensityLabel;

    public string LanguageLabel => Text.LanguageLabel;

    public string StartWithWindowsText => Text.StartWithWindowsText;

    public string AboutTitle => Text.AboutTitle;

    public string SoftwareLabel => Text.SoftwareLabel;

    public string IntroductionLabel => Text.IntroductionLabel;

    public string UsageLabel => Text.UsageLabel;

    public string VersionLabel => Text.VersionLabel;

    public string UpdatesTitle => Text.UpdatesTitle;

    public string CheckForUpdatesText => Text.CheckForUpdatesText;

    public string DownloadUpdateText => Text.DownloadUpdateText;

    public string DeveloperLabel => Text.DeveloperLabel;

    public string ContactLabel => Text.ContactLabel;

    public string ApplyText => Text.ApplyText;

    public string CloseText => Text.CloseText;

    public string AppName => _appBranding.DisplayNameOrDefault();

    public string AppDescription => _appBranding.DescriptionFor(_localizationService.IsChinese);

    public string UsageGuide => Text.UsageGuide;

    public string Version => GetApplicationVersion();

    public string Developer => Text.Developer;

    public string Contact => "2748432469@qq.com";

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

    public LanguageMode LanguageMode
    {
        get => _languageMode;
        set
        {
            if (!SetProperty(ref _languageMode, value))
            {
                return;
            }

            ClearStatus();
            _localizationService.SetLanguage(value);
            RaiseLocalizedTextChanged();
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

    public bool IsCheckingForUpdate
    {
        get => _isCheckingForUpdate;
        private set => SetProperty(ref _isCheckingForUpdate, value);
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        private set => SetProperty(ref _isUpdating, value);
    }

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set
        {
            if (SetProperty(ref _isUpdateAvailable, value) &&
                DownloadUpdateCommand is AsyncRelayCommand downloadUpdateCommand)
            {
                downloadUpdateCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand ApplyCommand { get; }

    public ICommand CheckForUpdatesCommand { get; }

    public ICommand DownloadUpdateCommand { get; }

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
            _languageMode = value.LanguageMode;
            _startWithWindows = value.StartWithWindows;
            OnPropertyChanged(nameof(DockEdge));
            OnPropertyChanged(nameof(DockOffsetRatio));
            OnPropertyChanged(nameof(ThemeMode));
            OnPropertyChanged(nameof(DensityMode));
            OnPropertyChanged(nameof(LanguageMode));
            OnPropertyChanged(nameof(StartWithWindows));
            RaiseLocalizedTextChanged();
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
            SetStatus(SaveErrorMessage, isError: true);
        }
        finally
        {
            IsApplying = false;
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_updateService is null)
        {
            SetStatus(UpdateFailedMessage, isError: true);
            return;
        }

        try
        {
            IsCheckingForUpdate = true;
            IsUpdateAvailable = false;
            _availableUpdate = null;
            SetStatus(Text.CheckingForUpdates, isError: false);

            var result = await _updateService.CheckForUpdatesAsync(Version);
            ApplyBranding(result.Manifest.Branding);
            if (!result.IsUpdateAvailable)
            {
                SetStatus(Text.NoUpdateAvailable, isError: false);
                return;
            }

            _availableUpdate = result.Manifest;
            IsUpdateAvailable = true;
            var message = string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.UpdateAvailableFormat, result.Manifest.Version);
            var notes = result.Manifest.ReleaseNotesFor(_localizationService.IsChinese);
            if (!string.IsNullOrWhiteSpace(notes))
            {
                message = $"{message} {notes}";
            }

            SetStatus(message, isError: false);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or JsonException or FormatException or IOException or TaskCanceledException)
        {
            IsUpdateAvailable = false;
            _availableUpdate = null;
            SetStatus(UpdateFailedMessage, isError: true);
        }
        finally
        {
            IsCheckingForUpdate = false;
        }
    }

    private async Task DownloadAndInstallUpdateAsync()
    {
        if (_updateService is null || _availableUpdate is null)
        {
            SetStatus(UpdateFailedMessage, isError: true);
            return;
        }

        try
        {
            IsUpdating = true;
            SetStatus(Text.DownloadingUpdate, isError: false);
            var progress = new Progress<double>(value =>
            {
                var percent = Math.Clamp((int)Math.Round(value * 100), 0, 100);
                SetStatus(string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.DownloadProgressFormat, percent), isError: false);
            });

            var installerPath = await _updateService.DownloadInstallerAsync(_availableUpdate, progress);
            _updateService.LaunchInstaller(installerPath);
            _shutdownApplication?.Invoke();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or TaskCanceledException)
        {
            SetStatus(UpdateFailedMessage, isError: true);
        }
        finally
        {
            IsUpdating = false;
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
            LanguageMode = LanguageMode,
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

    private void ApplyBranding(AppBranding? branding)
    {
        if (branding is null)
        {
            return;
        }

        _appBranding = branding;
        OnPropertyChanged(nameof(AppName));
        OnPropertyChanged(nameof(AppDescription));
    }

    private void ApplySettingsToProperties(AppSettings settings)
    {
        _dockEdge = settings.DockEdge;
        _dockOffsetRatio = settings.DockOffsetRatio;
        _themeMode = settings.ThemeMode;
        _densityMode = settings.DensityMode;
        _languageMode = settings.LanguageMode;
        _startWithWindows = settings.StartWithWindows;
        OnPropertyChanged(nameof(DockEdge));
        OnPropertyChanged(nameof(DockOffsetRatio));
        OnPropertyChanged(nameof(ThemeMode));
        OnPropertyChanged(nameof(DensityMode));
        OnPropertyChanged(nameof(LanguageMode));
        OnPropertyChanged(nameof(StartWithWindows));
        RaiseLocalizedTextChanged();
        OnPropertyChanged(nameof(Settings));
    }

    public string GetLanguageModeDisplayName(LanguageMode value)
    {
        return value switch
        {
            _ => _localizationService.LanguageName(value),
        };
    }

    public string GetThemeModeDisplayName(ThemeMode value)
    {
        return value switch
        {
            _ => _localizationService.ThemeName(value),
        };
    }

    public string GetDensityModeDisplayName(DensityMode value)
    {
        return value switch
        {
            _ => _localizationService.DensityName(value),
        };
    }

    private string SavedMessage => Text.SettingsSaved;

    private string SaveErrorMessage => Text.SettingsSaveFailed;

    private string UpdateFailedMessage => Text.UpdateFailed;

    private void RaiseLocalizedTextChanged()
    {
        RefreshOptionDisplayNames();
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(PreferencesTitle));
        OnPropertyChanged(nameof(ShelfPositionLabel));
        OnPropertyChanged(nameof(ResetDockPositionText));
        OnPropertyChanged(nameof(ThemeLabel));
        OnPropertyChanged(nameof(DensityLabel));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(StartWithWindowsText));
        OnPropertyChanged(nameof(AboutTitle));
        OnPropertyChanged(nameof(SoftwareLabel));
        OnPropertyChanged(nameof(IntroductionLabel));
        OnPropertyChanged(nameof(UsageLabel));
        OnPropertyChanged(nameof(VersionLabel));
        OnPropertyChanged(nameof(UpdatesTitle));
        OnPropertyChanged(nameof(CheckForUpdatesText));
        OnPropertyChanged(nameof(DownloadUpdateText));
        OnPropertyChanged(nameof(DeveloperLabel));
        OnPropertyChanged(nameof(ContactLabel));
        OnPropertyChanged(nameof(ApplyText));
        OnPropertyChanged(nameof(CloseText));
        OnPropertyChanged(nameof(AppName));
        OnPropertyChanged(nameof(AppDescription));
        OnPropertyChanged(nameof(UsageGuide));
        OnPropertyChanged(nameof(Developer));
    }

    private void RefreshOptionDisplayNames()
    {
        foreach (var option in ThemeModeOptions)
        {
            option.DisplayName = GetThemeModeDisplayName(option.Value);
        }

        foreach (var option in DensityModeOptions)
        {
            option.DisplayName = GetDensityModeDisplayName(option.Value);
        }

        foreach (var option in LanguageModeOptions)
        {
            option.DisplayName = GetLanguageModeDisplayName(option.Value);
        }
    }

    private static string GetApplicationVersion()
    {
        var assembly = typeof(SettingsViewModel).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var metadataIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
            return metadataIndex > 0
                ? informationalVersion[..metadataIndex]
                : informationalVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "Unknown";
    }
}

public sealed class LocalizedOption<T> : ObservableObject
{
    private string _displayName;

    public LocalizedOption(T value, string displayName)
    {
        Value = value;
        _displayName = displayName;
    }

    public T Value { get; }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }
}
