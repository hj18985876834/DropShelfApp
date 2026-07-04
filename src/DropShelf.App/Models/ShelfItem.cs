namespace DropShelf.App.Models;

public sealed class ShelfItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public ShelfItemType Type { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? SourcePath { get; init; }

    public string? Content { get; init; }

    public string? ImagePath { get; init; }

    public string? ThumbnailPath { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
