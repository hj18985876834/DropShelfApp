using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.Tests;

[TestClass]
public sealed class DragDropServiceTests
{
    [TestMethod]
    public void CreateFileSystemItems_CreatesFileItemFromPath()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "report.txt");
        File.WriteAllText(filePath, "hello");
        var service = new DragDropService();

        var items = service.CreateFileSystemItems([filePath]);

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.File, items[0].Type);
        Assert.AreEqual(filePath, items[0].SourcePath);
        Assert.AreEqual("report.txt", items[0].DisplayName);
    }

    [TestMethod]
    public void CreateFileSystemItems_CreatesFolderItemFromPath()
    {
        using var tempDirectory = new TempDirectory();
        var folderPath = Path.Combine(tempDirectory.Path, "Invoices");
        Directory.CreateDirectory(folderPath);
        var service = new DragDropService();

        var items = service.CreateFileSystemItems([folderPath]);

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.Folder, items[0].Type);
        Assert.AreEqual(folderPath, items[0].SourcePath);
        Assert.AreEqual("Invoices", items[0].DisplayName);
    }

    [TestMethod]
    public void CreateFileSystemItems_PreservesMixedFileAndFolderOrder()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "first.txt");
        var folderPath = Path.Combine(tempDirectory.Path, "Second");
        File.WriteAllText(filePath, "hello");
        Directory.CreateDirectory(folderPath);
        var service = new DragDropService();

        var items = service.CreateFileSystemItems([filePath, folderPath]);

        Assert.HasCount(2, items);
        Assert.AreEqual(ShelfItemType.File, items[0].Type);
        Assert.AreEqual(ShelfItemType.Folder, items[1].Type);
    }

    [TestMethod]
    public void CreateFileSystemItems_IgnoresMissingAndUnsupportedPaths()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "kept.txt");
        File.WriteAllText(filePath, "hello");
        var missingPath = Path.Combine(tempDirectory.Path, "missing.txt");
        var service = new DragDropService();

        var items = service.CreateFileSystemItems([missingPath, "", filePath]);

        Assert.HasCount(1, items);
        Assert.AreEqual(filePath, items[0].SourcePath);
    }

    [TestMethod]
    public void CreateFileSystemItems_TreatsImageFileAsFileReference()
    {
        using var tempDirectory = new TempDirectory();
        var imagePath = Path.Combine(tempDirectory.Path, "image.png");
        File.WriteAllText(imagePath, "path reference only");
        var service = new DragDropService();

        var items = service.CreateFileSystemItems([imagePath]);

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.File, items[0].Type);
        Assert.AreEqual(imagePath, items[0].SourcePath);
        Assert.IsNull(items[0].ImagePath);
        Assert.IsNull(items[0].ThumbnailPath);
    }

    [TestMethod]
    public void CreateItems_PrioritizesFileDropOverTextFormats()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "app-data");
        var filePath = Path.Combine(tempDirectory.Path, "image.png");
        File.WriteAllText(filePath, "path reference only");
        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.FileDrop, new[] { filePath });
        dataObject.SetText("https://example.com/image.png");
        var service = new DragDropService();

        var items = service.CreateItems(dataObject, new ImageStore(appDataRoot));

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.File, items[0].Type);
        Assert.AreEqual(filePath, items[0].SourcePath);
    }

    [TestMethod]
    public async Task CreateItemsAsync_CreatesImageItemFromBitmapData()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "app-data");
        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.Bitmap, CreateBitmap(24, 24));
        var service = new DragDropService();

        var items = await service.CreateItemsAsync(dataObject, new ImageStore(appDataRoot));

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.Image, items[0].Type);
        Assert.IsNotNull(items[0].ImagePath);
        Assert.IsNotNull(items[0].ThumbnailPath);
        Assert.IsTrue(File.Exists(items[0].ImagePath));
        Assert.IsTrue(File.Exists(items[0].ThumbnailPath));
    }

    [TestMethod]
    public void CanCreateItems_ChecksFormatsWithoutReadingPayload()
    {
        var dataObject = new FormatOnlyDataObject(DataFormats.UnicodeText);
        var service = new DragDropService();

        Assert.IsTrue(service.CanCreateItems(dataObject));
    }

    [TestMethod]
    public void CreateTextOrUrlItem_CreatesTextItemForPlainText()
    {
        var service = new DragDropService();

        var item = service.CreateTextOrUrlItem("hello world\r\nsecond line");

        Assert.IsNotNull(item);
        Assert.AreEqual(ShelfItemType.Text, item.Type);
        Assert.AreEqual("hello world\r\nsecond line", item.Content);
        Assert.AreEqual("hello world", item.DisplayName);
    }

    [TestMethod]
    public void CreateTextOrUrlItem_CreatesUrlItemForHttpUrl()
    {
        var service = new DragDropService();

        var item = service.CreateTextOrUrlItem("https://example.com/path?q=1");

        Assert.IsNotNull(item);
        Assert.AreEqual(ShelfItemType.Url, item.Type);
        Assert.AreEqual("https://example.com/path?q=1", item.Content);
        Assert.AreEqual("example.com", item.DisplayName);
    }

    [TestMethod]
    [DataRow("https://example.com")]
    [DataRow("http://example.com/path")]
    public void IsValidUrl_ReturnsTrueForHttpUrls(string url)
    {
        var service = new DragDropService();

        Assert.IsTrue(service.IsValidUrl(url));
    }

    [TestMethod]
    [DataRow("example.com")]
    [DataRow("ftp://example.com/file.txt")]
    [DataRow("not a url")]
    [DataRow("")]
    public void IsValidUrl_ReturnsFalseForUnsupportedText(string text)
    {
        var service = new DragDropService();

        Assert.IsFalse(service.IsValidUrl(text));
    }

    [TestMethod]
    public void CreateDragOutPayload_ReturnsCopyFileDropForExistingFile()
    {
        using var tempDirectory = new TempDirectory();
        var sourcePath = Path.Combine(tempDirectory.Path, "source.txt");
        File.WriteAllText(sourcePath, "content");
        var service = new DragDropService();

        var payload = service.CreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "source.txt",
            SourcePath = sourcePath,
        });

        Assert.IsNotNull(payload);
        Assert.AreEqual(DragDropEffects.Copy, payload.AllowedEffects);
        Assert.AreEqual(7, payload.TotalBytes);
        CollectionAssert.AreEqual(new[] { sourcePath }, payload.Paths.ToArray());
        Assert.IsTrue(payload.CreateDataObject().GetDataPresent(DragDropService.InternalDragFormat));
    }

    [TestMethod]
    public void CreateDragOutPayload_ReturnsCopyFileDropForExistingFolder()
    {
        using var tempDirectory = new TempDirectory();
        var sourcePath = Path.Combine(tempDirectory.Path, "folder");
        Directory.CreateDirectory(sourcePath);
        var service = new DragDropService();

        var payload = service.CreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.Folder,
            DisplayName = "folder",
            SourcePath = sourcePath,
        });

        Assert.IsNotNull(payload);
        Assert.AreEqual(DragDropEffects.Copy, payload.AllowedEffects);
        Assert.AreEqual(0, payload.TotalBytes);
        CollectionAssert.AreEqual(new[] { sourcePath }, payload.Paths.ToArray());
    }

    [TestMethod]
    public void TryCreateDragOutPayload_ReturnsMessageForOversizedFile()
    {
        using var tempDirectory = new TempDirectory();
        var sourcePath = Path.Combine(tempDirectory.Path, "large.bin");
        using (var stream = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.SetLength(DragDropService.MaxDragOutBytes + 1);
        }

        var service = new DragDropService();

        var result = service.TryCreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "large.bin",
            SourcePath = sourcePath,
        });

        Assert.IsFalse(result.CanStartDrag);
        Assert.IsFalse(result.CanDrag);
        Assert.IsNull(result.Payload);
        StringAssert.Contains(result.Message, "too large");
        StringAssert.Contains(result.Message, "512 MB");
    }

    [TestMethod]
    public void CreateItems_IgnoresInternalDragToAvoidDuplicateShelfRecords()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "app-data");
        var sourcePath = Path.Combine(tempDirectory.Path, "source.txt");
        File.WriteAllText(sourcePath, "content");
        var service = new DragDropService();
        var payload = service.CreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "source.txt",
            SourcePath = sourcePath,
        });
        Assert.IsNotNull(payload);
        var dataObject = payload.CreateDataObject();

        var items = service.CreateItems(dataObject, new ImageStore(appDataRoot));

        Assert.IsEmpty(items);
        Assert.IsFalse(service.CanCreateItems(dataObject));
    }

    [TestMethod]
    public void TryCreateDragOutPayload_ReturnsMessageForOversizedFolder()
    {
        using var tempDirectory = new TempDirectory();
        var sourcePath = Path.Combine(tempDirectory.Path, "large-folder");
        Directory.CreateDirectory(sourcePath);
        using (var stream = new FileStream(Path.Combine(sourcePath, "large.bin"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.SetLength(DragDropService.MaxDragOutBytes + 1);
        }

        var service = new DragDropService();

        var result = service.TryCreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.Folder,
            DisplayName = "large-folder",
            SourcePath = sourcePath,
        });

        Assert.IsFalse(result.CanStartDrag);
        Assert.IsFalse(result.CanDrag);
        Assert.IsNull(result.Payload);
        StringAssert.Contains(result.Message, "too large");
        StringAssert.Contains(result.Message, "512 MB");
    }

    [TestMethod]
    public void TryCreateDragOutPayload_ReturnsMessageForMissingSource()
    {
        var service = new DragDropService();

        var result = service.TryCreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "missing.txt",
            SourcePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.txt"),
        });

        Assert.IsFalse(result.CanStartDrag);
        Assert.IsFalse(result.CanDrag);
        Assert.IsNull(result.Payload);
        Assert.AreEqual("Source is missing.", result.Message);
    }

    [TestMethod]
    public void CreateDragOutPayload_ReturnsNullForMissingSource()
    {
        var service = new DragDropService();

        var payload = service.CreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "missing.txt",
            SourcePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.txt"),
        });

        Assert.IsNull(payload);
    }

    [TestMethod]
    public void CreateDragOutPayload_ReturnsNullForNonFileSystemItem()
    {
        var service = new DragDropService();

        var payload = service.CreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.Text,
            DisplayName = "note",
            Content = "hello",
        });

        Assert.IsNull(payload);
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

    private sealed class FormatOnlyDataObject : IDataObject
    {
        private readonly string _format;

        public FormatOnlyDataObject(string format)
        {
            _format = format;
        }

        public object GetData(string format)
        {
            throw new InvalidOperationException("CanCreateItems must not read payload data.");
        }

        public object GetData(Type format)
        {
            throw new InvalidOperationException("CanCreateItems must not read payload data.");
        }

        public object GetData(string format, bool autoConvert)
        {
            throw new InvalidOperationException("CanCreateItems must not read payload data.");
        }

        public bool GetDataPresent(string format)
        {
            return string.Equals(format, _format, StringComparison.Ordinal);
        }

        public bool GetDataPresent(Type format)
        {
            return string.Equals(format.FullName, _format, StringComparison.Ordinal);
        }

        public bool GetDataPresent(string format, bool autoConvert)
        {
            return !autoConvert && string.Equals(format, _format, StringComparison.Ordinal);
        }

        public string[] GetFormats()
        {
            return [_format];
        }

        public string[] GetFormats(bool autoConvert)
        {
            return [_format];
        }

        public void SetData(object data)
        {
            throw new NotSupportedException();
        }

        public void SetData(string format, object data)
        {
            throw new NotSupportedException();
        }

        public void SetData(Type format, object data)
        {
            throw new NotSupportedException();
        }

        public void SetData(string format, object data, bool autoConvert)
        {
            throw new NotSupportedException();
        }
    }
}
