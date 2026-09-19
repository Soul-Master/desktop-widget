using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopWidget.UI;

public sealed class DemoConfiguration
{
    public int RetrievalMilliseconds { get; set; } = 200;
    public int UpdateMilliseconds { get; set; } = 2000;
    public DateTimeOffset StartsAt { get; set; }
    public string PlanType { get; set; } = "plus";
    public DemoFrame[] Frames { get; set; } = Array.Empty<DemoFrame>();
}

public sealed class DemoFrame
{
    public double ElapsedMinutes { get; set; }
    public UsagePeriod[] Periods { get; set; } = Array.Empty<UsagePeriod>();
}

// One fixture frame per successful poll; stop after the final frame.
public sealed class DemoUsageService
{
    public DemoConfiguration Configuration { get; }
    private int index;
    public bool IsComplete => index >= Configuration.Frames.Length;
    private DateTimeOffset frameTime;
    private readonly System.Diagnostics.Stopwatch completedClock = new();
    public DateTimeOffset Now => frameTime + completedClock.Elapsed;
    public DemoUsageService(string path)
    {
        Configuration = JsonSerializer.Deserialize(File.ReadAllText(path), DemoJsonContext.Default.DemoConfiguration)
            ?? throw new InvalidDataException("Demo configuration is missing.");
        if (Configuration.StartsAt == default || Configuration.UpdateMilliseconds < 1 ||
            Configuration.RetrievalMilliseconds < 0 || Configuration.RetrievalMilliseconds >= Configuration.UpdateMilliseconds ||
            Configuration.Frames.Length == 0 || Configuration.Frames[0].ElapsedMinutes != 0)
            throw new InvalidDataException("Invalid demo timing or missing frames.");
        double previous = -1;
        foreach (var frame in Configuration.Frames)
        {
            if (frame is null || !double.IsFinite(frame.ElapsedMinutes) || frame.ElapsedMinutes <= previous ||
                frame.Periods is null || frame.Periods.Length != 2 ||
                !frame.Periods.Select(p => p?.Minutes).OrderBy(m => m).SequenceEqual(new int?[] { 300, 10080 }) ||
                frame.Periods.Any(p => !double.IsFinite(p.Remaining) || p.Remaining < 0 || p.Remaining > 100 ||
                    p.Reset is null || p.Reset <= Configuration.StartsAt.AddMinutes(frame.ElapsedMinutes)))
                throw new InvalidDataException("Demo frames require increasing times and valid five-hour and weekly periods.");
            previous = frame.ElapsedMinutes;
        }
        frameTime = Configuration.StartsAt;
    }
    public async Task<UsageSnapshot> ReadSnapshotAsync(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (IsComplete) throw new InvalidOperationException("Demo has no remaining data points.");
        await Task.Delay(Configuration.RetrievalMilliseconds, cancellation);
        var frame = Configuration.Frames[index];
        frameTime = Configuration.StartsAt.AddMinutes(frame.ElapsedMinutes);
        index++;
        if (IsComplete) completedClock.Start();
        return new(frame.Periods, Configuration.PlanType);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DemoConfiguration))]
internal partial class DemoJsonContext : JsonSerializerContext { }
