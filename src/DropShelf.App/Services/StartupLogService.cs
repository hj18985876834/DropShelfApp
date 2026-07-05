using System.IO;

namespace DropShelf.App.Services;

public sealed class StartupLogService
{
    private readonly string _logPath;

    public StartupLogService(string appDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataRoot);

        _logPath = Path.Combine(appDataRoot, "logs", "startup.log");
    }

    public void Write(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.AppendAllText(_logPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
        }
    }

    public void WriteException(Exception exception, string context)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Write($"{context}: {exception}");
    }
}
