using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Input;
using DropShelf.App.Commands;
using DropShelf.App.Models;
using DropShelf.App.Services;

namespace DropShelf.App.ViewModels;

public sealed class ShelfViewModel : ObservableObject
{
    private readonly Action? _openSettings;
    private readonly Action<bool>? _pinStateChanged;
    private readonly IClipboardService _clipboardService;
    private readonly IFileActionService _fileActionService;
    private readonly ImageStore? _imageStore;
    private readonly Func<int, bool>? _confirmClearAll;
    private readonly Func<int, bool>? _confirmRemoveSelected;
    private readonly Func<ShelfItem, string?>? _selectRelinkPath;
    private readonly LocalizationService _localizationService;
    private readonly DensityMode _densityMode;
    private readonly ThemeMode _themeMode;
    private readonly IReadOnlyList<LocalizedOption<ShelfFilterMode>> _filterModeOptions;
    private bool _isDragOverAccepted;
    private bool _isDragOverUnsupported;
    private bool _isEmpty = true;
    private bool _isShelfVisible;
    private bool _isShelfPinned;
    private ShelfFilterMode _activeFilter;
    private string _searchQuery = string.Empty;
    private ShelfItemViewModel? _selectedItem;
    private string? _shelfStatusMessage;

    public ShelfViewModel(
        Action? openSettings = null,
        IEnumerable<ShelfItem>? initialItems = null,
        IFileActionService? fileActionService = null,
        IClipboardService? clipboardService = null,
        ImageStore? imageStore = null,
        Func<int, bool>? confirmClearAll = null,
        Func<int, bool>? confirmRemoveSelected = null,
        Func<ShelfItem, string?>? selectRelinkPath = null,
        LocalizationService? localizationService = null,
        DensityMode densityMode = DensityMode.Compact,
        ThemeMode themeMode = ThemeMode.System,
        bool isShelfPinned = false,
        Action<bool>? pinStateChanged = null)
    {
        _openSettings = openSettings;
        _pinStateChanged = pinStateChanged;
        _fileActionService = fileActionService ?? new FileActionService();
        _clipboardService = clipboardService ?? new ClipboardService();
        _imageStore = imageStore;
        _confirmClearAll = confirmClearAll;
        _confirmRemoveSelected = confirmRemoveSelected;
        _selectRelinkPath = selectRelinkPath;
        _localizationService = localizationService ?? new LocalizationService();
        _densityMode = densityMode;
        _themeMode = themeMode;
        _isShelfPinned = isShelfPinned;
        _filterModeOptions = Enum.GetValues<ShelfFilterMode>()
            .Select(value => new LocalizedOption<ShelfFilterMode>(value, GetFilterModeDisplayName(value)))
            .ToArray();
        _localizationService.LanguageChanged += OnLanguageChanged;

        ShowShelfCommand = new RelayCommand(_ => IsShelfVisible = true);
        HideShelfCommand = new RelayCommand(_ => IsShelfVisible = false);
        ToggleShelfCommand = new RelayCommand(_ => IsShelfVisible = !IsShelfVisible);
        TogglePinCommand = new RelayCommand(_ => IsShelfPinned = !IsShelfPinned);
        OpenSettingsCommand = new RelayCommand(_ => _openSettings?.Invoke());
        ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => Items.Count > 0);
        ClearInvalidCommand = new RelayCommand(_ => ClearInvalid(), _ => HasInvalidItems);
        RemoveSelectedCommand = new RelayCommand(_ => RemoveSelected(), _ => GetEffectiveSelectedItems().Count > 0);
        CopySelectedCommand = new RelayCommand(_ => CopySelected(), _ => CanCopySelection());
        OpenSelectedCommand = new RelayCommand(_ => OpenSelected(), _ => SelectedItem?.CanOpen == true);
        Items.CollectionChanged += OnItemsChanged;

