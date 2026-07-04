using System.IO;

namespace DropShelf.App.Services;

public sealed class AppDataPathService
{
    public string GetAppDataRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "DropShelf");
    }
}
