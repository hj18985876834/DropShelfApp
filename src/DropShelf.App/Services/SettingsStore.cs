using System.IO;
using System.Text.Json;
using DropShelf.App.Models;

namespace DropShelf.App.Services;

public class SettingsStore
{
    private readonly string _settingsFilePath;

    public SettingsStore(string appDataRoot)
    {
        _settingsFilePath = Path.Combine(appDataRoot, "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(_settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                PersistenceJsonOptions.Default,
                cancellationToken).ConfigureAwait(false);

            if (settings is null || !HasValidEnums(settings))
            {
                return AppSettings.CreateDefault();
            }

            return settings;
        }
        catch (JsonException)
        {
            return AppSettings.CreateDefault();
        }
        catch (IOException)
        {
            return AppSettings.CreateDefault();
        }
    }

    public virtual async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, PersistenceJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool HasValidEnums(AppSettings settings)
    {
        return Enum.IsDefined(settings.DockEdge)
            && Enum.IsDefined(settings.ThemeMode)
            && Enum.IsDefined(settings.DensityMode)
            && Enum.IsDefined(settings.LanguageMode);
    }
}
