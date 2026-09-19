using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace DesktopWidget.UI;

public sealed class BudgetHistoryEntry
{
    public int Version { get; set; } = 1;
    public DateTimeOffset ObservedAt { get; set; }
    public string? PlanType { get; set; }
    public UsagePeriod[]? Periods { get; set; }
    public Dictionary<int, double>? Estimates { get; set; }
    public Guid? SegmentId { get; set; }
}

// JSON Lines containing accepted usage observations for the current longest window.
public sealed class BudgetHistory
{
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopWidget", "usage-history.jsonl");
    public static string? GetAccountKey(string? email) => string.IsNullOrWhiteSpace(email) ? null
        : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()))).ToLowerInvariant();
    public static string AccountPath(string accountKey, string? directory = null)
    {
        if (accountKey.Length != 64 || !System.Linq.Enumerable.All(accountKey, Uri.IsHexDigit))
            throw new ArgumentException("Account key must be a SHA-256 hex digest.", nameof(accountKey));
        return Path.Combine(directory ?? Path.GetDirectoryName(DefaultPath)!, $"usage-history-{accountKey.ToLowerInvariant()}.jsonl");
    }
    public string FilePath { get; }
    public BudgetEstimator Estimator { get; private set; } = new();
    private string? planType;

    public BudgetHistory(string filePath)
    {
        FilePath = filePath;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
            using (var file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) { }
            Guid? previousSegment = null;
            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var entry = JsonSerializer.Deserialize(line, UsageJsonContext.Default.BudgetHistoryEntry);
                    if (entry is not { Version: 1, Periods: not null } || entry.ObservedAt == default) continue;
                    if (Array.Exists(entry.Periods, p => p is null || !double.IsFinite(p.Remaining) || p.Remaining < 0 || p.Remaining > 100)) continue;
                    SetPlan(entry.PlanType);
                    if (entry.SegmentId is { } segment && segment != previousSegment) Estimator = new();
                    Estimator.Observe(entry.Periods, entry.ObservedAt);
                    if (entry.SegmentId is not null) Estimator.RestoreEstimates(entry.Periods, entry.Estimates);
                    previousSegment = entry.SegmentId;
                }
                catch (JsonException) {  }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {  }
    }

    private void SetPlan(string? currentPlan)
    {
        if (planType != currentPlan) Estimator = new();
        planType = currentPlan;
    }

    public IReadOnlyDictionary<int, double> Observe(UsagePeriod[] periods, string? currentPlan, DateTimeOffset? observedAt = null)
    {
        SetPlan(currentPlan);
        var timestamp = observedAt ?? DateTimeOffset.UtcNow;
        var estimates = Estimator.Observe(periods, timestamp);
        if (Estimator.HighestPeriodReset) Clear();
        if (Estimator.ShouldRecord) Append(new() { ObservedAt = timestamp, PlanType = currentPlan, Periods = periods, SegmentId = Estimator.SegmentId,
            Estimates = new(estimates) });
        return estimates;
    }

    private void Append(BudgetHistoryEntry entry)
    {
        try
        {
            // A leading newline keeps the next entry readable after a truncated write.
            File.AppendAllText(FilePath, "\n" + JsonSerializer.Serialize(entry, UsageJsonContext.Default.BudgetHistoryEntry), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {  }
    }

    private void Clear()
    {
        try { File.WriteAllText(FilePath, string.Empty, new UTF8Encoding(false)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { }
    }
}
