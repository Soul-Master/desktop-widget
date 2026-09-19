using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopWidget.UI;

public class RateLimitsResponse
{
    public RateLimitBucket? RateLimits { get; set; }
    public Dictionary<string, RateLimitBucket?>? RateLimitsByLimitId { get; set; }
    public string? PlanType => (RateLimitsByLimitId?.TryGetValue("codex", out var general) == true
        ? general : RateLimits is { LimitId: null or "codex" } ? RateLimits : null)?.PlanType;

    public IReadOnlyList<UsagePeriod> ToPeriods()
    {
        var bucket = RateLimitsByLimitId?.TryGetValue("codex", out var general) == true
            ? general
            : RateLimits is { LimitId: null or "codex" } ? RateLimits : null;
        if (bucket is null) return Array.Empty<UsagePeriod>();
        return new[] { bucket.Primary, bucket.Secondary }
            .Where(window => window?.UsedPercent is { } percent && double.IsFinite(percent))
            .Select(window => window!.ToPeriod())
            .OrderBy(period => period.Minutes ?? int.MaxValue)
            .ToArray();
    }
}

public sealed class RateLimitBucket
{
    public string? PlanType { get; set; }
    public string? LimitId { get; set; }
    public RateLimitWindow? Primary { get; set; }
    public RateLimitWindow? Secondary { get; set; }
}

public sealed class RateLimitWindow
{
    public double? UsedPercent { get; set; }
    public int? WindowDurationMins { get; set; }
    public long? ResetsAt { get; set; }

    public UsagePeriod ToPeriod()
    {
        DateTimeOffset? reset = null;
        if (ResetsAt is { } seconds)
        {
            try { reset = DateTimeOffset.FromUnixTimeSeconds(seconds); }
            catch (ArgumentOutOfRangeException) { }
        }
        return new(WindowDurationMins > 0 ? WindowDurationMins : null,
            Math.Clamp(100 - UsedPercent!.Value, 0, 100), reset);
    }
}

// JSON-RPC permits string and numeric IDs; notifications have no ID.
public sealed class AppServerResponse
{
    public JsonElement Id { get; set; }
    public AppServerResult? Result { get; set; }
    public AppServerError? Error { get; set; }
}

public sealed class AppServerResult : RateLimitsResponse
{
    public ChatGptAccount? Account { get; set; }
}

public sealed class ChatGptAccount
{
    public string? Type { get; set; }
    public string? Email { get; set; }
    public string? PlanType { get; set; }
}

public sealed class AppServerError
{
    public int Code { get; set; }
    public string? Message { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RateLimitsResponse))]
[JsonSerializable(typeof(AppServerResponse))]
[JsonSerializable(typeof(BudgetHistoryEntry))]
internal partial class UsageJsonContext : JsonSerializerContext { }
