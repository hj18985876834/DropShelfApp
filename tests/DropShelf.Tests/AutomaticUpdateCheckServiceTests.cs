using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.Tests;

[TestClass]
public sealed class AutomaticUpdateCheckServiceTests
{
    [TestMethod]
    public async Task CheckAsync_SkipsWhenModeIsNever()
    {
        using var tempDirectory = new TempDirectory();
        var now = DateTimeOffset.Parse("2026-07-18T00:00:00+00:00");
        var service = CreateService(tempDirectory.Path, ManifestJson("0.2.0"), now);

        var result = await service.CheckAsync(new AppSettings { AutoUpdateCheckMode = AutoUpdateCheckMode.Never }, "0.1.0");

        Assert.IsFalse(result.DidCheck);
        Assert.IsFalse(result.IsUpdateAvailable);
        Assert.IsNull(result.Settings.LastAutomaticUpdateCheckUtc);
    }

    [TestMethod]
    public async Task CheckAsync_SkipsWeeklyWhenLastCheckIsTooRecent()
    {
        using var tempDirectory = new TempDirectory();
        var now = DateTimeOffset.Parse("2026-07-18T00:00:00+00:00");
        var service = CreateService(tempDirectory.Path, ManifestJson("0.2.0"), now);

        var result = await service.CheckAsync(
            new AppSettings
            {
                AutoUpdateCheckMode = AutoUpdateCheckMode.Weekly,
                LastAutomaticUpdateCheckUtc = now.AddDays(-6),
            },
            "0.1.0");

        Assert.IsFalse(result.DidCheck);
        Assert.IsNull(result.Manifest);
    }

    [TestMethod]
    public async Task CheckAsync_ChecksDailyWhenIntervalHasElapsed()
    {
        using var tempDirectory = new TempDirectory();
        var now = DateTimeOffset.Parse("2026-07-18T00:00:00+00:00");
        var service = CreateService(tempDirectory.Path, ManifestJson("0.2.0"), now);

        var result = await service.CheckAsync(
            new AppSettings
            {
                AutoUpdateCheckMode = AutoUpdateCheckMode.Daily,
                LastAutomaticUpdateCheckUtc = now.AddDays(-1),
            },
            "0.1.0");

        Assert.IsTrue(result.DidCheck);
        Assert.IsTrue(result.IsUpdateAvailable);
        Assert.IsFalse(result.Failed);
        Assert.AreEqual(now, result.Settings.LastAutomaticUpdateCheckUtc);
        Assert.AreEqual("0.2.0", result.Manifest?.Version);
    }

    [TestMethod]
    public async Task CheckAsync_RecordsAttemptWhenCheckFails()
    {
        using var tempDirectory = new TempDirectory();
        var now = DateTimeOffset.Parse("2026-07-18T00:00:00+00:00");
        var updateService = new UpdateService(
            new HttpClient(new StatusHttpMessageHandler(HttpStatusCode.InternalServerError)),
            new Uri("https://example.com/latest.json"),
            Path.Combine(tempDirectory.Path, "updates"));
        var service = new AutomaticUpdateCheckService(updateService, () => now);

        var result = await service.CheckAsync(new AppSettings { AutoUpdateCheckMode = AutoUpdateCheckMode.Weekly }, "0.1.0");

        Assert.IsTrue(result.DidCheck);
        Assert.IsTrue(result.Failed);
        Assert.IsFalse(result.IsUpdateAvailable);
        Assert.AreEqual(now, result.Settings.LastAutomaticUpdateCheckUtc);
    }

    private static AutomaticUpdateCheckService CreateService(string root, string manifestJson, DateTimeOffset now)
    {
        var updateService = new UpdateService(
            new HttpClient(new JsonHttpMessageHandler(manifestJson)),
            new Uri("https://example.com/latest.json"),
            Path.Combine(root, "updates"));
        return new AutomaticUpdateCheckService(updateService, () => now);
    }

    private static string ManifestJson(string version)
    {
        return $$"""
            {
              "version": "{{version}}",
              "installerUrl": "https://example.com/EdgeTuckSetup.exe",
              "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
              "sizeBytes": 1,
              "releaseNotes": {
                "zh-CN": "更新说明。",
                "en-US": "Release notes."
              }
            }
            """;
    }

    private sealed class JsonHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _content;

        public JsonHttpMessageHandler(string content)
        {
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class StatusHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public StatusHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
