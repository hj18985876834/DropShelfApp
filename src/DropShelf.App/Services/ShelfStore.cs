using System.IO;
using System.Text.Json;
using DropShelf.App.Models;

namespace DropShelf.App.Services;

public sealed class ShelfStore
{
    private readonly string _shelfFilePath;

    public ShelfStore(string appDataRoot)
    {
        _shelfFilePath = Path.Combine(appDataRoot, "shelf.json");
    }

    public async Task<IReadOnlyList<ShelfItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_shelfFilePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_shelfFilePath);
        var items = await JsonSerializer.DeserializeAsync<List<ShelfItem>>(stream, cancellationToken: cancellationToken);
        return items ?? [];
    }

    public async Task SaveAsync(IEnumerable<ShelfItem> items, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_shelfFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_shelfFilePath);
        await JsonSerializer.SerializeAsync(stream, items, cancellationToken: cancellationToken);
    }
}
