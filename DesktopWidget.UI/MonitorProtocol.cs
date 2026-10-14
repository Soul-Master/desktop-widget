using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopWidget.UI;

// Both executables compile this contract. No reflection-based JSON or WinUI types cross the pipe.
public sealed class MonitorState
{
    public AppSettings Settings { get; set; } = new();
    public long Revision { get; set; }
    public UsagePeriod[] Periods { get; set; } = Array.Empty<UsagePeriod>();
    public Dictionary<int, double> Estimates { get; set; } = new();
    public string? PlanType { get; set; }
    public string Status { get; set; } = "Connecting to Codex";
    public string Message { get; set; } = "Connecting to Codex…";
    public bool IsLoading { get; set; }
    public bool IsStale { get; set; }
    public bool DemoComplete { get; set; }
    public int RefreshIntervalSeconds { get; set; } = 30;
    public DateTimeOffset Now { get; set; }
    public int UiTickMilliseconds { get; set; } = 1000;
}

public sealed class MonitorCommand
{
    public string Action { get; set; } = "snapshot";
    public int IntervalSeconds { get; set; }
    public long WindowHandle { get; set; }
    public AppSettings? Settings { get; set; }
}

public static class MonitorProtocol
{
    // Length-prefixed UTF-8 caps allocations and works with asynchronous byte-mode pipes.
    public const int MaximumMessageBytes = 256 * 1024;
    public static async Task WriteAsync<T>(Stream stream, T message,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type, CancellationToken token)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, type);
        if (bytes.Length > MaximumMessageBytes) throw new InvalidDataException("Monitor message is too large.");
        var header = BitConverter.GetBytes(bytes.Length);
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(bytes, token);
        await stream.FlushAsync(token);
    }

    public static async Task<T> ReadAsync<T>(Stream stream,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type, CancellationToken token)
    {
        var header = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(header, token);
        var length = BitConverter.ToInt32(header);
        if (length < 1 || length > MaximumMessageBytes) throw new InvalidDataException("Invalid monitor message length.");
        var bytes = new byte[length];
        await stream.ReadExactlyAsync(bytes, token);
        return JsonSerializer.Deserialize(bytes, type) ?? throw new JsonException("Missing monitor message.");
    }

    public static async Task<MonitorState> RequestAsync(string pipeName, MonitorCommand command, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        await using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync(timeout.Token);
        await WriteAsync(pipe, command, MonitorJsonContext.Default.MonitorCommand, timeout.Token);
        return await ReadAsync(pipe, MonitorJsonContext.Default.MonitorState, timeout.Token);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MonitorState))]
[JsonSerializable(typeof(MonitorCommand))]
public partial class MonitorJsonContext : JsonSerializerContext { }
