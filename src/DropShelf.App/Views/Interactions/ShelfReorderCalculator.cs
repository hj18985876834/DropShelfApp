namespace DropShelf.App.Views.Interactions;

public static class ShelfReorderCalculator
{
    public static ShelfReorderCalculation Calculate(
        IReadOnlyList<ShelfReorderItemLayout> layouts,
        int sourceIndex,
        double dragCenterY,
        double sourceShiftHeight)
    {
        ArgumentNullException.ThrowIfNull(layouts);

        if (sourceIndex < 0 || layouts.Count == 0)
        {
            return new ShelfReorderCalculation(-1, new Dictionary<int, double>());
        }

        var targetIndex = GetTargetIndex(layouts, sourceIndex, dragCenterY);
        if (targetIndex < 0)
        {
            return new ShelfReorderCalculation(-1, new Dictionary<int, double>());
        }

        var offsets = CalculateOffsets(layouts, sourceIndex, targetIndex, dragCenterY, sourceShiftHeight);
        return new ShelfReorderCalculation(targetIndex, offsets);
    }

    private static int GetTargetIndex(
        IReadOnlyList<ShelfReorderItemLayout> layouts,
        int sourceIndex,
        double dragCenterY)
    {
        var sourceLayoutPosition = IndexOf(layouts, sourceIndex);
        if (sourceLayoutPosition >= 0)
        {
            var sourceLayout = layouts[sourceLayoutPosition];
            if (dragCenterY >= sourceLayout.Midpoint)
            {
                var targetIndex = sourceIndex;
                for (var index = sourceLayoutPosition + 1; index < layouts.Count; index++)
                {
                    var layout = layouts[index];
                    if (dragCenterY < layout.Midpoint)
                    {
                        break;
                    }

                    targetIndex = layout.Index;
                }

                return targetIndex;
            }

            var upwardTargetIndex = sourceIndex;
            for (var index = sourceLayoutPosition - 1; index >= 0; index--)
            {
                var layout = layouts[index];
                if (dragCenterY > layout.Midpoint)
                {
                    break;
                }

                upwardTargetIndex = layout.Index;
            }

            return upwardTargetIndex;
        }

        var firstLayout = layouts[0];
        if (dragCenterY < firstLayout.Midpoint)
        {
            return firstLayout.Index;
        }

        var lastCandidate = firstLayout.Index;
        for (var index = 1; index < layouts.Count; index++)
        {
            var layout = layouts[index];
            if (dragCenterY < layout.Midpoint)
            {
                break;
            }

            lastCandidate = layout.Index;
        }

        return lastCandidate;
    }

    private static IReadOnlyDictionary<int, double> CalculateOffsets(
        IReadOnlyList<ShelfReorderItemLayout> layouts,
        int sourceIndex,
        int targetIndex,
        double dragCenterY,
        double sourceShiftHeight)
    {
        var offsets = layouts.ToDictionary(layout => layout.Index, _ => 0d);
        var sourceLayoutPosition = IndexOf(layouts, sourceIndex);
        if (sourceLayoutPosition >= 0)
        {
            if (dragCenterY >= layouts[sourceLayoutPosition].Midpoint)
            {
                ApplyDownwardOffsets(layouts, sourceLayoutPosition, dragCenterY, offsets);
            }
            else
            {
                ApplyUpwardOffsets(layouts, sourceLayoutPosition, dragCenterY, offsets);
            }

            return offsets;
        }

        if (targetIndex == sourceIndex)
        {
            return new Dictionary<int, double>();
        }

        var fallbackShiftHeight = Math.Max(1, sourceShiftHeight);
        foreach (var layout in layouts)
        {
            if (targetIndex > sourceIndex &&
                layout.Index > sourceIndex &&
                layout.Index <= targetIndex)
            {
                offsets[layout.Index] = -fallbackShiftHeight;
            }
            else if (targetIndex < sourceIndex &&
                layout.Index >= targetIndex &&
                layout.Index < sourceIndex)
            {
                offsets[layout.Index] = fallbackShiftHeight;
            }
        }

        return offsets;
    }

    private static void ApplyDownwardOffsets(
        IReadOnlyList<ShelfReorderItemLayout> layouts,
        int sourceLayoutPosition,
        double dragCenterY,
        IDictionary<int, double> offsets)
    {
        var previousBoundary = layouts[sourceLayoutPosition].Midpoint;
        for (var index = sourceLayoutPosition + 1; index < layouts.Count; index++)
        {
            var layout = layouts[index];
            var segmentLength = Math.Max(1, layout.Midpoint - previousBoundary);
            var progress = Math.Clamp((dragCenterY - previousBoundary) / segmentLength, 0, 1);
            if (progress <= 0)
            {
                break;
            }

            offsets[layout.Index] = -layout.ShiftHeight * progress;
            if (progress < 1)
            {
                break;
            }

            previousBoundary = layout.Midpoint;
        }
    }

    private static void ApplyUpwardOffsets(
        IReadOnlyList<ShelfReorderItemLayout> layouts,
        int sourceLayoutPosition,
        double dragCenterY,
        IDictionary<int, double> offsets)
    {
        var nextBoundary = layouts[sourceLayoutPosition].Midpoint;
        for (var index = sourceLayoutPosition - 1; index >= 0; index--)
        {
            var layout = layouts[index];
            var segmentLength = Math.Max(1, nextBoundary - layout.Midpoint);
            var progress = Math.Clamp((nextBoundary - dragCenterY) / segmentLength, 0, 1);
            if (progress <= 0)
            {
                break;
            }

            offsets[layout.Index] = layout.ShiftHeight * progress;
            if (progress < 1)
            {
                break;
            }

            nextBoundary = layout.Midpoint;
        }
    }

    private static int IndexOf(IReadOnlyList<ShelfReorderItemLayout> layouts, int visibleIndex)
    {
        for (var index = 0; index < layouts.Count; index++)
        {
            if (layouts[index].Index == visibleIndex)
            {
                return index;
            }
        }

        return -1;
    }
}

public sealed record ShelfReorderCalculation(
    int TargetIndex,
    IReadOnlyDictionary<int, double> Offsets);

public sealed record ShelfReorderItemLayout(
    int Index,
    double Top,
    double Height,
    double ShiftHeight)
{
    public double Midpoint => Top + (Height / 2);
}
