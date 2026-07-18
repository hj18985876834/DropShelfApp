using System.IO;
using System.Net.Http;
using System.Text.Json;
using DropShelf.App.Models;

namespace DropShelf.App.Services;

public sealed class AutomaticUpdateCheckService
{
    private static readonly TimeSpan DailyInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan WeeklyInterval = TimeSpan.FromDays(7);

    private readonly UpdateService _updateService;
    private readonly Func<DateTimeOffset> _utcNowProvider;

    public AutomaticUpdateCheckService(UpdateService updateService)
        : this(updateService, () => DateTimeOffset.UtcNow)
    {
    }

    public AutomaticUpdateCheckService(UpdateService updateService, Func<DateTimeOffset> utcNowProvider)
    {
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _utcNowProvider = utcNowProvider ?? throw new ArgumentNullException(nameof(utcNowProvider));
    }

    public bool ShouldCheck(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.AutoUpdateCheckMode == AutoUpdateCheckMode.Never)
        {
            return false;
        }

        if (settings.LastAutomaticUpdateCheckUtc is null)
        {
            return true;
        }

        var interval = settings.AutoUpdateCheckMode switch
        {
            AutoUpdateCheckMode.Daily => DailyInterval,
            AutoUpdateCheckMode.Weekly => WeeklyInterval,
            _ => Timeout.InfiniteTimeSpan,
        };

        if (interval == Timeout.InfiniteTimeSpan)
        {
            return false;
        }

        return _utcNowProvider() - settings.LastAutomaticUpdateCheckUtc.Value >= interval;
    }

    public async Task<AutomaticUpdateCheckResult> CheckAsync(
        AppSettings settings,
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!ShouldCheck(settings))
        {
            return AutomaticUpdateCheckResult.Skipped(settings);
        }

        var checkedSettings = WithLastAutomaticUpdateCheckUtc(settings, _utcNowProvider());
        try
        {
            var result = await _updateService.CheckForUpdatesAsync(currentVersion, cancellationToken).ConfigureAwait(false);
            return AutomaticUpdateCheckResult.Checked(checkedSettings, result);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or JsonException or FormatException or IOException or TaskCanceledException)
        {
            return AutomaticUpdateCheckResult.Failure(checkedSettings);
        }
    }

    private static AppSettings WithLastAutomaticUpdateCheckUtc(AppSettings settings, DateTimeOffset lastCheckUtc)
    {
        return new AppSettings
        {
            DockEdge = settings.DockEdge,
            DockOffsetRatio = settings.DockOffsetRatio,
            ThemeMode = settings.ThemeMode,
            DensityMode = settings.DensityMode,
            LanguageMode = settings.LanguageMode,
            StartWithWindows = settings.StartWithWindows,
            IsShelfPinned = settings.IsShelfPinned,
            AutoUpdateCheckMode = settings.AutoUpdateCheckMode,
            LastAutomaticUpdateCheckUtc = lastCheckUtc.ToUniversalTime(),
            PendingUpdateVersion = settings.PendingUpdateVersion,
            LastUpdateCompletedVersion = settings.LastUpdateCompletedVersion,
        };
    }
}

public sealed record AutomaticUpdateCheckResult(
    bool DidCheck,
    bool IsUpdateAvailable,
    bool Failed,
    AppSettings Settings,
    UpdateManifest? Manifest)
{
    public static AutomaticUpdateCheckResult Skipped(AppSettings settings)
    {
        return new AutomaticUpdateCheckResult(false, false, false, settings, null);
    }

    public static AutomaticUpdateCheckResult Checked(AppSettings settings, UpdateCheckResult result)
    {
        return new AutomaticUpdateCheckResult(true, result.IsUpdateAvailable, false, settings, result.Manifest);
    }

    public static AutomaticUpdateCheckResult Failure(AppSettings settings)
    {
        return new AutomaticUpdateCheckResult(true, false, true, settings, null);
    }
}
