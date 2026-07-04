using DropShelf.App.ViewModels;

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
