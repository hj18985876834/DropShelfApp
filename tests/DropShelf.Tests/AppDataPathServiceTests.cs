using System.IO;
using DropShelf.App.Services;

namespace DropShelf.Tests;

[TestClass]
public sealed class AppDataPathServiceTests
{
    [TestMethod]
    public void GetAppDataRoot_ComposesDropShelfDirectoryUnderLocalAppData()
    {
        var localAppDataRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new AppDataPathService(localAppDataRoot);

        var appDataRoot = service.GetAppDataRoot();

        Assert.AreEqual(Path.Combine(localAppDataRoot, "DropShelf"), appDataRoot);
    }
}
