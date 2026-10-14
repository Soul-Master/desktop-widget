using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using DesktopWidget.UI;

namespace DesktopWidget.Host;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try { Run(args); }
        catch (Exception exception)
        {
            MessageBox(IntPtr.Zero, exception.Message, "Codex Usage Widget", 0x10);
        }
    }

    private static void Run(string[] args)
    {
        var demoIndex = Array.IndexOf(args, "--demo");
        var demoPath = demoIndex < 0 ? null : demoIndex + 1 < args.Length && !args[demoIndex + 1].StartsWith("--", StringComparison.Ordinal)
            ? Path.GetFullPath(args[demoIndex + 1]) : Path.Combine(AppContext.BaseDirectory, "Demo", "plus-session.json");
        // One live host per user and Windows session; demos have isolated state and pipes.
        var identity = WindowsIdentity.GetCurrent().User?.Value + ":" + Process.GetCurrentProcess().SessionId;
        var suffix = demoPath is null ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))
            : "Demo." + Environment.ProcessId;
        var pipeName = "DesktopWidget.Monitor." + suffix;
        using var mutex = new Mutex(true, "Local\\" + pipeName, out var created);
        if (!created)
        {
            MonitorProtocol.RequestAsync(pipeName, new() { Action = "show" }, CancellationToken.None).GetAwaiter().GetResult();
            return;
        }
        try
        {
            using var host = new TrayHost(pipeName, demoPath);
            host.Run(!args.Contains("--hidden"));
        }
        finally { mutex.ReleaseMutex(); }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr window, string text, string caption, uint type);
}

internal sealed class TrayHost : IDisposable
{
    private const uint ShowMessage = 0x8002;
    private readonly string pipeName;
    private readonly string? demoPath;
    private readonly uint thread = GetCurrentThreadId();
    private readonly UsageMonitor monitor;
    private readonly CancellationTokenSource lifetime = new();
    private readonly TrayIndicator tray;
    private readonly Task server;
    private Process? ui;
    private long uiWindow;
    private int uiProcessId;
    private bool wanted, closing, exiting;

    public TrayHost(string pipeName, string? demoPath)
    {
        this.pipeName = pipeName;
        this.demoPath = demoPath;
        // Force the thread message queue to exist before the pipe can post commands.
        PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        monitor = new UsageMonitor(demoPath is null ? null : new DemoUsageService(demoPath));
        tray = new TrayIndicator(Toggle, () => _ = monitor.Refresh(), Exit);
        server = ServeAsync();
    }

