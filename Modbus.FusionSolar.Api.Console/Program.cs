using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Modbus.FusionSolar.Api;
using Modbus.FusionSolar.Api.Models;

const string usage = "Usage: Modbus.FusionSolar.Api.Console <host> [--port 502] [--unit-id 1] [--interval 5]";

if (args.Length == 1 && args[0] is "--help" or "-h")
{
    Console.WriteLine(usage);
    return 0;
}

string host;
var defaults = new FusionSolarConnectionOptions();
var port = defaults.Port;
var unitId = defaults.UnitId;
var intervalSeconds = 5;
try
{
    if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]) || args[0].StartsWith('-'))
        throw new ArgumentException("An inverter host name or IP address is required.");
    host = args[0];
    for (var index = 1; index < args.Length; index++)
    {
        switch (args[index])
        {
            case "--port":
                if (++index >= args.Length || !int.TryParse(args[index], out port) || port is < 1 or > 65535)
                    throw new ArgumentException("--port requires a number between 1 and 65535.");
                break;
            case "--unit-id":
                if (++index >= args.Length || !byte.TryParse(args[index], out unitId))
                    throw new ArgumentException("--unit-id requires a number between 0 and 255.");
                break;
            case "--interval":
                if (++index >= args.Length || !int.TryParse(args[index], out intervalSeconds) || intervalSeconds is < 1 or > 3600)
                    throw new ArgumentException("--interval requires a number of seconds between 1 and 3600.");
                break;
            default:
                throw new ArgumentException($"Unknown option '{args[index]}'.");
        }
    }
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(usage);
    return 2;
}

using var stop = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stop.Cancel();
};
Console.CancelKeyPress += cancelHandler;

try
{
    await using var client = new FusionSolarClient(host, new() { Port = port, UnitId = unitId });
    var solarTimeAccessor = new FusionSolarDataAccessor<SolarDataTime>(client);
    var solarAccessor = new FusionSolarDataAccessor<SolarProductionData>(client);
    var jsonContext = new SolarJsonContext(new JsonSerializerOptions(SolarJsonContext.Default.Options)
    {
        WriteIndented = !Console.IsOutputRedirected
    });
    while (!stop.IsCancellationRequested)
    {
        try
        {
            using var reading = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            reading.CancelAfter(TimeSpan.FromSeconds(30));
            var solarDataTime = await solarTimeAccessor.ReadValueAsync(reading.Token);
            var solar = await solarAccessor.ReadValueAsync(reading.Token);
            var snapshot = SolarPowerSnapshot.From(solar, true);
            var json = JsonSerializer.Serialize(snapshot, jsonContext.Options);
            // The inverter reports local epoch seconds; preserve its wall-clock time without a UTC conversion.
            var solarDataTimeText = solarDataTime.SystemTimeLocalTime is { } seconds
                ? new DateTime(1970, 1, 1).AddSeconds((double)seconds)
                    .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : "Unavailable";

            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }

            Console.WriteLine($"SolarDataTime: {solarDataTimeText} (inverter local time)");
            Console.WriteLine(json);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            break;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Solar read exceeded 30 seconds; retrying after the polling interval.");
        }
        catch (Exception exception) when (exception is IOException or System.Net.Sockets.SocketException or TimeoutException)
        {
            Console.Error.WriteLine($"Solar read failed: {exception.Message} Retrying after the polling interval.");
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stop.Token);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            break;
        }
    }
    return 0;
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SolarPowerSnapshot))]
internal partial class SolarJsonContext : JsonSerializerContext;
