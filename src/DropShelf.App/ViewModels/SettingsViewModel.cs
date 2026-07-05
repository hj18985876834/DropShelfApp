using System.IO;
using System.Reflection;
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
    private LanguageMode _languageMode;
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
        _languageMode = settings.LanguageMode;
        _startWithWindows = settings.StartWithWindows;

        ApplyCommand = new AsyncRelayCommand(_ => SaveAndApplyAsync(SavedMessage));
        ResetDockPositionCommand = new RelayCommand(_ => ResetDockPosition());
    }

    public IReadOnlyList<DockEdge> DockEdgeOptions { get; } = Enum.GetValues<DockEdge>();

    public IReadOnlyList<ThemeMode> ThemeModeOptions { get; } = Enum.GetValues<ThemeMode>();

    public IReadOnlyList<DensityMode> DensityModeOptions { get; } = Enum.GetValues<DensityMode>();

    public IReadOnlyList<LanguageMode> LanguageModeOptions { get; } = Enum.GetValues<LanguageMode>();

    public string WindowTitle => IsChinese ? "DropShelf 设置" : "DropShelf Settings";

    public string HeaderTitle => IsChinese ? "设置" : "Settings";

    public string PreferencesTitle => IsChinese ? "偏好设置" : "Preferences";

    public string ShelfPositionLabel => IsChinese ? "收纳栏位置" : "Shelf position";

    public string ResetDockPositionText => IsChinese ? "重置到右侧边缘" : "Reset to right edge";

    public string ThemeLabel => IsChinese ? "主题" : "Theme";

    public string DensityLabel => IsChinese ? "密度" : "Density";

    public string LanguageLabel => IsChinese ? "语言" : "Language";

    public string StartWithWindowsText => IsChinese ? "开机自启动" : "Start with Windows";

    public string AboutTitle => IsChinese ? "关于" : "About";

    public string SoftwareLabel => IsChinese ? "软件" : "Software";

    public string IntroductionLabel => IsChinese ? "介绍" : "Introduction";

    public string UsageLabel => IsChinese ? "使用方法" : "How to use";

    public string VersionLabel => IsChinese ? "版本" : "Version";

    public string DeveloperLabel => IsChinese ? "开发者" : "Developer";

    public string ContactLabel => IsChinese ? "联系方式" : "Contact";

    public string ApplyText => IsChinese ? "应用" : "Apply";

    public string CloseText => IsChinese ? "关闭" : "Close";

    public string AppName => "DropShelf";

    public string AppDescription =>
        IsChinese
            ? "这是由江江学长开发的一款运行于 Windows 本地桌面的临时收纳栏工具，可存放文件、文件夹、文本、链接与图片。"
            : "A local Windows desktop shelf developed by Jiangjiang Xuezhang for temporarily storing files, folders, text, links, and images.";

    public string UsageGuide =>
        IsChinese
            ? "将内容拖放到屏幕边缘手柄或打开后的收纳栏中，需要时可复制、打开、在资源管理器中定位、移除，或再拖回其他窗口使用。"
            : "Drag content onto the screen-edge handle or open shelf, then copy, open, reveal, remove, or drag items back out when needed.";

    public string Version => GetApplicationVersion();

    public string Developer => IsChinese ? "江江学长" : "Jiangjiang Xuezhang";

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
            LanguageMode.Chinese => IsChinese ? "中文" : "Chinese",
            LanguageMode.English => IsChinese ? "英文" : "English",
            _ => value.ToString(),
        };
    }

    private bool IsChinese => LanguageMode == LanguageMode.Chinese;

    private string SavedMessage => IsChinese ? "设置已保存。" : "Settings saved.";

    private string SaveErrorMessage => IsChinese ? "无法保存设置。" : "Unable to save settings.";

    private void RaiseLocalizedTextChanged()
    {
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
        OnPropertyChanged(nameof(DeveloperLabel));
        OnPropertyChanged(nameof(ContactLabel));
        OnPropertyChanged(nameof(ApplyText));
        OnPropertyChanged(nameof(CloseText));
        OnPropertyChanged(nameof(AppDescription));
        OnPropertyChanged(nameof(UsageGuide));
        OnPropertyChanged(nameof(Developer));
        OnPropertyChanged(nameof(LanguageModeOptions));
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
