using System.IO;
using System.Windows.Input;
using DropShelf.App.Commands;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.App.ViewModels;

public sealed class ShelfItemViewModel : ObservableObject
{
    private static readonly HashSet<string> ImageFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".avif",
        ".bmp",
        ".dib",
        ".gif",
        ".heic",
        ".heif",
        ".ico",
        ".jfif",
        ".jpe",
        ".jpeg",
        ".jpg",
        ".png",
        ".tif",
        ".tiff",
        ".webp",
        ".wdp",
    };

    private readonly IClipboardService _clipboardService;
    private readonly IFileActionService _fileActionService;
    private readonly LocalizationService _localizationService;
    private readonly Action<ShelfItemViewModel> _remove;
    private readonly Action<ShelfItemViewModel> _relink;
    private bool _isExpanded;
    private bool _isDuplicate;
    private bool _isReordering;
    private string? _statusMessage;

    public ShelfItemViewModel(
        ShelfItem item,
        IFileActionService fileActionService,
        IClipboardService clipboardService,
        Action<ShelfItemViewModel> remove,
        Action<ShelfItemViewModel>? relink = null,
        LocalizationService? localizationService = null)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        _fileActionService = fileActionService ?? throw new ArgumentNullException(nameof(fileActionService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
        _relink = relink ?? (_ => { });
        _localizationService = localizationService ?? new LocalizationService();

        CopyCommand = new RelayCommand(_ => Copy(), _ => CanCopy);
        CopyPathCommand = CopyCommand;
        OpenCommand = new RelayCommand(_ => Open(), _ => CanOpen);
        RevealCommand = new RelayCommand(_ => Reveal(), _ => CanUseFileSystemAction);
        RelinkCommand = new RelayCommand(_ => _relink(this), _ => CanRelink);
        RemoveCommand = new RelayCommand(_ => _remove(this));
    }

    public ShelfItem Item { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Item.DisplayName)
        ? SourcePath ?? _localizationService.Text.UntitledItem
        : Item.DisplayName;

    public string? SourcePath => Item.SourcePath;

    public string? ActionPath => Item.Type is ShelfItemType.Image
        ? Item.ImagePath
        : Item.SourcePath;

    public string? ThumbnailPath => Item.ThumbnailPath;

    public string? ImagePreviewPath => GetImagePreviewPath();

    public bool HasImagePreview => !string.IsNullOrWhiteSpace(ImagePreviewPath);

    public ShelfItemType Type => Item.Type;

    public string TypeLabel => Item.Type switch
    {
        ShelfItemType.File => "FILE",
        ShelfItemType.Folder => "DIR",
        ShelfItemType.Text => "TEXT",
        ShelfItemType.Url => "URL",
        ShelfItemType.Image => "IMG",
        _ => "ITEM",
    };

    public string TypeDisplayName => Item.Type switch
    {
        _ => _localizationService.TypeDisplayName(Item.Type),
    };

    public string TypeIcon => Item.Type switch
    {
        ShelfItemType.File => "\uE8A5",
        ShelfItemType.Folder => "\uE8B7",
        ShelfItemType.Text => "\uE8D2",
        ShelfItemType.Url => "\uE71B",
        ShelfItemType.Image => "\uEB9F",
        _ => "\uE8A5",
    };

    public string PathSummary => GetPathSummary();

    public string PreviewText => GetPreviewText();

    public string? PreviewTextTooltip => IsTextContentItem ? null : PreviewText;

    public string MetadataText => GetMetadataText();

    public bool IsFileSystemItem => Item.Type is ShelfItemType.File or ShelfItemType.Folder;

    public bool IsTextContentItem => Item.Type is ShelfItemType.Text;

    public string? ExpandedContent => IsTextContentItem
        ? Item.Content
        : null;

    public bool HasExpandedContent => !string.IsNullOrWhiteSpace(ExpandedContent);

    public bool IsExpanded
    {
        get => _isExpanded;
        private set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(IsExpandedContentVisible));
                OnPropertyChanged(nameof(PreviewTextTooltip));
            }
        }
    }

    public bool IsExpandedContentVisible => HasExpandedContent && IsExpanded;

    public bool IsReordering
    {
        get => _isReordering;
        set => SetProperty(ref _isReordering, value);
    }

    public bool HasSourcePath => !string.IsNullOrWhiteSpace(SourcePath);

    public bool HasActionPath => !string.IsNullOrWhiteSpace(ActionPath);

    public bool Exists => Item.Type switch
    {
        ShelfItemType.File => PathIsExistingFile(SourcePath),
        ShelfItemType.Folder => PathIsExistingDirectory(SourcePath),
        ShelfItemType.Image => PathIsExistingFile(ActionPath),
        _ => false,
    };

    public bool IsInvalidRecord => (IsFileSystemItem || Item.Type is ShelfItemType.Image) && !Exists;

    public bool IsMissing => IsInvalidRecord;

    public bool IsDuplicate
    {
        get => _isDuplicate;
        private set => SetProperty(ref _isDuplicate, value);
    }

    public bool CanUseFileSystemAction => (IsFileSystemItem || Item.Type is ShelfItemType.Image) && Exists;

    public bool CanRelink => IsFileSystemItem;

    public bool CanOpen => CanUseFileSystemAction ||
        Item.Type is ShelfItemType.Url && !string.IsNullOrWhiteSpace(Item.Content);

    public bool CanCopy => Item.Type is ShelfItemType.Image
        ? Exists
        : !string.IsNullOrWhiteSpace(ClipboardText);

    public string? ClipboardText => GetClipboardText();

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand CopyCommand { get; }

    public ICommand CopyPathCommand { get; }

    public ICommand OpenCommand { get; }

    public ICommand RevealCommand { get; }

    public ICommand RelinkCommand { get; }

    public ICommand RemoveCommand { get; }

    public string DragOutTooltip => _localizationService.Text.DragOutTooltip;

    public string ContextCopyText => _localizationService.Text.ContextCopy;

    public string ContextOpenText => _localizationService.Text.ContextOpen;

    public string ContextRevealText => _localizationService.Text.ContextReveal;

    public string ContextRelinkText => _localizationService.Text.ContextRelink;

    public string ContextRemoveText => _localizationService.Text.ContextRemove;

    public string MissingSourceText => IsFileSystemItem
        ? _localizationService.Text.MissingSourceAction
        : _localizationService.Text.MissingSource;

    public string DuplicateSourceText => _localizationService.Text.DuplicateSource;

    public string ReorderHandleTooltip => _localizationService.Text.ReorderHandleTooltip;

    public void RefreshPathState()
    {
        OnPathStateChanged();
    }

    public void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(TypeDisplayName));
        OnPropertyChanged(nameof(MetadataText));
        OnPropertyChanged(nameof(DragOutTooltip));
        OnPropertyChanged(nameof(ContextCopyText));
        OnPropertyChanged(nameof(ContextOpenText));
        OnPropertyChanged(nameof(ContextRevealText));
        OnPropertyChanged(nameof(ContextRelinkText));
        OnPropertyChanged(nameof(ContextRemoveText));
        OnPropertyChanged(nameof(MissingSourceText));
        OnPropertyChanged(nameof(DuplicateSourceText));
        OnPropertyChanged(nameof(ReorderHandleTooltip));
    }

    public void SetDuplicate(bool isDuplicate)
    {
        IsDuplicate = isDuplicate;
    }

    public void SetStatusMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        StatusMessage = message;
    }

    public void ToggleExpanded()
    {
        if (!HasExpandedContent)
        {
            IsExpanded = false;
            return;
        }

        IsExpanded = !IsExpanded;
    }

    public void Copy()
    {
        if (Item.Type is ShelfItemType.Image)
        {
            CopyImage();
            return;
        }

        var clipboardText = ClipboardText;
        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            StatusMessage = _localizationService.Text.CopyNoContent;
            return;
        }

        if (!_clipboardService.SetText(clipboardText))
        {
            StatusMessage = _localizationService.Text.CopyFailed;
            return;
        }

        StatusMessage = string.Equals(clipboardText, SourcePath, StringComparison.Ordinal)
            ? _localizationService.Text.PathCopied
            : _localizationService.Text.Copied;
    }

    public void Open()
    {
        if (Item.Type is ShelfItemType.Url)
        {
            StatusMessage = _fileActionService.OpenUrl(Item.Content ?? string.Empty)
                ? null
                : _localizationService.Text.UrlOpenFailed;
            return;
        }

        if (string.IsNullOrWhiteSpace(ActionPath))
        {
            StatusMessage = _localizationService.Text.NoPath;
            return;
        }

        StatusMessage = _fileActionService.Open(ActionPath)
            ? null
            : _localizationService.Text.OpenFailed;
        OnPathStateChanged();
    }

    private void CopyImage()
    {
        if (string.IsNullOrWhiteSpace(Item.ImagePath) || !Exists)
        {
            StatusMessage = _localizationService.Text.MissingImage;
            return;
        }

        StatusMessage = _clipboardService.SetImageFromPath(Item.ImagePath)
            ? _localizationService.Text.Copied
            : _localizationService.Text.ImageCopyFailed;
        OnPathStateChanged();
    }

    private void Reveal()
    {
        if (string.IsNullOrWhiteSpace(ActionPath))
        {
            StatusMessage = _localizationService.Text.NoPath;
            return;
        }

        StatusMessage = _fileActionService.RevealInExplorer(ActionPath)
            ? null
            : _localizationService.Text.RevealFailed;
        OnPathStateChanged();
    }

    private string GetPathSummary()
    {
        if (string.IsNullOrWhiteSpace(SourcePath))
        {
            return string.Empty;
        }

        try
        {
            var trimmedPath = SourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetDirectoryName(trimmedPath) ?? SourcePath;
        }
        catch (ArgumentException)
        {
            return SourcePath;
        }
    }

    private string GetPreviewText()
    {
        if (!string.IsNullOrWhiteSpace(Item.Content))
        {
            return Item.Content;
        }

        if (!string.IsNullOrWhiteSpace(PathSummary))
        {
            return PathSummary;
        }

        if (!string.IsNullOrWhiteSpace(Item.ImagePath))
        {
            return Item.ImagePath;
        }

        return Item.CreatedAt.LocalDateTime.ToString("g");
    }

    private string GetMetadataText()
    {
        var detail = Item.Type switch
        {
            ShelfItemType.File => GetFileDetail(),
            ShelfItemType.Folder => Exists ? _localizationService.Text.FolderDetail : _localizationService.Text.MissingDetail,
            ShelfItemType.Image => GetImageDetail(),
            ShelfItemType.Text => GetTextDetail(),
            ShelfItemType.Url => GetUrlDetail(),
            _ => _localizationService.Text.TypeItem,
        };

        return _localizationService.MetadataText(Item.Type, detail, Item.CreatedAt);
    }

    private string GetFileDetail()
    {
        if (string.IsNullOrWhiteSpace(SourcePath) || !Exists)
        {
            return _localizationService.Text.MissingDetail;
        }

        try
        {
            var fileInfo = new FileInfo(SourcePath);
            return FormatBytes(fileInfo.Length);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return _localizationService.Text.UnknownSize;
        }
    }

    private string GetImageDetail()
    {
        if (string.IsNullOrWhiteSpace(ActionPath) || !Exists)
        {
            return _localizationService.Text.MissingDetail;
        }

        try
        {
            var fileInfo = new FileInfo(ActionPath);
            return FormatBytes(fileInfo.Length);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return _localizationService.Text.UnknownSize;
        }
    }

    private string GetTextDetail()
    {
        var length = Item.Content?.Length ?? 0;
        return _localizationService.TextLengthDetail(length);
    }

    private string GetUrlDetail()
    {
        if (!Uri.TryCreate(Item.Content, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return _localizationService.Text.UrlDetail;
        }

        return uri.Host;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)Math.Max(0, bytes);
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {units[unitIndex]}"
            : $"{value:0.#} {units[unitIndex]}";
    }

    private string? GetClipboardText()
    {
        if (!string.IsNullOrWhiteSpace(Item.Content))
        {
            return Item.Content;
        }

        if (!string.IsNullOrWhiteSpace(SourcePath))
        {
            return SourcePath;
        }

        if (!string.IsNullOrWhiteSpace(Item.ImagePath))
        {
            return Item.ImagePath;
        }

        return null;
    }

    private string? GetImagePreviewPath()
    {
        if (Item.Type is ShelfItemType.Image)
        {
            return !string.IsNullOrWhiteSpace(Item.ThumbnailPath)
                ? Item.ThumbnailPath
                : Item.ImagePath;
        }

        if (Item.Type is ShelfItemType.File &&
            !string.IsNullOrWhiteSpace(Item.SourcePath) &&
            IsSupportedImageFile(Item.SourcePath))
        {
            return Item.SourcePath;
        }

        return null;
    }

    private static bool IsSupportedImageFile(string path)
    {
        try
        {
            return ImageFileExtensions.Contains(Path.GetExtension(path));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool PathIsExistingFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return File.Exists(path);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static bool PathIsExistingDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return Directory.Exists(path);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private void OnPathStateChanged()
    {
        OnPropertyChanged(nameof(ActionPath));
        OnPropertyChanged(nameof(HasActionPath));
        OnPropertyChanged(nameof(Exists));
        OnPropertyChanged(nameof(IsInvalidRecord));
        OnPropertyChanged(nameof(IsMissing));
        OnPropertyChanged(nameof(CanUseFileSystemAction));
        OnPropertyChanged(nameof(CanRelink));
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(CanCopy));
        OnPropertyChanged(nameof(ImagePreviewPath));
        OnPropertyChanged(nameof(HasImagePreview));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(MetadataText));

        if (OpenCommand is RelayCommand openCommand)
        {
            openCommand.RaiseCanExecuteChanged();
        }

        if (RevealCommand is RelayCommand revealCommand)
        {
            revealCommand.RaiseCanExecuteChanged();
        }

        if (RelinkCommand is RelayCommand relinkCommand)
        {
            relinkCommand.RaiseCanExecuteChanged();
        }

        if (CopyCommand is RelayCommand copyCommand)
        {
            copyCommand.RaiseCanExecuteChanged();
        }
    }
}
