using System.IO;
using System.Windows;
using System.Windows.Media;
using DropShelf.App.Models;
using Microsoft.Win32;
using DropShelfThemeMode = DropShelf.App.Models.ThemeMode;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfApplication = System.Windows.Application;

namespace DropShelf.App.Services;

public sealed class ThemeService
{
    private const string PersonalizeKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    public void Apply(WpfApplication application, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(settings);

        var useDarkTheme = IsDarkTheme(settings.ThemeMode);
        var resources = application.Resources;

        resources["DropShelfWindowBackground"] = Brush(useDarkTheme ? "#202428" : "#F8FAFB");
        resources["DropShelfPanelBackground"] = Brush(useDarkTheme ? "#252A2F" : "#F8FAFB");
        resources["DropShelfPanelBorderBrush"] = Brush(useDarkTheme ? "#414A52" : "#C9D2D8");
        resources["DropShelfSurfaceBrush"] = Brush(useDarkTheme ? "#2F363C" : "#FFFFFF");
        resources["DropShelfSurfaceHoverBrush"] = Brush(useDarkTheme ? "#39424A" : "#E3EEF4");
        resources["DropShelfSurfacePressedBrush"] = Brush(useDarkTheme ? "#43505A" : "#D2E2EC");
        resources["DropShelfCardBorderBrush"] = Brush(useDarkTheme ? "#47535C" : "#D7E0E6");
        resources["DropShelfSubtleBrush"] = Brush(useDarkTheme ? "#38424A" : "#EAF1F5");
        resources["DropShelfTextBrush"] = Brush(useDarkTheme ? "#F0F4F7" : "#1F2C33");
        resources["DropShelfMutedTextBrush"] = Brush(useDarkTheme ? "#B4C0C8" : "#687780");
        resources["DropShelfAccentBrush"] = Brush(useDarkTheme ? "#79B68B" : "#5FAE73");
        resources["DropShelfWarningBackgroundBrush"] = Brush(useDarkTheme ? "#49351E" : "#FFF2D8");
        resources["DropShelfWarningTextBrush"] = Brush(useDarkTheme ? "#FFD08A" : "#8A4F16");
        resources["DropShelfErrorTextBrush"] = Brush(useDarkTheme ? "#FFB3A8" : "#9A2E22");

        var comfortable = settings.DensityMode == DensityMode.Comfortable;
        resources["DropShelfPanelPadding"] = new Thickness(comfortable ? 20 : 12);
        resources["DropShelfCardPadding"] = new Thickness(comfortable ? 14 : 7);
        resources["DropShelfCardMargin"] = new Thickness(0, 0, 0, comfortable ? 12 : 5);
        resources["DropShelfCardMinHeight"] = comfortable ? 96.0 : 70.0;
        resources["DropShelfCardTextGap"] = new Thickness(0, comfortable ? 5 : 3, 0, 0);
        resources["DropShelfExpandedContentMargin"] = new Thickness(0, comfortable ? 12 : 7, 0, 0);
        resources["DropShelfExpandedContentPadding"] = new Thickness(comfortable ? 12 : 8, comfortable ? 10 : 6, comfortable ? 12 : 8, comfortable ? 10 : 6);
        resources["DropShelfDropZonePadding"] = new Thickness(comfortable ? 14 : 9);
        resources["DropShelfTypeBadgeColumnWidth"] = new GridLength(comfortable ? 48 : 34);
        resources["DropShelfTypeBadgeSize"] = comfortable ? 42.0 : 30.0;
        resources["DropShelfTypeIconFontSize"] = comfortable ? 19.0 : 15.0;
        resources["DropShelfActionButtonPadding"] = new Thickness(comfortable ? 9 : 7, comfortable ? 5 : 3, comfortable ? 9 : 7, comfortable ? 5 : 3);
        resources["DropShelfHeaderButtonSize"] = comfortable ? 34.0 : 28.0;
        resources["DropShelfHeaderButtonMargin"] = new Thickness(comfortable ? 7 : 5, 0, 0, 0);
        resources["DropShelfHeaderButtonFontSize"] = comfortable ? 14.0 : 12.0;
        resources["DropShelfTitleFontSize"] = comfortable ? 15.0 : 13.0;
        resources["DropShelfBodyFontSize"] = comfortable ? 12.5 : 11.0;
    }

    public bool IsDarkTheme(DropShelfThemeMode themeMode)
    {
        return themeMode switch
        {
            DropShelfThemeMode.Dark => true,
            DropShelfThemeMode.Light => false,
            DropShelfThemeMode.System => SystemPrefersDarkTheme(),
            _ => false,
        };
    }

    private static bool SystemPrefersDarkTheme()
    {
        try
        {
            return Registry.GetValue(PersonalizeKeyPath, AppsUseLightThemeValueName, 1) is int appsUseLightTheme
                && appsUseLightTheme == 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
