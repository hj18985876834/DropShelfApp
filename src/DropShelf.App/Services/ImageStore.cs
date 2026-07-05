using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DropShelf.App.Models;

namespace DropShelf.App.Services;

public sealed class ImageStore
{
    private const int ThumbnailMaxSide = 192;

    public ImageStore(string appDataRoot)
    {
        OriginalsDirectory = Path.Combine(appDataRoot, "images", "originals");
        ThumbnailsDirectory = Path.Combine(appDataRoot, "images", "thumbs");
    }

    public string OriginalsDirectory { get; }

    public string ThumbnailsDirectory { get; }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(OriginalsDirectory);
        Directory.CreateDirectory(ThumbnailsDirectory);
    }

    public ShelfItem SaveImage(BitmapSource image)
    {
        ArgumentNullException.ThrowIfNull(image);

        EnsureDirectories();

        var id = Guid.NewGuid();
        var originalPath = Path.Combine(OriginalsDirectory, $"{id:N}.png");
        var thumbnailPath = Path.Combine(ThumbnailsDirectory, $"{id:N}.png");

        SavePng(image, originalPath);
        SavePng(CreateThumbnail(image), thumbnailPath);

        return new ShelfItem
        {
            Id = id,
            Type = ShelfItemType.Image,
            DisplayName = $"Image {DateTimeOffset.Now:yyyy-MM-dd HH:mm}",
            ImagePath = originalPath,
            ThumbnailPath = thumbnailPath,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public Task<ShelfItem> SaveImageAsync(BitmapSource image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        var frozenImage = GetBackgroundSafeImage(image);
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SaveImage(frozenImage);
        }, cancellationToken);
    }

    public void DeleteImageFiles(ShelfItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Type != ShelfItemType.Image)
        {
            return;
        }

        DeleteIfAppOwned(item.ImagePath, OriginalsDirectory);
        DeleteIfAppOwned(item.ThumbnailPath, ThumbnailsDirectory);
    }

    public bool IsAppOwnedImagePath(string? path)
    {
        return IsUnderDirectory(path, OriginalsDirectory) || IsUnderDirectory(path, ThumbnailsDirectory);
    }

    private static void SavePng(BitmapSource source, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static BitmapSource GetBackgroundSafeImage(BitmapSource source)
    {
        if (source.IsFrozen)
        {
            return source;
        }

        if (source.CanFreeze)
        {
            source.Freeze();
            return source;
        }

        var copy = new WriteableBitmap(source);
        copy.Freeze();
        return copy;
    }

    private static BitmapSource CreateThumbnail(BitmapSource source)
    {
        if (source.PixelWidth <= ThumbnailMaxSide && source.PixelHeight <= ThumbnailMaxSide)
        {
            return source;
        }

        var scale = Math.Min(
            (double)ThumbnailMaxSide / source.PixelWidth,
            (double)ThumbnailMaxSide / source.PixelHeight);

        var thumbnail = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        thumbnail.Freeze();
        return thumbnail;
    }

    private static void DeleteIfAppOwned(string? path, string expectedDirectory)
    {
        if (!IsUnderDirectory(path, expectedDirectory))
        {
            return;
        }

        try
        {
            File.Delete(path!);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool IsUnderDirectory(string? path, string expectedDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string fullPath;
        string fullDirectory;
        try
        {
            fullPath = Path.GetFullPath(path);
            fullDirectory = Path.GetFullPath(expectedDirectory);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }

        if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar))
        {
            fullDirectory += Path.DirectorySeparatorChar;
        }

        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }
}
