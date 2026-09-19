using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopWidget.UI;

// Learns relative capacity from paired percentage drops, never from window duration.
public sealed class BudgetEstimator
{
    private readonly Dictionary<int, Sample> samples = new();
    private int? baselineMinutes;
    public bool ShouldRecord { get; private set; }
    public bool HighestPeriodReset { get; private set; }
    public Guid SegmentId { get; private set; } = Guid.NewGuid();
    private UsagePeriod[]? previous;
    private UsagePeriod? previousHighest;

    public IReadOnlyDictionary<int, double> Observe(IReadOnlyList<UsagePeriod> periods, DateTimeOffset? observedAt = null)
    {
        var now = observedAt ?? DateTimeOffset.UtcNow;
        ShouldRecord = false;
        HighestPeriodReset = false;
        var result = new Dictionary<int, double>();
        if (periods.Count < 2 || periods.Any(p => p is null || p.Minutes is not > 0 ||
            !double.IsFinite(p.Remaining) || p.Remaining < 0 || p.Remaining > 100 || p.Reset is null || p.Reset <= now) ||
            periods.Select(p => p.Minutes).Distinct().Count() != periods.Count)
        {
            samples.Clear();
            previous = null;
            SegmentId = Guid.NewGuid();
            return result;
        }
        var ordered = periods.OrderBy(p => p.Minutes).ToArray();
        var highest = ordered[^1];
        HighestPeriodReset = previousHighest is not null && highest.Minutes == previousHighest.Minutes &&
            (highest.Reset != previousHighest.Reset || highest.Remaining > previousHighest.Remaining);
        previousHighest = highest;
        if (HighestPeriodReset) samples.Clear();
        var sameWindows = previous is not null && previous.Select(p => p.Minutes).SequenceEqual(ordered.Select(p => p.Minutes));
        var boundary = HighestPeriodReset || sameWindows && ordered.Where((p, i) => p.Reset != previous![i].Reset || p.Remaining > previous[i].Remaining).Any();
        var unchanged = sameWindows && !boundary && ordered.Where((p, i) => p.Remaining != previous![i].Remaining).Any() == false;
        if (boundary || !sameWindows) SegmentId = Guid.NewGuid();
        ShouldRecord = !boundary && !unchanged;
        previous = ordered;
        var baseline = periods.Where(p => p.Minutes is > 0).OrderBy(p => p.Minutes).FirstOrDefault();
        if (baselineMinutes != baseline?.Minutes)
        {
            samples.Clear();
            baselineMinutes = baseline?.Minutes;
        }
        foreach (var key in samples.Keys.ToArray())
            if (!periods.Any(p => p.Minutes == key)) samples.Remove(key);
        if (baseline is null) return result;
        foreach (var period in periods.Where(p => p.Minutes > baseline.Minutes))
        {
            var key = period.Minutes!.Value;
            if (!samples.TryGetValue(key, out var sample))
                samples[key] = sample = new(baseline, period);
            // A reset, refill, or missing reset metadata makes the interval ambiguous.
            if (baseline.Reset is null || period.Reset is null || baseline.Reset <= now || period.Reset <= now)
            {
                samples[key] = new(baseline, period);
                continue;
            }
            if (baseline.Reset != sample.PreviousBase.Reset || period.Reset != sample.PreviousPeriod.Reset)
            {
                // Never compare consumption across a reset; retain the learned scale.
                samples[key] = sample = new(baseline, period) { CapacityRatio = sample.CapacityRatio };
            }
            else if (baseline.Remaining > sample.PreviousBase.Remaining || period.Remaining > sample.PreviousPeriod.Remaining)
            {
                samples[key] = new(baseline, period);
                continue;
            }
            sample.PreviousBase = baseline;
            sample.PreviousPeriod = period;
            var baseDrop = sample.AnchorBase.Remaining - baseline.Remaining;
            var periodDrop = sample.AnchorPeriod.Remaining - period.Remaining;
            // Accumulate from a common anchor so independently rounded polls do not
            // bias the ratio. Initial estimates may be noisy at one percentage point.
            if (!boundary && !unchanged && baseDrop >= 1 && periodDrop >= 1 && baseline.Remaining > 0 && period.Remaining > 0)
                sample.CapacityRatio = baseDrop / periodDrop;
            if (sample.CapacityRatio is { } ratio)
            {
                result[key] = period.Remaining / 100 * ratio;
            }
        }
        return result;
    }

    // Recover the learned scale from an accepted persisted point at a segment boundary.
    public void RestoreEstimates(IReadOnlyList<UsagePeriod> periods, IReadOnlyDictionary<int, double>? estimates)
    {
        if (estimates is null) return;
        foreach (var period in periods)
            if (period.Minutes is { } key && period.Remaining > 0 && samples.TryGetValue(key, out var sample) &&
                estimates.TryGetValue(key, out var estimate) && double.IsFinite(estimate) && estimate > 0)
                sample.CapacityRatio = estimate * 100 / period.Remaining;
    }

    private sealed class Sample(UsagePeriod baseline, UsagePeriod period)
    {
        public UsagePeriod AnchorBase { get; } = baseline;
        public UsagePeriod AnchorPeriod { get; } = period;
        public UsagePeriod PreviousBase { get; set; } = baseline;
        public UsagePeriod PreviousPeriod { get; set; } = period;
        public double? CapacityRatio { get; set; }
    }
}
