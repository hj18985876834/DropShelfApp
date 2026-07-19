using DropShelf.App.Interop;

namespace DropShelf.App.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    public const int ToggleShelfHotkeyId = 1;
    public const int QuickPasteHotkeyId = 2;
    public const uint VirtualKeySpace = 0x20;
    public const uint VirtualKeyV = 0x56;

    private readonly nint _windowHandle;
    private readonly List<int> _registeredIds = [];
    private bool _disposed;

    public GlobalHotkeyService(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));
        }

        _windowHandle = windowHandle;
    }

    public bool RegisterDefaultHotkeys()
    {
        return Register(ToggleShelfHotkeyId, NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModNoRepeat, VirtualKeySpace) &
            Register(QuickPasteHotkeyId, NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModNoRepeat, VirtualKeyV);
    }

    private bool Register(int id, uint modifiers, uint virtualKey)
    {
        if (NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, virtualKey))
        {
            _registeredIds.Add(id);
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var id in _registeredIds)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, id);
        }

        _registeredIds.Clear();
        _disposed = true;
    }
}
