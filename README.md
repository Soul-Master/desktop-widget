# Codex Usage Widget

Keep your Codex limits in sight and your focus on building. Codex Usage Widget brings your remaining usage, reset countdowns, and estimated weekly headroom to a compact Windows 11 desktop card—so you can check your budget at a glance and plan your next coding session.

<img width="802" height="554" alt="usage-limit" src="https://github.com/user-attachments/assets/64963d03-6975-458f-bb90-ac626b122b8a" />

## Features

- **Know what's left.** See remaining capacity across your general Codex usage windows, with animated progress bars, low-capacity highlights, and your ChatGPT plan badge.
- **See when you can start fresh.** Live countdowns and local reset times show when each usage window is due to reset.
- **Put your weekly budget in perspective.** The widget learns from your usage to estimate how many full short-window budgets remain in a longer window. Estimates appear as it gathers data and vary with workload and limits.
- **Keep an eye on usage from the tray.** A numeric tray indicator tracks your shortest usage window. Hide the card while monitoring continues, then bring it back with a click.
- **Make it feel at home on Windows.** A compact, always-on-top card sits above the taskbar with rounded corners, acrylic blur, system theme support, and adjustable acrylic tint. Windows transparency settings may affect the appearance.
- **Stay up to date.** Usage refreshes every 30 seconds by default, with adjustable intervals and manual refresh. If a refresh fails, the last values dim and are marked offline.
- **Use your existing Codex sign-in.** Connect through the Codex CLI with your ChatGPT account. The widget delegates authentication to Codex without reading or storing credentials; settings and usage history stay local.

Requires Windows 11 and a Codex CLI sign-in with a ChatGPT account. The widget shows general Codex subscription limits; model-specific limits and API-key usage are not included.

## Run

Requires Windows 11, .NET 8 Desktop Runtime, and the Windows App SDK runtime matching the project (Visual Studio can install development dependencies).

```powershell
dotnet build DesktopWidget.UI/DesktopWidget.UI.csproj -p:Platform=x64 -p:RuntimeIdentifier=win-x64
& ./DesktopWidget.UI/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/DesktopWidget.UI.exe
```

In Visual Studio, select x64 and the **DesktopWidget.UI (Unpackaged)** profile.

## Connect your account

Sign in to the Codex CLI using your ChatGPT account:

```powershell
codex login
```

The widget discovers `codex.exe` on PATH or in the installed VS Code OpenAI extension. If necessary, set `CODEX_WIDGET_CLI` to the full path of a native `codex.exe` before launching. For other architectures or installations, use this override. The widget inherits `CODEX_HOME` when set. The desktop app's sign-in may not be shared with the CLI; check `codex login status`.

API service-account keys do not supply ChatGPT subscription quota data. Do not put keys into this repository. The widget delegates authentication to Codex and does not read or store credentials.

## Behavior

- Uses the documented `codex app-server` initialize/initialized handshake and `account/rateLimits/read` request: https://learn.chatgpt.com/docs/app-server
- Displays every returned window in the **general `codex` bucket**, ordered by duration. Model-specific buckets are intentionally excluded.
- Displays the ChatGPT plan as a badge beside the header, using the general limits bucket's `planType` or `account/read` when absent. Unknown plans are hidden; the last known badge dims while offline. Account email is not stored.
- Bars and percentages indicate **remaining** capacity. The tray number (percentage units; hover for the percent sign and details) tracks the shortest known period, not the lowest numerical percentage.
- Longer-period labels show estimates such as **1 week (~10.5x)**: the remaining weekly budget is approximately 10.5 full shortest-period budgets. The shortest period has no multiplier. The widget learns relative capacity from paired percentage drops, using `long remaining / 100 × (short drop / long drop)`, not the ratio of period durations. Estimates appear after at least one percentage point of consumption in both windows and update as usage changes. Only valid, changed usage observations are appended to `%LOCALAPPDATA%\DesktopWidget\usage-history-<account-hash>.jsonl`. History is replayed on startup using the original observation timestamps. Normal resets start a fresh comparison while retaining the learned ratio; plan changes, unexpected refills, or changed/missing windows invalidate calibration. Rounded percentages and changes in workload or limits can affect accuracy; missing reset metadata prevents estimation.
- Refreshes every 30 seconds by default (adjustable from the status button), with a 25-second timeout. Countdowns update every second. Reset timestamps use local time.
- Failed refreshes dim the last values and label them offline. The tray displays a dash when unavailable or when its reset timestamp has passed. It never assumes a reset means 100% remaining.
- Click the tray icon to show/hide. Right-click for refresh or exit. The card's close button hides it to the tray; monitoring continues.
- Acrylic uses the system fallback when Windows disables transparency. Notification dimensions vary by Windows version and display settings; this is a notification-inspired persistent window, not a Windows toast. It can overlap actual notifications. Exclusive fullscreen apps and secure desktop may appear above it.
- Settings → **Acrylic Tint Opacity** offers **System default** or **0–100%** in 10% steps, applied immediately and saved across restarts. This adjusts the acrylic color tint, not the opacity of text or the entire window; 0% still includes acrylic blur/luminosity. Windows transparency and accessibility settings can override the effect.
- Windows may initially place the tray icon in the overflow area; pin it using Windows taskbar settings.

