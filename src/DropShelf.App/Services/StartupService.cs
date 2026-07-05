using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace DropShelf.App.Services;

public interface IStartupRegistry
{
    string? GetValue(string name);

    void SetValue(string name, string value);

    void DeleteValue(string name);
}

public sealed class StartupService
{
    public const string DefaultEntryName = "EdgeTuck";
    private const string LegacyEntryName = "DropShelf";

    private readonly string _entryName;
    private readonly string _executablePath;
    private readonly string? _legacyEntryName;
    private readonly IStartupRegistry _registry;

    public StartupService()
        : this(new CurrentUserRunRegistry(), DefaultEntryName, GetCurrentExecutablePath())
    {
    }

    public StartupService(IStartupRegistry registry, string entryName, string executablePath)
        : this(registry, entryName, executablePath, string.Equals(entryName, DefaultEntryName, StringComparison.Ordinal) ? LegacyEntryName : null)
    {
    }

    private StartupService(IStartupRegistry registry, string entryName, string executablePath, string? legacyEntryName)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _entryName = string.IsNullOrWhiteSpace(entryName)
            ? throw new ArgumentException("Entry name is required.", nameof(entryName))
            : entryName;
        _executablePath = string.IsNullOrWhiteSpace(executablePath)
            ? throw new ArgumentException("Executable path is required.", nameof(executablePath))
            : executablePath;
        _legacyEntryName = legacyEntryName;
    }

    public bool IsEnabled()
    {
        try
        {
            var value = _registry.GetValue(_entryName);
            if (string.Equals(Unquote(value), _executablePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var legacyValue = _legacyEntryName is null ? null : _registry.GetValue(_legacyEntryName);
            return string.Equals(Unquote(legacyValue), _executablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or InvalidOperationException)
        {
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            _registry.SetValue(_entryName, Quote(_executablePath));
            if (_legacyEntryName is not null)
            {
                _registry.DeleteValue(_legacyEntryName);
            }

            return;
        }

        _registry.DeleteValue(_entryName);
        if (_legacyEntryName is not null)
        {
            _registry.DeleteValue(_legacyEntryName);
        }
    }

    private static string GetCurrentExecutablePath()
    {
        return Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Unable to resolve the current executable path.");
    }

    private static string Quote(string value)
    {
        return $"\"{value}\"";
    }

    private static string? Unquote(string? value)
    {
        return value?.Trim().Trim('"');
    }

    private sealed class CurrentUserRunRegistry : IStartupRegistry
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public string? GetValue(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(name) as string;
        }

        public void SetValue(string name, string value)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("Unable to open the current-user startup registry key.");
            key.SetValue(name, value, RegistryValueKind.String);
        }

        public void DeleteValue(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
    }
}
