using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;

namespace DesktopWidget.UI;

// One instance per window. XAML supplies activation, theme, and accessibility policy.
public sealed class TintedAcrylicBackdrop(double tintOpacity) : SystemBackdrop
{
    private DesktopAcrylicController? controller;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        controller = new DesktopAcrylicController();
        controller.SetSystemBackdropConfiguration(GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot));
        controller.TintOpacity = (float)Math.Clamp(tintOpacity, 0, 1);
        controller.AddSystemBackdropTarget(connectedTarget);
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        if (controller is not null)
        {
            controller.RemoveSystemBackdropTarget(disconnectedTarget);
            controller.Dispose();
            controller = null;
        }
        base.OnTargetDisconnected(disconnectedTarget);
    }
}
