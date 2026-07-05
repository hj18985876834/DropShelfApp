using System.Windows;
using DropShelf.App.Models;
using DropShelf.App.Services;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;

namespace DropShelf.Tests;

[TestClass]
public sealed class WindowDockServiceTests
{
    [TestMethod]
    public void SnapToNearestEdge_ChoosesNearestHorizontalEdgeAndRatio()
    {
        var service = new WindowDockService();
        var workArea = new WpfRect(0, 0, 1000, 800);

        var placement = service.SnapToNearestEdge(new WpfPoint(760, 790), workArea);

        Assert.AreEqual(DockEdge.Bottom, placement.DockEdge);
        Assert.AreEqual(0.76, placement.DockOffsetRatio, 0.001);
    }

    [TestMethod]
    public void SnapToNearestEdge_ChoosesNearestVerticalEdgeAndRatio()
    {
        var service = new WindowDockService();
        var workArea = new WpfRect(0, 0, 1000, 800);

        var placement = service.SnapToNearestEdge(new WpfPoint(990, 200), workArea);

        Assert.AreEqual(DockEdge.Right, placement.DockEdge);
        Assert.AreEqual(0.25, placement.DockOffsetRatio, 0.001);
    }

    [TestMethod]
    public void SnapToNearestEdge_ClampsRatioToWorkArea()
    {
        var service = new WindowDockService();
        var workArea = new WpfRect(0, 0, 1000, 800);

        var placement = service.SnapToNearestEdge(new WpfPoint(-20, -40), workArea);

        Assert.AreEqual(DockEdge.Left, placement.DockEdge);
        Assert.AreEqual(0, placement.DockOffsetRatio);
    }

    [TestMethod]
    public void CollapsedHandleSizes_AreStableForVerticalAndHorizontalEdges()
    {
        Assert.AreEqual(WindowDockService.HandleThickness, WindowDockService.GetWindowWidth(DockEdge.Left, isShelfVisible: false));
        Assert.AreEqual(WindowDockService.HandleLength, WindowDockService.GetWindowHeight(DockEdge.Left, isShelfVisible: false));
        Assert.AreEqual(WindowDockService.HorizontalHandleLength, WindowDockService.GetWindowWidth(DockEdge.Top, isShelfVisible: false));
        Assert.AreEqual(WindowDockService.HandleThickness, WindowDockService.GetWindowHeight(DockEdge.Top, isShelfVisible: false));
    }

    [TestMethod]
    public void SnapToNearestEdge_AllowsCornerRatiosWithoutArtificialSafeMargin()
    {
        var service = new WindowDockService();
        var workArea = new WpfRect(0, 0, 1000, 800);

        var topLeft = service.SnapToNearestEdge(new WpfPoint(0, 0), workArea);
        var bottomRight = service.SnapToNearestEdge(new WpfPoint(1000, 800), workArea);

        Assert.AreEqual(0, topLeft.DockOffsetRatio);
        Assert.AreEqual(1, bottomRight.DockOffsetRatio);
    }
}
