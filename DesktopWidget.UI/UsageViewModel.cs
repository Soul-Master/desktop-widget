using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace DesktopWidget.UI;

public abstract class ObservableModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Notify(name);
        return true;
    }
}

public sealed record UpdateInterval(int Seconds, string Label);

public sealed class UsageViewModel : ObservableModel
{
    private BudgetEstimator budgetEstimator = new();
    private BudgetHistory? budgetHistory;
    private string? historyAccountKey;
    public string? HistoryFilePath => budgetHistory?.FilePath;
    public void UseAccountHistory(string? accountKey, string? directory = null)
    {
        if (accountKey is not null && accountKey == historyAccountKey) return;
        historyAccountKey = accountKey;
        budgetEstimator = new();
        budgetHistory = accountKey is null ? null : new BudgetHistory(BudgetHistory.AccountPath(accountKey, directory));
        foreach (var row in Periods) row.SetBudgetEstimate(null);
        Notify(nameof(HistoryFilePath));
    }
    public ObservableCollection<UsagePeriodViewModel> Periods { get; } = new();
    public IReadOnlyList<UpdateInterval> Intervals { get; } = new[]
    {
        new UpdateInterval(15, "15 seconds"), 
        new UpdateInterval(30, "30 seconds"),
        new UpdateInterval(60, "1 minute"), 
        new UpdateInterval(120, "2 minutes"),
        new UpdateInterval(300, "5 minutes"),
        new UpdateInterval(600, "10 minutes")
    };
    private UpdateInterval selectedInterval;
    private bool isLoading, isStale;
    private string? planType;
    public string? PlanType
    {
        get => planType;
        set
        {
            if (!Set(ref planType, string.IsNullOrWhiteSpace(value) ? null : value.Trim())) return;
            Notify(nameof(PlanLabel));
            Notify(nameof(HasPlan));
            Notify(nameof(PlanDescription));
        }
    }
    public bool HasPlan => planType is not null;
    public string PlanLabel => planType is null ? string.Empty
        : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(planType.Replace('_', ' ').Replace('-', ' '));
    public string PlanDescription => $"ChatGPT plan: {PlanLabel}";
    private string status = "Connecting to Codex", message = "Connecting to Codex…";
    public UsageViewModel(BudgetHistory? history = null)
    {
        budgetHistory = history;
        selectedInterval = Intervals[1];
    }
    public UpdateInterval SelectedInterval
    {
        get => selectedInterval;
        set { if (value is not null && Set(ref selectedInterval, value)) Notify(nameof(IntervalLabel)); }
    }
    public string IntervalLabel => $"{SelectedInterval.Seconds}s";
    public bool IsLoading { get => isLoading; set { if (Set(ref isLoading, value)) Notify(nameof(CanRefresh)); } }
    private bool refreshEnabled = true;
    public bool RefreshEnabled { get => refreshEnabled; set { if (Set(ref refreshEnabled, value)) Notify(nameof(CanRefresh)); } }
    public bool CanRefresh => RefreshEnabled && !IsLoading;
    public bool IsStale { get => isStale; set { if (Set(ref isStale, value)) Notify(nameof(PeriodsOpacity)); } }
    public double PeriodsOpacity => IsStale ? 0.5 : 1;
    public string Status { get => status; set => Set(ref status, value); }
    public string Message { get => message; set { if (Set(ref message, value)) Notify(nameof(HasMessage)); } }
    public bool HasMessage => !string.IsNullOrEmpty(Message);
    public void SetPeriods(IEnumerable<UsagePeriod> periods, DateTimeOffset? observedAt = null)
    {
        var incoming = periods.ToArray();
        var estimates = budgetHistory is null ? budgetEstimator.Observe(incoming, observedAt) : budgetHistory.Observe(incoming, PlanType, observedAt);
        for (var index = 0; index < incoming.Length; index++)
        {
            var period = incoming[index];
            var existing = Periods.Skip(index).FirstOrDefault(row => row.Model.Minutes == period.Minutes);
            if (existing is null) Periods.Insert(index, new(period));
            else
            {
                var oldIndex = Periods.IndexOf(existing);
                if (oldIndex != index) Periods.Move(oldIndex, index);
                existing.Update(period);
            }
        }
        while (Periods.Count > incoming.Length) Periods.RemoveAt(Periods.Count - 1);
        var shortestMinutes = incoming.Where(period => period.Minutes > 0).Select(period => period.Minutes).Min();
        foreach (var row in Periods)
        {
            row.ShowSeconds = row.Model.Minutes == shortestMinutes;
            row.Tick(observedAt ?? DateTimeOffset.UtcNow);
            row.SetBudgetEstimate(row.Model.Minutes is { } minutes && estimates.TryGetValue(minutes, out var estimate) ? estimate : null);
        }
    }
    public void Tick(DateTimeOffset now)
    {
        foreach (var period in Periods) period.Tick(now);
    }
}

public sealed class UsagePeriodViewModel : ObservableModel
{
    public UsagePeriod Model { get; private set; }
    private string countdown;
    private double? budgetEstimate;
    public bool ShowSeconds { get; set; } = true;
    public UsagePeriodViewModel(UsagePeriod model) { Model = model; countdown = model.Countdown(DateTimeOffset.UtcNow); }
    public string Label => budgetEstimate is { } estimate
        ? $"{Model.Label} (~{estimate.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}x)" : Model.Label;
    public void SetBudgetEstimate(double? estimate)
    {
        if (!Set(ref budgetEstimate, estimate)) return;
        Notify(nameof(Label));
        Notify(nameof(AccessibleLabel));
    }
    public double Remaining => Model.Remaining;
    public string RemainingText => $"{Math.Floor(Remaining)}%";
    public bool IsLow => Remaining <= 10;
    public bool IsNormal => !IsLow;
    public string AccessibleLabel => $"{Label} remaining capacity";
    public string ResetText => Model.Reset is { } reset ? $"Resets at {reset.ToLocalTime():g}" : "Reset time unavailable";
    public string Countdown { get => countdown; private set => Set(ref countdown, value); }
    public void Update(UsagePeriod model)
    {
        var previous = Model;
        Model = model;
        if (previous.Minutes != model.Minutes) { Notify(nameof(Label)); Notify(nameof(AccessibleLabel)); }
        if (previous.Remaining != model.Remaining)
        {
            Notify(nameof(Remaining)); Notify(nameof(RemainingText));
            Notify(nameof(IsLow)); Notify(nameof(IsNormal));
        }
        if (previous.Reset != model.Reset) Notify(nameof(ResetText));
        Tick(DateTimeOffset.UtcNow);
    }
    public void Tick(DateTimeOffset now) => Countdown = Model.Countdown(now, ShowSeconds);
}
