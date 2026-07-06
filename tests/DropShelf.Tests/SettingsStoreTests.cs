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
        Assert.AreEqual(0.5, settings.DockOffsetRatio);
        Assert.AreEqual(ThemeMode.System, settings.ThemeMode);
        Assert.AreEqual(DensityMode.Compact, settings.DensityMode);
        Assert.AreEqual(LanguageMode.Chinese, settings.LanguageMode);
        Assert.IsFalse(settings.StartWithWindows);
        Assert.IsFalse(settings.IsShelfPinned);
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
            DockOffsetRatio = 0.25,
            ThemeMode = ThemeMode.Dark,
            DensityMode = DensityMode.Comfortable,
            LanguageMode = LanguageMode.English,
            StartWithWindows = true,
            IsShelfPinned = true,
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.IsTrue(File.Exists(Path.Combine(appDataRoot, "settings.json")));
        Assert.AreEqual(expected.DockEdge, actual.DockEdge);
        Assert.AreEqual(expected.DockOffsetRatio, actual.DockOffsetRatio);
        Assert.AreEqual(expected.ThemeMode, actual.ThemeMode);
        Assert.AreEqual(expected.DensityMode, actual.DensityMode);
        Assert.AreEqual(expected.LanguageMode, actual.LanguageMode);
        Assert.AreEqual(expected.StartWithWindows, actual.StartWithWindows);
        Assert.AreEqual(expected.IsShelfPinned, actual.IsShelfPinned);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaultsWhenJsonIsMalformed()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "settings.json"), "{ malformed");
        var store = new SettingsStore(tempDirectory.Path);

        var settings = await store.LoadAsync();

        Assert.AreEqual(DockEdge.Right, settings.DockEdge);
        Assert.AreEqual(0.5, settings.DockOffsetRatio);
        Assert.AreEqual(ThemeMode.System, settings.ThemeMode);
        Assert.AreEqual(DensityMode.Compact, settings.DensityMode);
        Assert.AreEqual(LanguageMode.Chinese, settings.LanguageMode);
        Assert.IsFalse(settings.StartWithWindows);
        Assert.IsFalse(settings.IsShelfPinned);
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
              "dockOffsetRatio": 0.25,
              "themeMode": "dark",
              "densityMode": "compact",
              "languageMode": "klingon",
              "startWithWindows": true,
              "isShelfPinned": true
            }
            """);
        var store = new SettingsStore(tempDirectory.Path);

        var settings = await store.LoadAsync();

        Assert.AreEqual(DockEdge.Right, settings.DockEdge);
        Assert.AreEqual(0.5, settings.DockOffsetRatio);
        Assert.AreEqual(ThemeMode.System, settings.ThemeMode);
        Assert.AreEqual(DensityMode.Compact, settings.DensityMode);
        Assert.AreEqual(LanguageMode.Chinese, settings.LanguageMode);
        Assert.IsFalse(settings.StartWithWindows);
        Assert.IsFalse(settings.IsShelfPinned);
    }
}