## Verification

```powershell
dotnet run --project DesktopWidget.Checks
dotnet run --project DesktopWidget.Checks -- --live
```

The second command requires a signed-in Codex CLI and network connectivity; it reads account limits without starting a model turn. Checks cover sorting, general-bucket selection, nullable fields, remaining percentage calculation, clamping, and countdown expiry.

## Structure

- `UsageModels.cs`: typed app-server response, bucket, and window models with source-generated System.Text.Json serialization metadata.
- `UsageService.cs`: app-server transport and conversion to domain periods.
- `UsageViewModel.cs`: observable presentation state, interval choices, and countdown updates.
- `MainWindow.xaml`: compiled bindings and typed item templates for all widget content.
- `MainWindow.xaml.cs`: window positioning, tray integration, refresh orchestration, and lifecycle; no programmatic row construction.

### Budget history

The append-only JSON Lines file contains version, observedAt (UTC), planType, periods (minutes, remaining percentage, reset timestamp), estimates, and a comparison segment ID. Unchanged percentages, resets/refills, failed refreshes, and missing/invalid observations are excluded from new log entries and learning. Reset/refill readings only establish an internal comparison boundary. Segment IDs prevent replay from comparing across excluded intervals. Existing historical records are retained; legacy observations pass through the same estimator filtering on replay. No credentials or account email are recorded. Data is retained without automatic deletion, so the file grows over time. Existing diagnostic fields in old records are ignored; new records contain no diagnostic fields. Startup recalculates estimator state from the original observations. Invalid lines are skipped. History is separated by the SHA-256 hash of the normalized ChatGPT account email returned by account/read. Switching accounts loads the matching file and estimator; an unavailable identity disables persistence until it can be resolved. The email is used only in memory and is not written to the file. Workspaces sharing the same email share a file. The old unscoped usage-history.jsonl is preserved but not automatically imported because its account is unknown. Settings and history files are created by the updated app when it starts.

### App settings

The app creates `%LOCALAPPDATA%\DesktopWidget\settings.json` on startup with `refreshIntervalSeconds: 30` and `tintOpacityPercent: -1` (system default). Changes from the interval and tint menus are saved automatically. Manual file edits take effect after restarting the app. Supported intervals are 15, 30, 60, 120, 300, and 600 seconds; tint supports -1 or 0–100 in steps of 10. Invalid values fall back to defaults. Unreadable or malformed files do not prevent startup; storage failures are handled without diagnostic output.

### UI demo

Launch the built app with `DesktopWidget.exe --demo` to replay the bundled `Demo/plus-session.json`, or `DesktopWidget.exe --demo "C:\path\scenario.json"` for another fixture. Launch without the flag for live usage. Exit the existing widget first to avoid overlapping windows.
Demo sessions read existing settings without creating or modifying settings files. Tint and interval changes are session-only. Usage history remains in memory; no history/log files are created or changed.
