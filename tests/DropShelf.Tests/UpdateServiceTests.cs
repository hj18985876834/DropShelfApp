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

        var installerPath = await updateService.DownloadInstallerAsync(manifest);

        Assert.AreEqual("EdgeTuckSetup.exe", Path.GetFileName(installerPath));
        Assert.IsTrue(File.Exists(installerPath));
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
