using DesktopWidget.UI;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class HostLifecycleChecks
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine($"PASS {name}");
    }

    public static async Task RunAsync(string executable)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        executable = Path.GetFullPath(executable);
        var directory = Path.GetDirectoryName(executable)!;
        var windowExecutable = Path.Combine(directory, "DesktopWidget.Window.exe");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = directory };
        start.ArgumentList.Add("--demo");
        start.ArgumentList.Add("--hidden");
        using var host = Process.Start(start) ?? throw new Exception("Host did not start.");
        var children = new List<Process>();
        var pipeName = "DesktopWidget.Monitor.Demo." + host.Id;
        try
        {
            var initial = await MonitorProtocol.RequestAsync(pipeName, new(), CancellationToken.None);
            Check(!host.HasExited && FindWindowProcess(windowExecutable) is null,
                "Published hidden host starts without a WinUI process");
            Check(host.Modules.Cast<ProcessModule>().All(module =>
                !module.ModuleName.Contains("Microsoft.UI.Xaml", StringComparison.OrdinalIgnoreCase)),
                "Tray host does not load the WinUI XAML runtime");

            // A malformed client must not permanently stop the command server.
            await using (var malformed = new System.IO.Pipes.NamedPipeClientStream(".", pipeName, System.IO.Pipes.PipeDirection.InOut,
                System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly))
            {
                await malformed.ConnectAsync(5000);
                await malformed.WriteAsync(BitConverter.GetBytes(-1));
                await malformed.FlushAsync();
            }
            initial = await MonitorProtocol.RequestAsync(pipeName, new(), CancellationToken.None);
            Check(!host.HasExited, "Published host accepts new clients after a malformed pipe frame");
            await MonitorProtocol.RequestAsync(pipeName, new() { Action = "interval", IntervalSeconds = 120 }, CancellationToken.None);
            await MonitorProtocol.RequestAsync(pipeName, new() { Action = "settings", Settings = new() { TintOpacityPercent = 40 } }, CancellationToken.None);
            await MonitorProtocol.RequestAsync(pipeName, new() { Action = "show" }, CancellationToken.None);
            var first = await WaitForWindowAsync(windowExecutable);
            children.Add(first);
            var firstWindow = VisibleWindow(first.Id);
            Check(firstWindow != IntPtr.Zero, "Published host starts a visible window on demand");
            Check(PostMessage(firstWindow, 0x10, UIntPtr.Zero, IntPtr.Zero), "Window accepts normal WM_CLOSE");
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                await first.WaitForExitAsync(timeout.Token);
            Check(!host.HasExited, "Hiding card terminates WinUI while monitoring stays alive");
            var hidden = await MonitorProtocol.RequestAsync(pipeName, new(), CancellationToken.None);
            await Task.Delay(2200);
            var later = await MonitorProtocol.RequestAsync(pipeName, new(), CancellationToken.None);
            Check(later.Revision > hidden.Revision && later.Periods.Length == 2 && later.Estimates.ContainsKey(10080),
                "Hidden host keeps refreshing usage and budget estimates");
            host.Refresh();
            Console.WriteLine($"INFO Hidden host working set: {host.WorkingSet64 / 1024.0 / 1024:0.0} MiB (demo, this machine only)");
            await MonitorProtocol.RequestAsync(pipeName, new() { Action = "show" }, CancellationToken.None);
            var second = await WaitForWindowAsync(windowExecutable);
            children.Add(second);
            var reopened = await MonitorProtocol.RequestAsync(pipeName, new(), CancellationToken.None);
            Check(second.Id != first.Id && reopened.Settings.TintOpacityPercent == 40 && reopened.RefreshIntervalSeconds == 120,
                "Reopening creates a new window with retained session preferences");
            var hostWindow = OwnedWindow(host.Id, visibleOnly: false);
            PostMessage(hostWindow, 0x8001, UIntPtr.Zero, (IntPtr)0x400); // NIN_SELECT, same path as a tray click
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                await second.WaitForExitAsync(timeout.Token);
            Check(!host.HasExited, "Native tray toggle closes the window process and keeps the host alive");
            PostMessage(hostWindow, 0x8001, UIntPtr.Zero, (IntPtr)0x400);
            var third = await WaitForWindowAsync(windowExecutable);
            children.Add(third);
            Check(third.Id != second.Id, "Native tray toggle recreates a visible window");
            // WM_QUIT goes to the test host only, then its normal finally/Dispose path closes the child.
            var thread = GetWindowThreadProcessId(hostWindow, out _);
            Check(thread != 0 && PostThreadMessage(thread, 0x12, UIntPtr.Zero, IntPtr.Zero),
                "Test host receives shutdown on its owning message thread");
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                await host.WaitForExitAsync(timeout.Token);
            third.Refresh();
            Check(third.HasExited, "Host shutdown reaps the WinUI child");
        }
        finally
        {
            if (!host.HasExited) { host.Kill(entireProcessTree: true); await host.WaitForExitAsync(); }
            foreach (var child in children)
            {
                if (!child.HasExited) { child.Kill(); await child.WaitForExitAsync(); }
                child.Dispose();
            }
        }
    }

    private static async Task<Process> WaitForWindowAsync(string executable)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(15))
        {
            var process = FindWindowProcess(executable);
            if (process is not null)
            {
                if (VisibleWindow(process.Id) != IntPtr.Zero) return process;
                process.Dispose();
            }
            await Task.Delay(100);
        }
        throw new Exception("Published window did not become visible.");
    }

    private static Process? FindWindowProcess(string executable)
    {
        foreach (var process in Process.GetProcessesByName("DesktopWidget.Window"))
        {
            if (!process.HasExited && string.Equals(process.MainModule?.FileName, executable, StringComparison.OrdinalIgnoreCase))
                return process;
            process.Dispose();
        }
        return null;
    }

    private static IntPtr VisibleWindow(int processId) => OwnedWindow(processId, visibleOnly: true);
    private static IntPtr OwnedWindow(int processId, bool visibleOnly)
    {
        var result = IntPtr.Zero;
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out var owner);
            if (owner != processId || (visibleOnly && !IsWindowVisible(window))) return true;
            if (!visibleOnly)
            {
                var className = new System.Text.StringBuilder(256);
                GetClassName(window, className, className.Capacity);
                if (!className.ToString().StartsWith("DesktopWidget.TrayIndicator.", StringComparison.Ordinal)) return true;
            }
            result = window;
            return false;
        }, IntPtr.Zero);
        return result;
    }

    private delegate bool EnumerateWindow(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumerateWindow callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, System.Text.StringBuilder className, int capacity);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool PostThreadMessage(uint thread, uint message, UIntPtr wParam, IntPtr lParam);
}
