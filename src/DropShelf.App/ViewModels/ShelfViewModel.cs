using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using DropShelf.App.Commands;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.App.ViewModels;

public sealed class ShelfViewModel : ObservableObject
{
    private readonly Action? _openSettings;
    private readonly IClipboardService _clipboardService;
    private readonly IFileActionService _fileActionService;
    private readonly ImageStore? _imageStore;
    private readonly Func<int, bool>? _confirmClearAll;
    private readonly LocalizationService _localizationService;
    private readonly DensityMode _densityMode;
    private readonly ThemeMode _themeMode;
    private bool _isDragOverAccepted;
    private bool _isDragOverUnsupported;
    private bool _isEmpty = true;
    private bool _isShelfVisible;
    private ShelfItemViewModel? _selectedItem;

    public ShelfViewModel(
        Action? openSettings = null,
        IEnumerable<ShelfItem>? initialItems = null,
        IFileActionService? fileActionService = null,
        IClipboardService? clipboardService = null,
        ImageStore? imageStore = null,
        Func<int, bool>? confirmClearAll = null,
        LocalizationService? localizationService = null,
        DensityMode densityMode = DensityMode.Compact,
        ThemeMode themeMode = ThemeMode.System)
    {
        _openSettings = openSettings;
        _fileActionService = fileActionService ?? new FileActionService();
        _clipboardService = clipboardService ?? new ClipboardService();
        _imageStore = imageStore;
        _confirmClearAll = confirmClearAll;
        _localizationService = localizationService ?? new LocalizationService();
        _densityMode = densityMode;
        _themeMode = themeMode;
        _localizationService.LanguageChanged += OnLanguageChanged;

        ShowShelfCommand = new RelayCommand(_ => IsShelfVisible = true);
        HideShelfCommand = new RelayCommand(_ => IsShelfVisible = false);
        ToggleShelfCommand = new RelayCommand(_ => IsShelfVisible = !IsShelfVisible);
        OpenSettingsCommand = new RelayCommand(_ => _openSettings?.Invoke());
        ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => Items.Count > 0);
        RemoveSelectedCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedItem is not null);
        CopySelectedCommand = new RelayCommand(_ => CopySelected(), _ => SelectedItem?.CanCopy == true);
        OpenSelectedCommand = new RelayCommand(_ => OpenSelected(), _ => SelectedItem?.CanOpen == true);
        Items.CollectionChanged += OnItemsChanged;

        if (initialItems is not null)
        {
            AddItems(initialItems);
        }
    }

    public ObservableCollection<ShelfItemViewModel> Items { get; } = [];

    public bool IsShelfVisible
    {
        get => _isShelfVisible;
        set => SetProperty(ref _isShelfVisible, value);
    }

    public bool IsDragOverAccepted
    {
        get => _isDragOverAccepted;
        set
        {
            if (SetProperty(ref _isDragOverAccepted, value) && value)
            {
                IsDragOverUnsupported = false;
            }

            OnPropertyChanged(nameof(DropStateText));
        }
    }

    public bool IsDragOverUnsupported
    {
        get => _isDragOverUnsupported;
        set
        {
            if (SetProperty(ref _isDragOverUnsupported, value) && value)
            {
                IsDragOverAccepted = false;
            }

            OnPropertyChanged(nameof(DropStateText));
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public bool HasItems => !IsEmpty;

    public int ItemCount => Items.Count;

    public string ItemCountSuffix => _localizationService.Text.ShelfItemCountSuffix;

    public string ClearAllTooltip => _localizationService.Text.ClearAllTooltip;

    public string SettingsTooltip => _localizationService.Text.SettingsTooltip;

    public string CollapseTooltip => _localizationService.Text.CollapseTooltip;

    public string EmptyTitle => _localizationService.Text.EmptyTitle;

    public string EmptyDescription => _localizationService.Text.EmptyDescription;

    public string EmptyFileChip => _localizationService.Text.EmptyFileChip;

    public string EmptyTextChip => _localizationService.Text.EmptyTextChip;

    public string EmptyImageChip => _localizationService.Text.EmptyImageChip;

    public string ReleaseToAddText => _localizationService.Text.ReleaseToAdd;

    public string UnsupportedContentText => _localizationService.Text.UnsupportedContent;

    public string HandleTooltip => _localizationService.Text.HandleTooltip;

    public string DropStateText => IsDragOverUnsupported
        ? UnsupportedContentText
        : ReleaseToAddText;

    public ShelfItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                RaiseSelectedCommandCanExecuteChanged();
            }
        }
    }

    public bool IsCompactDensity => _densityMode == DensityMode.Compact;

    public bool IsDarkTheme => _themeMode == ThemeMode.Dark;

    public ICommand ShowShelfCommand { get; }

    public ICommand HideShelfCommand { get; }

    public ICommand ToggleShelfCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public ICommand ClearAllCommand { get; }

    public ICommand RemoveSelectedCommand { get; }

    public ICommand CopySelectedCommand { get; }

    public ICommand OpenSelectedCommand { get; }

    public IEnumerable<ShelfItem> GetShelfItems()
    {
        return Items.Select(item => item.Item);
    }

    public void AddItems(IEnumerable<ShelfItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var shouldSelectFirstNewItem = SelectedItem is null;
        ShelfItemViewModel? firstNewItem = null;

        foreach (var item in items)
        {
            var itemViewModel = CreateItemViewModel(item);
            firstNewItem ??= itemViewModel;
            Items.Add(itemViewModel);
        }

        if (shouldSelectFirstNewItem)
        {
            SelectedItem = firstNewItem;
        }
    }

    public void ClearAll()
    {
        if (Items.Count == 0)
        {
            return;
        }

        if (_confirmClearAll?.Invoke(Items.Count) == false)
        {
            return;
        }

        foreach (var item in Items)
        {
            _imageStore?.DeleteImageFiles(item.Item);
        }

        SelectedItem = null;
        Items.Clear();
    }

    public void RefreshItemStates()
    {
        foreach (var item in Items)
        {
            item.RefreshPathState();
        }

        RaiseSelectedCommandCanExecuteChanged();
    }

    private ShelfItemViewModel CreateItemViewModel(ShelfItem item)
    {
        return new ShelfItemViewModel(item, _fileActionService, _clipboardService, RemoveItem, _localizationService);
    }

    private void RemoveItem(ShelfItemViewModel item)
    {
        var itemIndex = Items.IndexOf(item);
        if (itemIndex < 0)
        {
            return;
        }

        var wasSelected = ReferenceEquals(SelectedItem, item);
        _imageStore?.DeleteImageFiles(item.Item);
        Items.RemoveAt(itemIndex);

        if (wasSelected)
        {
            SelectedItem = Items.Count == 0
                ? null
                : Items[Math.Min(itemIndex, Items.Count - 1)];
        }
    }

    private void RemoveSelected()
    {
        if (SelectedItem is not null)
        {
            RemoveItem(SelectedItem);
        }
    }

    private void CopySelected()
    {
        SelectedItem?.Copy();
        RaiseSelectedCommandCanExecuteChanged();
    }

    private void OpenSelected()
    {
        SelectedItem?.Open();
        RaiseSelectedCommandCanExecuteChanged();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        IsEmpty = Items.Count == 0;
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ItemCount));

        if (ClearAllCommand is RelayCommand clearCommand)
        {
            clearCommand.RaiseCanExecuteChanged();
        }

        RaiseSelectedCommandCanExecuteChanged();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ItemCountSuffix));
        OnPropertyChanged(nameof(ClearAllTooltip));
        OnPropertyChanged(nameof(SettingsTooltip));
        OnPropertyChanged(nameof(CollapseTooltip));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyDescription));
        OnPropertyChanged(nameof(EmptyFileChip));
        OnPropertyChanged(nameof(EmptyTextChip));
        OnPropertyChanged(nameof(EmptyImageChip));
        OnPropertyChanged(nameof(ReleaseToAddText));
        OnPropertyChanged(nameof(UnsupportedContentText));
        OnPropertyChanged(nameof(HandleTooltip));
        OnPropertyChanged(nameof(DropStateText));

        foreach (var item in Items)
        {
            item.RefreshLocalizedText();
        }
    }

    private void RaiseSelectedCommandCanExecuteChanged()
    {
        if (RemoveSelectedCommand is RelayCommand removeSelectedCommand)
        {
            removeSelectedCommand.RaiseCanExecuteChanged();
        }

        if (CopySelectedCommand is RelayCommand copySelectedCommand)
        {
            copySelectedCommand.RaiseCanExecuteChanged();
        }

        if (OpenSelectedCommand is RelayCommand openSelectedCommand)
        {
            openSelectedCommand.RaiseCanExecuteChanged();
        }
    }
}
