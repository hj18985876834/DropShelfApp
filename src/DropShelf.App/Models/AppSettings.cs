namespace DropShelf.App.Models;

public sealed class AppSettings
{
    public DockEdge DockEdge { get; init; } = DockEdge.Right;

    public ThemeMode ThemeMode { get; init; } = ThemeMode.System;

    public DensityMode DensityMode { get; init; } = DensityMode.Compact;

    public bool StartWithWindows { get; init; }

    public static AppSettings CreateDefault()
    {
        return new AppSettings();
    }
}
