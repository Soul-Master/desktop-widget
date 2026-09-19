using DesktopWidget.UI;
using System.Text.Json;
static void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine($"PASS {name}"); }
static IReadOnlyList<UsagePeriod> Parse(string json) => UsageService.Parse(json);
var periods = Parse("""{"rateLimitsByLimitId":{"codex":{"secondary":{"usedPercent":25,"windowDurationMins":10080,"resetsAt":2000000000},"primary":{"usedPercent":61,"windowDurationMins":300,"resetsAt":1900000000}},"other":{"primary":{"usedPercent":99,"windowDurationMins":1}}}}""");
Check(periods.Count == 2 && periods[0].Minutes == 300 && periods[0].Remaining == 39, "General bucket only; shortest period first; remaining calculation");
Check(periods[1].Label == "1 week", "Weekly period label");
foreach (var (minutes, expected) in new (int?, string)[]
{
    (1, "1 minute"), (90, "90 minutes"), (60, "1 hour"), (300, "5 hours"),
    (1440, "1 day"), (2880, "2 days"), (10080, "1 week"), (20160, "2 weeks"),
    (43200, "1 month"), (86400, "2 months"), (44640, "31 days"),
    (null, "Usage window"), (0, "Usage window"), (-1, "Usage window")
})
    Check(new UsagePeriod(minutes, 50, null).Label == expected, $"Period label: {expected} ({minutes} minutes)");
var nullable = Parse("""{"rateLimits":{"primary":{"usedPercent":120,"windowDurationMins":null,"resetsAt":null},"secondary":null}}""");
Check(nullable.Count == 1 && nullable[0].Remaining == 0 && nullable[0].Reset == null, "Legacy response, null metadata, clamping");
Check(Parse("""{"rateLimits":{"limitId":"other","primary":{"usedPercent":1}}}""").Count == 0, "Do not substitute model-specific limits");
var now = DateTimeOffset.UtcNow;
Check(new UsagePeriod(300, 10, now.AddSeconds(-1)).Countdown(now) == "Awaiting refreshed limit", "Expired window is not assumed reset");
Check(new UsagePeriod(300, 10, now.AddSeconds(3661)).Countdown(now) == "1h 01m 01s", "Countdown calculation");
Check(new UsagePeriod(300, 10, now.AddHours(1)).Countdown(now) == "1h 00s", "Shortest countdown always shows seconds");
Check(new UsagePeriod(300, 10, now.AddMilliseconds(500)).Countdown(now) == "00s", "Subsecond countdown shows zero seconds");
Check(new UsagePeriod(10080, 10, now.AddSeconds(3661)).Countdown(now, false) == "1h 01m", "Longer countdown hides seconds");
Check(new UsagePeriod(10080, 10, now.AddSeconds(30)).Countdown(now, false) == "<1m", "Longer countdown below one minute hides seconds");
Check(new UsagePeriod(10080, 10, now.AddDays(9).AddHours(3).AddMinutes(4).AddSeconds(5)).Countdown(now) == "1w 2d 3h 04m 05s", "Countdown splits weeks and days and pads minutes and seconds");
if (args.Contains("--live")) { var live = await new UsageService().ReadSnapshotAsync(CancellationToken.None); Check(live.Periods.Count > 0, "Live Codex account limits received"); Console.WriteLine($"Received {live.Periods.Count} general usage windows. Plan: {live.PlanType ?? "unavailable"}."); }


