using System.Windows;
using DropShelf.App.Models;

namespace DropShelf.App.Services;

public sealed class WindowDockService
{
    public const double HandleThickness = 36;
    public const double PanelWidth = 360;
    public const double PanelHeight = 520;

    private const double EdgeMargin = 12;

    public void Apply(Window window, DockEdge dockEdge, bool isShelfVisible)
    {
        ArgumentNullException.ThrowIfNull(window);

        var workArea = SystemParameters.WorkArea;
        var width = GetWindowWidth(dockEdge, isShelfVisible);
        var height = GetWindowHeight(dockEdge, isShelfVisible);

        window.Width = width;
        window.Height = height;

        switch (dockEdge)
        {
            case DockEdge.Left:
                window.Left = workArea.Left;
                window.Top = Center(workArea.Top, workArea.Height, height);
                break;
            case DockEdge.Right:
                window.Left = workArea.Right - width;
                window.Top = Center(workArea.Top, workArea.Height, height);
                break;
            case DockEdge.Top:
                window.Left = Center(workArea.Left, workArea.Width, width);
                window.Top = workArea.Top;
                break;
            case DockEdge.Bottom:
                window.Left = Center(workArea.Left, workArea.Width, width);
                window.Top = workArea.Bottom - height;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dockEdge), dockEdge, null);
        }
    }

    private static double GetWindowWidth(DockEdge dockEdge, bool isShelfVisible)
    {
        if (dockEdge is DockEdge.Top or DockEdge.Bottom)
        {
            return isShelfVisible ? PanelWidth : 160;
        }

        return isShelfVisible ? PanelWidth + HandleThickness : HandleThickness;
    }

    private static double GetWindowHeight(DockEdge dockEdge, bool isShelfVisible)
    {
        if (dockEdge is DockEdge.Top or DockEdge.Bottom)
        {
            return isShelfVisible ? PanelHeight + HandleThickness : HandleThickness;
        }

        return PanelHeight;
    }

    private static double Center(double start, double length, double size)
    {
        return Math.Max(start + EdgeMargin, start + (length - size) / 2);
    }
}
