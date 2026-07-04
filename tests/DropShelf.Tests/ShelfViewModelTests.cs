using DropShelf.App.ViewModels;
using DropShelf.App.Models;

namespace DropShelf.Tests;

[TestClass]
public sealed class ShelfViewModelTests
{
    [TestMethod]
    public void Constructor_StartsWithShelfHidden()
    {
        var viewModel = new ShelfViewModel();

        Assert.IsFalse(viewModel.IsShelfVisible);
    }

    [TestMethod]
    public void Constructor_AddsInitialShelfItemsInOrder()
    {
        var first = new ShelfItem { DisplayName = "First", Type = ShelfItemType.Text };
        var second = new ShelfItem { DisplayName = "Second", Type = ShelfItemType.Url };

        var viewModel = new ShelfViewModel(initialItems: [first, second]);

        Assert.AreEqual(2, viewModel.Items.Count);
        Assert.AreSame(first, viewModel.Items[0]);
        Assert.AreSame(second, viewModel.Items[1]);
    }

    [TestMethod]
    public void ShellCommands_UpdateShelfVisibility()
    {
        var viewModel = new ShelfViewModel();

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
    public void OpenSettingsCommand_InvokesConfiguredCallback()
    {
        var openCount = 0;
        var viewModel = new ShelfViewModel(() => openCount++);

        viewModel.OpenSettingsCommand.Execute(null);

        Assert.AreEqual(1, openCount);
    }
}
