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

    private readonly IntPtr hwnd;
    public MainWindow(DemoUsageService? demo = null)
    {
        this.demo = demo;
        settings = AppSettings.Load(readOnly: demo is not null);
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
        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        AppWindow.IsShownInSwitchers = false;
        var corner = 2;
        DwmSetWindowAttribute(hwnd, 33, ref corner, sizeof(int));
        tray = new TrayIndicator(() => DispatcherQueue.TryEnqueue(Toggle), () => DispatcherQueue.TryEnqueue(async () => await RefreshAsync()), () => DispatcherQueue.TryEnqueue(Exit));
        AppWindow.Closing += (_, e) => { if (!exiting) { e.Cancel = true; AppWindow.Hide(); } };
        Closed += (_, _) => { lifetime.Cancel(); clock.Stop(); tray.Dispose(); };
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
    private void Position()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var scale = GetDpiForWindow(hwnd) / 96.0;
        var width = Math.Min((int)(364 * scale), area.Width);
        var gap = (int)(12 * scale);
        RootLayout.Measure(new Windows.Foundation.Size(width / scale, double.PositiveInfinity));
        var height = Math.Min((int)Math.Ceiling(RootLayout.DesiredSize.Height * scale), area.Height - 2 * gap);
        var rect = new RectInt32(area.X + area.Width - width - gap, area.Y + area.Height - height - gap, width, height);
        if (AppWindow.Position.X != rect.X || AppWindow.Position.Y != rect.Y || AppWindow.Size.Width != width || AppWindow.Size.Height != height) AppWindow.MoveAndResize(rect);
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