Check(Parse("""{"rateLimitsByLimitId":{"codex":null},"rateLimits":{"primary":{"usedPercent":10}}}""").Count == 0, "Null general bucket does not fall back to unrelated data");
Check(Parse("""{"rateLimits":{"primary":{"windowDurationMins":300},"secondary":{"usedPercent":10,"resetsAt":9223372036854775807}},"futureMetadata":true}""").Single().Reset is null, "Missing percentage skipped; invalid timestamp handled; unknown metadata ignored");
var envelope = JsonSerializer.Deserialize("""{"id":2,"result":{"rateLimits":{"primary":{"usedPercent":42,"windowDurationMins":300}}}}""", UsageJsonContext.Default.AppServerResponse);
Check(envelope?.Result?.ToPeriods().Single().Remaining == 58, "Typed JSON-RPC response deserialization");
var vm = new UsageViewModel();
var changes = new List<string?>();
vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
Check(vm.SelectedInterval.Seconds == 30 && vm.IntervalLabel == "30s", "Default interval binding");
vm.IsLoading = true;
Check(!vm.CanRefresh && changes.Contains(nameof(vm.CanRefresh)), "Loading notifies refresh visibility");
vm.SelectedInterval = vm.Intervals[2];
Check(vm.IntervalLabel == "60s" && changes.Contains(nameof(vm.IntervalLabel)), "Interval changes notify the status button");
vm.IsStale = true;
vm.Message = string.Empty;
Check(vm.PeriodsOpacity == 0.5 && !vm.HasMessage && changes.Contains(nameof(vm.HasMessage)), "Offline opacity and message visibility binding");
var collectionChanges = 0;
vm.Periods.CollectionChanged += (_, _) => collectionChanges++;
vm.SetPeriods(new[] { new UsagePeriod(300, 39, now.AddSeconds(3661)) });
var row = vm.Periods.Single();
vm.Tick(now);
var countdownChanged = false;
row.PropertyChanged += (_, e) => countdownChanged |= e.PropertyName == nameof(row.Countdown);
vm.Tick(now.AddSeconds(1));
Check(collectionChanges > 0 && row.RemainingText == "39%" && row.Countdown == "1h 01m 00s" && countdownChanged, "Observable rows and live countdown binding");
var countdownVm = new UsageViewModel();
countdownVm.SetPeriods(new[] { new UsagePeriod(10080, 80, now.AddHours(2)), new UsagePeriod(300, 50, now.AddHours(1)) }, now);
Check(countdownVm.Periods[0].Countdown == "2h" && countdownVm.Periods[1].Countdown == "1h 00s", "Seconds follow shortest duration regardless of row order");
countdownVm.SetPeriods(new[] { new UsagePeriod(10080, 80, now.AddHours(2)) }, now);
Check(countdownVm.Periods[0].Countdown == "2h 00s", "Seconds switch when shortest period is removed");
vm.SetPeriods(Array.Empty<UsagePeriod>());
Check(vm.Periods.Count == 0, "Empty response removes previous bound rows");

var animatedVm = new UsageViewModel();
animatedVm.SetPeriods(new[] { new UsagePeriod(300, 50, now), new UsagePeriod(10080, 75, now) });
var shortRow = animatedVm.Periods[0];
var weeklyRow = animatedVm.Periods[1];
var remainingUpdates = 0;
shortRow.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(shortRow.Remaining)) remainingUpdates++; };
animatedVm.SetPeriods(new[] { new UsagePeriod(300, 8, now.AddHours(5)), new UsagePeriod(10080, 75, now) });
Check(ReferenceEquals(shortRow, animatedVm.Periods[0]) && ReferenceEquals(weeklyRow, animatedVm.Periods[1]), "Refresh preserves progress bar row identity");
Check(remainingUpdates == 1 && shortRow.Remaining == 8 && shortRow.IsLow && shortRow.RemainingText == "8%", "Changed percentage notifies animation target and warning state");
animatedVm.SetPeriods(new[] { new UsagePeriod(300, 8, now.AddHours(5)), new UsagePeriod(10080, 75, now) });
Check(remainingUpdates == 1, "Unchanged percentage does not restart animation");
animatedVm.SetPeriods(new[] { new UsagePeriod(60, 90, now), new UsagePeriod(10080, 70, now) });
Check(animatedVm.Periods.Count == 2 && animatedVm.Periods[0].Model.Minutes == 60 && ReferenceEquals(weeklyRow, animatedVm.Periods[1]), "Changed windows preserve surviving rows and remove obsolete rows");

