using System.Collections.ObjectModel;
using DropShelf.App.Models;

namespace DropShelf.App.ViewModels;

public sealed class ShelfViewModel : ObservableObject
{
    private ShelfItem? _selectedItem;

    public ObservableCollection<ShelfItem> Items { get; } = [];

    public ShelfItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }
}
