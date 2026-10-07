using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
namespace DesktopWidget.UI;
public sealed partial class MainWindow : Window
{
    private readonly UsageService service = new();
    private readonly DemoUsageService? demo;
    private DateTimeOffset UsageNow => demo?.Now ?? DateTimeOffset.Now;
    private readonly CancellationTokenSource lifetime = new();
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromSeconds(1) };
    public UsageViewModel ViewModel { get; } = new();
    private readonly TrayIndicator tray;
    private IReadOnlyList<UsagePeriod> periods = Array.Empty<UsagePeriod>();
    private DateTimeOffset nextRefresh = DateTimeOffset.MinValue;
    private DateTimeOffset? updated;
    private bool busy, stale, exiting;
    private Task activeRefresh = Task.CompletedTask;
    private int lastExpiredMask = -1;
    private int tintOpacityPercent = -1;
    private readonly AppSettings settings;
    private PointInt32? windowPosition;
    private volatile bool dragging;
    private PointInt32 dragStartPosition;
    private readonly InputNonClientPointerSource nonClientInput;
    private RectInt32[] captionRegions = Array.Empty<RectInt32>();
    private RectInt32[] buttonRegions = Array.Empty<RectInt32>();
    private XamlRoot? xamlRoot;
    private double layoutScale;
    private double windowScale;
    private bool positioning;
    private bool positionQueued;

    private readonly IntPtr hwnd;
    public MainWindow(DemoUsageService? demo = null)
    {
        this.demo = demo;
        settings = AppSettings.Load(readOnly: demo is not null);
        if (settings.WindowX is int x && settings.WindowY is int y)
            windowPosition = new PointInt32(x, y);
        InitializeComponent();
        tintOpacityPercent = settings.TintOpacityPercent;
        ViewModel.SelectedInterval = ViewModel.Intervals.First(interval => interval.Seconds == settings.RefreshIntervalSeconds);
        if (demo is not null)
        {
            clock.Interval = TimeSpan.FromMilliseconds(100);
        }
        ApplyTintOpacity();
        foreach (var percent in new[] { -1, 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 })
        {
            var item = new ToggleMenuFlyoutItem
            {
                Text = percent < 0 ? "System default" : $"{percent}%",
                Tag = percent,
                IsChecked = percent == tintOpacityPercent
            };
            item.Click += TintOpacityClick;
            TintOpacityMenu.Items.Add(item);
        }
        // Recreate with the new theme's defaults before applying the chosen opacity.
        RootLayout.ActualThemeChanged += (_, _) => ApplyTintOpacity();
        foreach (var interval in ViewModel.Intervals)
        {
            var item = new ToggleMenuFlyoutItem
            {
                Text = interval.Label,
                Tag = interval,
                IsChecked = interval == ViewModel.SelectedInterval
            };
            item.Click += UpdateIntervalClick;
            UpdateIntervalMenu.Items.Add(item);
        }
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UsageViewModel.SelectedInterval))
            {
                if (demo is null)
                    nextRefresh = DateTimeOffset.UtcNow.AddSeconds(ViewModel.SelectedInterval.Seconds);
                SyncIntervalMenu();
                if (demo is null)
                {
                    settings.RefreshIntervalSeconds = ViewModel.SelectedInterval.Seconds;
                    settings.Save();
                }
            }
        };
        hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        ExtendsContentIntoTitleBar = true;
        RootLayout.Loaded += RootLayoutLoaded;
        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        AppWindow.IsShownInSwitchers = false;
        nonClientInput = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        nonClientInput.EnteringMoveSize += NativeMoveStarting;
        nonClientInput.ExitedMoveSize += NativeMoveEnded;
        // Geometry is valid only after layout; cache regions to avoid resetting them on every tick.
        Header.LayoutUpdated += HeaderLayoutUpdated;
        // Move before measuring so a restored monitor's DPI determines the size.
        if (windowPosition is PointInt32 savedPosition) AppWindow.Move(savedPosition);
        var corner = 2;
        DwmSetWindowAttribute(hwnd, 33, ref corner, sizeof(int));
        tray = new TrayIndicator(() => DispatcherQueue.TryEnqueue(Toggle), () => DispatcherQueue.TryEnqueue(async () => await RefreshAsync()), () => DispatcherQueue.TryEnqueue(Exit));
        AppWindow.Closing += (_, e) => { if (!exiting) { e.Cancel = true; AppWindow.Hide(); } };
        AppWindow.Changed += AppWindowChanged;
        Closed += (_, _) =>
        {
            lifetime.Cancel();
            clock.Stop();
            tray.Dispose();
            RootLayout.Loaded -= RootLayoutLoaded;
            AppWindow.Changed -= AppWindowChanged;
            if (xamlRoot is not null) xamlRoot.Changed -= XamlRootChanged;
            nonClientInput.EnteringMoveSize -= NativeMoveStarting;
            nonClientInput.ExitedMoveSize -= NativeMoveEnded;
            Header.LayoutUpdated -= HeaderLayoutUpdated;
        };
        clock.Tick += async (_, _) =>
        {
            if (exiting) return;
            if (AppWindow.IsVisible)
            {
                ViewModel.Tick(UsageNow);
                Position();
            }
            UpdateTray();
            if (demo?.IsComplete != true && DateTimeOffset.UtcNow >= nextRefresh) await RefreshAsync();
        };
        Position();
        clock.Start();
        _ = RefreshAsync();
    }
    private void RootLayoutLoaded(object sender, RoutedEventArgs e)
    {
        if (xamlRoot is not null) xamlRoot.Changed -= XamlRootChanged;
        xamlRoot = RootLayout.XamlRoot;
        if (xamlRoot is not null) xamlRoot.Changed += XamlRootChanged;
        // The first measurement ran before XAML was attached to a display.
        layoutScale = 0;
        QueuePosition();
    }
    private void XamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        // Changed also fires for our own resize; only scale changes need remeasurement.
        if (sender.RasterizationScale != layoutScale) QueuePosition();
    }
    private void AppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        // Moving between monitors changes native DPI before XAML necessarily reports it.
        if (!positioning && (args.DidPositionChange || args.DidSizeChange)
            && GetDpiForWindow(hwnd) / 96.0 != windowScale) QueuePosition();
    }
    private void QueuePosition()
    {
        if (exiting || positionQueued) return;
        positionQueued = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            positionQueued = false;
            if (!exiting) Position();
        });
    }
    private static void InvalidateLayout(UIElement element)
    {
        // Dirty descendants too: measuring the root again can reuse cached text/row sizes.
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
            if (VisualTreeHelper.GetChild(element, index) is UIElement child) InvalidateLayout(child);
        element.InvalidateMeasure();
        element.InvalidateArrange();
    }
    private void Position()
    {
        if (exiting || positioning) return;
        var dpi = GetDpiForWindow(hwnd);
        var nativeScale = (dpi == 0 ? 96 : dpi) / 96.0;
        // Resize for a DPI transition even inside the native move loop. Leave its position alone.
        if (dragging && nativeScale == windowScale) return;
        positioning = true;
        try
        {
            windowScale = nativeScale;
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
            var chromeWidth = Math.Max(0, AppWindow.Size.Width - AppWindow.ClientSize.Width);
            var chromeHeight = Math.Max(0, AppWindow.Size.Height - AppWindow.ClientSize.Height);
            var gap = (int)Math.Ceiling(12 * nativeScale);
            var sizing = WindowClientSizing.Calculate(RootLayout.Width, nativeScale,
                area.Width - chromeWidth, area.Height - chromeHeight - 2 * gap);

            // Elements always measure in the same logical canvas, regardless of HWND/XAML DPI timing.
            RootLayout.MaxHeight = sizing.MaxLogicalHeight;
            var scale = xamlRoot?.RasterizationScale ?? nativeScale;
            if (scale != layoutScale)
            {
                layoutScale = scale;
                InvalidateLayout(RootLayout);
            }
            RootLayout.Measure(new Windows.Foundation.Size(RootLayout.Width, double.PositiveInfinity));
            var clientSize = new SizeInt32(sizing.ClientWidth, sizing.ClientHeight(RootLayout.DesiredSize.Height));
            // Resize the usable area, not the outer rectangle (which includes non-client chrome).
            if (AppWindow.ClientSize.Width != clientSize.Width || AppWindow.ClientSize.Height != clientSize.Height)
                AppWindow.ResizeClient(clientSize);

            if (dragging) return;
            var placement = WindowPlacement.Fit(windowPosition?.X, windowPosition?.Y,
                AppWindow.Size.Width, AppWindow.Size.Height,
                new WindowPlacement(area.X, area.Y, area.Width, area.Height), gap);
            if (AppWindow.Position.X != placement.X || AppWindow.Position.Y != placement.Y)
                AppWindow.Move(new PointInt32(placement.X, placement.Y));
            if (windowPosition.HasValue)
            {
                windowPosition = AppWindow.Position;
                RememberPosition();
            }
        }
        finally { positioning = false; }
    }
    private void HeaderLayoutUpdated(object? sender, object e) => UpdateNativeDragRegions();
    private void UpdateNativeDragRegions()
    {
        if (dragging || exiting || Header.XamlRoot is null || Header.ActualWidth <= 0 || Header.ActualHeight <= 0) return;
        var scale = Header.XamlRoot.RasterizationScale;
        RectInt32 Bounds(FrameworkElement element)
        {
            // Include the Viewbox transform before converting to physical client coordinates.
            var bounds = element.TransformToVisual(ScalingView).TransformBounds(
                new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
            // Native regions use physical client pixels; round outward and clip to the window.
            var left = Math.Clamp((int)Math.Floor(bounds.Left * scale), 0, AppWindow.ClientSize.Width);
            var top = Math.Clamp((int)Math.Floor(bounds.Top * scale), 0, AppWindow.ClientSize.Height);
            var right = Math.Clamp((int)Math.Ceiling(bounds.Right * scale), left, AppWindow.ClientSize.Width);
            var bottom = Math.Clamp((int)Math.Ceiling(bounds.Bottom * scale), top, AppWindow.ClientSize.Height);
            return new RectInt32(left, top, right - left, bottom - top);
        }
        var caption = new[] { Bounds(Header) };
        var buttons = new[] { Bounds(RefreshArea), Bounds(SettingsButton), Bounds(HideButton) };
        if (captionRegions.SequenceEqual(caption) && buttonRegions.SequenceEqual(buttons)) return;
        nonClientInput.SetRegionRects(NonClientRegionKind.Caption, caption);
        // Passthrough regions keep the complete button bounds interactive, including the refresh spinner.
        nonClientInput.SetRegionRects(NonClientRegionKind.Passthrough, buttons);
        captionRegions = caption;
        buttonRegions = buttons;
    }
    private void NativeMoveStarting(InputNonClientPointerSource sender, EnteringMoveSizeEventArgs args)
    {
        dragStartPosition = AppWindow.Position;
        dragging = true;
    }
    private void NativeMoveEnded(InputNonClientPointerSource sender, ExitedMoveSizeEventArgs args)
    {
        var start = dragStartPosition;
        var end = AppWindow.Position;
        void CompleteMove()
        {
            dragging = false;
            if (exiting) return;
            if (end.X != start.X || end.Y != start.Y) windowPosition = end;
            Position();
            UpdateNativeDragRegions();
        }
        // Keep layout and settings work on the XAML dispatcher if the input source calls off-thread.
        if (DispatcherQueue.HasThreadAccess) CompleteMove();
        else DispatcherQueue.TryEnqueue(CompleteMove);
    }
    private void RememberPosition()
    {
        var position = AppWindow.Position;
        if (settings.WindowX == position.X && settings.WindowY == position.Y) return;
        settings.WindowX = position.X;
        settings.WindowY = position.Y;
        settings.Save();
    }
    private void Toggle()
    {
        if (AppWindow.IsVisible) AppWindow.Hide();
        else
        {
            ViewModel.Tick(UsageNow);
            Position();
            UpdateTray(force: true);
            AppWindow.Show();
        }
    }
    private async void Exit()
    {
        if (exiting) return;
        exiting = true;
        clock.Stop();
        ViewModel.IsLoading = true;
        lifetime.Cancel();
        // Reap the cancelled app-server process before stopping the UI loop.
        await activeRefresh;
        Close();
        Application.Current.Exit();
    }
    private void HideClick(object sender, RoutedEventArgs e) => AppWindow.Hide();
    private void TintOpacityClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem { Tag: int percent } && percent != tintOpacityPercent)
        {
            tintOpacityPercent = percent;
            ApplyTintOpacity();
            settings.TintOpacityPercent = percent;
            settings.Save();
        }
        foreach (var item in TintOpacityMenu.Items.OfType<ToggleMenuFlyoutItem>())
            item.IsChecked = Equals(item.Tag, tintOpacityPercent);
    }
    private void ApplyTintOpacity()
    {
        SystemBackdrop = tintOpacityPercent < 0
            ? new DesktopAcrylicBackdrop()
            : new TintedAcrylicBackdrop(tintOpacityPercent / 100.0);
    }
    private void UpdateIntervalClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem { Tag: UpdateInterval interval })
            ViewModel.SelectedInterval = interval;
        // Clicking the selected interval must keep it selected.
        SyncIntervalMenu();
    }
    private void SyncIntervalMenu()
    {
        foreach (var item in UpdateIntervalMenu.Items.OfType<ToggleMenuFlyoutItem>())
            item.IsChecked = Equals(item.Tag, ViewModel.SelectedInterval);
    }
    private async void RefreshClick(object sender, RoutedEventArgs e) => await RefreshAsync();
    private Task RefreshAsync()
    {
        if (busy || exiting) return activeRefresh;
        activeRefresh = RefreshCoreAsync();
        return activeRefresh;
    }
    private async Task RefreshCoreAsync()
    {
        if (busy || exiting) return;
        busy = true;
        var refreshStarted = DateTimeOffset.UtcNow;

        ViewModel.IsLoading = true;
        ViewModel.Status = "Refreshing…";
        try
        {
            UsageSnapshot snapshot;
            if (demo?.IsComplete == true)
            {
                await Task.Delay(demo.Configuration.RetrievalMilliseconds, lifetime.Token);
                snapshot = new UsageSnapshot(periods, ViewModel.PlanType);
            }
            else snapshot = demo is null ? await service.ReadSnapshotAsync(lifetime.Token) : await demo.ReadSnapshotAsync(lifetime.Token);
            if (exiting) return;
            periods = snapshot.Periods;
            if (demo is null) ViewModel.UseAccountHistory(snapshot.AccountKey);
            ViewModel.PlanType = snapshot.PlanType;
            updated = UsageNow;
            stale = false;

            ViewModel.Message = periods.Count == 0 ? "No general usage limits returned. Sign in to Codex with your ChatGPT account." : string.Empty;
            ViewModel.IsStale = false;
            ViewModel.SetPeriods(periods, UsageNow);
            ViewModel.Tick(UsageNow);
            ViewModel.Status = $"Last Updated: {updated:HH:mm}";
            if (demo?.IsComplete == true)
            {
                clock.Interval = TimeSpan.FromSeconds(1);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { return; }
        catch (Exception exception)
        {
            if (exiting) return;
            stale = true;

            ViewModel.Message = exception is OperationCanceledException ? "Connection timed out. Retrying automatically." : exception is System.IO.FileNotFoundException or InvalidOperationException ? exception.Message : "Unable to connect to Codex. Check your connection and Codex sign-in.";
            ViewModel.Status = updated.HasValue ? $"Offline · Last read {updated:HH:mm}" : "Not connected";
            ViewModel.IsStale = true;
        }
        finally
        {
            busy = false;
            nextRefresh = demo is null ? DateTimeOffset.UtcNow.AddSeconds(ViewModel.SelectedInterval.Seconds)
                : refreshStarted.AddMilliseconds(demo.Configuration.UpdateMilliseconds);
            if (!exiting)
            {
                ViewModel.IsLoading = false;
                UpdateTray(force: true);
                if (AppWindow.IsVisible) Position();
            }
        }
    }
    private void UpdateTray(bool force = false)
    {
        // Expiry is the only tooltip change between account refreshes.
        var expiredMask = 0;
        for (var index = 0; index < periods.Count; index++)
            if (periods[index].Reset <= UsageNow) expiredMask |= 1 << index;
        if (!force && expiredMask == lastExpiredMask) return;
        lastExpiredMask = expiredMask;
        var shortest = periods.FirstOrDefault(p => p.Minutes.HasValue);
        var available = !stale && shortest is not null && !(shortest.Reset <= UsageNow);
        var tooltip = "Codex - Usage Limits";
        if (periods.Count == 0) tooltip += "\nUsage unavailable";
        else
        {
            tooltip += "\n" + string.Join("\n", periods.Select(period =>
                $"{period.Label}  {Math.Floor(period.Remaining)}%{(period.Reset <= UsageNow ? " (awaiting refresh)" : string.Empty)}"));
            if (stale) tooltip += "\nOffline - last known values";
        }
        tray.Update(available ? (int)Math.Floor(shortest!.Remaining) : null, tooltip);
    }
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
