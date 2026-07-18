namespace DropShelf.App.Models;

public sealed class AppSettings
{
    public DockEdge DockEdge { get; init; } = DockEdge.Right;

    public double DockOffsetRatio { get; init; } = 0.5;

    public ThemeMode ThemeMode { get; init; } = ThemeMode.System;

    public DensityMode DensityMode { get; init; } = DensityMode.Compact;

    public LanguageMode LanguageMode { get; init; } = LanguageMode.Chinese;

    public bool StartWithWindows { get; init; }

    public bool IsShelfPinned { get; init; }

    public AutoUpdateCheckMode AutoUpdateCheckMode { get; init; } = AutoUpdateCheckMode.Weekly;

    public DateTimeOffset? LastAutomaticUpdateCheckUtc { get; init; }

    public string? PendingUpdateVersion { get; init; }

    public string? LastUpdateCompletedVersion { get; init; }

    public static AppSettings CreateDefault()
    {
        return new AppSettings();
    }
}
