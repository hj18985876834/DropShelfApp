using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using DropShelf.App.Services;

namespace DropShelf.Tests;

[TestClass]
public sealed class UpdateServiceTests
{
    [TestMethod]
    public async Task DownloadInstallerAsync_UsesInstallerFileNameFromManifestUrl()
    {
        using var tempDirectory = new TempDirectory();
        var installerBytes = Encoding.UTF8.GetBytes("installer");
        var manifest = new UpdateManifest
        {
            Version = "0.2.0",
            InstallerUrl = "https://example.com/releases/EdgeTuckSetup.exe",
            Sha256 = Convert.ToHexString(SHA256.HashData(installerBytes)),
            SizeBytes = installerBytes.Length,
        };
        var updateService = new UpdateService(
            new HttpClient(new StubHttpMessageHandler(installerBytes)),
            new Uri("https://example.com/latest.json"),
            Path.Combine(tempDirectory.Path, "updates"));

        var result = await updateService.DownloadInstallerAsync(manifest);

        Assert.AreEqual("EdgeTuckSetup.exe", Path.GetFileName(result.InstallerPath));
        Assert.AreEqual("0.2.0", result.Version);
        Assert.AreEqual(manifest.Sha256, result.Sha256);
        Assert.AreEqual(installerBytes.Length, result.SizeBytes);
        Assert.IsTrue(File.Exists(result.InstallerPath));
    }

    [TestMethod]
    public async Task DownloadInstallerAsync_DeletesTempFileWhenChecksumDoesNotMatch()
    {
        using var tempDirectory = new TempDirectory();
        var manifest = new UpdateManifest
        {
            Version = "0.2.0",
            InstallerUrl = "https://example.com/releases/EdgeTuckSetup.exe",
            Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            SizeBytes = 9,
        };
        var updatesRoot = Path.Combine(tempDirectory.Path, "updates");
        var updateService = new UpdateService(
            new HttpClient(new StubHttpMessageHandler(Encoding.UTF8.GetBytes("installer"))),
            new Uri("https://example.com/latest.json"),
            updatesRoot);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => updateService.DownloadInstallerAsync(manifest));

        Assert.IsFalse(File.Exists(Path.Combine(updatesRoot, "0.2.0", "EdgeTuckSetup.exe.download")));
        Assert.IsFalse(File.Exists(Path.Combine(updatesRoot, "0.2.0", "EdgeTuckSetup.exe")));
    }

    [TestMethod]
    public void Validate_RequiresPositiveInstallerSize()
    {
        var manifest = new UpdateManifest
        {
            Version = "0.2.0",
            InstallerUrl = "https://example.com/releases/EdgeTuckSetup.exe",
            Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        };

        Assert.ThrowsExactly<InvalidDataException>(manifest.Validate);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] _content;

        public StubHttpMessageHandler(byte[] content)
        {
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_content),
            };
            return Task.FromResult(response);
        }
    }
}
