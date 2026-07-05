using System.Windows;
using DropShelf.App.Models;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;

namespace DropShelf.App.Services;

public sealed class WindowDockService
{
    public const double HandleThickness = 36;
    public const double HandleLength = 132;
    public const double HorizontalHandleLength = 132;
    public const double PanelWidth = 360;
    public const double PanelHeight = 520;

    public void ApplyHandle(Window window, DockEdge dockEdge, double dockOffsetRatio)
    {
        Apply(window, dockEdge, dockOffsetRatio, isShelfVisible: false);
    }

    public void ApplyPanel(Window panelWindow, Window handleWindow, DockEdge dockEdge)
    {
        ArgumentNullException.ThrowIfNull(panelWindow);
        ArgumentNullException.ThrowIfNull(handleWindow);

        panelWindow.Width = PanelWidth;
        panelWindow.Height = PanelHeight;

        switch (dockEdge)
        {
            case DockEdge.Left:
                panelWindow.Left = handleWindow.Left + handleWindow.Width;
                panelWindow.Top = handleWindow.Top + (handleWindow.Height / 2) - (panelWindow.Height / 2);
                break;
            case DockEdge.Right:
                panelWindow.Left = handleWindow.Left - panelWindow.Width;
                panelWindow.Top = handleWindow.Top + (handleWindow.Height / 2) - (panelWindow.Height / 2);
                break;
            case DockEdge.Top:
                panelWindow.Left = handleWindow.Left + (handleWindow.Width / 2) - (panelWindow.Width / 2);
                panelWindow.Top = handleWindow.Top + handleWindow.Height;
                break;
            case DockEdge.Bottom:
                panelWindow.Left = handleWindow.Left + (handleWindow.Width / 2) - (panelWindow.Width / 2);
                panelWindow.Top = handleWindow.Top - panelWindow.Height;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dockEdge), dockEdge, null);
        }

        ClampWindowToWorkArea(panelWindow);
    }

    public void Apply(Window window, DockEdge dockEdge, double dockOffsetRatio, bool isShelfVisible)
    {
        ArgumentNullException.ThrowIfNull(window);

        var workArea = SystemParameters.WorkArea;
        var width = GetWindowWidth(dockEdge, isShelfVisible);
        var height = GetWindowHeight(dockEdge, isShelfVisible);
        var centerRatio = ClampRatio(dockOffsetRatio);

        window.Width = width;
        window.Height = height;

        switch (dockEdge)
        {
            case DockEdge.Left:
                window.Left = workArea.Left;
                window.Top = Clamp(workArea.Top + (workArea.Height * centerRatio) - (height / 2), workArea.Top, workArea.Bottom - height);
                break;
            case DockEdge.Right:
                window.Left = workArea.Right - width;
                window.Top = Clamp(workArea.Top + (workArea.Height * centerRatio) - (height / 2), workArea.Top, workArea.Bottom - height);
                break;
            case DockEdge.Top:
                window.Left = Clamp(workArea.Left + (workArea.Width * centerRatio) - (width / 2), workArea.Left, workArea.Right - width);
                window.Top = workArea.Top;
                break;
            case DockEdge.Bottom:
                window.Left = Clamp(workArea.Left + (workArea.Width * centerRatio) - (width / 2), workArea.Left, workArea.Right - width);
                window.Top = workArea.Bottom - height;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dockEdge), dockEdge, null);
        }
    }

    public void Apply(Window window, DockEdge dockEdge, bool isShelfVisible)
    {
        Apply(window, dockEdge, AppSettings.CreateDefault().DockOffsetRatio, isShelfVisible);
    }

    public void PlaceCollapsedAt(Window window, WpfPoint screenCenter)
    {
        ArgumentNullException.ThrowIfNull(window);

        var workArea = SystemParameters.WorkArea;
        var left = screenCenter.X - (window.Width / 2);
        var top = screenCenter.Y - (window.Height / 2);

        PlaceAt(window, left, top);
    }

    public void PlaceAt(Window window, double left, double top)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.Left = left;
        window.Top = top;
        ClampWindowToWorkArea(window);
    }

    public DockPlacement SnapToNearestEdge(WpfPoint screenCenter)
    {
        return SnapToNearestEdge(screenCenter, SystemParameters.WorkArea);
    }

    public DockPlacement SnapToNearestEdge(WpfPoint screenCenter, WpfRect workArea)
    {
        var distances = new (DockEdge Edge, double Distance)[]
        {
            (DockEdge.Left, Math.Abs(screenCenter.X - workArea.Left)),
            (DockEdge.Right, Math.Abs(workArea.Right - screenCenter.X)),
            (DockEdge.Top, Math.Abs(screenCenter.Y - workArea.Top)),
            (DockEdge.Bottom, Math.Abs(workArea.Bottom - screenCenter.Y)),
        };

        var nearestEdge = distances.MinBy(item => item.Distance).Edge;
        var ratio = nearestEdge is DockEdge.Left or DockEdge.Right
            ? (screenCenter.Y - workArea.Top) / workArea.Height
            : (screenCenter.X - workArea.Left) / workArea.Width;

        return new DockPlacement(nearestEdge, ClampRatio(ratio));
    }

    public static double GetWindowWidth(DockEdge dockEdge, bool isShelfVisible)
    {
        if (dockEdge is DockEdge.Top or DockEdge.Bottom)
        {
            return isShelfVisible ? PanelWidth : HorizontalHandleLength;
        }

        return isShelfVisible ? PanelWidth + HandleThickness : HandleThickness;
    }

    public static double GetWindowHeight(DockEdge dockEdge, bool isShelfVisible)
    {
        if (dockEdge is DockEdge.Top or DockEdge.Bottom)
        {
            return isShelfVisible ? PanelHeight + HandleThickness : HandleThickness;
        }

        return isShelfVisible ? PanelHeight : HandleLength;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (maximum < minimum)
        {
            return minimum;
        }

        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static double ClampRatio(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return AppSettings.CreateDefault().DockOffsetRatio;
        }

        return Clamp(value, 0, 1);
    }

    private static void ClampWindowToWorkArea(Window window)
    {
        var workArea = SystemParameters.WorkArea;
        window.Left = Clamp(window.Left, workArea.Left, workArea.Right - window.Width);
        window.Top = Clamp(window.Top, workArea.Top, workArea.Bottom - window.Height);
    }
}
