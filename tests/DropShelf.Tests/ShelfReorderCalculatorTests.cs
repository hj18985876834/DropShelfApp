using DropShelf.App.Views.Interactions;

namespace DropShelf.Tests;

[TestClass]
public sealed class ShelfReorderCalculatorTests
{
    [TestMethod]
    public void Calculate_KeepsTargetButStartsVisualShiftBeforeCrossingNextMidpoint()
    {
        var result = ShelfReorderCalculator.Calculate(CreateUniformLayouts(3), sourceIndex: 0, dragCenterY: 149, sourceShiftHeight: 100);

        Assert.AreEqual(0, result.TargetIndex);
        Assert.AreEqual(-99, result.Offsets[1], 0.001);
        Assert.AreEqual(0, result.Offsets[2], 0.001);
    }

    [TestMethod]
    public void Calculate_MovesDownAfterCrossingNextMidpoint()
    {
        var result = ShelfReorderCalculator.Calculate(CreateUniformLayouts(3), sourceIndex: 0, dragCenterY: 151, sourceShiftHeight: 100);

        Assert.AreEqual(1, result.TargetIndex);
        Assert.AreEqual(-100, result.Offsets[1], 0.001);
        Assert.AreEqual(-1, result.Offsets[2], 0.001);
    }

    [TestMethod]
    public void Calculate_MovesDownAcrossMultipleItems()
    {
        var result = ShelfReorderCalculator.Calculate(CreateUniformLayouts(4), sourceIndex: 0, dragCenterY: 260, sourceShiftHeight: 100);

        Assert.AreEqual(2, result.TargetIndex);
        Assert.AreEqual(-100, result.Offsets[1], 0.001);
        Assert.AreEqual(-100, result.Offsets[2], 0.001);
        Assert.AreEqual(-10, result.Offsets[3], 0.001);
    }

    [TestMethod]
    public void Calculate_MovesUpAfterCrossingPreviousMidpoint()
    {
        var result = ShelfReorderCalculator.Calculate(CreateUniformLayouts(3), sourceIndex: 2, dragCenterY: 149, sourceShiftHeight: 100);

        Assert.AreEqual(1, result.TargetIndex);
        Assert.AreEqual(1, result.Offsets[0], 0.001);
        Assert.AreEqual(100, result.Offsets[1], 0.001);
    }

    [TestMethod]
    public void Calculate_MovesUpAcrossMultipleItems()
    {
        var result = ShelfReorderCalculator.Calculate(CreateUniformLayouts(4), sourceIndex: 3, dragCenterY: 120, sourceShiftHeight: 100);

        Assert.AreEqual(1, result.TargetIndex);
        Assert.AreEqual(30, result.Offsets[0], 0.001);
        Assert.AreEqual(100, result.Offsets[1], 0.001);
        Assert.AreEqual(100, result.Offsets[2], 0.001);
    }

    [TestMethod]
    public void Calculate_UsesEachCardsShiftHeight()
    {
        ShelfReorderItemLayout[] layouts =
        [
            new(0, 0, 80, 90),
            new(1, 90, 120, 130),
            new(2, 220, 70, 80),
        ];

        var result = ShelfReorderCalculator.Calculate(layouts, sourceIndex: 0, dragCenterY: 155, sourceShiftHeight: 90);

        Assert.AreEqual(1, result.TargetIndex);
        Assert.AreEqual(-130, result.Offsets[1], 0.001);
        Assert.AreEqual(-3.81, result.Offsets[2], 0.01);
    }

    [TestMethod]
    public void Calculate_WhenSourceIsNotRealized_UsesVisibleTargetAndFallbackShift()
    {
        ShelfReorderItemLayout[] layouts =
        [
            new(5, 0, 100, 100),
            new(6, 100, 100, 100),
            new(7, 200, 100, 100),
        ];

        var result = ShelfReorderCalculator.Calculate(layouts, sourceIndex: 2, dragCenterY: 260, sourceShiftHeight: 96);

        Assert.AreEqual(7, result.TargetIndex);
        Assert.AreEqual(-96, result.Offsets[5], 0.001);
        Assert.AreEqual(-96, result.Offsets[6], 0.001);
        Assert.AreEqual(-96, result.Offsets[7], 0.001);
    }

    [TestMethod]
    public void Calculate_ReturnsInvalidTargetForInvalidSource()
    {
        var result = ShelfReorderCalculator.Calculate(CreateUniformLayouts(3), sourceIndex: -1, dragCenterY: 100, sourceShiftHeight: 100);

        Assert.AreEqual(-1, result.TargetIndex);
        Assert.IsEmpty(result.Offsets);
    }

    private static ShelfReorderItemLayout[] CreateUniformLayouts(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new ShelfReorderItemLayout(index, index * 100, 100, 100))
            .ToArray();
    }
}
