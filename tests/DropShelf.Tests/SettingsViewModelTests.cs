using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;
using DropShelf.App.Commands;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DropShelf.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
    [TestMethod]
    public void AboutProperties_ExposeApplicationInformation()
    {
        var viewModel = new SettingsViewModel(AppSettings.CreateDefault());

        Assert.AreEqual("EdgeTuck", viewModel.AppName);
        Assert.IsTrue(viewModel.AppDescription.Contains("贴边常驻", StringComparison.Ordinal));
        Assert.IsTrue(viewModel.AppDescription.Contains("不会上传到云端", StringComparison.Ordinal));
        Assert.AreEqual(LanguageMode.Chinese, viewModel.LanguageMode);
        Assert.AreEqual("EdgeTuck 设置", viewModel.WindowTitle);
        Assert.AreEqual("语言", viewModel.LanguageLabel);
        Assert.AreEqual("跟随系统", viewModel.GetThemeModeDisplayName(ThemeMode.System));
        Assert.AreEqual("浅色", viewModel.GetThemeModeDisplayName(ThemeMode.Light));
        Assert.AreEqual("深色", viewModel.GetThemeModeDisplayName(ThemeMode.Dark));
        Assert.AreEqual("紧凑", viewModel.GetDensityModeDisplayName(DensityMode.Compact));
        Assert.AreEqual("舒适", viewModel.GetDensityModeDisplayName(DensityMode.Comfortable));
        Assert.AreEqual("英文", viewModel.GetLanguageModeDisplayName(LanguageMode.English));
        Assert.AreEqual(AutoUpdateCheckMode.Weekly, viewModel.AutoUpdateCheckMode);
        Assert.AreEqual("每周", viewModel.GetAutoUpdateCheckModeDisplayName(AutoUpdateCheckMode.Weekly));
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

        Assert.AreEqual("EdgeTuck Settings", viewModel.WindowTitle);
        Assert.AreEqual("Language", viewModel.LanguageLabel);
        Assert.AreEqual("System", viewModel.GetThemeModeDisplayName(ThemeMode.System));
        Assert.AreEqual("Light", viewModel.GetThemeModeDisplayName(ThemeMode.Light));
        Assert.AreEqual("Dark", viewModel.GetThemeModeDisplayName(ThemeMode.Dark));
        Assert.AreEqual("Compact", viewModel.GetDensityModeDisplayName(DensityMode.Compact));
        Assert.AreEqual("Comfortable", viewModel.GetDensityModeDisplayName(DensityMode.Comfortable));
        Assert.AreEqual("Chinese", viewModel.GetLanguageModeDisplayName(LanguageMode.Chinese));
        Assert.AreEqual("Weekly", viewModel.GetAutoUpdateCheckModeDisplayName(AutoUpdateCheckMode.Weekly));
        Assert.IsTrue(viewModel.AppDescription.Contains("local Windows edge shelf", StringComparison.Ordinal));
        Assert.AreEqual("Jiangjiang Xuezhang", viewModel.Developer);
    }

    [TestMethod]
    public void LanguageMode_UpdatesSettingsOptionDisplayNames()
    {
        var viewModel = new SettingsViewModel(AppSettings.CreateDefault());
        var themeOption = viewModel.ThemeModeOptions.Single(option => option.Value == ThemeMode.System);
        var densityOption = viewModel.DensityModeOptions.Single(option => option.Value == DensityMode.Comfortable);
        var languageOption = viewModel.LanguageModeOptions.Single(option => option.Value == LanguageMode.English);
        var updateOption = viewModel.AutoUpdateCheckModeOptions.Single(option => option.Value == AutoUpdateCheckMode.Weekly);

        Assert.AreEqual("跟随系统", themeOption.DisplayName);
        Assert.AreEqual("舒适", densityOption.DisplayName);
        Assert.AreEqual("英文", languageOption.DisplayName);
        Assert.AreEqual("每周", updateOption.DisplayName);

        viewModel.LanguageMode = LanguageMode.English;

        Assert.AreSame(themeOption, viewModel.ThemeModeOptions.Single(option => option.Value == ThemeMode.System));
        Assert.AreSame(densityOption, viewModel.DensityModeOptions.Single(option => option.Value == DensityMode.Comfortable));
        Assert.AreSame(languageOption, viewModel.LanguageModeOptions.Single(option => option.Value == LanguageMode.English));
        Assert.AreSame(updateOption, viewModel.AutoUpdateCheckModeOptions.Single(option => option.Value == AutoUpdateCheckMode.Weekly));
        Assert.AreEqual("System", themeOption.DisplayName);
        Assert.AreEqual("Comfortable", densityOption.DisplayName);
        Assert.AreEqual("English", languageOption.DisplayName);
        Assert.AreEqual("Weekly", updateOption.DisplayName);
    }

    [TestMethod]
    public async Task CheckForUpdates_SyncsBrandingFromLatestManifestWhenAlreadyCurrent()
    {
        using var tempDirectory = new TempDirectory();
        var manifestJson = """
            {
              "version": "0.1.0",
              "installerUrl": "https://example.com/EdgeTuckSetup.exe",
              "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
              "sizeBytes": 1,
              "branding": {
                "displayName": "EdgeTuck Daily",
                "descriptions": {
                  "zh-CN": "用于同步的中文介绍。",
                  "en-US": "English description from manifest."
                }
              },
              "releaseNotes": {
                "zh-CN": "当前版本。",
                "en-US": "Current version."
              }
            }
            """;
        var updateService = new UpdateService(
            new HttpClient(new StubHttpMessageHandler(manifestJson)),
            new Uri("https://example.com/latest.json"),
            Path.Combine(tempDirectory.Path, "updates"));
        var viewModel = new SettingsViewModel(
            AppSettings.CreateDefault(),
            null,
            null,
            updateService,
            null,
            null);

        await ((AsyncRelayCommand)viewModel.CheckForUpdatesCommand).ExecuteAsync();

        Assert.AreEqual("EdgeTuck Daily", viewModel.AppName);
        Assert.AreEqual("用于同步的中文介绍。", viewModel.AppDescription);
        Assert.IsFalse(viewModel.IsUpdateAvailable);
        Assert.AreEqual("当前已是最新版本。", viewModel.StatusMessage);

        viewModel.LanguageMode = LanguageMode.English;

        Assert.AreEqual("English description from manifest.", viewModel.AppDescription);
    }

    [TestMethod]
    public void ApplyAvailableUpdate_ShowsUpdateDetailsAndReleaseNotes()
    {
        var viewModel = new SettingsViewModel(AppSettings.CreateDefault());
        var manifest = new UpdateManifest
        {
            Version = "0.2.0",
            InstallerUrl = "https://example.com/EdgeTuckSetup.exe",
            Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            SizeBytes = 1024,
            ReleaseDate = "2026-07-18",
            ReleaseNotes = new UpdateReleaseNotes
            {
                Chinese = "更新说明。",
                English = "Release notes.",
            },
        };

        viewModel.ApplyAvailableUpdate(manifest, updateStatus: true);

        Assert.IsTrue(viewModel.IsUpdateAvailable);
        Assert.IsTrue(viewModel.HasUpdateDetails);
        StringAssert.Contains(viewModel.UpdateDetails, "0.2.0");
        StringAssert.Contains(viewModel.UpdateDetails, "2026-07-18");
        Assert.AreEqual("更新说明。", viewModel.UpdateReleaseNotes);
        StringAssert.Contains(viewModel.StatusMessage, "发现新版本 0.2.0");

        viewModel.LanguageMode = LanguageMode.English;

        Assert.AreEqual("Release notes.", viewModel.UpdateReleaseNotes);
        StringAssert.Contains(viewModel.UpdateDetails, "Version: 0.2.0");
    }

    [TestMethod]
    public async Task DownloadUpdateCommand_CancelledConfirmationDoesNotDownload()
    {
        var manifest = new UpdateManifest
        {
            Version = "0.2.0",
            InstallerUrl = "https://example.com/EdgeTuckSetup.exe",
            Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            SizeBytes = 1,
        };
        var updateService = new UpdateService(
            new HttpClient(new ThrowingHttpMessageHandler()),
            new Uri("https://example.com/latest.json"),
            Path.GetTempPath());
        var viewModel = new SettingsViewModel(
            AppSettings.CreateDefault(),
            null,
            null,
            updateService,
            null,
            null,
            (_, _) => false,
            manifest);

        await ((AsyncRelayCommand)viewModel.DownloadUpdateCommand).ExecuteAsync();

        Assert.AreEqual("已取消安装更新。", viewModel.StatusMessage);
        Assert.IsFalse(viewModel.IsStatusError);
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
        viewModel.AutoUpdateCheckMode = AutoUpdateCheckMode.Never;
        viewModel.ResetDockPositionCommand.Execute(null);
        Assert.IsNull(applied);

        await ExecuteApplyAsync(viewModel);

        var saved = await store.LoadAsync();
        Assert.AreEqual(DockEdge.Right, saved.DockEdge);
        Assert.AreEqual(0.5, saved.DockOffsetRatio);
        Assert.AreEqual(LanguageMode.Chinese, saved.LanguageMode);
        Assert.AreEqual(AutoUpdateCheckMode.Never, saved.AutoUpdateCheckMode);
        Assert.AreEqual(DockEdge.Right, applied?.DockEdge);
        Assert.AreEqual(0.5, applied?.DockOffsetRatio);
        Assert.AreEqual(LanguageMode.Chinese, applied?.LanguageMode);
        Assert.AreEqual(AutoUpdateCheckMode.Never, applied?.AutoUpdateCheckMode);
        Assert.IsFalse(saved.IsShelfPinned);
        Assert.IsFalse(applied?.IsShelfPinned);
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
            new AppSettings { IsShelfPinned = true },
            store,
            null,
            settings => applied = settings);

        viewModel.LanguageMode = LanguageMode.English;

        await ExecuteApplyAsync(viewModel);

        var saved = await store.LoadAsync();
        Assert.AreEqual(LanguageMode.English, saved.LanguageMode);
        Assert.AreEqual(LanguageMode.English, applied?.LanguageMode);
        Assert.IsTrue(saved.IsShelfPinned);
        Assert.IsTrue(applied?.IsShelfPinned);
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

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _content;

        public StubHttpMessageHandler(string content)
        {
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Download should not be requested.");
        }
    }
}
