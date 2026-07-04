using System.IO;

namespace DropShelf.App.Services;

public sealed class ImageStore
{
    public ImageStore(string appDataRoot)
    {
        OriginalsDirectory = Path.Combine(appDataRoot, "images", "originals");
        ThumbnailsDirectory = Path.Combine(appDataRoot, "images", "thumbs");
    }

    public string OriginalsDirectory { get; }

    public string ThumbnailsDirectory { get; }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(OriginalsDirectory);
        Directory.CreateDirectory(ThumbnailsDirectory);
    }
}
