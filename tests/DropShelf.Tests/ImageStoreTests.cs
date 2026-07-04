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
}
