using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DropShelf.App.Services;

public sealed class UpdateService
{
    public static readonly Uri DefaultManifestUri = new("https://raw.githubusercontent.com/hj18985876834/DropShelfApp/main/updates/latest.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _manifestUri;
    private readonly string _updatesRoot;

    public UpdateService(string appDataRoot)
        : this(new HttpClient(), DefaultManifestUri, Path.Combine(appDataRoot, "updates"))
    {
    }

    public UpdateService(HttpClient httpClient, Uri manifestUri, string updatesRoot)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _manifestUri = manifestUri ?? throw new ArgumentNullException(nameof(manifestUri));
        _updatesRoot = string.IsNullOrWhiteSpace(updatesRoot)
            ? throw new ArgumentException("Updates root is required.", nameof(updatesRoot))
            : updatesRoot;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        var current = AppVersion.Parse(currentVersion);
        using var response = await _httpClient.GetAsync(_manifestUri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("Update manifest is empty.");
        manifest.Validate();

        var latest = AppVersion.Parse(manifest.Version);
        return latest.CompareTo(current) > 0
            ? UpdateCheckResult.Available(manifest)
            : UpdateCheckResult.NotAvailable(manifest);
    }

    public async Task<DownloadedInstallerResult> DownloadInstallerAsync(UpdateManifest manifest, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        manifest.Validate();
        var version = AppVersion.Parse(manifest.Version);
        var targetDirectory = Path.Combine(_updatesRoot, version.ToString());
        Directory.CreateDirectory(targetDirectory);
        var targetPath = Path.Combine(targetDirectory, manifest.InstallerFileName);
        var tempPath = targetPath + ".download";

        using var response = await _httpClient.GetAsync(manifest.InstallerUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? manifest.SizeBytes;
        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var destination = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await CopyWithProgressAsync(source, destination, totalBytes, progress, cancellationToken).ConfigureAwait(false);
        }

        var actualHash = await ComputeSha256Async(tempPath, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(actualHash, manifest.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempPath);
            throw new InvalidDataException("Downloaded installer checksum does not match the update manifest.");
        }

        var actualSizeBytes = new FileInfo(tempPath).Length;
        if (manifest.SizeBytes is { } expectedSizeBytes && actualSizeBytes != expectedSizeBytes)
        {
            File.Delete(tempPath);
            throw new InvalidDataException("Downloaded installer size does not match the update manifest.");
        }

        File.Move(tempPath, targetPath, overwrite: true);
        progress?.Report(1);
        return new DownloadedInstallerResult(targetPath, manifest.Version, actualHash, actualSizeBytes);
    }

    public void LaunchInstaller(string installerPath)
    {
        if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
        {
            throw new FileNotFoundException("Installer was not found.", installerPath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true,
        });
    }

    private static async Task CopyWithProgressAsync(Stream source, Stream destination, long? totalBytes, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long copiedBytes = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            copiedBytes += bytesRead;
            if (totalBytes is > 0)
            {
                progress?.Report(Math.Clamp((double)copiedBytes / totalBytes.Value, 0, 1));
            }
        }
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }
}

public sealed record UpdateCheckResult(bool IsUpdateAvailable, UpdateManifest Manifest)
{
    public static UpdateCheckResult Available(UpdateManifest manifest)
    {
        return new UpdateCheckResult(true, manifest);
    }

    public static UpdateCheckResult NotAvailable(UpdateManifest manifest)
    {
        return new UpdateCheckResult(false, manifest);
    }
}

public sealed class UpdateManifest
{
    public string Version { get; init; } = string.Empty;

    public string InstallerUrl { get; init; } = string.Empty;

    public string Sha256 { get; init; } = string.Empty;

    public long? SizeBytes { get; init; }

    public string? ReleaseDate { get; init; }

    public bool Mandatory { get; init; }

    public AppBranding? Branding { get; init; }

    public UpdateReleaseNotes ReleaseNotes { get; init; } = new();

    [JsonIgnore]
    public Uri InstallerUri => new(InstallerUrl);

    [JsonIgnore]
    public string InstallerFileName
    {
        get
        {
            var fileName = Path.GetFileName(InstallerUri.LocalPath);
            return string.IsNullOrWhiteSpace(fileName)
                ? "EdgeTuckSetup.exe"
                : fileName;
        }
    }

    public string ReleaseNotesFor(bool useChinese)
    {
        return useChinese
            ? FirstNonEmpty(ReleaseNotes.Chinese, ReleaseNotes.English)
            : FirstNonEmpty(ReleaseNotes.English, ReleaseNotes.Chinese);
    }

    public void Validate()
    {
        _ = AppVersion.Parse(Version);
        if (!Uri.TryCreate(InstallerUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException("Update manifest installerUrl is invalid.");
        }

        if (Sha256.Length != 64 || Sha256.Any(value => !Uri.IsHexDigit(value)))
        {
            throw new InvalidDataException("Update manifest sha256 is invalid.");
        }

        if (SizeBytes is null or <= 0)
        {
            throw new InvalidDataException("Update manifest sizeBytes is invalid.");
        }
    }

    private static string FirstNonEmpty(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        return fallback ?? string.Empty;
    }
}

public sealed record DownloadedInstallerResult(
    string InstallerPath,
    string Version,
    string Sha256,
    long SizeBytes);

public sealed class UpdateReleaseNotes
{
    [JsonPropertyName("zh-CN")]
    public string? Chinese { get; init; }

    [JsonPropertyName("en-US")]
    public string? English { get; init; }
}

public readonly record struct AppVersion(int Major, int Minor, int Patch) : IComparable<AppVersion>
{
    public static AppVersion Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("Version is required.");
        }

        var normalized = value.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        var metadataIndex = normalized.IndexOfAny(['+', '-']);
        if (metadataIndex >= 0)
        {
            normalized = normalized[..metadataIndex];
        }

        var parts = normalized.Split('.');
        if (parts.Length is < 2 or > 3 ||
            !int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor))
        {
            throw new FormatException($"Version '{value}' is invalid.");
        }

        var patch = 0;
        if (parts.Length == 3 && !int.TryParse(parts[2], out patch))
        {
            throw new FormatException($"Version '{value}' is invalid.");
        }

        return new AppVersion(major, minor, patch);
    }

    public int CompareTo(AppVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        var minorComparison = Minor.CompareTo(other.Minor);
        return minorComparison != 0
            ? minorComparison
            : Patch.CompareTo(other.Patch);
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}
