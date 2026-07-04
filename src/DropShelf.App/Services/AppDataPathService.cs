using System.IO;

namespace DropShelf.App.Services;

public sealed class AppDataPathService
{
    private readonly string? _localAppDataRoot;

    public AppDataPathService(string? localAppDataRoot = null)
    {
        _localAppDataRoot = localAppDataRoot;
    }

    public string GetAppDataRoot()
    {
        var localAppData = _localAppDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "DropShelf");
    }
}
