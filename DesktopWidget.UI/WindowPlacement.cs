using System;

namespace DesktopWidget.UI;

internal readonly record struct WindowPlacement(int X, int Y, int Width, int Height)
{
    public static WindowPlacement Fit(int? x, int? y, int width, int height, WindowPlacement workArea, int gap)
    {
        width = Math.Clamp(width, 1, Math.Max(1, workArea.Width));
        height = Math.Clamp(height, 1, Math.Max(1, workArea.Height));
        // Reduce the margin when the window fills a small work area.
        var horizontalGap = Math.Min(Math.Max(0, gap), Math.Max(0, (workArea.Width - width) / 2));
        var verticalGap = Math.Min(Math.Max(0, gap), Math.Max(0, (workArea.Height - height) / 2));
        var left = workArea.X + horizontalGap;
        var top = workArea.Y + verticalGap;
        var right = workArea.X + Math.Max(1, workArea.Width) - width - horizontalGap;
        var bottom = workArea.Y + Math.Max(1, workArea.Height) - height - verticalGap;
        var hasPosition = x.HasValue && y.HasValue;
        return new(
            hasPosition ? Math.Clamp(x!.Value, left, right) : right,
            hasPosition ? Math.Clamp(y!.Value, top, bottom) : bottom,
            width, height);
    }
}
