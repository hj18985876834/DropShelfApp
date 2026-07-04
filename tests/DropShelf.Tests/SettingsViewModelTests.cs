using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;

namespace DropShelf.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
    [TestMethod]
    public async Task ChangingDockEdge_SavesAndAppliesSettings()
    {
        using var tempDirectory = new TempDirectory();
        var store = new SettingsStore(tempDirectory.Path);
        AppSettings? applied = null;
        var viewModel = new SettingsViewModel(
            AppSettings.CreateDefault(),
            store,
            new StartupService(new FakeStartupRegistry(), "DropShelf", @"C:\Apps\DropShelf.exe"),
            settings => applied = settings);

        viewModel.DockEdge = DockEdge.Left;

        var saved = await store.LoadAsync();
        Assert.AreEqual(DockEdge.Left, saved.DockEdge);
        Assert.AreEqual(DockEdge.Left, applied?.DockEdge);
        Assert.IsTrue(viewModel.HasStatus);
        Assert.IsFalse(viewModel.IsStatusError);
    }

    [TestMethod]
    public async Task ChangingStartup_UpdatesRegistryAndSavedSettings()
    {
        using var tempDirectory = new TempDirectory();
        var registry = new FakeStartupRegistry();
        var store = new SettingsStore(tempDirectory.Path);
        var viewModel = new SettingsViewModel(
            AppSettings.CreateDefault(),
            store,
            new StartupService(registry, "DropShelf", @"C:\Apps\DropShelf.exe"),
            null);

        viewModel.StartWithWindows = true;

        var saved = await store.LoadAsync();
        Assert.AreEqual("\"C:\\Apps\\DropShelf.exe\"", registry.Values["DropShelf"]);
        Assert.IsTrue(saved.StartWithWindows);
        Assert.IsFalse(viewModel.IsStatusError);
    }

    [TestMethod]
    public void StartupRegistryFailure_RollsBackToggleAndShowsError()
    {
        var viewModel = new SettingsViewModel(
            AppSettings.CreateDefault(),
            null,
            new StartupService(new ThrowingStartupRegistry(), "DropShelf", @"C:\Apps\DropShelf.exe"),
            null);

        viewModel.StartWithWindows = true;

        Assert.IsFalse(viewModel.StartWithWindows);
        Assert.IsTrue(viewModel.HasStatus);
        Assert.IsTrue(viewModel.IsStatusError);
    }

    private sealed class FakeStartupRegistry : IStartupRegistry
    {
        public Dictionary<string, string> Values { get; } = [];

        public string? GetValue(string name)
        {
            return Values.TryGetValue(name, out var value) ? value : null;
        }

        public void SetValue(string name, string value)
        {
            Values[name] = value;
        }

        public void DeleteValue(string name)
        {
            Values.Remove(name);
        }
    }

    private sealed class ThrowingStartupRegistry : IStartupRegistry
    {
        public string? GetValue(string name)
        {
            return null;
        }

        public void SetValue(string name, string value)
        {
            throw new UnauthorizedAccessException();
        }

        public void DeleteValue(string name)
        {
            throw new UnauthorizedAccessException();
        }
    }
}
