using DesktopWidget.Host;
using DesktopWidget.UI;
using System.IO.Pipes;

internal static class MonitorChecks
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine($"PASS {name}");
    }

    public static async Task RunAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DesktopWidget.MonitorChecks." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await CheckDemoAsync(Path.Combine(directory, "demo-settings.json"));
            await CheckSettingsAsync(Path.Combine(directory, "live-settings.json"));
            await CheckProtocolAsync();
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static async Task CheckDemoAsync(string settingsPath)
    {
        var demo = new DemoUsageService(Path.Combine(AppContext.BaseDirectory, "Demo", "plus-session.json"));
        demo.Configuration.UpdateMilliseconds = 60_000;
        using var monitor = new UsageMonitor(demo, settingsPath);
        try
        {
            await monitor.Refresh();
            var first = monitor.Snapshot();
            Check(first.Periods.Length == 2 && !first.IsLoading && !first.IsStale,
                "Tray host observes usage without a window");
            monitor.SetInterval(120);
            monitor.SetWindowSettings(new()
            {
                RefreshIntervalSeconds = 30, TintOpacityPercent = 40, WindowX = -1800, WindowY = 20,
                WindowMonitor = new("demo-monitor", -1920, 0, 1920, 1080, -1920, 0, 1920, 1040, 144)
            });
            var state = monitor.Snapshot();
            Check(state.Settings.RefreshIntervalSeconds == 120 && state.Settings.TintOpacityPercent == 40 &&
                state.Settings.WindowX == -1800 && !File.Exists(settingsPath),
                "Demo preferences survive window recreation in host memory without disk writes");
            state.Settings.TintOpacityPercent = 90;
            Check(monitor.Snapshot().Settings.TintOpacityPercent == 40,
                "Window settings snapshots cannot mutate host state");
            await monitor.Refresh();
            state = monitor.Snapshot();
            Check(state.Revision > first.Revision && state.Estimates.ContainsKey(10080),
                "Host retains budget calibration while windows are absent");
            var window = new UsageViewModel();
            window.SetPeriods(state.Periods, state.Now, state.Estimates);
            Check(window.Periods[1].BudgetEstimate == state.Estimates[10080] && window.HistoryFilePath is null,
                "Recreated window consumes host estimates without owning history");
            window.SetPeriods(state.Periods, state.Now, new Dictionary<int, double>());
            Check(window.Periods.All(row => row.BudgetEstimate is null),
                "Empty host estimates clear old window estimates without recalibrating cached data");
            demo.Configuration.RetrievalMilliseconds = 10_000;
            var pending = monitor.Refresh();
            Check(ReferenceEquals(pending, monitor.Refresh()), "Concurrent host refresh commands share one retrieval");
            await monitor.StopAsync();
            await pending;
            Check(monitor.Snapshot().Periods.SequenceEqual(state.Periods) && !monitor.Snapshot().IsLoading,
                "Stopping host cancels pending retrieval and retains the last accepted sample");
        }
        finally { await monitor.StopAsync(); }
    }

    private static async Task CheckSettingsAsync(string path)
    {
        var response = new TaskCompletionSource<UsageSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var monitor = new UsageMonitor(null, path, token => response.Task.WaitAsync(token));
        try
        {
            Check(File.Exists(path), "Hidden live host creates default settings without a window");
            monitor.SetInterval(60);
            monitor.SetWindowSettings(new() { TintOpacityPercent = 70 });
            var restored = AppSettings.Load(path, readOnly: true);
            Check(restored.RefreshIntervalSeconds == 60 && restored.TintOpacityPercent == 70,
                "Host persists preferences and geometry commands preserve the chosen interval");
            monitor.SetInterval(17);
            monitor.SetWindowSettings(new() { TintOpacityPercent = 37, WindowX = 10 });
            var invalid = monitor.Snapshot();
            Check(invalid.RefreshIntervalSeconds == 60 && invalid.Settings.TintOpacityPercent == -1 &&
                invalid.Settings.WindowX is null && invalid.Settings.WindowY is null,
                "Host rejects invalid intervals, tints, and incomplete positions");
            response.SetException(new IOException("Test connection failure"));
            await monitor.Refresh();
            Check(monitor.Snapshot().IsStale && !monitor.Snapshot().IsLoading,
                "Host marks failed refresh offline without blocking the tray");
        }
        finally { await monitor.StopAsync(); }
    }

    private static async Task CheckProtocolAsync()
    {
        var state = new MonitorState
        {
            Revision = 42, Now = DateTimeOffset.UtcNow,
            Periods = new[] { new UsagePeriod(300, 43, DateTimeOffset.UtcNow.AddHours(1)) },
            Estimates = new() { [10080] = 6.05 }, Settings = new() { TintOpacityPercent = 20 }
        };
        using var stream = new MemoryStream();
        await MonitorProtocol.WriteAsync(stream, state, MonitorJsonContext.Default.MonitorState, CancellationToken.None);
        stream.Position = 0;
        var roundTrip = await MonitorProtocol.ReadAsync(stream, MonitorJsonContext.Default.MonitorState, CancellationToken.None);
        Check(roundTrip.Revision == 42 && roundTrip.Periods.SequenceEqual(state.Periods) &&
            roundTrip.Estimates[10080] == 6.05 && roundTrip.Settings.TintOpacityPercent == 20,
            "Source-generated monitor protocol round-trips usage, estimates, and settings");
        foreach (var length in new[] { 0, -1, MonitorProtocol.MaximumMessageBytes + 1 })
        {
            using var malformed = new MemoryStream(BitConverter.GetBytes(length));
            try
            {
                await MonitorProtocol.ReadAsync(malformed, MonitorJsonContext.Default.MonitorState, CancellationToken.None);
                throw new Exception("Invalid message accepted");
            }
            catch (InvalidDataException) { }
        }
        using var truncated = new MemoryStream(new byte[] { 10, 0, 0, 0, 123 });
        try
        {
            await MonitorProtocol.ReadAsync(truncated, MonitorJsonContext.Default.MonitorState, CancellationToken.None);
            throw new Exception("Truncated message accepted");
        }
        catch (EndOfStreamException) { }
        Check(true, "Monitor protocol rejects invalid lengths and truncated payloads");

        var pipeName = "DesktopWidget.Checks." + Guid.NewGuid().ToString("N");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var request = MonitorProtocol.RequestAsync(pipeName, new() { Action = "interval", IntervalSeconds = 120 }, timeout.Token);
        await server.WaitForConnectionAsync(timeout.Token);
        var command = await MonitorProtocol.ReadAsync(server, MonitorJsonContext.Default.MonitorCommand, timeout.Token);
        await MonitorProtocol.WriteAsync(server, state, MonitorJsonContext.Default.MonitorState, timeout.Token);
        var received = await request;
        Check(command.Action == "interval" && command.IntervalSeconds == 120 && received.Revision == 42,
            "Monitor client exchanges commands and snapshots through a current-user named pipe");
    }
}