var budgetVm = new UsageViewModel();
var shortReset = now.AddHours(5);
var weekReset = now.AddDays(7);
void BudgetPoll(double shortRemaining, double weekRemaining) => budgetVm.SetPeriods(new[]
{
    new UsagePeriod(300, shortRemaining, shortReset), new UsagePeriod(10080, weekRemaining, weekReset)
});
BudgetPoll(100, 72);
Check(budgetVm.Periods[1].Label == "1 week", "No invented estimate before observing consumption");
BudgetPoll(85, 72);
Check(budgetVm.Periods[1].Label == "1 week", "Rounded unchanged weekly usage does not cause division by zero");
BudgetPoll(70, 70);
Check(budgetVm.Periods[0].Label == "5 hours" && budgetVm.Periods[1].Label == "1 week (~10.5x)", "Paired 30/2 point drop estimates 10.5 full shortest budgets remaining");
BudgetPoll(40, 68);
Check(budgetVm.Periods[1].Label == "1 week (~10.2x)", "Remaining estimate updates using accumulated paired consumption");
BudgetPoll(0, 65);
Check(budgetVm.Periods[1].Label == "1 week (~9.8x)", "Exhausted baseline retains learned capacity without learning from capped usage");
shortReset = shortReset.AddHours(5);
BudgetPoll(100, 65);
Check(budgetVm.Periods[1].Label == "1 week (~9.8x)", "Reset preserves learned capacity but starts a fresh comparison");
BudgetPoll(70, 63);
Check(budgetVm.Periods[1].Label == "1 week (~9.5x)", "Learns again after reset");
BudgetPoll(80, 63);
Check(budgetVm.Periods[1].Label == "1 week", "Unannounced refill invalidates comparison");
budgetVm.SetPeriods(Array.Empty<UsagePeriod>());
BudgetPoll(50, 60);
Check(budgetVm.Periods[1].Label == "1 week", "Missing windows clear observation history");
var estimator = new BudgetEstimator();
estimator.Observe(new[] { new UsagePeriod(300, 100, null), new UsagePeriod(10080, 80, null) });
Check(estimator.Observe(new[] { new UsagePeriod(300, 70, null), new UsagePeriod(10080, 78, null) }).Count == 0, "Missing reset metadata cannot establish a safe comparison");

var accountEnvelope = JsonSerializer.Deserialize("""{"id":3,"result":{"account":{"type":"chatgpt","email":"ignored@example.com","planType":"pro"}}}""", UsageJsonContext.Default.AppServerResponse);
Check(accountEnvelope?.Result?.Account is { Type: "chatgpt", PlanType: "pro" }, "Account response exposes plan without storing email");
var planLimits = JsonSerializer.Deserialize("""{"rateLimitsByLimitId":{"codex":{"planType":"plus"},"other":{"planType":"pro"}}}""", UsageJsonContext.Default.RateLimitsResponse);
Check(planLimits?.PlanType == "plus", "Plan taken from general usage bucket");
var planVm = new UsageViewModel();
Check(!planVm.HasPlan, "Plan badge hidden until known");
planVm.PlanType = "pro";
Check(planVm.HasPlan && planVm.PlanLabel == "Pro", "Plan badge formats API plan");
planVm.PlanType = "future_plan";
Check(planVm.PlanLabel == "Future Plan", "Unknown future plan names retained");
planVm.PlanType = " ";
Check(!planVm.HasPlan && planVm.PlanLabel == string.Empty, "Unavailable plan clears badge");

