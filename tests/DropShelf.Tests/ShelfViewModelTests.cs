using System.IO;
using DropShelf.App.Models;
using DropShelf.App.Services;
using DropShelf.App.ViewModels;

namespace DropShelf.Tests;

[TestClass]
public sealed class ShelfViewModelTests
{
    [TestMethod]
    public void Constructor_StartsWithShelfHidden()
    {
        var viewModel = CreateViewModel();

        Assert.IsFalse(viewModel.IsShelfVisible);
    }

    [TestMethod]
    public void Constructor_AddsInitialShelfItemsInOrder()
    {
        var first = new ShelfItem { DisplayName = "First", Type = ShelfItemType.Text };
        var second = new ShelfItem { DisplayName = "Second", Type = ShelfItemType.Url };

        var viewModel = CreateViewModel(initialItems: [first, second]);

        Assert.HasCount(2, viewModel.Items);
        Assert.AreSame(first, viewModel.Items[0].Item);
        Assert.AreSame(second, viewModel.Items[1].Item);
        CollectionAssert.AreEqual(new[] { first, second }, viewModel.GetShelfItems().ToArray());
        Assert.IsFalse(viewModel.IsEmpty);
        Assert.IsTrue(viewModel.HasItems);
    }

    [TestMethod]
    public void ShellCommands_UpdateShelfVisibility()
    {
        var viewModel = CreateViewModel();

        viewModel.ShowShelfCommand.Execute(null);
        Assert.IsTrue(viewModel.IsShelfVisible);

        viewModel.ToggleShelfCommand.Execute(null);
        Assert.IsFalse(viewModel.IsShelfVisible);

        viewModel.ToggleShelfCommand.Execute(null);
        Assert.IsTrue(viewModel.IsShelfVisible);

        viewModel.HideShelfCommand.Execute(null);
        Assert.IsFalse(viewModel.IsShelfVisible);
    }

    [TestMethod]
    public void TogglePinCommand_UpdatesPinnedStateAndNotifiesCallback()
    {
        bool? notifiedState = null;
        var viewModel = CreateViewModel(pinStateChanged: value => notifiedState = value);

        Assert.IsFalse(viewModel.IsShelfPinned);
        Assert.AreEqual("固定收纳栏", viewModel.PinShelfTooltip);

        viewModel.TogglePinCommand.Execute(null);

        Assert.IsTrue(viewModel.IsShelfPinned);
        Assert.IsTrue(notifiedState);
        Assert.AreEqual("取消固定收纳栏", viewModel.PinShelfTooltip);

        viewModel.TogglePinCommand.Execute(null);

        Assert.IsFalse(viewModel.IsShelfPinned);
        Assert.IsFalse(notifiedState);
    }

    [TestMethod]
    public void OpenSettingsCommand_InvokesConfiguredCallback()
    {
        var openCount = 0;
        var viewModel = new ShelfViewModel(
            () => openCount++,
            fileActionService: new FakeFileActionService(),
            clipboardService: new FakeClipboardService());

        viewModel.OpenSettingsCommand.Execute(null);

        Assert.AreEqual(1, openCount);
    }

