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
    private readonly Action<ShelfItemViewModel> _remove;
    private string? _statusMessage;

    public ShelfItemViewModel(
        ShelfItem item,
        IFileActionService fileActionService,
        IClipboardService clipboardService,
        Action<ShelfItemViewModel> remove)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        _fileActionService = fileActionService ?? throw new ArgumentNullException(nameof(fileActionService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));

        CopyCommand = new RelayCommand(_ => Copy(), _ => CanCopy);
        CopyPathCommand = CopyCommand;
        OpenCommand = new RelayCommand(_ => Open(), _ => CanOpen);
        RevealCommand = new RelayCommand(_ => Reveal(), _ => CanUseFileSystemAction);
        RemoveCommand = new RelayCommand(_ => _remove(this));
    }

    public ShelfItem Item { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Item.DisplayName)
        ? SourcePath ?? "Untitled item"
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
        ShelfItemType.File => "文件",
        ShelfItemType.Folder => "文件夹",
        ShelfItemType.Text => "文本",
        ShelfItemType.Url => "链接",
        ShelfItemType.Image => "图片",
        _ => "项目",
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

    public bool IsFileSystemItem => Item.Type is ShelfItemType.File or ShelfItemType.Folder;

    public bool IsTextContentItem => Item.Type is ShelfItemType.Text or ShelfItemType.Url;

    public string? ExpandedContent => IsTextContentItem
        ? Item.Content
        : null;

    public bool HasExpandedContent => !string.IsNullOrWhiteSpace(ExpandedContent);

    public bool HasSourcePath => !string.IsNullOrWhiteSpace(SourcePath);

    public bool HasActionPath => !string.IsNullOrWhiteSpace(ActionPath);

    public bool Exists => HasActionPath && _fileActionService.PathExists(ActionPath!);

    public bool IsMissing => (IsFileSystemItem || Item.Type is ShelfItemType.Image) && !Exists;

    public bool CanUseFileSystemAction => (IsFileSystemItem || Item.Type is ShelfItemType.Image) && Exists;

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

    public ICommand RemoveCommand { get; }

    public void RefreshPathState()
    {
        OnPathStateChanged();
    }

    public void SetStatusMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        StatusMessage = message;
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
            StatusMessage = "没有可复制的内容。";
            return;
        }

        if (!_clipboardService.SetText(clipboardText))
        {
            StatusMessage = "复制失败。";
            return;
        }

        StatusMessage = string.Equals(clipboardText, SourcePath, StringComparison.Ordinal)
            ? "路径已复制。"
            : "已复制。";
    }

    public void Open()
    {
        if (Item.Type is ShelfItemType.Url)
        {
            StatusMessage = _fileActionService.OpenUrl(Item.Content ?? string.Empty)
                ? null
                : "链接无法打开。";
            return;
        }

        if (string.IsNullOrWhiteSpace(ActionPath))
        {
            StatusMessage = "没有可用路径。";
            return;
        }

        StatusMessage = _fileActionService.Open(ActionPath)
            ? null
            : "项目缺失或无法打开。";
        OnPathStateChanged();
    }

    private void CopyImage()
    {
        if (string.IsNullOrWhiteSpace(Item.ImagePath) || !Exists)
        {
            StatusMessage = "图片缺失。";
            return;
        }

        StatusMessage = _clipboardService.SetImageFromPath(Item.ImagePath)
            ? "已复制。"
            : "图片复制失败。";
        OnPathStateChanged();
    }

    private void Reveal()
    {
        if (string.IsNullOrWhiteSpace(ActionPath))
        {
            StatusMessage = "没有可用路径。";
            return;
        }

        StatusMessage = _fileActionService.RevealInExplorer(ActionPath)
            ? null
            : "项目缺失或无法定位。";
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

    private void OnPathStateChanged()
    {
        OnPropertyChanged(nameof(ActionPath));
        OnPropertyChanged(nameof(HasActionPath));
        OnPropertyChanged(nameof(Exists));
        OnPropertyChanged(nameof(IsMissing));
        OnPropertyChanged(nameof(CanUseFileSystemAction));
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(CanCopy));
        OnPropertyChanged(nameof(ImagePreviewPath));
        OnPropertyChanged(nameof(HasImagePreview));

        if (OpenCommand is RelayCommand openCommand)
        {
            openCommand.RaiseCanExecuteChanged();
        }

        if (RevealCommand is RelayCommand revealCommand)
        {
            revealCommand.RaiseCanExecuteChanged();
        }

        if (CopyCommand is RelayCommand copyCommand)
        {
            copyCommand.RaiseCanExecuteChanged();
        }
    }
}
