using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.Tests;

[TestClass]
public sealed class ImageStoreTests
{
    [TestMethod]
    public void SaveImage_CreatesOriginalAndThumbnailUnderAppData()
    {
        using var tempDirectory = new TempDirectory();
        var store = new ImageStore(tempDirectory.Path);
        var bitmap = CreateBitmap(320, 160);

        var item = store.SaveImage(bitmap);

        Assert.AreEqual(ShelfItemType.Image, item.Type);
        Assert.IsNotNull(item.ImagePath);
        Assert.IsNotNull(item.ThumbnailPath);
        Assert.IsTrue(File.Exists(item.ImagePath));
        Assert.IsTrue(File.Exists(item.ThumbnailPath));
        Assert.IsTrue(store.IsAppOwnedImagePath(item.ImagePath));
        Assert.IsTrue(store.IsAppOwnedImagePath(item.ThumbnailPath));
        StringAssert.StartsWith(item.ImagePath, store.OriginalsDirectory);
        StringAssert.StartsWith(item.ThumbnailPath, store.ThumbnailsDirectory);
    }

    [TestMethod]
    public async Task SaveImageAsync_CreatesOriginalAndThumbnailUnderAppData()
    {
        using var tempDirectory = new TempDirectory();
        var store = new ImageStore(tempDirectory.Path);
        var bitmap = CreateBitmap(320, 160);

        var item = await store.SaveImageAsync(bitmap);

        Assert.AreEqual(ShelfItemType.Image, item.Type);
        Assert.IsNotNull(item.ImagePath);
        Assert.IsNotNull(item.ThumbnailPath);
        Assert.IsTrue(File.Exists(item.ImagePath));
        Assert.IsTrue(File.Exists(item.ThumbnailPath));
        Assert.IsTrue(store.IsAppOwnedImagePath(item.ImagePath));
        Assert.IsTrue(store.IsAppOwnedImagePath(item.ThumbnailPath));
    }

    [TestMethod]
    public void SaveImage_NormalizesNonBgraBitmapToDecodablePng()
    {
        using var tempDirectory = new TempDirectory();
        var store = new ImageStore(tempDirectory.Path);
        var bitmap = CreateGrayBitmap(32, 32);

        var item = store.SaveImage(bitmap);

        AssertPngCanDecode(item.ImagePath);
        AssertPngCanDecode(item.ThumbnailPath);
    }

    [TestMethod]
    public void DeleteImageFiles_RemovesAppOwnedOriginalAndThumbnail()
    {
        using var tempDirectory = new TempDirectory();
        var store = new ImageStore(tempDirectory.Path);
        var item = store.SaveImage(CreateBitmap(24, 24));

        store.DeleteImageFiles(item);

        Assert.IsFalse(File.Exists(item.ImagePath));
        Assert.IsFalse(File.Exists(item.ThumbnailPath));
    }

    [TestMethod]
    public void DeleteImageFiles_KeepsExternalImagePath()
    {
        using var tempDirectory = new TempDirectory();
        var store = new ImageStore(Path.Combine(tempDirectory.Path, "app-data"));
        var externalImagePath = Path.Combine(tempDirectory.Path, "external.png");
        File.WriteAllText(externalImagePath, "image");
        var item = new ShelfItem
        {
            Type = ShelfItemType.Image,
            ImagePath = externalImagePath,
        };

        store.DeleteImageFiles(item);

        Assert.IsTrue(File.Exists(externalImagePath));
    }

    [TestMethod]
    public void DeleteImageFiles_IgnoresAlreadyMissingFiles()
    {
        using var tempDirectory = new TempDirectory();
        var store = new ImageStore(tempDirectory.Path);
        var item = new ShelfItem
        {
            Type = ShelfItemType.Image,
            ImagePath = Path.Combine(store.OriginalsDirectory, "missing.png"),
            ThumbnailPath = Path.Combine(store.ThumbnailsDirectory, "missing.png"),
        };

        store.DeleteImageFiles(item);
    }

    private static BitmapSource CreateBitmap(int width, int height)
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = 0x34;
            pixels[index + 1] = 0x86;
            pixels[index + 2] = 0xC5;
            pixels[index + 3] = 0xFF;
        }

        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource CreateGrayBitmap(int width, int height)
    {
        var stride = width;
        var pixels = Enumerable.Repeat((byte)0x80, stride * height).ToArray();
        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Gray8,
            null,
            pixels,
            stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static void AssertPngCanDecode(string? path)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(path));
        Assert.IsTrue(File.Exists(path));

        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        Assert.HasCount(1, decoder.Frames);
    }
}
