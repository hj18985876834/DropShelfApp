using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using DropShelf.App.Models;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDataObject = System.Windows.IDataObject;
using WpfDataObjectImplementation = System.Windows.DataObject;
using WpfDragDropEffects = System.Windows.DragDropEffects;

namespace DropShelf.App.Services;

public sealed class DragDropService
{
    private const int DisplayNameMaxLength = 80;

    public bool CanCreateItems(WpfDataObject dataObject)
    {
        ArgumentNullException.ThrowIfNull(dataObject);

        if (dataObject.GetDataPresent(WpfDataFormats.FileDrop))
        {
            return CanCreateFileSystemItems(dataObject);
        }

        return dataObject.GetDataPresent(WpfDataFormats.Bitmap)
            || !string.IsNullOrWhiteSpace(GetText(dataObject));
    }

    public bool CanCreateFileSystemItems(WpfDataObject dataObject)
    {
        ArgumentNullException.ThrowIfNull(dataObject);
        return GetDroppedPaths(dataObject).Any(IsSupportedPath);
    }

    public IReadOnlyList<ShelfItem> CreateItems(WpfDataObject dataObject, ImageStore imageStore)
    {
        ArgumentNullException.ThrowIfNull(dataObject);
        ArgumentNullException.ThrowIfNull(imageStore);

        if (dataObject.GetDataPresent(WpfDataFormats.FileDrop))
        {
            return CreateFileSystemItems(dataObject);
        }

        if (TryGetBitmap(dataObject, out var bitmap))
        {
            return [imageStore.SaveImage(bitmap)];
        }

        var textItem = CreateTextOrUrlItem(GetText(dataObject));
        return textItem is null ? [] : [textItem];
    }

    public IReadOnlyList<ShelfItem> CreateFileSystemItems(WpfDataObject dataObject)
    {
        ArgumentNullException.ThrowIfNull(dataObject);
        return CreateFileSystemItems(GetDroppedPaths(dataObject));
    }

    public IReadOnlyList<ShelfItem> CreateFileSystemItems(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var items = new List<ShelfItem>();
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var type = GetItemType(path);
            if (type is null)
            {
                continue;
            }

            items.Add(new ShelfItem
            {
                Type = type.Value,
                SourcePath = path,
                DisplayName = GetDisplayName(path),
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        return items;
    }

    public ShelfItem? CreateTextOrUrlItem(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        if (TryCreateHttpUrl(trimmed, out var uri))
        {
            return new ShelfItem
            {
                Type = ShelfItemType.Url,
                DisplayName = uri.Host,
                Content = uri.AbsoluteUri,
                CreatedAt = DateTimeOffset.UtcNow,
            };
        }

        return new ShelfItem
        {
            Type = ShelfItemType.Text,
            DisplayName = CreateTextPreview(text),
            Content = text,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public bool IsValidUrl(string? text)
    {
        return TryCreateHttpUrl(text?.Trim(), out _);
    }

    public DragOutPayload? CreateDragOutPayload(ShelfItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Type is not (ShelfItemType.File or ShelfItemType.Folder) ||
            string.IsNullOrWhiteSpace(item.SourcePath) ||
            !IsSupportedPath(item.SourcePath))
        {
            return null;
        }

        return new DragOutPayload([item.SourcePath], WpfDragDropEffects.Copy);
    }

    private static IEnumerable<string> GetDroppedPaths(WpfDataObject dataObject)
    {
        if (!dataObject.GetDataPresent(WpfDataFormats.FileDrop))
        {
            return [];
        }

        return dataObject.GetData(WpfDataFormats.FileDrop) as string[] ?? [];
    }

    private static bool TryGetBitmap(WpfDataObject dataObject, out BitmapSource bitmap)
    {
        if (dataObject.GetData(WpfDataFormats.Bitmap) is BitmapSource bitmapSource)
        {
            bitmap = bitmapSource;
            return true;
        }

        bitmap = null!;
        return false;
    }

    private static string? GetText(WpfDataObject dataObject)
    {
        if (dataObject.GetData(WpfDataFormats.UnicodeText) is string unicodeText)
        {
            return unicodeText;
        }

        return dataObject.GetData(WpfDataFormats.Text) as string;
    }

    private static bool IsSupportedPath(string path)
    {
        return GetItemType(path) is not null;
    }

    private static ShelfItemType? GetItemType(string path)
    {
        if (File.Exists(path))
        {
            return ShelfItemType.File;
        }

        if (Directory.Exists(path))
        {
            return ShelfItemType.Folder;
        }

        return null;
    }

    private static string GetDisplayName(string path)
    {
        try
        {
            var trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var displayName = Path.GetFileName(trimmedPath);
            return string.IsNullOrWhiteSpace(displayName) ? path : displayName;
        }
        catch (ArgumentException)
        {
            return path;
        }
    }

    private static string CreateTextPreview(string text)
    {
        var firstLine = text
            .ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return "Text";
        }

        return firstLine.Length <= DisplayNameMaxLength
            ? firstLine
            : $"{firstLine[..DisplayNameMaxLength]}...";
    }

    private static bool TryCreateHttpUrl(string? text, out Uri uri)
    {
        if (Uri.TryCreate(text, UriKind.Absolute, out uri!)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        uri = null!;
        return false;
    }
}

public sealed class DragOutPayload
{
    private readonly string[] _paths;

    public DragOutPayload(IEnumerable<string> paths, WpfDragDropEffects allowedEffects)
    {
        ArgumentNullException.ThrowIfNull(paths);

        _paths = paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        AllowedEffects = allowedEffects;
    }

    public IReadOnlyList<string> Paths => _paths;

    public WpfDragDropEffects AllowedEffects { get; }

    public WpfDataObject CreateDataObject()
    {
        var dataObject = new WpfDataObjectImplementation();
        dataObject.SetData(WpfDataFormats.FileDrop, _paths);
        return dataObject;
    }
}
