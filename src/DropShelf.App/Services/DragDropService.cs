using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DropShelf.App.Models;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingImage = System.Drawing.Image;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
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
    private static readonly string[] EncodedImageFormats = ["PNG", "JFIF", "TIFF"];
    private static readonly string[] DibImageFormats = [WpfDataFormats.Dib, "DeviceIndependentBitmapV5", "Format17"];
    private static readonly string[] DrawingImageFormats = ["System.Drawing.Bitmap", WpfDataFormats.Bitmap];

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
            || DibImageFormats.Any(format => HasNativeFormat(dataObject, format))
            || EncodedImageFormats.Any(format => HasNativeFormat(dataObject, format))
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

        if (item.Type is not (ShelfItemType.File or ShelfItemType.Folder or ShelfItemType.Image))
        {
            return DragOutPayloadResult.Invalid("Only file, folder, and image items can be dragged out.");
        }

        var dragOutPath = item.Type == ShelfItemType.Image
            ? item.ImagePath
            : item.SourcePath;
        if (string.IsNullOrWhiteSpace(dragOutPath))
        {
            return DragOutPayloadResult.Invalid("No source path available.");
        }

        var dragOutType = GetItemType(dragOutPath);
        if (dragOutType is null)
        {
            return DragOutPayloadResult.Invalid("Source is missing.");
        }

        if (!TryGetPathSize(dragOutPath, dragOutType.Value, MaxDragOutBytes, out var sizeBytes))
        {
            return DragOutPayloadResult.Invalid("Cannot read item size for drag-out.");
        }

        if (sizeBytes > MaxDragOutBytes)
        {
            return DragOutPayloadResult.Invalid($"Item is too large to drag out. Limit is {FormatBytes(MaxDragOutBytes)}.");
        }

        return DragOutPayloadResult.Valid(new DragOutPayload([dragOutPath], WpfDragDropEffects.Copy, sizeBytes, null));
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
        if (TryGetEncodedBitmap(dataObject, out bitmap) ||
            TryGetDrawingBitmap(dataObject, out bitmap) ||
            TryGetDibBitmap(dataObject, out bitmap) ||
            TryGetWpfBitmap(dataObject, autoConvert: false, out bitmap) ||
            TryGetWpfBitmap(dataObject, autoConvert: true, out bitmap))
        {
            bitmap = CreateBackgroundSafeCopy(bitmap);
            return true;
        }

        bitmap = null!;
        return false;
    }

    private static bool TryGetWpfBitmap(WpfDataObject dataObject, bool autoConvert, out BitmapSource bitmap)
    {
        if (TryGetData(dataObject, WpfDataFormats.Bitmap, autoConvert, out var data) &&
            data is BitmapSource bitmapSource)
        {
            bitmap = bitmapSource;
            return true;
        }

        bitmap = null!;
        return false;
    }

    private static bool TryGetDrawingBitmap(WpfDataObject dataObject, out BitmapSource bitmap)
    {
        foreach (var format in DrawingImageFormats)
        {
            if (TryGetData(dataObject, format, autoConvert: false, out var data) &&
                data is DrawingImage drawingImage)
            {
                bitmap = ConvertDrawingImage(drawingImage);
                return true;
            }
        }

        bitmap = null!;
        return false;
    }

    private static BitmapSource ConvertDrawingImage(DrawingImage drawingImage)
    {
        using var copiedBitmap = new DrawingBitmap(drawingImage.Width, drawingImage.Height, DrawingPixelFormat.Format32bppPArgb);
        using (var graphics = System.Drawing.Graphics.FromImage(copiedBitmap))
        {
            graphics.DrawImage(drawingImage, new DrawingRectangle(0, 0, copiedBitmap.Width, copiedBitmap.Height));
        }

        return ConvertDrawingBitmap(copiedBitmap);
    }

    private static bool TryGetEncodedBitmap(WpfDataObject dataObject, out BitmapSource bitmap)
    {
        foreach (var format in EncodedImageFormats)
        {
            if (TryGetData(dataObject, format, autoConvert: false, out var data) &&
                TryCreateEncodedBitmapFromData(data, out bitmap))
            {
                return true;
            }
        }

        bitmap = null!;
        return false;
    }

    private static bool TryCreateEncodedBitmapFromData(object? data, out BitmapSource bitmap)
    {
        switch (data)
        {
            case null:
                bitmap = null!;
                return false;
            case BitmapSource bitmapSource:
                bitmap = bitmapSource;
                return true;
            case byte[] bytes:
                return TryCreateBitmapFromEncodedBytes(bytes, out bitmap);
            case MemoryStream memoryStream:
                return TryCreateBitmapFromEncodedBytes(memoryStream.ToArray(), out bitmap);
            case Stream stream:
                using (var copy = new MemoryStream())
                {
                    stream.CopyTo(copy);
                    return TryCreateBitmapFromEncodedBytes(copy.ToArray(), out bitmap);
                }
            default:
                bitmap = null!;
                return false;
        }
    }

    private static bool TryGetDibBitmap(WpfDataObject dataObject, out BitmapSource bitmap)
    {
        foreach (var format in DibImageFormats)
        {
            if (TryGetData(dataObject, format, autoConvert: false, out var data) &&
                TryCreateDibBitmapFromData(data, out bitmap))
            {
                return true;
            }
        }

        bitmap = null!;
        return false;
    }

    private static bool TryCreateDibBitmapFromData(object? data, out BitmapSource bitmap)
    {
        switch (data)
        {
            case null:
                bitmap = null!;
                return false;
            case BitmapSource bitmapSource:
                bitmap = bitmapSource;
                return true;
            case byte[] bytes:
                return TryCreateBitmapFromDibBytes(bytes, out bitmap);
            case MemoryStream memoryStream:
                return TryCreateBitmapFromDibBytes(memoryStream.ToArray(), out bitmap);
            case Stream stream:
                using (var copy = new MemoryStream())
                {
                    stream.CopyTo(copy);
                    return TryCreateBitmapFromDibBytes(copy.ToArray(), out bitmap);
                }
            default:
                bitmap = null!;
                return false;
        }
    }

    private static bool TryCreateBitmapFromEncodedBytes(byte[] bytes, out BitmapSource bitmap)
    {
        if (bytes.Length == 0)
        {
            bitmap = null!;
            return false;
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            bitmap = decoder.Frames[0];
            return true;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException or InvalidOperationException)
        {
            bitmap = null!;
            return false;
        }
    }

    private static bool TryCreateBitmapFromDibBytes(byte[] dibBytes, out BitmapSource bitmap)
    {
        if (TryCreateBitmapFromUncompressedDibBytes(dibBytes, out bitmap))
        {
            return true;
        }

        try
        {
            using var bitmapStream = new MemoryStream(CreateBmpBytesFromDib(dibBytes));
            var decoder = new BmpBitmapDecoder(
                bitmapStream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            bitmap = decoder.Frames[0];
            return true;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException or InvalidOperationException or ArgumentException)
        {
            bitmap = null!;
            return false;
        }
    }

    private static bool TryCreateBitmapFromUncompressedDibBytes(byte[] dibBytes, out BitmapSource bitmap)
    {
        const int bitmapInfoHeaderSize = 40;
        const int biWidthOffset = 4;
        const int biHeightOffset = 8;
        const int biPlanesOffset = 12;
        const int biBitCountOffset = 14;
        const int biCompressionOffset = 16;
        const int biRgb = 0;
        const short expectedPlanes = 1;

        bitmap = null!;
        if (dibBytes.Length < bitmapInfoHeaderSize)
        {
            return false;
        }

        var headerSize = ReadInt32LittleEndian(dibBytes, 0);
        if (headerSize < bitmapInfoHeaderSize || headerSize > dibBytes.Length)
        {
            return false;
        }

        var width = ReadInt32LittleEndian(dibBytes, biWidthOffset);
        var rawHeight = ReadInt32LittleEndian(dibBytes, biHeightOffset);
        var planes = ReadInt16LittleEndian(dibBytes, biPlanesOffset);
        var bitCount = ReadInt16LittleEndian(dibBytes, biBitCountOffset);
        var compression = ReadInt32LittleEndian(dibBytes, biCompressionOffset);
        if (width <= 0 || rawHeight == 0 || planes != expectedPlanes || compression != biRgb)
        {
            return false;
        }

        var height = Math.Abs(rawHeight);
        var topDown = rawHeight < 0;
        var sourceStride = GetDibStride(width, bitCount);
        if (sourceStride <= 0)
        {
            return false;
        }

        var pixelOffset = GetDibPixelDataOffset(dibBytes);
        var requiredLength = pixelOffset + (sourceStride * height);
        if (pixelOffset < 0 || requiredLength > dibBytes.Length)
        {
            return false;
        }

        var pixels = bitCount switch
        {
            24 => CopyBgr24DibToBgra32(dibBytes, pixelOffset, width, height, sourceStride, topDown),
            32 => CopyBgra32DibToBgra32(dibBytes, pixelOffset, width, height, sourceStride, topDown),
            _ => null,
        };
        if (pixels is null)
        {
            return false;
        }

        bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            width * 4);
        bitmap.Freeze();
        return true;
    }

    private static byte[] CopyBgr24DibToBgra32(byte[] source, int pixelOffset, int width, int height, int sourceStride, bool topDown)
    {
        var destinationStride = width * 4;
        var destination = new byte[destinationStride * height];

        for (var y = 0; y < height; y++)
        {
            var sourceY = topDown ? y : height - 1 - y;
            var sourceRow = pixelOffset + (sourceY * sourceStride);
            var destinationRow = y * destinationStride;
            for (var x = 0; x < width; x++)
            {
                var sourceIndex = sourceRow + (x * 3);
                var destinationIndex = destinationRow + (x * 4);
                destination[destinationIndex] = source[sourceIndex];
                destination[destinationIndex + 1] = source[sourceIndex + 1];
                destination[destinationIndex + 2] = source[sourceIndex + 2];
                destination[destinationIndex + 3] = 0xFF;
            }
        }

        return destination;
    }

    private static byte[] CopyBgra32DibToBgra32(byte[] source, int pixelOffset, int width, int height, int sourceStride, bool topDown)
    {
        var destinationStride = width * 4;
        var destination = new byte[destinationStride * height];
        var hasNonZeroAlpha = false;

        for (var y = 0; y < height; y++)
        {
            var sourceY = topDown ? y : height - 1 - y;
            var sourceRow = pixelOffset + (sourceY * sourceStride);
            var destinationRow = y * destinationStride;
            for (var x = 0; x < width; x++)
            {
                var sourceIndex = sourceRow + (x * 4);
                var destinationIndex = destinationRow + (x * 4);
                destination[destinationIndex] = source[sourceIndex];
                destination[destinationIndex + 1] = source[sourceIndex + 1];
                destination[destinationIndex + 2] = source[sourceIndex + 2];
                destination[destinationIndex + 3] = source[sourceIndex + 3];
                hasNonZeroAlpha |= source[sourceIndex + 3] != 0;
            }
        }

        if (!hasNonZeroAlpha)
        {
            for (var alphaIndex = 3; alphaIndex < destination.Length; alphaIndex += 4)
            {
                destination[alphaIndex] = 0xFF;
            }
        }

        return destination;
    }

    private static byte[] CreateBmpBytesFromDib(byte[] dibBytes)
    {
        const int bitmapFileHeaderSize = 14;
        var pixelDataOffset = bitmapFileHeaderSize + GetDibPixelDataOffset(dibBytes);
        var fileSize = bitmapFileHeaderSize + dibBytes.Length;
        var bmpBytes = new byte[fileSize];

        bmpBytes[0] = (byte)'B';
        bmpBytes[1] = (byte)'M';
        WriteInt32LittleEndian(bmpBytes, 2, fileSize);
        WriteInt32LittleEndian(bmpBytes, 10, pixelDataOffset);
        Buffer.BlockCopy(dibBytes, 0, bmpBytes, bitmapFileHeaderSize, dibBytes.Length);
        return bmpBytes;
    }

    private static int GetDibPixelDataOffset(byte[] dibBytes)
    {
        const int bitmapInfoHeaderSize = 40;
        const int biBitCountOffset = 14;
        const int biCompressionOffset = 16;
        const int biClrUsedOffset = 32;
        const int rgbQuadSize = 4;
        const int bitFieldsMaskBytes = 12;
        const int biBitFields = 3;
        const int biAlphaBitFields = 6;

        if (dibBytes.Length < bitmapInfoHeaderSize)
        {
            throw new ArgumentException("DIB data is too short.", nameof(dibBytes));
        }

        var headerSize = ReadInt32LittleEndian(dibBytes, 0);
        if (headerSize <= 0 || headerSize > dibBytes.Length)
        {
            throw new ArgumentException("DIB header is invalid.", nameof(dibBytes));
        }

        if (headerSize < bitmapInfoHeaderSize)
        {
            return headerSize;
        }

        var bitCount = ReadInt16LittleEndian(dibBytes, biBitCountOffset);
        var compression = ReadInt32LittleEndian(dibBytes, biCompressionOffset);
        var colorCount = ReadInt32LittleEndian(dibBytes, biClrUsedOffset);
        if (colorCount == 0 && bitCount <= 8)
        {
            colorCount = 1 << bitCount;
        }

        var maskBytes = headerSize == bitmapInfoHeaderSize &&
            compression is biBitFields or biAlphaBitFields
                ? bitFieldsMaskBytes
                : 0;

        return headerSize + maskBytes + (colorCount * rgbQuadSize);
    }

    private static int GetDibStride(int width, short bitCount)
    {
        return bitCount is 24 or 32
            ? ((width * bitCount + 31) / 32) * 4
            : 0;
    }

    private static BitmapSource ConvertDrawingBitmap(DrawingBitmap drawingBitmap)
    {
        var hBitmap = drawingBitmap.GetHbitmap();
        try
        {
            var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            _ = DeleteObject(hBitmap);
        }
    }

    private static BitmapSource CreateBackgroundSafeCopy(BitmapSource source)
    {
        var copy = new WriteableBitmap(source);
        copy.Freeze();
        return copy;
    }

    private static short ReadInt16LittleEndian(byte[] bytes, int startIndex)
    {
        return BitConverter.ToInt16(bytes, startIndex);
    }

    private static int ReadInt32LittleEndian(byte[] bytes, int startIndex)
    {
        return BitConverter.ToInt32(bytes, startIndex);
    }

    private static void WriteInt32LittleEndian(byte[] bytes, int startIndex, int value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(valueBytes, 0, bytes, startIndex, valueBytes.Length);
    }

    private static bool TryGetData(WpfDataObject dataObject, string format, bool autoConvert, out object? data)
    {
        try
        {
            data = dataObject.GetData(format, autoConvert);
            return data is not null;
        }
        catch (Exception ex) when (ex is ExternalException or COMException or InvalidOperationException or NotSupportedException)
        {
            data = null;
            return false;
        }
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

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