var historyPath = Path.Combine(Path.GetTempPath(), $"widget-history-{Guid.NewGuid():N}.jsonl");
try
{
    var historicalTime = now.AddDays(-1);
    UsagePeriod[] HistoryPeriods(double shortLeft, double longLeft) => new[]
    {
        new UsagePeriod(300, shortLeft, historicalTime.AddHours(5)),
        new UsagePeriod(10080, longLeft, historicalTime.AddDays(7))
    };
    var history = new BudgetHistory(historyPath);
    history.Observe(HistoryPeriods(100, 72), "pro", historicalTime);
    history.Observe(HistoryPeriods(85, 71), "pro", historicalTime.AddMinutes(1));
    history.Observe(HistoryPeriods(85, 71), "pro", historicalTime.AddMinutes(2));
    Check(File.ReadLines(historyPath).Count(line => !string.IsNullOrWhiteSpace(line)) == 2, "Unchanged percentages are not logged");
    var restarted = new BudgetHistory(historyPath);
    var restored = restarted.Observe(new[] { new UsagePeriod(300, 100, now.AddHours(5)), new UsagePeriod(10080, 70, historicalTime.AddDays(7)) }, "pro", now);
    Check(Math.Abs(restored[10080] - 10.5) < 0.001, "Restart replays historical timestamps and retains ratio across expired historical windows");
    Check(!File.ReadAllText(historyPath).Contains("diagnostics") && !File.ReadAllText(historyPath).Contains("error"), "History omits diagnostic fields");
    var entries = File.ReadLines(historyPath).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => JsonSerializer.Deserialize(line, UsageJsonContext.Default.BudgetHistoryEntry)).ToArray();
    Check(entries.Length == 2 && entries[1]!.Estimates!.ContainsKey(10080), "History includes accepted estimates and omits reset reading");
    restarted.Observe(new[] { new UsagePeriod(300, double.NaN, now.AddHours(5)), new UsagePeriod(10080, 70, historicalTime.AddDays(7)) }, "pro", now);
    restarted.Observe(Array.Empty<UsagePeriod>(), "pro", now);
    Check(File.ReadLines(historyPath).Count(line => !string.IsNullOrWhiteSpace(line)) == 2, "Missing and invalid readings are not logged");
    File.AppendAllText(historyPath, "\n{truncated");
    var recovered = new BudgetHistory(historyPath);
    var switched = recovered.Observe(new[] { new UsagePeriod(300, 90, now.AddHours(5)), new UsagePeriod(10080, 69, now.AddDays(7)) }, "plus", now);
    Check(switched.Count == 0, "Plan changes start a new calibration");
    Check(File.ReadLines(historyPath).Last().StartsWith("{"), "New observations remain separate from truncated history");
}
finally { File.Delete(historyPath); }

