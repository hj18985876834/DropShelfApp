using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using WpfClipboard = System.Windows.Clipboard;

namespace DropShelf.App.Services;

public interface IClipboardService
{
    bool SetText(string text);

    bool SetImageFromPath(string path);
}

public sealed class ClipboardService : IClipboardService
{
    public bool SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            WpfClipboard.SetText(text);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    public bool SetImageFromPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            WpfClipboard.SetImage(bitmap);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
