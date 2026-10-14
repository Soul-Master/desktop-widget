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

Requires Windows 11, the .NET 10 SDK for development builds, and the Windows App SDK runtime matching the project (Visual Studio can install development dependencies).

```powershell
dotnet build DesktopWidget.UI/DesktopWidget.UI.csproj -c Debug -p:Platform=x64 -p:RuntimeIdentifier=win-x64
& ./DesktopWidget.UI/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/DesktopWidget.exe
```

In Visual Studio, select x64 and the **DesktopWidget.UI (Unpackaged)** profile.

Direct window/F5 launches forward to the tray host. To debug window code, attach to the `DesktopWidget.Window.exe` process after showing the card; it is recreated when the card is reopened.

## Publish

The existing `win-x64` and `win-x86` publish profiles enable Native AOT, trimming, and .NET self-contained deployment. ReadyToRun is disabled. Native compilation happens during publishing; normal development builds still use managed code.

Install Visual Studio's **Desktop development with C++** workload for the native compiler and linker, alongside the .NET 10 SDK. See [Native AOT prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

```powershell
dotnet publish DesktopWidget.UI/DesktopWidget.UI.csproj -c Release -r win-x64 -p:Platform=x64
dotnet publish DesktopWidget.UI/DesktopWidget.UI.csproj -c Release -r win-x86 -p:Platform=x86
```

These commands select the matching existing publish profile. Close a widget running from the destination folder before publishing there, or override `PublishDir` to use another folder. Distribute the entire publish directory, including the native DLLs, resources, and demo fixture. Published Native AOT executables do not require an installed .NET runtime; the matching Windows App SDK runtime and a signed-in Codex CLI are still required for live usage. The profiles do not bundle the Windows App SDK runtime. Launch `DesktopWidget.exe`, the independent tray host; it starts `DesktopWidget.Window.exe` when the card is shown. Both executables and supporting files must stay together. Use `DesktopWidget.exe --hidden` to start monitoring without opening the card.

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
- Account switches through the Codex CLI are picked up on the next scheduled or manual refresh without restarting the widget. Each refresh starts a fresh app-server and reads account identity before and after fetching limits. A detected identity or plan change during retrieval rejects that observation and retries at the next interval; the previous values are marked offline. Optional identity reads each have a 3-second timeout within the overall 25-second refresh timeout. Failed identity checks leave usage visible without assigning it to an account history. The general limits bucket's plan takes precedence, with verified account metadata as a fallback.
- Refreshes every 30 seconds by default (adjustable from the status button), with a 25-second timeout. Countdowns update every second. Reset timestamps use local time.
- Failed refreshes dim the last values and label them offline. The tray displays a dash when unavailable or when its reset timestamp has passed. It never assumes a reset means 100% remaining.
- Click the tray icon to show/hide. Right-click for refresh or exit. The card's close button exits the WinUI window process; the independent tray host keeps monitoring. Reopening starts a fresh window with the current usage, estimates, and preferences. Launching the live host again shows the existing instance for the same user and Windows session. Reopening has process-startup overhead; hidden memory usage still includes monitoring, history, and temporary Codex app-server processes.
- Drag the header to move the card using Windows' native caption dragging; refresh, settings, and hide controls remain interactive passthrough areas. Drag regions follow the header layout and display scaling. The position and selected monitor identity, physical bounds, work area, and DPI are saved after dragging. The position is restored when shown again or restarted only if that monitor still matches. If the selected monitor is disconnected, replaced, rearranged, resized, or its DPI/work area changes, the saved position is cleared and the card returns to the bottom-right of the current primary screen above the taskbar, using that screen's DPI. Changes to unrelated monitors do not invalidate the saved position. Content resizing preserves the chosen position where it fits without overwriting it during layout. The entire card scales with monitor DPI: elements keep a 364-unit logical layout width, and the usable client area is resized in physical pixels. DPI transitions resize during dragging without overriding Windows' move position. On unusually narrow screens, the whole card scales down; tall content remains scrollable. The first launch uses the bottom-right corner of the primary screen above the taskbar.
- Acrylic uses the system fallback when Windows disables transparency. Notification dimensions vary by Windows version and display settings; this is a notification-inspired persistent window, not a Windows toast. It can overlap actual notifications. Exclusive fullscreen apps and secure desktop may appear above it.
- Settings → **Acrylic Tint Opacity** offers **System default** or **0–100%** in 10% steps, applied immediately and saved across restarts. This adjusts the acrylic color tint, not the opacity of text or the entire window; 0% still includes acrylic blur/luminosity. Windows transparency and accessibility settings can override the effect.
- Windows may initially place the tray icon in the overflow area; pin it using Windows taskbar settings.

## Verification

```powershell
dotnet run --project DesktopWidget.Checks
dotnet run --project DesktopWidget.Checks -- --windows-placement
dotnet run --project DesktopWidget.Checks -- --windows-tray
dotnet run --project DesktopWidget.Checks -- --windows-host "D:\DesktopWidget\artifacts\tray-host-x64\DesktopWidget.exe"
dotnet run --project DesktopWidget.Checks -- --live
```

