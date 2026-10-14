using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;

namespace DesktopWidget.UI;

public partial class App : Application
{
    private Window? window;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var arguments = Environment.GetCommandLineArgs();
        var pipeIndex = Array.IndexOf(arguments, "--monitor-pipe");
        if (pipeIndex >= 0 && pipeIndex + 1 < arguments.Length)
        {
            try
            {
                var pipeName = arguments[pipeIndex + 1];
                var state = await MonitorProtocol.RequestAsync(pipeName, new(), System.Threading.CancellationToken.None);
                window = new MainWindow(pipeName, state);
                window.Activate();
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or System.Text.Json.JsonException or OperationCanceledException)
            {
                Exit();
            }
            return;
        }
        // Keep F5/direct UI launches useful; normal launches use DesktopWidget.exe (the host).
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "DesktopWidget.exe"))
        { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory };
        for (var index = 1; index < arguments.Length; index++) start.ArgumentList.Add(arguments[index]);
        using var host = Process.Start(start);
        Exit();
    }
}
