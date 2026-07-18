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
        Assert.AreEqual(AutoUpdateCheckMode.Weekly, settings.AutoUpdateCheckMode);
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
            AutoUpdateCheckMode = AutoUpdateCheckMode.Daily,
            LastAutomaticUpdateCheckUtc = DateTimeOffset.Parse("2026-07-17T12:00:00+00:00"),
            PendingUpdateVersion = "0.2.0",
            LastUpdateCompletedVersion = "0.1.2",
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
        Assert.AreEqual(expected.AutoUpdateCheckMode, actual.AutoUpdateCheckMode);
        Assert.AreEqual(expected.LastAutomaticUpdateCheckUtc, actual.LastAutomaticUpdateCheckUtc);
        Assert.AreEqual(expected.PendingUpdateVersion, actual.PendingUpdateVersion);
        Assert.AreEqual(expected.LastUpdateCompletedVersion, actual.LastUpdateCompletedVersion);
    }

    [TestMethod]
    public async Task LoadAsync_UsesDefaultsForSettingsAddedAfterOlderJson()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(
            Path.Combine(tempDirectory.Path, "settings.json"),
            """
            {
              "dockEdge": "left",
              "dockOffsetRatio": 0.25,
              "themeMode": "dark",
              "densityMode": "comfortable",
              "languageMode": "english",
              "startWithWindows": true,
              "isShelfPinned": true
            }
            """);
        var store = new SettingsStore(tempDirectory.Path);

        var settings = await store.LoadAsync();

        Assert.AreEqual(DockEdge.Left, settings.DockEdge);
        Assert.AreEqual(AutoUpdateCheckMode.Weekly, settings.AutoUpdateCheckMode);
        Assert.IsNull(settings.LastAutomaticUpdateCheckUtc);
        Assert.IsNull(settings.PendingUpdateVersion);
        Assert.IsNull(settings.LastUpdateCompletedVersion);
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
        Assert.AreEqual(AutoUpdateCheckMode.Weekly, settings.AutoUpdateCheckMode);
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
        Assert.AreEqual(AutoUpdateCheckMode.Weekly, settings.AutoUpdateCheckMode);
    }
}
