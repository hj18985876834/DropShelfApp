using System.Collections.ObjectModel;
using System.Windows.Input;
using DropShelf.App.Commands;
using DropShelf.App.Models;

namespace DropShelf.App.ViewModels;

public sealed class ShelfViewModel : ObservableObject
{
    private readonly Action? _openSettings;
    private bool _isShelfVisible;
    private ShelfItem? _selectedItem;

    public ShelfViewModel(Action? openSettings = null, IEnumerable<ShelfItem>? initialItems = null)
    {
        _openSettings = openSettings;
        ShowShelfCommand = new RelayCommand(_ => IsShelfVisible = true);
        HideShelfCommand = new RelayCommand(_ => IsShelfVisible = false);
        ToggleShelfCommand = new RelayCommand(_ => IsShelfVisible = !IsShelfVisible);
        OpenSettingsCommand = new RelayCommand(_ => _openSettings?.Invoke());

        if (initialItems is not null)
        {
            foreach (var item in initialItems)
            {
                Items.Add(item);
            }
        }
    }

    public ObservableCollection<ShelfItem> Items { get; } = [];

    public bool IsShelfVisible
    {
        get => _isShelfVisible;
        set => SetProperty(ref _isShelfVisible, value);
    }

    public ShelfItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public ICommand ShowShelfCommand { get; }

    public ICommand HideShelfCommand { get; }

    public ICommand ToggleShelfCommand { get; }

    public ICommand OpenSettingsCommand { get; }
}