var threePeriodPath = Path.Combine(Path.GetTempPath(), $"widget-three-periods-{Guid.NewGuid():N}.jsonl");
try
{
    var shortEnd = now.AddHours(5);
    var middleEnd = now.AddDays(1);
    var highestEnd = now.AddDays(7);
    UsagePeriod[] ThreePeriods(double shortLeft, double middleLeft, double highestLeft) => new[]
    {
        new UsagePeriod(10080, highestLeft, highestEnd),
        new UsagePeriod(300, shortLeft, shortEnd),
        new UsagePeriod(1440, middleLeft, middleEnd)
    };
    var history = new BudgetHistory(threePeriodPath);
    history.Observe(ThreePeriods(100, 100, 100), "pro", now);
    var learned = history.Observe(ThreePeriods(80, 90, 98), "pro", now);
    Check(Math.Abs(learned[1440] - 1.8) < 0.001 && Math.Abs(learned[10080] - 9.8) < 0.001,
        "Three unordered periods learn independent capacities against the shortest window");
    var beforeReset = File.ReadAllText(threePeriodPath);
    shortEnd = shortEnd.AddHours(5);
    middleEnd = middleEnd.AddDays(1);
    var retained = history.Observe(ThreePeriods(100, 100, 97), "pro", now);
    Check(Math.Abs(retained[1440] - 2) < 0.001 && Math.Abs(retained[10080] - 9.7) < 0.001 &&
        File.ReadAllText(threePeriodPath) == beforeReset,
        "Resetting both lower periods retains history and both learned capacities");
    var updated = history.Observe(ThreePeriods(70, 90, 95), "pro", now);
    Check(Math.Abs(updated[1440] - 2.7) < 0.001 && Math.Abs(updated[10080] - 14.25) < 0.001,
        "Both estimates relearn exclusively from consumption after the two lower resets");
    history = new BudgetHistory(threePeriodPath);
    var restored = history.Observe(ThreePeriods(70, 90, 95), "pro", now);
    Check(Math.Abs(restored[1440] - 2.7) < 0.001 && Math.Abs(restored[10080] - 14.25) < 0.001,
        "Restart restores both estimates after lower-period resets");
    history.Observe(Array.Empty<UsagePeriod>(), "pro", now);
    highestEnd = highestEnd.AddDays(7);
    Check(history.Observe(ThreePeriods(60, 80, 94), "pro", now).Count == 0 &&
        File.ReadAllText(threePeriodPath).Length == 0,
        "Highest reset clears history and all estimates even after a missing reading and without a refill");
    Check(new BudgetHistory(threePeriodPath).Observe(ThreePeriods(60, 80, 94), "pro", now).Count == 0,
        "Restart cannot restore estimates from the previous highest window");
    var fresh = history.Observe(ThreePeriods(40, 70, 92), "pro", now);
    Check(Math.Abs(fresh[1440] - 1.4) < 0.001 && Math.Abs(fresh[10080] - 9.2) < 0.001,
        "Highest reset starts fresh calibration for every longer period");
    Check(history.Observe(ThreePeriods(30, 60, 100), "pro", now).Count == 0 &&
        File.ReadAllText(threePeriodPath).Length == 0,
        "Highest refill without changed reset metadata also clears history");
}
finally { File.Delete(threePeriodPath); }

var exclusions = new BudgetEstimator();
UsagePeriod[] ExclusionPeriods(double shortLeft, double longLeft, DateTimeOffset shortEnd, DateTimeOffset longEnd) => new[]
{
    new UsagePeriod(300, shortLeft, shortEnd), new UsagePeriod(10080, longLeft, longEnd)
};
var endShort = now.AddHours(5);
var endLong = now.AddDays(7);
exclusions.Observe(ExclusionPeriods(90, 90, endShort, endLong), now);
Check(exclusions.Observe(ExclusionPeriods(100, 89, endShort, endLong), now).Count == 0,
    "Short-period increase cannot be treated as usage");
Check(!exclusions.ShouldRecord, "Refill is excluded from persistence");
exclusions.Observe(ExclusionPeriods(80, 87, endShort, endLong), now);
Check(exclusions.Observe(ExclusionPeriods(70, 95, endShort, endLong), now).Count == 0,
    "Long-period increase invalidates calibration even while short usage rises");
var resetOnly = new BudgetEstimator();
resetOnly.Observe(ExclusionPeriods(100, 90, endShort, endLong), now);
Check(resetOnly.Observe(ExclusionPeriods(50, 80, endShort.AddHours(5), endLong), now).Count == 0,
    "Changed reset excludes interval even when both percentages dropped");
Check(!resetOnly.ShouldRecord, "Reset is excluded from persistence");
var afterReset = resetOnly.Observe(ExclusionPeriods(40, 79, endShort.AddHours(5), endLong), now);
Check(Math.Abs(afterReset[10080] - 7.9) < 0.001, "Only post-reset consumption contributes to the new ratio");

