using DropShelf.App.Models;

namespace DropShelf.Tests;

[TestClass]
public sealed class AppSettingsTests
{
    [TestMethod]
    public void CreateDefault_UsesExpectedMvpDefaults()
    {
        var settings = AppSettings.CreateDefault();

        Assert.AreEqual(DockEdge.Right, settings.DockEdge);
        Assert.AreEqual(ThemeMode.System, settings.ThemeMode);
        Assert.AreEqual(DensityMode.Compact, settings.DensityMode);
        Assert.IsFalse(settings.StartWithWindows);
    }
}
