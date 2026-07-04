using System.IO;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.Tests;

[TestClass]
public sealed class SettingsStoreTests
{
    [TestMethod]
    public async Task LoadAsync_ReturnsDefaultsWhenFileIsMissing()
    {
        using var tempDirectory = new TempDirectory();
        var store = new SettingsStore(tempDirectory.Path);

        var settings = await store.LoadAsync();

        Assert.AreEqual(DockEdge.Right, settings.DockEdge);
        Assert.AreEqual(ThemeMode.System, settings.ThemeMode);
        Assert.AreEqual(DensityMode.Compact, settings.DensityMode);
        Assert.IsFalse(settings.StartWithWindows);
    }

    [TestMethod]
    public async Task SaveAsync_CreatesDirectoryAndRoundTripsSettings()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "nested", "DropShelf");
        var store = new SettingsStore(appDataRoot);
        var expected = new AppSettings
        {
            DockEdge = DockEdge.Left,
            ThemeMode = ThemeMode.Dark,
            DensityMode = DensityMode.Comfortable,
            StartWithWindows = true,
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.IsTrue(File.Exists(Path.Combine(appDataRoot, "settings.json")));
        Assert.AreEqual(expected.DockEdge, actual.DockEdge);
        Assert.AreEqual(expected.ThemeMode, actual.ThemeMode);
        Assert.AreEqual(expected.DensityMode, actual.DensityMode);
        Assert.AreEqual(expected.StartWithWindows, actual.StartWithWindows);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaultsWhenJsonIsMalformed()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "settings.json"), "{ malformed");
        var store = new SettingsStore(tempDirectory.Path);

        var settings = await store.LoadAsync();

        Assert.AreEqual(DockEdge.Right, settings.DockEdge);
        Assert.AreEqual(ThemeMode.System, settings.ThemeMode);
        Assert.AreEqual(DensityMode.Compact, settings.DensityMode);
        Assert.IsFalse(settings.StartWithWindows);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaultsWhenEnumValueIsUnknown()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(
            Path.Combine(tempDirectory.Path, "settings.json"),
            """
            {
              "dockEdge": "diagonal",
              "themeMode": "dark",
              "densityMode": "compact",
              "startWithWindows": true
            }
            """);
        var store = new SettingsStore(tempDirectory.Path);

        var settings = await store.LoadAsync();

        Assert.AreEqual(DockEdge.Right, settings.DockEdge);
        Assert.AreEqual(ThemeMode.System, settings.ThemeMode);
        Assert.AreEqual(DensityMode.Compact, settings.DensityMode);
        Assert.IsFalse(settings.StartWithWindows);
    }
}
