using System.Globalization;
using System.Windows.Data;
using DropShelf.App.Models;
using DropShelf.App.ViewModels;
using WpfBinding = System.Windows.Data.Binding;

namespace DropShelf.App.Converters;

public sealed class LanguageModeDisplayConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 ||
            values[0] is not LanguageMode languageMode ||
            values[1] is not SettingsViewModel viewModel)
        {
            return string.Empty;
        }

        return viewModel.GetLanguageModeDisplayName(languageMode);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return targetTypes.Select(_ => WpfBinding.DoNothing).ToArray();
    }
}
