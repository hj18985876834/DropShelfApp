using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;
using DropShelf.App.Commands;

namespace DropShelf.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
    [TestMethod]
    public void AboutProperties_ExposeApplicationInformation()
    {
        var viewModel = new SettingsViewModel(AppSettings.CreateDefault());

        Assert.AreEqual("DropShelf", viewModel.AppName);
        Assert.AreEqual(
            "这是由江江学长开发的一款运行于 Windows 本地桌面的临时收纳栏工具，可存放文件、文件夹、文本、链接与图片。",
            viewModel.AppDescription);
        Assert.AreEqual(LanguageMode.Chinese, viewModel.LanguageMode);
        Assert.AreEqual("DropShelf 设置", viewModel.WindowTitle);
        Assert.AreEqual("语言", viewModel.LanguageLabel);
        Assert.AreEqual("英文", viewModel.GetLanguageModeDisplayName(LanguageMode.English));
        Assert.IsTrue(viewModel.UsageGuide.Contains("拖放", StringComparison.Ordinal));
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.Version));
        Assert.IsFalse(viewModel.Version.Contains('+', StringComparison.Ordinal));
        Assert.AreEqual("江江学长", viewModel.Developer);
        Assert.AreEqual("2748432469@qq.com", viewModel.Contact);
    }

    [TestMethod]
    public void LanguageMode_UpdatesDisplayedSettingsText()
    {
        var viewModel = new SettingsViewModel(AppSettings.CreateDefault())
        {
            LanguageMode = LanguageMode.English,
        };

        Assert.AreEqual("DropShelf Settings", viewModel.WindowTitle);
        Assert.AreEqual("Language", viewModel.LanguageLabel);
        Assert.AreEqual("Chinese", viewModel.GetLanguageModeDisplayName(LanguageMode.Chinese));
        Assert.IsTrue(viewModel.AppDescription.Contains("local Windows desktop shelf", StringComparison.Ordinal));
        Assert.AreEqual("Jiangjiang Xuezhang", viewModel.Developer);
    }

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
        Assert.AreEqual(LanguageMode.Chinese, saved.LanguageMode);
        Assert.AreEqual(DockEdge.Right, applied?.DockEdge);
        Assert.AreEqual(0.5, applied?.DockOffsetRatio);
        Assert.AreEqual(LanguageMode.Chinese, applied?.LanguageMode);
        Assert.IsTrue(viewModel.HasStatus);
        Assert.IsFalse(viewModel.IsStatusError);
    }

    [TestMethod]
    public async Task ApplyCommand_SavesLanguageMode()
    {
        using var tempDirectory = new TempDirectory();
        var store = new SettingsStore(tempDirectory.Path);
        AppSettings? applied = null;
        var viewModel = new SettingsViewModel(
            AppSettings.CreateDefault(),
            store,
            null,
            settings => applied = settings);

        viewModel.LanguageMode = LanguageMode.English;

        await ExecuteApplyAsync(viewModel);

        var saved = await store.LoadAsync();
        Assert.AreEqual(LanguageMode.English, saved.LanguageMode);
        Assert.AreEqual(LanguageMode.English, applied?.LanguageMode);
        Assert.AreEqual("Settings saved.", viewModel.StatusMessage);
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
