using DropShelf.App.Models;

namespace DropShelf.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private AppSettings _settings = AppSettings.CreateDefault();

    public SettingsViewModel()
    {
    }

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }
}