    [TestMethod]
    public void RemoveItemCommand_RemovesRecordOnlyAndDoesNotDeleteSourceFile()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "source.txt");
        File.WriteAllText(filePath, "keep me");
        var item = new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "source.txt",
            SourcePath = filePath,
        };
        var viewModel = CreateViewModel(initialItems: [item]);

        viewModel.Items[0].RemoveCommand.Execute(null);

        Assert.IsEmpty(viewModel.Items);
        Assert.IsTrue(File.Exists(filePath));
        Assert.AreEqual("keep me", File.ReadAllText(filePath));
    }

    [TestMethod]
    public void ClearAll_RemovesRecordsOnlyAndDoesNotDeleteSourceFiles()
    {
        using var tempDirectory = new TempDirectory();
        var firstPath = Path.Combine(tempDirectory.Path, "first.txt");
        var secondPath = Path.Combine(tempDirectory.Path, "second.txt");
        File.WriteAllText(firstPath, "first");
        File.WriteAllText(secondPath, "second");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "first.txt", SourcePath = firstPath },
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "second.txt", SourcePath = secondPath },
        ]);

        viewModel.ClearAllCommand.Execute(null);

        Assert.IsEmpty(viewModel.Items);
        Assert.IsTrue(viewModel.IsEmpty);
        Assert.IsFalse(viewModel.HasItems);
        Assert.IsTrue(File.Exists(firstPath));
        Assert.IsTrue(File.Exists(secondPath));
    }

    [TestMethod]
    public void ClearAll_DoesNotRemoveItemsWhenConfirmationIsRejected()
    {
        var confirmedItemCount = 0;
        var item = new ShelfItem { Type = ShelfItemType.Text, DisplayName = "Note", Content = "keep" };
        var viewModel = CreateViewModel(
            initialItems: [item],
            confirmClearAll: count =>
            {
                confirmedItemCount = count;
                return false;
            });

        viewModel.ClearAllCommand.Execute(null);

        Assert.HasCount(1, viewModel.Items);
        Assert.AreEqual(1, confirmedItemCount);
        Assert.IsFalse(viewModel.IsEmpty);
    }

    [TestMethod]
    public void RemoveItemCommand_DeletesAppOwnedImageFiles()
    {
        using var tempDirectory = new TempDirectory();
        var imageStore = new ImageStore(tempDirectory.Path);
        var imagePath = Path.Combine(imageStore.OriginalsDirectory, "image.png");
        var thumbnailPath = Path.Combine(imageStore.ThumbnailsDirectory, "thumb.png");
        Directory.CreateDirectory(imageStore.OriginalsDirectory);
        Directory.CreateDirectory(imageStore.ThumbnailsDirectory);
        File.WriteAllText(imagePath, "image");
        File.WriteAllText(thumbnailPath, "thumb");
        var item = new ShelfItem
        {
            Type = ShelfItemType.Image,
            DisplayName = "image",
            ImagePath = imagePath,
            ThumbnailPath = thumbnailPath,
        };
        var viewModel = CreateViewModel(initialItems: [item], imageStore: imageStore);

        viewModel.Items[0].RemoveCommand.Execute(null);

        Assert.IsEmpty(viewModel.Items);
        Assert.IsFalse(File.Exists(imagePath));
        Assert.IsFalse(File.Exists(thumbnailPath));
    }

    [TestMethod]
    public void ClearAll_DeletesAppOwnedImageFiles()
    {
        using var tempDirectory = new TempDirectory();
        var imageStore = new ImageStore(tempDirectory.Path);
        var imagePath = Path.Combine(imageStore.OriginalsDirectory, "image.png");
        var thumbnailPath = Path.Combine(imageStore.ThumbnailsDirectory, "thumb.png");
        Directory.CreateDirectory(imageStore.OriginalsDirectory);
        Directory.CreateDirectory(imageStore.ThumbnailsDirectory);
        File.WriteAllText(imagePath, "image");
        File.WriteAllText(thumbnailPath, "thumb");
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem
                {
                    Type = ShelfItemType.Image,
                    DisplayName = "image",
                    ImagePath = imagePath,
                    ThumbnailPath = thumbnailPath,
                },
            ],
            imageStore: imageStore);

        viewModel.ClearAllCommand.Execute(null);

        Assert.IsEmpty(viewModel.Items);
        Assert.IsFalse(File.Exists(imagePath));
        Assert.IsFalse(File.Exists(thumbnailPath));
    }

    [TestMethod]
    public void ShelfItemViewModel_ReturnsMissingStateWhenSourcePathDoesNotExist()
    {
        var item = new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "missing.txt",
            SourcePath = @"C:\Temp\missing.txt",
        };
        var viewModel = CreateViewModel(initialItems: [item]);
        var itemViewModel = viewModel.Items[0];

        Assert.IsTrue(itemViewModel.IsMissing);
        Assert.IsFalse(itemViewModel.OpenCommand.CanExecute(null));
        Assert.IsFalse(itemViewModel.RevealCommand.CanExecute(null));
        Assert.IsTrue(itemViewModel.RemoveCommand.CanExecute(null));
    }

    [TestMethod]
    public void CopyPathCommand_CopiesOriginalSourcePath()
    {
        var clipboard = new FakeClipboardService();
        var item = new ShelfItem
        {
            Type = ShelfItemType.Folder,
            DisplayName = "Downloads",
            SourcePath = @"C:\Users\me\Downloads",
        };
        var viewModel = CreateViewModel(initialItems: [item], clipboardService: clipboard);

        viewModel.Items[0].CopyPathCommand.Execute(null);

        Assert.AreEqual(item.SourcePath, clipboard.Text);
        Assert.AreEqual("路径已复制。", viewModel.Items[0].StatusMessage);
    }

    [TestMethod]
    public void LanguageChange_UpdatesShelfAndExistingItemText()
    {
        var localizationService = new LocalizationService();
        var createdAt = new DateTimeOffset(2026, 7, 5, 9, 30, 0, TimeSpan.Zero);
        var createdAtText = createdAt.LocalDateTime.ToString("MM-dd HH:mm");
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem
                {
                    Type = ShelfItemType.Text,
                    DisplayName = "Note",
                    Content = "hello",
                    CreatedAt = createdAt,
                },
            ],
            localizationService: localizationService);

        Assert.AreEqual("暂存架为空", viewModel.EmptyTitle);
        Assert.AreEqual($"文本 · 5 字符 · {createdAtText}", viewModel.Items[0].MetadataText);

        localizationService.SetLanguage(LanguageMode.English);

        Assert.AreEqual("Shelf is empty", viewModel.EmptyTitle);
        Assert.AreEqual($"Text · 5 chars · {createdAtText}", viewModel.Items[0].MetadataText);
        Assert.AreEqual("Copy", viewModel.Items[0].ContextCopyText);
    }

    [TestMethod]
    public void SetStatusMessage_UpdatesCardStatus()
    {
        var item = new ShelfItem
        {
            Type = ShelfItemType.File,
            DisplayName = "large.bin",
            SourcePath = @"C:\Temp\large.bin",
        };
        var viewModel = CreateViewModel(initialItems: [item]);

        viewModel.Items[0].SetStatusMessage("Item is too large to drag out.");

        Assert.AreEqual("Item is too large to drag out.", viewModel.Items[0].StatusMessage);
    }

    [TestMethod]
    public void ShelfItemViewModel_UsesSourcePathAsPreviewForCommonImageFiles()
    {
        using var tempDirectory = new TempDirectory();
        var imagePath = Path.Combine(tempDirectory.Path, "photo.webp");
        File.WriteAllText(imagePath, "preview path only");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.File,
                DisplayName = "photo.webp",
                SourcePath = imagePath,
            },
        ]);

        Assert.IsTrue(viewModel.Items[0].HasImagePreview);
        Assert.AreEqual(imagePath, viewModel.Items[0].ImagePreviewPath);
    }

    [TestMethod]
    public void ShelfItemViewModel_DoesNotUseSourcePathAsPreviewForNonImageFiles()
    {
        using var tempDirectory = new TempDirectory();
        var textPath = Path.Combine(tempDirectory.Path, "notes.txt");
        File.WriteAllText(textPath, "plain text");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.File,
                DisplayName = "notes.txt",
                SourcePath = textPath,
            },
        ]);

        Assert.IsFalse(viewModel.Items[0].HasImagePreview);
        Assert.IsNull(viewModel.Items[0].ImagePreviewPath);
    }

    [TestMethod]
    public void ShelfItemViewModel_PrefersThumbnailForAppOwnedImageItem()
    {
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.Image,
                DisplayName = "image",
                ImagePath = @"C:\Images\original.png",
                ThumbnailPath = @"C:\Images\thumb.png",
            },
        ]);

        Assert.IsTrue(viewModel.Items[0].HasImagePreview);
        Assert.AreEqual(@"C:\Images\thumb.png", viewModel.Items[0].ImagePreviewPath);
    }

    [TestMethod]
    public void ShelfItemViewModel_ExpandsTextItemsOnlyAfterToggle()
    {
        const string content = "full note content";
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.Text,
                DisplayName = "Content",
                Content = content,
            },
        ]);

        Assert.IsTrue(viewModel.Items[0].IsTextContentItem);
        Assert.IsTrue(viewModel.Items[0].HasExpandedContent);
        Assert.AreEqual(content, viewModel.Items[0].ExpandedContent);
        Assert.IsFalse(viewModel.Items[0].IsExpanded);
        Assert.IsFalse(viewModel.Items[0].IsExpandedContentVisible);
        Assert.IsNull(viewModel.Items[0].PreviewTextTooltip);

        viewModel.Items[0].ToggleExpanded();

        Assert.IsTrue(viewModel.Items[0].IsExpanded);
        Assert.IsTrue(viewModel.Items[0].IsExpandedContentVisible);
        Assert.IsNull(viewModel.Items[0].PreviewTextTooltip);

        viewModel.Items[0].ToggleExpanded();

        Assert.IsFalse(viewModel.Items[0].IsExpanded);
        Assert.IsFalse(viewModel.Items[0].IsExpandedContentVisible);
        Assert.IsNull(viewModel.Items[0].PreviewTextTooltip);
    }

    [TestMethod]
    public void ShelfItemViewModel_ReorderingStateCanBeToggled()
    {
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.Text,
                DisplayName = "Content",
                Content = "content",
            },
        ]);

        viewModel.Items[0].IsReordering = true;

        Assert.IsTrue(viewModel.Items[0].IsReordering);

        viewModel.Items[0].IsReordering = false;

        Assert.IsFalse(viewModel.Items[0].IsReordering);
    }

    [TestMethod]
    public void ShelfItemViewModel_DoesNotExpandUrlItems()
    {
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.Url,
                DisplayName = "Example",
                Content = "https://example.com/path?q=1",
            },
        ]);

        Assert.IsFalse(viewModel.Items[0].IsTextContentItem);
        Assert.IsFalse(viewModel.Items[0].HasExpandedContent);
        Assert.IsNull(viewModel.Items[0].ExpandedContent);

        viewModel.Items[0].ToggleExpanded();

        Assert.IsFalse(viewModel.Items[0].IsExpanded);
        Assert.IsFalse(viewModel.Items[0].IsExpandedContentVisible);
    }

    [TestMethod]
    public void ShelfItemViewModel_MetadataShowsFileSizeAndCreatedTime()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "report.txt");
        File.WriteAllBytes(filePath, new byte[1536]);
        var createdAt = new DateTimeOffset(2026, 7, 5, 9, 30, 0, TimeSpan.Zero);
        var createdAtText = createdAt.LocalDateTime.ToString("MM-dd HH:mm");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.File,
                DisplayName = "report.txt",
                SourcePath = filePath,
                CreatedAt = createdAt,
            },
        ]);

        Assert.AreEqual($"文件 · 1.5 KB · {createdAtText}", viewModel.Items[0].MetadataText);
    }

    [TestMethod]
    public void ShelfItemViewModel_MetadataShowsMissingStateForMissingFile()
    {
        var createdAt = new DateTimeOffset(2026, 7, 5, 9, 30, 0, TimeSpan.Zero);
        var createdAtText = createdAt.LocalDateTime.ToString("MM-dd HH:mm");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.File,
                DisplayName = "missing.txt",
                SourcePath = @"C:\Temp\missing.txt",
                CreatedAt = createdAt,
            },
        ]);

        Assert.AreEqual($"文件 · 已缺失 · {createdAtText}", viewModel.Items[0].MetadataText);
    }

    [TestMethod]
    public void ShelfItemViewModel_MetadataShowsTextLength()
    {
        var createdAt = new DateTimeOffset(2026, 7, 5, 9, 30, 0, TimeSpan.Zero);
        var createdAtText = createdAt.LocalDateTime.ToString("MM-dd HH:mm");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.Text,
                DisplayName = "Note",
                Content = "hello",
                CreatedAt = createdAt,
            },
        ]);

        Assert.AreEqual($"文本 · 5 字符 · {createdAtText}", viewModel.Items[0].MetadataText);
    }

    [TestMethod]
    public void ShelfItemViewModel_MetadataShowsUrlHost()
    {
        var createdAt = new DateTimeOffset(2026, 7, 5, 9, 30, 0, TimeSpan.Zero);
        var createdAtText = createdAt.LocalDateTime.ToString("MM-dd HH:mm");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = ShelfItemType.Url,
                DisplayName = "Example",
                Content = "https://example.com/path",
                CreatedAt = createdAt,
            },
        ]);

        Assert.AreEqual($"链接 · example.com · {createdAtText}", viewModel.Items[0].MetadataText);
    }

    [TestMethod]
    [DataRow(ShelfItemType.Url)]
    [DataRow(ShelfItemType.File)]
    [DataRow(ShelfItemType.Folder)]
    [DataRow(ShelfItemType.Image)]
    public void ShelfItemViewModel_DoesNotExpandNonTextContentItems(ShelfItemType type)
    {
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem
            {
                Type = type,
                DisplayName = "Not text",
                Content = "content should stay compact",
                SourcePath = @"C:\Temp\source.txt",
                ImagePath = @"C:\Images\image.png",
            },
        ]);

        Assert.IsFalse(viewModel.Items[0].IsTextContentItem);
        Assert.IsFalse(viewModel.Items[0].HasExpandedContent);
        Assert.IsNull(viewModel.Items[0].ExpandedContent);
    }

    [TestMethod]
    public void Constructor_SelectsFirstInitialItem()
    {
        var first = new ShelfItem { DisplayName = "First", Type = ShelfItemType.Text, Content = "first" };
        var second = new ShelfItem { DisplayName = "Second", Type = ShelfItemType.Text, Content = "second" };

        var viewModel = CreateViewModel(initialItems: [first, second]);

        Assert.AreSame(viewModel.Items[0], viewModel.SelectedItem);
    }

    [TestMethod]
    public void RemoveSelectedCommand_SelectsNextItemAfterRemoval()
    {
        var first = new ShelfItem { DisplayName = "First", Type = ShelfItemType.Text, Content = "first" };
        var second = new ShelfItem { DisplayName = "Second", Type = ShelfItemType.Text, Content = "second" };
        var viewModel = CreateViewModel(initialItems: [first, second]);

        viewModel.RemoveSelectedCommand.Execute(null);

        Assert.HasCount(1, viewModel.Items);
        Assert.AreSame(second, viewModel.SelectedItem?.Item);
    }

    [TestMethod]
    public void RemoveSelectedCommand_SelectsPreviousItemWhenLastItemIsRemoved()
    {
        var first = new ShelfItem { DisplayName = "First", Type = ShelfItemType.Text, Content = "first" };
        var second = new ShelfItem { DisplayName = "Second", Type = ShelfItemType.Text, Content = "second" };
        var viewModel = CreateViewModel(initialItems: [first, second]);
        viewModel.SelectedItem = viewModel.Items[1];

        viewModel.RemoveSelectedCommand.Execute(null);

        Assert.HasCount(1, viewModel.Items);
        Assert.AreSame(first, viewModel.SelectedItem?.Item);
    }

    [TestMethod]
    public void CopySelectedCommand_CopiesTextContent()
    {
        var clipboard = new FakeClipboardService();
        var item = new ShelfItem
        {
            Type = ShelfItemType.Text,
            DisplayName = "Note",
            Content = "full note content",
        };
        var viewModel = CreateViewModel(initialItems: [item], clipboardService: clipboard);

        viewModel.CopySelectedCommand.Execute(null);

        Assert.AreEqual("full note content", clipboard.Text);
        Assert.AreEqual("已复制。", viewModel.SelectedItem?.StatusMessage);
    }

    [TestMethod]
    public void OpenSelectedCommand_OpensSelectedUrl()
    {
        var fileActions = new FakeFileActionService();
        var item = new ShelfItem
        {
            Type = ShelfItemType.Url,
            DisplayName = "example.com",
            Content = "https://example.com/path",
        };
        var viewModel = CreateViewModel(initialItems: [item], fileActionService: fileActions);

        Assert.IsTrue(viewModel.OpenSelectedCommand.CanExecute(null));
        viewModel.OpenSelectedCommand.Execute(null);

        Assert.AreEqual("https://example.com/path", fileActions.OpenedUrl);
    }

    [TestMethod]
    public void SelectedCommands_AreDisabledWithoutSelectedItem()
    {
        var viewModel = CreateViewModel();

        Assert.IsFalse(viewModel.RemoveSelectedCommand.CanExecute(null));
        Assert.IsFalse(viewModel.CopySelectedCommand.CanExecute(null));
        Assert.IsFalse(viewModel.OpenSelectedCommand.CanExecute(null));
    }

    [TestMethod]
    [DataRow(ShelfFilterMode.File, ShelfItemType.File, "文件")]
    [DataRow(ShelfFilterMode.Folder, ShelfItemType.Folder, "文件夹")]
    [DataRow(ShelfFilterMode.Text, ShelfItemType.Text, "文本")]
    [DataRow(ShelfFilterMode.Url, ShelfItemType.Url, "链接")]
    [DataRow(ShelfFilterMode.Image, ShelfItemType.Image, "图片")]
    public void ActiveFilter_ShowsOnlyMatchingType(ShelfFilterMode filterMode, ShelfItemType expectedType, string expectedFilterName)
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);

        viewModel.ActiveFilter = filterMode;

        Assert.HasCount(1, viewModel.VisibleItems);
        Assert.AreEqual(expectedType, viewModel.VisibleItems[0].Type);
        Assert.AreEqual(expectedFilterName, viewModel.FilterModeOptions.Single(option => option.Value == filterMode).DisplayName);
        CollectionAssert.AreEqual(items, viewModel.GetShelfItems().ToArray());
    }

    [TestMethod]
    public void ActiveFilter_AllRestoresManualOrder()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);

        viewModel.ActiveFilter = ShelfFilterMode.Text;
        viewModel.ActiveFilter = ShelfFilterMode.All;

        CollectionAssert.AreEqual(items, viewModel.VisibleItems.Select(item => item.Item).ToArray());
        CollectionAssert.AreEqual(items, viewModel.GetShelfItems().ToArray());
    }

    [TestMethod]
    public void ActiveFilter_SelectsFirstVisibleItemWhenSelectedItemIsHidden()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);
        viewModel.SelectedItem = viewModel.Items[0];

        viewModel.ActiveFilter = ShelfFilterMode.Text;

        Assert.AreSame(items[2], viewModel.SelectedItem?.Item);
    }

    [TestMethod]
    public void ActiveFilter_ReportsNoResultsWhenNoItemsMatch()
    {
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem { Type = ShelfItemType.Text, DisplayName = "Note", Content = "note" },
        ]);

        viewModel.ActiveFilter = ShelfFilterMode.Image;

        Assert.IsTrue(viewModel.HasItems);
        Assert.IsFalse(viewModel.HasVisibleItems);
        Assert.IsTrue(viewModel.IsNoResults);
        Assert.AreEqual("没有匹配项", viewModel.NoResultsTitle);
    }

    [TestMethod]
    public void MoveItem_ReordersBackingItemsAndPreservesSelection()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);

        viewModel.MoveItem(viewModel.Items[2], 0);

        CollectionAssert.AreEqual(new[] { items[2], items[0], items[1], items[3], items[4] }, viewModel.GetShelfItems().ToArray());
        CollectionAssert.AreEqual(viewModel.GetShelfItems().ToArray(), viewModel.VisibleItems.Select(item => item.Item).ToArray());
        Assert.AreSame(items[2], viewModel.SelectedItem?.Item);
    }

    [TestMethod]
    public void MoveItem_DoesNothingWhenFilterIsActive()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);
        viewModel.ActiveFilter = ShelfFilterMode.Text;

        viewModel.MoveItem(viewModel.VisibleItems[0], 0);

        CollectionAssert.AreEqual(items, viewModel.GetShelfItems().ToArray());
    }

    [TestMethod]
    public void MoveSelectedItem_MovesSelectedItemByVisibleOffset()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);
        viewModel.SelectedItem = viewModel.Items[1];

        var moved = viewModel.MoveSelectedItem(1);

        Assert.IsTrue(moved);
        CollectionAssert.AreEqual(new[] { items[0], items[2], items[1], items[3], items[4] }, viewModel.GetShelfItems().ToArray());
        Assert.AreSame(items[1], viewModel.SelectedItem?.Item);
    }

    [TestMethod]
    public void MoveSelectedItem_ClampsAtListBoundary()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);
        viewModel.SelectedItem = viewModel.Items[0];

        var moved = viewModel.MoveSelectedItem(-1);

        Assert.IsFalse(moved);
        CollectionAssert.AreEqual(items, viewModel.GetShelfItems().ToArray());
    }

    [TestMethod]
    public void MoveSelectedItem_DoesNothingWhenFilterIsActive()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);
        viewModel.SelectedItem = viewModel.Items[2];
        viewModel.ActiveFilter = ShelfFilterMode.Text;

        var moved = viewModel.MoveSelectedItem(1);

        Assert.IsFalse(moved);
        CollectionAssert.AreEqual(items, viewModel.GetShelfItems().ToArray());
    }

    [TestMethod]
    public void LanguageChange_UpdatesFilterAndCleanupText()
    {
        var localizationService = new LocalizationService();
        var viewModel = CreateViewModel(localizationService: localizationService);

        Assert.AreEqual("筛选", viewModel.FilterLabel);
        Assert.AreEqual("全部", viewModel.FilterModeOptions.Single(option => option.Value == ShelfFilterMode.All).DisplayName);

        localizationService.SetLanguage(LanguageMode.English);

        Assert.AreEqual("Filter", viewModel.FilterLabel);
        Assert.AreEqual("All", viewModel.FilterModeOptions.Single(option => option.Value == ShelfFilterMode.All).DisplayName);
    }

    private static ShelfViewModel CreateViewModel(
        IEnumerable<ShelfItem>? initialItems = null,
        IFileActionService? fileActionService = null,
        IClipboardService? clipboardService = null,
        ImageStore? imageStore = null,
        Func<int, bool>? confirmClearAll = null,
        LocalizationService? localizationService = null,
        bool isShelfPinned = false,
        Action<bool>? pinStateChanged = null)
    {
        return new ShelfViewModel(
            initialItems: initialItems,
            fileActionService: fileActionService ?? new FakeFileActionService(),
            clipboardService: clipboardService ?? new FakeClipboardService(),
            imageStore: imageStore,
            confirmClearAll: confirmClearAll,
            localizationService: localizationService,
            isShelfPinned: isShelfPinned,
            pinStateChanged: pinStateChanged);
    }

    private static IEnumerable<ShelfItem> CreateMixedItems()
    {
        return
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "File", SourcePath = @"C:\Temp\file.txt" },
            new ShelfItem { Type = ShelfItemType.Folder, DisplayName = "Folder", SourcePath = @"C:\Temp" },
            new ShelfItem { Type = ShelfItemType.Text, DisplayName = "Text", Content = "note" },
            new ShelfItem { Type = ShelfItemType.Url, DisplayName = "Link", Content = "https://example.com" },
            new ShelfItem { Type = ShelfItemType.Image, DisplayName = "Image", ImagePath = @"C:\Temp\image.png" },
        ];
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string? Text { get; private set; }

        public bool SetText(string text)
        {
            Text = text;
            return true;
        }

        public bool SetImageFromPath(string path)
        {
            Text = path;
            return File.Exists(path);
        }
    }

    private sealed class FakeFileActionService : IFileActionService
    {
        public string? OpenedUrl { get; private set; }

        public bool PathExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public bool Open(string path)
        {
            return PathExists(path);
        }

        public bool OpenUrl(string url)
        {
            var opened = Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            if (opened)
            {
                OpenedUrl = url;
            }

            return opened;
        }

        public bool RevealInExplorer(string path)
        {
            return PathExists(path);
        }
    }
}
