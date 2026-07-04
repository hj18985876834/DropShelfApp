using System.IO;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.Tests;

[TestClass]
public sealed class ShelfStoreTests
{
    [TestMethod]
    public async Task LoadAsync_ReturnsEmptyListWhenFileIsMissing()
    {
        using var tempDirectory = new TempDirectory();
        var store = new ShelfStore(tempDirectory.Path);

        var items = await store.LoadAsync();

        Assert.AreEqual(0, items.Count);
    }

    [TestMethod]
    public async Task SaveAsync_CreatesDirectoryAndRoundTripsItemsInOrder()
    {
        using var tempDirectory = new TempDirectory();
        var appDataRoot = Path.Combine(tempDirectory.Path, "nested", "DropShelf");
        var store = new ShelfStore(appDataRoot);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var expected = new[]
        {
            new ShelfItem
            {
                Id = firstId,
                Type = ShelfItemType.File,
                DisplayName = "Report.pdf",
                SourcePath = @"C:\Users\me\Downloads\Report.pdf",
                CreatedAt = DateTimeOffset.Parse("2026-07-04T08:30:00Z"),
            },
            new ShelfItem
            {
                Id = secondId,
                Type = ShelfItemType.Text,
                DisplayName = "Note",
                Content = "Call Alice",
                CreatedAt = DateTimeOffset.Parse("2026-07-04T08:35:00Z"),
            },
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.IsTrue(File.Exists(Path.Combine(appDataRoot, "shelf.json")));
        Assert.AreEqual(2, actual.Count);
        Assert.AreEqual(firstId, actual[0].Id);
        Assert.AreEqual(ShelfItemType.File, actual[0].Type);
        Assert.AreEqual(expected[0].DisplayName, actual[0].DisplayName);
        Assert.AreEqual(expected[0].SourcePath, actual[0].SourcePath);
        Assert.AreEqual(secondId, actual[1].Id);
        Assert.AreEqual(ShelfItemType.Text, actual[1].Type);
        Assert.AreEqual(expected[1].Content, actual[1].Content);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsEmptyListWhenJsonIsMalformed()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "shelf.json"), "{ malformed");
        var store = new ShelfStore(tempDirectory.Path);

        var items = await store.LoadAsync();

        Assert.AreEqual(0, items.Count);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsEmptyListWhenItemTypeIsUnknown()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(
            Path.Combine(tempDirectory.Path, "shelf.json"),
            """
            [
              {
                "id": "9b75ab36-f734-4c8f-a758-9c5351437807",
                "type": "unknown",
                "displayName": "Bad item",
                "createdAt": "2026-07-04T08:30:00Z"
              }
            ]
            """);
        var store = new ShelfStore(tempDirectory.Path);

        var items = await store.LoadAsync();

        Assert.AreEqual(0, items.Count);
    }
}