        if (initialItems is not null)
        {
            AddInitialItems(initialItems);
        }
    }

    public ObservableCollection<ShelfItemViewModel> Items { get; } = [];

    public ObservableCollection<ShelfItemViewModel> VisibleItems { get; } = [];

    public ObservableCollection<ShelfItemViewModel> SelectedItems { get; } = [];

    public IReadOnlyList<LocalizedOption<ShelfFilterMode>> FilterModeOptions => _filterModeOptions;

    public bool IsShelfVisible
    {
        get => _isShelfVisible;
        set => SetProperty(ref _isShelfVisible, value);
    }

    public bool IsShelfPinned
    {
        get => _isShelfPinned;
        set
        {
            if (!SetProperty(ref _isShelfPinned, value))
            {
                return;
            }

            OnPropertyChanged(nameof(PinShelfTooltip));
            _pinStateChanged?.Invoke(value);
        }
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

    public int VisibleItemCount => VisibleItems.Count;

    public int InvalidItemCount => Items.Count(item => item.IsInvalidRecord);

    public int DuplicateItemCount => Items.Count(item => item.IsDuplicate);

    public bool HasInvalidItems => InvalidItemCount > 0;

    public bool HasDuplicateItems => DuplicateItemCount > 0;

    public bool HasVisibleItems => VisibleItems.Count > 0;

    public int SelectedItemCount => SelectedItems.Count;

    public bool HasMultipleSelectedItems => SelectedItems.Count > 1;

    public bool IsNoResults => HasItems && !HasVisibleItems;

    public string ItemCountSuffix => _localizationService.Text.ShelfItemCountSuffix;

    public string FilterLabel => _localizationService.Text.FilterLabel;

    public string SearchPlaceholder => _localizationService.Text.SearchPlaceholder;

    public string NoResultsTitle => _localizationService.Text.NoResultsTitle;

    public string NoResultsDescription => _localizationService.Text.NoResultsDescription;

    public string ClearAllTooltip => _localizationService.Text.ClearAllTooltip;

    public string ClearInvalidTooltip => _localizationService.Text.ClearInvalidTooltip;

    public string PinShelfTooltip => IsShelfPinned
        ? _localizationService.Text.UnpinShelfTooltip
        : _localizationService.Text.PinShelfTooltip;

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

    public string? ShelfStatusMessage
    {
        get => _shelfStatusMessage;
        private set
        {
            if (SetProperty(ref _shelfStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasShelfStatusMessage));
            }
        }
    }

    public bool HasShelfStatusMessage => !string.IsNullOrWhiteSpace(ShelfStatusMessage);

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

    public ShelfFilterMode ActiveFilter
    {
        get => _activeFilter;
        set
        {
            if (!SetProperty(ref _activeFilter, value))
            {
                return;
            }

            RefreshVisibleItems();
            OnPropertyChanged(nameof(NoResultsTitle));
            OnPropertyChanged(nameof(NoResultsDescription));
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!SetProperty(ref _searchQuery, value ?? string.Empty))
            {
                return;
            }

            RefreshVisibleItems();
            OnPropertyChanged(nameof(IsSearchActive));
            OnPropertyChanged(nameof(NoResultsTitle));
            OnPropertyChanged(nameof(NoResultsDescription));
        }
    }

    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchQuery);

    public ICommand ShowShelfCommand { get; }

    public ICommand HideShelfCommand { get; }

    public ICommand ToggleShelfCommand { get; }

    public ICommand TogglePinCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public ICommand ClearAllCommand { get; }

    public ICommand ClearInvalidCommand { get; }

    public ICommand RemoveSelectedCommand { get; }

    public ICommand CopySelectedCommand { get; }

    public ICommand OpenSelectedCommand { get; }

    public IEnumerable<ShelfItem> GetShelfItems()
    {
        return Items.Select(item => item.Item);
    }

    public ShelfAddResult AddItems(IEnumerable<ShelfItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var shouldSelectFirstNewItem = SelectedItem is null;
        ShelfItemViewModel? firstNewItem = null;
        var addedItems = new List<ShelfItem>();
        var addedCount = 0;
        var duplicateCount = 0;
        var pathKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var existingItem in Items)
        {
            if (TryCreatePathKey(existingItem.Item, out var existingKey))
            {
                pathKeys.Add(existingKey);
            }
        }

        foreach (var item in items)
        {
            if (TryCreatePathKey(item, out var pathKey) && !pathKeys.Add(pathKey))
            {
                duplicateCount++;
                continue;
            }

            var itemViewModel = CreateItemViewModel(item);
            firstNewItem ??= itemViewModel;
            Items.Add(itemViewModel);
            addedItems.Add(item);
            addedCount++;
        }

        if (shouldSelectFirstNewItem)
        {
            SelectedItem = firstNewItem is not null && VisibleItems.Contains(firstNewItem)
                ? firstNewItem
                : VisibleItems.FirstOrDefault();
        }

        if (addedCount > 0 || duplicateCount > 0)
        {
            ShelfStatusMessage = _localizationService.AddItemsMessage(addedItems, duplicateCount);
        }

        return new ShelfAddResult(addedCount, duplicateCount);
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
        ClearSelectedItems();
        Items.Clear();
    }

    public void RefreshItemStates()
    {
        foreach (var item in Items)
        {
            item.RefreshPathState();
        }

        RefreshDuplicateStates();
        RefreshVisibleItems();
        RaiseCollectionStateChanged();
        RaiseClearInvalidCommandCanExecuteChanged();
        RaiseSelectedCommandCanExecuteChanged();
    }

    public void ClearShelfStatusMessage()
    {
        ShelfStatusMessage = null;
    }

    public void MoveItem(ShelfItemViewModel item, int targetVisibleIndex)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (ActiveFilter is not ShelfFilterMode.All)
        {
            return;
        }

        var sourceIndex = Items.IndexOf(item);
        if (sourceIndex < 0 || targetVisibleIndex < 0 || targetVisibleIndex >= VisibleItems.Count)
        {
            return;
        }

        var targetItem = VisibleItems[targetVisibleIndex];
        var targetIndex = Items.IndexOf(targetItem);
        if (targetIndex < 0 || sourceIndex == targetIndex)
        {
            return;
        }

        Items.Move(sourceIndex, targetIndex);
        SelectOnly(item);
    }

    public void SelectOnly(ShelfItemViewModel? item)
    {
        ClearSelectedItems();
        SelectedItem = item is not null && VisibleItems.Contains(item)
            ? item
            : null;

        if (SelectedItem is not null)
        {
            AddSelectedItem(SelectedItem);
        }
    }

    public void ToggleSelection(ShelfItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!VisibleItems.Contains(item))
        {
            return;
        }

        if (SelectedItems.Contains(item))
        {
            RemoveSelectedItem(item);
            SelectedItem = SelectedItems.LastOrDefault();
        }
        else
        {
            AddSelectedItem(item);
            SelectedItem = item;
        }
    }

    public void SelectRangeTo(ShelfItemViewModel item)
    {
        SelectRangeTo(item, SelectedItem);
    }

    public void SelectRangeTo(ShelfItemViewModel item, ShelfItemViewModel? anchor)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!VisibleItems.Contains(item))
        {
            return;
        }

        anchor = anchor is not null && VisibleItems.Contains(anchor)
            ? anchor
            : SelectedItems.FirstOrDefault(selectedItem => VisibleItems.Contains(selectedItem));
        anchor ??= item;

        var anchorIndex = VisibleItems.IndexOf(anchor);
        var targetIndex = VisibleItems.IndexOf(item);
        if (anchorIndex < 0 || targetIndex < 0)
        {
            return;
        }

        ClearSelectedItems();
        var startIndex = Math.Min(anchorIndex, targetIndex);
        var endIndex = Math.Max(anchorIndex, targetIndex);
        for (var index = startIndex; index <= endIndex; index++)
        {
            AddSelectedItem(VisibleItems[index]);
        }

        SelectedItem = item;
    }

    public void SelectAllVisible()
    {
        ClearSelectedItems();
        foreach (var item in VisibleItems)
        {
            AddSelectedItem(item);
        }

        SelectedItem = SelectedItems.FirstOrDefault();
    }

    public bool MoveSelectedItem(int visibleOffset)
    {
        if (SelectedItem is null || ActiveFilter is not ShelfFilterMode.All)
        {
            return false;
        }

        var sourceVisibleIndex = VisibleItems.IndexOf(SelectedItem);
        if (sourceVisibleIndex < 0)
        {
            return false;
        }

        var targetVisibleIndex = Math.Clamp(sourceVisibleIndex + visibleOffset, 0, VisibleItems.Count - 1);
        if (targetVisibleIndex == sourceVisibleIndex)
        {
            return false;
        }

        MoveItem(SelectedItem, targetVisibleIndex);
        return true;
    }

    private ShelfItemViewModel CreateItemViewModel(ShelfItem item)
    {
        return new ShelfItemViewModel(item, _fileActionService, _clipboardService, RemoveFromCardCommand, RelinkItem, _localizationService);
    }

    private void RemoveFromCardCommand(ShelfItemViewModel item)
    {
        if (item.IsBatchSelected && SelectedItems.Count > 1)
        {
            RemoveSelected();
            return;
        }

        RemoveItem(item);
    }

    private void AddInitialItems(IEnumerable<ShelfItem> items)
    {
        foreach (var item in items)
        {
            Items.Add(CreateItemViewModel(item));
        }

        RefreshDuplicateStates();

        if (SelectedItem is null)
        {
            SelectedItem = VisibleItems.FirstOrDefault();
        }
    }

    private void RemoveItem(ShelfItemViewModel item)
    {
        var itemIndex = Items.IndexOf(item);
        if (itemIndex < 0)
        {
            return;
        }

        var wasSelected = ReferenceEquals(SelectedItem, item);
        var wasBatchSelected = SelectedItems.Contains(item);
        _imageStore?.DeleteImageFiles(item.Item);
        Items.RemoveAt(itemIndex);
        if (wasBatchSelected)
        {
            RemoveSelectedItem(item);
        }

        if (wasSelected)
        {
            SelectOnly(Items.Count == 0
                ? null
                : FirstVisibleItemAtOrBefore(itemIndex));
        }
    }

    private void RelinkItem(ShelfItemViewModel item)
    {
        var itemIndex = Items.IndexOf(item);
        if (itemIndex < 0 || !item.CanRelink)
        {
            return;
        }

        var newPath = _selectRelinkPath?.Invoke(item.Item);
        if (string.IsNullOrWhiteSpace(newPath))
        {
            return;
        }

        if (!IsValidRelinkPath(item.Type, newPath))
        {
            item.SetStatusMessage(_localizationService.Text.RelinkInvalidPath);
            item.RefreshPathState();
            return;
        }

        var replacement = CreateRelinkedItem(item.Item, newPath);
        if (WouldDuplicateExistingPath(replacement, item.Item.Id))
        {
            item.SetStatusMessage(_localizationService.Text.RelinkDuplicate);
            item.RefreshPathState();
            return;
        }

        var replacementViewModel = CreateItemViewModel(replacement);
        replacementViewModel.SetStatusMessage(_localizationService.Text.RelinkUpdated);
        Items[itemIndex] = replacementViewModel;
        SelectedItem = VisibleItems.Contains(replacementViewModel)
            ? replacementViewModel
            : VisibleItems.FirstOrDefault();
    }

    private void ClearInvalid()
    {
        var removedCount = 0;
        for (var index = Items.Count - 1; index >= 0; index--)
        {
            if (!Items[index].IsInvalidRecord)
            {
                continue;
            }

            var item = Items[index];
            _imageStore?.DeleteImageFiles(item.Item);
            Items.RemoveAt(index);
            if (SelectedItems.Contains(item))
            {
                RemoveSelectedItem(item);
            }

            removedCount++;
        }

        if (removedCount > 0)
        {
            ShelfStatusMessage = _localizationService.ClearInvalidMessage(removedCount);
        }
    }

    private void RemoveSelected()
    {
        var selectedItems = GetEffectiveSelectedItems();
        if (selectedItems.Count == 0)
        {
            return;
        }

        if (selectedItems.Count > 1 && _confirmRemoveSelected?.Invoke(selectedItems.Count) == false)
        {
            return;
        }

        foreach (var item in selectedItems)
        {
            RemoveItem(item);
        }
    }

    private void CopySelected()
    {
        var selectedItems = GetEffectiveSelectedItems();
        if (selectedItems.Count == 0)
        {
            return;
        }

        if (selectedItems.Count == 1)
        {
            selectedItems[0].Copy();
        }
        else
        {
            CopySelectedItems(selectedItems);
        }

        RaiseSelectedCommandCanExecuteChanged();
    }

    private void OpenSelected()
    {
        SelectedItem?.Open();
        RaiseSelectedCommandCanExecuteChanged();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshDuplicateStates();
        RefreshVisibleItems();
        RaiseCollectionStateChanged();

        if (ClearAllCommand is RelayCommand clearCommand)
        {
            clearCommand.RaiseCanExecuteChanged();
        }

        RaiseClearInvalidCommandCanExecuteChanged();
        RaiseSelectedCommandCanExecuteChanged();
    }

    private void RaiseClearInvalidCommandCanExecuteChanged()
    {
        if (ClearInvalidCommand is RelayCommand clearInvalidCommand)
        {
            clearInvalidCommand.RaiseCanExecuteChanged();
        }

    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ItemCountSuffix));
        OnPropertyChanged(nameof(FilterLabel));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(NoResultsTitle));
        OnPropertyChanged(nameof(NoResultsDescription));
        OnPropertyChanged(nameof(PinShelfTooltip));
        OnPropertyChanged(nameof(ClearAllTooltip));
        OnPropertyChanged(nameof(ClearInvalidTooltip));
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

        foreach (var option in _filterModeOptions)
        {
            option.DisplayName = GetFilterModeDisplayName(option.Value);
        }
    }

    private void RefreshVisibleItems()
    {
        var previousSelection = SelectedItem;
        var visibleItems = Items.Where(MatchesVisibleCriteria).ToArray();
        VisibleItems.Clear();
        foreach (var item in visibleItems)
        {
            VisibleItems.Add(item);
        }

        OnPropertyChanged(nameof(VisibleItemCount));
        OnPropertyChanged(nameof(HasVisibleItems));
        OnPropertyChanged(nameof(IsNoResults));

        RemoveHiddenSelections();

        if (SelectedItems.Count > 0)
        {
            SelectedItem = SelectedItems.LastOrDefault();
        }
        else if (previousSelection is not null && VisibleItems.Contains(previousSelection))
        {
            SelectedItem = previousSelection;
            AddSelectedItem(previousSelection);
        }
        else
        {
            SelectedItem = VisibleItems.FirstOrDefault();
            if (SelectedItem is not null)
            {
                AddSelectedItem(SelectedItem);
            }
        }
    }

    private IReadOnlyList<ShelfItemViewModel> GetEffectiveSelectedItems()
    {
        if (SelectedItem is not null && !SelectedItems.Contains(SelectedItem))
        {
            return [SelectedItem];
        }

        if (SelectedItems.Count > 0)
        {
            return SelectedItems.ToArray();
        }

        return SelectedItem is null ? [] : [SelectedItem];
    }

    private bool CanCopySelection()
    {
        var selectedItems = GetEffectiveSelectedItems();
        if (selectedItems.Count == 0)
        {
            return false;
        }

        if (selectedItems.Count == 1)
        {
            return selectedItems[0].CanCopy;
        }

        return selectedItems.All(item => item.Type switch
        {
            ShelfItemType.File or ShelfItemType.Folder or ShelfItemType.Image => !string.IsNullOrWhiteSpace(item.ActionPath),
            _ => !string.IsNullOrWhiteSpace(item.ClipboardText),
        });
    }

    private void CopySelectedItems(IReadOnlyList<ShelfItemViewModel> selectedItems)
    {
        var type = selectedItems[0].Type;
        if (selectedItems.Any(item => item.Type != type))
        {
            ShelfStatusMessage = _localizationService.Text.MultiCopyMixedTypes;
            return;
        }

        var copied = type switch
        {
            ShelfItemType.File => CopySelectedFilePaths(selectedItems, item => item.SourcePath),
            ShelfItemType.Folder => CopySelectedFilePaths(selectedItems, item => item.SourcePath),
            ShelfItemType.Image => CopySelectedFilePaths(selectedItems, item => item.Item.ImagePath),
            ShelfItemType.Text => CopySelectedText(selectedItems, item => item.Item.Content),
            ShelfItemType.Url => CopySelectedText(selectedItems, item => item.Item.Content),
            _ => false,
        };

        ShelfStatusMessage = copied
            ? _localizationService.CopySelectedMessage(type, selectedItems.Count)
            : _localizationService.Text.MultiCopyNoContent;
    }

    private bool CopySelectedFilePaths(IReadOnlyList<ShelfItemViewModel> selectedItems, Func<ShelfItemViewModel, string?> getPath)
    {
        var paths = selectedItems
            .Select(getPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();

        return paths.Length == selectedItems.Count && _clipboardService.SetFileDropList(paths);
    }

    private bool CopySelectedText(IReadOnlyList<ShelfItemViewModel> selectedItems, Func<ShelfItemViewModel, string?> getText)
    {
        var lines = selectedItems
            .Select(getText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Cast<string>()
            .ToArray();

        return lines.Length == selectedItems.Count && _clipboardService.SetText(string.Join(Environment.NewLine, lines));
    }

    private void AddSelectedItem(ShelfItemViewModel item)
    {
        if (SelectedItems.Contains(item))
        {
            return;
        }

        item.IsBatchSelected = true;
        SelectedItems.Add(item);
        RaiseSelectionStateChanged();
    }

    private void RemoveSelectedItem(ShelfItemViewModel item)
    {
        if (!SelectedItems.Remove(item))
        {
            return;
        }

        item.IsBatchSelected = false;
        RaiseSelectionStateChanged();
    }

    private void ClearSelectedItems()
    {
        foreach (var item in SelectedItems)
        {
            item.IsBatchSelected = false;
        }

        SelectedItems.Clear();
        RaiseSelectionStateChanged();
    }

    private void RemoveHiddenSelections()
    {
        foreach (var item in SelectedItems.Where(item => !VisibleItems.Contains(item)).ToArray())
        {
            RemoveSelectedItem(item);
        }
    }

    private void RaiseSelectionStateChanged()
    {
        OnPropertyChanged(nameof(SelectedItemCount));
        OnPropertyChanged(nameof(HasMultipleSelectedItems));
        RaiseSelectedCommandCanExecuteChanged();
    }

    private bool MatchesVisibleCriteria(ShelfItemViewModel item)
    {
        return MatchesFilter(item) && MatchesSearch(item);
    }

    private bool MatchesFilter(ShelfItemViewModel item)
    {
        return ActiveFilter switch
        {
            ShelfFilterMode.All => true,
            ShelfFilterMode.File => item.Type is ShelfItemType.File,
            ShelfFilterMode.Folder => item.Type is ShelfItemType.Folder,
            ShelfFilterMode.Text => item.Type is ShelfItemType.Text,
            ShelfFilterMode.Url => item.Type is ShelfItemType.Url,
            ShelfFilterMode.Image => item.Type is ShelfItemType.Image,
            _ => true,
        };
    }

    private bool MatchesSearch(ShelfItemViewModel item)
    {
        var query = SearchQuery.Trim();
        if (query.Length == 0)
        {
            return true;
        }

        return ContainsSearchText(item.DisplayName, query) ||
            ContainsSearchText(item.SourcePath, query) ||
            ContainsSearchText(item.Item.Content, query) ||
            ContainsSearchText(item.Item.ImagePath, query) ||
            ContainsSearchText(item.Item.ThumbnailPath, query) ||
            ContainsSearchText(item.PathSummary, query) ||
            ContainsSearchText(item.PreviewText, query);
    }

    private static bool ContainsSearchText(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private ShelfItemViewModel? FirstVisibleItemAtOrBefore(int itemIndex)
    {
        if (VisibleItems.Count == 0)
        {
            return null;
        }

        var directCandidate = itemIndex < Items.Count
            ? Items[itemIndex]
            : Items.LastOrDefault();
        if (directCandidate is not null && VisibleItems.Contains(directCandidate))
        {
            return directCandidate;
        }

        return VisibleItems.FirstOrDefault();
    }

    private void RaiseCollectionStateChanged()
    {
        IsEmpty = Items.Count == 0;
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(InvalidItemCount));
        OnPropertyChanged(nameof(DuplicateItemCount));
        OnPropertyChanged(nameof(HasInvalidItems));
        OnPropertyChanged(nameof(HasDuplicateItems));
        OnPropertyChanged(nameof(IsNoResults));
    }

    private string GetFilterModeDisplayName(ShelfFilterMode value)
    {
        return value switch
        {
            ShelfFilterMode.All => _localizationService.Text.FilterAll,
            ShelfFilterMode.File => _localizationService.Text.FilterFiles,
            ShelfFilterMode.Folder => _localizationService.Text.FilterFolders,
            ShelfFilterMode.Text => _localizationService.Text.FilterText,
            ShelfFilterMode.Url => _localizationService.Text.FilterLinks,
            ShelfFilterMode.Image => _localizationService.Text.FilterImages,
            _ => value.ToString(),
        };
    }

    private void RefreshDuplicateStates()
    {
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Items)
        {
            var isDuplicate = TryCreatePathKey(item.Item, out var key) && !seenKeys.Add(key);
            item.SetDuplicate(isDuplicate);
        }
    }

    private bool WouldDuplicateExistingPath(ShelfItem candidate, Guid excludedItemId)
    {
        if (!TryCreatePathKey(candidate, out var candidateKey))
        {
            return false;
        }

        return Items.Any(item =>
            item.Item.Id != excludedItemId &&
            TryCreatePathKey(item.Item, out var existingKey) &&
            string.Equals(candidateKey, existingKey, StringComparison.OrdinalIgnoreCase));
    }

    private static ShelfItem CreateRelinkedItem(ShelfItem current, string newPath)
    {
        return new ShelfItem
        {
            Id = current.Id,
            Type = current.Type,
            DisplayName = GetDisplayName(newPath),
            SourcePath = newPath,
            Content = current.Content,
            ImagePath = current.ImagePath,
            ThumbnailPath = current.ThumbnailPath,
            CreatedAt = current.CreatedAt,
        };
    }

    private static bool IsValidRelinkPath(ShelfItemType type, string path)
    {
        return type switch
        {
            ShelfItemType.File => File.Exists(path),
            ShelfItemType.Folder => Directory.Exists(path),
            _ => false,
        };
    }

    private static bool TryCreatePathKey(ShelfItem item, out string key)
    {
        if (item.Type is not (ShelfItemType.File or ShelfItemType.Folder) ||
            string.IsNullOrWhiteSpace(item.SourcePath))
        {
            key = string.Empty;
            return false;
        }

        key = $"{item.Type}:{NormalizePathForComparison(item.SourcePath)}";
        return true;
    }

    private static string NormalizePathForComparison(string path)
    {
        var trimmed = path.Trim();
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmed));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return trimmed.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    private static string GetDisplayName(string path)
    {
        try
        {
            var trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fileName = Path.GetFileName(trimmedPath);
            return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
        }
        catch (ArgumentException)
        {
            return path;
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