var accountA = BudgetHistory.GetAccountKey("first@example.com")!;
var accountB = BudgetHistory.GetAccountKey("second@example.com")!;
Check(accountA == BudgetHistory.GetAccountKey(" FIRST@example.com ") && accountA != accountB, "Stable distinct account keys with normalized emails");
Check(BudgetHistory.GetAccountKey(null) is null && BudgetHistory.GetAccountKey(" ") is null, "Missing identity cannot select a shared history");
var accountDirectory = Path.Combine(Path.GetTempPath(), $"widget-accounts-{Guid.NewGuid():N}");
try
{
    var accountsVm = new UsageViewModel { PlanType = "plus" };
    accountsVm.UseAccountHistory(accountA, accountDirectory);
    accountsVm.SetPeriods(ExclusionPeriods(100, 72, endShort, endLong));
    accountsVm.SetPeriods(ExclusionPeriods(70, 70, endShort, endLong));
    Check(accountsVm.Periods[1].Label == "1 week (~10.5x)", "First account learns its own budget");
    var firstPath = accountsVm.HistoryFilePath;
    accountsVm.UseAccountHistory(accountB, accountDirectory);
    accountsVm.SetPeriods(ExclusionPeriods(70, 70, endShort, endLong));
    Check(accountsVm.HistoryFilePath != firstPath && accountsVm.Periods[1].Label == "1 week", "Same-plan account switch isolates history and estimate");
    accountsVm.UseAccountHistory(accountA, accountDirectory);
    accountsVm.SetPeriods(ExclusionPeriods(70, 70, endShort, endLong));
    Check(accountsVm.Periods[1].Label == "1 week (~10.5x)", "Returning account restores its own observations");
    accountsVm.UseAccountHistory(null, accountDirectory);
    accountsVm.SetPeriods(ExclusionPeriods(70, 70, endShort, endLong));
    Check(accountsVm.HistoryFilePath is null && accountsVm.Periods[1].Label == "1 week", "Unavailable identity clears previous account history");
}
finally
{
    foreach (var file in Directory.GetFiles(accountDirectory)) File.Delete(file);
    Directory.Delete(accountDirectory);
}

var settingsDirectory = Path.Combine(Path.GetTempPath(), "DesktopWidget-settings-" + Guid.NewGuid().ToString("N"));
var settingsPath = Path.Combine(settingsDirectory, "settings.json");
try
{
    var readOnlySettings = AppSettings.Load(settingsPath, readOnly: true);
    readOnlySettings.TintOpacityPercent = 70;
    readOnlySettings.Save(settingsPath);
    Check(!Directory.Exists(settingsDirectory), "Demo settings do not create a file or directory");
    var settings = AppSettings.Load(settingsPath);
    Check(File.Exists(settingsPath) && settings.RefreshIntervalSeconds == 30 && settings.TintOpacityPercent == -1, "Missing settings created with defaults");
    settings.RefreshIntervalSeconds = 120;
    settings.TintOpacityPercent = 40;
    settings.Save(settingsPath);
    var restored = AppSettings.Load(settingsPath);
    Check(restored.RefreshIntervalSeconds == 120 && restored.TintOpacityPercent == 40, "Settings survive reload");
    var originalSettings = File.ReadAllBytes(settingsPath);
    var originalWriteTime = File.GetLastWriteTimeUtc(settingsPath);
    readOnlySettings = AppSettings.Load(settingsPath, readOnly: true);
    Check(readOnlySettings.TintOpacityPercent == 40, "Demo reads existing preferences");
    readOnlySettings.TintOpacityPercent = 90;
    readOnlySettings.RefreshIntervalSeconds = 15;
    readOnlySettings.Save(settingsPath);
    Check(File.ReadAllBytes(settingsPath).SequenceEqual(originalSettings) && File.GetLastWriteTimeUtc(settingsPath) == originalWriteTime,
        "Demo preference changes preserve settings contents and modification time");
    File.WriteAllText(settingsPath, """{"refreshIntervalSeconds":0,"tintOpacityPercent":33}""");
    restored = AppSettings.Load(settingsPath);
    Check(restored.RefreshIntervalSeconds == 30 && restored.TintOpacityPercent == -1, "Unsupported settings fall back to defaults");
    File.WriteAllText(settingsPath, "broken json");
    AppSettings.Load(settingsPath, readOnly: true).Save(settingsPath);
    Check(AppSettings.Load(settingsPath).RefreshIntervalSeconds == 30 && File.ReadAllText(settingsPath) == "broken json", "Malformed settings do not crash or overwrite original");
}
finally
{
    File.Delete(settingsPath);
    Directory.Delete(settingsDirectory);
}