The `--windows-placement` command also checks native monitor enumeration, device identities, and screen/work-area rectangles on Windows. The `--windows-tray` command requires an interactive Windows session with Explorer running. It briefly creates a test tray icon and opens its native menus, checking numeric rendering and transparency at multiple DPI sizes, action callbacks, callback lifetime, GDI resource cleanup, simulated Explorer-restart recovery, and disposal; it does not contact Codex or restart Explorer. The `--live` command requires a signed-in Codex CLI and network connectivity; it reads account limits without starting a model turn. The `--windows-host` command takes the full path to a built or published host with its window executable alongside it. It starts an isolated demo host, checks malformed-client recovery, hidden monitoring, window termination/recreation, session preferences, absence of the WinUI XAML runtime in the host, and child cleanup. It briefly opens a test card and reports the hidden demo working set; this measurement varies by machine and does not establish live memory usage. It leaves existing live widgets and account data alone. The main checks also cover the monitor protocol, settings ownership, concurrent refreshes, cancellation, and estimates supplied to recreated windows. The main checks cover sorting, general-bucket selection, nullable fields, remaining percentage calculation, clamping, countdown expiry, budget estimation/history, settings, saved window positions and work-area bounds, demo playback, and account switches (including switching back, plan changes, unavailable identity, logout, and changes during retrieval).

For DPI layout verification, launch with `--demo` to use local sample data. Drag the card between monitors set to 200% and 150%, then back, including repeated crossings and monitors left of the primary display. Also change the current monitor's scaling while the card is visible and while hidden to the tray, then reopen it. After each transition, check that the title, percentage, reset time, and countdown retain their layout and the card remains on-screen. On an unconstrained work area, the client width should be 728 physical pixels at 200% and 546 at 150%; check that refresh, settings, and hide still respond within their visible bounds. Verify a wrapped connection message and a long plan badge as well; constrained work areas may still require scrolling or ellipsis. For restore verification, drag to a secondary monitor and restart with the same configuration: the position should be preserved. Change that monitor's DPI, resolution, orientation, position, or taskbar work area, or disconnect it, then restart/reopen: the widget should reset to the primary screen's bottom-right corner and stay there until dragged again. Changing an unrelated monitor's DPI should retain the saved position. Repeat with the primary screen itself at a non-100% DPI. The console checks cover 800 mixed-DPI and monitor-origin combinations, saved monitor validation, primary fallback placement, client sizing, and pixel rounding, but do not exercise WinUI rendering or monitor DPI events.

## Structure

- `UsageModels.cs`: typed app-server response, bucket, and window models with source-generated System.Text.Json serialization metadata.
- `UsageService.cs`: app-server transport and conversion to domain periods.
- `UsageViewModel.cs`: observable presentation state, interval choices, and countdown updates.
- `MainWindow.xaml`: compiled bindings and typed item templates for all widget content.
- `DesktopWidget.Host/Program.cs`: native tray message loop, one live instance per user/session, on-demand window process, and current-user named pipe server.
- `DesktopWidget.Host/UsageMonitor.cs`: refresh scheduling, account history, budget estimates, and settings ownership, independent of window lifetime.
- `MonitorProtocol.cs`: bounded, source-generated JSON messages for host commands and state snapshots.
- `MainWindow.xaml.cs`: window positioning, host state presentation, preferences, and window lifecycle; no programmatic row construction.
- `TrayIndicator.cs`: Win32 notification icon, native context menu, and hidden callback window; the menu uses Windows system styling.
- `TrayIconRenderer.cs`: direct GDI+ rendering using the existing Segoe UI sizing and antialiasing, without a Windows Forms or System.Drawing dependency.

### Budget history

The append-only JSON Lines file contains version, observedAt (UTC), planType, periods (minutes, remaining percentage, reset timestamp), estimates, and a comparison segment ID. Unchanged percentages, resets/refills, failed refreshes, and missing/invalid observations are excluded from new log entries and learning. Reset/refill readings only establish an internal comparison boundary. Segment IDs prevent replay from comparing across excluded intervals. Existing historical records are retained; legacy observations pass through the same estimator filtering on replay. No credentials or account email are recorded. Data is retained without automatic deletion, so the file grows over time. Existing diagnostic fields in old records are ignored; new records contain no diagnostic fields. Startup recalculates estimator state from the original observations. Invalid lines are skipped. History is separated by the SHA-256 hash of the normalized ChatGPT account email returned by account/read. Switching accounts loads the matching file and estimator; an unavailable identity disables persistence until it can be resolved. The email is used only in memory and is not written to the file. Workspaces sharing the same email share a file. The old unscoped usage-history.jsonl is preserved but not automatically imported because its account is unknown. Settings and history files are created by the updated app when it starts.

### App settings

The tray host creates `%LOCALAPPDATA%\DesktopWidget\settings.json` on startup with `refreshIntervalSeconds: 30` and `tintOpacityPercent: -1` (system default). Changes from the interval and tint menus are saved automatically. `windowX` and `windowY` store the window's top-left screen coordinates in physical pixels after dragging, including negative coordinates for other monitors; both are null until a position is chosen. `windowMonitor` stores the selected monitor's device interface identity, physical screen/work-area rectangles, and DPI. Coordinates are restored only when that snapshot still matches. A changed or missing selected monitor clears both the coordinates and snapshot and restores the primary screen's bottom-right default. Legacy settings with coordinates but no valid monitor snapshot also reset once; the next drag saves the required metadata. Existing settings without coordinates retain the default placement. Manual file edits take effect after restarting the app. Supported intervals are 15, 30, 60, 120, 300, and 600 seconds; tint supports -1 or 0–100 in steps of 10. Invalid values fall back to defaults. Unreadable or malformed files do not prevent startup; storage failures are handled without diagnostic output, so position changes may only last for the current session if saving fails.

### UI demo

Launch the built app with `DesktopWidget.exe --demo` to replay the bundled `Demo/plus-session.json`, or `DesktopWidget.exe --demo "C:\path\scenario.json"` for another fixture. Launch without the flag for live usage. Exit the existing widget first to avoid overlapping windows.
Demo hosts read existing settings without creating or modifying settings files and retain session preferences when the card is closed and reopened. Tint, interval, and position changes are session-only. Usage history remains in memory; no history/log files are created or changed.
