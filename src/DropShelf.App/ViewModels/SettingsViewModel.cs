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
    private readonly Func<string, string, bool>? _confirmUpdateInstall;
    private AppBranding _appBranding = AppBranding.Default;
    private AppSettings _lastAppliedSettings;
    private UpdateManifest? _availableUpdate;
    private AutoUpdateCheckMode _autoUpdateCheckMode;
    private DensityMode _densityMode;
    private DockEdge _dockEdge;
    private double _dockOffsetRatio;
    private bool _hasUpdateDetails;
    private bool _hasStatus;
    private bool _isApplying;
    private bool _isCheckingForUpdate;
    private bool _isStatusError;
    private bool _isUpdateAvailable;
    private bool _isUpdating;
    private bool _isShelfPinned;
    private LanguageMode _languageMode;
    private bool _startWithWindows;
    private string _statusMessage = string.Empty;
    private ThemeMode _themeMode;
    private string _updateDetails = string.Empty;
    private string _updateReleaseNotes = string.Empty;
    private readonly IReadOnlyList<LocalizedOption<ThemeMode>> _themeModeOptions;
    private readonly IReadOnlyList<LocalizedOption<DensityMode>> _densityModeOptions;
    private readonly IReadOnlyList<LocalizedOption<LanguageMode>> _languageModeOptions;
    private readonly IReadOnlyList<LocalizedOption<AutoUpdateCheckMode>> _autoUpdateCheckModeOptions;

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
        Action<AppSettings>? applySettings,
        Func<string, string, bool>? confirmUpdateInstall = null,
        UpdateManifest? initialAvailableUpdate = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _localizationService = new LocalizationService(settings.LanguageMode);
        _settingsStore = settingsStore;
        _startupService = startupService;
        _updateService = updateService;
        _shutdownApplication = shutdownApplication;
        _confirmUpdateInstall = confirmUpdateInstall;
        _applySettings = applySettings;
        _lastAppliedSettings = settings;
        _dockEdge = settings.DockEdge;
        _dockOffsetRatio = settings.DockOffsetRatio;
        _themeMode = settings.ThemeMode;
        _densityMode = settings.DensityMode;
        _languageMode = settings.LanguageMode;
        _startWithWindows = settings.StartWithWindows;
        _isShelfPinned = settings.IsShelfPinned;
        _autoUpdateCheckMode = settings.AutoUpdateCheckMode;
        _themeModeOptions = Enum.GetValues<ThemeMode>()
            .Select(value => new LocalizedOption<ThemeMode>(value, GetThemeModeDisplayName(value)))
            .ToArray();
        _densityModeOptions = Enum.GetValues<DensityMode>()
            .Select(value => new LocalizedOption<DensityMode>(value, GetDensityModeDisplayName(value)))
            .ToArray();
        _languageModeOptions = Enum.GetValues<LanguageMode>()
            .Select(value => new LocalizedOption<LanguageMode>(value, GetLanguageModeDisplayName(value)))
            .ToArray();
        _autoUpdateCheckModeOptions = Enum.GetValues<AutoUpdateCheckMode>()
            .Select(value => new LocalizedOption<AutoUpdateCheckMode>(value, GetAutoUpdateCheckModeDisplayName(value)))
            .ToArray();

        ApplyCommand = new AsyncRelayCommand(_ => SaveAndApplyAsync(SavedMessage));
        CheckForUpdatesCommand = new AsyncRelayCommand(_ => CheckForUpdatesAsync(), _ => _updateService is not null);
        DownloadUpdateCommand = new AsyncRelayCommand(_ => DownloadAndInstallUpdateAsync(), _ => _updateService is not null && IsUpdateAvailable && _availableUpdate is not null);
        ResetDockPositionCommand = new RelayCommand(_ => ResetDockPosition());

        if (initialAvailableUpdate is not null)
        {
            ApplyAvailableUpdate(initialAvailableUpdate, updateStatus: false);
        }
    }

    public IReadOnlyList<DockEdge> DockEdgeOptions { get; } = Enum.GetValues<DockEdge>();

    public IReadOnlyList<LocalizedOption<ThemeMode>> ThemeModeOptions => _themeModeOptions;

    public IReadOnlyList<LocalizedOption<DensityMode>> DensityModeOptions => _densityModeOptions;

    public IReadOnlyList<LocalizedOption<LanguageMode>> LanguageModeOptions => _languageModeOptions;

    public IReadOnlyList<LocalizedOption<AutoUpdateCheckMode>> AutoUpdateCheckModeOptions => _autoUpdateCheckModeOptions;

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

    public string AutoUpdateCheckLabel => Text.AutoUpdateCheckLabel;

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
            if (_availableUpdate is not null)
            {
                SetUpdateDetails(_availableUpdate);
            }

            RaiseLocalizedTextChanged();
        }
    }

    public AutoUpdateCheckMode AutoUpdateCheckMode
    {
        get => _autoUpdateCheckMode;
        set => SetPendingProperty(ref _autoUpdateCheckMode, value);
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

    public string UpdateDetails
    {
        get => _updateDetails;
        private set => SetProperty(ref _updateDetails, value);
    }

    public string UpdateReleaseNotes
    {
        get => _updateReleaseNotes;
        private set => SetProperty(ref _updateReleaseNotes, value);
    }

    public bool HasUpdateDetails
    {
        get => _hasUpdateDetails;
        private set => SetProperty(ref _hasUpdateDetails, value);
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
            _isShelfPinned = value.IsShelfPinned;
            _autoUpdateCheckMode = value.AutoUpdateCheckMode;
            OnPropertyChanged(nameof(DockEdge));
            OnPropertyChanged(nameof(DockOffsetRatio));
            OnPropertyChanged(nameof(ThemeMode));
            OnPropertyChanged(nameof(DensityMode));
            OnPropertyChanged(nameof(LanguageMode));
            OnPropertyChanged(nameof(StartWithWindows));
            OnPropertyChanged(nameof(AutoUpdateCheckMode));
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
                ClearUpdateDetails();
                SetStatus(Text.NoUpdateAvailable, isError: false);
                return;
            }

            ApplyAvailableUpdate(result.Manifest, updateStatus: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or JsonException or FormatException or IOException or TaskCanceledException)
        {
            IsUpdateAvailable = false;
            _availableUpdate = null;
            ClearUpdateDetails();
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
            if (!ConfirmUpdateInstall(_availableUpdate))
            {
                SetStatus(Text.UpdateInstallCancelled, isError: false);
                return;
            }

            SetStatus(Text.DownloadingUpdate, isError: false);
            var progress = new Progress<double>(value =>
            {
                var percent = Math.Clamp((int)Math.Round(value * 100), 0, 100);
                SetStatus(string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.DownloadProgressFormat, percent), isError: false);
            });

            var download = await _updateService.DownloadInstallerAsync(_availableUpdate, progress);
            await SavePendingUpdateVersionAsync(download.Version);
            SetStatus(string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.UpdateDownloadedVerifiedFormat, download.Version), isError: false);
            _updateService.LaunchInstaller(download.InstallerPath);
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
            IsShelfPinned = _isShelfPinned,
            AutoUpdateCheckMode = AutoUpdateCheckMode,
            LastAutomaticUpdateCheckUtc = _lastAppliedSettings.LastAutomaticUpdateCheckUtc,
            PendingUpdateVersion = _lastAppliedSettings.PendingUpdateVersion,
            LastUpdateCompletedVersion = _lastAppliedSettings.LastUpdateCompletedVersion,
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

    public void ApplyAvailableUpdate(UpdateManifest manifest, bool updateStatus)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        _availableUpdate = manifest;
        IsUpdateAvailable = true;
        ApplyBranding(manifest.Branding);
        SetUpdateDetails(manifest);

        if (!updateStatus)
        {
            return;
        }

        var message = string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.UpdateAvailableFormat, manifest.Version);
        SetStatus(message, isError: false);
    }

    private void SetUpdateDetails(UpdateManifest manifest)
    {
        var details = new List<string>
        {
            string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.UpdateVersionFormat, manifest.Version),
            string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.UpdateSizeFormat, FormatSize(manifest.SizeBytes)),
            string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.UpdateSha256Format, ShortSha256(manifest.Sha256)),
            manifest.Mandatory ? Text.MandatoryUpdateText : Text.OptionalUpdateText,
        };

        if (!string.IsNullOrWhiteSpace(manifest.ReleaseDate))
        {
            details.Insert(1, string.Format(System.Globalization.CultureInfo.CurrentCulture, Text.UpdateReleaseDateFormat, manifest.ReleaseDate));
        }

        UpdateDetails = string.Join(Environment.NewLine, details);
        UpdateReleaseNotes = manifest.ReleaseNotesFor(_localizationService.IsChinese);
        HasUpdateDetails = true;
    }

    private void ClearUpdateDetails()
    {
        UpdateDetails = string.Empty;
        UpdateReleaseNotes = string.Empty;
        HasUpdateDetails = false;
    }

    private bool ConfirmUpdateInstall(UpdateManifest manifest)
    {
        if (_confirmUpdateInstall is null)
        {
            return true;
        }

        var notes = manifest.ReleaseNotesFor(_localizationService.IsChinese);
        var message = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            Text.UpdateInstallConfirmMessageFormat,
            manifest.Version,
            string.IsNullOrWhiteSpace(notes) ? Text.NoReleaseNotesText : notes);
        return _confirmUpdateInstall(Text.UpdateInstallConfirmTitle, message);
    }

    private async Task SavePendingUpdateVersionAsync(string version)
    {
        var settings = new AppSettings
        {
            DockEdge = _lastAppliedSettings.DockEdge,
            DockOffsetRatio = _lastAppliedSettings.DockOffsetRatio,
            ThemeMode = _lastAppliedSettings.ThemeMode,
            DensityMode = _lastAppliedSettings.DensityMode,
            LanguageMode = _lastAppliedSettings.LanguageMode,
            StartWithWindows = _lastAppliedSettings.StartWithWindows,
            IsShelfPinned = _lastAppliedSettings.IsShelfPinned,
            AutoUpdateCheckMode = _lastAppliedSettings.AutoUpdateCheckMode,
            LastAutomaticUpdateCheckUtc = _lastAppliedSettings.LastAutomaticUpdateCheckUtc,
            PendingUpdateVersion = version,
            LastUpdateCompletedVersion = _lastAppliedSettings.LastUpdateCompletedVersion,
        };

        if (_settingsStore is not null)
        {
            await _settingsStore.SaveAsync(settings);
        }

        _applySettings?.Invoke(settings);
        _lastAppliedSettings = settings;
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
        _isShelfPinned = settings.IsShelfPinned;
        _autoUpdateCheckMode = settings.AutoUpdateCheckMode;
        OnPropertyChanged(nameof(DockEdge));
        OnPropertyChanged(nameof(DockOffsetRatio));
        OnPropertyChanged(nameof(ThemeMode));
        OnPropertyChanged(nameof(DensityMode));
        OnPropertyChanged(nameof(LanguageMode));
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(AutoUpdateCheckMode));
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

    public string GetAutoUpdateCheckModeDisplayName(AutoUpdateCheckMode value)
    {
        return _localizationService.AutoUpdateCheckModeName(value);
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
        OnPropertyChanged(nameof(AutoUpdateCheckLabel));
        OnPropertyChanged(nameof(AboutTitle));
        OnPropertyChanged(nameof(SoftwareLabel));
        OnPropertyChanged(nameof(IntroductionLabel));
        OnPropertyChanged(nameof(UsageLabel));
        OnPropertyChanged(nameof(VersionLabel));
        OnPropertyChanged(nameof(UpdatesTitle));
        OnPropertyChanged(nameof(CheckForUpdatesText));
        OnPropertyChanged(nameof(DownloadUpdateText));
        OnPropertyChanged(nameof(UpdateDetails));
        OnPropertyChanged(nameof(UpdateReleaseNotes));
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

        foreach (var option in AutoUpdateCheckModeOptions)
        {
            option.DisplayName = GetAutoUpdateCheckModeDisplayName(option.Value);
        }
    }

    public static string GetApplicationVersion()
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

    private static string FormatSize(long? sizeBytes)
    {
        if (sizeBytes is null)
        {
            return "Unknown";
        }

        var size = sizeBytes.Value;
        if (size >= 1024 * 1024)
        {
            return $"{size / 1024d / 1024d:0.0} MB";
        }

        if (size >= 1024)
        {
            return $"{size / 1024d:0.0} KB";
        }

        return $"{size} B";
    }

    private static string ShortSha256(string sha256)
    {
        return sha256.Length <= 16
            ? sha256
            : $"{sha256[..8]}...{sha256[^8..]}";
    }
}