var demo = new DemoUsageService(Path.Combine(AppContext.BaseDirectory, "Demo", "plus-session.json"));
Check(demo.Configuration.UpdateMilliseconds == 1000 && demo.Configuration.Frames.Length == 24 && demo.Configuration.RetrievalMilliseconds == 200, "Demo timing configuration");
var demoWatch = System.Diagnostics.Stopwatch.StartNew();
var demoSnapshot = await demo.ReadSnapshotAsync(CancellationToken.None);
Check(demoWatch.ElapsedMilliseconds >= 180 && demoSnapshot.PlanType == "plus" && demoSnapshot.Periods.All(p => p.Remaining == 100), "Demo retrieval delay and initial Plus snapshot");
var demoVm = new UsageViewModel();
demoVm.SetPeriods(demoSnapshot.Periods, demo.Now);
var firstDemoRow = demoVm.Periods[0];
var demoEstimator = new BudgetEstimator();
demoEstimator.Observe(demoSnapshot.Periods, demo.Now);
demo.Configuration.RetrievalMilliseconds = 0;
double previousWeeklyEstimate = 0;
for (var frame = 1; frame < demo.Configuration.Frames.Length; frame++)
{
    demoSnapshot = await demo.ReadSnapshotAsync(CancellationToken.None);
    var estimate = demoEstimator.Observe(demoSnapshot.Periods, demo.Now);
    Check(estimate.TryGetValue(10080, out var weekly) && weekly > previousWeeklyEstimate && weekly < 7,
        "Demo remaining weekly estimate increases while staying below full capacity");
    if (frame == 1) Check(Math.Abs(weekly - 3) < 0.001, "Small initial usage learns about 3x remaining");
    previousWeeklyEstimate = weekly;
    demoVm.SetPeriods(demoSnapshot.Periods, demo.Now);
    demoVm.Tick(demo.Now);
}
Check(Math.Abs(previousWeeklyEstimate - 6.05) < 0.001 &&
    Math.Abs(previousWeeklyEstimate * 100 / demoSnapshot.Periods[1].Remaining - 7) < 0.001,
    "High cumulative usage learns 7x full capacity and 6.05x remaining");
Check(demo.Now.ToString("HH:mm") == "14:20" && demoSnapshot.Periods[0].Remaining == 5 &&
    (demoSnapshot.Periods[0].Reset - demo.Now)?.TotalSeconds is > 1858 and <= 1860, "Demo ends at 14:20 with 5 percent and approximately 31 minutes before reset");
Check(ReferenceEquals(firstDemoRow, demoVm.Periods[0]) && demoVm.Periods[0].IsLow && demoVm.HistoryFilePath is null, "Demo preserves animated rows, low state, and isolates history");
var demoEnd = demo.Now;
Check(demo.IsComplete, "Demo completes after final snapshot");
var rejectedExhaustedDemo = false;
try { await demo.ReadSnapshotAsync(CancellationToken.None); }
catch (InvalidOperationException) { rejectedExhaustedDemo = true; }
Check(rejectedExhaustedDemo, "Exhausted demo rejects further retrieval");
await Task.Delay(1100);
demoVm.Tick(demo.Now);
Check(demo.Now >= demoEnd.AddSeconds(1) && demoVm.Periods[0].Countdown == demoSnapshot.Periods[0].Countdown(demo.Now), "Completed demo clock and countdown continue in real time");
demoVm.RefreshEnabled = false;
Check(!demoVm.CanRefresh && !demoVm.IsLoading, "Completed demo disables refresh without showing loading");
using var cancelledDemo = new CancellationTokenSource();
cancelledDemo.Cancel();
try { await demo.ReadSnapshotAsync(cancelledDemo.Token); throw new Exception("Demo cancellation ignored"); }
catch (OperationCanceledException) { Check(demo.IsComplete, "Demo retrieval supports cancellation"); }

