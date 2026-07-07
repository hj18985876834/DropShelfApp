namespace DropShelf.App.ViewModels;

public sealed class LocalizedOption<T> : ObservableObject
{
    private string _displayName;

    public LocalizedOption(T value, string displayName)
    {
        Value = value;
        _displayName = displayName;
    }

    public T Value { get; }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }
}
