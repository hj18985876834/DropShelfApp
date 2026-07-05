using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;
using DropShelf.App.Commands;

namespace DropShelf.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
    [TestMethod]
    public async Task ApplyCommand_SavesAndAppliesSettings()
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
        viewModel.ResetDockPositionCommand.Execute(null);
        Assert.IsNull(applied);

        await ExecuteApplyAsync(viewModel);

        var saved = await store.LoadAsync();
        Assert.AreEqual(DockEdge.Right, saved.DockEdge);
        Assert.AreEqual(0.5, saved.DockOffsetRatio);
        Assert.AreEqual(DockEdge.Right, applied?.DockEdge);
        Assert.AreEqual(0.5, applied?.DockOffsetRatio);
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
        Assert.IsFalse(registry.Values.ContainsKey("DropShelf"));

        await ExecuteApplyAsync(viewModel);

        var saved = await store.LoadAsync();
        Assert.AreEqual("\"C:\\Apps\\DropShelf.exe\"", registry.Values["DropShelf"]);
        Assert.IsTrue(saved.StartWithWindows);
        Assert.IsFalse(viewModel.IsStatusError);
    }

    [TestMethod]
    public async Task StartupRegistryFailure_RollsBackToggleAndShowsError()
    {
        var viewModel = new SettingsViewModel(
            AppSettings.CreateDefault(),
            null,
            new StartupService(new ThrowingStartupRegistry(), "DropShelf", @"C:\Apps\DropShelf.exe"),
            null);

        viewModel.StartWithWindows = true;
        Assert.IsTrue(viewModel.StartWithWindows);
        Assert.IsFalse(viewModel.HasStatus);

        await ExecuteApplyAsync(viewModel);

        Assert.IsFalse(viewModel.StartWithWindows);
        Assert.IsTrue(viewModel.HasStatus);
        Assert.IsTrue(viewModel.IsStatusError);
    }

    [TestMethod]
    public async Task ApplyCommand_DisablesWhileSaveIsPending()
    {
        using var tempDirectory = new TempDirectory();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new DelayedSettingsStore(tempDirectory.Path, gate.Task);
        var viewModel = new SettingsViewModel(AppSettings.CreateDefault(), store, null, null);
        var command = (AsyncRelayCommand)viewModel.ApplyCommand;

        var applyTask = command.ExecuteAsync();

        Assert.IsTrue(viewModel.IsApplying);
        Assert.IsFalse(command.CanExecute(null));

        gate.SetResult();
        await applyTask;

        Assert.IsFalse(viewModel.IsApplying);
        Assert.IsTrue(command.CanExecute(null));
        Assert.IsFalse(viewModel.IsStatusError);
    }

    private static Task ExecuteApplyAsync(SettingsViewModel viewModel)
    {
        return ((AsyncRelayCommand)viewModel.ApplyCommand).ExecuteAsync();
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

    private sealed class DelayedSettingsStore : SettingsStore
    {
        private readonly Task _delay;

        public DelayedSettingsStore(string appDataRoot, Task delay)
            : base(appDataRoot)
        {
            _delay = delay;
        }

        public override async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            await _delay;
            await base.SaveAsync(settings, cancellationToken);
        }
    }
}
