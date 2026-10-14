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
using WinRT;
namespace DesktopWidget.UI;
public sealed partial class MainWindow : Window
{
    private readonly string pipeName;
    private DateTimeOffset usageNow = DateTimeOffset.Now;
    private DateTimeOffset stateReceived = DateTimeOffset.UtcNow;
    private DateTimeOffset UsageNow => usageNow + (DateTimeOffset.UtcNow - stateReceived);
    private readonly SemaphoreSlim requests = new(1, 1);
    private long appliedRevision = -1;
    private bool applyingState, polling;
    private readonly CancellationTokenSource lifetime = new();
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromSeconds(1) };
    public UsageViewModel ViewModel { get; } = new();
    private bool exiting;
    private int tintOpacityPercent = -1;
    private readonly AppSettings settings;
    private PointInt32? windowPosition;
    private WindowMonitorSettings? windowMonitor;
    private bool rememberPositionPending;
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
    public MainWindow(string pipeName, MonitorState initialState)
    {
        this.pipeName = pipeName;
        settings = initialState.Settings;
        if (settings.WindowX is int x && settings.WindowY is int y)
            windowPosition = new PointInt32(x, y);
        windowMonitor = settings.WindowMonitor;
        InitializeComponent();
        tintOpacityPercent = settings.TintOpacityPercent;
        ViewModel.SelectedInterval = ViewModel.Intervals.First(interval => interval.Seconds == settings.RefreshIntervalSeconds);
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
                SyncIntervalMenu();
                if (!applyingState)
                {
                    settings.RefreshIntervalSeconds = ViewModel.SelectedInterval.Seconds;
                    _ = RequestAsync(new() { Action = "interval", IntervalSeconds = ViewModel.SelectedInterval.Seconds });
                }
            }
        };
        hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        ExtendsContentIntoTitleBar = true;
        RootLayout.Loaded += RootLayoutLoaded;
        // Preserve the derived WinRT projection when publishing with trimming / Native AOT.
        var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
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
        var corner = 2;
        DwmSetWindowAttribute(hwnd, 33, ref corner, sizeof(int));
        AppWindow.Closing += async (_, e) =>
        {
            if (exiting) return;
            e.Cancel = true;
            await CloseWindowAsync();
        };
        AppWindow.Changed += AppWindowChanged;
        Closed += (_, _) =>
        {
            exiting = true;
            lifetime.Cancel();
            clock.Stop();
            SystemBackdrop = null;
            RootLayout.Loaded -= RootLayoutLoaded;
            AppWindow.Changed -= AppWindowChanged;
            if (xamlRoot is not null) xamlRoot.Changed -= XamlRootChanged;
            nonClientInput.EnteringMoveSize -= NativeMoveStarting;
            nonClientInput.ExitedMoveSize -= NativeMoveEnded;
            Header.LayoutUpdated -= HeaderLayoutUpdated;
            Application.Current.Exit();
        };
        clock.Tick += async (_, _) =>
        {
            if (exiting || polling) return;
            ViewModel.Tick(UsageNow);
            Position();
            polling = true;
            try { await RequestAsync(new()); }
            finally { polling = false; }
        };
        ApplyState(initialState);
        Position();
        clock.Start();
        _ = RequestAsync(new() { Action = "ready", WindowHandle = hwnd.ToInt64() });
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
    private void ResetPosition()
    {
        windowPosition = null;
        windowMonitor = null;
        rememberPositionPending = false;
        if (settings.WindowX is null && settings.WindowY is null && settings.WindowMonitor is null) return;
        settings.WindowX = settings.WindowY = null;
        settings.WindowMonitor = null;
        SaveWindowSettings();
    }
    private void MoveToMonitor(WindowMonitor monitor)
    {
        if (WindowMonitor.FromWindow(hwnd)?.Handle == monitor.Handle) return;
        var area = monitor.Settings;
        // A tiny staging rectangle wholly inside the target selects its native DPI before measuring.
        // Moving the old, larger rectangle to a saved point can select an adjacent monitor instead.
        AppWindow.MoveAndResize(new RectInt32(area.WorkX + area.WorkWidth / 2,
            area.WorkY + area.WorkHeight / 2, 1, 1));
    }
    private void Position()
    {
        if (exiting || positioning) return;
        positioning = true;
        try
        {
            WindowMonitor? monitor;
            if (dragging)
            {
                // A user drag may deliberately select another monitor; do not validate the old location.
                monitor = WindowMonitor.FromWindow(hwnd);
            }
            else
            {
                var monitors = WindowMonitor.FindAll();
                monitor = windowPosition.HasValue && windowMonitor?.IsValid == true
                    ? monitors.FirstOrDefault(m => windowMonitor.MatchesGeometry(m.Settings)) : null;
                if (monitor is null)
                {
                    ResetPosition();
                    monitor = monitors.FirstOrDefault(m => m.IsPrimary);
                }
            }
            // Displays can temporarily disappear while Windows applies a configuration change.
            if (monitor is null) return;
            if (!dragging) MoveToMonitor(monitor);
            var dpi = GetDpiForWindow(hwnd);
            if (dpi == 0) return;
            if (!dragging && windowPosition.HasValue &&
                windowMonitor?.CanRestore(monitor.Settings with { Dpi = dpi }) != true)
            {
                // Geometry matched, but its scaling changed: discard the old location permanently.
                ResetPosition();
                monitor = WindowMonitor.FindAll().FirstOrDefault(m => m.IsPrimary);
                if (monitor is null) return;
                MoveToMonitor(monitor);
                dpi = GetDpiForWindow(hwnd);
                if (dpi == 0) return;
            }
            var nativeScale = dpi / 96.0;
            windowScale = nativeScale;
            var area = monitor.Settings;
            var chromeWidth = Math.Max(0, AppWindow.Size.Width - AppWindow.ClientSize.Width);
            var chromeHeight = Math.Max(0, AppWindow.Size.Height - AppWindow.ClientSize.Height);
            var gap = (int)Math.Ceiling(12 * nativeScale);
            var sizing = WindowClientSizing.Calculate(RootLayout.Width, nativeScale,
                area.WorkWidth - chromeWidth, area.WorkHeight - chromeHeight - 2 * gap);

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
                new WindowPlacement(area.WorkX, area.WorkY, area.WorkWidth, area.WorkHeight), gap);
            if (AppWindow.Position.X != placement.X || AppWindow.Position.Y != placement.Y)
                AppWindow.Move(new PointInt32(placement.X, placement.Y));
            // Native DPI can change synchronously during Move/Resize. Remeasure before saving anything.
            if (GetDpiForWindow(hwnd) != dpi || WindowMonitor.FromWindow(hwnd)?.Handle != monitor.Handle)
            {
                QueuePosition();
                return;
            }
            if (rememberPositionPending && windowPosition.HasValue)
            {
                windowPosition = AppWindow.Position;
                windowMonitor = area with { Dpi = dpi };
                RememberPosition();
                rememberPositionPending = false;
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
            if (end.X != start.X || end.Y != start.Y)
            {
                windowPosition = end;
                windowMonitor = WindowMonitor.FromWindow(hwnd) is { } monitor
                    ? monitor.Settings with { Dpi = GetDpiForWindow(hwnd) } : null;
                rememberPositionPending = true;
            }
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
        if (settings.WindowX == position.X && settings.WindowY == position.Y &&
            settings.WindowMonitor == windowMonitor) return;
        settings.WindowX = position.X;
        settings.WindowY = position.Y;
        settings.WindowMonitor = windowMonitor;
        SaveWindowSettings();
    }
    // Closing destroys the window and exits this process. Only the native host remains.
    private async void HideClick(object sender, RoutedEventArgs e) => await CloseWindowAsync();

    private async Task CloseWindowAsync()
    {
        if (exiting) return;
        exiting = true;
        clock.Stop();
        // Finish queued preferences before the Closed handler cancels pipe requests.
        try
        {
            await requests.WaitAsync(lifetime.Token);
            requests.Release();
        }
        catch (OperationCanceledException) { }
        Close();
    }
    private void TintOpacityClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem { Tag: int percent } && percent != tintOpacityPercent)
        {
            tintOpacityPercent = percent;
            ApplyTintOpacity();
            settings.TintOpacityPercent = percent;
            SaveWindowSettings();
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
    private async void RefreshClick(object sender, RoutedEventArgs e)
        => await RequestAsync(new() { Action = "refresh" });

    private void SaveWindowSettings()
        => _ = RequestAsync(new() { Action = "settings", Settings = settings.Copy() });

    private void ApplyState(MonitorState state)
    {
        usageNow = state.Now;
        stateReceived = DateTimeOffset.UtcNow;
        clock.Interval = TimeSpan.FromMilliseconds(state.UiTickMilliseconds);
        if (state.Revision == appliedRevision)
        {
            ViewModel.Tick(UsageNow);
            return;
        }
        appliedRevision = state.Revision;
        applyingState = true;
        try
        {
            settings.RefreshIntervalSeconds = state.RefreshIntervalSeconds;
            ViewModel.SelectedInterval = ViewModel.Intervals.First(i => i.Seconds == state.RefreshIntervalSeconds);
            ViewModel.PlanType = state.PlanType;
            ViewModel.SetPeriods(state.Periods, UsageNow, state.Estimates);
            ViewModel.IsLoading = state.IsLoading;
            ViewModel.IsStale = state.IsStale;
            ViewModel.Message = state.Message;
            ViewModel.Status = state.Status;
            ViewModel.RefreshEnabled = !state.DemoComplete;
            Position();
        }
        finally { applyingState = false; }
    }

    private async Task RequestAsync(MonitorCommand command)
    {
        try
        {
            await requests.WaitAsync(lifetime.Token);
            try
            {
                var state = await MonitorProtocol.RequestAsync(pipeName, command, lifetime.Token);
                if (exiting) return;
                ApplyState(state);
            }
            finally { requests.Release(); }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception) when (exception is System.IO.IOException or System.IO.InvalidDataException or System.Text.Json.JsonException or OperationCanceledException)
        {
            // Never leave an orphaned WinUI process resident after the monitor stops.
            if (!exiting) await CloseWindowAsync();
        }
    }
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
