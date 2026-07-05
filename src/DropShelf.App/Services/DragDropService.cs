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
    public const string InternalDragFormat = "DropShelf.InternalDrag";
    private const int DisplayNameMaxLength = 80;
    public const long MaxDragOutBytes = 512L * 1024 * 1024;

    public bool CanCreateItems(WpfDataObject dataObject)
    {
        ArgumentNullException.ThrowIfNull(dataObject);

        if (IsInternalDrag(dataObject))
        {
            return false;
        }

        if (HasNativeFormat(dataObject, WpfDataFormats.FileDrop))
        {
            return true;
        }

        return HasNativeFormat(dataObject, WpfDataFormats.Bitmap)
            || HasNativeFormat(dataObject, WpfDataFormats.UnicodeText)
            || HasNativeFormat(dataObject, WpfDataFormats.Text);
    }

    public bool CanCreateFileSystemItems(WpfDataObject dataObject)
    {
        ArgumentNullException.ThrowIfNull(dataObject);
        if (IsInternalDrag(dataObject))
        {
            return false;
        }

        return HasNativeFormat(dataObject, WpfDataFormats.FileDrop);
    }

    public IReadOnlyList<ShelfItem> CreateItems(WpfDataObject dataObject, ImageStore imageStore)
    {
        ArgumentNullException.ThrowIfNull(dataObject);
        ArgumentNullException.ThrowIfNull(imageStore);

        if (IsInternalDrag(dataObject))
        {
            return [];
        }

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

    public async Task<IReadOnlyList<ShelfItem>> CreateItemsAsync(
        WpfDataObject dataObject,
        ImageStore imageStore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataObject);
        ArgumentNullException.ThrowIfNull(imageStore);

        if (IsInternalDrag(dataObject))
        {
            return [];
        }

        if (dataObject.GetDataPresent(WpfDataFormats.FileDrop))
        {
            return CreateFileSystemItems(dataObject);
        }

        if (TryGetBitmap(dataObject, out var bitmap))
        {
            var item = await imageStore.SaveImageAsync(bitmap, cancellationToken).ConfigureAwait(true);
            return [item];
        }

        var textItem = CreateTextOrUrlItem(GetText(dataObject));
        return textItem is null ? [] : [textItem];
    }

    public IReadOnlyList<ShelfItem> CreateFileSystemItems(WpfDataObject dataObject)
    {
        ArgumentNullException.ThrowIfNull(dataObject);
        if (IsInternalDrag(dataObject))
        {
            return [];
        }

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
        return TryCreateDragOutPayload(item).Payload;
    }

    public DragOutPayloadResult TryCreateDragOutPayload(ShelfItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Type is not (ShelfItemType.File or ShelfItemType.Folder))
        {
            return DragOutPayloadResult.Invalid("Only file and folder items can be dragged out.");
        }

        if (string.IsNullOrWhiteSpace(item.SourcePath))
        {
            return DragOutPayloadResult.Invalid("No source path available.");
        }

        var type = GetItemType(item.SourcePath);
        if (type is null)
        {
            return DragOutPayloadResult.Invalid("Source is missing.");
        }

        if (!TryGetPathSize(item.SourcePath, type.Value, MaxDragOutBytes, out var sizeBytes))
        {
            return DragOutPayloadResult.Invalid("Cannot read item size for drag-out.");
        }

        if (sizeBytes > MaxDragOutBytes)
        {
            return DragOutPayloadResult.Invalid($"Item is too large to drag out. Limit is {FormatBytes(MaxDragOutBytes)}.");
        }

        return DragOutPayloadResult.Valid(new DragOutPayload([item.SourcePath], WpfDragDropEffects.Copy, sizeBytes, null));
    }

    public bool IsInternalDrag(WpfDataObject dataObject)
    {
        ArgumentNullException.ThrowIfNull(dataObject);
        return HasNativeFormat(dataObject, InternalDragFormat);
    }

    private static bool HasNativeFormat(WpfDataObject dataObject, string format)
    {
        return dataObject.GetDataPresent(format, autoConvert: false);
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

    private static bool TryGetPathSize(string path, ShelfItemType type, long stopAfterBytes, out long sizeBytes)
    {
        try
        {
            sizeBytes = type == ShelfItemType.File
                ? new FileInfo(path).Length
                : GetDirectorySize(path, stopAfterBytes);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException or FileNotFoundException or PathTooLongException or System.Security.SecurityException)
        {
            sizeBytes = 0;
            return false;
        }
    }

    private static long GetDirectorySize(string path, long stopAfterBytes)
    {
        var totalBytes = 0L;
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(path);

        while (pendingDirectories.Count > 0)
        {
            var currentDirectory = pendingDirectories.Pop();

            foreach (var filePath in Directory.EnumerateFiles(currentDirectory))
            {
                totalBytes += new FileInfo(filePath).Length;
                if (totalBytes > stopAfterBytes)
                {
                    return totalBytes;
                }
            }

            foreach (var directoryPath in Directory.EnumerateDirectories(currentDirectory))
            {
                pendingDirectories.Push(directoryPath);
            }
        }

        return totalBytes;
    }

    private static string FormatBytes(long bytes)
    {
        const long mib = 1024L * 1024;
        return $"{bytes / mib:N0} MB";
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

    public DragOutPayload(IEnumerable<string> paths, WpfDragDropEffects allowedEffects, long totalBytes, string? blockedMessage)
    {
        ArgumentNullException.ThrowIfNull(paths);

        _paths = paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        AllowedEffects = allowedEffects;
        TotalBytes = totalBytes;
        BlockedMessage = blockedMessage;
    }

    public IReadOnlyList<string> Paths => _paths;

    public WpfDragDropEffects AllowedEffects { get; }

    public long TotalBytes { get; }

    public string? BlockedMessage { get; }

    public WpfDataObject CreateDataObject()
    {
        var dataObject = new WpfDataObjectImplementation();
        dataObject.SetData(DragDropService.InternalDragFormat, true);
        dataObject.SetData(WpfDataFormats.FileDrop, _paths);
        return dataObject;
    }
}

public sealed class DragOutPayloadResult
{
    private DragOutPayloadResult(DragOutPayload? payload, string? message, bool canStartDrag)
    {
        Payload = payload;
        Message = message;
        CanStartDrag = canStartDrag;
    }

    public DragOutPayload? Payload { get; }

    public string? Message { get; }

    public bool CanDrag => Payload is not null;

    public bool CanStartDrag { get; }

    public static DragOutPayloadResult Valid(DragOutPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new DragOutPayloadResult(payload, null, canStartDrag: true);
    }

    public static DragOutPayloadResult Invalid(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new DragOutPayloadResult(null, message, canStartDrag: false);
    }
}
