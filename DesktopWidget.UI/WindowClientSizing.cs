using System;

namespace DesktopWidget.UI;

internal readonly record struct WindowClientSizing(double Scale, int ClientWidth, int MaxClientHeight)
{
    public double MaxLogicalHeight => MaxClientHeight / Scale;

    public static WindowClientSizing Calculate(double logicalWidth, double nativeScale, int availableWidth, int availableHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nativeScale);
        availableWidth = Math.Max(1, availableWidth);
        availableHeight = Math.Max(1, availableHeight);
        // Preserve the layout width even on a narrow screen by scaling the whole canvas uniformly.
        var scale = Math.Min(nativeScale, availableWidth / logicalWidth);
        var width = Math.Min(availableWidth, (int)Math.Ceiling(logicalWidth * scale));
        return new(scale, width, availableHeight);
    }

    public int ClientHeight(double logicalHeight) =>
        (int)Math.Clamp(Math.Ceiling(logicalHeight * Scale), 1, MaxClientHeight);
}
