namespace DropShelf.App.Interop;

internal static class NativeMethods
{
    public const int HwndBroadcast = 0xFFFF;
    public const int SwShow = 5;

    public static readonly nint HwndTopMost = new(-1);
    public static readonly nint HwndNoTopMost = new(-2);

    public const uint SwpNoMove = 0x0002;
    public const uint SwpNoSize = 0x0001;
    public const uint SwpShowWindow = 0x0040;

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern int RegisterWindowMessage(string lpString);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(nint hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
