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
    public void RemoveItemCommand_KeepsExternalImageFile()
    {
        using var tempDirectory = new TempDirectory();
        var imageStore = new ImageStore(Path.Combine(tempDirectory.Path, "app-data"));
        var imagePath = Path.Combine(tempDirectory.Path, "external.png");
        File.WriteAllText(imagePath, "image");
        var item = new ShelfItem
        {
            Type = ShelfItemType.Image,
            DisplayName = "external.png",
            SourcePath = imagePath,
            ImagePath = imagePath,
        };
        var viewModel = CreateViewModel(initialItems: [item], imageStore: imageStore);

        viewModel.Items[0].RemoveCommand.Execute(null);

        Assert.IsEmpty(viewModel.Items);
        Assert.IsTrue(File.Exists(imagePath));
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
    public void SearchQuery_FiltersByDisplayNameContentAndPathWithoutChangingBackingItems()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);

        viewModel.SearchQuery = "example";

        Assert.HasCount(1, viewModel.VisibleItems);
        Assert.AreSame(items[3], viewModel.VisibleItems[0].Item);
        CollectionAssert.AreEqual(items, viewModel.GetShelfItems().ToArray());

        viewModel.SearchQuery = @"c:\temp";

        Assert.HasCount(3, viewModel.VisibleItems);
        CollectionAssert.AreEqual(
            new[] { items[0], items[1], items[4] },
            viewModel.VisibleItems.Select(item => item.Item).ToArray());
        CollectionAssert.AreEqual(items, viewModel.GetShelfItems().ToArray());
    }

    [TestMethod]
    public void SearchQuery_StacksWithActiveFilter()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);

        viewModel.ActiveFilter = ShelfFilterMode.Text;
        viewModel.SearchQuery = "note";

        Assert.HasCount(1, viewModel.VisibleItems);
        Assert.AreSame(items[2], viewModel.VisibleItems[0].Item);

        viewModel.SearchQuery = "example";

        Assert.IsFalse(viewModel.HasVisibleItems);
        Assert.IsTrue(viewModel.IsNoResults);
        Assert.IsTrue(viewModel.IsSearchActive);
    }

    [TestMethod]
    public void SearchQuery_SelectsFirstVisibleItemWhenSelectedItemIsHidden()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);
        viewModel.SelectedItem = viewModel.Items[0];

        viewModel.SearchQuery = "note";

        Assert.AreSame(items[2], viewModel.SelectedItem?.Item);
    }

    [TestMethod]
    public void SearchQuery_ClearRestoresVisibleItemsInManualOrder()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items);

        viewModel.SearchQuery = "note";
        viewModel.SearchQuery = string.Empty;

        CollectionAssert.AreEqual(items, viewModel.VisibleItems.Select(item => item.Item).ToArray());
    }

    [TestMethod]
    public void FilterModeOptions_ShowOnlyTypeFilters()
    {
        var viewModel = CreateViewModel();

        CollectionAssert.AreEqual(
            new[]
            {
                ShelfFilterMode.All,
                ShelfFilterMode.File,
                ShelfFilterMode.Folder,
                ShelfFilterMode.Text,
                ShelfFilterMode.Url,
                ShelfFilterMode.Image,
            },
            viewModel.FilterModeOptions.Select(option => option.Value).ToArray());
    }

    [TestMethod]
    public void SelectOnly_SelectsSingleVisibleItemAndClearsPreviousBatchSelection()
    {
        var viewModel = CreateViewModel(initialItems: CreateMixedItems());

        viewModel.SelectAllVisible();
        viewModel.SelectOnly(viewModel.VisibleItems[2]);

        Assert.HasCount(1, viewModel.SelectedItems);
        Assert.AreSame(viewModel.VisibleItems[2], viewModel.SelectedItem);
        Assert.IsTrue(viewModel.VisibleItems[2].IsBatchSelected);
        Assert.IsFalse(viewModel.VisibleItems[0].IsBatchSelected);
    }

    [TestMethod]
    public void ToggleSelection_TogglesVisibleItemAndKeepsPrimarySelection()
    {
        var viewModel = CreateViewModel(initialItems: CreateMixedItems());

        viewModel.SelectOnly(viewModel.VisibleItems[0]);
        viewModel.ToggleSelection(viewModel.VisibleItems[2]);

        Assert.HasCount(2, viewModel.SelectedItems);
        Assert.AreSame(viewModel.VisibleItems[2], viewModel.SelectedItem);
        Assert.IsTrue(viewModel.VisibleItems[0].IsBatchSelected);
        Assert.IsTrue(viewModel.VisibleItems[2].IsBatchSelected);

        viewModel.ToggleSelection(viewModel.VisibleItems[2]);

        Assert.HasCount(1, viewModel.SelectedItems);
        Assert.AreSame(viewModel.VisibleItems[0], viewModel.SelectedItem);
        Assert.IsFalse(viewModel.VisibleItems[2].IsBatchSelected);
    }

    [TestMethod]
    public void SelectRangeTo_SelectsVisibleRangeFromPrimarySelection()
    {
        var viewModel = CreateViewModel(initialItems: CreateMixedItems());

        viewModel.SelectOnly(viewModel.VisibleItems[1]);
        viewModel.SelectRangeTo(viewModel.VisibleItems[3]);

        CollectionAssert.AreEqual(
            new[] { viewModel.VisibleItems[1], viewModel.VisibleItems[2], viewModel.VisibleItems[3] },
            viewModel.SelectedItems.ToArray());
        Assert.AreSame(viewModel.VisibleItems[3], viewModel.SelectedItem);
    }

    [TestMethod]
    public void SelectRangeTo_UsesProvidedAnchorWhenPrimarySelectionAlreadyMoved()
    {
        var viewModel = CreateViewModel(initialItems: CreateMixedItems());
        var anchor = viewModel.VisibleItems[1];
        viewModel.SelectedItem = viewModel.VisibleItems[3];

        viewModel.SelectRangeTo(viewModel.VisibleItems[3], anchor);

        CollectionAssert.AreEqual(
            new[] { viewModel.VisibleItems[1], viewModel.VisibleItems[2], viewModel.VisibleItems[3] },
            viewModel.SelectedItems.ToArray());
    }

    [TestMethod]
    public void SelectAllVisible_SelectsOnlyFilteredSearchResults()
    {
        var viewModel = CreateViewModel(initialItems: CreateMixedItems());

        viewModel.SearchQuery = @"c:\temp";
        viewModel.SelectAllVisible();

        Assert.HasCount(3, viewModel.SelectedItems);
        CollectionAssert.AreEqual(viewModel.VisibleItems.ToArray(), viewModel.SelectedItems.ToArray());
    }

    [TestMethod]
    public void SearchQuery_RemovesHiddenItemsFromBatchSelection()
    {
        var viewModel = CreateViewModel(initialItems: CreateMixedItems());

        viewModel.SelectAllVisible();
        viewModel.SearchQuery = "note";

        Assert.HasCount(1, viewModel.SelectedItems);
        Assert.AreSame(viewModel.VisibleItems[0], viewModel.SelectedItems[0]);
        Assert.AreSame(viewModel.VisibleItems[0], viewModel.SelectedItem);
    }

    [TestMethod]
    public void RemoveSelectedCommand_ConfirmsBeforeRemovingMultipleRecords()
    {
        var confirmationCount = 0;
        var viewModel = CreateViewModel(
            initialItems: CreateMixedItems(),
            confirmRemoveSelected: count =>
            {
                confirmationCount = count;
                return false;
            });

        viewModel.SelectRangeTo(viewModel.VisibleItems[2]);
        viewModel.RemoveSelectedCommand.Execute(null);

        Assert.AreEqual(3, confirmationCount);
        Assert.HasCount(5, viewModel.Items);
    }

    [TestMethod]
    public void RemoveSelectedCommand_RemovesConfirmedSelectedRecordsOnly()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items, confirmRemoveSelected: _ => true);

        viewModel.SelectOnly(viewModel.VisibleItems[1]);
        viewModel.ToggleSelection(viewModel.VisibleItems[3]);
        viewModel.RemoveSelectedCommand.Execute(null);

        CollectionAssert.AreEqual(
            new[] { items[0], items[2], items[4] },
            viewModel.GetShelfItems().ToArray());
        Assert.HasCount(1, viewModel.SelectedItems);
    }

    [TestMethod]
    public void CardRemoveCommand_RemovesBatchSelectionWhenItemIsSelectedInBatch()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items, confirmRemoveSelected: _ => true);

        viewModel.SelectOnly(viewModel.VisibleItems[1]);
        viewModel.ToggleSelection(viewModel.VisibleItems[3]);
        viewModel.VisibleItems[1].RemoveCommand.Execute(null);

        CollectionAssert.AreEqual(
            new[] { items[0], items[2], items[4] },
            viewModel.GetShelfItems().ToArray());
    }

    [TestMethod]
    public void CardRemoveCommand_RemovesOnlyClickedItemWhenItemIsNotInBatchSelection()
    {
        var items = CreateMixedItems().ToArray();
        var viewModel = CreateViewModel(initialItems: items, confirmRemoveSelected: _ => true);

        viewModel.SelectOnly(viewModel.VisibleItems[1]);
        viewModel.ToggleSelection(viewModel.VisibleItems[3]);
        viewModel.VisibleItems[0].RemoveCommand.Execute(null);

        CollectionAssert.AreEqual(
            new[] { items[1], items[2], items[3], items[4] },
            viewModel.GetShelfItems().ToArray());
    }

    [TestMethod]
    public void CopySelectedCommand_RejectsMixedTypeBatch()
    {
        var clipboard = new FakeClipboardService();
        var viewModel = CreateViewModel(initialItems: CreateMixedItems(), clipboardService: clipboard);

        viewModel.SelectOnly(viewModel.VisibleItems[0]);
        viewModel.ToggleSelection(viewModel.VisibleItems[2]);
        viewModel.CopySelectedCommand.Execute(null);

        Assert.AreEqual("请选择同一类型的暂存项后再复制。", viewModel.ShelfStatusMessage);
        Assert.IsNull(clipboard.Text);
        Assert.IsEmpty(clipboard.FileDropList);
    }

    [TestMethod]
    public void CopySelectedCommand_CopiesSameTypeTextAsNewlineSeparatedText()
    {
        var clipboard = new FakeClipboardService();
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem { Type = ShelfItemType.Text, DisplayName = "One", Content = "alpha" },
                new ShelfItem { Type = ShelfItemType.Text, DisplayName = "Two", Content = "beta" },
            ],
            clipboardService: clipboard);

        viewModel.SelectAllVisible();
        viewModel.CopySelectedCommand.Execute(null);

        Assert.AreEqual($"alpha{Environment.NewLine}beta", clipboard.Text);
        Assert.AreEqual("已复制 2 条文本。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void CopySelectedCommand_CopiesSameTypeFilesAsFileDropList()
    {
        var clipboard = new FakeClipboardService();
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem { Type = ShelfItemType.File, DisplayName = "One", SourcePath = @"C:\Temp\one.txt" },
                new ShelfItem { Type = ShelfItemType.File, DisplayName = "Two", SourcePath = @"C:\Temp\two.txt" },
            ],
            clipboardService: clipboard);

        viewModel.SelectAllVisible();
        viewModel.CopySelectedCommand.Execute(null);

        CollectionAssert.AreEqual(
            new[] { @"C:\Temp\one.txt", @"C:\Temp\two.txt" },
            clipboard.FileDropList.ToArray());
        Assert.AreEqual("已复制 2 个文件。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void CopySelectedCommand_CopiesSameTypeFoldersAsFileDropList()
    {
        var clipboard = new FakeClipboardService();
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem { Type = ShelfItemType.Folder, DisplayName = "One", SourcePath = @"C:\Temp\one" },
                new ShelfItem { Type = ShelfItemType.Folder, DisplayName = "Two", SourcePath = @"C:\Temp\two" },
            ],
            clipboardService: clipboard);

        viewModel.SelectAllVisible();
        viewModel.CopySelectedCommand.Execute(null);

        CollectionAssert.AreEqual(
            new[] { @"C:\Temp\one", @"C:\Temp\two" },
            clipboard.FileDropList.ToArray());
        Assert.AreEqual("已复制 2 个文件夹。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void CopySelectedCommand_CopiesSameTypeImagesAsFileDropList()
    {
        var clipboard = new FakeClipboardService();
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem { Type = ShelfItemType.Image, DisplayName = "One", ImagePath = @"C:\Temp\one.png" },
                new ShelfItem { Type = ShelfItemType.Image, DisplayName = "Two", ImagePath = @"C:\Temp\two.png" },
            ],
            clipboardService: clipboard);

        viewModel.SelectAllVisible();
        viewModel.CopySelectedCommand.Execute(null);

        CollectionAssert.AreEqual(
            new[] { @"C:\Temp\one.png", @"C:\Temp\two.png" },
            clipboard.FileDropList.ToArray());
        Assert.AreEqual("已复制 2 张图片。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void CopySelectedCommand_CopiesSameTypeUrlsAsNewlineSeparatedLinks()
    {
        var clipboard = new FakeClipboardService();
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem { Type = ShelfItemType.Url, DisplayName = "One", Content = "https://example.com/one" },
                new ShelfItem { Type = ShelfItemType.Url, DisplayName = "Two", Content = "https://example.com/two" },
            ],
            clipboardService: clipboard);

        viewModel.SelectAllVisible();
        viewModel.CopySelectedCommand.Execute(null);

        Assert.AreEqual($"https://example.com/one{Environment.NewLine}https://example.com/two", clipboard.Text);
        Assert.AreEqual("已复制 2 个链接。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void AddItems_SkipsDuplicateFilePaths()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "duplicate.txt");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "duplicate.txt", SourcePath = filePath },
        ]);

        var result = viewModel.AddItems(
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "duplicate.txt", SourcePath = filePath },
        ]);

        Assert.AreEqual(new ShelfAddResult(0, 1), result);
        Assert.HasCount(1, viewModel.Items);
        Assert.AreEqual("已跳过 1 个重复路径。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void ClearShelfStatusMessage_HidesTransientShelfFeedback()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "duplicate.txt");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "duplicate.txt", SourcePath = filePath },
        ]);
        viewModel.AddItems(
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "duplicate.txt", SourcePath = filePath },
        ]);

        viewModel.ClearShelfStatusMessage();

        Assert.IsNull(viewModel.ShelfStatusMessage);
        Assert.IsFalse(viewModel.HasShelfStatusMessage);
    }

    [TestMethod]
    public void AddItems_SkipsDuplicatePathsWithinSameBatch()
    {
        var folderPath = Path.Combine(Path.GetTempPath(), "duplicate-folder");
        var viewModel = CreateViewModel();

        var result = viewModel.AddItems(
        [
            new ShelfItem { Type = ShelfItemType.Folder, DisplayName = "folder", SourcePath = folderPath },
            new ShelfItem { Type = ShelfItemType.Folder, DisplayName = "folder", SourcePath = folderPath + Path.DirectorySeparatorChar },
        ]);

        Assert.AreEqual(new ShelfAddResult(1, 1), result);
        Assert.HasCount(1, viewModel.Items);
        Assert.AreEqual("已添加 1 个文件夹，跳过 1 个重复路径。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void AddItems_ReportsAddedItemsByType()
    {
        var viewModel = CreateViewModel();

        viewModel.AddItems(
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "report.txt", SourcePath = @"C:\Temp\report.txt" },
            new ShelfItem { Type = ShelfItemType.Folder, DisplayName = "Assets", SourcePath = @"C:\Temp\Assets" },
            new ShelfItem { Type = ShelfItemType.Url, DisplayName = "example.com", Content = "https://example.com" },
        ]);

        Assert.AreEqual("已添加 1 个文件、1 个文件夹、1 个链接。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void Constructor_PreservesAndMarksExistingDuplicateRecords()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "existing-duplicate.txt");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "first", SourcePath = filePath },
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "second", SourcePath = filePath.ToUpperInvariant() },
        ]);

        Assert.HasCount(2, viewModel.Items);
        Assert.IsFalse(viewModel.Items[0].IsDuplicate);
        Assert.IsTrue(viewModel.Items[1].IsDuplicate);
        Assert.AreEqual(1, viewModel.DuplicateItemCount);

        Assert.HasCount(6, viewModel.FilterModeOptions);
    }

    [TestMethod]
    public void InvalidItemCount_TracksInvalidFileSystemItems()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "valid.txt");
        File.WriteAllText(filePath, "valid");
        var folderPath = Path.Combine(tempDirectory.Path, "folder");
        Directory.CreateDirectory(folderPath);
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "valid", SourcePath = filePath },
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "folder-as-file", SourcePath = folderPath },
            new ShelfItem { Type = ShelfItemType.Folder, DisplayName = "missing", SourcePath = Path.Combine(tempDirectory.Path, "missing-folder") },
            new ShelfItem { Type = ShelfItemType.Text, DisplayName = "note", Content = "note" },
        ]);

        Assert.AreEqual(2, viewModel.InvalidItemCount);
        CollectionAssert.AreEqual(
            new[] { "folder-as-file", "missing" },
            viewModel.Items.Where(item => item.IsInvalidRecord).Select(item => item.DisplayName).ToArray());
        Assert.HasCount(6, viewModel.FilterModeOptions);
    }

    [TestMethod]
    public void ClearInvalidCommand_ClearsInvalidItemsOnly()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "valid.txt");
        File.WriteAllText(filePath, "valid");
        var missingPath = Path.Combine(tempDirectory.Path, "missing.txt");
        var viewModel = CreateViewModel(initialItems:
        [
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "valid", SourcePath = filePath },
            new ShelfItem { Type = ShelfItemType.File, DisplayName = "missing", SourcePath = missingPath },
        ]);

        viewModel.ClearInvalidCommand.Execute(null);

        Assert.HasCount(1, viewModel.Items);
        Assert.AreEqual("valid", viewModel.Items[0].DisplayName);
        Assert.IsTrue(File.Exists(filePath));
        Assert.AreEqual("已清理 1 条无效记录。", viewModel.ShelfStatusMessage);
    }

    [TestMethod]
    public void RelinkCommand_UpdatesPathAndPreservesRecordIdentity()
    {
        using var tempDirectory = new TempDirectory();
        var newPath = Path.Combine(tempDirectory.Path, "moved.txt");
        File.WriteAllText(newPath, "moved");
        var itemId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 7, 5, 9, 30, 0, TimeSpan.Zero);
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem
                {
                    Id = itemId,
                    Type = ShelfItemType.File,
                    DisplayName = "old.txt",
                    SourcePath = Path.Combine(tempDirectory.Path, "old.txt"),
                    CreatedAt = createdAt,
                },
            ],
            selectRelinkPath: _ => newPath);

        viewModel.Items[0].RelinkCommand.Execute(null);

        Assert.HasCount(1, viewModel.Items);
        Assert.AreEqual(itemId, viewModel.Items[0].Item.Id);
        Assert.AreEqual(createdAt, viewModel.Items[0].Item.CreatedAt);
        Assert.AreEqual(newPath, viewModel.Items[0].Item.SourcePath);
        Assert.AreEqual("moved.txt", viewModel.Items[0].Item.DisplayName);
        Assert.IsFalse(viewModel.Items[0].IsInvalidRecord);
        Assert.AreEqual("已重新关联。", viewModel.Items[0].StatusMessage);
    }

    [TestMethod]
    public void RelinkCommand_RejectsPathThatWouldDuplicateExistingRecord()
    {
        using var tempDirectory = new TempDirectory();
        var existingPath = Path.Combine(tempDirectory.Path, "existing.txt");
        File.WriteAllText(existingPath, "existing");
        var missingPath = Path.Combine(tempDirectory.Path, "missing.txt");
        var viewModel = CreateViewModel(
            initialItems:
            [
                new ShelfItem { Type = ShelfItemType.File, DisplayName = "existing", SourcePath = existingPath },
                new ShelfItem { Type = ShelfItemType.File, DisplayName = "missing", SourcePath = missingPath },
            ],
            selectRelinkPath: _ => existingPath);

        viewModel.Items[1].RelinkCommand.Execute(null);

        Assert.AreEqual(missingPath, viewModel.Items[1].Item.SourcePath);
        Assert.AreEqual("该路径已在收纳栏中。", viewModel.Items[1].StatusMessage);
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
        Assert.AreEqual("Search cards", viewModel.SearchPlaceholder);
        Assert.AreEqual("All", viewModel.FilterModeOptions.Single(option => option.Value == ShelfFilterMode.All).DisplayName);
    }

    private static ShelfViewModel CreateViewModel(
        IEnumerable<ShelfItem>? initialItems = null,
        IFileActionService? fileActionService = null,
        IClipboardService? clipboardService = null,
        ImageStore? imageStore = null,
        Func<int, bool>? confirmClearAll = null,
        Func<int, bool>? confirmRemoveSelected = null,
        Func<ShelfItem, string?>? selectRelinkPath = null,
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
            confirmRemoveSelected: confirmRemoveSelected,
            selectRelinkPath: selectRelinkPath,
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

        public List<string> FileDropList { get; } = [];

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

        public bool SetFileDropList(IEnumerable<string> paths)
        {
            FileDropList.Clear();
            FileDropList.AddRange(paths);
            return FileDropList.Count > 0;
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
