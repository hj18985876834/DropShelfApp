using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DropShelf.App.Models;
using DropShelf.App.Services;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingBrushes = System.Drawing.Brushes;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using DrawingRectangle = System.Drawing.Rectangle;

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
    public void CreateFileSystemItems_TreatsImageFileAsImageReference()
    {
        using var tempDirectory = new TempDirectory();
        var imagePath = Path.Combine(tempDirectory.Path, "image.png");
        File.WriteAllText(imagePath, "path reference only");
        var service = new DragDropService();

        var items = service.CreateFileSystemItems([imagePath]);

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.Image, items[0].Type);
        Assert.AreEqual(imagePath, items[0].SourcePath);
        Assert.AreEqual(imagePath, items[0].ImagePath);
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
        Assert.AreEqual(ShelfItemType.Image, items[0].Type);
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
    public async Task CreateItemsAsync_CreatesImageItemFromDrawingBitmapData()
    {
        using var tempDirectory = new TempDirectory();
        using var drawingBitmap = CreateDrawingBitmap(24, 24);
        var appDataRoot = Path.Combine(tempDirectory.Path, "app-data");
        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.Bitmap, drawingBitmap, autoConvert: false);
        var service = new DragDropService();

        var items = await service.CreateItemsAsync(dataObject, new ImageStore(appDataRoot));

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.Image, items[0].Type);
        AssertPngCanDecode(items[0].ImagePath);
        AssertPngCanDecode(items[0].ThumbnailPath);
    }

    [TestMethod]
    public async Task CreateItemsAsync_CreatesImageItemFromPngClipboardBytes()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "app-data");
        var dataObject = new DataObject();
        dataObject.SetData("PNG", CreatePngBytes(CreateBitmap(24, 24)), autoConvert: false);
        var service = new DragDropService();

        var items = await service.CreateItemsAsync(dataObject, new ImageStore(appDataRoot));

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.Image, items[0].Type);
        AssertPngCanDecode(items[0].ImagePath);
        AssertPngCanDecode(items[0].ThumbnailPath);
    }

    [TestMethod]
    public async Task CreateItemsAsync_CreatesImageItemFromDibClipboardBytes()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "app-data");
        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.Dib, CreateDibBytes(24, 24), autoConvert: false);
        var service = new DragDropService();

        var items = await service.CreateItemsAsync(dataObject, new ImageStore(appDataRoot));

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.Image, items[0].Type);
        AssertPngCanDecode(items[0].ImagePath);
        AssertPngCanDecode(items[0].ThumbnailPath);
    }

    [TestMethod]
    public async Task CreateItemsAsync_CreatesImageItemFromDibV5ClipboardBytes()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "app-data");
        var dataObject = new DataObject();
        dataObject.SetData("DeviceIndependentBitmapV5", CreateDibBytes(24, 24), autoConvert: false);
        var service = new DragDropService();

        var items = await service.CreateItemsAsync(dataObject, new ImageStore(appDataRoot));

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.Image, items[0].Type);
        AssertPngCanDecode(items[0].ImagePath);
        AssertPngCanDecode(items[0].ThumbnailPath);
    }

    [TestMethod]
    public async Task CreateItemsAsync_TreatsAllZeroDibAlphaAsOpaque()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "app-data");
        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.Dib, CreateDibBytes(24, 24, alpha: 0x00), autoConvert: false);
        var service = new DragDropService();

        var items = await service.CreateItemsAsync(dataObject, new ImageStore(appDataRoot));

        Assert.HasCount(1, items);
        var pixel = ReadFirstPixel(items[0].ImagePath);
        Assert.AreEqual(0x34, pixel.Blue);
        Assert.AreEqual(0x86, pixel.Green);
        Assert.AreEqual(0xC5, pixel.Red);
        Assert.AreEqual(0xFF, pixel.Alpha);
    }

    [TestMethod]
    public void CanCreateItems_AcceptsDibV5FormatWithoutReadingPayload()
    {
        var dataObject = new FormatOnlyDataObject("DeviceIndependentBitmapV5");
        var service = new DragDropService();

        Assert.IsTrue(service.CanCreateItems(dataObject));
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
    public void CreateTextOrUrlItems_CreatesMultipleUrlItemsFromLines()
    {
        var service = new DragDropService();

        var items = service.CreateTextOrUrlItems("https://example.com/one\r\nhttps://example.com/two");

        Assert.HasCount(2, items);
        Assert.IsTrue(items.All(item => item.Type == ShelfItemType.Url));
        CollectionAssert.AreEqual(
            new[] { "https://example.com/one", "https://example.com/two" },
            items.Select(item => item.Content).ToArray());
    }

    [TestMethod]
    public void CreateTextOrUrlItems_CreatesFileFolderAndUrlItemsFromStructuredLines()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "report.txt");
        var folderPath = Path.Combine(tempDirectory.Path, "Assets");
        File.WriteAllText(filePath, "hello");
        Directory.CreateDirectory(folderPath);
        var service = new DragDropService();

        var items = service.CreateTextOrUrlItems($"{filePath}{Environment.NewLine}{folderPath}{Environment.NewLine}https://example.com");

        Assert.HasCount(3, items);
        Assert.AreEqual(ShelfItemType.File, items[0].Type);
        Assert.AreEqual(ShelfItemType.Folder, items[1].Type);
        Assert.AreEqual(ShelfItemType.Url, items[2].Type);
    }

    [TestMethod]
    public void CreateTextOrUrlItems_KeepsPlainMultilineTextAsSingleTextItem()
    {
        var service = new DragDropService();

        var items = service.CreateTextOrUrlItems("first line\r\nsecond line");

        Assert.HasCount(1, items);
        Assert.AreEqual(ShelfItemType.Text, items[0].Type);
        Assert.AreEqual("first line\r\nsecond line", items[0].Content);
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
    public void CreateDragOutPayload_ReturnsCopyFileDropForExistingImageItem()
    {
        using var tempDirectory = new TempDirectory();
        var imagePath = Path.Combine(tempDirectory.Path, "capture.png");
        File.WriteAllBytes(imagePath, [0x01, 0x02, 0x03]);
        var service = new DragDropService();

        var payload = service.CreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.Image,
            DisplayName = "capture",
            ImagePath = imagePath,
        });

        Assert.IsNotNull(payload);
        Assert.AreEqual(DragDropEffects.Copy, payload.AllowedEffects);
        Assert.AreEqual(3, payload.TotalBytes);
        CollectionAssert.AreEqual(new[] { imagePath }, payload.Paths.ToArray());
        Assert.IsTrue(payload.CreateDataObject().GetDataPresent(DataFormats.FileDrop));
    }

    [TestMethod]
    public void CreateDragOutPayload_ReturnsTextPayloadForTextItem()
    {
        var service = new DragDropService();

        var payload = service.CreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.Text,
            DisplayName = "note",
            Content = "hello world",
        });

        Assert.IsNotNull(payload);
        Assert.IsTrue(payload.HasText);
        Assert.AreEqual("hello world", payload.Text);
        Assert.AreEqual(11, payload.TotalBytes);
        var dataObject = payload.CreateDataObject();
        Assert.IsTrue(dataObject.GetDataPresent(DragDropService.InternalDragFormat));
        Assert.IsTrue(dataObject.GetDataPresent(DataFormats.UnicodeText));
        Assert.AreEqual("hello world", dataObject.GetData(DataFormats.UnicodeText));
        Assert.IsFalse(dataObject.GetDataPresent(DataFormats.FileDrop));
    }

    [TestMethod]
    public void CreateDragOutPayload_ReturnsTextOnlyPayloadForUrlItem()
    {
        var service = new DragDropService();

        var payload = service.CreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.Url,
            DisplayName = "example.com",
            Content = "https://example.com/path",
        });

        Assert.IsNotNull(payload);
        var dataObject = payload.CreateDataObject();
        Assert.AreEqual("https://example.com/path", dataObject.GetData(DataFormats.UnicodeText));
        Assert.IsFalse(dataObject.GetDataPresent("UniformResourceLocatorW"));
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
        StringAssert.Contains(result.Message, "项目过大");
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
        StringAssert.Contains(result.Message, "项目过大");
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
        Assert.AreEqual("源文件缺失。", result.Message);
    }

    [TestMethod]
    public void TryCreateDragOutPayload_ReturnsMessageForMissingImagePath()
    {
        var service = new DragDropService();

        var result = service.TryCreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.Image,
            DisplayName = "missing image",
            ImagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.png"),
        });

        Assert.IsFalse(result.CanStartDrag);
        Assert.IsFalse(result.CanDrag);
        Assert.IsNull(result.Payload);
        Assert.AreEqual("源文件缺失。", result.Message);
    }

    [TestMethod]
    public void TryCreateDragOutPayload_UsesConfiguredLanguageForMessages()
    {
        var service = new DragDropService(new LocalizationService(LanguageMode.English));

        var result = service.TryCreateDragOutPayload(new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "missing.txt",
            SourcePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.txt"),
        });

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

        Assert.IsNotNull(payload);
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

    private static DrawingBitmap CreateDrawingBitmap(int width, int height)
    {
        var bitmap = new DrawingBitmap(width, height, DrawingPixelFormat.Format32bppPArgb);
        using var graphics = DrawingGraphics.FromImage(bitmap);
        graphics.FillRectangle(DrawingBrushes.CornflowerBlue, new DrawingRectangle(0, 0, width, height));
        return bitmap;
    }

    private static byte[] CreatePngBytes(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static byte[] CreateDibBytes(int width, int height, byte alpha = 0xFF)
    {
        const int headerSize = 40;
        const short planes = 1;
        const short bitCount = 32;
        var stride = width * 4;
        var imageSize = stride * height;
        var bytes = new byte[headerSize + imageSize];

        WriteInt32LittleEndian(bytes, 0, headerSize);
        WriteInt32LittleEndian(bytes, 4, width);
        WriteInt32LittleEndian(bytes, 8, height);
        WriteInt16LittleEndian(bytes, 12, planes);
        WriteInt16LittleEndian(bytes, 14, bitCount);
        WriteInt32LittleEndian(bytes, 20, imageSize);

        var pixelStart = headerSize;
        for (var row = 0; row < height; row++)
        {
            for (var column = 0; column < width; column++)
            {
                var index = pixelStart + (row * stride) + (column * 4);
                bytes[index] = 0x34;
                bytes[index + 1] = 0x86;
                bytes[index + 2] = 0xC5;
                bytes[index + 3] = alpha;
            }
        }

        return bytes;
    }

    private static (byte Blue, byte Green, byte Red, byte Alpha) ReadFirstPixel(string? path)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(path));

        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
        var pixels = new byte[4];
        frame.CopyPixels(new Int32Rect(0, 0, 1, 1), pixels, 4, 0);
        return (pixels[0], pixels[1], pixels[2], pixels[3]);
    }

    private static void AssertPngCanDecode(string? path)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(path));
        Assert.IsTrue(File.Exists(path));

        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        Assert.HasCount(1, decoder.Frames);
    }

    private static void WriteInt16LittleEndian(byte[] bytes, int startIndex, short value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(valueBytes, 0, bytes, startIndex, valueBytes.Length);
    }

    private static void WriteInt32LittleEndian(byte[] bytes, int startIndex, int value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(valueBytes, 0, bytes, startIndex, valueBytes.Length);
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