    public void Run(bool show)
    {
        wanted = show;
        ReconcileWindow();
        Tick();
        var timer = SetTimer(IntPtr.Zero, UIntPtr.Zero, 1000, IntPtr.Zero);
        if (timer == UIntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            int result;
            while ((result = GetMessage(out var message, IntPtr.Zero, 0, 0)) > 0)
            {
                try
                {
                    if (message.Id == ShowMessage)
                    {
                        ReconcileWindow();
                        wanted = true;
                        ReconcileWindow();
                        var hwnd = (IntPtr)Interlocked.Read(ref uiWindow);
                        if (hwnd != IntPtr.Zero && !closing) SetForegroundWindow(hwnd);
                    }
                    else if (message.Id == 0x113 && message.Window == IntPtr.Zero && message.WParam == timer) Tick();
                    else { TranslateMessage(ref message); DispatchMessage(ref message); }
                }
                catch (Exception exception) { Debug.WriteLine(exception); }
            }
            if (result < 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally { KillTimer(IntPtr.Zero, timer); }
    }

    private void Toggle()
    {
        if (ui?.HasExited == true) ReconcileWindow();
        wanted = !wanted;
        ReconcileWindow();
    }

    private void ReconcileWindow()
    {
        if (ui is not null && ui.HasExited)
        {
            ui.Dispose();
            ui = null;
            Interlocked.Exchange(ref uiWindow, 0);
            Volatile.Write(ref uiProcessId, 0);
            // A user close hides the card. A toggle during shutdown can request reopening.
            if (!closing) wanted = false;
            closing = false;
        }
        if (ui is not null)
        {
            var hwnd = (IntPtr)Interlocked.Read(ref uiWindow);
            if (!wanted && !closing && hwnd != IntPtr.Zero)
            {
                PostMessage(hwnd, 0x10, UIntPtr.Zero, IntPtr.Zero); // WM_CLOSE, normal cleanup
                closing = true;
            }
            return;
        }
        if (!wanted || exiting) return;
        var path = Path.Combine(AppContext.BaseDirectory, "DesktopWidget.Window.exe");
        if (!File.Exists(path))
        {
            wanted = false;
            throw new FileNotFoundException("The widget window is missing. Keep the entire publish folder together.", path);
        }
        var start = new ProcessStartInfo(path) { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory };
        start.ArgumentList.Add("--monitor-pipe");
        start.ArgumentList.Add(pipeName);
        if (demoPath is not null) start.ArgumentList.Add("--demo-session");
        ui = Process.Start(start) ?? throw new InvalidOperationException("Unable to start the widget window.");
        Volatile.Write(ref uiProcessId, ui.Id);
    }

    private void Tick()
    {
        ReconcileWindow();
        var state = monitor.Snapshot();
        var shortest = state.Periods.FirstOrDefault(p => p.Minutes.HasValue);
        var available = !state.IsStale && shortest is not null && !(shortest.Reset <= state.Now);
        var tooltip = "Codex - Usage Limits";
        tooltip += state.Periods.Length == 0 ? "\nUsage unavailable" : "\n" + string.Join("\n", state.Periods.Select(p =>
            $"{p.Label}  {Math.Floor(p.Remaining)}%{(p.Reset <= state.Now ? " (awaiting refresh)" : string.Empty)}"));
        if (state.IsStale) tooltip += "\nOffline - last known values";
        tray.Update(available ? (int)Math.Floor(shortest!.Remaining) : null, tooltip);
    }

    private async Task ServeAsync()
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                await using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(lifetime.Token);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                try
                {
                    var command = await MonitorProtocol.ReadAsync(pipe, MonitorJsonContext.Default.MonitorCommand, timeout.Token);
                    switch (command.Action)
                    {
                        case "refresh": _ = monitor.Refresh(); break;
                        case "interval": monitor.SetInterval(command.IntervalSeconds); break;
                        case "settings" when command.Settings is not null: monitor.SetWindowSettings(command.Settings); break;
                        case "ready":
                            // Never send WM_CLOSE to a window outside the child process we own.
                            GetWindowThreadProcessId((IntPtr)command.WindowHandle, out var processId);
                            if (processId != 0 && processId == Volatile.Read(ref uiProcessId))
                                Interlocked.Exchange(ref uiWindow, command.WindowHandle);
                            break;
                        case "show": PostThreadMessage(thread, ShowMessage, UIntPtr.Zero, IntPtr.Zero); break;
                    }
                    await MonitorProtocol.WriteAsync(pipe, monitor.Snapshot(), MonitorJsonContext.Default.MonitorState, timeout.Token);
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or System.Text.Json.JsonException or OperationCanceledException)
                { Debug.WriteLine(exception); }
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }

    private void Exit()
    {
        exiting = true;
        wanted = false;
        ReconcileWindow();
        PostQuitMessage(0);
    }

    public void Dispose()
    {
        exiting = true;
        lifetime.Cancel();
        tray.Dispose();
        if (ui is not null)
        {
            if (!ui.HasExited)
            {
                var hwnd = (IntPtr)Interlocked.Read(ref uiWindow);
                if (hwnd != IntPtr.Zero) PostMessage(hwnd, 0x10, UIntPtr.Zero, IntPtr.Zero);
                if (!ui.WaitForExit(5000)) { ui.Kill(entireProcessTree: true); ui.WaitForExit(); }
            }
            ui.Dispose();
        }
        server.GetAwaiter().GetResult();
        monitor.StopAsync().GetAwaiter().GetResult();
        monitor.Dispose();
        lifetime.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)] private struct Message
    {
        public IntPtr Window;
        public uint Id;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X, Y;
        public uint Private;
    }
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", SetLastError = true)] private static extern int GetMessage(out Message message, IntPtr window, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll", SetLastError = true)] private static extern UIntPtr SetTimer(IntPtr window, UIntPtr id, uint milliseconds, IntPtr callback);
    [DllImport("user32.dll")] private static extern bool KillTimer(IntPtr window, UIntPtr id);
    [DllImport("user32.dll")] private static extern bool PostThreadMessage(uint thread, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int exitCode);
}
