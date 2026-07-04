using DropShelf.App.Services;

namespace DropShelf.Tests;

[TestClass]
public sealed class StartupServiceTests
{
    [TestMethod]
    public void SetEnabled_WritesQuotedExecutablePath()
    {
        var registry = new FakeStartupRegistry();
        var service = new StartupService(registry, "DropShelf", @"C:\Apps\DropShelf.exe");

        service.SetEnabled(true);

        Assert.AreEqual("\"C:\\Apps\\DropShelf.exe\"", registry.Values["DropShelf"]);
        Assert.IsTrue(service.IsEnabled());
    }

    [TestMethod]
    public void SetEnabled_RemovesStartupEntry()
    {
        var registry = new FakeStartupRegistry();
        registry.Values["DropShelf"] = "\"C:\\Apps\\DropShelf.exe\"";
        var service = new StartupService(registry, "DropShelf", @"C:\Apps\DropShelf.exe");

        service.SetEnabled(false);

        Assert.IsFalse(registry.Values.ContainsKey("DropShelf"));
        Assert.IsFalse(service.IsEnabled());
    }

    [TestMethod]
    public void IsEnabled_ReturnsFalseWhenExecutablePathChanged()
    {
        var registry = new FakeStartupRegistry();
        registry.Values["DropShelf"] = "\"C:\\Old\\DropShelf.exe\"";
        var service = new StartupService(registry, "DropShelf", @"C:\New\DropShelf.exe");

        Assert.IsFalse(service.IsEnabled());
    }

    private sealed class FakeStartupRegistry : IStartupRegistry
    {
        public Dictionary<string, string> Values { get; } = [];

        public string? GetValue(string name)
        {
            return Values.TryGetValue(name, out var value) ? value : null;
        }

        public void SetValue(string name, string value)
        {
            Values[name] = value;
        }

        public void DeleteValue(string name)
        {
            Values.Remove(name);
        }
    }
}
