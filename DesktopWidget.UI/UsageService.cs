using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace DesktopWidget.UI;
public sealed record UsagePeriod(int? Minutes, double Remaining, DateTimeOffset? Reset)
{
    // The API supplies fixed minutes, not calendar units. A month here means
    // exactly 30 days; other durations retain their largest exact unit.
    private static readonly (int Minutes, string Unit)[] LabelUnits =
    {
        (30 * 24 * 60, "month"),
        (7 * 24 * 60, "week"),
        (24 * 60, "day"),
        (60, "hour"),
        (1, "minute")
    };

    public string Label
    {
        get
        {
            if (Minutes is not > 0) return "Usage window";
            foreach (var (duration, unit) in LabelUnits)
            {
                if (Minutes.Value % duration != 0) continue;
                var count = Minutes.Value / duration;
                return $"{count} {unit}{(count == 1 ? string.Empty : "s")}";
            }
            return "Usage window";
        }
    }
    public bool Expired => Reset <= DateTimeOffset.UtcNow;
    public string Countdown(DateTimeOffset now, bool showSeconds = true)
    {
        if (Reset is null) return "Reset time unavailable";
        var left = Reset.Value - now;
        if (left <= TimeSpan.Zero) return "Awaiting refreshed limit";
        var units = new List<string>(5);
        if (left.Days / 7 > 0) units.Add($"{left.Days / 7}w");
        if (left.Days % 7 > 0) units.Add($"{left.Days % 7}d");
        if (left.Hours > 0) units.Add($"{left.Hours}h");
        if (left.Minutes > 0) units.Add($"{left.Minutes:00}m");
        if (showSeconds) units.Add($"{left.Seconds:00}s");
        return units.Count > 0 ? string.Join(" ", units) : "<1m";
    }
}
public sealed record UsageSnapshot(IReadOnlyList<UsagePeriod> Periods, string? PlanType, string? AccountKey = null);

public sealed class UsageService
{
    public static IReadOnlyList<UsagePeriod> Parse(string json) =>
        (JsonSerializer.Deserialize(json, UsageJsonContext.Default.RateLimitsResponse)
            ?? throw new JsonException("Missing rate-limit response.")).ToPeriods();
    public async Task<IReadOnlyList<UsagePeriod>> ReadAsync(CancellationToken cancellation)
        => (await ReadSnapshotAsync(cancellation)).Periods;

    public async Task<UsageSnapshot> ReadSnapshotAsync(CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(25));
        using var process = new Process { StartInfo = new ProcessStartInfo(FindCodex(), "app-server") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetTempPath() } };
        process.Start();
        process.ErrorDataReceived += (_, _) => { };
        process.BeginErrorReadLine();
        try
        {
            await process.StandardInput.WriteLineAsync("{\"id\":1,\"method\":\"initialize\",\"params\":{\"clientInfo\":{\"name\":\"codex_usage_widget\",\"title\":\"Codex Usage Widget\",\"version\":\"1.0.0\"}}}");
            await ReceiveAsync(process, 1, timeout.Token);
            await process.StandardInput.WriteLineAsync("{\"method\":\"initialized\"}");
            await process.StandardInput.WriteLineAsync("{\"id\":2,\"method\":\"account/rateLimits/read\"}");
            var limits = await ReceiveAsync(process, 2, timeout.Token);
            var plan = limits.PlanType;
            string? accountKey = null;
            {
                try
                {
                    await process.StandardInput.WriteLineAsync("{\"id\":3,\"method\":\"account/read\",\"params\":{\"refreshToken\":false}}");
                    var account = (await ReceiveAsync(process, 3, timeout.Token)).Account;
                    if (account?.Type == "chatgpt")
                    {
                        plan = account.PlanType ?? plan;
                        accountKey = BudgetHistory.GetAccountKey(account.Email);
                    }
                }
                // A missing/unsupported account endpoint must not hide valid usage.
                catch (InvalidOperationException) { }
                catch (IOException) { }
                catch (JsonException) { }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { }
            }
            return new(limits.ToPeriods(), plan, accountKey);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }
    }
    private static async Task<AppServerResult> ReceiveAsync(Process process, int id, CancellationToken token)
    {
        while (await process.StandardOutput.ReadLineAsync(token) is { } line)
        {
            var response = JsonSerializer.Deserialize(line, UsageJsonContext.Default.AppServerResponse);
            if (response is null || response.Id.ValueKind != JsonValueKind.Number ||
                !response.Id.TryGetInt32(out var number) || number != id) continue;
            if (response.Error is not null)
                throw new InvalidOperationException("Codex could not read account limits. Check your Codex ChatGPT sign-in.");
            return response.Result ?? throw new JsonException("Missing app-server result.");
        }
        throw new IOException("Codex closed the connection. Update Codex and retry.");
    }
    private static string FindCodex()
    {
        var configured = Environment.GetEnvironmentVariable("CODEX_WIDGET_CLI");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return configured;
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(directory.Trim('"'), "codex.exe");
            if (File.Exists(candidate)) return candidate;
        }
        var extensions = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vscode", "extensions");
        if (Directory.Exists(extensions))
        {
            var candidate = Directory.EnumerateDirectories(extensions, "openai.chatgpt-*").OrderByDescending(Directory.GetLastWriteTimeUtc).Select(p => Path.Combine(p, "bin", "windows-x86_64", "codex.exe")).FirstOrDefault(File.Exists);
            if (candidate is not null) return candidate;
        }
        throw new FileNotFoundException("Install Codex CLI and sign in with ChatGPT. See README for setup.");
    }
}
