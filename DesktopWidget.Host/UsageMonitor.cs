using DesktopWidget.UI;

namespace DesktopWidget.Host;

// One owner observes history. UI restarts must never replay a cached sample into the estimator.
internal sealed class UsageMonitor : IDisposable
{
    private readonly object gate = new();
    private readonly Func<CancellationToken, Task<UsageSnapshot>> readSnapshot;
    private readonly UsageViewModel model = new();
    private readonly DemoUsageService? demo;
    private readonly AppSettings settings;
    private readonly string? settingsPath;
    private readonly CancellationTokenSource lifetime = new();
    private Task refresh = Task.CompletedTask;
    private DateTimeOffset nextRefresh = DateTimeOffset.MinValue;
    private long revision;
    private bool stopping;
    public Task Loop { get; }

    public UsageMonitor(DemoUsageService? demo, string? settingsPath = null,
        Func<CancellationToken, Task<UsageSnapshot>>? readSnapshot = null)
    {
        this.demo = demo;
        this.readSnapshot = readSnapshot ?? (demo is null
            ? new UsageService().ReadSnapshotAsync : demo.ReadSnapshotAsync);
        this.settingsPath = settingsPath;
        settings = AppSettings.Load(settingsPath, readOnly: demo is not null);
        model.SelectedInterval = model.Intervals.First(i => i.Seconds == settings.RefreshIntervalSeconds);
        Loop = RunAsync();
    }

    private DateTimeOffset Now => demo?.Now ?? DateTimeOffset.Now;

    public MonitorState Snapshot()
    {
        lock (gate)
            return new()
            {
                Settings = settings.Copy(),
                Revision = revision,
                Periods = model.Periods.Select(row => row.Model).ToArray(),
                Estimates = model.Periods.Where(row => row.Model.Minutes.HasValue && row.BudgetEstimate.HasValue)
                    .ToDictionary(row => row.Model.Minutes!.Value, row => row.BudgetEstimate!.Value),
                PlanType = model.PlanType, Status = model.Status, Message = model.Message,
                IsLoading = model.IsLoading, IsStale = model.IsStale,
                RefreshIntervalSeconds = model.SelectedInterval.Seconds,
                Now = Now, DemoComplete = demo?.IsComplete == true,
                UiTickMilliseconds = demo is not null && !demo.IsComplete ? 100 : 1000
            };
    }

    public void SetInterval(int seconds)
    {
        lock (gate)
        {
            var interval = model.Intervals.FirstOrDefault(i => i.Seconds == seconds);
            if (interval is null || interval == model.SelectedInterval) return;
            model.SelectedInterval = interval;
            settings.RefreshIntervalSeconds = seconds;
            settings.Save(settingsPath);
            if (demo is null) nextRefresh = DateTimeOffset.UtcNow.AddSeconds(seconds);
            revision++;
        }
    }

    public void SetWindowSettings(AppSettings incoming)
    {
        lock (gate)
        {
            // Interval has its own command; a queued geometry update must not undo it.
            settings.TintOpacityPercent = incoming.TintOpacityPercent is -1 or >= 0 and <= 100 &&
                (incoming.TintOpacityPercent == -1 || incoming.TintOpacityPercent % 10 == 0)
                ? incoming.TintOpacityPercent : -1;
            var validPosition = incoming.WindowX.HasValue && incoming.WindowY.HasValue &&
                incoming.WindowMonitor?.IsValid == true;
            settings.WindowX = validPosition ? incoming.WindowX : null;
            settings.WindowY = validPosition ? incoming.WindowY : null;
            settings.WindowMonitor = validPosition ? incoming.WindowMonitor : null;
            settings.Save(settingsPath);
            revision++;
        }
    }

    public Task Refresh()
    {
        lock (gate)
        {
            if (stopping || model.IsLoading || demo?.IsComplete == true) return refresh;
            model.IsLoading = true;
            model.Status = "Refreshing…";
            revision++;
            // Scheduling avoids doing process startup or history IO inside a tray callback.
            return refresh = Task.Run(RefreshAsync);
        }
    }

    private async Task RunAsync()
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                bool due;
                lock (gate) due = DateTimeOffset.UtcNow >= nextRefresh;
                if (due) await Refresh();
                await Task.Delay(demo is not null && !demo.IsComplete ? 100 : 1000, lifetime.Token);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }

    private async Task RefreshAsync()
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            var snapshot = await readSnapshot(lifetime.Token);
            lock (gate)
            {
                if (stopping) return;
                if (demo is null) model.UseAccountHistory(snapshot.AccountKey);
                model.PlanType = snapshot.PlanType;
                model.SetPeriods(snapshot.Periods, Now);
                model.IsStale = false;
                model.Message = snapshot.Periods.Count == 0
                    ? "No general usage limits returned. Sign in to Codex with your ChatGPT account." : string.Empty;
                model.Status = $"Last Updated: {Now:HH:mm}";
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            lock (gate)
            {
                model.IsStale = true;
                model.Status = "Not connected · last known values";
                model.Message = exception is OperationCanceledException ? "Connection timed out. Retrying automatically."
                    : exception is FileNotFoundException or InvalidOperationException ? exception.Message
                    : "Unable to connect to Codex. Check your connection and Codex sign-in.";
            }
        }
        finally
        {
            lock (gate)
            {
                model.IsLoading = false;
                nextRefresh = demo is null ? DateTimeOffset.UtcNow.AddSeconds(model.SelectedInterval.Seconds)
                    : started.AddMilliseconds(demo.Configuration.UpdateMilliseconds);
                revision++;
            }
        }
    }

    public async Task StopAsync()
    {
        Task pending;
        lock (gate) { stopping = true; pending = refresh; }
        lifetime.Cancel();
        await Loop;
        await pending; // UsageService reaps its app-server process before the host exits.
    }

    public void Dispose() => lifetime.Dispose();
}
