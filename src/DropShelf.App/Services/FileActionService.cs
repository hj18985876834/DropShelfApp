using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace DropShelf.App.Services;

public interface IFileActionService
{
    bool PathExists(string path);

    bool Open(string path);

    bool OpenUrl(string url);

    bool RevealInExplorer(string path);
}

public sealed class FileActionService : IFileActionService
{
    public bool PathExists(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return File.Exists(path) || Directory.Exists(path);
    }

    public bool Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!PathExists(path))
        {
            return false;
        }

        return TryStart(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    public bool OpenUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        return TryStart(new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true,
        });
    }

    public bool RevealInExplorer(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!PathExists(path))
        {
            return false;
        }

        return TryStart(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{path}\"",
            UseShellExecute = true,
        });
    }

    private static bool TryStart(ProcessStartInfo startInfo)
    {
        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
